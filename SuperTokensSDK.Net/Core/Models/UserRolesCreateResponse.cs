using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Response for creating a role or adding permissions to a role.
/// </summary>
public class UserRolesCreateResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("createdNewRole")]
    public bool CreatedNewRole { get; set; }
}
