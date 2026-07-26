using System.Net;

namespace HttpRequesting.Tests;

/// <summary>
/// Captures the outgoing request and replies with a canned response, so the runner can be exercised
/// without a network.
/// </summary>
internal sealed class StubHandler(HttpResponseMessage? response = null) : HttpMessageHandler
{
    private readonly HttpResponseMessage _response = response ?? new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent("ok")
    };

    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return _response;
    }
}

internal sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => throw exception;
}
