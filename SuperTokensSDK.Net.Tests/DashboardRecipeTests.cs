using Moq;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;
using SuperTokensSDK.Net.Recipes.Dashboard;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class DashboardRecipeTests
{
    private readonly Mock<ICoreApiClient> _coreMock = new();

    [Fact]
    public async Task SignInAsync_CallsCore_AndReturnsSession()
    {
        _coreMock.Setup(c => c.DashboardSignInAsync("key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardSignInResponse { Status = "OK", Session = "s1" });

        var recipe = new DashboardRecipe(_coreMock.Object);
        var session = await recipe.SignInAsync("key");

        Assert.Equal("s1", session);
        _coreMock.Verify(c => c.DashboardSignInAsync("key", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SignInAsync_ReturnsNull_WhenStatusNotOk()
    {
        _coreMock.Setup(c => c.DashboardSignInAsync("key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardSignInResponse { Status = "UNAUTHORISED" });

        var recipe = new DashboardRecipe(_coreMock.Object);
        var session = await recipe.SignInAsync("key");

        Assert.Null(session);
    }

    [Fact]
    public async Task SignOutAsync_CallsCore_AndReturnsTrueOnOk()
    {
        _coreMock.Setup(c => c.DashboardSignOutAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardSignOutResponse { Status = "OK" });

        var recipe = new DashboardRecipe(_coreMock.Object);
        var result = await recipe.SignOutAsync();

        Assert.True(result);
        _coreMock.Verify(c => c.DashboardSignOutAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UsersGetAsync_CallsCore_AndReturnsUsersWithToken()
    {
        var users = new List<DashboardUser>
        {
            new()
            {
                RecipeId = "emailpassword",
                User = new DashboardUserData { Id = "u1", Email = "a@b.com", TimeJoined = 1 },
                TenantIds = ["public"]
            }
        };
        _coreMock.Setup(c => c.DashboardGetUsersAsync(10, "token", "DESC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardUsersResponse { Status = "OK", Users = users, NextPaginationToken = "next" });

        var recipe = new DashboardRecipe(_coreMock.Object);
        var (resultUsers, nextToken) = await recipe.UsersGetAsync(10, "token");

        Assert.Single(resultUsers);
        Assert.Equal("u1", resultUsers[0].User.Id);
        Assert.Equal("next", nextToken);
    }

    [Fact]
    public async Task UsersCountGetAsync_CallsCore_AndReturnsCount()
    {
        _coreMock.Setup(c => c.DashboardGetUsersCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardUsersCountResponse { Status = "OK", Count = 7 });

        var recipe = new DashboardRecipe(_coreMock.Object);
        var count = await recipe.UsersCountGetAsync();

        Assert.Equal(7, count);
    }

    [Fact]
    public async Task TenantsListAsync_CallsCore_AndReturnsTenants()
    {
        var tenants = new List<DashboardTenantInfo> { new() { TenantId = "public" }, new() { TenantId = "t1" } };
        _coreMock.Setup(c => c.DashboardListTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardTenantsListResponse { Status = "OK", Tenants = tenants });

        var recipe = new DashboardRecipe(_coreMock.Object);
        var result = await recipe.TenantsListAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task UserDetailsGetAsync_CallsCore_AndReturnsUser()
    {
        var user = new Dictionary<string, object> { ["id"] = "u1" };
        _coreMock.Setup(c => c.DashboardGetUserDetailsAsync("u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardUserDetailsResponse { Status = "OK", User = user });

        var recipe = new DashboardRecipe(_coreMock.Object);
        var result = await recipe.UserDetailsGetAsync("u1");

        Assert.NotNull(result);
        Assert.Equal("u1", result!["id"]);
    }

    [Fact]
    public async Task UserRemoveAsync_CallsCore_AndReturnsTrueOnOk()
    {
        _coreMock.Setup(c => c.DashboardDeleteUserAsync("u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardSignOutResponse { Status = "OK" });

        var recipe = new DashboardRecipe(_coreMock.Object);
        var result = await recipe.UserRemoveAsync("u1");

        Assert.True(result);
    }

    [Fact]
    public async Task UserEmailVerifyAsync_CallsCore_AndReturnsTrueOnOk()
    {
        _coreMock.Setup(c => c.DashboardVerifyUserEmailAsync("u1", "a@b.com", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardSignOutResponse { Status = "OK" });

        var recipe = new DashboardRecipe(_coreMock.Object);
        var result = await recipe.UserEmailVerifyAsync("u1", "a@b.com", true);

        Assert.True(result);
    }

    [Fact]
    public async Task UserPasswordUpdateAsync_CallsCore_AndReturnsTrueOnOk()
    {
        _coreMock.Setup(c => c.DashboardUpdateUserPasswordAsync("u1", "newpass", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardSignOutResponse { Status = "OK" });

        var recipe = new DashboardRecipe(_coreMock.Object);
        var result = await recipe.UserPasswordUpdateAsync("u1", "newpass");

        Assert.True(result);
    }

    [Fact]
    public async Task UserMetadataUpdateAsync_CallsCore_AndReturnsTrueOnOk()
    {
        var metadata = new Dictionary<string, object> { ["key"] = "value" };
        _coreMock.Setup(c => c.DashboardUpdateUserMetadataAsync("u1", metadata, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardSignOutResponse { Status = "OK" });

        var recipe = new DashboardRecipe(_coreMock.Object);
        var result = await recipe.UserMetadataUpdateAsync("u1", metadata);

        Assert.True(result);
    }

    [Fact]
    public async Task UserSessionsGetAsync_CallsCore_AndReturnsSessions()
    {
        var sessions = new List<DashboardSessionInfo>
        {
            new() { SessionHandle = "h1", TimeCreated = 1, TimeExpires = 2 }
        };
        _coreMock.Setup(c => c.DashboardGetUserSessionsAsync("u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardUserSessionsResponse { Status = "OK", Sessions = sessions });

        var recipe = new DashboardRecipe(_coreMock.Object);
        var result = await recipe.UserSessionsGetAsync("u1");

        Assert.Single(result);
        Assert.Equal("h1", result[0].SessionHandle);
    }

    [Fact]
    public async Task SearchTagsAsync_CallsCore_AndReturnsTags()
    {
        _coreMock.Setup(c => c.DashboardSearchTagsAsync("q", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardSearchTagsResponse { Status = "OK", Tags = ["t1", "t2"] });

        var recipe = new DashboardRecipe(_coreMock.Object);
        var result = await recipe.SearchTagsAsync("q");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task AnalyticsGetAsync_CallsCore_AndReturnsData()
    {
        var data = new Dictionary<string, object> { ["mau"] = 100 };
        _coreMock.Setup(c => c.DashboardGetAnalyticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardAnalyticsResponse { Status = "OK", Data = data });

        var recipe = new DashboardRecipe(_coreMock.Object);
        var result = await recipe.AnalyticsGetAsync();

        Assert.NotNull(result);
        Assert.Equal(100, result!["mau"]);
    }

    [Fact]
    public void Constructor_NullCoreApiClient_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DashboardRecipe(null!));
    }
}
