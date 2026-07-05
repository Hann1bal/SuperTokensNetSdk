using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;

namespace SuperTokensSDK.Net.Recipes.Multitenancy;

public class MultitenancyRecipe
{
    private readonly ICoreApiClient _coreApiClient;

    public MultitenancyRecipe(ICoreApiClient coreApiClient)
    {
        _coreApiClient = coreApiClient ?? throw new ArgumentNullException(nameof(coreApiClient));
    }

    public async Task<(TenantConfig config, bool createdNew)> CreateOrUpdateTenantAsync(string tenantId, TenantConfig config, CancellationToken ct = default)
    {
        var response = await _coreApiClient.CreateOrUpdateTenantAsync(tenantId, config, ct);
        if (response.Status != Constants.Status.Ok)
            throw new SuperTokensException($"Failed to create or update tenant: {response.Status}");
        return (config, response.CreatedNew);
    }

    public async Task<bool> DeleteTenantAsync(string tenantId, bool? deleteConditional = null, CancellationToken ct = default)
    {
        var response = await _coreApiClient.DeleteTenantAsync(tenantId, deleteConditional, ct);
        return response.Status == Constants.Status.Ok && response.DidExist;
    }

    public async Task<TenantConfig?> GetTenantAsync(string tenantId, CancellationToken ct = default)
    {
        var response = await _coreApiClient.GetTenantAsync(tenantId, ct);
        return response.TenantConfig;
    }

    public async Task<List<TenantConfig>> ListAllTenantsAsync(CancellationToken ct = default)
    {
        var response = await _coreApiClient.ListAllTenantsAsync(ct);
        return response.Tenants ?? new();
    }

    public async Task CreateOrUpdateThirdPartyConfigAsync(string tenantId, Dictionary<string, object> config, CancellationToken ct = default)
    {
        await _coreApiClient.CreateOrUpdateThirdPartyConfigAsync(tenantId, config, ct);
    }

    public async Task DeleteThirdPartyConfigAsync(string tenantId, string thirdPartyId, CancellationToken ct = default)
    {
        await _coreApiClient.DeleteThirdPartyConfigAsync(tenantId, thirdPartyId, ct);
    }

    public async Task<AssociateUserResponse> AssociateUserToTenantAsync(string tenantId, string userId, CancellationToken ct = default)
    {
        return await _coreApiClient.AssociateUserToTenantAsync(tenantId, userId, ct);
    }

    public async Task<bool> DisassociateUserFromTenantAsync(string tenantId, string userId, CancellationToken ct = default)
    {
        return await _coreApiClient.DisassociateUserFromTenantAsync(tenantId, userId, ct);
    }
}
