namespace SuperTokensSDK.Net.Recipes.ThirdParty;

public static class BuiltInProviders
{
    public static TypeProvider Google(TypeProviderConfig input) => new()
    {
        Id = "google",
        Config = new TypeProviderConfig
        {
            ClientId = input.ClientId,
            ClientSecret = input.ClientSecret,
            Scope = input.Scope ?? "openid email profile",
            AuthorizationRedirectUrl = input.AuthorizationRedirectUrl,
            AuthorizationEndpoint = input.AuthorizationEndpoint ?? "https://accounts.google.com/o/oauth2/v2/auth",
            TokenEndpoint = input.TokenEndpoint ?? "https://oauth2.googleapis.com/token",
            UserInfoEndpoint = input.UserInfoEndpoint ?? "https://openidconnect.googleapis.com/v1/userinfo",
            RequiresEmail = input.RequiresEmail ?? true
        }
    };

    public static TypeProvider Github(TypeProviderConfig input) => new()
    {
        Id = "github",
        Config = new TypeProviderConfig
        {
            ClientId = input.ClientId,
            ClientSecret = input.ClientSecret,
            Scope = input.Scope ?? "user:email",
            AuthorizationRedirectUrl = input.AuthorizationRedirectUrl,
            AuthorizationEndpoint = input.AuthorizationEndpoint ?? "https://github.com/login/oauth/authorize",
            TokenEndpoint = input.TokenEndpoint ?? "https://github.com/login/oauth/access_token",
            UserInfoEndpoint = input.UserInfoEndpoint ?? "https://api.github.com/user",
            RequiresEmail = input.RequiresEmail ?? true
        }
    };

    public static TypeProvider Apple(TypeProviderConfig input) => new()
    {
        Id = "apple",
        Config = new TypeProviderConfig
        {
            ClientId = input.ClientId,
            ClientSecret = input.ClientSecret,
            Scope = input.Scope ?? "openid email name",
            AuthorizationRedirectUrl = input.AuthorizationRedirectUrl,
            AuthorizationEndpoint = input.AuthorizationEndpoint ?? "https://appleid.apple.com/auth/authorize",
            TokenEndpoint = input.TokenEndpoint ?? "https://appleid.apple.com/auth/token",
            UserInfoEndpoint = input.UserInfoEndpoint,
            RequiresEmail = input.RequiresEmail ?? true
        }
    };

    public static TypeProvider Discord(TypeProviderConfig input) => new()
    {
        Id = "discord",
        Config = new TypeProviderConfig
        {
            ClientId = input.ClientId,
            ClientSecret = input.ClientSecret,
            Scope = input.Scope ?? "identify email",
            AuthorizationRedirectUrl = input.AuthorizationRedirectUrl,
            AuthorizationEndpoint = input.AuthorizationEndpoint ?? "https://discord.com/api/oauth2/authorize",
            TokenEndpoint = input.TokenEndpoint ?? "https://discord.com/api/oauth2/token",
            UserInfoEndpoint = input.UserInfoEndpoint ?? "https://discord.com/api/users/@me",
            RequiresEmail = input.RequiresEmail ?? true
        }
    };

    public static TypeProvider Facebook(TypeProviderConfig input) => new()
    {
        Id = "facebook",
        Config = new TypeProviderConfig
        {
            ClientId = input.ClientId,
            ClientSecret = input.ClientSecret,
            Scope = input.Scope ?? "email",
            AuthorizationRedirectUrl = input.AuthorizationRedirectUrl,
            AuthorizationEndpoint = input.AuthorizationEndpoint ?? "https://www.facebook.com/v18.0/dialog/oauth",
            TokenEndpoint = input.TokenEndpoint ?? "https://graph.facebook.com/v18.0/oauth/access_token",
            UserInfoEndpoint = input.UserInfoEndpoint ?? "https://graph.facebook.com/me?fields=id,email",
            RequiresEmail = input.RequiresEmail ?? true
        }
    };

    public static TypeProvider GitLab(TypeProviderConfig input) => new()
    {
        Id = "gitlab",
        Config = new TypeProviderConfig
        {
            ClientId = input.ClientId,
            ClientSecret = input.ClientSecret,
            Scope = input.Scope ?? "read_user",
            AuthorizationRedirectUrl = input.AuthorizationRedirectUrl,
            AuthorizationEndpoint = input.AuthorizationEndpoint ?? "https://gitlab.com/oauth/authorize",
            TokenEndpoint = input.TokenEndpoint ?? "https://gitlab.com/oauth/token",
            UserInfoEndpoint = input.UserInfoEndpoint ?? "https://gitlab.com/api/v4/user",
            RequiresEmail = input.RequiresEmail ?? true
        }
    };
}
