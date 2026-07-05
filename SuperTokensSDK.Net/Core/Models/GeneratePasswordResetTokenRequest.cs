using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Request body for generating a password reset token.
/// </summary>
public class GeneratePasswordResetTokenRequest
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}
