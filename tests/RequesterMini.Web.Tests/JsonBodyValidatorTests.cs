using RequesterMini.Web.Services;

namespace RequesterMini.Web.Tests;

public class JsonBodyValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{}")]
    [InlineData("{\"a\": [1, 2, {\"b\": null}]}")]
    public void Validate_ValidOrEmpty_ReturnsNull(string body)
    {
        Assert.Null(JsonBodyValidator.Validate(body));
    }

    [Fact]
    public void Validate_Malformed_ReportsLineAndColumn()
    {
        var error = JsonBodyValidator.Validate("{\n  \"a\": ,\n}");

        Assert.NotNull(error);
        Assert.Contains("Line 2", error);
        Assert.Contains("Col ", error);
    }

    [Fact]
    public void Validate_Malformed_DropsFrameworkPositionSuffix()
    {
        var error = JsonBodyValidator.Validate("{oops}");

        Assert.NotNull(error);
        Assert.DoesNotContain("BytePositionInLine", error);
    }
}
