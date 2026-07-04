using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Response from creating a new session via CDI.
/// </summary>
public class CreateSessionResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("session")]
    public SessionData? Session { get; set; }

    [JsonPropertyName("accessToken")]
    public TokenInfo? AccessToken { get; set; }

    [JsonPropertyName("refreshToken")]
    public TokenInfo? RefreshToken { get; set; }

    [JsonPropertyName("frontToken")]
    public string? FrontToken { get; set; }
}

public class SessionData
{
    [JsonPropertyName("handle")]
    public string? Handle { get; set; }

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    [JsonPropertyName("userDataInJWT")]
    public Dictionary<string, object>? UserDataInJwt { get; set; }

    [JsonPropertyName("userDataInDatabase")]
    public Dictionary<string, object>? UserDataInDatabase { get; set; }
}

public class TokenInfo
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("expiry")]
    public long Expiry { get; set; }

    [JsonPropertyName("createdTime")]
    public long CreatedTime { get; set; }
}
