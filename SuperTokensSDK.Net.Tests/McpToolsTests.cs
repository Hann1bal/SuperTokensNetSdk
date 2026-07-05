using Moq;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;
using SuperTokensSDK.Net.Mcp;
using SuperTokensSDK.Net.Recipes.EmailPassword;
using SuperTokensSDK.Net.Recipes.Session;
using SuperTokensSDK.Net.Recipes.UserRoles;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class McpToolsTests
{
    private readonly Mock<ICoreApiClient> _emailCoreMock = new();
    private readonly Mock<ICoreApiClient> _sessionCoreMock = new();
    private readonly Mock<ICoreApiClient> _rolesCoreMock = new();
    private readonly EmailPasswordRecipe _emailRecipe;
    private readonly SessionRecipe _sessionRecipe;
    private readonly UserRolesRecipe _rolesRecipe;
    private readonly McpTools _tools;

    public McpToolsTests()
    {
        _emailRecipe = new EmailPasswordRecipe(_emailCoreMock.Object);
        _sessionRecipe = new SessionRecipe(_sessionCoreMock.Object);
        _rolesRecipe = new UserRolesRecipe(_rolesCoreMock.Object);
        _tools = new McpTools(_emailRecipe, _sessionRecipe, _rolesRecipe);
    }

    [Fact]
    public async Task CreateUserAsync_CallsSignUp_AndAssignsRole()
    {
        _emailCoreMock.Setup(c => c.SignUpAsync(It.Is<SignUpRequest>(r => r.Email == "a@b.com" && r.Password == "p"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignUpResponse { Status = "OK", User = new UserResponse { Id = "u1", Email = "a@b.com" } });
        _rolesCoreMock.Setup(c => c.AddUserRolesAsync(It.Is<UserRolesRequest>(r => r.UserId == "u1" && r.Roles.Count == 1 && r.Roles[0] == "admin"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatusResponse { Status = "OK" });

        var result = await _tools.CreateUserAsync(new Dictionary<string, object> { ["email"] = "a@b.com", ["password"] = "p", ["role"] = "admin" }, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Contains("u1", result.Content[0].Text);
    }

    [Fact]
    public async Task CreateUserAsync_MissingEmailOrPassword_ReturnsError()
    {
        _emailCoreMock.Setup(c => c.SignUpAsync(It.Is<SignUpRequest>(r => r.Email == "" && r.Password == "p"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignUpResponse { Status = "OK", User = null });

        var result = await _tools.CreateUserAsync(new Dictionary<string, object> { ["email"] = "", ["password"] = "p" }, CancellationToken.None);

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task VerifySessionAsync_CallsSessionRecipe()
    {
        var jwt = TestJwtHelper.CreateJwt(new Dictionary<string, object>
        {
            ["sub"] = "u-verify",
            ["sessionHandle"] = "sh-verify"
        });
        var container = new SessionContainer("sh-verify", "u-verify");
        _sessionCoreMock.Setup(c => c.VerifySessionAsync(It.Is<VerifySessionRequest>(r => r.AccessToken == jwt), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSessionResponse
            {
                Status = "OK",
                Session = new SessionStruct { Handle = "sh-verify", UserId = "u-verify", UserDataInJWT = new() }
            });

        var result = await _tools.VerifySessionAsync(new Dictionary<string, object> { ["token"] = jwt }, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Contains("u-verify", result.Content[0].Text);
    }

    [Fact]
    public async Task GetUserRolesAsync_CallsUserRolesRecipe()
    {
        _rolesCoreMock.Setup(c => c.GetUserRolesAsync("u2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRolesResponse { Status = "OK", Roles = new List<string> { "editor" } });

        var result = await _tools.GetUserRolesAsync(new Dictionary<string, object> { ["userId"] = "u2" }, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Contains("editor", result.Content[0].Text);
    }

    [Fact]
    public async Task AssignRoleAsync_CallsUserRolesRecipe()
    {
        _rolesCoreMock.Setup(c => c.AddUserRolesAsync(It.Is<UserRolesRequest>(r => r.UserId == "u3" && r.Roles[0] == "admin"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatusResponse { Status = "OK" });

        var result = await _tools.AssignRoleAsync(new Dictionary<string, object> { ["userId"] = "u3", ["role"] = "admin" }, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Contains("OK", result.Content[0].Text);
    }

    [Fact]
    public async Task RevokeSessionAsync_CallsSessionRecipe()
    {
        _sessionCoreMock.Setup(c => c.RevokeMultipleSessionsAsync(It.Is<List<string>>(l => l.Contains("sh-revoke")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "sh-revoke" });

        var result = await _tools.RevokeSessionAsync(new Dictionary<string, object> { ["sessionHandle"] = "sh-revoke" }, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Contains("OK", result.Content[0].Text);
    }

    [Fact]
    public void Constructor_NullEmailPassword_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new McpTools(null!, _sessionRecipe, _rolesRecipe));
    }

    [Fact]
    public void Constructor_NullSessionRecipe_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new McpTools(_emailRecipe, null!, _rolesRecipe));
    }

    [Fact]
    public void Constructor_NullUserRolesRecipe_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new McpTools(_emailRecipe, _sessionRecipe, null!));
    }
}
