using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;

namespace SuperTokensSDK.Net.Recipes.ThirdParty;

/// <summary>
/// SuperTokens ThirdParty recipe: signs users in/up with OAuth providers
/// (Google, GitHub, Apple, etc.) and resolves users by id, third-party info
/// or email.
/// </summary>
public class ThirdPartyRecipe
{
    private readonly ICoreApiClient _coreApiClient;

    public ThirdPartyRecipe(ICoreApiClient coreApiClient)
    {
        _coreApiClient = coreApiClient ?? throw new ArgumentNullException(nameof(coreApiClient));
    }

    public async Task<(ThirdPartyUser user, bool createdNewUser)> SignInUpAsync(ThirdPartyInfo thirdPartyInfo, string oauthTokens, string tenantId = "public", CancellationToken ct = default)
    {
        var response = await _coreApiClient.ThirdPartySignInUpAsync(
            new SignInUpRequest { ThirdParty = thirdPartyInfo, OauthTokens = oauthTokens }, tenantId, ct);
        if (response.Status != Constants.Status.Ok || response.User == null)
            throw new SuperTokensException($"ThirdParty sign-in/up failed: {response.Status}");
        return (response.User, response.CreatedNewUser);
    }

    public async Task<(ThirdPartyUser user, bool createdNewUser)> ManuallyCreateOrUpdateUserAsync(string thirdPartyId, string thirdPartyUserId, string? email, string tenantId = "public", CancellationToken ct = default)
    {
        var response = await _coreApiClient.ManuallyCreateOrUpdateThirdPartyUserAsync(
            new ManuallyCreateOrUpdateUserRequest { ThirdPartyId = thirdPartyId, ThirdPartyUserId = thirdPartyUserId, Email = email }, tenantId, ct);
        if (response.Status != Constants.Status.Ok || response.User == null)
            throw new SuperTokensException($"ThirdParty manually create/update failed: {response.Status}");
        return (response.User, response.CreatedNewUser);
    }

    public async Task<ThirdPartyUser?> GetUserByIdAsync(string userId, CancellationToken ct = default)
    {
        return await _coreApiClient.GetThirdPartyUserByIdAsync(userId, ct);
    }

    public async Task<ThirdPartyUser?> GetUserByThirdPartyInfoAsync(string thirdPartyId, string thirdPartyUserId, string tenantId = "public", CancellationToken ct = default)
    {
        return await _coreApiClient.GetThirdPartyUserByThirdPartyInfoAsync(
            new ThirdPartyInfo { ThirdPartyId = thirdPartyId, ThirdPartyUserId = thirdPartyUserId }, tenantId, ct);
    }

    public async Task<List<UserByEmailItem>> GetUsersByEmailAsync(string email, string tenantId = "public", CancellationToken ct = default)
    {
        var response = await _coreApiClient.GetThirdPartyUsersByEmailAsync(email, tenantId, ct);
        if (response.Status != Constants.Status.Ok)
            throw new SuperTokensException($"ThirdParty get users by email failed: {response.Status}");
        return response.Users;
    }

    public TypeProvider GetProvider(string providerId, TypeProviderConfig config)
    {
        return providerId switch
        {
            "google" => BuiltInProviders.Google(config),
            "github" => BuiltInProviders.Github(config),
            "apple" => BuiltInProviders.Apple(config),
            "discord" => BuiltInProviders.Discord(config),
            "facebook" => BuiltInProviders.Facebook(config),
            "gitlab" => BuiltInProviders.GitLab(config),
            _ => new TypeProvider { Id = providerId, Config = config }
        };
    }
}
