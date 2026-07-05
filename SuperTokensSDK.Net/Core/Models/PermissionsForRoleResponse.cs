using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Response containing permissions for a role.
/// </summary>
public class PermissionsForRoleResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("permissions")]
    public List<string> Permissions { get; set; } = [];
}
