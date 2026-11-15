namespace UrlQuery;

/// <summary>
/// Reads and rewrites the query-string portion of a URL.
/// <para>
/// Everything is done with plain string splitting rather than <see cref="Uri"/> on purpose: the URL
/// being edited is often incomplete or invalid while the user types (e.g. "https://"), and
/// <see cref="Uri"/> throws on those. These methods never throw for malformed input.
/// </para>
/// </summary>
public static class UrlQueryCodec
{
    /// <summary>
    /// Extracts the query parameters from <paramref name="url"/>, in order, with keys and values unescaped.
    /// A segment without '=' yields an empty value. Returns an empty list when there is no query.
    /// </summary>
    public static List<QueryParam> Parse(string url)
    {
        ArgumentNullException.ThrowIfNull(url);

        // The fragment follows the query, so drop it before looking for '?'.
        var withoutFragment = SplitFragment(url).body;

        int queryStart = withoutFragment.IndexOf('?');
        return queryStart < 0 ? [] : ParseQuery(withoutFragment[(queryStart + 1)..]);
    }

    /// <summary>
    /// Parses a bare <c>key=value&amp;key2=value2</c> string — a query string without its leading '?'.
    /// </summary>
    /// <remarks>
    /// '+' is kept as a literal plus here, as everywhere else in this codec: this parses text a user
    /// is still typing into a URL, where a plus far more often means itself. A form body is different;
    /// use <see cref="ParseForm"/> for that.
    /// </remarks>
    public static List<QueryParam> ParseQuery(string query) => ParseSegments(query, plusIsSpace: false);

    /// <summary>
    /// Parses an <c>application/x-www-form-urlencoded</c> body. The one difference from
    /// <see cref="ParseQuery"/> is that '+' means a space, as the form encoding defines it — and as
    /// <c>FormBody</c> writes it back, so what the user types round-trips to what is sent.
    /// </summary>
    public static List<QueryParam> ParseForm(string body) => ParseSegments(body, plusIsSpace: true);

    private static List<QueryParam> ParseSegments(string text, bool plusIsSpace)
    {
        ArgumentNullException.ThrowIfNull(text);

        var result = new List<QueryParam>();
        if (text.Length == 0) return result;

        foreach (var segment in text.Split('&'))
        {
            if (segment.Length == 0) continue;

            int eq = segment.IndexOf('=');
            var key = eq < 0 ? segment : segment[..eq];
            var value = eq < 0 ? "" : segment[(eq + 1)..];

            if (key.Length == 0) continue;

            result.Add(new QueryParam(Unescape(key, plusIsSpace), Unescape(value, plusIsSpace)));
        }

        return result;
    }

    /// <summary>
    /// Returns <paramref name="url"/> with its query string replaced by <paramref name="parameters"/>.
    /// Entries with a blank key are skipped; when nothing is left the '?' is dropped entirely.
    /// Any fragment on the original URL is preserved.
    /// </summary>
    public static string Build(string url, IEnumerable<QueryParam> parameters)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(parameters);

        var (body, fragment) = SplitFragment(url);

        int queryStart = body.IndexOf('?');
        var baseUrl = queryStart < 0 ? body : body[..queryStart];

        var segments = new List<string>();
        foreach (var (key, value) in parameters)
        {
            if (string.IsNullOrWhiteSpace(key)) continue;
            segments.Add($"{Escape(key)}={Escape(value)}");
        }

        return segments.Count == 0
            ? baseUrl + fragment
            : $"{baseUrl}?{string.Join('&', segments)}{fragment}";
    }

    // Returns the part before '#' and the fragment (including '#', or empty when there is none).
    private static (string body, string fragment) SplitFragment(string url)
    {
        int hash = url.IndexOf('#');
        return hash < 0 ? (url, "") : (url[..hash], url[hash..]);
    }

    // Uri.UnescapeDataString leaves '+' alone. That is deliberate: '+' only means space under
    // form encoding, and treating it as space here would corrupt URLs that use a literal '+'.
    private static string Unescape(string value, bool plusIsSpace = false)
    {
        try
        {
            // The '+' is replaced BEFORE unescaping, so an encoded %2B still comes out as a plus.
            return Uri.UnescapeDataString(plusIsSpace ? value.Replace('+', ' ') : value);
        }
        catch (UriFormatException)
        {
            return value; // half-typed escape like "%z" — keep the raw text
        }
    }

    private static string Escape(string value) =>
        value.Length == 0 ? "" : Uri.EscapeDataString(value);
}
