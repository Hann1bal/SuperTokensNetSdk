using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Request body for adding/removing roles from a user via CDI.
/// </summary>
public class UserRolesRequest
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = [];
}
