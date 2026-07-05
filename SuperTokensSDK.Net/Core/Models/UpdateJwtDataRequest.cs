using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Request body for updating the access token payload of a session.
/// </summary>
public class UpdateJwtDataRequest
{
    [JsonPropertyName("sessionHandle")]
    public string SessionHandle { get; set; } = "";

    [JsonPropertyName("userDataInJWT")]
    public Dictionary<string, object?> UserDataInJWT { get; set; } = new();
}
