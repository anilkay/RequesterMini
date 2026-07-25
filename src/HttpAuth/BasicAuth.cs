using System.Text;

namespace HttpAuth;

/// <summary>
/// Builds HTTP Basic authentication credentials as described in RFC 7617.
/// </summary>
public static class BasicAuth
{
    /// <summary>The header these credentials belong in.</summary>
    public const string HeaderName = "Authorization";

    /// <summary>
    /// Returns the full header value, e.g. <c>Basic dXNlcjpwYXNz</c>.
    /// </summary>
    /// <remarks>
    /// Credentials are encoded as UTF-8 before Base64, per the RFC 7617 charset recommendation.
    /// A colon in <paramref name="password"/> is fine — only the first colon separates the two
    /// fields — but a colon in <paramref name="username"/> produces credentials the server cannot
    /// split unambiguously, so callers should avoid it.
    /// </remarks>
    public static string BuildHeaderValue(string username, string password)
    {
        ArgumentNullException.ThrowIfNull(username);
        ArgumentNullException.ThrowIfNull(password);

        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        return $"Basic {token}";
    }
}
