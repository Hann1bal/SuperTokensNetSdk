using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Request body for creating a new session via CDI 5.0.
/// </summary>
public class CreateSessionRequest
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("userDataInJWT")]
    public Dictionary<string, object>? UserDataInJWT { get; set; }

    [JsonPropertyName("userDataInDatabase")]
    public Dictionary<string, object>? UserDataInDatabase { get; set; }

    [JsonPropertyName("enableAntiCsrf")]
    public bool EnableAntiCsrf { get; set; } = true;
}
