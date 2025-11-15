using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RequesterMini.Utils;
public class MakeRequest {
    private readonly string HtttpMethod;
    private readonly string MethodBody;
    private readonly  string MethodBodyType;

    private readonly HttpClient HttpClient;

    private readonly string Url;
    
    private readonly Dictionary<string, string> Headers;

    public MakeRequest(HttpClient httpClient,string httpMethod,string methodBody,string methodBodyType,string url, Dictionary<string, string> headers = null)
    {
        HttpClient=httpClient;
        HtttpMethod=httpMethod;
        MethodBody=methodBody;
        MethodBodyType=methodBodyType;
        Url=url;
        Headers=headers ?? new Dictionary<string, string>();
    }

    public async  Task<(string statusCode,string response, DateTime? FinishedTime)> Execute()
    {
        HttpMethod method = new HttpMethod(HtttpMethod);

        var request = new HttpRequestMessage(method, Url);

        HttpContent content;
        switch (MethodBodyType.ToLowerInvariant())
        {
            case "json":
                content = new StringContent(MethodBody, Encoding.UTF8, "application/json");
                break;
            case "xml":
                content = new StringContent(MethodBody, Encoding.UTF8, "application/xml");
                break;
            case "form":
                var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(MethodBody), "fieldName");
                content = formData;
                break;
            default:
                content = new StringContent(MethodBody);
                break;
        }
        request.Content = content;

        // Add custom headers after content is set
        foreach (var header in Headers)
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

        var response = await HttpClient.SendAsync(request);

        string responseContent = await response.Content.ReadAsStringAsync();

        return (response.StatusCode.ToString(),responseContent,null);
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
