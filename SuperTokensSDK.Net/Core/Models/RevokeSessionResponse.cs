using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Response from revoking a session via CDI.
/// </summary>
public class RevokeSessionResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
