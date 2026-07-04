using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;

namespace SuperTokensSDK.Net.Recipes.UserRoles;

/// <summary>
/// SuperTokens UserRoles recipe operations.
/// </summary>
public class UserRolesRecipe
{
    private readonly ICoreApiClient _coreApiClient;

    public UserRolesRecipe(ICoreApiClient coreApiClient)
    {
        _coreApiClient = coreApiClient ?? throw new ArgumentNullException(nameof(coreApiClient));
    }

    public async Task AddRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
    {
        await _coreApiClient.AddUserRolesAsync(new UserRolesRequest { UserId = userId, Roles = [role] }, cancellationToken);
    }

    public async Task AddRolesAsync(string userId, IEnumerable<string> roles, CancellationToken cancellationToken = default)
    {
        await _coreApiClient.AddUserRolesAsync(new UserRolesRequest { UserId = userId, Roles = roles.ToList() }, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var response = await _coreApiClient.GetUserRolesAsync(userId, cancellationToken);
        return response.Roles.AsReadOnly();
    }

    public async Task RemoveRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
    {
        await _coreApiClient.RemoveUserRolesAsync(new UserRolesRequest { UserId = userId, Roles = [role] }, cancellationToken);
    }

    public async Task RemoveRolesAsync(string userId, IEnumerable<string> roles, CancellationToken cancellationToken = default)
    {
        await _coreApiClient.RemoveUserRolesAsync(new UserRolesRequest { UserId = userId, Roles = roles.ToList() }, cancellationToken);
    }

    public async Task<bool> DoesRoleExistAsync(string userId, string role, CancellationToken cancellationToken = default)
    {
        var response = await _coreApiClient.DoesRoleExistAsync(userId, role, cancellationToken);
        return response.DoesRoleExist;
    }
}
