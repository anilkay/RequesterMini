using OneOf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RequesterMini.Models;
using RequesterMini.Utils.Timers;

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
                    var formData = new MultipartFormDataContent();
                    formData.Add(new StringContent(_methodBody), "fieldName");
                    content = formData;
                    break;
                default:
                    content = new StringContent(_methodBody);
                    break;
            }
            request.Content = content;

            // Add custom headers after content is set
            foreach (var header in _headers)
            {
                if (!string.IsNullOrWhiteSpace(header.Key))
                {
                    // Content-related headers should be added to Content.Headers
                    if (IsContentHeader(header.Key))
                    {
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
            var errorMessage = $"Error while making {_httpMethod} request to {_url}: {ex.Message}";
            return new RequestFailure(errorMessage, ex);
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
