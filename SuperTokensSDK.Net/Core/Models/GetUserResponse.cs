using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Response containing a single user from SuperTokens Core.
/// </summary>
public class GetUserResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("user")]
    public UserResponse? User { get; set; }
}
