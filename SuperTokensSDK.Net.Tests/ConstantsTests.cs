using SuperTokensSDK.Net.Core;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class ConstantsTests
{
    [Fact]
    public void SupportedCdiVersions_Contains_5_0()
    {
        Assert.Contains("5.0", Constants.SupportedCdiVersions);
    }

    [Fact]
    public void RecipeIds_AreCorrect()
    {
        Assert.Equal("session", Constants.RecipeIds.Session);
        Assert.Equal("emailpassword", Constants.RecipeIds.EmailPassword);
        Assert.Equal("userroles", Constants.RecipeIds.UserRoles);
        Assert.Equal("usermetadata", Constants.RecipeIds.UserMetadata);
    }

    [Fact]
    public void CookieNames_AreCorrect()
    {
        Assert.Equal("sAccessToken", Constants.CookieNames.AccessToken);
        Assert.Equal("sRefreshToken", Constants.CookieNames.RefreshToken);
        Assert.Equal("sIdRefreshToken", Constants.CookieNames.IdRefreshToken);
        Assert.Equal("sAntiCsrf", Constants.CookieNames.AntiCsrf);
    }

    [Fact]
    public void HeaderNames_AreCorrect()
    {
        Assert.Equal("st-access-token", Constants.HeaderNames.AccessToken);
        Assert.Equal("st-refresh-token", Constants.HeaderNames.RefreshToken);
        Assert.Equal("anti-csrf", Constants.HeaderNames.AntiCsrf);
        Assert.Equal("front-token", Constants.HeaderNames.FrontToken);
        Assert.Equal("st-auth-mode", Constants.HeaderNames.AuthMode);
        Assert.Equal("rid", Constants.HeaderNames.Rid);
        Assert.Equal("cdi-version", Constants.HeaderNames.CdiVersion);
        Assert.Equal("api-key", Constants.HeaderNames.ApiKey);
    }

    [Fact]
    public void Paths_AreCorrect()
    {
        Assert.Equal("/apiversion", Constants.Paths.ApiVersion);
        Assert.Equal("/recipe/session", Constants.Paths.RecipeSession);
        Assert.Equal("/recipe/session/verify", Constants.Paths.RecipeSessionVerify);
        Assert.Equal("/recipe/session/refresh", Constants.Paths.RecipeSessionRefresh);
        Assert.Equal("/recipe/session/revoke", Constants.Paths.RecipeSessionRevoke);
        Assert.Equal("/recipe/signup", Constants.Paths.RecipeSignUp);
        Assert.Equal("/recipe/signin", Constants.Paths.RecipeSignIn);
        Assert.Equal("/recipe/user/password/reset", Constants.Paths.RecipeUserPasswordReset);
        Assert.Equal("/recipe/user/roles", Constants.Paths.RecipeUserRoles);
        Assert.Equal("/recipe/user/role", Constants.Paths.RecipeUserRole);
        Assert.Equal("/recipe/user/metadata", Constants.Paths.RecipeUserMetadata);
    }

    [Fact]
    public void StatusValues_AreCorrect()
    {
        Assert.Equal("OK", Constants.Status.Ok);
        Assert.Equal("UNAUTHORISED", Constants.Status.Unauthorized);
        Assert.Equal("TRY_REFRESH_TOKEN", Constants.Status.TryRefreshToken);
        Assert.Equal("TOKEN_THEFT_DETECTED", Constants.Status.TokenTheftDetected);
        Assert.Equal("INVALID_CLAIMS", Constants.Status.InvalidClaims);
    }

    [Fact]
    public void RateLimitConstants_AreCorrect()
    {
        Assert.Equal(429, Constants.RateLimitStatusCode);
        Assert.Equal(5, Constants.RateLimitRetries);
    }

    [Fact]
    public void DefaultTenantId_IsPublic()
    {
        Assert.Equal("public", Constants.DefaultTenantId);
    }
}
