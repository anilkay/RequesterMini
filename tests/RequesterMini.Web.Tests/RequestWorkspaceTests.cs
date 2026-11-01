using System.Net;
using RequesterMini.Web.Services;

namespace RequesterMini.Web.Tests;

public class RequestWorkspaceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly RecordingHandler _handler = new();

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private RequestHistoryService NewHistory() =>
        new(Path.Combine(_directory, "history.json"));

    private EnvironmentService NewEnvironments() =>
        new(Path.Combine(_directory, $"environments-{Guid.NewGuid()}.json"));

    private RequestWorkspace NewWorkspace(RequestHistoryService history) =>
        new(new StubClientFactory(_handler), history, NewEnvironments());

    private RequestWorkspace NewWorkspace(RequestHistoryService history, EnvironmentService environments) =>
        new(new StubClientFactory(_handler), history, environments);

    [Fact]
    public void Constructor_SeedsQueryParamsFromStartUrl()
    {
        var workspace = NewWorkspace(NewHistory());

        Assert.Empty(workspace.QueryParams);
        Assert.Equal("GET", workspace.Method);
    }

    [Fact]
    public void Url_WithQueryString_PopulatesParams()
    {
        var workspace = NewWorkspace(NewHistory());

        workspace.Url = "https://example.test/api?page=2&sort=name";

        Assert.Collection(workspace.QueryParams,
            first => Assert.Equal(("page", "2"), (first.Key, first.Value)),
            second => Assert.Equal(("sort", "name"), (second.Key, second.Value)));
    }

    [Fact]
    public void SyncUrlFromParams_EnabledRows_RewriteUrl()
    {
        var workspace = NewWorkspace(NewHistory());
        workspace.Url = "https://example.test/api";

        workspace.AddQueryParam();
        workspace.QueryParams[0].Key = "q";
        workspace.QueryParams[0].Value = "a b";
        workspace.SyncUrlFromParams();

        Assert.Equal("https://example.test/api?q=a%20b", workspace.Url);
    }

    [Fact]
    public void SyncUrlFromParams_DisabledRow_IsLeftOutOfUrlButKept()
    {
        var workspace = NewWorkspace(NewHistory());
        workspace.Url = "https://example.test/api?keep=1";

        workspace.AddQueryParam();
        workspace.QueryParams[1].Key = "skip";
        workspace.QueryParams[1].Value = "1";
        workspace.QueryParams[1].IsEnabled = false;
        workspace.SyncUrlFromParams();

        Assert.Equal("https://example.test/api?keep=1", workspace.Url);
        Assert.Equal(2, workspace.QueryParams.Count);
    }

    [Fact]
    public void IsBodyEnabled_FollowsMethod()
    {
        var workspace = NewWorkspace(NewHistory());

        Assert.False(workspace.IsBodyEnabled);

        workspace.Method = "POST";
        Assert.True(workspace.IsBodyEnabled);
    }

    [Fact]
    public void BodyJsonError_OnlyReportedForJsonBodyType()
    {
        var workspace = NewWorkspace(NewHistory());

        workspace.Body = "{oops}";
        Assert.NotNull(workspace.BodyJsonError);

        workspace.BodyType = "Text";
        Assert.Null(workspace.BodyJsonError);
    }

    [Fact]
    public void BuildCurl_IncludesMethodHeadersAndBody()
    {
        var workspace = NewWorkspace(NewHistory());
        workspace.Method = "POST";
        workspace.Url = "https://example.test/api";
        workspace.Body = "{\"a\":1}";
        workspace.AddHeader();
        workspace.Headers[0].Key = "X-Trace";
        workspace.Headers[0].Value = "abc";

        var curl = workspace.BuildCurl();

        Assert.Contains("-X POST", curl);
        Assert.Contains("'X-Trace: abc'", curl);
        Assert.Contains("'Content-Type: application/json'", curl);
        Assert.Contains("{\"a\":1}", curl);
    }

    [Fact]
    public void BuildCurl_BasicAuth_UsesUserFlag()
    {
        var workspace = NewWorkspace(NewHistory());
        workspace.Url = "https://example.test/api";
        workspace.AuthType = "Basic";
        workspace.AuthUsername = "user";
        workspace.AuthPassword = "pass";

        Assert.Contains("-u 'user:pass'", workspace.BuildCurl());
    }

    [Fact]
    public void BuildCurl_DisabledHeader_IsOmitted()
    {
        var workspace = NewWorkspace(NewHistory());
        workspace.Url = "https://example.test/api";
        workspace.AddHeader();
        workspace.Headers[0].Key = "X-Off";
        workspace.Headers[0].Value = "1";
        workspace.Headers[0].IsEnabled = false;

        Assert.DoesNotContain("X-Off", workspace.BuildCurl());
    }

    [Fact]
    public async Task SendAsync_Success_FillsResponseAndHistory()
    {
        var history = NewHistory();
        var workspace = NewWorkspace(history);
        workspace.Url = "https://example.test/api";

        await workspace.SendAsync();

        Assert.NotNull(workspace.Response);
        Assert.Equal("OK", workspace.Response!.StatusText);
        Assert.False(workspace.Response.IsFailure);
        Assert.Equal("https://example.test/api", history.Snapshot().Single().Url);
    }

    [Fact]
    public async Task SendAsync_BasicAuth_SendsHeaderButDoesNotPersistIt()
    {
        var history = NewHistory();
        var workspace = NewWorkspace(history);
        workspace.Url = "https://example.test/api";
        workspace.AuthType = "Basic";
        workspace.AuthUsername = "user";
        workspace.AuthPassword = "pass";

        await workspace.SendAsync();

        Assert.True(_handler.LastRequest!.Headers.Contains("Authorization"));
        Assert.DoesNotContain("Authorization", history.Snapshot().Single().Headers.Keys);
    }

    [Fact]
    public async Task SendAsync_BearerAuth_SendsTokenButDoesNotPersistIt()
    {
        var history = NewHistory();
        var workspace = NewWorkspace(history);
        workspace.Url = "https://example.test/api";
        workspace.AuthType = "Bearer";
        workspace.AuthToken = "abc123";

        await workspace.SendAsync();

        Assert.Equal("Bearer abc123", _handler.LastRequest!.Headers.GetValues("Authorization").Single());
        Assert.DoesNotContain("Authorization", history.Snapshot().Single().Headers.Keys);
    }

    [Fact]
    public async Task SendAsync_BearerAuth_WinsOverHandTypedAuthorizationHeader()
    {
        var workspace = NewWorkspace(NewHistory());
        workspace.Url = "https://example.test/api";
        workspace.AddHeader();
        workspace.Headers[0].Key = "Authorization";
        workspace.Headers[0].Value = "Basic stale";
        workspace.AuthType = "Bearer";
        workspace.AuthToken = "abc123";

        await workspace.SendAsync();

        Assert.Equal("Bearer abc123", _handler.LastRequest!.Headers.GetValues("Authorization").Single());
    }

    [Fact]
    public async Task SendAsync_BlankBearerToken_SendsNoAuthorization()
    {
        var workspace = NewWorkspace(NewHistory());
        workspace.Url = "https://example.test/api";
        workspace.AuthType = "Bearer";
        workspace.AuthToken = "   ";

        await workspace.SendAsync();

        Assert.False(_handler.LastRequest!.Headers.Contains("Authorization"));
    }

    [Fact]
    public void BuildCurl_BearerAuth_AddsAuthorizationHeader()
    {
        var workspace = NewWorkspace(NewHistory());
        workspace.Url = "https://example.test/api";
        workspace.AuthType = "Bearer";
        workspace.AuthToken = "abc123";

        var curl = workspace.BuildCurl();

        Assert.Contains("-H 'Authorization: Bearer abc123'", curl);
        Assert.DoesNotContain("-u ", curl);
    }

    [Fact]
    public async Task SendAsync_MalformedProxyUrl_ReportsItWithoutSending()
    {
        var history = NewHistory();
        var workspace = NewWorkspace(history);
        workspace.Url = "https://example.test/api";
        workspace.ProxyUrl = "not a url";

        await workspace.SendAsync();

        Assert.True(workspace.Response!.IsFailure);
        Assert.Contains("not a url", workspace.Response.Body);
        Assert.Null(_handler.LastRequest);
        Assert.Empty(history.Snapshot());
    }

    [Fact]
    public async Task SendAsync_ProxyUrl_IsPersistedToHistoryButCredentialsAreNot()
    {
        var history = NewHistory();
        var workspace = NewWorkspace(history);
        workspace.Url = "https://example.test/api";

        await workspace.SendAsync();
        var saved = history.Snapshot().Single();

        Assert.Equal("", saved.ProxyUrl);
        Assert.DoesNotContain("pass", System.Text.Json.JsonSerializer.Serialize(saved));
    }

    [Fact]
    public void LoadFrom_EntryWithProxy_RestoresTheProxyUrl()
    {
        var workspace = NewWorkspace(NewHistory());
        var stored = new Models.StoredRequest(
            "GET", "https://example.test/", "", "Json", "OK", "{}", [], DateTime.UtcNow, "http://proxy.test:8080");

        workspace.LoadFrom(stored);

        Assert.Equal("http://proxy.test:8080", workspace.ProxyUrl);
        Assert.True(workspace.IsProxyEnabled);
    }

    [Fact]
    public async Task SendAsync_BlankUrl_DoesNothing()
    {
        var history = NewHistory();
        var workspace = NewWorkspace(history);
        workspace.Url = "";

        await workspace.SendAsync();

        Assert.Null(workspace.Response);
        Assert.Empty(history.Snapshot());
    }

    [Fact]
    public async Task SendAsync_Failure_ReportsErrorResponse()
    {
        var history = NewHistory();
        var workspace = new RequestWorkspace(
            new StubClientFactory(new RecordingHandler { Throw = new HttpRequestException("no route") }),
            history,
            NewEnvironments());
        workspace.Url = "https://example.test/api";

        await workspace.SendAsync();

        Assert.True(workspace.Response!.IsFailure);
        Assert.Equal("Error", workspace.Response.StatusText);
        Assert.Contains("no route", workspace.Response.Body);
    }

    [Fact]
    public async Task SendAsync_Placeholders_AreResolvedFromTheActiveEnvironment()
    {
        var environments = NewEnvironments();
        environments.Create("dev");
        environments.SetVariable("dev", "baseUrl", "https://api.test");
        environments.SetVariable("dev", "token", "abc123");

        var workspace = NewWorkspace(NewHistory(), environments);
        workspace.Url = "{{baseUrl}}/posts/1";
        workspace.AuthType = "Bearer";
        workspace.AuthToken = "{{token}}";

        await workspace.SendAsync();

        Assert.Equal("https://api.test/posts/1", _handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal("Bearer abc123", _handler.LastRequest.Headers.GetValues("Authorization").Single());
    }

    [Fact]
    public async Task SendAsync_Placeholders_AreResolvedInHeadersAndBody()
    {
        var environments = NewEnvironments();
        environments.Create("dev");
        environments.SetVariable("dev", "tenant", "acme");

        var workspace = NewWorkspace(NewHistory(), environments);
        workspace.Url = "https://example.test/api";
        workspace.Method = "POST";
        workspace.Body = "{\"tenant\":\"{{tenant}}\"}";
        workspace.AddHeader();
        workspace.Headers[0].Key = "X-Tenant";
        workspace.Headers[0].Value = "{{tenant}}";

        await workspace.SendAsync();

        Assert.Equal("acme", _handler.LastRequest!.Headers.GetValues("X-Tenant").Single());
        Assert.Equal("{\"tenant\":\"acme\"}", _handler.LastRequestBody);
    }

    [Fact]
    public async Task SendAsync_History_KeepsThePlaceholderRatherThanTheResolvedValue()
    {
        var environments = NewEnvironments();
        environments.Create("dev");
        environments.SetVariable("dev", "baseUrl", "https://api.test");

        var history = NewHistory();
        var workspace = NewWorkspace(history, environments);
        workspace.Url = "{{baseUrl}}/posts";

        await workspace.SendAsync();

        Assert.Equal("{{baseUrl}}/posts", history.Snapshot().Single().Url);
    }

    [Fact]
    public async Task SendAsync_NoActiveEnvironment_SendsThePlaceholderAsWritten()
    {
        var workspace = NewWorkspace(NewHistory());
        workspace.Url = "https://example.test/{{missing}}";

        await workspace.SendAsync();

        Assert.Equal("https://example.test/{{missing}}", _handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public void BuildCurl_ResolvesPlaceholders()
    {
        var environments = NewEnvironments();
        environments.Create("dev");
        environments.SetVariable("dev", "baseUrl", "https://api.test");

        var workspace = NewWorkspace(NewHistory(), environments);
        workspace.Url = "{{baseUrl}}/posts";

        var curl = workspace.BuildCurl();

        Assert.Contains("https://api.test/posts", curl);
        Assert.DoesNotContain("{{baseUrl}}", curl);
    }

    [Fact]
    public void UnresolvedVariables_ReportsNamesTheEnvironmentDoesNotDefine()
    {
        var environments = NewEnvironments();
        environments.Create("dev");
        environments.SetVariable("dev", "known", "x");

        var workspace = NewWorkspace(NewHistory(), environments);
        workspace.Url = "{{known}}/{{missing}}";
        workspace.AddHeader();
        workspace.Headers[0].Key = "X-Other";
        workspace.Headers[0].Value = "{{alsoMissing}}";

        Assert.Equal(["missing", "alsoMissing"], workspace.UnresolvedVariables());
    }

    [Fact]
    public void UnresolvedVariables_EverythingDefined_IsEmpty()
    {
        var environments = NewEnvironments();
        environments.Create("dev");
        environments.SetVariable("dev", "baseUrl", "https://api.test");

        var workspace = NewWorkspace(NewHistory(), environments);
        workspace.Url = "{{baseUrl}}/posts";

        Assert.Empty(workspace.UnresolvedVariables());
    }

    [Fact]
    public void LoadFrom_HistoryEntry_RestoresRequestAndResponse()
    {
        var workspace = NewWorkspace(NewHistory());
        var stored = new Models.StoredRequest(
            "PUT",
            "https://example.test/items/1?tag=x",
            "{\"a\":1}",
            "Json",
            "OK",
            "{}",
            new Dictionary<string, string> { ["X-Trace"] = "abc" },
            DateTime.UtcNow);

        workspace.LoadFrom(stored);

        Assert.Equal("PUT", workspace.Method);
        Assert.Equal("{\"a\":1}", workspace.Body);
        Assert.Equal("abc", workspace.Headers.Single().Value);
        Assert.Equal("tag", workspace.QueryParams.Single().Key);
        Assert.Equal("OK", workspace.Response!.StatusText);
    }

    [Fact]
    public void ApplyBruRequest_ReplacesCurrentRequest()
    {
        var workspace = NewWorkspace(NewHistory());
        var imported = new BrunoImporter.BruRequest(
            "Create post",
            "POST",
            "https://example.test/posts?draft=1",
            new Dictionary<string, string> { ["Accept"] = "application/json" },
            "{\"a\":1}",
            "Json");

        workspace.ApplyBruRequest(imported);

        Assert.Equal("POST", workspace.Method);
        Assert.Equal("https://example.test/posts?draft=1", workspace.Url);
        Assert.Equal("Accept", workspace.Headers.Single().Key);
        Assert.Equal("draft", workspace.QueryParams.Single().Key);
    }

    private sealed class StubClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }
        public Exception? Throw { get; init; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            if (Throw is not null) throw Throw;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }
}
