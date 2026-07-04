using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Response from refreshing a session via CDI.
/// </summary>
public class RefreshSessionCoreResponse
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
