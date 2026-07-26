using System.Net;

namespace HttpRequesting.Tests;

public class HttpRequestRunnerTests
{
    private static HttpClient ClientFor(HttpMessageHandler handler) => new(handler);

    [Fact]
    public async Task SendAsync_GetRequest_SendsNoBody()
    {
        var handler = new StubHandler();
        var runner = new HttpRequestRunner(ClientFor(handler));

        await runner.SendAsync(new HttpRequestSpec("GET", "https://example.test/", Body: "ignored"));

        Assert.Null(handler.LastRequest!.Content);
    }

    [Theory]
    [InlineData("Json", "application/json")]
    [InlineData("Xml", "application/xml")]
    public async Task SendAsync_PostWithBodyType_SetsContentType(string bodyType, string expected)
    {
        var handler = new StubHandler();
        var runner = new HttpRequestRunner(ClientFor(handler));

        await runner.SendAsync(new HttpRequestSpec("POST", "https://example.test/", "{}", bodyType));

        Assert.Equal(expected, handler.LastRequest!.Content!.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task SendAsync_ContentHeader_GoesToContentHeaders()
    {
        var handler = new StubHandler();
        var runner = new HttpRequestRunner(ClientFor(handler));

        await runner.SendAsync(new HttpRequestSpec("POST", "https://example.test/", "{}", "Json",
            new Dictionary<string, string> { ["Content-Disposition"] = "inline" }));

        // HttpRequestMessage.Headers rejects content headers outright, so landing there would have thrown.
        Assert.Equal("inline", handler.LastRequest!.Content!.Headers.GetValues("Content-Disposition").Single());
    }

    [Fact]
    public async Task SendAsync_CustomHeader_GoesToRequestHeaders()
    {
        var handler = new StubHandler();
        var runner = new HttpRequestRunner(ClientFor(handler));

        await runner.SendAsync(new HttpRequestSpec("GET", "https://example.test/", Headers:
            new Dictionary<string, string> { ["Authorization"] = "Basic abc" }));

        Assert.Equal("Basic abc", handler.LastRequest!.Headers.GetValues("Authorization").Single());
    }

    [Fact]
    public async Task SendAsync_BlankHeaderKey_IsSkipped()
    {
        var handler = new StubHandler();
        var runner = new HttpRequestRunner(ClientFor(handler));

        var result = await runner.SendAsync(new HttpRequestSpec("GET", "https://example.test/", Headers:
            new Dictionary<string, string> { ["  "] = "value" }));

        Assert.True(result.IsT0);
        Assert.Empty(handler.LastRequest!.Headers);
    }

    [Fact]
    public async Task SendAsync_SuccessfulResponse_ReportsStatusAndBody()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("{\"id\":1}", System.Text.Encoding.UTF8, "application/json")
        };
        var runner = new HttpRequestRunner(ClientFor(new StubHandler(response)));

        var result = await runner.SendAsync(new HttpRequestSpec("POST", "https://example.test/"));

        var success = result.AsT0;
        Assert.Equal("Created", success.StatusCode);
        Assert.Equal(201, success.StatusNumber);
        Assert.True(success.IsSuccessStatus);
        Assert.Equal("{\"id\":1}", success.ResponseBody);
        Assert.Equal("application/json", success.ContentType);
        Assert.NotNull(success.RequestTime);
    }

    [Fact]
    public async Task SendAsync_Response_ExposesResponseHeaders()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("hi") };
        response.Headers.TryAddWithoutValidation("X-Trace", "abc123");
        var runner = new HttpRequestRunner(ClientFor(new StubHandler(response)));

        var result = await runner.SendAsync(new HttpRequestSpec("GET", "https://example.test/"));

        Assert.Equal("abc123", result.AsT0.Headers["X-Trace"]);
    }

    [Fact]
    public async Task SendAsync_ErrorStatus_IsStillSuccessResult()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("nope") };
        var runner = new HttpRequestRunner(ClientFor(new StubHandler(response)));

        var result = await runner.SendAsync(new HttpRequestSpec("GET", "https://example.test/"));

        Assert.True(result.IsT0);
        Assert.False(result.AsT0.IsSuccessStatus);
        Assert.Equal(404, result.AsT0.StatusNumber);
    }

    [Fact]
    public async Task SendAsync_NetworkError_ReturnsFailureWithException()
    {
        var runner = new HttpRequestRunner(ClientFor(new ThrowingHandler(new HttpRequestException("boom"))));

        var result = await runner.SendAsync(new HttpRequestSpec("GET", "https://example.test/"));

        Assert.True(result.IsT1);
        Assert.Contains("boom", result.AsT1.Message);
        Assert.IsType<HttpRequestException>(result.AsT1.Exception);
    }

    [Fact]
    public async Task SendAsync_Cancelled_ReturnsCancellationFailure()
    {
        var runner = new HttpRequestRunner(ClientFor(new ThrowingHandler(new OperationCanceledException())));

        var result = await runner.SendAsync(new HttpRequestSpec("GET", "https://example.test/"));

        Assert.True(result.IsT1);
        Assert.Equal("Request was cancelled.", result.AsT1.Message);
        Assert.Null(result.AsT1.Exception);
    }

    [Fact]
    public async Task SendAsync_InvalidUrl_ReturnsFailure()
    {
        var runner = new HttpRequestRunner(ClientFor(new StubHandler()));

        var result = await runner.SendAsync(new HttpRequestSpec("GET", "not a url"));

        Assert.True(result.IsT1);
    }

    [Fact]
    public void Constructor_NullClient_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new HttpRequestRunner(null!));
    }
}
