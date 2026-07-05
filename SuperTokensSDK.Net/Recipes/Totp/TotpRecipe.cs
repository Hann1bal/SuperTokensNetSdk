using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;

namespace SuperTokensSDK.Net.Recipes.Totp;

/// <summary>
/// SuperTokens TOTP recipe: creates and verifies TOTP devices and codes
/// for two-factor authentication.
/// </summary>
public class TotpRecipe
{
    private readonly ICoreApiClient _coreApiClient;

    public TotpRecipe(ICoreApiClient coreApiClient)
    {
        _coreApiClient = coreApiClient ?? throw new ArgumentNullException(nameof(coreApiClient));
    }

    public async Task<(string secret, string deviceName)> CreateDeviceAsync(
        string userId, string? deviceName = null, int skew = 0, int period = 30,
        CancellationToken ct = default)
    {
        var response = await _coreApiClient.CreateTotpDeviceAsync(
            new CreateTotpDeviceRequest { UserId = userId, DeviceName = deviceName, Skew = skew, Period = period }, ct);
        if (response.Status != Constants.Status.Ok || response.Secret == null)
            throw new SuperTokensException($"Failed to create TOTP device: {response.Status}");
        return (response.Secret, response.DeviceName ?? deviceName ?? "");
    }

    public async Task<bool> VerifyDeviceAsync(string userId, string deviceName, string totp, CancellationToken ct = default)
    {
        var response = await _coreApiClient.VerifyTotpDeviceAsync(
            new VerifyTotpDeviceRequest { UserId = userId, DeviceName = deviceName, Totp = totp }, ct);
        return response.Status == Constants.Status.Ok;
    }

    public async Task<bool> VerifyCodeAsync(string userId, string totp, CancellationToken ct = default)
    {
        var response = await _coreApiClient.VerifyTotpCodeAsync(
            new VerifyTotpCodeRequest { UserId = userId, Totp = totp, AllowUnverifiedDevices = false }, ct);
        return response.Status == Constants.Status.Ok;
    }

    public async Task<List<TotpDevice>> ListDevicesAsync(string userId, CancellationToken ct = default)
    {
        var response = await _coreApiClient.ListTotpDevicesAsync(userId, ct);
        return response.Devices ?? new();
    }

    public async Task<bool> RemoveDeviceAsync(string userId, string deviceName, CancellationToken ct = default)
    {
        var response = await _coreApiClient.RemoveTotpDeviceAsync(
            new RemoveTotpDeviceRequest { UserId = userId, DeviceName = deviceName }, ct);
        return response.DidDeviceExist;
    }
}
