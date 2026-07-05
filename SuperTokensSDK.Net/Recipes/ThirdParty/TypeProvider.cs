namespace SuperTokensSDK.Net.Recipes.ThirdParty;

/// <summary>
/// Describes a configured third-party OAuth provider instance, including its
/// identifier and connection configuration.
/// </summary>
public class TypeProvider
{
    /// <summary>Provider identifier (e.g. "google", "github").</summary>
    public string Id { get; init; } = "";
    /// <summary>OAuth connection configuration for this provider.</summary>
    public TypeProviderConfig Config { get; init; } = new();
}

/// <summary>
/// OAuth connection configuration used to talk to a third-party provider.
/// </summary>
public class TypeProviderConfig
{
    /// <summary>OAuth client id issued by the provider.</summary>
    public string? ClientId { get; set; }
    /// <summary>OAuth client secret issued by the provider.</summary>
    public string? ClientSecret { get; set; }
    /// <summary>OAuth scopes to request (space-delimited).</summary>
    public string? Scope { get; set; } = "openid email";
    /// <summary>Redirect URL used after the authorization step.</summary>
    public string? AuthorizationRedirectUrl { get; set; }
    /// <summary>Provider authorization endpoint URL.</summary>
    public string? AuthorizationEndpoint { get; set; }
    /// <summary>Provider token exchange endpoint URL.</summary>
    public string? TokenEndpoint { get; set; }
    /// <summary>Provider user info endpoint URL.</summary>
    public string? UserInfoEndpoint { get; set; }
    /// <summary>Whether the provider must return a verified email.</summary>
    public bool? RequiresEmail { get; set; } = true;
}

/// <summary>
/// User info returned by a third-party provider after a successful sign-in.
/// </summary>
public class ProviderUserInfo
{
    /// <summary>Stable id of the user at the third-party provider.</summary>
    public string? ThirdPartyUserId { get; set; }
    /// <summary>Email address returned by the provider, if any.</summary>
    public string? Email { get; set; }
    /// <summary>Whether the provider confirmed the email as verified.</summary>
    public bool? IsEmailVerified { get; set; }
}
