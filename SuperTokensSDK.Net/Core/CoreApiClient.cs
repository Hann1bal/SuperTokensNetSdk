using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SuperTokensSDK.Net.Configuration;
using SuperTokensSDK.Net.Core.Models;
using SystemIdentityModelTokensJwt = System.IdentityModel.Tokens.Jwt;

namespace SuperTokensSDK.Net.Core;

/// <summary>
/// HTTP client implementation for talking to SuperTokens Core via CDI.
/// Supports CDI version negotiation, recipe id headers, rate limit retries,
/// multi-host failover and typed error responses.
/// </summary>
public class CoreApiClient : ICoreApiClient
{
    private readonly HttpClient _httpClient;
    private readonly SuperTokensOptions _options;
    private readonly ILogger<CoreApiClient> _logger;
    private readonly IReadOnlyList<Uri> _hosts;
    private readonly JwksClient? _jwksClient;

    private readonly SemaphoreSlim _versionSemaphore = new(1, 1);
    private string? _cdiVersion;
    private long _hostIndex = -1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public CoreApiClient(HttpClient httpClient, IOptions<SuperTokensOptions> options, ILogger<CoreApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _hosts = ParseHosts(_options.CoreUri);

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(Constants.HeaderNames.ApiKey, _options.ApiKey);
        }
    }

    public CoreApiClient(HttpClient httpClient, IOptions<SuperTokensOptions> options, ILogger<CoreApiClient> logger, JwksClient jwksClient)
        : this(httpClient, options, logger)
    {
        _jwksClient = jwksClient ?? throw new ArgumentNullException(nameof(jwksClient));
    }

    #region Recipe API methods

    public async Task<CreateOrRefreshAPIResponse> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<CreateSessionRequest, CreateOrRefreshAPIResponse>(
            HttpMethod.Post, Constants.Paths.RecipeSession, request, Constants.RecipeIds.Session, cancellationToken);
    }

    public async Task<GetSessionResponse> VerifySessionAsync(VerifySessionRequest request, CancellationToken cancellationToken = default)
    {
        // Hybrid approach matching the official SuperTokens Go SDK:
        // 1. Verify JWT signature locally using JWKS (with kid matching + algorithm restriction)
        // 2. Anti-CSRF check locally (compare token in JWT with provided token)
        // 3. Only call Core's /recipe/session/verify when checkDatabase is true or parentRefreshTokenHash1 is present
        // 4. If JWKS fetch fails, throw TryRefreshTokenException (NO unsigned fallback)

        if (_jwksClient == null)
        {
            // No JWKS client configured - must call Core's verify endpoint
            return await SendJsonAsync<VerifySessionRequest, GetSessionResponse>(
                HttpMethod.Post, Constants.Paths.RecipeSessionVerify, request, Constants.RecipeIds.Session, cancellationToken);
        }

        // Step 1: Local JWT signature verification using JWKS
        Dictionary<string, object>? payload;
        try
        {
            payload = await VerifyJwtSignatureAsync(request.AccessToken, cancellationToken);
        }
        catch (UnauthorizedException)
        {
            throw; // Signature verification failed - re-throw
        }

        if (payload == null)
        {
            // JWKS fetch failed - cannot verify locally, tell client to refresh
            throw new TryRefreshTokenException("Failed to fetch JWKS from Core. Please refresh the session.");
        }

        // Step 2: Extract session info from verified JWT payload
        var userId = payload.GetValueOrDefault("sub")?.ToString() ?? "";
        var sessionHandle = payload.GetValueOrDefault("sessionHandle")?.ToString() ?? "";
        var antiCsrfTokenInJwt = payload.GetValueOrDefault("antiCsrfToken")?.ToString();
        var parentRefreshTokenHash1 = payload.GetValueOrDefault("parentRefreshTokenHash1")?.ToString();

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedException("Access token does not contain a valid userId.");
        }

        // Step 3: Anti-CSRF check (local, matching Go SDK behavior)
        if (request.DoAntiCsrfCheck && request.EnableAntiCsrf)
        {
            if (string.IsNullOrEmpty(request.AntiCsrfToken))
            {
                throw new TryRefreshTokenException(
                    "Provided antiCsrfToken is undefined. If you do not want anti-csrf check for this API, please set doAntiCsrfCheck to false.");
            }
            if (antiCsrfTokenInJwt != request.AntiCsrfToken)
            {
                throw new TryRefreshTokenException("anti-csrf check failed");
            }
        }

        // Step 4: Extract userDataInJWT (custom claims, excluding protected fields)
        var userData = new Dictionary<string, object>();
        var protectedFields = new HashSet<string>
        {
            "sub", "iat", "exp", "sessionHandle",
            "parentRefreshTokenHash1", "refreshTokenHash1",
            "antiCsrfToken", "rsub", "tId"
        };
        foreach (var kvp in payload)
        {
            if (!protectedFields.Contains(kvp.Key))
            {
                userData[kvp.Key] = kvp.Value;
            }
        }

        // Step 5: If checkDatabase is true OR parentRefreshTokenHash1 is present,
        // call Core's verify endpoint for database-level checks (revocation, token theft)
        if (request.CheckDatabase || !string.IsNullOrEmpty(parentRefreshTokenHash1))
        {
            return await SendJsonAsync<VerifySessionRequest, GetSessionResponse>(
                HttpMethod.Post, Constants.Paths.RecipeSessionVerify, request, Constants.RecipeIds.Session, cancellationToken);
        }

        // Step 6: Return session info from local JWT verification (no Core call needed)
        return new GetSessionResponse
        {
            Status = "OK",
            Session = new SessionStruct
            {
                Handle = sessionHandle,
                UserId = userId,
                UserDataInJWT = userData,
                TenantId = payload.GetValueOrDefault("tId")?.ToString() ?? Constants.DefaultTenantId
            }
        };
    }

    public async Task<CreateOrRefreshAPIResponse> RefreshSessionAsync(RefreshSessionRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<RefreshSessionRequest, CreateOrRefreshAPIResponse>(
            HttpMethod.Post, Constants.Paths.RecipeSessionRefresh, request, Constants.RecipeIds.Session, cancellationToken);
    }

    public async Task<RevokeSessionResponse> RevokeSessionAsync(RevokeSessionRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<RevokeSessionRequest, RevokeSessionResponse>(
            HttpMethod.Post, Constants.Paths.RecipeSessionRevoke, request, Constants.RecipeIds.Session, cancellationToken);
    }

    public async Task<SignUpResponse> SignUpAsync(SignUpRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<SignUpRequest, SignUpResponse>(
            HttpMethod.Post, Constants.Paths.RecipeSignUp, request, Constants.RecipeIds.EmailPassword, cancellationToken);
    }

    public async Task<SignUpResponse> SignInAsync(SignUpRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<SignUpRequest, SignUpResponse>(
            HttpMethod.Post, Constants.Paths.RecipeSignIn, request, Constants.RecipeIds.EmailPassword, cancellationToken);
    }

    public async Task<StatusResponse> ResetPasswordAsync(PasswordResetRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<PasswordResetRequest, StatusResponse>(
            HttpMethod.Post, Constants.Paths.RecipeUserPasswordReset, request, Constants.RecipeIds.EmailPassword, cancellationToken);
    }

    public async Task<GetUserResponse> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["userId"] = userId;
        return await GetJsonAsync<GetUserResponse>(
            $"/recipe/user?{query}", Constants.RecipeIds.EmailPassword, cancellationToken);
    }

    public async Task<GetUserResponse> GetUserByEmailAsync(string email, string tenantId = "public", CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["email"] = email;
        return await GetJsonAsync<GetUserResponse>(
            $"{tenantId}/recipe/user?{query}", Constants.RecipeIds.EmailPassword, cancellationToken);
    }

    public async Task<GeneratePasswordResetTokenResponse> GeneratePasswordResetTokenAsync(GeneratePasswordResetTokenRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<GeneratePasswordResetTokenRequest, GeneratePasswordResetTokenResponse>(
            HttpMethod.Post, "/recipe/user/password/reset/token", request, Constants.RecipeIds.EmailPassword, cancellationToken);
    }

    public async Task<StatusResponse> UpdateEmailOrPasswordAsync(UpdateEmailOrPasswordRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<UpdateEmailOrPasswordRequest, StatusResponse>(
            HttpMethod.Put, "/recipe/user", request, Constants.RecipeIds.EmailPassword, cancellationToken);
    }

    public async Task<EmailExistsResponse> EmailExistsAsync(string email, string tenantId = "public", CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["email"] = email;
        return await GetJsonAsync<EmailExistsResponse>(
            $"{tenantId}/recipe/signup/email/exists?{query}", Constants.RecipeIds.EmailPassword, cancellationToken);
    }

    public async Task<StatusResponse> AddUserRolesAsync(UserRolesRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<UserRolesRequest, StatusResponse>(
            HttpMethod.Put, Constants.Paths.RecipeUserRoles, request, Constants.RecipeIds.UserRoles, cancellationToken);
    }

    public async Task<UserRolesResponse> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["userId"] = userId;
        return await GetJsonAsync<UserRolesResponse>(
            $"{Constants.Paths.RecipeUserRoles}?{query}", Constants.RecipeIds.UserRoles, cancellationToken);
    }

    public async Task<StatusResponse> RemoveUserRolesAsync(UserRolesRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<UserRolesRequest, StatusResponse>(
            HttpMethod.Delete, Constants.Paths.RecipeUserRoles, request, Constants.RecipeIds.UserRoles, cancellationToken);
    }

    public async Task<RoleExistsResponse> DoesRoleExistAsync(string userId, string role, CancellationToken cancellationToken = default)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["userId"] = userId;
        query["role"] = role;
        return await GetJsonAsync<RoleExistsResponse>(
            $"{Constants.Paths.RecipeUserRole}?{query}", Constants.RecipeIds.UserRoles, cancellationToken);
    }

    public async Task<UsersThatHaveRoleResponse> GetUsersThatHaveRoleAsync(string role, string tenantId = "public", int? limit = null, string? timeJoinedOrder = null, string? paginationToken = null, CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["role"] = role;
        if (limit.HasValue)
        {
            query["limit"] = limit.Value.ToString();
        }
        if (!string.IsNullOrEmpty(timeJoinedOrder))
        {
            query["timeJoinedOrder"] = timeJoinedOrder;
        }
        if (!string.IsNullOrEmpty(paginationToken))
        {
            query["paginationToken"] = paginationToken;
        }
        return await GetJsonAsync<UsersThatHaveRoleResponse>(
            $"{tenantId}/recipe/role/users?{query}", Constants.RecipeIds.UserRoles, cancellationToken);
    }

    public async Task<UserRolesCreateResponse> CreateNewRoleOrAddPermissionsAsync(UserRolesCreateRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<UserRolesCreateRequest, UserRolesCreateResponse>(
            HttpMethod.Put, "/recipe/role", request, Constants.RecipeIds.UserRoles, cancellationToken);
    }

    public async Task<PermissionsForRoleResponse> GetPermissionsForRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["role"] = role;
        return await GetJsonAsync<PermissionsForRoleResponse>(
            $"/recipe/role/permissions?{query}", Constants.RecipeIds.UserRoles, cancellationToken);
    }

    public async Task<StatusResponse> RemovePermissionsFromRoleAsync(RemovePermissionsRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<RemovePermissionsRequest, StatusResponse>(
            HttpMethod.Post, "/recipe/role/permissions/remove", request, Constants.RecipeIds.UserRoles, cancellationToken);
    }

    public async Task<RolesWithPermissionResponse> GetRolesThatHavePermissionAsync(string permission, CancellationToken cancellationToken = default)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["permission"] = permission;
        return await GetJsonAsync<RolesWithPermissionResponse>(
            $"/recipe/permission/roles?{query}", Constants.RecipeIds.UserRoles, cancellationToken);
    }

    public async Task<DeleteRoleResponse> DeleteRoleAsync(DeleteRoleRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<DeleteRoleRequest, DeleteRoleResponse>(
            HttpMethod.Post, "/recipe/role/remove", request, Constants.RecipeIds.UserRoles, cancellationToken);
    }

    public async Task<AllRolesResponse> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        return await GetJsonAsync<AllRolesResponse>(
            "/recipe/roles", Constants.RecipeIds.UserRoles, cancellationToken);
    }

    public async Task<UserMetadataResponse> GetUserMetadataAsync(string userId, CancellationToken cancellationToken = default)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["userId"] = userId;
        return await GetJsonAsync<UserMetadataResponse>(
            $"{Constants.Paths.RecipeUserMetadata}?{query}", Constants.RecipeIds.UserMetadata, cancellationToken);
    }

    public async Task<StatusResponse> UpdateUserMetadataAsync(UserMetadataUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<UserMetadataUpdateRequest, StatusResponse>(
            HttpMethod.Put, Constants.Paths.RecipeUserMetadata, request, Constants.RecipeIds.UserMetadata, cancellationToken);
    }

    public async Task<StatusResponse> UpdateJwtDataAsync(UpdateJwtDataRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<UpdateJwtDataRequest, StatusResponse>(
            HttpMethod.Put, Constants.Paths.RecipeJwtData, request, Constants.RecipeIds.Session, cancellationToken);
    }

    public async Task<CreateTotpDeviceResponse> CreateTotpDeviceAsync(CreateTotpDeviceRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<CreateTotpDeviceRequest, CreateTotpDeviceResponse>(
            HttpMethod.Post, Constants.TotpPaths.RecipeTotpDevice, request, Constants.RecipeIds.Totp, cancellationToken);
    }

    public async Task<VerifyTotpDeviceResponse> VerifyTotpDeviceAsync(VerifyTotpDeviceRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<VerifyTotpDeviceRequest, VerifyTotpDeviceResponse>(
            HttpMethod.Post, Constants.TotpPaths.RecipeTotpDeviceVerify, request, Constants.RecipeIds.Totp, cancellationToken);
    }

    public async Task<VerifyTotpCodeResponse> VerifyTotpCodeAsync(VerifyTotpCodeRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<VerifyTotpCodeRequest, VerifyTotpCodeResponse>(
            HttpMethod.Post, Constants.TotpPaths.RecipeTotpVerify, request, Constants.RecipeIds.Totp, cancellationToken);
    }

    public async Task<ListTotpDevicesResponse> ListTotpDevicesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["userId"] = userId;
        return await GetJsonAsync<ListTotpDevicesResponse>(
            $"{Constants.TotpPaths.RecipeTotpDeviceList}?{query}", Constants.RecipeIds.Totp, cancellationToken);
    }

    public async Task<RemoveTotpDeviceResponse> RemoveTotpDeviceAsync(RemoveTotpDeviceRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<RemoveTotpDeviceRequest, RemoveTotpDeviceResponse>(
            HttpMethod.Post, Constants.TotpPaths.RecipeTotpDeviceRemove, request, Constants.RecipeIds.Totp, cancellationToken);
    }

    public async Task<CreateCodeResponse> CreatePasswordlessCodeAsync(CreateCodeRequest request, string tenantId = "public", CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        return await SendJsonAsync<CreateCodeRequest, CreateCodeResponse>(
            HttpMethod.Post, $"{tenantId}{Constants.PasswordlessPaths.RecipeSigninupCode}", request, Constants.RecipeIds.Passwordless, cancellationToken);
    }

    public async Task<ConsumeCodeResponse> ConsumePasswordlessCodeAsync(ConsumeCodeRequest request, string tenantId = "public", CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        return await SendJsonAsync<ConsumeCodeRequest, ConsumeCodeResponse>(
            HttpMethod.Post, $"{tenantId}{Constants.PasswordlessPaths.RecipeSigninupCodeConsume}", request, Constants.RecipeIds.Passwordless, cancellationToken);
    }

    public async Task<CreateEmailVerificationTokenResponse> CreateEmailVerificationTokenAsync(string userId, string? email, string tenantId = "public", CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        return await SendJsonAsync<CreateEmailVerificationTokenRequest, CreateEmailVerificationTokenResponse>(
            HttpMethod.Post, $"{tenantId}{Constants.EmailVerificationPaths.RecipeUserEmailVerifyToken}", new CreateEmailVerificationTokenRequest { UserId = userId, Email = email }, Constants.RecipeIds.EmailVerification, cancellationToken);
    }

    public async Task<VerifyEmailResponse> VerifyEmailAsync(string token, string tenantId = "public", CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        return await SendJsonAsync<VerifyEmailRequest, VerifyEmailResponse>(
            HttpMethod.Post, $"{tenantId}{Constants.EmailVerificationPaths.RecipeUserEmailVerify}", new VerifyEmailRequest { Token = token }, Constants.RecipeIds.EmailVerification, cancellationToken);
    }

    public async Task<IsEmailVerifiedResponse> IsEmailVerifiedAsync(string userId, string? email, string tenantId = "public", CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["userId"] = userId;
        if (!string.IsNullOrEmpty(email))
        {
            query["email"] = email;
        }
        return await GetJsonAsync<IsEmailVerifiedResponse>(
            $"{tenantId}{Constants.EmailVerificationPaths.RecipeUserEmailVerify}?{query}", Constants.RecipeIds.EmailVerification, cancellationToken);
    }

    public async Task RevokeEmailVerificationTokensAsync(string userId, string? email, string tenantId = "public", CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        await SendJsonAsync<RevokeEmailVerificationTokensRequest, StatusResponse>(
            HttpMethod.Post, $"{tenantId}{Constants.EmailVerificationPaths.RecipeUserEmailVerifyTokenRemove}", new RevokeEmailVerificationTokensRequest { UserId = userId, Email = email }, Constants.RecipeIds.EmailVerification, cancellationToken);
    }

    public async Task UnverifyEmailAsync(string userId, string? email, string tenantId = "public", CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        await SendJsonAsync<UnverifyEmailRequest, StatusResponse>(
            HttpMethod.Post, $"{tenantId}{Constants.EmailVerificationPaths.RecipeUserEmailVerifyRemove}", new UnverifyEmailRequest { UserId = userId, Email = email }, Constants.RecipeIds.EmailVerification, cancellationToken);
    }

    public async Task<CreateJwtResponse> CreateJwtAsync(Dictionary<string, object> payload, int validityInSeconds = 3600, string? useStaticSigningKey = null, string tenantId = "public", CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        return await SendJsonAsync<CreateJwtRequest, CreateJwtResponse>(
            HttpMethod.Post, $"{tenantId}{Constants.JwtPaths.RecipeJwt}", new CreateJwtRequest { Payload = payload, Validity = validityInSeconds, UseStaticSigningKey = useStaticSigningKey }, Constants.RecipeIds.Jwt, cancellationToken);
    }

    public async Task<JwksResponse> GetJwksAsync(CancellationToken cancellationToken = default)
    {
        return await GetJsonAsync<JwksResponse>(
            Constants.JwtPaths.WellKnownJwks, null, cancellationToken);
    }

    public async Task<CreateOrUpdateTenantResponse> CreateOrUpdateTenantAsync(string tenantId, TenantConfig config, CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        return await SendJsonAsync<CreateOrUpdateTenantRequest, CreateOrUpdateTenantResponse>(
            HttpMethod.Put, Constants.MultitenancyPaths.RecipeMultitenancyTenant, new CreateOrUpdateTenantRequest { TenantId = tenantId, Config = config }, Constants.RecipeIds.Multitenancy, cancellationToken);
    }

    public async Task<DeleteTenantResponse> DeleteTenantAsync(string tenantId, bool? deleteConditional = null, CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        return await SendJsonAsync<DeleteTenantRequest, DeleteTenantResponse>(
            HttpMethod.Post, Constants.MultitenancyPaths.RecipeMultitenancyTenantRemove, new DeleteTenantRequest { TenantId = tenantId, DeleteConditional = deleteConditional }, Constants.RecipeIds.Multitenancy, cancellationToken);
    }

    public async Task<GetTenantResponse> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        return await GetJsonAsync<GetTenantResponse>(
            $"{tenantId}{Constants.MultitenancyPaths.RecipeMultitenancyTenant}", Constants.RecipeIds.Multitenancy, cancellationToken);
    }

    public async Task<ListTenantsResponse> ListAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        return await GetJsonAsync<ListTenantsResponse>(
            Constants.MultitenancyPaths.RecipeMultitenancyTenantList, Constants.RecipeIds.Multitenancy, cancellationToken);
    }

    public async Task CreateOrUpdateThirdPartyConfigAsync(string tenantId, Dictionary<string, object> config, CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        await SendJsonAsync<Dictionary<string, object>, StatusResponse>(
            HttpMethod.Put, $"{tenantId}{Constants.MultitenancyPaths.RecipeMultitenancyConfigThirdParty}", config, Constants.RecipeIds.Multitenancy, cancellationToken);
    }

    public async Task DeleteThirdPartyConfigAsync(string tenantId, string thirdPartyId, CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        await SendJsonAsync<Dictionary<string, object>, StatusResponse>(
            HttpMethod.Post, $"{tenantId}{Constants.MultitenancyPaths.RecipeMultitenancyConfigThirdPartyRemove}", new Dictionary<string, object> { ["thirdPartyId"] = thirdPartyId }, Constants.RecipeIds.Multitenancy, cancellationToken);
    }

    public async Task<AssociateUserResponse> AssociateUserToTenantAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        return await SendJsonAsync<AssociateUserRequest, AssociateUserResponse>(
            HttpMethod.Post, $"{tenantId}{Constants.MultitenancyPaths.RecipeMultitenancyTenantUser}", new AssociateUserRequest { UserId = userId }, Constants.RecipeIds.Multitenancy, cancellationToken);
    }

    public async Task<bool> DisassociateUserFromTenantAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        var response = await SendJsonAsync<DisassociateUserFromTenantRequest, StatusResponse>(
            HttpMethod.Post, $"{tenantId}{Constants.MultitenancyPaths.RecipeMultitenancyTenantUserRemove}", new DisassociateUserFromTenantRequest { UserId = userId }, Constants.RecipeIds.Multitenancy, cancellationToken);
        return response.Status == Constants.Status.Ok;
    }

    public async Task<List<string>> GetAllSessionHandlesForUserAsync(string userId, string tenantId = "public", bool fetchAcrossAllTenants = false, CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["userId"] = userId;
        query["fetchAcrossAllTenants"] = fetchAcrossAllTenants ? "true" : "false";
        var response = await GetJsonAsync<GetAllSessionHandlesResponse>(
            $"{tenantId}/recipe/session/user?{query}", Constants.RecipeIds.Session, cancellationToken);
        return response.SessionHandles;
    }

    public async Task<SessionInfo?> GetSessionInformationAsync(string sessionHandle, CancellationToken cancellationToken = default)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["sessionHandle"] = sessionHandle;
        try
        {
            var response = await GetJsonAsync<SessionInformationResponse>(
                $"/recipe/session?{query}", Constants.RecipeIds.Session, cancellationToken);
            if (response.Status != Constants.Status.Ok)
                return null;
            return response.Session;
        }
        catch (SuperTokensException)
        {
            return null;
        }
    }

    public async Task<List<string>> RevokeMultipleSessionsAsync(List<string> sessionHandles, CancellationToken cancellationToken = default)
    {
        var response = await SendJsonAsync<RevokeMultipleSessionsRequest, RevokeMultipleSessionsResponse>(
            HttpMethod.Post, "/recipe/session/remove", new RevokeMultipleSessionsRequest { SessionHandles = sessionHandles }, Constants.RecipeIds.Session, cancellationToken);
        return response.SessionHandlesRevoked;
    }

    public async Task<List<string>> RevokeAllSessionsForUserAsync(string userId, string tenantId = "public", bool revokeAcrossAllTenants = false, CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        var response = await SendJsonAsync<RevokeAllSessionsRequest, RevokeAllSessionsResponse>(
            HttpMethod.Post, $"{tenantId}/recipe/session/remove", new RevokeAllSessionsRequest { UserId = userId, RevokeAcrossAllTenants = revokeAcrossAllTenants }, Constants.RecipeIds.Session, cancellationToken);
        return response.SessionHandlesRevoked;
    }

    public async Task<UserListResponse> GetUsersAsync(int limit = 100, string? paginationToken = null, string timeJoinedOrder = "DESC", CancellationToken cancellationToken = default)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["limit"] = limit.ToString();
        query["timeJoinedOrder"] = timeJoinedOrder;
        if (!string.IsNullOrEmpty(paginationToken))
        {
            query["paginationToken"] = paginationToken;
        }
        return await GetJsonAsync<UserListResponse>(
            $"/users?{query}", null, cancellationToken);
    }

    public async Task<UserCountResponse> GetUserCountAsync(CancellationToken cancellationToken = default)
    {
        return await GetJsonAsync<UserCountResponse>(
            "/users/count", null, cancellationToken);
    }

    public async Task<StatusResponse> DeleteUserAsync(DeleteUserRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<DeleteUserRequest, StatusResponse>(
            HttpMethod.Post, "/user/remove", request, null, cancellationToken);
    }

    public async Task<SignInUpResponse> ThirdPartySignInUpAsync(SignInUpRequest request, string tenantId = "public", CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        return await SendJsonAsync<SignInUpRequest, SignInUpResponse>(
            HttpMethod.Post, $"{tenantId}{Constants.ThirdPartyPaths.RecipeSigninup}", request, Constants.RecipeIds.ThirdParty, cancellationToken);
    }

    public async Task<ManuallyCreateOrUpdateUserResponse> ManuallyCreateOrUpdateThirdPartyUserAsync(ManuallyCreateOrUpdateUserRequest request, string tenantId = "public", CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        return await SendJsonAsync<ManuallyCreateOrUpdateUserRequest, ManuallyCreateOrUpdateUserResponse>(
            HttpMethod.Post, $"{tenantId}{Constants.ThirdPartyPaths.RecipeSigninup}", request, Constants.RecipeIds.ThirdParty, cancellationToken);
    }

    public async Task<ThirdPartyUser?> GetThirdPartyUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["userId"] = userId;
        return await GetJsonAsync<ThirdPartyUser>(
            $"/recipe/user?{query}", Constants.RecipeIds.ThirdParty, cancellationToken);
    }

    public async Task<ThirdPartyUser?> GetThirdPartyUserByThirdPartyInfoAsync(ThirdPartyInfo info, string tenantId = "public", CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["thirdPartyId"] = info.ThirdPartyId;
        query["thirdPartyUserId"] = info.ThirdPartyUserId;
        return await GetJsonAsync<ThirdPartyUser>(
            $"{tenantId}{Constants.ThirdPartyPaths.RecipeSigninup}?{query}", Constants.RecipeIds.ThirdParty, cancellationToken);
    }

    public async Task<GetUsersByEmailResponse> GetThirdPartyUsersByEmailAsync(string email, string tenantId = "public", CancellationToken cancellationToken = default)
    {
        ValidateTenantId(tenantId);
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["email"] = email;
        return await GetJsonAsync<GetUsersByEmailResponse>(
            $"{tenantId}{Constants.ThirdPartyPaths.RecipeUsersByEmail}?{query}", Constants.RecipeIds.ThirdParty, cancellationToken);
    }

    public async Task<DashboardSignInResponse> DashboardSignInAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<DashboardSignInRequest, DashboardSignInResponse>(
            HttpMethod.Post, Constants.DashboardPaths.RecipeDashboardSessionVerify, new DashboardSignInRequest { ApiKey = apiKey }, Constants.RecipeIds.Dashboard, cancellationToken);
    }

    public async Task<DashboardSignOutResponse> DashboardSignOutAsync(CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<DashboardSignInRequest, DashboardSignOutResponse>(
            HttpMethod.Post, Constants.DashboardPaths.RecipeDashboardSessionVerify, new DashboardSignInRequest { ApiKey = null! }, Constants.RecipeIds.Dashboard, cancellationToken);
    }

    public async Task<DashboardUsersResponse> DashboardGetUsersAsync(int? limit = null, string? paginationToken = null, string timeJoinedOrder = "DESC", CancellationToken cancellationToken = default)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        if (limit.HasValue)
        {
            query["limit"] = limit.Value.ToString();
        }
        query["timeJoinedOrder"] = timeJoinedOrder;
        if (!string.IsNullOrEmpty(paginationToken))
        {
            query["paginationToken"] = paginationToken;
        }
        return await GetJsonAsync<DashboardUsersResponse>(
            $"{Constants.DashboardPaths.Users}?{query}", Constants.RecipeIds.Dashboard, cancellationToken);
    }

    public async Task<DashboardUsersCountResponse> DashboardGetUsersCountAsync(CancellationToken cancellationToken = default)
    {
        return await GetJsonAsync<DashboardUsersCountResponse>(
            Constants.DashboardPaths.UsersCount, Constants.RecipeIds.Dashboard, cancellationToken);
    }

    public async Task<DashboardTenantsListResponse> DashboardListTenantsAsync(CancellationToken cancellationToken = default)
    {
        return await GetJsonAsync<DashboardTenantsListResponse>(
            Constants.DashboardPaths.TenantsList, Constants.RecipeIds.Dashboard, cancellationToken);
    }

    public async Task<DashboardUserDetailsResponse> DashboardGetUserDetailsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["userId"] = userId;
        return await GetJsonAsync<DashboardUserDetailsResponse>(
            $"{Constants.DashboardPaths.UserDetails}?{query}", Constants.RecipeIds.Dashboard, cancellationToken);
    }

    public async Task<DashboardSignOutResponse> DashboardDeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<DashboardUserDetailsRequest, DashboardSignOutResponse>(
            HttpMethod.Post, Constants.DashboardPaths.UserRemove, new DashboardUserDetailsRequest { UserId = userId }, Constants.RecipeIds.Dashboard, cancellationToken);
    }

    public async Task<DashboardSignOutResponse> DashboardVerifyUserEmailAsync(string userId, string? email, bool verified, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<DashboardUserEmailVerifyRequest, DashboardSignOutResponse>(
            HttpMethod.Post, Constants.DashboardPaths.UserEmailVerify, new DashboardUserEmailVerifyRequest { UserId = userId, Email = email, Verified = verified }, Constants.RecipeIds.Dashboard, cancellationToken);
    }

    public async Task<DashboardSignOutResponse> DashboardUpdateUserPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<DashboardUserPasswordRequest, DashboardSignOutResponse>(
            HttpMethod.Post, Constants.DashboardPaths.UserPassword, new DashboardUserPasswordRequest { UserId = userId, NewPassword = newPassword }, Constants.RecipeIds.Dashboard, cancellationToken);
    }

    public async Task<DashboardSignOutResponse> DashboardUpdateUserMetadataAsync(string userId, Dictionary<string, object> metadata, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<DashboardUserMetadataRequest, DashboardSignOutResponse>(
            HttpMethod.Put, Constants.DashboardPaths.UserMetadata, new DashboardUserMetadataRequest { UserId = userId, Metadata = metadata }, Constants.RecipeIds.Dashboard, cancellationToken);
    }

    public async Task<DashboardUserSessionsResponse> DashboardGetUserSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["userId"] = userId;
        return await GetJsonAsync<DashboardUserSessionsResponse>(
            $"{Constants.DashboardPaths.UserSessions}?{query}", Constants.RecipeIds.Dashboard, cancellationToken);
    }

    public async Task<DashboardSearchTagsResponse> DashboardSearchTagsAsync(string query, CancellationToken cancellationToken = default)
    {
        var qs = System.Web.HttpUtility.ParseQueryString(string.Empty);
        qs["q"] = query;
        return await GetJsonAsync<DashboardSearchTagsResponse>(
            $"{Constants.DashboardPaths.SearchTags}?{qs}", Constants.RecipeIds.Dashboard, cancellationToken);
    }

    public async Task<DashboardAnalyticsResponse> DashboardGetAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        return await GetJsonAsync<DashboardAnalyticsResponse>(
            Constants.DashboardPaths.Analytics, Constants.RecipeIds.Dashboard, cancellationToken);
    }

    public async Task<HttpResponseMessage> ProxyToCoreAsync(string method, string path, string? body, string recipeId, CancellationToken cancellationToken = default)
    {
        return await SendWithRetryAsync(new HttpMethod(method), path, body, recipeId, cancellationToken);
    }

    #endregion

    #region HTTP helpers

    private async Task<TResponse> GetJsonAsync<TResponse>(string pathAndQuery, string? rid, CancellationToken cancellationToken) where TResponse : new()
    {
        _logger.LogDebug("CDI GET {Path}", GetPathForLogging(pathAndQuery));
        using var response = await SendWithRetryAsync(HttpMethod.Get, pathAndQuery, null, rid, cancellationToken);
        return await DeserializeResponseAsync<TResponse>(response, cancellationToken);
    }

    private async Task<TResponse> SendJsonAsync<TRequest, TResponse>(HttpMethod method, string path, TRequest requestBody, string? rid, CancellationToken cancellationToken) where TResponse : new()
    {
        _logger.LogDebug("CDI {Method} {Path}", method, GetPathForLogging(path));
        var json = JsonSerializer.Serialize(requestBody, JsonOptions);
        using var response = await SendWithRetryAsync(method, path, json, rid, cancellationToken);
        return await DeserializeResponseAsync<TResponse>(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(HttpMethod method, string pathAndQuery, string? jsonBody, string? rid, CancellationToken cancellationToken)
    {
        var cdiVersion = await GetOrNegotiateCdiVersionAsync(cancellationToken);
        var isRecipePath = IsRecipePath(pathAndQuery);
        var pathForLogging = GetPathForLogging(pathAndQuery);

        Exception? lastException = null;
        var hostCount = _hosts.Count;

        for (var hostAttempt = 0; hostAttempt < hostCount; hostAttempt++)
        {
            var host = GetNextHost();
            var url = new Uri(host, pathAndQuery);
            _logger.LogDebug("CDI request to {Host}{Path}", host, pathForLogging);

            for (var retry = 0; retry <= Constants.RateLimitRetries; retry++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var request = new HttpRequestMessage(method, url);
                request.Headers.TryAddWithoutValidation(Constants.HeaderNames.CdiVersion, cdiVersion);

                if (!string.IsNullOrEmpty(rid) && isRecipePath)
                {
                    request.Headers.TryAddWithoutValidation(Constants.HeaderNames.Rid, rid);
                }

                if (!string.IsNullOrEmpty(jsonBody))
                {
                    request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                }

                try
                {
                    var response = await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                    if ((int)response.StatusCode == Constants.RateLimitStatusCode && retry < Constants.RateLimitRetries)
                    {
                        var delayMs = 10 + (250 * retry) + Random.Shared.Next(0, 100);
                        _logger.LogDebug("CDI rate limited; retrying in {DelayMs}ms", delayMs);
                        await Task.Delay(delayMs, cancellationToken);
                        continue;
                    }

                    return response;
                }
                catch (HttpRequestException ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "CDI request failed for host {Host}; failing over", host);
                    break; // try next host
                }
                catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "CDI request timed out for host {Host}; failing over", host);
                    break; // timeout, try next host
                }
            }
        }

        throw new SuperTokensException(
            "All SuperTokens Core hosts failed or the request was repeatedly rate limited.",
            lastException ?? new Exception("No hosts available."));
    }

    #endregion

    #region Response parsing and error handling

    private static async Task<TResponse> DeserializeResponseAsync<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken) where TResponse : new()
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            ThrowFromErrorBody(content, (int)response.StatusCode);
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return new TResponse();
        }

        using var document = JsonDocument.Parse(content);
        CheckStatusAndThrow(document);

        var result = JsonSerializer.Deserialize<TResponse>(content, JsonOptions);
        return result ?? new TResponse();
    }

    private static void CheckStatusAndThrow(JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("status", out var statusElement))
        {
            return;
        }

        var status = statusElement.GetString();
        if (string.IsNullOrEmpty(status) ||
            status.Equals(Constants.Status.Ok, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        ThrowFromStatus(document, status);
    }

    private static void ThrowFromErrorBody(string content, int httpStatusCode)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("status", out var statusElement))
            {
                var status = statusElement.GetString();
                if (!string.IsNullOrEmpty(status))
                {
                    ThrowFromStatus(document, status);
                    return;
                }
            }
        }
        catch (JsonException)
        {
            // body is not JSON, fall back to HTTP exception
        }

        throw new HttpRequestException(
            $"SuperTokens Core returned {httpStatusCode}: {TruncateForLogging(content)}",
            null,
            (HttpStatusCode)httpStatusCode);
    }

    private static void ThrowFromStatus(JsonDocument document, string status)
    {
        var message = document.RootElement.TryGetProperty("message", out var messageElement)
            ? messageElement.GetString() ?? status
            : status;

        if (status.Equals(Constants.Status.Unauthorized, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedException(message);
        }

        if (status.Equals(Constants.Status.TryRefreshToken, StringComparison.OrdinalIgnoreCase))
        {
            throw new TryRefreshTokenException(message);
        }

        if (status.Equals("NEEDS_REFRESH", StringComparison.OrdinalIgnoreCase))
        {
            throw new TryRefreshTokenException(message);
        }

        if (status.Equals(Constants.Status.TokenTheftDetected, StringComparison.OrdinalIgnoreCase))
        {
            var payload = document.RootElement.GetProperty("payload");
            var sessionHandle = payload.GetProperty("sessionHandle").GetString() ?? "";
            var userId = payload.GetProperty("userId").GetString() ?? "";
            throw new TokenTheftDetectedException(sessionHandle, userId);
        }

        if (status.Equals(Constants.Status.InvalidClaims, StringComparison.OrdinalIgnoreCase))
        {
            var invalidClaims = document.RootElement.GetProperty("invalidClaims").Deserialize<List<InvalidClaim>>(JsonOptions) ?? [];
            throw new InvalidClaimException(invalidClaims);
        }

        // Unknown status that is not OK: treat as generic SDK exception.
        throw new SuperTokensException($"SuperTokens Core returned status {status}: {message}");
    }

    #endregion

    #region CDI version negotiation

    private async Task<string> GetOrNegotiateCdiVersionAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_cdiVersion))
        {
            return _cdiVersion;
        }

        await _versionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(_cdiVersion))
            {
                return _cdiVersion;
            }

            _cdiVersion = await NegotiateCdiVersionAsync(cancellationToken);
            return _cdiVersion;
        }
        finally
        {
            _versionSemaphore.Release();
        }
    }

    private async Task<string> NegotiateCdiVersionAsync(CancellationToken cancellationToken)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(_options.ApiDomain))
        {
            query["apiDomain"] = _options.ApiDomain;
        }

        if (!string.IsNullOrWhiteSpace(_options.WebsiteDomain))
        {
            query["websiteDomain"] = _options.WebsiteDomain;
        }

        var pathAndQuery = $"{Constants.Paths.ApiVersion}?{query}";
        Exception? lastException = null;

        for (var hostAttempt = 0; hostAttempt < _hosts.Count; hostAttempt++)
        {
            var host = GetNextHost();
            var url = new Uri(host, pathAndQuery);

            try
            {
                using var response = await _httpClient.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var apiVersion = JsonSerializer.Deserialize<ApiVersionResponse>(content, JsonOptions);

                var matchedVersion = SelectHighestMatchingVersion(apiVersion?.Versions);
                if (!string.IsNullOrEmpty(matchedVersion))
                {
                    _logger.LogInformation("Negotiated CDI version {Version} with {Host}", matchedVersion, host);
                    return matchedVersion;
                }

                throw new SuperTokensException(
                    $"SuperTokens Core does not support any of the SDK CDI versions: {string.Join(", ", Constants.SupportedCdiVersions)}");
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "CDI version negotiation failed for host {Host}; failing over", host);
            }
        }

        throw new SuperTokensException(
            "Could not negotiate CDI version with any SuperTokens Core host.",
            lastException ?? new Exception("No hosts available."));
    }

    private static string? SelectHighestMatchingVersion(IEnumerable<string>? serverVersions)
    {
        if (serverVersions == null)
        {
            return null;
        }

        var supported = new HashSet<string>(Constants.SupportedCdiVersions, StringComparer.OrdinalIgnoreCase);
        return serverVersions
            .Where(v => supported.Contains(v))
            .OrderByDescending(ParseVersion)
            .FirstOrDefault();
    }

    private static (int Major, int Minor) ParseVersion(string version)
    {
        var parts = version.Split('.');
        var major = int.TryParse(parts.ElementAtOrDefault(0), out var m) ? m : 0;
        var minor = int.TryParse(parts.ElementAtOrDefault(1), out var n) ? n : 0;
        return (major, minor);
    }

    #endregion

    #region Host utilities

    private static IReadOnlyList<Uri> ParseHosts(string? coreUri)
    {
        if (string.IsNullOrWhiteSpace(coreUri))
        {
            throw new ArgumentException(
                "SuperTokens CoreUri is not configured. " +
                "Set SuperTokensOptions.CoreUri in your application configuration.");
        }

        var parts = coreUri.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var hosts = new List<Uri>();
        foreach (var part in parts)
        {
            if (Uri.TryCreate(part, UriKind.Absolute, out var uri))
            {
                hosts.Add(uri);
            }
            else
            {
                throw new ArgumentException($"Invalid SuperTokens Core URI: '{part}'");
            }
        }

        if (hosts.Count == 0)
        {
            throw new ArgumentException("At least one valid SuperTokens Core URI is required.");
        }

        return hosts.AsReadOnly();
    }

    private Uri GetNextHost()
    {
        if (_hosts.Count == 1)
        {
            return _hosts[0];
        }

        var index = (int)(Interlocked.Increment(ref _hostIndex) % _hosts.Count);
        return _hosts[index];
    }

    private static bool IsRecipePath(string path)
    {
        return path.StartsWith("/recipe/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates that a tenant identifier matches the safe character set
    /// <c>[a-zA-Z0-9_-]+</c> to prevent path traversal via the tenantId
    /// segment interpolated into CDI URL paths.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tenantId"/>
    /// is null, empty, or contains characters outside the allowed set.</exception>
    private static void ValidateTenantId(string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new ArgumentException(
                "tenantId must not be null or empty.", nameof(tenantId));
        }

        foreach (var c in tenantId)
        {
            var isAllowed =
                (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                (c >= '0' && c <= '9') ||
                c == '_' || c == '-';
            if (!isAllowed)
            {
                throw new ArgumentException(
                    $"tenantId '{tenantId}' contains invalid character '{c}'. " +
                    "Only letters, digits, underscore and hyphen are allowed.",
                    nameof(tenantId));
            }
        }
    }

    /// <summary>
    /// Returns the path portion of a URL without the query string, used to
    /// avoid logging PII (emails, user IDs) that may be present in query
    /// parameters.
    /// </summary>
    private static string GetPathForLogging(string pathAndQuery)
    {
        if (string.IsNullOrEmpty(pathAndQuery))
        {
            return pathAndQuery;
        }

        var queryIndex = pathAndQuery.IndexOf('?');
        return queryIndex >= 0 ? pathAndQuery[..queryIndex] : pathAndQuery;
    }

    /// <summary>
    /// Truncates a response body for inclusion in exception messages to avoid
    /// leaking large amounts of data or PII into logs.
    /// </summary>
    private static string TruncateForLogging(string content, int maxLength = 200)
    {
        if (string.IsNullOrEmpty(content))
        {
            return string.Empty;
        }

        return content.Length <= maxLength
            ? content
            : content[..maxLength] + "...(truncated)";
    }

    #endregion

    private class ApiVersionResponse
    {
        [JsonPropertyName("versions")]
        public List<string> Versions { get; set; } = [];
    }

    private async Task<Dictionary<string, object>?> VerifyJwtSignatureAsync(string accessToken, CancellationToken cancellationToken)
    {
        if (_hosts.Count == 0)
        {
            return null;
        }

        var coreUri = _hosts[0].ToString().TrimEnd('/');
        JsonWebKeySet? jwks;
        try
        {
            jwks = await _jwksClient!.GetKeysAsync(coreUri, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch JWKS from Core.");
            return null;
        }

        if (jwks == null)
        {
            return null;
        }

        // Extract kid from JWT header for proper key matching
        string? kid = null;
        try
        {
            var headerPart = accessToken.Split('.')[0];
            headerPart = headerPart.Replace('-', '+').Replace('_', '/');
            switch (headerPart.Length % 4)
            {
                case 2: headerPart += "=="; break;
                case 3: headerPart += "="; break;
            }
            var headerJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(headerPart));
            using var headerDoc = System.Text.Json.JsonDocument.Parse(headerJson);
            kid = headerDoc.RootElement.TryGetProperty("kid", out var kidElement) ? kidElement.GetString() : null;
        }
        catch
        {
            // If we can't parse the header, let validation fail naturally
        }

        // Select the specific key matching the kid, or fall back to all keys
        var signingKeys = jwks.GetSigningKeys();
        if (!string.IsNullOrEmpty(kid))
        {
            var matchingKey = signingKeys.FirstOrDefault(k => k.KeyId == kid);
            if (matchingKey != null)
            {
                signingKeys = [matchingKey];
            }
            else
            {
                // Token references a kid not in the JWKS - reject it
                throw new UnauthorizedException($"JWT key ID '{kid}' not found in JWKS.");
            }
        }

        var handler = new SystemIdentityModelTokensJwt.JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            IssuerSigningKeys = signingKeys,
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            // Restrict to RSA algorithms to prevent algorithm confusion attacks
            ValidAlgorithms = ["RS256", "RS384", "RS512"]
        };

        try
        {
            handler.ValidateToken(accessToken, parameters, out var securityToken);
            if (securityToken is not SystemIdentityModelTokensJwt.JwtSecurityToken jwtToken)
            {
                throw new UnauthorizedException("Access token signature could not be verified.");
            }

            return jwtToken.Payload
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value ?? (object)string.Empty);
        }
        catch (SecurityTokenException ex)
        {
            throw new UnauthorizedException($"Access token verification failed: {ex.Message}");
        }
    }
}
