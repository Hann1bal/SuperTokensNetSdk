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

    /// <summary>
    /// Allowlist of origins permitted for cross-origin requests with credentials.
    /// When non-empty, the SuperTokens API middleware only reflects an Origin
    /// header back with <c>Access-Control-Allow-Credentials: true</c> if the
    /// origin is present in this list. When empty (default), the middleware
    /// reflects any origin for backward compatibility, which is insecure when
    /// serving authenticated browser traffic. Configure this list explicitly in
    /// production deployments.
    /// </summary>
    public List<string> AllowedOrigins { get; set; } = new();

    /// <summary>
    /// Validates required configuration fields and throws <see cref="InvalidOperationException"/>
    /// if any critical field is missing. <see cref="CoreUri"/> and <see cref="AppName"/>
    /// are always required. <see cref="ApiDomain"/> and <see cref="WebsiteDomain"/> are
    /// required for CDI version negotiation but are not enforced here for backward
    /// compatibility with callers that negotiate the CDI version out-of-band.
    /// Called by <c>AddSuperTokens</c> during service registration.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(CoreUri))
        {
            throw new InvalidOperationException(
                "SuperTokensOptions.CoreUri must be configured. Set it in appsettings or via the configure delegate.");
        }

        if (string.IsNullOrWhiteSpace(AppName))
        {
            throw new InvalidOperationException(
                "SuperTokensOptions.AppName must be configured.");
        }

        // ApiDomain and WebsiteDomain are used for CDI version negotiation.
        // They are strongly recommended but not enforced here to preserve
        // backward compatibility with callers that supply them lazily.
        if (string.IsNullOrWhiteSpace(ApiDomain))
        {
            // CDI negotiation will still proceed without apiDomain but may
            // produce a degraded experience. Surfaced as a non-fatal check.
        }

        if (string.IsNullOrWhiteSpace(WebsiteDomain))
        {
            // Same rationale as ApiDomain above.
        }
    }
}
