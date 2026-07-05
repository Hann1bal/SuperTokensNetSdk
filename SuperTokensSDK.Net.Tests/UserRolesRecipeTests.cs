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
    public async Task GetUsersThatHaveRoleAsync_CallsCore_AndReturnsUsers()
    {
        _coreMock.Setup(c => c.GetUsersThatHaveRoleAsync("admin", "public", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsersThatHaveRoleResponse { Status = "OK", Users = ["u1", "u2"], NextPaginationToken = "token" });

        var recipe = new UserRolesRecipe(_coreMock.Object);
        var users = await recipe.GetUsersThatHaveRoleAsync("admin");

        Assert.Equal(2, users.Count);
        Assert.Equal("u1", users[0]);
        _coreMock.Verify(c => c.GetUsersThatHaveRoleAsync("admin", "public", null, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateNewRoleOrAddPermissionsAsync_CallsCore_AndReturnsCreatedNewRole()
    {
        _coreMock.Setup(c => c.CreateNewRoleOrAddPermissionsAsync(It.IsAny<UserRolesCreateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRolesCreateResponse { Status = "OK", CreatedNewRole = true });

        var recipe = new UserRolesRecipe(_coreMock.Object);
        var created = await recipe.CreateNewRoleOrAddPermissionsAsync("admin", ["read", "write"]);

        Assert.True(created);
        _coreMock.Verify(c => c.CreateNewRoleOrAddPermissionsAsync(
            It.Is<UserRolesCreateRequest>(r => r.Role == "admin" && r.Permissions.SequenceEqual(new[] { "read", "write" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPermissionsForRoleAsync_CallsCore_AndReturnsPermissions()
    {
        _coreMock.Setup(c => c.GetPermissionsForRoleAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PermissionsForRoleResponse { Status = "OK", Permissions = ["read", "write"] });

        var recipe = new UserRolesRecipe(_coreMock.Object);
        var permissions = await recipe.GetPermissionsForRoleAsync("admin");

        Assert.Equal(2, permissions.Count);
        Assert.Equal("write", permissions[1]);
        _coreMock.Verify(c => c.GetPermissionsForRoleAsync("admin", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemovePermissionsFromRoleAsync_CallsCore_WithCorrectRequest()
    {
        _coreMock.Setup(c => c.RemovePermissionsFromRoleAsync(It.IsAny<RemovePermissionsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatusResponse { Status = "OK" });

        var recipe = new UserRolesRecipe(_coreMock.Object);
        await recipe.RemovePermissionsFromRoleAsync("admin", ["write"]);

        _coreMock.Verify(c => c.RemovePermissionsFromRoleAsync(
            It.Is<RemovePermissionsRequest>(r => r.Role == "admin" && r.Permissions.SequenceEqual(new[] { "write" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRolesThatHavePermissionAsync_CallsCore_AndReturnsRoles()
    {
        _coreMock.Setup(c => c.GetRolesThatHavePermissionAsync("write", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RolesWithPermissionResponse { Status = "OK", Roles = ["admin", "editor"] });

        var recipe = new UserRolesRecipe(_coreMock.Object);
        var roles = await recipe.GetRolesThatHavePermissionAsync("write");

        Assert.Equal(2, roles.Count);
        _coreMock.Verify(c => c.GetRolesThatHavePermissionAsync("write", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteRoleAsync_CallsCore_AndReturnsDidRoleExist()
    {
        _coreMock.Setup(c => c.DeleteRoleAsync(It.IsAny<DeleteRoleRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteRoleResponse { Status = "OK", DidRoleExist = true });

        var recipe = new UserRolesRecipe(_coreMock.Object);
        var existed = await recipe.DeleteRoleAsync("legacy");

        Assert.True(existed);
        _coreMock.Verify(c => c.DeleteRoleAsync(
            It.Is<DeleteRoleRequest>(r => r.Role == "legacy"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllRolesAsync_CallsCore_AndReturnsRoles()
    {
        _coreMock.Setup(c => c.GetAllRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AllRolesResponse { Status = "OK", Roles = ["admin", "user"] });

        var recipe = new UserRolesRecipe(_coreMock.Object);
        var roles = await recipe.GetAllRolesAsync();

        Assert.Equal(2, roles.Count);
        _coreMock.Verify(c => c.GetAllRolesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_NullCoreApiClient_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new UserRolesRecipe(null!));
    }
}
