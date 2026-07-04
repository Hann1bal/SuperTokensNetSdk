namespace SuperTokensSDK.Net.Configuration;

/// <summary>
/// Configuration options for SuperTokens SDK integration.
/// </summary>
public class SuperTokensOptions
{
    /// <summary>
    /// Base URI of the SuperTokens Core service (default: http://supertokens-core:3567).
    /// </summary>
    public string? CoreUri { get; set; } = "http://supertokens-core:3567";

    /// <summary>
    /// API key used to authenticate with SuperTokens Core.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Application name used by SuperTokens Core.
    /// </summary>
    public string? AppName { get; set; } = "HistoneDB";

    /// <summary>
    /// Connection URI for the frontend (API domain) used by the SDK.
    /// </summary>
    public string? ApiDomain { get; set; }

    /// <summary>
    /// Website domain used by the SDK for cookie/CSRF handling.
    /// </summary>
    public string? WebsiteDomain { get; set; }

    /// <summary>
    /// Name of the access token cookie.
    /// </summary>
    public string AccessTokenCookieName { get; set; } = "sAccessToken";

    /// <summary>
    /// Name of the refresh token cookie.
    /// </summary>
    public string RefreshTokenCookieName { get; set; } = "sRefreshToken";

    /// <summary>
    /// Name of the anti-CSRF token cookie/header.
    /// </summary>
    public string AntiCsrfCookieName { get; set; } = "sAntiCsrf";

    /// <summary>
    /// Enable anti-CSRF protection for cookie-based sessions.
    /// </summary>
    public bool EnableAntiCsrf { get; set; } = true;
}
