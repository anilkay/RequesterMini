namespace RequesterMini.Web.Models;

/// <summary>
/// A history entry. Auth and proxy credentials are deliberately absent — the generated
/// Authorization header is never persisted, and neither is a proxy username or password. Only the
/// proxy URL is kept, so a restored request still goes the same route. Matches the desktop app.
/// </summary>
public sealed record StoredRequest(
    string Method,
    string Url,
    string Body,
    string BodyType,
    string ResponseStatusCode,
    string ResponseBody,
    Dictionary<string, string> Headers,
    DateTime SavedAtUtc,
    string ProxyUrl = "");
