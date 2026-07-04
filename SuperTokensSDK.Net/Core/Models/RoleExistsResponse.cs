using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Response for checking if a role exists.
/// </summary>
public class RoleExistsResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("doesRoleExist")]
    public bool DoesRoleExist { get; set; }
}
