using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Request body for verifying an access token via CDI.
/// </summary>
public class VerifySessionRequest
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("antiCsrfToken")]
    public string? AntiCsrfToken { get; set; }

    [JsonPropertyName("doAntiCsrfCheck")]
    public bool DoAntiCsrfCheck { get; set; } = true;

    [JsonPropertyName("checkDatabase")]
    public bool CheckDatabase { get; set; } = false;
}
