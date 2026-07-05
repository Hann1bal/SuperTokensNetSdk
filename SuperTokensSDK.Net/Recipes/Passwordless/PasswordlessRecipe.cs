using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;

namespace SuperTokensSDK.Net.Recipes.Passwordless;

public class PasswordlessRecipe
{
    private readonly ICoreApiClient _coreApiClient;

    public PasswordlessRecipe(ICoreApiClient coreApiClient)
    {
        _coreApiClient = coreApiClient ?? throw new ArgumentNullException(nameof(coreApiClient));
    }

    public async Task<(string deviceId, string preAuthSessionId, string linkCode)> CreateCodeAsync(string? email = null, string? phoneNumber = null, string? deviceId = null, string tenantId = "public", CancellationToken ct = default)
    {
        var response = await _coreApiClient.CreatePasswordlessCodeAsync(
            new CreateCodeRequest { Email = email, PhoneNumber = phoneNumber, DeviceId = deviceId }, tenantId, ct);
        if (response.Status != "OK")
            throw new SuperTokensException($"Failed to create passwordless code: {response.Status}");
        return (response.DeviceId ?? "", response.PreAuthSessionId ?? "", response.LinkCode ?? "");
    }

    public async Task<PasswordlessUser> ConsumeCodeAsync(string preAuthSessionId, string? linkCode = null, string? deviceId = null, string? userInputCode = null, string tenantId = "public", CancellationToken ct = default)
    {
        var response = await _coreApiClient.ConsumePasswordlessCodeAsync(
            new ConsumeCodeRequest { PreAuthSessionId = preAuthSessionId, LinkCode = linkCode, DeviceId = deviceId, UserInputCode = userInputCode }, tenantId, ct);
        if (response.Status != "OK")
            throw new SuperTokensException($"Failed to consume passwordless code: {response.Status}");
        return response.User ?? throw new SuperTokensException("ConsumeCode returned OK but no user");
    }
}
