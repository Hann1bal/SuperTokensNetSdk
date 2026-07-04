using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Request body for updating user metadata via CDI.
/// </summary>
public class UserMetadataUpdateRequest
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("metadataUpdate")]
    public Dictionary<string, object>? MetadataUpdate { get; set; }
}
