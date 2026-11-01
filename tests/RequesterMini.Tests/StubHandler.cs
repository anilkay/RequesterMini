using System.Net;

namespace RequesterMini.Tests;

/// <summary>Captures the outgoing request and replies with a canned response.</summary>
internal sealed class StubHandler : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }
    public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;
    public string ResponseBody { get; init; } = "{\"ok\":true}";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        return new HttpResponseMessage(Status)
        {
            Content = new StringContent(ResponseBody, System.Text.Encoding.UTF8, "application/json"),
        };
    }
}
