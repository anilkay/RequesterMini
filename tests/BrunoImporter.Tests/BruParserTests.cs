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
        var result = BruParser.Parse(GetUsersBru());

        Assert.Equal("Get Users", result.Name);
    }

    [Fact]
    public void Parse_GetBlock_ExtractsUrlAndMethod()
    {
        var result = BruParser.Parse(GetUsersBru());

        Assert.Equal("GET", result.Method);
        Assert.Equal("https://api.example.com/users", result.Url);
    }

    [Fact]
    public void Parse_HeadersBlock_ExtractsHeaders()
    {
        var result = BruParser.Parse(GetUsersBru());

        Assert.Equal("application/json", result.Headers["Content-Type"]);
        Assert.Equal("Bearer token123", result.Headers["Authorization"]);
    }

    [Fact]
    public void Parse_PostWithJsonBody_ExtractsBodyAndBodyType()
    {
        var result = BruParser.Parse(GetCreateUserBru());

        Assert.Equal("Json", result.BodyType);
        Assert.Contains("\"name\": \"John\"", result.Body);
    }

    [Fact]
    public void Parse_BodyTypeNone_ReturnsNoneBodyType()
    {
        var bru = """
            get {
              url: https://api.example.com/test
              body: none
            }
            """;

        var result = BruParser.Parse(bru);

        Assert.Equal("None", result.BodyType);
    }

    [Fact]
    public void Parse_BodyTypeText_ReturnsTextBodyType()
    {
        var bru = """
            post {
              url: https://api.example.com/test
              body: text
            }

            body:text {
              hello world
            }
            """;

        var result = BruParser.Parse(bru);

        Assert.Equal("Text", result.BodyType);
        Assert.Contains("hello world", result.Body);
    }

    [Fact]
    public void Parse_BodyTypeFormUrlEncoded_ReturnsFormUrlEncodedBodyType()
    {
        var bru = """
            post {
              url: https://api.example.com/form
              body: form-urlencoded
            }
            """;

        var result = BruParser.Parse(bru);

        Assert.Equal("FormUrlEncoded", result.BodyType);
    }

    [Fact]
    public void Parse_EmptyContent_ReturnsDefaults()
    {
        var result = BruParser.Parse("");

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
        var bru = """
            headers {
              # this is a comment
              Content-Type: application/json
            }
            """;

        var result = BruParser.Parse(bru);

        Assert.Single(result.Headers);
        Assert.Equal("application/json", result.Headers["Content-Type"]);
    }

    [Fact]
    public void Parse_MissingFields_UsesDefaults()
    {
        var bru = """
            meta {
              name: Partial Request
            }
            """;

        var result = BruParser.Parse(bru);

        Assert.Equal("Partial Request", result.Name);
        Assert.Equal("GET",  result.Method);
        Assert.Equal("",     result.Url);
        Assert.Equal("",     result.Body);
        Assert.Equal("None", result.BodyType);
        Assert.Empty(result.Headers);
    }

    [Fact]
    public void Parse_PutMethod_ExtractsMethod()
    {
        var bru = """
            put {
              url: https://api.example.com/users/1
              body: json
            }
            """;

        var result = BruParser.Parse(bru);

        Assert.Equal("PUT", result.Method);
    }

    [Fact]
    public void Parse_DeleteMethod_ExtractsMethod()
    {
        var bru = """
            delete {
              url: https://api.example.com/users/1
              body: none
            }
            """;

        var result = BruParser.Parse(bru);

        Assert.Equal("DELETE", result.Method);
    }

    [Fact]
    public void Parse_UrlWithColons_ParsedCorrectly()
    {
        var bru = """
            get {
              url: https://api.example.com/v1/users
              body: none
            }
            """;

        var result = BruParser.Parse(bru);

        Assert.Equal("https://api.example.com/v1/users", result.Url);
    }

    [Fact]
    public void Parse_PlainBodyBlock_DefaultsToJson()
    {
        var bru = """
            post {
              url: https://api.example.com/users
            }

            body {
              {
                "username": "john",
                "password": "secret"
              }
            }
            """;

        var result = BruParser.Parse(bru);

        Assert.Equal("Json", result.BodyType);
        Assert.Contains("\"username\": \"john\"", result.Body);
    }

    [Fact]
    public void Parse_DisabledHeaders_AreSkipped()
    {
        var bru = """
            get {
              url: https://api.example.com/test
            }

            headers {
              Content-Type: application/json
              ~Authorization: Bearer old-token
              Accept: text/html
            }
            """;

        var result = BruParser.Parse(bru);

        Assert.Equal(2, result.Headers.Count);
        Assert.Equal("application/json", result.Headers["Content-Type"]);
        Assert.Equal("text/html", result.Headers["Accept"]);
        Assert.False(result.Headers.ContainsKey("Authorization"));
    }

    [Fact]
    public void Parse_TraceMethod_ExtractsMethod()
    {
        var bru = """
            trace {
              url: https://api.example.com/debug
            }
            """;

        var result = BruParser.Parse(bru);

        Assert.Equal("TRACE", result.Method);
    }

    [Fact]
    public void Parse_BodyXml_ExtractsContent()
    {
        var bru = """
            post {
              url: https://api.example.com/xml
              body: xml
            }

            body:xml {
              <root>
                <name>Test</name>
              </root>
            }
            """;

        var result = BruParser.Parse(bru);

        Assert.Contains("<name>Test</name>", result.Body);
        Assert.Equal("None", result.BodyType);
    }
}
