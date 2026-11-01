namespace HttpAuth;

/// <summary>
/// Builds HTTP Bearer authentication credentials as described in RFC 6750.
/// </summary>
public static class BearerAuth
{
    private const string Prefix = "Bearer";

    /// <summary>The header these credentials belong in.</summary>
    public const string HeaderName = "Authorization";

    /// <summary>
    /// Returns the full header value, e.g. <c>Bearer eyJhbGci...</c>.
    /// </summary>
    /// <remarks>
    /// Surrounding whitespace is trimmed, because a token pasted from a terminal or a JSON blob
    /// usually arrives with a newline that would otherwise make the header invalid. A token that
    /// already carries the <c>Bearer</c> prefix is passed through rather than prefixed twice —
    /// people paste the whole header value as often as the token alone.
    /// </remarks>
    public static string BuildHeaderValue(string token)
    {
        ArgumentNullException.ThrowIfNull(token);

        var trimmed = token.Trim();

        return trimmed.StartsWith(Prefix + " ", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"{Prefix} {trimmed}";
    }
}
