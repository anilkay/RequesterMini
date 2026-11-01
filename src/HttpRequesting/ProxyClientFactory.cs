using System.Net;
using OneOf;

namespace HttpRequesting;

/// <summary>Why a proxy client could not be built. Carries a message fit to show the user.</summary>
public sealed record ProxyError(string Message);

/// <summary>
/// Builds the <see cref="HttpClient"/> that routes through a user-configured proxy.
/// <para>
/// A proxy belongs to the transport rather than to any one request, so it is configured on the
/// client rather than carried on <see cref="HttpRequestSpec"/>. A malformed proxy URL is an expected
/// outcome of a user typing one, so it comes back as <see cref="ProxyError"/> rather than an
/// exception.
/// </para>
/// </summary>
public static class ProxyClientFactory
{
    /// <summary>
    /// Returns a client bound to <paramref name="settings"/>. The caller owns the client and should
    /// dispose it once the request is done — these are per-request throwaways rather than the shared
    /// client, since the proxy can change between sends.
    /// </summary>
    public static OneOf<HttpClient, ProxyError> Create(ProxySettings settings)
    {
        var proxy = CreateProxy(settings);

        return proxy.IsT1
            ? proxy.AsT1
            : new HttpClient(new HttpClientHandler { Proxy = proxy.AsT0, UseProxy = true });
    }

    /// <summary>
    /// Builds just the proxy, without a client around it. Split out so the address and credentials
    /// can be inspected — once wrapped in an <see cref="HttpClient"/> they are no longer reachable.
    /// </summary>
    public static OneOf<WebProxy, ProxyError> CreateProxy(ProxySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.IsEnabled)
        {
            return new ProxyError("No proxy is configured.");
        }

        // The scheme check matters: Uri.TryCreate reads "proxy.test:8080" as scheme "proxy.test"
        // with path "8080", so a host:port typed without a scheme would otherwise be accepted here
        // and only fail later, as an opaque error on the request itself.
        if (!Uri.TryCreate(settings.Url.Trim(), UriKind.Absolute, out var proxyUri)
            || (proxyUri.Scheme != Uri.UriSchemeHttp && proxyUri.Scheme != Uri.UriSchemeHttps))
        {
            return new ProxyError($"Invalid proxy URL: {settings.Url}. Expected something like http://proxy-host:8080.");
        }

        return new WebProxy(proxyUri)
        {
            Credentials = settings.Username.Length == 0
                ? null
                : new NetworkCredential(settings.Username, settings.Password),
        };
    }
}
