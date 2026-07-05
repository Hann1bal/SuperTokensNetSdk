using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;

namespace SuperTokensSDK.Net.Recipes.Dashboard;

/// <summary>
/// SuperTokens Dashboard recipe operations for admin sign-in, user
/// management, tenant listing and analytics.
/// </summary>
public class DashboardRecipe
{
    private readonly ICoreApiClient _coreApiClient;

    public DashboardRecipe(ICoreApiClient coreApiClient)
    {
        _coreApiClient = coreApiClient ?? throw new ArgumentNullException(nameof(coreApiClient));
    }

    public async Task<string?> SignInAsync(string apiKey, CancellationToken ct = default)
    {
        var response = await _coreApiClient.DashboardSignInAsync(apiKey, ct);
        return response.Status == Constants.Status.Ok ? response.Session : null;
    }

    public async Task<bool> SignOutAsync(CancellationToken ct = default)
    {
        var response = await _coreApiClient.DashboardSignOutAsync(ct);
        return response.Status == Constants.Status.Ok;
    }

    public async Task<(List<DashboardUser> users, string? nextToken)> UsersGetAsync(
        int? limit = null,
        string? paginationToken = null,
        string timeJoinedOrder = "DESC",
        CancellationToken ct = default)
    {
        var response = await _coreApiClient.DashboardGetUsersAsync(limit, paginationToken, timeJoinedOrder, ct);
        return (response.Users, response.NextPaginationToken);
    }

    public async Task<int> UsersCountGetAsync(CancellationToken ct = default)
    {
        var response = await _coreApiClient.DashboardGetUsersCountAsync(ct);
        return response.Count;
    }

    public async Task<List<DashboardTenantInfo>> TenantsListAsync(CancellationToken ct = default)
    {
        var response = await _coreApiClient.DashboardListTenantsAsync(ct);
        return response.Tenants;
    }

    public async Task<Dictionary<string, object>?> UserDetailsGetAsync(string userId, CancellationToken ct = default)
    {
        var response = await _coreApiClient.DashboardGetUserDetailsAsync(userId, ct);
        return response.User;
    }

    public async Task<bool> UserRemoveAsync(string userId, CancellationToken ct = default)
    {
        var response = await _coreApiClient.DashboardDeleteUserAsync(userId, ct);
        return response.Status == Constants.Status.Ok;
    }

    public async Task<bool> UserEmailVerifyAsync(string userId, string? email, bool verified, CancellationToken ct = default)
    {
        var response = await _coreApiClient.DashboardVerifyUserEmailAsync(userId, email, verified, ct);
        return response.Status == Constants.Status.Ok;
    }

    public async Task<bool> UserPasswordUpdateAsync(string userId, string newPassword, CancellationToken ct = default)
    {
        var response = await _coreApiClient.DashboardUpdateUserPasswordAsync(userId, newPassword, ct);
        return response.Status == Constants.Status.Ok;
    }

    public async Task<bool> UserMetadataUpdateAsync(string userId, Dictionary<string, object> metadata, CancellationToken ct = default)
    {
        var response = await _coreApiClient.DashboardUpdateUserMetadataAsync(userId, metadata, ct);
        return response.Status == Constants.Status.Ok;
    }

    public async Task<List<DashboardSessionInfo>> UserSessionsGetAsync(string userId, CancellationToken ct = default)
    {
        var response = await _coreApiClient.DashboardGetUserSessionsAsync(userId, ct);
        return response.Sessions;
    }

    public async Task<List<string>> SearchTagsAsync(string query, CancellationToken ct = default)
    {
        var response = await _coreApiClient.DashboardSearchTagsAsync(query, ct);
        return response.Tags;
    }

    public async Task<Dictionary<string, object>?> AnalyticsGetAsync(CancellationToken ct = default)
    {
        var response = await _coreApiClient.DashboardGetAnalyticsAsync(ct);
        return response.Data;
    }
}
