using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Request body for deleting a role.
/// </summary>
public class DeleteRoleRequest
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";
}
