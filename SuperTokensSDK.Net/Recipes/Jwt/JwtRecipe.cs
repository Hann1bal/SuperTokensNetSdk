using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;

namespace SuperTokensSDK.Net.Recipes.Jwt;

/// <summary>
/// SuperTokens JWT recipe: creates short-lived JWTs signed by Core and
/// exposes the public JWKS for signature verification.
/// </summary>
public class JwtRecipe
{
    private readonly ICoreApiClient _coreApiClient;

    public JwtRecipe(ICoreApiClient coreApiClient)
    {
        _coreApiClient = coreApiClient ?? throw new ArgumentNullException(nameof(coreApiClient));
    }

    public async Task<string> CreateJwtAsync(Dictionary<string, object> payload, int validityInSeconds = 3600, string? useStaticSigningKey = null, string tenantId = "public", CancellationToken ct = default)
    {
        var response = await _coreApiClient.CreateJwtAsync(payload, validityInSeconds, useStaticSigningKey, tenantId, ct);
        if (response.Status != Constants.Status.Ok || response.Jwt == null)
            throw new SuperTokensException($"Failed to create JWT: {response.Status}");
        return response.Jwt;
    }

    public async Task<JwksResponse> GetJwksAsync(CancellationToken ct = default)
    {
        return await _coreApiClient.GetJwksAsync(ct);
    }
}
