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
    Task<StatusResponse> AddUserRolesAsync(UserRolesRequest request, CancellationToken cancellationToken = default);
    Task<UserRolesResponse> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);
    Task<StatusResponse> RemoveUserRolesAsync(UserRolesRequest request, CancellationToken cancellationToken = default);
    Task<RoleExistsResponse> DoesRoleExistAsync(string userId, string role, CancellationToken cancellationToken = default);
    Task<UserMetadataResponse> GetUserMetadataAsync(string userId, CancellationToken cancellationToken = default);
    Task<StatusResponse> UpdateUserMetadataAsync(UserMetadataUpdateRequest request, CancellationToken cancellationToken = default);
    Task<CreateTotpDeviceResponse> CreateTotpDeviceAsync(CreateTotpDeviceRequest request, CancellationToken cancellationToken = default);
    Task<VerifyTotpDeviceResponse> VerifyTotpDeviceAsync(VerifyTotpDeviceRequest request, CancellationToken cancellationToken = default);
    Task<VerifyTotpCodeResponse> VerifyTotpCodeAsync(VerifyTotpCodeRequest request, CancellationToken cancellationToken = default);
    Task<ListTotpDevicesResponse> ListTotpDevicesAsync(string userId, CancellationToken cancellationToken = default);
    Task<RemoveTotpDeviceResponse> RemoveTotpDeviceAsync(RemoveTotpDeviceRequest request, CancellationToken cancellationToken = default);
}
