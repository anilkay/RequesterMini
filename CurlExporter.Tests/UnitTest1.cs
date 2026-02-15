using Xunit;

namespace CurlExporter.Tests;

public class CurlCommandBuilderTests
{
    [Fact]
    public void Build_GetRequest_OmitsMethodFlag()
    {
        var result = new CurlCommandBuilder()
            .SetMethod("GET")
            .SetUrl("https://example.com")
            .Build();

        Assert.Equal("curl 'https://example.com'", result);
    }

    [Fact]
    public void Build_PostRequest_IncludesMethodFlag()
    {
        var result = new CurlCommandBuilder()
            .SetMethod("POST")
            .SetUrl("https://example.com")
            .Build();

        Assert.Equal("curl -X POST 'https://example.com'", result);
    }

    [Fact]
    public void Build_WithJsonBody_IncludesContentTypeAndData()
    {
        var result = new CurlCommandBuilder()
            .SetMethod("POST")
            .SetUrl("https://example.com/api")
            .SetBody("{\"key\":\"value\"}", "application/json")
            .Build();

        Assert.Contains("-H 'Content-Type: application/json'", result);
        Assert.Contains("-d '{\"key\":\"value\"}'", result);
    }

    [Fact]
    public void Build_WithHeaders_IncludesAllHeaders()
    {
        var result = new CurlCommandBuilder()
            .SetMethod("GET")
            .SetUrl("https://example.com")
            .AddHeader("Authorization", "Bearer token123")
            .AddHeader("Accept", "application/json")
            .Build();

        Assert.Contains("-H 'Authorization: Bearer token123'", result);
        Assert.Contains("-H 'Accept: application/json'", result);
    }

    [Fact]
    public void Build_WithCustomContentTypeHeader_DoesNotDuplicate()
    {
        var result = new CurlCommandBuilder()
            .SetMethod("POST")
            .SetUrl("https://example.com")
            .AddHeader("Content-Type", "application/xml")
            .SetBody("<root/>", "application/json")
            .Build();

        // The explicit header should win; Content-Type should appear only once
        var count = result.Split("Content-Type").Length - 1;
        Assert.Equal(1, count);
        Assert.Contains("application/xml", result);
    }

    [Fact]
    public void Build_UrlWithSingleQuote_EscapesCorrectly()
    {
        var result = new CurlCommandBuilder()
            .SetMethod("GET")
            .SetUrl("https://example.com/search?q=it's")
            .Build();

        Assert.Contains("'https://example.com/search?q=it'\\''s'", result);
    }

    [Fact]
    public void Build_BodyWithSingleQuote_EscapesCorrectly()
    {
        var result = new CurlCommandBuilder()
            .SetMethod("POST")
            .SetUrl("https://example.com")
            .SetBody("it's a test", "text/plain")
            .Build();

        Assert.Contains("-d 'it'\\''s a test'", result);
    }

    [Fact]
    public void Build_WithoutUrl_Throws()
    {
        var builder = new CurlCommandBuilder()
            .SetMethod("GET");

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_PutWithFullRequest_ProducesCorrectCommand()
    {
        var result = new CurlCommandBuilder()
            .SetMethod("PUT")
            .SetUrl("https://api.example.com/items/1")
            .AddHeader("Authorization", "Bearer abc")
            .SetBody("{\"name\":\"updated\"}", "application/json")
            .Build();

        Assert.StartsWith("curl -X PUT", result);
        Assert.Contains("'https://api.example.com/items/1'", result);
        Assert.Contains("-H 'Authorization: Bearer abc'", result);
        Assert.Contains("-H 'Content-Type: application/json'", result);
        Assert.Contains("-d '{\"name\":\"updated\"}'", result);
    }

    [Fact]
    public void Build_DeleteWithNoBody_HasNoDataFlag()
    {
        var result = new CurlCommandBuilder()
            .SetMethod("DELETE")
            .SetUrl("https://api.example.com/items/1")
            .Build();

        Assert.Equal("curl -X DELETE 'https://api.example.com/items/1'", result);
        Assert.DoesNotContain("-d", result);
    }

    [Fact]
    public void SetMethod_ConvertsToUpperCase()
    {
        var result = new CurlCommandBuilder()
            .SetMethod("patch")
            .SetUrl("https://example.com")
            .Build();

        Assert.Contains("-X PATCH", result);
    }

    [Theory]
    [InlineData(BodyType.Json, "application/json")]
    [InlineData(BodyType.Xml, "application/xml")]
    [InlineData(BodyType.Form, "multipart/form-data")]
    [InlineData(BodyType.Text, "text/plain")]
    public void SetBody_WithBodyType_MapsToCorrectContentType(BodyType bodyType, string expectedContentType)
    {
        var result = new CurlCommandBuilder()
            .SetMethod("POST")
            .SetUrl("https://example.com")
            .SetBody("test", bodyType)
            .Build();

        Assert.Contains($"-H 'Content-Type: {expectedContentType}'", result);
        Assert.Contains("-d 'test'", result);
    }
}
