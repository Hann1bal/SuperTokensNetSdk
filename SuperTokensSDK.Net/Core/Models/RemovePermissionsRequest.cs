using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Request body for removing permissions from a role.
/// </summary>
public class RemovePermissionsRequest
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("permissions")]
    public List<string> Permissions { get; set; } = [];
}
