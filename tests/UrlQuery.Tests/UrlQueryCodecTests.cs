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

    // {{placeholders}} ------------------------------------------------------------------------
    // The variables feature resolves {{name}} when a request is sent. Rewriting the query used to
    // percent-encode the braces, so touching the Params tab turned {{id}} into %7B%7Bid%7D%7D and the
    // placeholder stopped resolving.

    [Fact]
    public void Build_PlaceholderInAValue_IsNotPercentEncoded()
    {
        var result = UrlQueryCodec.Build("https://example.com/a", [new QueryParam("id", "{{id}}")]);

        Assert.Equal("https://example.com/a?id={{id}}", result);
    }

    [Fact]
    public void Build_PlaceholderInAKey_IsNotPercentEncoded()
    {
        var result = UrlQueryCodec.Build("https://example.com", [new QueryParam("{{k}}", "v")]);

        Assert.Equal("https://example.com?{{k}}=v", result);
    }

    [Fact]
    public void Build_PlaceholderWithSpacesInsideTheBraces_IsNotPercentEncoded()
    {
        var result = UrlQueryCodec.Build("https://example.com", [new QueryParam("id", "{{ id }}")]);

        Assert.Equal("https://example.com?id={{ id }}", result);
    }

    [Fact]
    public void Build_TextAroundAPlaceholder_IsStillEscaped()
    {
        var result = UrlQueryCodec.Build("https://example.com", [new QueryParam("q", "pre {{id}} post&x")]);

        Assert.Equal("https://example.com?q=pre%20{{id}}%20post%26x", result);
    }

    [Theory]
    [InlineData("{notAPlaceholder}", "%7BnotAPlaceholder%7D")]
    [InlineData("{{}}", "%7B%7B%7D%7D")]
    [InlineData("{{not a name}}", "%7B%7Bnot%20a%20name%7D%7D")]
    public void Build_BracesThatAreNotAPlaceholder_AreEscapedAsBefore(string value, string expected)
    {
        var result = UrlQueryCodec.Build("https://example.com", [new QueryParam("q", value)]);

        Assert.Equal($"https://example.com?q={expected}", result);
    }

    [Fact]
    public void ParseThenBuild_KeepsAPlaceholderIntact()
    {
        const string url = "https://example.com/items?id={{id}}&sort={{ order }}";

        Assert.Equal(url, UrlQueryCodec.Build(url, UrlQueryCodec.Parse(url)));
    }

    // Untouched parameters keep their text ---------------------------------------------------------

    [Theory]
    [InlineData("https://example.com?q=hello+world")]                  // '+' in a URL is a space to most servers
    [InlineData("https://example.com?t=a%2Bb")]                        // %2B is a literal plus
    [InlineData("https://example.com?q=hello+world&t=a%2Bb")]
    [InlineData("https://example.com?flag")]                           // a key with no '='
    [InlineData("https://example.com?a=1&a=2")]                        // repeated keys
    [InlineData("https://example.com?x=%41")]                          // an escape that did not need escaping
    public void ParseThenBuild_KeepsAnUntouchedParameterExactlyAsWritten(string url)
    {
        // Parse treats '+' as a literal and Build escapes it, so a round trip used to turn
        // "hello+world" into "hello%2Bworld" — a different request — just from opening the Params tab.
        Assert.Equal(url, UrlQueryCodec.Build(url, UrlQueryCodec.Parse(url)));
    }

    [Fact]
    public void Build_EditedParameter_IsEncodedFreshWhileTheOthersKeepTheirText()
    {
        const string url = "https://example.com?q=hello+world&id=1";
        var parameters = UrlQueryCodec.Parse(url);
        parameters[1] = parameters[1] with { Value = "2" };

        Assert.Equal("https://example.com?q=hello+world&id=2", UrlQueryCodec.Build(url, parameters));
    }

    [Fact]
    public void Build_RepeatedKeys_EditingOneLeavesTheOtherAsWritten()
    {
        const string url = "https://example.com?a=1&a=2";
        var parameters = UrlQueryCodec.Parse(url);
        parameters[0] = parameters[0] with { Value = "9" };

        Assert.Equal("https://example.com?a=9&a=2", UrlQueryCodec.Build(url, parameters));
    }

    [Fact]
    public void Build_ReorderedParameters_KeepEachOnesText()
    {
        const string url = "https://example.com?a=x+y&b=1";
        var parameters = UrlQueryCodec.Parse(url);
        parameters.Reverse();

        Assert.Equal("https://example.com?b=1&a=x+y", UrlQueryCodec.Build(url, parameters));
    }

    [Fact]
    public void Build_ANewParameterWithAPlus_IsEscapedSoItCannotBeMistakenForASpace()
    {
        var result = UrlQueryCodec.Build("https://example.com", [new QueryParam("t", "a+b")]);

        Assert.Equal("https://example.com?t=a%2Bb", result);
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

    [Fact]
    public void ParseQuery_BarePairs_AreParsedInOrder()
    {
        var result = UrlQueryCodec.ParseQuery("name=ada&role=engineer");

        Assert.Collection(result,
            first => Assert.Equal(("name", "ada"), (first.Key, first.Value)),
            second => Assert.Equal(("role", "engineer"), (second.Key, second.Value)));
    }

    [Fact]
    public void ParseQuery_EscapedValue_IsUnescaped()
    {
        Assert.Equal("hello world", UrlQueryCodec.ParseQuery("q=hello%20world").Single().Value);
    }

    [Fact]
    public void ParseQuery_SegmentWithoutEquals_YieldsEmptyValue()
    {
        var single = UrlQueryCodec.ParseQuery("flag").Single();

        Assert.Equal("flag", single.Key);
        Assert.Equal("", single.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("&")]
    [InlineData("=novalue")]
    public void ParseQuery_NothingUsable_ReturnsEmpty(string query)
    {
        Assert.Empty(UrlQueryCodec.ParseQuery(query));
    }

    [Fact]
    public void ParseQuery_Plus_StaysLiteral()
    {
        Assert.Equal("a+b", UrlQueryCodec.ParseQuery("q=a+b").Single().Value);
    }

    [Fact]
    public void ParseForm_Plus_IsASpace()
    {
        Assert.Equal("hello world", UrlQueryCodec.ParseForm("q=hello+world").Single().Value);
    }

    [Fact]
    public void ParseForm_EncodedPlus_IsALiteralPlus()
    {
        // The '+' is turned into a space BEFORE unescaping, so %2B still decodes to a plus.
        Assert.Equal("a+b", UrlQueryCodec.ParseForm("q=a%2Bb").Single().Value);
    }

    [Fact]
    public void ParseForm_PlusInTheKey_IsASpaceToo()
    {
        Assert.Equal("my key", UrlQueryCodec.ParseForm("my+key=v").Single().Key);
    }

    [Fact]
    public void ParseForm_OtherwiseBehavesLikeParseQuery()
    {
        Assert.Equal(
            UrlQueryCodec.ParseQuery("a=1&b=x%20y&flag&=orphan&&c="),
            UrlQueryCodec.ParseForm("a=1&b=x%20y&flag&=orphan&&c="));
    }

    [Fact]
    public void ParseForm_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => UrlQueryCodec.ParseForm(null!));
    }

    [Fact]
    public void ParseQuery_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => UrlQueryCodec.ParseQuery(null!));
    }

    [Fact]
    public void Parse_UrlWithQuery_MatchesParseQueryOfSamePairs()
    {
        Assert.Equal(
            UrlQueryCodec.ParseQuery("a=1&b=2"),
            UrlQueryCodec.Parse("https://example.com/path?a=1&b=2"));
    }
}
