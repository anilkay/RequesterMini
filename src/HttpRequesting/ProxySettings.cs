namespace HttpRequesting;

/// <summary>
/// An HTTP proxy to route requests through. Blank credentials mean an unauthenticated proxy.
/// </summary>
public sealed record ProxySettings(string Url, string Username = "", string Password = "")
{
    /// <summary>A blank URL means "no proxy" — the common case, so it is not an error.</summary>
    public bool IsEnabled => !string.IsNullOrWhiteSpace(Url);

    public static ProxySettings None { get; } = new("");
}
