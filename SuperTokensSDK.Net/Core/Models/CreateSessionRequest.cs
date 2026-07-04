using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Request body for creating a new session via CDI.
/// </summary>
public class CreateSessionRequest
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("accessTokenPayload")]
    public Dictionary<string, object>? AccessTokenPayload { get; set; }

    [JsonPropertyName("sessionData")]
    public Dictionary<string, object>? SessionData { get; set; }
}
