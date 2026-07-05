namespace SuperTokensSDK.Net.Core.Models;

public sealed class ThirdPartyUser
{
    public string Id { get; set; } = "";
    public string? Email { get; set; }
    public long? TimeJoined { get; set; }
    public string[]? TenantIds { get; set; }
    public string[]? ThirdPartyIds { get; set; }
}

public sealed class ThirdPartyInfo
{
    public string ThirdPartyId { get; set; } = "";
    public string ThirdPartyUserId { get; set; } = "";
    public string? Email { get; set; }
}

public sealed class SignInUpRequest
{
    public ThirdPartyInfo ThirdParty { get; set; } = new();
    public string? OauthTokens { get; set; }
}

public sealed class SignInUpResponse
{
    public string? Status { get; set; }
    public bool CreatedNewUser { get; set; }
    public ThirdPartyUser? User { get; set; }
    public RecipeUserId? RecipeUserId { get; set; }
}

public sealed class RecipeUserId
{
    public string Id { get; set; } = "";
    public string? RecipeId { get; set; }
}

public sealed class ManuallyCreateOrUpdateUserRequest
{
    public string ThirdPartyId { get; set; } = "";
    public string ThirdPartyUserId { get; set; } = "";
    public string? Email { get; set; }
}

public sealed class ManuallyCreateOrUpdateUserResponse
{
    public string? Status { get; set; }
    public bool CreatedNewUser { get; set; }
    public ThirdPartyUser? User { get; set; }
}

public sealed class GetUsersByEmailResponse
{
    public string? Status { get; set; }
    public List<UserByEmailItem> Users { get; set; } = new();
}

public sealed class UserByEmailItem
{
    public string RecipeId { get; set; } = "";
    public ThirdPartyUser User { get; set; } = new();
}
