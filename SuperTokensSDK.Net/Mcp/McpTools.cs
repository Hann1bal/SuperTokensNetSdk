using System.Text.Json;
using SuperTokensSDK.Net.Recipes.EmailPassword;
using SuperTokensSDK.Net.Recipes.Session;
using SuperTokensSDK.Net.Recipes.UserRoles;

namespace SuperTokensSDK.Net.Mcp;

/// <summary>
/// Implementations of MCP tools backed by SuperTokens recipes.
/// </summary>
public class McpTools
{
    private readonly EmailPasswordRecipe _emailPassword;
    private readonly SessionRecipe _session;
    private readonly UserRolesRecipe _userRoles;

    public McpTools(EmailPasswordRecipe emailPassword, SessionRecipe session, UserRolesRecipe userRoles)
    {
        _emailPassword = emailPassword ?? throw new ArgumentNullException(nameof(emailPassword));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _userRoles = userRoles ?? throw new ArgumentNullException(nameof(userRoles));
    }

    public async Task<McpToolResult> CreateUserAsync(Dictionary<string, object>? arguments, CancellationToken cancellationToken)
    {
        var email = GetStringArgument(arguments, "email");
        var password = GetStringArgument(arguments, "password");
        var role = GetStringArgument(arguments, "role");

        var user = await _emailPassword.SignUpAsync(email, password, cancellationToken);
        if (user == null || string.IsNullOrWhiteSpace(user.Id))
        {
            return ErrorResult("Failed to create user.");
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            await _userRoles.AddRoleAsync(user.Id, role, cancellationToken);
        }

        return SuccessResult(new { userId = user.Id, email = user.Email, role });
    }

    public async Task<McpToolResult> VerifySessionAsync(Dictionary<string, object>? arguments, CancellationToken cancellationToken)
    {
        var token = GetStringArgument(arguments, "token");
        var session = await _session.VerifySessionAsync(token, cancellationToken: cancellationToken);
        return SuccessResult(new { userId = session.UserId, sessionHandle = session.SessionHandle, roles = session.UserDataInJwt.GetValueOrDefault("roles") });
    }

    public async Task<McpToolResult> GetUserRolesAsync(Dictionary<string, object>? arguments, CancellationToken cancellationToken)
    {
        var userId = GetStringArgument(arguments, "userId");
        var roles = await _userRoles.GetRolesAsync(userId, cancellationToken);
        return SuccessResult(new { userId, roles });
    }

    public async Task<McpToolResult> AssignRoleAsync(Dictionary<string, object>? arguments, CancellationToken cancellationToken)
    {
        var userId = GetStringArgument(arguments, "userId");
        var role = GetStringArgument(arguments, "role");
        await _userRoles.AddRoleAsync(userId, role, cancellationToken);
        return SuccessResult(new { userId, role, status = "OK" });
    }

    public async Task<McpToolResult> RevokeSessionAsync(Dictionary<string, object>? arguments, CancellationToken cancellationToken)
    {
        var sessionHandle = GetStringArgument(arguments, "sessionHandle");
        await _session.RevokeSessionAsync(sessionHandle, cancellationToken);
        return SuccessResult(new { sessionHandle, status = "OK" });
    }

    private static string GetStringArgument(Dictionary<string, object>? arguments, string key)
    {
        if (arguments == null || !arguments.TryGetValue(key, out var value) || value == null) return string.Empty;
        return value.ToString() ?? string.Empty;
    }

    private static McpToolResult SuccessResult(object data) => new()
    {
        Content = new List<McpToolContent> { new() { Text = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) } }
    };

    private static McpToolResult ErrorResult(string message) => new()
    {
        IsError = true,
        Content = new List<McpToolContent> { new() { Text = message } }
    };
}
