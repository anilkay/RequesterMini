using UrlQuery;

namespace UrlQuery.Tests;

public class UrlQueryCodecTests
{
    [Fact]
    public void Parse_NoQuery_ReturnsEmpty()
    {
        Assert.Empty(UrlQueryCodec.Parse("https://example.com/posts"));
    }

    [Fact]
    public void Parse_EmptyQuery_ReturnsEmpty()
    {
        Assert.Empty(UrlQueryCodec.Parse("https://example.com/posts?"));
    }

    [Fact]
    public void Parse_MultipleParams_PreservesOrder()
    {
        var result = UrlQueryCodec.Parse("https://example.com?b=2&a=1");

        Assert.Equal(new QueryParam("b", "2"), result[0]);
        Assert.Equal(new QueryParam("a", "1"), result[1]);
    }

    [Fact]
    public void Parse_SegmentWithoutEquals_YieldsEmptyValue()
    {
        var result = UrlQueryCodec.Parse("https://example.com?flag");

        Assert.Equal(new QueryParam("flag", ""), Assert.Single(result));
    }

    [Fact]
    public void Parse_EscapedKeyAndValue_AreUnescaped()
    {
        var result = UrlQueryCodec.Parse("https://example.com?q=hello%20world&a%2Bb=c%26d");

        Assert.Equal(new QueryParam("q", "hello world"), result[0]);
        Assert.Equal(new QueryParam("a+b", "c&d"), result[1]);
    }

    [Fact]
    public void Parse_LiteralPlus_IsNotTreatedAsSpace()
    {
        var result = UrlQueryCodec.Parse("https://example.com?q=a+b");

        Assert.Equal(new QueryParam("q", "a+b"), Assert.Single(result));
    }

    [Fact]
    public void Parse_ValueContainingEquals_KeepsRemainderInValue()
    {
        var result = UrlQueryCodec.Parse("https://example.com?token=a=b=c");

        Assert.Equal(new QueryParam("token", "a=b=c"), Assert.Single(result));
    }

    [Fact]
    public void Parse_DuplicateKeys_AreAllReturned()
    {
        var result = UrlQueryCodec.Parse("https://example.com?tag=x&tag=y");

        Assert.Equal(2, result.Count);
        Assert.Equal("x", result[0].Value);
        Assert.Equal("y", result[1].Value);
    }

    [Fact]
    public void Parse_FragmentAfterQuery_IsNotPartOfLastValue()
    {
        var result = UrlQueryCodec.Parse("https://example.com?a=1#section");

        Assert.Equal(new QueryParam("a", "1"), Assert.Single(result));
    }

    [Fact]
    public void Parse_QueryInsideFragmentOnly_IsIgnored()
    {
        Assert.Empty(UrlQueryCodec.Parse("https://example.com#/route?a=1"));
    }

    [Fact]
    public void Parse_BlankKeysAndEmptySegments_AreSkipped()
    {
        var result = UrlQueryCodec.Parse("https://example.com?&=1&a=2&");

        Assert.Equal(new QueryParam("a", "2"), Assert.Single(result));
    }

    [Fact]
    public void Parse_IncompleteUrlBeingTyped_DoesNotThrow()
    {
        Assert.Empty(UrlQueryCodec.Parse("https://"));
        Assert.Empty(UrlQueryCodec.Parse(""));
    }

    [Fact]
    public void Parse_MalformedEscape_KeepsRawText()
    {
        var result = UrlQueryCodec.Parse("https://example.com?q=100%");

        Assert.Equal(new QueryParam("q", "100%"), Assert.Single(result));
    }

    [Fact]
    public void Parse_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => UrlQueryCodec.Parse(null!));
    }

    [Fact]
    public void Build_NoParams_DropsQuestionMark()
    {
        var result = UrlQueryCodec.Build("https://example.com/posts?a=1", []);

        Assert.Equal("https://example.com/posts", result);
    }

    [Fact]
    public void Build_ReplacesExistingQuery()
    {
        var result = UrlQueryCodec.Build("https://example.com?old=1", [new QueryParam("new", "2")]);

        Assert.Equal("https://example.com?new=2", result);
    }

    [Fact]
    public void Build_AppendsQueryWhenUrlHasNone()
    {
        var result = UrlQueryCodec.Build("https://example.com/posts",
            [new QueryParam("a", "1"), new QueryParam("b", "2")]);

        Assert.Equal("https://example.com/posts?a=1&b=2", result);
    }

    [Fact]
    public void Build_EmptyValue_EmitsKeyWithTrailingEquals()
    {
        var result = UrlQueryCodec.Build("https://example.com", [new QueryParam("a", "")]);

        Assert.Equal("https://example.com?a=", result);
    }

    [Fact]
    public void Build_EscapesReservedCharacters()
    {
        var result = UrlQueryCodec.Build("https://example.com",
            [new QueryParam("a b", "c&d=e")]);

        Assert.Equal("https://example.com?a%20b=c%26d%3De", result);
    }

    [Fact]
    public void Build_BlankKeys_AreSkipped()
    {
        var result = UrlQueryCodec.Build("https://example.com",
            [new QueryParam("", "1"), new QueryParam("  ", "2"), new QueryParam("a", "3")]);

        Assert.Equal("https://example.com?a=3", result);
    }

    [Fact]
    public void Build_PreservesFragment()
    {
        var result = UrlQueryCodec.Build("https://example.com?a=1#section", [new QueryParam("b", "2")]);

        Assert.Equal("https://example.com?b=2#section", result);
    }

    [Fact]
    public void Build_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => UrlQueryCodec.Build(null!, []));
        Assert.Throws<ArgumentNullException>(() => UrlQueryCodec.Build("https://example.com", null!));
    }

    [Theory]
    [InlineData("https://example.com?a=1&b=2")]
    [InlineData("https://example.com?q=hello%20world")]
    [InlineData("https://example.com?token=a%3Db")]
    [InlineData("https://example.com?a=1#frag")]
    public void ParseThenBuild_RoundTripsUrl(string url)
    {
        Assert.Equal(url, UrlQueryCodec.Build(url, UrlQueryCodec.Parse(url)));
    }
}
