using Microsoft.JSInterop;
using RequesterMini.Web.Utils;

namespace RequesterMini.Web.Tests;

public class BrowserActionsTests
{
    /// <summary>An IJSRuntime that returns a canned value or throws, and records the call it got.</summary>
    private sealed class FakeJs(object? result = null, Exception? throws = null) : IJSRuntime
    {
        public string? Identifier { get; private set; }
        public object?[]? Args { get; private set; }

        // The two overloads repeat their four lines on purpose. Inside an IJSRuntime implementation
        // the BL0016 analyzer reads any call to a local method — another overload, a private helper
        // — as a JS interop call that wants a try/catch, so sharing the body would just move the
        // warning rather than remove it.
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Identifier = identifier;
            Args = args;

            if (throws is not null) throw throws;

            return ValueTask.FromResult(result is TValue value ? value : default!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Identifier = identifier;
            Args = args;

            if (throws is not null) throw throws;

            return ValueTask.FromResult(result is TValue value ? value : default!);
        }
    }

    [Fact]
    public async Task CopyText_BrowserReportsSuccess_ReturnsTrue()
    {
        var js = new FakeJs(result: true);

        Assert.True(await js.CopyTextAsync("hello"));
        Assert.Equal("requesterMini.copyText", js.Identifier);
        Assert.Equal("hello", js.Args!.Single());
    }

    [Fact]
    public async Task CopyText_BrowserReportsFailure_ReturnsFalseSoThePageDoesNotClaimItCopied()
    {
        // The JS side returns false when neither the clipboard API nor the legacy fallback worked.
        Assert.False(await new FakeJs(result: false).CopyTextAsync("hello"));
    }

    [Fact]
    public async Task CopyText_CircuitDisconnected_ReturnsFalseInsteadOfThrowing()
    {
        var js = new FakeJs(throws: new JSDisconnectedException("gone"));

        Assert.False(await js.CopyTextAsync("hello"));
    }

    [Fact]
    public async Task CopyText_JsThrows_ReturnsFalseInsteadOfThrowing()
    {
        var js = new FakeJs(throws: new JSException("denied"));

        Assert.False(await js.CopyTextAsync("hello"));
    }

    [Fact]
    public async Task DownloadText_PassesFileNameAndContent()
    {
        var js = new FakeJs();

        Assert.True(await js.DownloadTextAsync("response.json", "{}"));
        Assert.Equal("requesterMini.download", js.Identifier);
        Assert.Equal(["response.json", "{}"], js.Args);
    }

    [Fact]
    public async Task DownloadText_CircuitDisconnected_ReturnsFalse()
    {
        Assert.False(await new FakeJs(throws: new JSDisconnectedException("gone")).DownloadTextAsync("a.txt", "x"));
    }

    [Fact]
    public async Task DownloadText_JsThrows_ReturnsFalse()
    {
        Assert.False(await new FakeJs(throws: new JSException("denied")).DownloadTextAsync("a.txt", "x"));
    }

    [Fact]
    public async Task CopyText_NullRuntime_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => BrowserActions.CopyTextAsync(null!, "x"));
    }
}
