namespace SuperTokensSDK.Net.Core;

/// <summary>
/// Constants for SuperTokens CDI, recipe IDs, cookie names and header names.
/// Matches the official SuperTokens Go SDK.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Supported CDI versions. Highest is preferred.
    /// </summary>
    public static readonly string[] SupportedCdiVersions = ["5.0"];

    /// <summary>
    /// Recipe IDs used in the rid header.
    /// </summary>
    public static class RecipeIds
    {
        public const string Session = "session";
        public const string EmailPassword = "emailpassword";
        public const string UserRoles = "userroles";
        public const string UserMetadata = "usermetadata";
        public const string Totp = "totp";
    }

    /// <summary>
    /// TOTP recipe CDI paths.
    /// </summary>
    public static class TotpPaths
    {
        public const string RecipeTotpDevice = "/recipe/totp/device";
        public const string RecipeTotpDeviceList = "/recipe/totp/device/list";
        public const string RecipeTotpDeviceRemove = "/recipe/totp/device/remove";
        public const string RecipeTotpDeviceVerify = "/recipe/totp/device/verify";
        public const string RecipeTotpVerify = "/recipe/totp/verify";
    }

    /// <summary>
    /// Cookie names.
    /// </summary>
    public static class CookieNames
    {
        public const string AccessToken = "sAccessToken";
        public const string RefreshToken = "sRefreshToken";
        public const string IdRefreshToken = "sIdRefreshToken";
        public const string AntiCsrf = "sAntiCsrf";
    }

    /// <summary>
    /// Header names.
    /// </summary>
    public static class HeaderNames
    {
        public const string AccessToken = "st-access-token";
        public const string RefreshToken = "st-refresh-token";
        public const string AntiCsrf = "anti-csrf";
        public const string FrontToken = "front-token";
        public const string AuthMode = "st-auth-mode";
        public const string Rid = "rid";
        public const string CdiVersion = "cdi-version";
        public const string ApiKey = "api-key";
    }

    /// <summary>
    /// CDI API paths.
    /// </summary>
    public static class Paths
    {
        public const string ApiVersion = "/apiversion";
        public const string RecipeSession = "/recipe/session";
        public const string RecipeSessionVerify = "/recipe/session/verify";
        public const string RecipeSessionRefresh = "/recipe/session/refresh";
        public const string RecipeSessionRevoke = "/recipe/session/revoke";
        public const string RecipeSignUp = "/recipe/signup";
        public const string RecipeSignIn = "/recipe/signin";
        public const string RecipeUserPasswordReset = "/recipe/user/password/reset";
        public const string RecipeUserRoles = "/recipe/user/roles";
        public const string RecipeUserRole = "/recipe/user/role";
        public const string RecipeUserMetadata = "/recipe/user/metadata";
    }

    /// <summary>
    /// Default tenant id used by SuperTokens.
    /// </summary>
    public const string DefaultTenantId = "public";

    /// <summary>
    /// Status values returned by SuperTokens Core.
    /// </summary>
    public static class Status
    {
        public const string Ok = "OK";
        public const string Unauthorized = "UNAUTHORISED";
        public const string TryRefreshToken = "TRY_REFRESH_TOKEN";
        public const string TokenTheftDetected = "TOKEN_THEFT_DETECTED";
        public const string InvalidClaims = "INVALID_CLAIMS";
        public const string DeviceAlreadyExistsError = "DEVICE_ALREADY_EXISTS_ERROR";
        public const string UnknownDeviceError = "UNKNOWN_DEVICE_ERROR";
        public const string UnknownUserIdError = "UNKNOWN_USER_ID_ERROR";
        public const string InvalidTotpError = "INVALID_TOTP_ERROR";
        public const string LimitReachedError = "LIMIT_REACHED_ERROR";
    }

    /// <summary>
    /// HTTP status code returned by Core when rate limited.
    /// </summary>
    public const int RateLimitStatusCode = 429;

    /// <summary>
    /// Number of retries on rate limit.
    /// </summary>
    public const int RateLimitRetries = 5;
}
