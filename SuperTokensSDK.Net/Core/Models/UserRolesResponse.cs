using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Response containing roles for a user from SuperTokens Core.
/// </summary>
public class UserRolesResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = [];
}
