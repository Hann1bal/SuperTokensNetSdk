using Moq;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;
using SuperTokensSDK.Net.Recipes.UserManagement;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class UserManagementRecipeTests
{
    private readonly Mock<ICoreApiClient> _coreMock = new();

    [Fact]
    public async Task GetUsersAsync_CallsCore_AndReturnsUsersWithToken()
    {
        var users = new List<UserListItem>
        {
            new()
            {
                RecipeId = "emailpassword",
                User = new UserResponse { Id = "u1", Email = "a@b.com", TimeJoined = 1 },
                TenantIds = ["public"]
            }
        };
        _coreMock.Setup(c => c.GetUsersAsync(50, "token", "DESC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserListResponse { Status = "OK", Users = users, NextPaginationToken = "next" });

        var recipe = new UserManagementRecipe(_coreMock.Object);
        var (resultUsers, nextToken) = await recipe.GetUsersAsync(50, "token");

        Assert.Single(resultUsers);
        Assert.Equal("u1", resultUsers[0].User.Id);
        Assert.Equal("next", nextToken);
        _coreMock.Verify(c => c.GetUsersAsync(50, "token", "DESC", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserCountAsync_CallsCore_AndReturnsCount()
    {
        _coreMock.Setup(c => c.GetUserCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserCountResponse { Status = "OK", Count = 42 });

        var recipe = new UserManagementRecipe(_coreMock.Object);
        var count = await recipe.GetUserCountAsync();

        Assert.Equal(42, count);
        _coreMock.Verify(c => c.GetUserCountAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_CallsCore_AndReturnsTrueOnOk()
    {
        _coreMock.Setup(c => c.DeleteUserAsync(It.IsAny<DeleteUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatusResponse { Status = "OK" });

        var recipe = new UserManagementRecipe(_coreMock.Object);
        var deleted = await recipe.DeleteUserAsync("u2");

        Assert.True(deleted);
        _coreMock.Verify(c => c.DeleteUserAsync(
            It.Is<DeleteUserRequest>(r => r.UserId == "u2"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsFalse_WhenStatusNotOk()
    {
        _coreMock.Setup(c => c.DeleteUserAsync(It.IsAny<DeleteUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatusResponse { Status = "UNKNOWN_USER_ID_ERROR" });

        var recipe = new UserManagementRecipe(_coreMock.Object);
        var deleted = await recipe.DeleteUserAsync("u-missing");

        Assert.False(deleted);
    }

    [Fact]
    public void Constructor_NullCoreApiClient_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new UserManagementRecipe(null!));
    }
}
