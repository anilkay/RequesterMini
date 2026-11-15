namespace HttpRequesting;

/// <summary>
/// Describes where a request is going in a form that is safe to show and to write to disk.
/// </summary>
public static class RequestTarget
{
    /// <summary>
    /// Returns <c>scheme://host[:port]</c>, or null when <paramref name="url"/> is not an absolute URL.
    /// <para>
    /// The path, query string, fragment and any <c>user:password@</c> are left out on purpose. Failure
    /// messages are shown, logged and saved to history, and a request whose URL came out of a variable
    /// such as <c>?key={{apiKey}}</c> has the resolved secret in it by then — which the persisted-as-
    /// template rule is meant to keep off disk. Secrets also live in paths (webhook URLs, bot tokens).
    /// </para>
    /// </summary>
    public static string? Describe(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
        {
            return null;
        }

        return uri.IsDefaultPort
            ? $"{uri.Scheme}://{uri.Host}"
            : $"{uri.Scheme}://{uri.Host}:{uri.Port}";
    }

    /// <summary>The sentence used by both request senders when a request fails outright.</summary>
    public static string FailureMessage(string method, string? url, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var target = Describe(url);

        return target is null
            ? $"Error while making {method} request: {exception.Message}"
            : $"Error while making {method} request to {target}: {exception.Message}";
    }
}
