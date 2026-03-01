using BrunoImporter;

namespace BrunoImporter.Tests;

public class BruParserTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static string GetUsersBru() => """
        meta {
          name: Get Users
          type: http
          seq: 1
        }

        get {
          url: https://api.example.com/users
          body: none
          auth: none
        }

        headers {
          Content-Type: application/json
          Authorization: Bearer token123
        }
        """;

    private static string GetCreateUserBru() => """
        meta {
          name: Create User
          type: http
          seq: 2
        }

        post {
          url: https://api.example.com/users
          body: json
          auth: none
        }

        headers {
          Content-Type: application/json
        }

        body:json {
          {
            "name": "John"
          }
        }
        """;

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_MetaBlock_ExtractsName()
    {
        var result = BruYamlParser.Parse(GetUsersBru());
        Assert.Equal("Get Users", result.Name);
    }

    [Fact]
    public void Parse_GetBlock_ExtractsUrlAndMethod()
    {
        var result = BruYamlParser.Parse(GetUsersBru());
        Assert.Equal("GET", result.Method);
        Assert.Equal("https://api.example.com/users", result.Url);
    }

    [Fact]
    public void Parse_HeadersBlock_ExtractsHeaders()
    {
        var result = BruYamlParser.Parse(GetUsersBru());
        Assert.Equal("application/json", result.Headers["Content-Type"]);
        Assert.Equal("Bearer token123", result.Headers["Authorization"]);
    }

    [Fact]
    public void Parse_PostWithJsonBody_ExtractsBodyAndBodyType()
    {
        var result = BruYamlParser.Parse(GetCreateUserBru());
        Assert.Equal("Json", result.BodyType);
        Assert.Contains("\"name\": \"John\"", result.Body);
    }

    [Fact]
    public void Parse_BodyTypeNone_ReturnsNoneBodyType()
    {
        var result = BruYamlParser.Parse("get {\n  url: https://api.example.com/test\n  body: none\n}");
        Assert.Equal("None", result.BodyType);
    }

    [Fact]
    public void Parse_BodyTypeText_ReturnsTextBodyType()
    {
        var bru = "post {\n  url: https://api.example.com/test\n  body: text\n}\n\nbody:text {\n  hello world\n}";
        var result = BruYamlParser.Parse(bru);
        Assert.Equal("Text", result.BodyType);
        Assert.Contains("hello world", result.Body);
    }

    [Fact]
    public void Parse_BodyTypeFormUrlEncoded_ReturnsFormUrlEncodedBodyType()
    {
        var result = BruYamlParser.Parse("post {\n  url: https://api.example.com/form\n  body: form-urlencoded\n}");
        Assert.Equal("FormUrlEncoded", result.BodyType);
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
    public void Parse_CommentsInBlock_AreIgnored()
    {
        var bru = "headers {\n  # this is a comment\n  Content-Type: application/json\n}";
        var result = BruYamlParser.Parse(bru);
        Assert.Single(result.Headers);
        Assert.Equal("application/json", result.Headers["Content-Type"]);
    }

    [Fact]
    public void Parse_MissingFields_UsesDefaults()
    {
        var result = BruYamlParser.Parse("meta {\n  name: Partial Request\n}");
        Assert.Equal("Partial Request", result.Name);
        Assert.Equal("GET", result.Method);
        Assert.Equal("", result.Url);
        Assert.Empty(result.Headers);
    }

    [Fact]
    public void Parse_PutMethod_ExtractsMethod()
    {
        var result = BruYamlParser.Parse("put {\n  url: https://api.example.com/users/1\n  body: json\n}");
        Assert.Equal("PUT", result.Method);
    }

    [Fact]
    public void Parse_DeleteMethod_ExtractsMethod()
    {
        var result = BruYamlParser.Parse("delete {\n  url: https://api.example.com/users/1\n  body: none\n}");
        Assert.Equal("DELETE", result.Method);
    }

    [Fact]
    public void Parse_UrlWithColons_ParsedCorrectly()
    {
        var result = BruYamlParser.Parse("get {\n  url: https://api.example.com/v1/users\n  body: none\n}");
        Assert.Equal("https://api.example.com/v1/users", result.Url);
    }

    [Fact]
    public void Parse_PlainBodyBlock_DefaultsToJson()
    {
        var bru = "post {\n  url: https://api.example.com/users\n}\n\nbody {\n  {\n    \"username\": \"john\"\n  }\n}";
        var result = BruYamlParser.Parse(bru);
        Assert.Equal("Json", result.BodyType);
        Assert.Contains("\"username\": \"john\"", result.Body);
    }

    [Fact]
    public void Parse_DisabledHeaders_AreSkipped()
    {
        var bru = "get {\n  url: https://api.example.com/test\n}\n\nheaders {\n  Content-Type: application/json\n  ~Authorization: Bearer old-token\n  Accept: text/html\n}";
        var result = BruYamlParser.Parse(bru);
        Assert.Equal(2, result.Headers.Count);
        Assert.Equal("application/json", result.Headers["Content-Type"]);
        Assert.Equal("text/html", result.Headers["Accept"]);
        Assert.False(result.Headers.ContainsKey("Authorization"));
    }

    [Fact]
    public void Parse_TraceMethod_ExtractsMethod()
    {
        var result = BruYamlParser.Parse("trace {\n  url: https://api.example.com/debug\n}");
        Assert.Equal("TRACE", result.Method);
    }

    [Fact]
    public void Parse_BodyXml_ExtractsContent()
    {
        var bru = "post {\n  url: https://api.example.com/xml\n  body: xml\n}\n\nbody:xml {\n  <root><name>Test</name></root>\n}";
        var result = BruYamlParser.Parse(bru);
        Assert.Contains("<name>Test</name>", result.Body);
    }
}