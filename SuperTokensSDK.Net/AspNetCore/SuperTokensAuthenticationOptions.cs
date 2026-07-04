using Microsoft.AspNetCore.Authentication;

namespace SuperTokensSDK.Net.AspNetCore;

/// <summary>
/// Options for the SuperTokens authentication handler.
/// </summary>
public class SuperTokensAuthenticationOptions : AuthenticationSchemeOptions
{
    public string AccessTokenCookieName { get; set; } = "sAccessToken";
    public string RefreshTokenCookieName { get; set; } = "sRefreshToken";
    public string AntiCsrfCookieName { get; set; } = "sAntiCsrf";
}
