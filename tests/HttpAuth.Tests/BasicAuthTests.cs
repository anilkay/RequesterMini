using System.Text;
using HttpAuth;

namespace HttpAuth.Tests;

public class BasicAuthTests
{
    [Fact]
    public void BuildHeaderValue_KnownCredentials_MatchesRfcExample()
    {
        // RFC 7617 section 2 example: "Aladdin" / "open sesame"
        var result = BasicAuth.BuildHeaderValue("Aladdin", "open sesame");

        Assert.Equal("Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==", result);
    }

    [Fact]
    public void BuildHeaderValue_StartsWithBasicScheme()
    {
        Assert.StartsWith("Basic ", BasicAuth.BuildHeaderValue("user", "pass"));
    }

    [Fact]
    public void BuildHeaderValue_DecodesBackToColonSeparatedPair()
    {
        var result = BasicAuth.BuildHeaderValue("user", "pass");
        var token = result["Basic ".Length..];

        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));

        Assert.Equal("user:pass", decoded);
    }

    [Fact]
    public void BuildHeaderValue_EmptyPassword_EncodesTrailingColon()
    {
        var result = BasicAuth.BuildHeaderValue("user", "");
        var decoded = Decode(result);

        Assert.Equal("user:", decoded);
    }

    [Fact]
    public void BuildHeaderValue_EmptyUsername_EncodesLeadingColon()
    {
        var result = BasicAuth.BuildHeaderValue("", "pass");
        var decoded = Decode(result);

        Assert.Equal(":pass", decoded);
    }

    [Fact]
    public void BuildHeaderValue_PasswordContainingColon_IsPreserved()
    {
        var result = BasicAuth.BuildHeaderValue("user", "pa:ss:word");
        var decoded = Decode(result);

        Assert.Equal("user:pa:ss:word", decoded);
    }

    [Fact]
    public void BuildHeaderValue_NonAsciiCredentials_AreUtf8Encoded()
    {
        var result = BasicAuth.BuildHeaderValue("kullanıcı", "şifre");
        var token = result["Basic ".Length..];

        var expected = Convert.ToBase64String(Encoding.UTF8.GetBytes("kullanıcı:şifre"));
        Assert.Equal(expected, token);
        Assert.Equal("kullanıcı:şifre", Decode(result));
    }

    [Fact]
    public void BuildHeaderValue_SpecialCharacters_AreNotEscaped()
    {
        var decoded = Decode(BasicAuth.BuildHeaderValue("a b&c", "p@ss/w=rd"));

        Assert.Equal("a b&c:p@ss/w=rd", decoded);
    }

    [Fact]
    public void BuildHeaderValue_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => BasicAuth.BuildHeaderValue(null!, "pass"));
        Assert.Throws<ArgumentNullException>(() => BasicAuth.BuildHeaderValue("user", null!));
    }

    [Fact]
    public void HeaderName_IsAuthorization()
    {
        Assert.Equal("Authorization", BasicAuth.HeaderName);
    }

    private static string Decode(string headerValue) =>
        Encoding.UTF8.GetString(Convert.FromBase64String(headerValue["Basic ".Length..]));
}
