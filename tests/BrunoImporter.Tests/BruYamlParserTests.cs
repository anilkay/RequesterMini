using BrunoImporter;

namespace BrunoImporter.Tests;

public class BruYamlParserTests
{
    private static string PostWithJsonBody() => """
        info:
          name: falsePost
          type: http
          seq: 1

        http:
          method: POST
          url: https://google.com/test
          body:
            type: json
            data: |-
              {
                "test":1,
                "age":"anil"
              }
          auth: inherit

        settings:
          encodeUrl: true
          timeout: 0
        """;

    private static string GetRequest() => """
        info:
          name: Get Users
          type: http
          seq: 1

        http:
          method: GET
          url: https://api.example.com/users
          auth: none
        """;

    private static string WithHeaders() => """
        info:
          name: With Headers
          type: http

        http:
          method: GET
          url: https://api.example.com/test
          headers:
            Content-Type: application/json
            Authorization: Bearer token123
          auth: none
        """;

    [Fact]
    public void Parse_PostWithJsonBody_ExtractsAllFields()
    {
        var result = BruYamlParser.Parse(PostWithJsonBody());

        Assert.Equal("falsePost", result.Name);
        Assert.Equal("POST", result.Method);
        Assert.Equal("https://google.com/test", result.Url);
        Assert.Equal("Json", result.BodyType);
        Assert.Contains("\"test\":1", result.Body);
        Assert.Contains("\"age\":\"anil\"", result.Body);
    }

    [Fact]
    public void Parse_GetRequest_ExtractsMethodAndUrl()
    {
        var result = BruYamlParser.Parse(GetRequest());

        Assert.Equal("GET", result.Method);
        Assert.Equal("https://api.example.com/users", result.Url);
        Assert.Equal("Get Users", result.Name);
    }

    [Fact]
    public void Parse_Headers_ExtractsAllHeaders()
    {
        var result = BruYamlParser.Parse(WithHeaders());

        Assert.Equal(2, result.Headers.Count);
        Assert.Equal("application/json", result.Headers["Content-Type"]);
        Assert.Equal("Bearer token123", result.Headers["Authorization"]);
    }

    [Fact]
    public void Parse_EmptyContent_ReturnsDefaults()
    {
        var result = BruYamlParser.Parse("");

        Assert.Equal("", result.Name);
        Assert.Equal("GET", result.Method);
        Assert.Equal("", result.Url);
        Assert.Equal("", result.Body);
        Assert.Equal("None", result.BodyType);
        Assert.Empty(result.Headers);
    }

    [Fact]
    public void Parse_BodyTypeText_MapsCorrectly()
    {
        var yaml = """
            http:
              method: POST
              url: https://api.example.com/text
              body:
                type: text
                data: hello world
            """;

        var result = BruYamlParser.Parse(yaml);

        Assert.Equal("Text", result.BodyType);
        Assert.Equal("hello world", result.Body);
    }

    [Fact]
    public void Parse_UrlWithQueryParams_PreservesFullUrl()
    {
        var yaml = """
            http:
              method: GET
              url: https://api.example.com/search?q=hello&page=1
            """;

        var result = BruYamlParser.Parse(yaml);

        Assert.Equal("https://api.example.com/search?q=hello&page=1", result.Url);
    }
}

public class BrunoFileImporterTests
{
    [Fact]
    public void Parse_BruContent_UsesBruParser()
    {
        var bruContent = """
            get {
              url: https://api.example.com/users
              body: none
            }
            """;

        var result = BrunoFileImporter.Parse(bruContent);

        Assert.Equal("GET", result.Method);
        Assert.Equal("https://api.example.com/users", result.Url);
    }

    [Fact]
    public void Parse_YamlContent_UsesYamlParser()
    {
        var yamlContent = """
            info:
              name: Test
            http:
              method: POST
              url: https://api.example.com/test
            """;

        var result = BrunoFileImporter.Parse(yamlContent);

        Assert.Equal("POST", result.Method);
        Assert.Equal("https://api.example.com/test", result.Url);
        Assert.Equal("Test", result.Name);
    }
}
