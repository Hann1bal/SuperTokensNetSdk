using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Request body for revoking a session via CDI.
/// </summary>
public class RevokeSessionRequest
{
    [JsonPropertyName("sessionHandle")]
    public string SessionHandle { get; set; } = "";
}
