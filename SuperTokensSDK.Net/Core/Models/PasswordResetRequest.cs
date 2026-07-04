using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Request body for resetting a user password via CDI.
/// </summary>
public class PasswordResetRequest
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; } = "";
}
