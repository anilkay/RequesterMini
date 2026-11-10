using Microsoft.JSInterop;

namespace RequesterMini.Web.Utils;

/// <summary>
/// The browser calls the pages make, wrapped so a failure is a <c>false</c> rather than an
/// exception — and so a page can tell the user the truth.
/// <para>
/// In Blazor Server a JS call can fail without anything being wrong with the app: the circuit may
/// have dropped while the call was in flight (<see cref="JSDisconnectedException"/>), or the browser
/// may refuse the operation (<see cref="JSException"/>). Both used to surface as an unhandled
/// exception, and a copy the browser had actually refused still showed "copied".
/// </para>
/// </summary>
internal static class BrowserActions
{
    /// <summary>
    /// Puts <paramref name="text"/> on the clipboard. Returns whether it got there — the JS side
    /// reports <c>false</c> when the clipboard API and the legacy fallback are both unavailable.
    /// </summary>
    internal static async Task<bool> CopyTextAsync(this IJSRuntime js, string text)
    {
        ArgumentNullException.ThrowIfNull(js);

        try
        {
            return await js.InvokeAsync<bool>("requesterMini.copyText", text);
        }
        catch (JSDisconnectedException)
        {
            return false;  // the circuit is gone; there is no one left to tell
        }
        catch (JSException)
        {
            return false;
        }
    }

    /// <summary>Offers <paramref name="text"/> to the browser as a file download.</summary>
    internal static async Task<bool> DownloadTextAsync(this IJSRuntime js, string fileName, string text)
    {
        ArgumentNullException.ThrowIfNull(js);

        try
        {
            await js.InvokeVoidAsync("requesterMini.download", fileName, text);
            return true;
        }
        catch (JSDisconnectedException)
        {
            return false;
        }
        catch (JSException)
        {
            return false;
        }
    }
}
