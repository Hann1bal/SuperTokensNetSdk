namespace SuperTokensSDK.Net.Configuration;

/// <summary>
/// Configuration options for SuperTokens SDK integration.
/// </summary>
public class SuperTokensOptions
{
    /// <summary>
    /// Base URI(s) of the SuperTokens Core service.
    /// Multiple hosts can be separated with a semicolon for round-robin and failover support.
    /// Required — must be set by the consuming application.
    /// </summary>
    public string? CoreUri { get; set; }

    /// <summary>
    /// API key used to authenticate with SuperTokens Core.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Application name used by SuperTokens Core.
    /// Required — must be set by the consuming application.
    /// </summary>
    public string? AppName { get; set; }

    /// <summary>
    /// Connection URI for the frontend (API domain) used during CDI version negotiation.
    /// </summary>
    public string? ApiDomain { get; set; }

    /// <summary>
    /// Website domain used by the SDK for cookie/CSRF handling and CDI version negotiation.
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
