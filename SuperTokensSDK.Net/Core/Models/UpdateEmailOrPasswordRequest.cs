using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Request body for updating a user's email or password.
/// </summary>
public class UpdateEmailOrPasswordRequest
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }
}
