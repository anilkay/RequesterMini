using RequesterMini.Web.Services;

namespace RequesterMini.Web.Tests;

public class ContentFormatterTests
{
    [Theory]
    [InlineData("application/json", "{}", ContentKind.Json)]
    [InlineData("application/problem+json", "{}", ContentKind.Json)]
    [InlineData("application/xml", "<a/>", ContentKind.Xml)]
    [InlineData("text/plain", "hello", ContentKind.Plain)]
    public void Detect_KnownContentType_UsesIt(string contentType, string body, ContentKind expected)
    {
        Assert.Equal(expected, ContentFormatter.Detect(contentType, body));
    }

    [Theory]
    [InlineData("{\"a\":1}", ContentKind.Json)]
    [InlineData("  [1,2]", ContentKind.Json)]
    [InlineData("<root/>", ContentKind.Xml)]
    [InlineData("plain text", ContentKind.Plain)]
    [InlineData("", ContentKind.Plain)]
    public void Detect_NoContentType_SniffsBody(string body, ContentKind expected)
    {
        Assert.Equal(expected, ContentFormatter.Detect(null, body));
    }

    [Fact]
    public void Detect_HtmlContentType_FallsBackToSniffing()
    {
        // An HTML page is markup, so the XML colorizer is the useful one.
        Assert.Equal(ContentKind.Xml, ContentFormatter.Detect("text/html", "<html></html>"));
    }

    [Fact]
    public void Pretty_Json_Indents()
    {
        var result = ContentFormatter.Pretty("{\"a\":1}", ContentKind.Json);

        Assert.Contains("\n", result);
        Assert.Contains("\"a\": 1", result);
    }

    [Fact]
    public void Pretty_MalformedJson_ReturnsInputUnchanged()
    {
        const string broken = "{\"a\":}";

        Assert.Equal(broken, ContentFormatter.Pretty(broken, ContentKind.Json));
    }

    [Fact]
    public void Pretty_Xml_Indents()
    {
        var result = ContentFormatter.Pretty("<root><child>1</child></root>", ContentKind.Xml);

        Assert.Contains("\n  <child>", result);
    }

    [Fact]
    public void Pretty_MalformedXml_ReturnsInputUnchanged()
    {
        const string broken = "<root><child></root>";

        Assert.Equal(broken, ContentFormatter.Pretty(broken, ContentKind.Xml));
    }

    [Fact]
    public void Pretty_Plain_ReturnsInputUnchanged()
    {
        Assert.Equal("  spaced  ", ContentFormatter.Pretty("  spaced  ", ContentKind.Plain));
    }
}
