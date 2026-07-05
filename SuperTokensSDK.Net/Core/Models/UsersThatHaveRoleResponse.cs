using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Response containing users that have a specific role.
/// </summary>
public class UsersThatHaveRoleResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("users")]
    public List<string> Users { get; set; } = [];

    [JsonPropertyName("nextPaginationToken")]
    public string? NextPaginationToken { get; set; }
}
