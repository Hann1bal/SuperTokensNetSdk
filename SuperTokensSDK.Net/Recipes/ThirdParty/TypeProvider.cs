namespace SuperTokensSDK.Net.Recipes.ThirdParty;

public class TypeProvider
{
    public string Id { get; init; } = "";
    public TypeProviderConfig Config { get; init; } = new();
}

public class TypeProviderConfig
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? Scope { get; set; } = "openid email";
    public string? AuthorizationRedirectUrl { get; set; }
    public string? AuthorizationEndpoint { get; set; }
    public string? TokenEndpoint { get; set; }
    public string? UserInfoEndpoint { get; set; }
    public bool? RequiresEmail { get; set; } = true;
}

public class ProviderUserInfo
{
    public string? ThirdPartyUserId { get; set; }
    public string? Email { get; set; }
    public bool? IsEmailVerified { get; set; }
}
