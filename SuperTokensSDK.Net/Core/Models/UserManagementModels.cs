using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

/// <summary>
/// Response containing a paginated list of users from SuperTokens Core.
/// </summary>
public class UserListResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("users")]
    public List<UserListItem> Users { get; set; } = [];

    [JsonPropertyName("nextPaginationToken")]
    public string? NextPaginationToken { get; set; }
}

/// <summary>
/// A single user item returned in the user list response.
/// </summary>
public class UserListItem
{
    [JsonPropertyName("recipeId")]
    public string RecipeId { get; set; } = "";

    [JsonPropertyName("user")]
    public UserResponse User { get; set; } = new();

    [JsonPropertyName("tenantIds")]
    public string[] TenantIds { get; set; } = [];
}

/// <summary>
/// Response containing the total user count.
/// </summary>
public class UserCountResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

/// <summary>
/// Request body for deleting a user.
/// </summary>
public class DeleteUserRequest
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("removeUserIdMappingInfo")]
    public bool? RemoveUserIdMappingInfo { get; set; }
}
