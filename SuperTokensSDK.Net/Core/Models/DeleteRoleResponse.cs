using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Response for deleting a role.
/// </summary>
public class DeleteRoleResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("didRoleExist")]
    public bool DidRoleExist { get; set; }
}
