using System.Net;

namespace RequesterMini.Tests;

/// <summary>Captures the outgoing request and replies with a canned response.</summary>
internal sealed class StubHandler : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }
    public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;
    public string ResponseBody { get; init; } = "{\"ok\":true}";
    public Exception? Throws { get; init; }

    /// <summary>Completes when a request has reached the handler.</summary>
    public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>When set, the response is held back until this completes.</summary>
    public Task? Gate { get; init; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        Entered.TrySetResult();
        if (Gate is not null) await Gate;

        if (Throws is not null) throw Throws;

        return new HttpResponseMessage(Status)
        {
            Content = new StringContent(ResponseBody, System.Text.Encoding.UTF8, "application/json"),
        };
    }
}
