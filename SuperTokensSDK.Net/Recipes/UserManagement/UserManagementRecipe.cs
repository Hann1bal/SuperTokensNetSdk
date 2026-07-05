using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;

namespace SuperTokensSDK.Net.Recipes.UserManagement;

/// <summary>
/// SuperTokens cross-recipe user management operations.
/// </summary>
public class UserManagementRecipe
{
    private readonly ICoreApiClient _coreApiClient;

    public UserManagementRecipe(ICoreApiClient coreApiClient)
    {
        _coreApiClient = coreApiClient ?? throw new ArgumentNullException(nameof(coreApiClient));
    }

    public async Task<(List<UserListItem> users, string? nextToken)> GetUsersAsync(
        int limit = 100,
        string? paginationToken = null,
        string timeJoinedOrder = "DESC",
        CancellationToken cancellationToken = default)
    {
        var response = await _coreApiClient.GetUsersAsync(limit, paginationToken, timeJoinedOrder, cancellationToken);
        return (response.Users, response.NextPaginationToken);
    }

    public async Task<int> GetUserCountAsync(CancellationToken cancellationToken = default)
    {
        var response = await _coreApiClient.GetUserCountAsync(cancellationToken);
        return response.Count;
    }

    public async Task<bool> DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var response = await _coreApiClient.DeleteUserAsync(
            new DeleteUserRequest { UserId = userId }, cancellationToken);
        return response.Status == Constants.Status.Ok;
    }
}
