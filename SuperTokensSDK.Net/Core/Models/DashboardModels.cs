using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core.Models;

public class DashboardSignInRequest
{
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = "";
}

public class DashboardSignInResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("session")]
    public string? Session { get; set; }
}

public class DashboardSignOutResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public class DashboardUsersResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("users")]
    public List<DashboardUser> Users { get; set; } = [];

    [JsonPropertyName("nextPaginationToken")]
    public string? NextPaginationToken { get; set; }
}

public class DashboardUser
{
    [JsonPropertyName("recipeId")]
    public string RecipeId { get; set; } = "";

    [JsonPropertyName("user")]
    public DashboardUserData User { get; set; } = new();

    [JsonPropertyName("tenantIds")]
    public List<string> TenantIds { get; set; } = [];
}

public class DashboardUserData
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("timeJoined")]
    public long TimeJoined { get; set; }

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("thirdParty")]
    public DashboardThirdParty? ThirdParty { get; set; }
}

public class DashboardThirdParty
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }
}

public class DashboardUsersCountResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class DashboardTenantsListResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("tenants")]
    public List<DashboardTenantInfo> Tenants { get; set; } = [];
}

public class DashboardTenantInfo
{
    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = "";

    [JsonPropertyName("coreConfig")]
    public Dictionary<string, object>? CoreConfig { get; set; }
}

public class DashboardUserDetailsResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("user")]
    public Dictionary<string, object>? User { get; set; }
}

public class DashboardUserDetailsRequest
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = "";
}

public class DashboardUserEmailVerifyRequest
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("verified")]
    public bool Verified { get; set; }
}

public class DashboardUserPasswordRequest
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; } = "";
}

public class DashboardUserMetadataRequest
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class DashboardUserSessionsResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("sessions")]
    public List<DashboardSessionInfo> Sessions { get; set; } = [];
}

public class DashboardSessionInfo
{
    [JsonPropertyName("sessionHandle")]
    public string SessionHandle { get; set; } = "";

    [JsonPropertyName("timeCreated")]
    public long TimeCreated { get; set; }

    [JsonPropertyName("timeExpires")]
    public long TimeExpires { get; set; }
}

public class DashboardSearchTagsResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];
}

public class DashboardAnalyticsResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("data")]
    public Dictionary<string, object>? Data { get; set; }
}
