using Templating;

namespace Templating.Tests;

public class VariableInterpolatorTests
{
    private static Dictionary<string, string> Vars(params (string Key, string Value)[] pairs) =>
        pairs.ToDictionary(p => p.Key, p => p.Value);

    [Fact]
    public void Resolve_KnownPlaceholder_IsSubstituted()
    {
        var result = VariableInterpolator.Resolve(
            "{{baseUrl}}/posts/1", Vars(("baseUrl", "https://api.test")));

        Assert.Equal("https://api.test/posts/1", result);
    }

    [Fact]
    public void Resolve_SeveralPlaceholders_AreAllSubstituted()
    {
        var result = VariableInterpolator.Resolve(
            "{{scheme}}://{{host}}/{{path}}",
            Vars(("scheme", "https"), ("host", "api.test"), ("path", "posts")));

        Assert.Equal("https://api.test/posts", result);
    }

    [Fact]
    public void Resolve_RepeatedPlaceholder_IsSubstitutedEverywhere()
    {
        var result = VariableInterpolator.Resolve("{{a}}-{{a}}", Vars(("a", "x")));

        Assert.Equal("x-x", result);
    }

    [Theory]
    [InlineData("{{ name }}")]
    [InlineData("{{name }}")]
    [InlineData("{{  name}}")]
    public void Resolve_SurroundingSpacesInsideBraces_AreAllowed(string template)
    {
        Assert.Equal("ada", VariableInterpolator.Resolve(template, Vars(("name", "ada"))));
    }

    [Fact]
    public void Resolve_UnknownPlaceholder_IsLeftAsWritten()
    {
        var result = VariableInterpolator.Resolve("{{missing}}/x", Vars(("other", "1")));

        Assert.Equal("{{missing}}/x", result);
    }

    [Fact]
    public void Resolve_VariableDefinedInTermsOfAnother_IsResolved()
    {
        var result = VariableInterpolator.Resolve(
            "{{baseUrl}}/posts",
            Vars(("baseUrl", "{{scheme}}://{{host}}"), ("scheme", "https"), ("host", "api.test")));

        Assert.Equal("https://api.test/posts", result);
    }

    [Fact]
    public void Resolve_VariablesReferringToEachOther_StopsInsteadOfLooping()
    {
        // Nothing sensible to produce here; what matters is that it returns rather than hanging.
        var result = VariableInterpolator.Resolve("{{a}}", Vars(("a", "{{b}}"), ("b", "{{a}}")));

        Assert.Contains("{{", result);
    }

    [Theory]
    [InlineData("{\"a\": 1}")]
    [InlineData("{{}}")]
    [InlineData("{{ }}")]
    [InlineData("{{not a name}}")]
    [InlineData("{single}")]
    public void Resolve_TextThatIsNotAPlaceholder_IsUntouched(string template)
    {
        Assert.Equal(template, VariableInterpolator.Resolve(template, Vars(("a", "x"))));
    }

    [Fact]
    public void Resolve_JsonBodyWithPlaceholder_KeepsTheJsonIntact()
    {
        var result = VariableInterpolator.Resolve(
            "{\"token\": \"{{token}}\", \"nested\": {\"a\": 1}}", Vars(("token", "abc")));

        Assert.Equal("{\"token\": \"abc\", \"nested\": {\"a\": 1}}", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Resolve_EmptyTemplate_ReturnsEmpty(string? template)
    {
        Assert.Equal("", VariableInterpolator.Resolve(template, Vars(("a", "x"))));
    }

    [Fact]
    public void Resolve_NoVariables_ReturnsTemplateUnchanged()
    {
        Assert.Equal("{{a}}", VariableInterpolator.Resolve("{{a}}", null));
        Assert.Equal("{{a}}", VariableInterpolator.Resolve("{{a}}", Vars()));
    }

    [Fact]
    public void Resolve_NameWithDotsAndDashes_IsSupported()
    {
        var result = VariableInterpolator.Resolve(
            "{{api.base-url}}", Vars(("api.base-url", "https://api.test")));

        Assert.Equal("https://api.test", result);
    }

    [Fact]
    public void FindUnresolved_ReportsOnlyMissingNames()
    {
        var missing = VariableInterpolator.FindUnresolved(
            "{{known}}/{{missing}}", Vars(("known", "x")));

        Assert.Equal(["missing"], missing);
    }

    [Fact]
    public void FindUnresolved_RepeatedMissingName_IsReportedOnce()
    {
        var missing = VariableInterpolator.FindUnresolved("{{a}}{{a}}", null);

        Assert.Equal(["a"], missing);
    }

    [Fact]
    public void FindUnresolved_SeveralMissing_AreInFirstSeenOrder()
    {
        var missing = VariableInterpolator.FindUnresolved("{{z}} {{a}} {{z}}", null);

        Assert.Equal(["z", "a"], missing);
    }

    [Fact]
    public void FindUnresolved_NothingMissing_IsEmpty()
    {
        Assert.Empty(VariableInterpolator.FindUnresolved("{{a}}", Vars(("a", "x"))));
    }

    [Theory]
    [InlineData("{{a}}", true)]
    [InlineData("plain text", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void HasPlaceholders_DetectsThem(string? template, bool expected)
    {
        Assert.Equal(expected, VariableInterpolator.HasPlaceholders(template));
    }
}
