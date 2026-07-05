using SuperTokensSDK.Net.Configuration;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class SuperTokensOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new SuperTokensOptions();

        Assert.Equal("sAccessToken", options.AccessTokenCookieName);
        Assert.Equal("sRefreshToken", options.RefreshTokenCookieName);
        Assert.Equal("sAntiCsrf", options.AntiCsrfCookieName);
        Assert.True(options.EnableAntiCsrf);
    }

    [Fact]
    public void CoreUri_IsSettable()
    {
        var options = new SuperTokensOptions { CoreUri = "http://localhost:3567" };
        Assert.Equal("http://localhost:3567", options.CoreUri);
    }

    [Fact]
    public void ApiKey_IsSettable()
    {
        var options = new SuperTokensOptions { ApiKey = "key-123" };
        Assert.Equal("key-123", options.ApiKey);
    }

    [Fact]
    public void AppName_IsSettable()
    {
        var options = new SuperTokensOptions { AppName = "TestApp" };
        Assert.Equal("TestApp", options.AppName);
    }

    [Fact]
    public void ApiDomain_IsSettable()
    {
        var options = new SuperTokensOptions { ApiDomain = "http://api.example.com" };
        Assert.Equal("http://api.example.com", options.ApiDomain);
    }

    [Fact]
    public void WebsiteDomain_IsSettable()
    {
        var options = new SuperTokensOptions { WebsiteDomain = "http://web.example.com" };
        Assert.Equal("http://web.example.com", options.WebsiteDomain);
    }
}
