using HttpAuth;

namespace HttpAuth.Tests;

public class BearerAuthTests
{
    [Fact]
    public void BuildHeaderValue_Token_IsPrefixedWithScheme()
    {
        Assert.Equal("Bearer abc123", BearerAuth.BuildHeaderValue("abc123"));
    }

    [Theory]
    [InlineData("  abc123  ")]
    [InlineData("abc123\n")]
    [InlineData("\tabc123")]
    public void BuildHeaderValue_SurroundingWhitespace_IsTrimmed(string token)
    {
        Assert.Equal("Bearer abc123", BearerAuth.BuildHeaderValue(token));
    }

    [Theory]
    [InlineData("Bearer abc123")]
    [InlineData("bearer abc123")]
    [InlineData("  Bearer abc123  ")]
    public void BuildHeaderValue_TokenAlreadyPrefixed_IsNotPrefixedTwice(string token)
    {
        var result = BearerAuth.BuildHeaderValue(token);

        Assert.Equal(token.Trim(), result);
        Assert.DoesNotContain("Bearer Bearer", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildHeaderValue_TokenStartingWithBearerButNoSpace_IsStillPrefixed()
    {
        // "Bearerish" is a token, not a scheme — only "Bearer " marks an already-built header value.
        Assert.Equal("Bearer Bearerish", BearerAuth.BuildHeaderValue("Bearerish"));
    }

    [Fact]
    public void BuildHeaderValue_EmptyToken_StillProducesScheme()
    {
        Assert.Equal("Bearer ", BearerAuth.BuildHeaderValue(""));
    }

    [Fact]
    public void BuildHeaderValue_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => BearerAuth.BuildHeaderValue(null!));
    }

    [Fact]
    public void HeaderName_MatchesBasicAuth()
    {
        Assert.Equal(BasicAuth.HeaderName, BearerAuth.HeaderName);
    }
}
