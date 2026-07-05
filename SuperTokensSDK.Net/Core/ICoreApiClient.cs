using SuperTokensSDK.Net.Core.Models;

namespace SuperTokensSDK.Net.Core;

/// <summary>
/// Client for the SuperTokens Core Driver Interface (CDI).
/// </summary>
public interface ICoreApiClient
{
    Task<CreateOrRefreshAPIResponse> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);
    Task<GetSessionResponse> VerifySessionAsync(VerifySessionRequest request, CancellationToken cancellationToken = default);
    Task<CreateOrRefreshAPIResponse> RefreshSessionAsync(RefreshSessionRequest request, CancellationToken cancellationToken = default);
    Task<RevokeSessionResponse> RevokeSessionAsync(RevokeSessionRequest request, CancellationToken cancellationToken = default);
    Task<SignUpResponse> SignUpAsync(SignUpRequest request, CancellationToken cancellationToken = default);
    Task<SignUpResponse> SignInAsync(SignUpRequest request, CancellationToken cancellationToken = default);
    Task<StatusResponse> ResetPasswordAsync(PasswordResetRequest request, CancellationToken cancellationToken = default);
    Task<GetUserResponse> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<GetUserResponse> GetUserByEmailAsync(string email, string tenantId = "public", CancellationToken cancellationToken = default);
    Task<GeneratePasswordResetTokenResponse> GeneratePasswordResetTokenAsync(GeneratePasswordResetTokenRequest request, CancellationToken cancellationToken = default);
    Task<StatusResponse> UpdateEmailOrPasswordAsync(UpdateEmailOrPasswordRequest request, CancellationToken cancellationToken = default);
    Task<EmailExistsResponse> EmailExistsAsync(string email, string tenantId = "public", CancellationToken cancellationToken = default);
    Task<StatusResponse> AddUserRolesAsync(UserRolesRequest request, CancellationToken cancellationToken = default);
    Task<UserRolesResponse> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);
    Task<StatusResponse> RemoveUserRolesAsync(UserRolesRequest request, CancellationToken cancellationToken = default);
    Task<RoleExistsResponse> DoesRoleExistAsync(string userId, string role, CancellationToken cancellationToken = default);
    Task<UsersThatHaveRoleResponse> GetUsersThatHaveRoleAsync(string role, string tenantId = "public", int? limit = null, string? timeJoinedOrder = null, string? paginationToken = null, CancellationToken cancellationToken = default);
    Task<UserRolesCreateResponse> CreateNewRoleOrAddPermissionsAsync(UserRolesCreateRequest request, CancellationToken cancellationToken = default);
    Task<PermissionsForRoleResponse> GetPermissionsForRoleAsync(string role, CancellationToken cancellationToken = default);
    Task<StatusResponse> RemovePermissionsFromRoleAsync(RemovePermissionsRequest request, CancellationToken cancellationToken = default);
    Task<RolesWithPermissionResponse> GetRolesThatHavePermissionAsync(string permission, CancellationToken cancellationToken = default);
    Task<DeleteRoleResponse> DeleteRoleAsync(DeleteRoleRequest request, CancellationToken cancellationToken = default);
    Task<AllRolesResponse> GetAllRolesAsync(CancellationToken cancellationToken = default);
    Task<UserMetadataResponse> GetUserMetadataAsync(string userId, CancellationToken cancellationToken = default);
    Task<StatusResponse> UpdateUserMetadataAsync(UserMetadataUpdateRequest request, CancellationToken cancellationToken = default);
    Task<StatusResponse> UpdateJwtDataAsync(UpdateJwtDataRequest request, CancellationToken cancellationToken = default);
    Task<CreateTotpDeviceResponse> CreateTotpDeviceAsync(CreateTotpDeviceRequest request, CancellationToken cancellationToken = default);
    Task<VerifyTotpDeviceResponse> VerifyTotpDeviceAsync(VerifyTotpDeviceRequest request, CancellationToken cancellationToken = default);
    Task<VerifyTotpCodeResponse> VerifyTotpCodeAsync(VerifyTotpCodeRequest request, CancellationToken cancellationToken = default);
    Task<ListTotpDevicesResponse> ListTotpDevicesAsync(string userId, CancellationToken cancellationToken = default);
    Task<RemoveTotpDeviceResponse> RemoveTotpDeviceAsync(RemoveTotpDeviceRequest request, CancellationToken cancellationToken = default);

    // Passwordless recipe
    Task<CreateCodeResponse> CreatePasswordlessCodeAsync(CreateCodeRequest request, string tenantId = "public", CancellationToken cancellationToken = default);
    Task<ConsumeCodeResponse> ConsumePasswordlessCodeAsync(ConsumeCodeRequest request, string tenantId = "public", CancellationToken cancellationToken = default);

    // Email verification recipe
    Task<CreateEmailVerificationTokenResponse> CreateEmailVerificationTokenAsync(string userId, string? email, string tenantId = "public", CancellationToken cancellationToken = default);
    Task<VerifyEmailResponse> VerifyEmailAsync(string token, string tenantId = "public", CancellationToken cancellationToken = default);
    Task<IsEmailVerifiedResponse> IsEmailVerifiedAsync(string userId, string? email, string tenantId = "public", CancellationToken cancellationToken = default);
    Task RevokeEmailVerificationTokensAsync(string userId, string? email, string tenantId = "public", CancellationToken cancellationToken = default);
    Task UnverifyEmailAsync(string userId, string? email, string tenantId = "public", CancellationToken cancellationToken = default);

    // JWT recipe
    Task<CreateJwtResponse> CreateJwtAsync(Dictionary<string, object> payload, int validityInSeconds = 3600, string? useStaticSigningKey = null, string tenantId = "public", CancellationToken cancellationToken = default);
    Task<JwksResponse> GetJwksAsync(CancellationToken cancellationToken = default);

    // Multitenancy recipe
    Task<CreateOrUpdateTenantResponse> CreateOrUpdateTenantAsync(string tenantId, TenantConfig config, CancellationToken cancellationToken = default);
    Task<DeleteTenantResponse> DeleteTenantAsync(string tenantId, bool? deleteConditional = null, CancellationToken cancellationToken = default);
    Task<GetTenantResponse> GetTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<ListTenantsResponse> ListAllTenantsAsync(CancellationToken cancellationToken = default);
    Task CreateOrUpdateThirdPartyConfigAsync(string tenantId, Dictionary<string, object> config, CancellationToken cancellationToken = default);
    Task DeleteThirdPartyConfigAsync(string tenantId, string thirdPartyId, CancellationToken cancellationToken = default);
    Task<AssociateUserResponse> AssociateUserToTenantAsync(string tenantId, string userId, CancellationToken cancellationToken = default);
    Task<bool> DisassociateUserFromTenantAsync(string tenantId, string userId, CancellationToken cancellationToken = default);

    // Session management
    Task<List<string>> GetAllSessionHandlesForUserAsync(string userId, string tenantId = "public", bool fetchAcrossAllTenants = false, CancellationToken cancellationToken = default);
    Task<SessionInfo?> GetSessionInformationAsync(string sessionHandle, CancellationToken cancellationToken = default);
    Task<List<string>> RevokeMultipleSessionsAsync(List<string> sessionHandles, CancellationToken cancellationToken = default);
    Task<List<string>> RevokeAllSessionsForUserAsync(string userId, string tenantId = "public", bool revokeAcrossAllTenants = false, CancellationToken cancellationToken = default);

    // User management
    Task<UserListResponse> GetUsersAsync(int limit = 100, string? paginationToken = null, string timeJoinedOrder = "DESC", CancellationToken cancellationToken = default);
    Task<UserCountResponse> GetUserCountAsync(CancellationToken cancellationToken = default);
    Task<StatusResponse> DeleteUserAsync(DeleteUserRequest request, CancellationToken cancellationToken = default);
}
