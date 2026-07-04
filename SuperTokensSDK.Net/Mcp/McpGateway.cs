using System.Text.Json;

namespace SuperTokensSDK.Net.Mcp;

/// <summary>
/// Gateway exposing SuperTokens operations as MCP tool definitions.
/// </summary>
public class McpGateway
{
    private readonly McpTools _tools;

    public McpGateway(McpTools tools)
    {
        _tools = tools ?? throw new ArgumentNullException(nameof(tools));
    }

    public IReadOnlyList<McpToolDefinition> GetToolDefinitions() => new List<McpToolDefinition>
    {
        new()
        {
            Name = "create_user",
            Description = "Create a new SuperTokens user with email, password, and optional role.",
            InputSchema = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["email"] = new Dictionary<string, string> { ["type"] = "string", ["description"] = "User email address" },
                    ["password"] = new Dictionary<string, string> { ["type"] = "string", ["description"] = "User password" },
                    ["role"] = new Dictionary<string, string> { ["type"] = "string", ["description"] = "Optional role (admin, staff, user, viewer)" }
                },
                ["required"] = new[] { "email", "password" }
            }
        },
        new()
        {
            Name = "verify_session",
            Description = "Verify a SuperTokens access token.",
            InputSchema = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["token"] = new Dictionary<string, string> { ["type"] = "string", ["description"] = "Access token to verify" }
                },
                ["required"] = new[] { "token" }
            }
        },
        new()
        {
            Name = "get_user_roles",
            Description = "Get roles assigned to a SuperTokens user.",
            InputSchema = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["userId"] = new Dictionary<string, string> { ["type"] = "string", ["description"] = "SuperTokens user ID" }
                },
                ["required"] = new[] { "userId" }
            }
        },
        new()
        {
            Name = "assign_role",
            Description = "Assign a role to a SuperTokens user.",
            InputSchema = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["userId"] = new Dictionary<string, string> { ["type"] = "string", ["description"] = "SuperTokens user ID" },
                    ["role"] = new Dictionary<string, string> { ["type"] = "string", ["description"] = "Role to assign" }
                },
                ["required"] = new[] { "userId", "role" }
            }
        },
        new()
        {
            Name = "revoke_session",
            Description = "Revoke a SuperTokens session by session handle.",
            InputSchema = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["sessionHandle"] = new Dictionary<string, string> { ["type"] = "string", ["description"] = "Session handle to revoke" }
                },
                ["required"] = new[] { "sessionHandle" }
            }
        }
    }.AsReadOnly();

    public async Task<McpToolResult> ExecuteToolAsync(McpToolRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = request.Name.ToLowerInvariant() switch
            {
                "create_user" => await _tools.CreateUserAsync(request.Arguments, cancellationToken),
                "verify_session" => await _tools.VerifySessionAsync(request.Arguments, cancellationToken),
                "get_user_roles" => await _tools.GetUserRolesAsync(request.Arguments, cancellationToken),
                "assign_role" => await _tools.AssignRoleAsync(request.Arguments, cancellationToken),
                "revoke_session" => await _tools.RevokeSessionAsync(request.Arguments, cancellationToken),
                _ => new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpToolContent> { new() { Text = $"Unknown tool: {request.Name}" } }
                }
            };

            return result;
        }
        catch (Exception ex)
        {
            return new McpToolResult
            {
                IsError = true,
                Content = new List<McpToolContent> { new() { Text = $"Tool execution failed: {ex.Message}" } }
            };
        }
    }

    public static string ToJson(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
}
