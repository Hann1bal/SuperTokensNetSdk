using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Response for generating a password reset token.
/// </summary>
public class GeneratePasswordResetTokenResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }
}
