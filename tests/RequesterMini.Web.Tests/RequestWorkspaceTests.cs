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

    private RequestWorkspace NewWorkspace(RequestHistoryService history) =>
        new(new StubClientFactory(_handler), history);

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
            history);
        workspace.Url = "https://example.test/api";

        await workspace.SendAsync();

        Assert.True(workspace.Response!.IsFailure);
        Assert.Equal("Error", workspace.Response.StatusText);
        Assert.Contains("no route", workspace.Response.Body);
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
        public Exception? Throw { get; init; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;

            if (Throw is not null) throw Throw;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }
}
