namespace RequesterMini.Web.Models;

/// <summary>
/// A history entry. Auth credentials are deliberately absent — the generated Authorization header is
/// never persisted, matching the desktop app.
/// </summary>
public sealed record StoredRequest(
    string Method,
    string Url,
    string Body,
    string BodyType,
    string ResponseStatusCode,
    string ResponseBody,
    Dictionary<string, string> Headers,
    DateTime SavedAtUtc);
