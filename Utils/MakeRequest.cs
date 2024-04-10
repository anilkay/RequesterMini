using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

public class MakeRequest {
    private readonly string HtttpMethod;
    private readonly string MethodBody;
    private readonly  string MethodBodyType;

    private readonly HttpClient HttpClient;

    private readonly string Url;

    //Generate Constructor with All

    public MakeRequest(HttpClient httpClient,string httpMethod,string methodBody,string methodBodyType,string url)
    {
        HttpClient=httpClient;
        HtttpMethod=httpMethod;
        MethodBody=methodBody;
        MethodBodyType=methodBodyType;
        Url=url;

        
    }

    public async  Task<(string statusCode,string response, DateTime? FinishedTime)> Execute()
    {
        //Cautious This Leads to Socker Starvation

        HttpMethod method = new HttpMethod(HtttpMethod);

        var request = new HttpRequestMessage(method, Url);

                    HttpContent content;
            switch (MethodBodyType.ToLower())
            {
                case "json":
                    content = new StringContent(MethodBody, Encoding.UTF8, "application/json");
                    break;
                case "xml":
                    content = new StringContent(MethodBody, Encoding.UTF8, "application/xml");
                    break;
                case "form":
                    // Form verisi için örnek bir dictionary kabul edildiğini varsayalım.
                    // Gerçek uygulamada bu dictionary metin gövdesine göre uygun şekilde oluşturulmalıdır.
                    var formData = new MultipartFormDataContent();
                    formData.Add(new StringContent(MethodBody), "fieldName"); // Burada fieldName, form alan adınız olacaktır.
                    content = formData;
                    break;
                default:
                    content = new StringContent(MethodBody); // Varsayılan olarak düz metin olarak kabul edilir.
                    break;
            }
            request.Content = content;
        

        // İsteği gönder ve yanıtı al
        var response = await HttpClient.SendAsync(request);

        string responseContent = await response.Content.ReadAsStringAsync();




        
        return (response.StatusCode.ToString(),responseContent,null);
        
    }
     

   
}