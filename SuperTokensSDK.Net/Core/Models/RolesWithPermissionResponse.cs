using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Response containing roles that have a specific permission.
/// </summary>
public class RolesWithPermissionResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = [];
}
