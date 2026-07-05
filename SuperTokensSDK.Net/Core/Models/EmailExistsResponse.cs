using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Response for checking if an email exists.
/// </summary>
public class EmailExistsResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("exists")]
    public bool Exists { get; set; }
}
