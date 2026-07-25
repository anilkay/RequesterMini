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

        var result = new List<QueryParam>();

        // The fragment follows the query, so drop it before looking for '?'.
        var withoutFragment = SplitFragment(url).body;

        int queryStart = withoutFragment.IndexOf('?');
        if (queryStart < 0) return result;

        var query = withoutFragment[(queryStart + 1)..];
        if (query.Length == 0) return result;

        foreach (var segment in query.Split('&'))
        {
            if (segment.Length == 0) continue;

            int eq = segment.IndexOf('=');
            var key = eq < 0 ? segment : segment[..eq];
            var value = eq < 0 ? "" : segment[(eq + 1)..];

            if (key.Length == 0) continue;

            result.Add(new QueryParam(Unescape(key), Unescape(value)));
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
    private static string Unescape(string value)
    {
        try
        {
            return Uri.UnescapeDataString(value);
        }
        catch (UriFormatException)
        {
            return value; // half-typed escape like "%z" — keep the raw text
        }
    }

    private static string Escape(string value) =>
        value.Length == 0 ? "" : Uri.EscapeDataString(value);
}
