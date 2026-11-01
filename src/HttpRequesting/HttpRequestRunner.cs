using System.Net.Http.Headers;
using System.Text;
using HttpRequesting.Timers;
using OneOf;
using UrlQuery;

namespace HttpRequesting;

/// <summary>
/// Sends a <see cref="HttpRequestSpec"/> and reports the outcome as
/// <see cref="OneOf{RequestSuccess, RequestFailure}"/> — network errors and cancellation are expected
/// results here, not exceptions.
/// </summary>
public sealed class HttpRequestRunner
{
    private static readonly HashSet<string> ContentHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Content-Type",
        "Content-Length",
        "Content-Encoding",
        "Content-Language",
        "Content-Location",
        "Content-MD5",
        "Content-Range",
        "Content-Disposition",
        "Expires",
        "Last-Modified"
    };

    private readonly HttpClient _httpClient;
    private readonly IElapsedTimerFactory _timerFactory;

    public HttpRequestRunner(HttpClient httpClient, IElapsedTimerFactory? timerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
        _timerFactory = timerFactory ?? new StopwatchTimerFactory();
    }

    public async Task<OneOf<RequestSuccess, RequestFailure>> SendAsync(
        HttpRequestSpec spec,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);

        try
        {
            using var request = new HttpRequestMessage(new HttpMethod(spec.Method), spec.Url);

            if (!IsBodyless(spec.Method))
            {
                request.Content = BuildContent(spec.Body, spec.BodyType);
            }

            // After the content exists, so content headers can be routed to it.
            foreach (var (key, value) in spec.Headers ?? new Dictionary<string, string>())
            {
                if (string.IsNullOrWhiteSpace(key)) continue;

                if (ContentHeaders.Contains(key) && request.Content is not null)
                {
                    request.Content.Headers.TryAddWithoutValidation(key, value);
                }
                else
                {
                    request.Headers.TryAddWithoutValidation(key, value);
                }
            }

            var timer = _timerFactory.StartNew();
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            timer.Stop();

            return new RequestSuccess(
                response.StatusCode.ToString(),
                (int)response.StatusCode,
                responseBody,
                DateTime.UtcNow,
                timer.Elapsed,
                response.Content.Headers.ContentType?.MediaType,
                CollectHeaders(response));
        }
        catch (OperationCanceledException)
        {
            return new RequestFailure("Request was cancelled.");
        }
        catch (Exception ex)
        {
            return new RequestFailure($"Error while making {spec.Method} request to {spec.Url}: {ex.Message}", ex);
        }
    }

    private static bool IsBodyless(string method) =>
        string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase);

    private static HttpContent BuildContent(string body, string bodyType) =>
        bodyType.ToLowerInvariant() switch
        {
            "json" => new StringContent(body, Encoding.UTF8, "application/json"),
            "xml" => new StringContent(body, Encoding.UTF8, "application/xml"),
            "form" => BuildFormContent(body),
            _ => new StringContent(body)
        };

    /// <summary>
    /// Reads the body as <c>key=value&amp;key2=value2</c> and sends it form-encoded. The pairs are
    /// re-encoded rather than passed through, so what reaches the wire is valid regardless of how
    /// the user escaped what they typed.
    /// </summary>
    private static HttpContent BuildFormContent(string body) =>
        new FormUrlEncodedContent(
            UrlQueryCodec.ParseQuery(body).Select(p => new KeyValuePair<string, string>(p.Key, p.Value)));

    private static Dictionary<string, string> CollectHeaders(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Copy(response.Headers);
        Copy(response.Content.Headers);
        return headers;

        void Copy(HttpHeaders source)
        {
            foreach (var (name, values) in source)
            {
                headers[name] = string.Join(", ", values);
            }
        }
    }
}
