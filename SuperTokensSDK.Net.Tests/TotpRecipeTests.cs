using Moq;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;
using SuperTokensSDK.Net.Recipes.Totp;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class TotpRecipeTests
{
    private readonly Mock<ICoreApiClient> _coreMock = new();

    [Fact]
    public void Constructor_NullCoreApiClient_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TotpRecipe(null!));
    }

    [Fact]
    public async Task CreateDeviceAsync_CallsCore()
    {
        _coreMock.Setup(c => c.CreateTotpDeviceAsync(It.IsAny<CreateTotpDeviceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateTotpDeviceResponse
            {
                Status = "OK",
                Secret = "secret-abc",
                DeviceName = "my-device"
            });

        var recipe = new TotpRecipe(_coreMock.Object);
        var (secret, deviceName) = await recipe.CreateDeviceAsync("u1", "my-device");

        Assert.Equal("secret-abc", secret);
        Assert.Equal("my-device", deviceName);

        _coreMock.Verify(c => c.CreateTotpDeviceAsync(
            It.Is<CreateTotpDeviceRequest>(r =>
                r.UserId == "u1" && r.DeviceName == "my-device" && r.Period == 30 && r.Skew == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateDeviceAsync_CoreReturnsError_Throws()
    {
        _coreMock.Setup(c => c.CreateTotpDeviceAsync(It.IsAny<CreateTotpDeviceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateTotpDeviceResponse { Status = "UNKNOWN_USER_ID_ERROR" });

        var recipe = new TotpRecipe(_coreMock.Object);
        await Assert.ThrowsAsync<SuperTokensException>(() => recipe.CreateDeviceAsync("u1"));
    }

    [Fact]
    public async Task VerifyDeviceAsync_CallsCore()
    {
        _coreMock.Setup(c => c.VerifyTotpDeviceAsync(It.IsAny<VerifyTotpDeviceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerifyTotpDeviceResponse { Status = "OK", WasAlreadyVerified = false });

        var recipe = new TotpRecipe(_coreMock.Object);
        var result = await recipe.VerifyDeviceAsync("u1", "dev1", "123456");

        Assert.True(result);
        _coreMock.Verify(c => c.VerifyTotpDeviceAsync(
            It.Is<VerifyTotpDeviceRequest>(r =>
                r.UserId == "u1" && r.DeviceName == "dev1" && r.Totp == "123456"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyDeviceAsync_CoreReturnsError_ReturnsFalse()
    {
        _coreMock.Setup(c => c.VerifyTotpDeviceAsync(It.IsAny<VerifyTotpDeviceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerifyTotpDeviceResponse { Status = "INVALID_TOTP_ERROR" });

        var recipe = new TotpRecipe(_coreMock.Object);
        var result = await recipe.VerifyDeviceAsync("u1", "dev1", "000000");

        Assert.False(result);
    }

    [Fact]
    public async Task ListDevicesAsync_CallsCore()
    {
        var devices = new List<TotpDevice>
        {
            new() { Name = "d1", Period = 30, Skew = 0, Verified = true },
            new() { Name = "d2", Period = 60, Skew = 1, Verified = false }
        };
        _coreMock.Setup(c => c.ListTotpDevicesAsync("u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListTotpDevicesResponse { Status = "OK", Devices = devices });

        var recipe = new TotpRecipe(_coreMock.Object);
        var result = await recipe.ListDevicesAsync("u1");

        Assert.Equal(2, result.Count);
        Assert.Equal("d1", result[0].Name);
        Assert.True(result[0].Verified);
        Assert.Equal("d2", result[1].Name);
        Assert.False(result[1].Verified);

        _coreMock.Verify(c => c.ListTotpDevicesAsync("u1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListDevicesAsync_CoreReturnsNullDevices_ReturnsEmptyList()
    {
        _coreMock.Setup(c => c.ListTotpDevicesAsync("u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListTotpDevicesResponse { Status = "OK", Devices = null! });

        var recipe = new TotpRecipe(_coreMock.Object);
        var result = await recipe.ListDevicesAsync("u1");

        Assert.Empty(result);
    }

    [Fact]
    public async Task RemoveDeviceAsync_CallsCore()
    {
        _coreMock.Setup(c => c.RemoveTotpDeviceAsync(It.IsAny<RemoveTotpDeviceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveTotpDeviceResponse { Status = "OK", DidDeviceExist = true });

        var recipe = new TotpRecipe(_coreMock.Object);
        var result = await recipe.RemoveDeviceAsync("u1", "dev1");

        Assert.True(result);
        _coreMock.Verify(c => c.RemoveTotpDeviceAsync(
            It.Is<RemoveTotpDeviceRequest>(r => r.UserId == "u1" && r.DeviceName == "dev1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveDeviceAsync_DeviceDoesNotExist_ReturnsFalse()
    {
        _coreMock.Setup(c => c.RemoveTotpDeviceAsync(It.IsAny<RemoveTotpDeviceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveTotpDeviceResponse { Status = "OK", DidDeviceExist = false });

        var recipe = new TotpRecipe(_coreMock.Object);
        var result = await recipe.RemoveDeviceAsync("u1", "missing");

        Assert.False(result);
    }

    [Fact]
    public async Task VerifyCodeAsync_CallsCore()
    {
        _coreMock.Setup(c => c.VerifyTotpCodeAsync(It.IsAny<VerifyTotpCodeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerifyTotpCodeResponse { Status = "OK" });

        var recipe = new TotpRecipe(_coreMock.Object);
        var result = await recipe.VerifyCodeAsync("u1", "123456");

        Assert.True(result);
        _coreMock.Verify(c => c.VerifyTotpCodeAsync(
            It.Is<VerifyTotpCodeRequest>(r =>
                r.UserId == "u1" && r.Totp == "123456" && r.AllowUnverifiedDevices == false),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyCodeAsync_CoreReturnsError_ReturnsFalse()
    {
        _coreMock.Setup(c => c.VerifyTotpCodeAsync(It.IsAny<VerifyTotpCodeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerifyTotpCodeResponse { Status = "INVALID_TOTP_ERROR" });

        var recipe = new TotpRecipe(_coreMock.Object);
        var result = await recipe.VerifyCodeAsync("u1", "000000");

        Assert.False(result);
    }
}
