using Moq;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Claims;
using SuperTokensSDK.Net.Core.Models;
using SuperTokensSDK.Net.Recipes.Session;
using SuperTokensSDK.Net.Recipes.UserRoles;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class SessionClaimTests
{
    private readonly Mock<ICoreApiClient> _coreMock = new();

    [Fact]
    public async Task PrimitiveArrayClaim_Includes_ReturnsValidWhenPresent()
    {
        var claim = PrimitiveArrayClaim.Create("roles", async (_, _, _) => new[] { "admin", "editor" }, TimeSpan.FromMinutes(5));
        var validator = claim.Includes("admin");

        var payload = new Dictionary<string, object?> { ["roles"] = new[] { "admin", "editor" } };
        var result = validator.Validate(payload);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task PrimitiveArrayClaim_Includes_ReturnsInvalidWhenMissing()
    {
        var claim = PrimitiveArrayClaim.Create("roles", async (_, _, _) => new[] { "admin" }, TimeSpan.FromMinutes(5));
        var validator = claim.Includes("editor");

        var payload = new Dictionary<string, object?> { ["roles"] = new[] { "admin" } };
        var result = validator.Validate(payload);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task PrimitiveArrayClaim_Excludes_ReturnsInvalidWhenPresent()
    {
        var claim = PrimitiveArrayClaim.Create("roles", async (_, _, _) => Array.Empty<string>(), TimeSpan.FromMinutes(5));
        var validator = claim.Excludes("admin");

        var payload = new Dictionary<string, object?> { ["roles"] = new[] { "admin" } };
        var result = validator.Validate(payload);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateClaimsAsync_RefetchesMissingClaim_AndValidates()
    {
        _coreMock.SetupSequence(c => c.GetSessionInformationAsync("sh1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionInfo
            {
                Handle = "sh1",
                UserId = "u1",
                TenantId = "public",
                UserDataInJWT = new Dictionary<string, object>()
            })
            .ReturnsAsync(new SessionInfo
            {
                Handle = "sh1",
                UserId = "u1",
                TenantId = "public",
                UserDataInJWT = new Dictionary<string, object>
                {
                    ["st-role"] = new[] { "admin" },
                    ["_st-role"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }
            });

        _coreMock.Setup(c => c.UpdateJwtDataAsync(It.IsAny<UpdateJwtDataRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatusResponse { Status = "OK" });

        var recipe = new SessionRecipe(_coreMock.Object);
        var roleClaim = PrimitiveArrayClaim.Create("st-role", async (_, _, _) => new[] { "admin" }, TimeSpan.FromMinutes(5));
        var (_, validators) = UserRolesClaims.CreateRoleValidators(roleClaim);

        var results = await recipe.ValidateClaimsAsync("sh1", validators.ToList());

        Assert.Single(results);
        Assert.True(results[0].IsValid);
        _coreMock.Verify(c => c.UpdateJwtDataAsync(
            It.Is<UpdateJwtDataRequest>(r =>
                r.SessionHandle == "sh1" &&
                r.UserDataInJWT.ContainsKey("st-role") &&
                r.UserDataInJWT.ContainsKey("_st-role")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetClaimValueAsync_PersistsClaim()
    {
        _coreMock.Setup(c => c.GetSessionInformationAsync("sh2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionInfo
            {
                Handle = "sh2",
                UserId = "u2",
                TenantId = "public",
                UserDataInJWT = new Dictionary<string, object>()
            });

        _coreMock.Setup(c => c.UpdateJwtDataAsync(It.IsAny<UpdateJwtDataRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatusResponse { Status = "OK" });

        var recipe = new SessionRecipe(_coreMock.Object);
        var claim = PrimitiveArrayClaim.Create("st-role", async (_, _, _) => new[] { "admin" }, TimeSpan.FromMinutes(5));

        await recipe.SetClaimValueAsync("sh2", claim, new[] { "admin", "editor" });

        _coreMock.Verify(c => c.UpdateJwtDataAsync(
            It.Is<UpdateJwtDataRequest>(r =>
                r.UserDataInJWT.ContainsKey("st-role") &&
                r.UserDataInJWT.ContainsKey("_st-role")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetClaimValueAsync_ReturnsTypedValue()
    {
        _coreMock.Setup(c => c.GetSessionInformationAsync("sh3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionInfo
            {
                Handle = "sh3",
                UserId = "u3",
                TenantId = "public",
                UserDataInJWT = new Dictionary<string, object>
                {
                    ["st-role"] = new[] { "admin" }
                }
            });

        var recipe = new SessionRecipe(_coreMock.Object);
        var claim = PrimitiveArrayClaim.Create("st-role", async (_, _, _) => new[] { "admin" }, TimeSpan.FromMinutes(5));

        var value = await recipe.GetClaimValueAsync<string[]>("sh3", claim);

        Assert.NotNull(value);
        Assert.Single(value);
        Assert.Equal("admin", value[0]);
    }

    [Fact]
    public async Task RemoveClaimAsync_RemovesClaimAndMetadata()
    {
        _coreMock.Setup(c => c.GetSessionInformationAsync("sh4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionInfo
            {
                Handle = "sh4",
                UserId = "u4",
                TenantId = "public",
                UserDataInJWT = new Dictionary<string, object>
                {
                    ["st-role"] = new[] { "admin" },
                    ["_st-role"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }
            });

        _coreMock.Setup(c => c.UpdateJwtDataAsync(It.IsAny<UpdateJwtDataRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatusResponse { Status = "OK" });

        var recipe = new SessionRecipe(_coreMock.Object);
        var claim = PrimitiveArrayClaim.Create("st-role", async (_, _, _) => new[] { "admin" }, TimeSpan.FromMinutes(5));

        await recipe.RemoveClaimAsync("sh4", claim);

        _coreMock.Verify(c => c.UpdateJwtDataAsync(
            It.Is<UpdateJwtDataRequest>(r =>
                !r.UserDataInJWT.ContainsKey("st-role") &&
                !r.UserDataInJWT.ContainsKey("_st-role")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
