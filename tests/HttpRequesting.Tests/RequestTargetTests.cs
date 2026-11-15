namespace HttpRequesting.Tests;

public class RequestTargetTests
{
    [Theory]
    [InlineData("https://api.test/items/1", "https://api.test")]
    [InlineData("https://api.test", "https://api.test")]
    [InlineData("http://api.test:8080/x", "http://api.test:8080")]
    [InlineData("https://api.test:443/x", "https://api.test")]          // a default port is not shown
    [InlineData("http://localhost:5168/healthz", "http://localhost:5168")]
    public void Describe_AbsoluteUrl_KeepsOnlySchemeHostAndPort(string url, string expected)
    {
        Assert.Equal(expected, RequestTarget.Describe(url));
    }

    [Fact]
    public void Describe_LeavesOutQueryFragmentAndUserInfo()
    {
        var described = RequestTarget.Describe("https://user:pw@api.test/bot123SECRET/send?key=S3CR3T#frag");

        Assert.Equal("https://api.test", described);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("/relative/path?key=S3CR3T")]
    [InlineData("{{baseUrl}}/posts")]
    public void Describe_NotAnAbsoluteUrl_ReturnsNullRatherThanEchoingIt(string? url)
    {
        Assert.Null(RequestTarget.Describe(url));
    }

    [Fact]
    public void FailureMessage_NamesTheHostAndTheReason()
    {
        var message = RequestTarget.FailureMessage("GET", "https://api.test/x?k=1", new HttpRequestException("boom"));

        Assert.Equal("Error while making GET request to https://api.test: boom", message);
    }

    [Fact]
    public void FailureMessage_UnparseableUrl_OmitsTheTargetEntirely()
    {
        var message = RequestTarget.FailureMessage("GET", "{{baseUrl}}/x?k=S3CR3T", new HttpRequestException("boom"));

        Assert.Equal("Error while making GET request: boom", message);
        Assert.DoesNotContain("S3CR3T", message);
    }
}
