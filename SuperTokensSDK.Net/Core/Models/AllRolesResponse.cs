using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Response containing all roles.
/// </summary>
public class AllRolesResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = [];
}
