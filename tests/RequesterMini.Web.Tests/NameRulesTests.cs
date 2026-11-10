using RequesterMini.Web.Services;

namespace RequesterMini.Web.Tests;

public class NameRulesTests
{
    [Fact]
    public void Validate_FreshName_SucceedsWithTheTrimmedValue()
    {
        var result = NameRules.Validate("  Api  ", [], "collection");

        Assert.True(result.IsSuccess);
        Assert.Equal("Api", result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankName_FailsWithAReadableReason(string? name)
    {
        var result = NameRules.Validate(name, [], "collection");

        Assert.True(result.IsFailed);
        Assert.Contains("cannot be blank", result.Errors.Single().Message);
        Assert.Contains("collection", result.Errors.Single().Message);
    }

    [Theory]
    [InlineData("Api")]
    [InlineData("api")]
    [InlineData("API")]
    public void Validate_NameTakenIgnoringCase_FailsAndNamesTheClash(string name)
    {
        var result = NameRules.Validate(name, ["Api"], "environment");

        Assert.True(result.IsFailed);
        Assert.Contains($"\"{name}\"", result.Errors.Single().Message);
        Assert.Contains("environment", result.Errors.Single().Message);
    }

    [Fact]
    public void Validate_TrimmedNameThatClashes_IsStillAClash()
    {
        Assert.True(NameRules.Validate("  api  ", ["Api"], "collection").IsFailed);
    }

    [Fact]
    public void Validate_NameDifferentFromAllExisting_Succeeds()
    {
        Assert.True(NameRules.Validate("Staging", ["Dev", "Prod"], "environment").IsSuccess);
    }
}
