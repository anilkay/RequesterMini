using OneOf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RequesterMini.Models;
using RequesterMini.Utils.Timers;

// By alias rather than a plain using: HttpRequesting also defines RequestSuccess and RequestFailure,
// which would collide with the RequesterMini.Models pair this file returns.
using FormBody = HttpRequesting.FormBody;
using RequestTarget = HttpRequesting.RequestTarget;

namespace RequesterMini.Utils;
public class MakeRequest {
    private readonly string _httpMethod;
    private readonly string _methodBody;
    private readonly  string _methodBodyType;

    private readonly HttpClient _httpClient;
    private readonly IElapsedTimerFactory _timerFactory;

    private readonly string _url;

    private readonly Dictionary<string, string> _headers;

    public MakeRequest(HttpClient httpClient,string httpMethod,string methodBody,string methodBodyType,string url, Dictionary<string, string>? headers = null, IElapsedTimerFactory? timerFactory = null)
    {
        _httpClient=httpClient;
        _httpMethod=httpMethod;
        _methodBody=methodBody;
        _methodBodyType=methodBodyType;
        _url=url;
        _headers=headers ?? new Dictionary<string, string>();
        _timerFactory=timerFactory ?? new StopwatchTimerFactory();
    }

    public async Task<OneOf<RequestSuccess, RequestFailure>> Execute(CancellationToken cancellationToken = default)
    {
        try
        {
            HttpMethod method = new HttpMethod(_httpMethod);

            var request = new HttpRequestMessage(method, _url);

            if (!string.Equals(_httpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                HttpContent content;
                switch (_methodBodyType.ToLowerInvariant())
                {
                    case "json":
                        content = new StringContent(_methodBody, Encoding.UTF8, "application/json");
                        break;
                    case "xml":
                        content = new StringContent(_methodBody, Encoding.UTF8, "application/xml");
                        break;
                    case "form":
                        // Shared with the cURL export, so the exported command sends the same text.
                        // StringContent with the media type set afterwards keeps the header as plain
                        // application/x-www-form-urlencoded.
                        content = new StringContent(FormBody.Encode(_methodBody), Encoding.UTF8)
                        {
                            Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded") },
                        };
                        break;
                    default:
                        content = new StringContent(_methodBody);
                        break;
                }
                request.Content = content;
            }

            // Add custom headers after content is set
            foreach (var header in _headers)
            {
                if (!string.IsNullOrWhiteSpace(header.Key))
                {
                    // Content-related headers should be added to Content.Headers
                    if (IsContentHeader(header.Key) && request.Content is not null)
                    {
                        // Replace, never append: the generated Content-Type is already set, and a second
                        // value sent "application/json; charset=utf-8, application/vnd.api+json".
                        request.Content.Headers.Remove(header.Key);
                        request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                    else
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }

            var timer = _timerFactory.StartNew();
            var response = await _httpClient.SendAsync(request, cancellationToken);
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            timer.Stop();

            return new RequestSuccess(response.StatusCode.ToString(), responseContent, DateTime.UtcNow, timer.Elapsed);
        }

        catch (OperationCanceledException)
        {
            return new RequestFailure("Request was cancelled.");
        }
        catch (Exception ex)
        {
            // The URL's path and query are left out: the message is shown, logged and saved to history,
            // and a {{variable}} in the URL has been resolved to its secret by now.
            return new RequestFailure(RequestTarget.FailureMessage(_httpMethod, _url, ex), ex);
        }
    }

    private static bool IsContentHeader(string headerName)
    {
        // Common content headers that should be added to Content.Headers
        var contentHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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
        
        return contentHeaders.Contains(headerName);
    }
}
