using SuperTokensSDK.Net.Core;

namespace SuperTokensSDK.Net.Recipes.EmailVerification;

public class EmailVerificationRecipe
{
    private readonly ICoreApiClient _coreApiClient;

    public EmailVerificationRecipe(ICoreApiClient coreApiClient)
    {
        _coreApiClient = coreApiClient ?? throw new ArgumentNullException(nameof(coreApiClient));
    }

    public async Task<string> CreateEmailVerificationTokenAsync(string userId, string? email = null, string tenantId = "public", CancellationToken ct = default)
    {
        var response = await _coreApiClient.CreateEmailVerificationTokenAsync(userId, email, tenantId, ct);
        if (response.Status != Constants.Status.Ok || response.Token == null)
            throw new SuperTokensException($"Failed to create email verification token: {response.Status}");
        return response.Token;
    }

    public async Task<bool> VerifyEmailAsync(string token, string tenantId = "public", CancellationToken ct = default)
    {
        var response = await _coreApiClient.VerifyEmailAsync(token, tenantId, ct);
        return response.Status == Constants.Status.Ok && response.EmailVerified;
    }

    public async Task<bool> IsEmailVerifiedAsync(string userId, string? email = null, string tenantId = "public", CancellationToken ct = default)
    {
        var response = await _coreApiClient.IsEmailVerifiedAsync(userId, email, tenantId, ct);
        return response.Status == Constants.Status.Ok && response.IsVerified;
    }

    public async Task RevokeEmailVerificationTokensAsync(string userId, string? email = null, string tenantId = "public", CancellationToken ct = default)
    {
        await _coreApiClient.RevokeEmailVerificationTokensAsync(userId, email, tenantId, ct);
    }

    public async Task UnverifyEmailAsync(string userId, string? email = null, string tenantId = "public", CancellationToken ct = default)
    {
        await _coreApiClient.UnverifyEmailAsync(userId, email, tenantId, ct);
    }
}
