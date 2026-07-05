using Moq;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;
using SuperTokensSDK.Net.Recipes.UserRoles;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class UserRolesRecipeTests
{
    private readonly Mock<ICoreApiClient> _coreMock = new();

    [Fact]
    public async Task AddRoleAsync_CallsCore_WithSingleRole()
    {
        _coreMock.Setup(c => c.AddUserRolesAsync(It.IsAny<UserRolesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatusResponse { Status = "OK" });

        var recipe = new UserRolesRecipe(_coreMock.Object);
        await recipe.AddRoleAsync("u1", "admin");

        _coreMock.Verify(c => c.AddUserRolesAsync(
            It.Is<UserRolesRequest>(r => r.UserId == "u1" && r.Roles.Count == 1 && r.Roles[0] == "admin"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddRolesAsync_CallsCore_WithFullList()
    {
        _coreMock.Setup(c => c.AddUserRolesAsync(It.IsAny<UserRolesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatusResponse { Status = "OK" });

        var recipe = new UserRolesRecipe(_coreMock.Object);
        await recipe.AddRolesAsync("u2", new[] { "admin", "editor" });

        _coreMock.Verify(c => c.AddUserRolesAsync(
            It.Is<UserRolesRequest>(r => r.UserId == "u2" && r.Roles.SequenceEqual(new[] { "admin", "editor" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRolesAsync_CallsCore_AndReturnsReadOnlyList()
    {
        _coreMock.Setup(c => c.GetUserRolesAsync("u3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRolesResponse { Status = "OK", Roles = new List<string> { "admin", "viewer" } });

        var recipe = new UserRolesRecipe(_coreMock.Object);
        var roles = await recipe.GetRolesAsync("u3");

        Assert.Equal(2, roles.Count);
        Assert.Equal("admin", roles[0]);
        Assert.Equal("viewer", roles[1]);
        _coreMock.Verify(c => c.GetUserRolesAsync("u3", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveRoleAsync_CallsCore_WithSingleRole()
    {
        _coreMock.Setup(c => c.RemoveUserRolesAsync(It.IsAny<UserRolesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatusResponse { Status = "OK" });

        var recipe = new UserRolesRecipe(_coreMock.Object);
        await recipe.RemoveRoleAsync("u4", "editor");

        _coreMock.Verify(c => c.RemoveUserRolesAsync(
            It.Is<UserRolesRequest>(r => r.UserId == "u4" && r.Roles.Count == 1 && r.Roles[0] == "editor"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveRolesAsync_CallsCore_WithFullList()
    {
        _coreMock.Setup(c => c.RemoveUserRolesAsync(It.IsAny<UserRolesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatusResponse { Status = "OK" });

        var recipe = new UserRolesRecipe(_coreMock.Object);
        await recipe.RemoveRolesAsync("u5", new[] { "admin", "editor" });

        _coreMock.Verify(c => c.RemoveUserRolesAsync(
            It.Is<UserRolesRequest>(r => r.UserId == "u5" && r.Roles.SequenceEqual(new[] { "admin", "editor" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DoesRoleExistAsync_CallsCore_AndReturnsBool()
    {
        _coreMock.Setup(c => c.DoesRoleExistAsync("u6", "admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleExistsResponse { Status = "OK", DoesRoleExist = true });

        var recipe = new UserRolesRecipe(_coreMock.Object);
        var exists = await recipe.DoesRoleExistAsync("u6", "admin");

        Assert.True(exists);
        _coreMock.Verify(c => c.DoesRoleExistAsync("u6", "admin", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_NullCoreApiClient_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new UserRolesRecipe(null!));
    }
}
