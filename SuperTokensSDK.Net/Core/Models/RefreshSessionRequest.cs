using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Request body for refreshing a session via CDI.
/// </summary>
public class RefreshSessionRequest
{
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = "";

    [JsonPropertyName("antiCsrfToken")]
    public string? AntiCsrfToken { get; set; }

    [JsonPropertyName("enableAntiCsrf")]
    public bool EnableAntiCsrf { get; set; } = true;
}
