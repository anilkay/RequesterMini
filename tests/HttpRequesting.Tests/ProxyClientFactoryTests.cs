using System.Net;

namespace HttpRequesting.Tests;

public class ProxyClientFactoryTests
{
    [Fact]
    public void CreateProxy_ValidUrl_UsesItAsTheAddress()
    {
        var result = ProxyClientFactory.CreateProxy(new ProxySettings("http://proxy.test:8080"));

        Assert.Equal(new Uri("http://proxy.test:8080"), result.AsT0.Address);
    }

    [Fact]
    public void CreateProxy_SurroundingWhitespace_IsTrimmed()
    {
        var result = ProxyClientFactory.CreateProxy(new ProxySettings("  http://proxy.test:8080  "));

        Assert.Equal(new Uri("http://proxy.test:8080"), result.AsT0.Address);
    }

    [Fact]
    public void CreateProxy_NoUsername_LeavesCredentialsUnset()
    {
        var result = ProxyClientFactory.CreateProxy(new ProxySettings("http://proxy.test:8080"));

        Assert.Null(result.AsT0.Credentials);
    }

    [Fact]
    public void CreateProxy_Username_SetsNetworkCredential()
    {
        var result = ProxyClientFactory.CreateProxy(
            new ProxySettings("http://proxy.test:8080", "user", "pass"));

        var credential = Assert.IsType<NetworkCredential>(result.AsT0.Credentials);
        Assert.Equal("user", credential.UserName);
        Assert.Equal("pass", credential.Password);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateProxy_BlankUrl_ReportsNoProxy(string url)
    {
        var result = ProxyClientFactory.CreateProxy(new ProxySettings(url));

        Assert.True(result.IsT1);
        Assert.Equal("No proxy is configured.", result.AsT1.Message);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("proxy.test:8080")]  // no scheme — Uri reads "proxy.test" as the scheme
    [InlineData("ftp://proxy.test")]
    public void CreateProxy_UrlWithoutHttpScheme_ReportsItWithTheInput(string url)
    {
        var result = ProxyClientFactory.CreateProxy(new ProxySettings(url));

        Assert.True(result.IsT1);
        Assert.Contains(url, result.AsT1.Message);
    }

    [Theory]
    [InlineData("http://proxy.test:8080")]
    [InlineData("https://proxy.test:8080")]
    public void CreateProxy_HttpAndHttpsSchemes_AreAccepted(string url)
    {
        Assert.True(ProxyClientFactory.CreateProxy(new ProxySettings(url)).IsT0);
    }

    [Fact]
    public void Create_ValidSettings_ReturnsClient()
    {
        var result = ProxyClientFactory.Create(new ProxySettings("http://proxy.test:8080"));

        Assert.True(result.IsT0);
        result.AsT0.Dispose();
    }

    [Fact]
    public void Create_MalformedUrl_ReturnsSameErrorAsCreateProxy()
    {
        var settings = new ProxySettings("not a url");

        Assert.Equal(
            ProxyClientFactory.CreateProxy(settings).AsT1,
            ProxyClientFactory.Create(settings).AsT1);
    }

    [Fact]
    public void CreateProxy_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ProxyClientFactory.CreateProxy(null!));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("http://proxy.test", true)]
    public void IsEnabled_FollowsTheUrl(string url, bool expected)
    {
        Assert.Equal(expected, new ProxySettings(url).IsEnabled);
    }

    [Fact]
    public void None_IsNotEnabled()
    {
        Assert.False(ProxySettings.None.IsEnabled);
    }
}
