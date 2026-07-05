using Microsoft.AspNetCore.Authentication;

namespace SuperTokensSDK.Net.AspNetCore;

/// <summary>
/// Options for the SuperTokens authentication handler.
/// </summary>
public class SuperTokensAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>Name of the cookie holding the SuperTokens access token.</summary>
    public string AccessTokenCookieName { get; set; } = "sAccessToken";
    /// <summary>Name of the cookie holding the SuperTokens refresh token.</summary>
    public string RefreshTokenCookieName { get; set; } = "sRefreshToken";
    /// <summary>Name of the cookie/header holding the anti-CSRF token.</summary>
    public string AntiCsrfCookieName { get; set; } = "sAntiCsrf";
    /// <summary>Whether anti-CSRF protection is enabled for cookie sessions.</summary>
    public bool EnableAntiCsrf { get; set; } = true;
}
