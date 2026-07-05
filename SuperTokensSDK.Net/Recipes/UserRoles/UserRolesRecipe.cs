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

    public async Task<List<string>> GetPermissionsForRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        var response = await _coreApiClient.GetPermissionsForRoleAsync(role, cancellationToken);
        return response.Permissions;
    }

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var roles = await GetRolesAsync(userId, cancellationToken);
        var permissions = new HashSet<string>(StringComparer.Ordinal);

        foreach (var role in roles)
        {
            var rolePermissions = await GetPermissionsForRoleAsync(role, cancellationToken);
            foreach (var permission in rolePermissions)
            {
                permissions.Add(permission);
            }
        }

        return permissions.ToList().AsReadOnly();
    }

    public async Task<List<string>> GetUsersThatHaveRoleAsync(
        string role,
        string tenantId = "public",
        int? limit = null,
        string? paginationToken = null,
        string? timeJoinedOrder = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _coreApiClient.GetUsersThatHaveRoleAsync(
            role, tenantId, limit, timeJoinedOrder, paginationToken, cancellationToken);
        return response.Users;
    }

    public async Task<bool> CreateNewRoleOrAddPermissionsAsync(string role, List<string> permissions, CancellationToken cancellationToken = default)
    {
        var response = await _coreApiClient.CreateNewRoleOrAddPermissionsAsync(
            new UserRolesCreateRequest { Role = role, Permissions = permissions }, cancellationToken);
        return response.CreatedNewRole;
    }

    public async Task RemovePermissionsFromRoleAsync(string role, List<string> permissions, CancellationToken cancellationToken = default)
    {
        await _coreApiClient.RemovePermissionsFromRoleAsync(
            new RemovePermissionsRequest { Role = role, Permissions = permissions }, cancellationToken);
    }

    public async Task<List<string>> GetRolesThatHavePermissionAsync(string permission, CancellationToken cancellationToken = default)
    {
        var response = await _coreApiClient.GetRolesThatHavePermissionAsync(permission, cancellationToken);
        return response.Roles;
    }

    public async Task<bool> DeleteRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        var response = await _coreApiClient.DeleteRoleAsync(new DeleteRoleRequest { Role = role }, cancellationToken);
        return response.DidRoleExist;
    }

    public async Task<List<string>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _coreApiClient.GetAllRolesAsync(cancellationToken);
        return response.Roles;
    }
}
