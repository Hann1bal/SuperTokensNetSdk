using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Request body for creating a role or adding permissions to a role.
/// </summary>
public class UserRolesCreateRequest
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("permissions")]
    public List<string> Permissions { get; set; } = [];
}
