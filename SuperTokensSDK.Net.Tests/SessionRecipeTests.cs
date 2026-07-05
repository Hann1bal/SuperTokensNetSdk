using Moq;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;
using SuperTokensSDK.Net.Recipes.Session;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class SessionRecipeTests
{
    private readonly Mock<ICoreApiClient> _coreMock = new();

    [Fact]
    public async Task CreateSessionAsync_CallsCore_WithCorrectRequest()
    {
        var response = new CreateOrRefreshAPIResponse
        {
            Status = "OK",
            Session = new SessionStruct { Handle = "sh1", UserId = "u1", UserDataInJWT = new() },
            AccessToken = new TokenInfo { Token = "at1", Expiry = 1893456000000 },
            RefreshToken = new TokenInfo { Token = "rt1", Expiry = 1893456000000 },
            AntiCsrfToken = "csrf1"
        };
        _coreMock.Setup(c => c.CreateSessionAsync(It.IsAny<CreateSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var recipe = new SessionRecipe(_coreMock.Object);
        var container = await recipe.CreateSessionAsync("u1", new Dictionary<string, object> { ["roles"] = "admin" }, new Dictionary<string, object> { ["x"] = 1 });

        Assert.Equal("u1", container.UserId);
        Assert.Equal("at1", container.AccessToken);
        Assert.Equal("rt1", container.RefreshToken);
        Assert.Equal("csrf1", container.AntiCsrfToken);

        _coreMock.Verify(c => c.CreateSessionAsync(
            It.Is<CreateSessionRequest>(r =>
                r.UserId == "u1" &&
                r.EnableAntiCsrf == true &&
                r.UserDataInJWT != null && (string)r.UserDataInJWT["roles"]! == "admin" &&
                r.UserDataInDatabase != null && (int)r.UserDataInDatabase["x"]! == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSessionAsync_WorksWithNullPayloads()
    {
        _coreMock.Setup(c => c.CreateSessionAsync(It.IsAny<CreateSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateOrRefreshAPIResponse
            {
                Status = "OK",
                Session = new SessionStruct { Handle = "sh", UserId = "u", UserDataInJWT = new() }
            });

        var recipe = new SessionRecipe(_coreMock.Object);
        var container = await recipe.CreateSessionAsync("u", null, null);

        Assert.NotNull(container);
        _coreMock.Verify(c => c.CreateSessionAsync(
            It.Is<CreateSessionRequest>(r => r.UserDataInJWT != null && r.UserDataInDatabase != null && r.EnableAntiCsrf),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifySessionAsync_ValidJwt_ReturnsContainer()
    {
        var jwt = TestJwtHelper.CreateJwt(new Dictionary<string, object>
        {
            ["sub"] = "user-valid",
            ["sessionHandle"] = "sh-valid",
            ["roles"] = "admin",
            ["custom"] = "v"
        });

        var recipe = new SessionRecipe(_coreMock.Object);
        var container = await recipe.VerifySessionAsync(jwt);

        Assert.Equal("user-valid", container.UserId);
        Assert.Equal("sh-valid", container.SessionHandle);
        Assert.Equal(jwt, container.AccessToken);
        Assert.Equal("admin", container.UserDataInJwt["roles"]);
        Assert.Equal("v", container.UserDataInJwt["custom"]);
    }

    [Fact]
    public async Task VerifySessionAsync_ExpiredJwt_ThrowsUnauthorizedException()
    {
        var jwt = TestJwtHelper.CreateJwt(new Dictionary<string, object>
        {
            ["sub"] = "user-expired",
            ["sessionHandle"] = "sh-expired"
        }, expired: true);

        var recipe = new SessionRecipe(_coreMock.Object);
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => recipe.VerifySessionAsync(jwt));
        Assert.Contains("expired", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifySessionAsync_MalformedJwt_ThrowsUnauthorizedException()
    {
        var recipe = new SessionRecipe(_coreMock.Object);
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => recipe.VerifySessionAsync("not-a-jwt"));
        Assert.IsAssignableFrom<SuperTokensException>(ex);
    }

    [Fact]
    public async Task VerifySessionAsync_MissingSub_ThrowsUnauthorizedException()
    {
        var jwt = TestJwtHelper.CreateJwt(new Dictionary<string, object>
        {
            ["sessionHandle"] = "sh-no-sub"
        });

        var recipe = new SessionRecipe(_coreMock.Object);
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => recipe.VerifySessionAsync(jwt));
        Assert.Contains("userId", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifySessionAsync_StripsProtectedFields()
    {
        var jwt = TestJwtHelper.CreateJwt(new Dictionary<string, object>
        {
            ["sub"] = "user-protected",
            ["iat"] = 1234567890,
            ["exp"] = TestJwtHelper.FutureEpoch(),
            ["sessionHandle"] = "sh-protected",
            ["parentRefreshTokenHash1"] = "h1",
            ["refreshTokenHash1"] = "h2",
            ["antiCsrfToken"] = "csrf",
            ["rsub"] = "r",
            ["tId"] = "public",
            ["custom"] = "keep"
        });

        var recipe = new SessionRecipe(_coreMock.Object);
        var container = await recipe.VerifySessionAsync(jwt);

        var protectedFields = new[] { "sub", "iat", "exp", "sessionHandle", "parentRefreshTokenHash1", "refreshTokenHash1", "antiCsrfToken", "rsub", "tId" };
        foreach (var field in protectedFields)
        {
            Assert.False(container.UserDataInJwt.ContainsKey(field), $"Protected field '{field}' should be stripped.");
        }

        Assert.Equal("keep", container.UserDataInJwt["custom"]);
    }

    [Fact]
    public async Task RefreshSessionAsync_WithAntiCsrfToken_SendsEnableAntiCsrf()
    {
        _coreMock.Setup(c => c.RefreshSessionAsync(It.IsAny<RefreshSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateOrRefreshAPIResponse
            {
                Status = "OK",
                Session = new SessionStruct { Handle = "sh-new", UserId = "u", UserDataInJWT = new() }
            });

        var recipe = new SessionRecipe(_coreMock.Object);
        await recipe.RefreshSessionAsync("rt", "csrf-token");

        _coreMock.Verify(c => c.RefreshSessionAsync(
            It.Is<RefreshSessionRequest>(r => r.RefreshToken == "rt" && r.AntiCsrfToken == "csrf-token" && r.EnableAntiCsrf),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshSessionAsync_WithoutAntiCsrfToken_DoesNotEnableAntiCsrf()
    {
        _coreMock.Setup(c => c.RefreshSessionAsync(It.IsAny<RefreshSessionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateOrRefreshAPIResponse
            {
                Status = "OK",
                Session = new SessionStruct { Handle = "sh-new", UserId = "u", UserDataInJWT = new() }
            });

        var recipe = new SessionRecipe(_coreMock.Object);
        await recipe.RefreshSessionAsync("rt", null);

        _coreMock.Verify(c => c.RefreshSessionAsync(
            It.Is<RefreshSessionRequest>(r => r.RefreshToken == "rt" && r.AntiCsrfToken == null && !r.EnableAntiCsrf),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeSessionAsync_CallsCore_WithSessionHandle()
    {
        _coreMock.Setup(c => c.RevokeMultipleSessionsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "sh-revoke" });

        var recipe = new SessionRecipe(_coreMock.Object);
        await recipe.RevokeSessionAsync("sh-revoke");

        _coreMock.Verify(c => c.RevokeMultipleSessionsAsync(
            It.Is<List<string>>(l => l.Contains("sh-revoke")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_NullCoreApiClient_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SessionRecipe(null!));
    }
}
