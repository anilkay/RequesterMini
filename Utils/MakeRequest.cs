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

        // Add custom headers
        foreach (var header in Headers)
        {
            if (!string.IsNullOrWhiteSpace(header.Key))
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

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

        var response = await HttpClient.SendAsync(request);

        string responseContent = await response.Content.ReadAsStringAsync();

        return (response.StatusCode.ToString(),responseContent,null);
    }
}
