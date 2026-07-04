using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// SuperTokens session structure returned by Core.
/// </summary>
public class SessionStruct
{
    [JsonPropertyName("handle")]
    public string Handle { get; set; } = "";

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("userDataInJWT")]
    public Dictionary<string, object> UserDataInJWT { get; set; } = new();

    [JsonPropertyName("expiryTime")]
    public long ExpiryTime { get; set; }

    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = Constants.DefaultTenantId;
}

/// <summary>
/// Token information returned by Core.
/// </summary>
public class TokenInfo
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = "";

    [JsonPropertyName("expiry")]
    public long Expiry { get; set; }

    [JsonPropertyName("createdTime")]
    public long CreatedTime { get; set; }
}

/// <summary>
/// Response for creating a new session or refreshing a session.
/// </summary>
public class CreateOrRefreshAPIResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("session")]
    public SessionStruct Session { get; set; } = new();

    [JsonPropertyName("accessToken")]
    public TokenInfo? AccessToken { get; set; }

    [JsonPropertyName("refreshToken")]
    public TokenInfo? RefreshToken { get; set; }

    [JsonPropertyName("antiCsrfToken")]
    public string? AntiCsrfToken { get; set; }
}

/// <summary>
/// Response for verifying a session.
/// </summary>
public class GetSessionResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("session")]
    public SessionStruct? Session { get; set; }

    [JsonPropertyName("accessToken")]
    public TokenInfo? AccessToken { get; set; }
}
