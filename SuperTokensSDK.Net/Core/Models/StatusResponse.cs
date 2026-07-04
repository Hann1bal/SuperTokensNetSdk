using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Generic status response from SuperTokens Core.
/// </summary>
public class StatusResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
