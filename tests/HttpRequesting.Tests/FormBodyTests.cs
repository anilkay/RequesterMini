namespace HttpRequesting.Tests;

public class FormBodyTests
{
    [Theory]
    [InlineData("name=ada&role=engineer", "name=ada&role=engineer")]
    [InlineData("q=hello world", "q=hello+world")]          // a typed space is written as '+'
    [InlineData("q=hello+world", "q=hello+world")]          // and a typed '+' already means a space
    [InlineData("q=a%2Bb", "q=a%2Bb")]                      // an encoded plus stays a literal plus
    [InlineData("t=a=b", "t=a%3Db")]                        // '=' inside a value must not read as a separator
    [InlineData("t=a%26b", "t=a%26b")]                      // nor '&'
    [InlineData("n=çay", "n=%C3%A7ay")]                     // non-ASCII is UTF-8 percent-encoded
    [InlineData("flag", "flag=")]                           // a key with no value
    [InlineData("a=1&&b=2&", "a=1&b=2")]                    // empty segments are skipped
    [InlineData("=orphan&k=v", "k=v")]                      // a value with no key is dropped
    [InlineData("", "")]
    public void Encode_TypedBody_ProducesTheWireText(string typed, string expected)
    {
        Assert.Equal(expected, FormBody.Encode(typed));
    }

    [Theory]
    [InlineData("name=ada&role=engineer")]
    [InlineData("q=hello world&tag=a=b")]
    [InlineData("q=hello+world")]
    [InlineData("k=~-._*'()!")]                             // characters whose escaping differs between encoders
    [InlineData("n=çay&e=日本語")]
    [InlineData("k=%41%42")]
    public async Task Encode_MatchesWhatFormUrlEncodedContentWouldHaveSent(string typed)
    {
        // The sender used to build FormUrlEncodedContent from the parsed pairs. FormBody must write the
        // same bytes, so moving to it changed nothing for any body except the '+' meaning.
        var pairs = UrlQuery.UrlQueryCodec.ParseForm(typed)
            .Select(p => new KeyValuePair<string, string>(p.Key, p.Value));

        var legacy = await new FormUrlEncodedContent(pairs).ReadAsStringAsync();

        Assert.Equal(legacy, FormBody.Encode(typed));
    }

    [Fact]
    public void Encode_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => FormBody.Encode(null!));
    }
}
