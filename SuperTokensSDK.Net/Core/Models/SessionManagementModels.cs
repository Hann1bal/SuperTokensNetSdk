using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

public sealed class SessionInfo
{
    [JsonPropertyName("sessionHandle")]
    public string Handle { get; set; } = "";

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("recipeUserId")]
    public string RecipeUserId { get; set; } = "";

    [JsonPropertyName("userDataInJWT")]
    public Dictionary<string, object> UserDataInJWT { get; set; } = new();

    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = Constants.DefaultTenantId;

    [JsonPropertyName("sessionDataInDatabase")]
    public Dictionary<string, object> SessionDataInDatabase { get; set; } = new();

    [JsonPropertyName("timeCreated")]
    public long TimeCreated { get; set; }

    [JsonPropertyName("expiry")]
    public long Expiry { get; set; }
}

public sealed class GetAllSessionHandlesResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("sessionHandles")]
    public List<string> SessionHandles { get; set; } = new();
}

public sealed class SessionInformationResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("session")]
    public SessionInfo? Session { get; set; }
}

public sealed class RevokeMultipleSessionsRequest
{
    [JsonPropertyName("sessionHandles")]
    public List<string> SessionHandles { get; set; } = new();
}

public sealed class RevokeMultipleSessionsResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("sessionHandlesRevoked")]
    public List<string> SessionHandlesRevoked { get; set; } = new();
}

public sealed class RevokeAllSessionsRequest
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("revokeAcrossAllTenants")]
    public bool RevokeAcrossAllTenants { get; set; }
}

public sealed class RevokeAllSessionsResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("sessionHandlesRevoked")]
    public List<string> SessionHandlesRevoked { get; set; } = new();
}
