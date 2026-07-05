using System.Security.Claims;

namespace SuperTokensSDK.Net.Recipes.Session;

/// <summary>
/// Wraps session creation/verification/refresh/revocation results.
/// Provides helpers to read user data and build claims identities.
/// </summary>
public class SessionContainer
{
    /// <summary>Opaque handle identifying this session in SuperTokens Core.</summary>
    public string SessionHandle { get; set; }
    /// <summary>SuperTokens user id associated with the session.</summary>
    public string UserId { get; set; }
    /// <summary>Current access token (JWT) when available.</summary>
    public string? AccessToken { get; set; }
    /// <summary>Current refresh token when available.</summary>
    public string? RefreshToken { get; set; }
    /// <summary>Anti-CSRF token bound to this session, if enabled.</summary>
    public string? AntiCsrfToken { get; set; }
    /// <summary>UTC expiry of the current access token.</summary>
    public DateTime AccessTokenExpiry { get; set; }
    /// <summary>UTC expiry of the current refresh token.</summary>
    public DateTime RefreshTokenExpiry { get; set; }
    /// <summary>Custom JWT payload (claims) attached to the session.</summary>
    public Dictionary<string, object> UserDataInJwt { get; set; }

    public SessionContainer(string sessionHandle, string userId, Dictionary<string, object>? userDataInJwt = null)
    {
        SessionHandle = sessionHandle;
        UserId = userId;
        UserDataInJwt = userDataInJwt ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Builds a ClaimsPrincipal from the session user id and JWT payload.
    /// </summary>
    public ClaimsPrincipal GetClaimsPrincipal()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, UserId), new Claim("sub", UserId)], "SuperTokens");

        foreach (var kvp in UserDataInJwt)
        {
            if (kvp.Key.Equals("roles", StringComparison.OrdinalIgnoreCase) &&
                kvp.Value is System.Text.Json.JsonElement rolesElement &&
                rolesElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var role in rolesElement.EnumerateArray())
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role.GetString() ?? string.Empty));
                }
            }
            else
            {
                identity.AddClaim(new Claim(kvp.Key, kvp.Value?.ToString() ?? string.Empty));
            }
        }

        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Returns a typed value from the JWT payload, or the default value.
    /// </summary>
    public T? GetClaim<T>(string key, T? defaultValue = default)
    {
        if (UserDataInJwt.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }

        if (value is System.Text.Json.JsonElement element)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(element.GetRawText());
        }

        return defaultValue;
    }
}
