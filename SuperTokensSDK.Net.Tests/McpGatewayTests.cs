using Moq;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;
using SuperTokensSDK.Net.Mcp;
using SuperTokensSDK.Net.Recipes.EmailPassword;
using SuperTokensSDK.Net.Recipes.Session;
using SuperTokensSDK.Net.Recipes.UserRoles;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class McpGatewayTests
{
    private readonly Mock<ICoreApiClient> _emailCoreMock = new();
    private readonly Mock<ICoreApiClient> _sessionCoreMock = new();
    private readonly Mock<ICoreApiClient> _rolesCoreMock = new();
    private readonly McpTools _tools;
    private readonly McpGateway _gateway;

    public McpGatewayTests()
    {
        _tools = new McpTools(
            new EmailPasswordRecipe(_emailCoreMock.Object),
            new SessionRecipe(_sessionCoreMock.Object),
            new UserRolesRecipe(_rolesCoreMock.Object));
        _gateway = new McpGateway(_tools);
    }

    [Fact]
    public void GetToolDefinitions_ReturnsFiveTools_WithCorrectNames()
    {
        var tools = _gateway.GetToolDefinitions();

        Assert.Equal(5, tools.Count);
        Assert.Equal(new[] { "create_user", "verify_session", "get_user_roles", "assign_role", "revoke_session" }, tools.Select(t => t.Name));
    }

    [Fact]
    public async Task ExecuteToolAsync_DispatchesCreateUserCaseInsensitive()
    {
        _emailCoreMock.Setup(c => c.SignUpAsync(It.Is<SignUpRequest>(r => r.Email == "a@b.com" && r.Password == "p"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignUpResponse { Status = "OK", User = new UserResponse { Id = "u1" } });

        var result = await _gateway.ExecuteToolAsync(new McpToolRequest { Name = "CREATE_USER", Arguments = new Dictionary<string, object> { ["email"] = "a@b.com", ["password"] = "p" } });

        Assert.False(result.IsError);
        Assert.Contains("u1", result.Content[0].Text);
    }

    [Fact]
    public async Task ExecuteToolAsync_UnknownTool_ReturnsErrorResult()
    {
        var result = await _gateway.ExecuteToolAsync(new McpToolRequest { Name = "unknown_tool" });

        Assert.True(result.IsError);
        Assert.Contains("Unknown tool", result.Content[0].Text);
    }

    [Fact]
    public async Task ExecuteToolAsync_ToolThrows_ReturnsErrorResultWithMessage()
    {
        _emailCoreMock.Setup(c => c.SignUpAsync(It.Is<SignUpRequest>(r => r.Email == "a@b.com" && r.Password == "p"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _gateway.ExecuteToolAsync(new McpToolRequest { Name = "create_user", Arguments = new Dictionary<string, object> { ["email"] = "a@b.com", ["password"] = "p" } });

        Assert.True(result.IsError);
        Assert.Contains("boom", result.Content[0].Text);
    }

    [Fact]
    public void ToJson_SerializesCamelCase()
    {
        var json = McpGateway.ToJson(new { SomeProperty = "value", Another = 1 });
        Assert.Contains("\"someProperty\":\"value\"", json);
        Assert.Contains("\"another\":1", json);
    }

    [Fact]
    public void Constructor_NullTools_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new McpGateway(null!));
    }
}
