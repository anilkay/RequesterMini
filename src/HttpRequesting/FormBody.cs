using UrlQuery;

namespace HttpRequesting;

/// <summary>
/// Turns what a user typed in the Form body box into the exact
/// <c>application/x-www-form-urlencoded</c> text that goes on the wire.
/// <para>
/// This is the single source for that text. The request sender and the cURL export both call it, so
/// the exported command sends the same bytes the app does; before, the sender re-encoded the body
/// while the export passed the raw text to <c>-d</c>, and the two could disagree on any value that
/// needed escaping.
/// </para>
/// </summary>
public static class FormBody
{
    /// <summary>
    /// Reads the body as <c>key=value&amp;key2=value2</c> and writes it back encoded. A typed space or
    /// '+' becomes '+', a '=' or '&amp;' inside a value is escaped, and the result is valid however the
    /// user escaped what they typed.
    /// </summary>
    public static string Encode(string body)
    {
        ArgumentNullException.ThrowIfNull(body);

        return string.Join('&', UrlQueryCodec.ParseForm(body).Select(p => $"{Escape(p.Key)}={Escape(p.Value)}"));
    }

    // The same escaping FormUrlEncodedContent applies: percent-encode everything unreserved, then
    // write a space as '+'. A test pins the two together.
    private static string Escape(string value) => Uri.EscapeDataString(value).Replace("%20", "+");
}
