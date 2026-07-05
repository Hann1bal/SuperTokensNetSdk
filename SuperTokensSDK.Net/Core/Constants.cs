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
        public const string Passwordless = "passwordless";
        public const string EmailVerification = "emailverification";
        public const string Jwt = "jwt";
        public const string Multitenancy = "multitenancy";
        public const string ThirdParty = "thirdparty";
        public const string Dashboard = "dashboard";
    }

    /// <summary>
    /// Dashboard recipe CDI paths.
    /// </summary>
    public static class DashboardPaths
    {
        public const string RecipeDashboardSessionVerify = "/recipe/dashboard/session/verify";
        public const string Users = "/users";
        public const string UsersCount = "/users/count";
        public const string UserRemove = "/user/remove";
        public const string TenantsList = "/tenants/list";
        public const string UserDetails = "/user";
        public const string UserEmailVerify = "/user/email/verify";
        public const string UserPassword = "/user/password";
        public const string UserMetadata = "/user/metadata";
        public const string UserSessions = "/user/sessions";
        public const string SearchTags = "/search/tags";
        public const string Analytics = "/analytics";
    }

    /// <summary>
    /// Email verification recipe CDI paths.
    /// </summary>
    public static class EmailVerificationPaths
    {
        public const string RecipeUserEmailVerifyToken = "/recipe/user/email/verify/token";
        public const string RecipeUserEmailVerify = "/recipe/user/email/verify";
        public const string RecipeUserEmailVerifyTokenRemove = "/recipe/user/email/verify/token/remove";
        public const string RecipeUserEmailVerifyRemove = "/recipe/user/email/verify/remove";
    }

    /// <summary>
    /// JWT recipe CDI paths.
    /// </summary>
    public static class JwtPaths
    {
        public const string RecipeJwt = "/recipe/jwt";
        public const string WellKnownJwks = "/.well-known/jwks.json";
    }

    /// <summary>
    /// Multitenancy recipe CDI paths.
    /// </summary>
    public static class MultitenancyPaths
    {
        public const string RecipeMultitenancyTenant = "/recipe/multitenancy/tenant";
        public const string RecipeMultitenancyTenantRemove = "/recipe/multitenancy/tenant/remove";
        public const string RecipeMultitenancyTenantList = "/recipe/multitenancy/tenant/list";
        public const string RecipeMultitenancyConfigThirdParty = "/recipe/multitenancy/config/thirdparty";
        public const string RecipeMultitenancyConfigThirdPartyRemove = "/recipe/multitenancy/config/thirdparty/remove";
        public const string RecipeMultitenancyTenantUser = "/recipe/multitenancy/tenant/user";
        public const string RecipeMultitenancyTenantUserRemove = "/recipe/multitenancy/tenant/user/remove";
    }

    /// <summary>
    /// Passwordless recipe CDI paths.
    /// </summary>
    public static class PasswordlessPaths
    {
        public const string RecipeSigninupCode = "/recipe/signinup/code";
        public const string RecipeSigninupCodeConsume = "/recipe/signinup/code/consume";
        public const string RecipeSigninupCodeCheck = "/recipe/signinup/code/check";
        public const string RecipeSigninupCodes = "/recipe/signinup/codes";
        public const string RecipeSigninupCodesRemove = "/recipe/signinup/codes/remove";
    }

    /// <summary>
    /// ThirdParty recipe CDI paths.
    /// </summary>
    public static class ThirdPartyPaths
    {
        public const string RecipeSigninup = "/recipe/signinup";
        public const string RecipeUsersByEmail = "/recipe/users/by-email";
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
        public const string RecipeJwtData = "/recipe/jwt/data";
        public const string RecipeRolePermissions = "/recipe/role/permissions";
    }

    /// <summary>
    /// Default tenant id used by SuperTokens.
    /// </summary>
    public const string DefaultTenantId = "public";

    /// <summary>
    /// Named HttpClient names used by the SDK.
    /// </summary>
    public static class HttpClientNames
    {
        public const string CoreApiClient = "SuperTokensCoreApiClient";
    }

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
