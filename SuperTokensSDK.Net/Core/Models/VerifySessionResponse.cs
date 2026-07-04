using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Response from verifying an access token via CDI.
/// </summary>
public class VerifySessionResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("session")]
    public SessionData? Session { get; set; }

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    [JsonPropertyName("userDataInJWT")]
    public Dictionary<string, object>? UserDataInJwt { get; set; }
}
