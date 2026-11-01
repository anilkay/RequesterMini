using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using ReactiveUI;
using ReactiveUI.Reactive;
using RequesterMini.Constants;
using RequesterMini.Utils;
using RequesterMini.ViewModels;
using Templating;

namespace RequesterMini.Tests;

public class MainWindowViewModelTests : ReactiveTestBase
{
    private static MainWindowViewModel WithClient(StubHandler handler) =>
        new(new HttpClient(handler));

    /// <summary>Runs the export and returns what it put on the clipboard.</summary>
    private static string ExportCurl(MainWindowViewModel vm)
    {
        var captured = "";
        vm.CopyToClipboard.RegisterHandler(interaction =>
        {
            captured = interaction.Input;
            interaction.SetOutput(Unit.Default);
        });

        vm.ExportCurlCommand.Execute().Subscribe();
        return captured;
    }

    private static void PublishEnvironment(string name, params (string Key, string Value)[] variables)
    {
        var dto = new VariableEnvironment(name, variables.ToDictionary(v => v.Key, v => v.Value));

        MessageBus.Current.SendMessage(
            JsonSerializer.Serialize(dto, SourceGenerationContext.Default.VariableEnvironment),
            MessageBusConstants.ActiveEnvironmentChanged);
    }

    // Query params -------------------------------------------------------

    [Fact]
    public void Url_WithQueryString_PopulatesQueryParams()
    {
        var vm = new MainWindowViewModel { Url = "https://example.test/api?page=2&sort=name" };

        Assert.Collection(vm.QueryParams,
            first => Assert.Equal(("page", "2"), (first.Key, first.Value)),
            second => Assert.Equal(("sort", "name"), (second.Key, second.Value)));
    }

    [Fact]
    public void QueryParam_Edited_RewritesTheUrl()
    {
        var vm = new MainWindowViewModel { Url = "https://example.test/api" };

        vm.AddQueryParamCommand.Execute().Subscribe();
        vm.QueryParams[0].Key = "q";
        vm.QueryParams[0].Value = "a b";

        Assert.Equal("https://example.test/api?q=a%20b", vm.Url);
    }

    [Fact]
    public void QueryParam_Disabled_IsKeptButLeftOutOfTheUrl()
    {
        var vm = new MainWindowViewModel { Url = "https://example.test/api?keep=1" };

        vm.AddQueryParamCommand.Execute().Subscribe();
        vm.QueryParams[1].Key = "skip";
        vm.QueryParams[1].Value = "1";
        vm.QueryParams[1].IsEnabled = false;

        Assert.Equal("https://example.test/api?keep=1", vm.Url);
        Assert.Equal(2, vm.QueryParams.Count);
    }

    // Method and auth state ----------------------------------------------

    [Theory]
    [InlineData("GET", false)]
    [InlineData("POST", true)]
    [InlineData("PUT", true)]
    public void IsBodyEnabled_FollowsTheMethod(string method, bool expected)
    {
        var vm = new MainWindowViewModel { SelectedHttpMethod = method };

        Assert.Equal(expected, vm.IsBodyEnabled);
    }

    [Theory]
    [InlineData("None", false, false, false)]
    [InlineData("Basic", true, false, true)]
    [InlineData("Bearer", false, true, true)]
    public void AuthType_DrivesWhichFieldsAreShown(string authType, bool basic, bool bearer, bool anyCredentials)
    {
        var vm = new MainWindowViewModel { SelectedAuthType = authType };

        Assert.Equal(basic, vm.IsBasicAuthSelected);
        Assert.Equal(bearer, vm.IsBearerAuthSelected);
        Assert.Equal(anyCredentials, vm.IsAuthCredentialsVisible);
    }

    // cURL export ---------------------------------------------------------

    [Fact]
    public void ExportCurl_MethodAndUrl_AreIncluded()
    {
        var vm = new MainWindowViewModel { SelectedHttpMethod = "POST", Url = "https://example.test/api" };

        var curl = ExportCurl(vm);

        Assert.Contains("-X POST", curl);
        Assert.Contains("'https://example.test/api'", curl);
    }

    [Fact]
    public void ExportCurl_BasicAuth_UsesTheUserFlag()
    {
        var vm = new MainWindowViewModel
        {
            Url = "https://example.test/api",
            SelectedAuthType = "Basic",
            AuthUsername = "user",
            AuthPassword = "pass",
        };

        Assert.Contains("-u 'user:pass'", ExportCurl(vm));
    }

    [Fact]
    public void ExportCurl_BearerAuth_AddsTheAuthorizationHeader()
    {
        var vm = new MainWindowViewModel
        {
            Url = "https://example.test/api",
            SelectedAuthType = "Bearer",
            AuthToken = "abc123",
        };

        Assert.Contains("-H 'Authorization: Bearer abc123'", ExportCurl(vm));
    }

    [Fact]
    public void ExportCurl_BearerAuth_WinsOverAHandTypedAuthorizationHeader()
    {
        var vm = new MainWindowViewModel
        {
            Url = "https://example.test/api",
            SelectedAuthType = "Bearer",
            AuthToken = "abc123",
        };
        vm.AddHeaderCommand.Execute().Subscribe();
        vm.Headers[0].Key = "Authorization";
        vm.Headers[0].Value = "Basic stale";

        var curl = ExportCurl(vm);

        Assert.Contains("Bearer abc123", curl);
        Assert.DoesNotContain("Basic stale", curl);
    }

    [Fact]
    public void ExportCurl_DisabledHeader_IsOmitted()
    {
        var vm = new MainWindowViewModel { Url = "https://example.test/api" };
        vm.AddHeaderCommand.Execute().Subscribe();
        vm.Headers[0].Key = "X-Off";
        vm.Headers[0].Value = "1";
        vm.Headers[0].IsEnabled = false;

        Assert.DoesNotContain("X-Off", ExportCurl(vm));
    }

    [Fact]
    public void ExportCurl_FormBody_UsesTheFormContentType()
    {
        var vm = new MainWindowViewModel
        {
            SelectedHttpMethod = "POST",
            Url = "https://example.test/api",
            SelectedBodyType = "Form",
            Body = "a=1&b=2",
        };

        var curl = ExportCurl(vm);

        Assert.Contains("-H 'Content-Type: application/x-www-form-urlencoded'", curl);
        Assert.Contains("-d 'a=1&b=2'", curl);
    }

    // Variables ------------------------------------------------------------

    [Fact]
    public void ActiveEnvironmentMessage_UpdatesTheDisplayedName()
    {
        var vm = new MainWindowViewModel();

        PublishEnvironment("dev", ("baseUrl", "https://api.test"));

        Assert.Equal("dev", vm.ActiveEnvironmentName);
    }

    [Fact]
    public void ActiveEnvironmentMessage_WithNoEnvironment_SaysSo()
    {
        var vm = new MainWindowViewModel();

        PublishEnvironment("");

        Assert.Equal("No environment", vm.ActiveEnvironmentName);
    }

    [Fact]
    public void ExportCurl_ResolvesPlaceholdersFromTheActiveEnvironment()
    {
        var vm = new MainWindowViewModel();
        PublishEnvironment("dev", ("baseUrl", "https://api.test"));
        vm.Url = "{{baseUrl}}/posts";

        var curl = ExportCurl(vm);

        Assert.Contains("https://api.test/posts", curl);
        Assert.DoesNotContain("{{baseUrl}}", curl);
    }

    // Sending ---------------------------------------------------------------

    [Fact]
    public async Task Send_Success_FillsTheResponseFields()
    {
        var handler = new StubHandler { ResponseBody = "{\"id\":1}" };
        var vm = WithClient(handler);
        vm.Url = "https://example.test/api";

        await vm.ClickCommand.Execute();

        Assert.Equal("OK", vm.ResponseStatusCode);
        Assert.Equal("{\"id\":1}", vm.ResponseBody);
        Assert.NotEqual("", vm.RequestTime);
    }

    [Fact]
    public async Task Send_ResolvesPlaceholdersInTheUrl()
    {
        var handler = new StubHandler();
        var vm = WithClient(handler);
        PublishEnvironment("dev", ("baseUrl", "https://api.test"));
        vm.Url = "{{baseUrl}}/posts";

        await vm.ClickCommand.Execute();

        Assert.Equal("https://api.test/posts", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task Send_BearerAuth_SendsTheAuthorizationHeader()
    {
        var handler = new StubHandler();
        var vm = WithClient(handler);
        vm.Url = "https://example.test/api";
        vm.SelectedAuthType = "Bearer";
        vm.AuthToken = "abc123";

        await vm.ClickCommand.Execute();

        Assert.Equal("Bearer abc123", handler.LastRequest!.Headers.GetValues("Authorization").Single());
    }

    [Fact]
    public async Task Send_FormBody_IsSentUrlEncoded()
    {
        var handler = new StubHandler();
        var vm = WithClient(handler);
        vm.Url = "https://example.test/api";
        vm.SelectedHttpMethod = "POST";
        vm.SelectedBodyType = "Form";
        vm.Body = "name=ada&role=engineer";

        await vm.ClickCommand.Execute();

        Assert.Equal(
            "application/x-www-form-urlencoded",
            handler.LastRequest!.Content!.Headers.ContentType!.MediaType);
        Assert.Equal("name=ada&role=engineer", handler.LastRequestBody);
    }

    [Fact]
    public async Task Send_ErrorStatus_IsReportedAsThatStatus()
    {
        var handler = new StubHandler { Status = HttpStatusCode.NotFound, ResponseBody = "nope" };
        var vm = WithClient(handler);
        vm.Url = "https://example.test/api";

        await vm.ClickCommand.Execute();

        Assert.Equal("NotFound", vm.ResponseStatusCode);
    }

    [Fact]
    public async Task Send_MalformedProxyUrl_ReportsItWithoutSending()
    {
        var handler = new StubHandler();
        var vm = WithClient(handler);
        vm.Url = "https://example.test/api";
        vm.ProxyUrl = "proxy.test:8080";

        await vm.ClickCommand.Execute();

        Assert.Equal("Error", vm.ResponseStatusCode);
        Assert.Contains("proxy.test:8080", vm.ResponseBody);
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task Send_WithoutAnHttpClient_DoesNothing()
    {
        var vm = new MainWindowViewModel { Url = "https://example.test/api" };

        await vm.ClickCommand.Execute();

        Assert.Equal("", vm.ResponseStatusCode);
    }

    // Loading a saved request -------------------------------------------------

    [Fact]
    public void LoadRequestMessage_RestoresTheRequest()
    {
        var vm = new MainWindowViewModel();
        var dto = new OldRequestDto(
            "PUT",
            "https://example.test/items/1?tag=x",
            "{\"a\":1}",
            "OK",
            "{}",
            new Dictionary<string, string> { ["X-Trace"] = "abc" },
            "http://proxy.test:8080");

        MessageBus.Current.SendMessage(
            JsonSerializer.Serialize(dto, SourceGenerationContext.Default.OldRequestDto),
            MessageBusConstants.LoadRequest);

        Assert.Equal("PUT", vm.SelectedHttpMethod);
        Assert.Equal("{\"a\":1}", vm.Body);
        Assert.Equal("abc", vm.Headers.Single().Value);
        Assert.Equal("tag", vm.QueryParams.Single().Key);
        Assert.Equal("http://proxy.test:8080", vm.ProxyUrl);
    }
}
