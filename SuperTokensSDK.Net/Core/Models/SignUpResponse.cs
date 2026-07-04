using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Response from signing up or signing in a user via CDI.
/// </summary>
public class SignUpResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("user")]
    public UserResponse? User { get; set; }
}
