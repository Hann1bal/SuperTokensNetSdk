using Moq;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;
using SuperTokensSDK.Net.Recipes.Passwordless;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class PasswordlessRecipeTests
{
    private readonly Mock<ICoreApiClient> _coreMock = new();

    [Fact]
    public void Constructor_NullCoreApiClient_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new PasswordlessRecipe(null!));
    }

    [Fact]
    public void ValidateAndNormalizePhoneNumber_ValidNumber_ReturnsE164()
    {
        var recipe = new PasswordlessRecipe(_coreMock.Object);
        var result = recipe.ValidateAndNormalizePhoneNumber("+14155551234");

        Assert.NotNull(result);
        Assert.Equal("+14155551234", result);
    }

    [Fact]
    public void ValidateAndNormalizePhoneNumber_InvalidNumber_Throws()
    {
        var recipe = new PasswordlessRecipe(_coreMock.Object);
        var ex = Assert.Throws<SuperTokensException>(() => recipe.ValidateAndNormalizePhoneNumber("not-a-number"));
        Assert.Contains("invalid", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAndNormalizePhoneNumber_Null_ReturnsNull()
    {
        var recipe = new PasswordlessRecipe(_coreMock.Object);
        var result = recipe.ValidateAndNormalizePhoneNumber(null);
        Assert.Null(result);
    }

    [Fact]
    public void ValidateAndNormalizePhoneNumber_CustomValidator_ReturnsResult()
    {
        var recipe = new PasswordlessRecipe(_coreMock.Object)
        {
            Overrides = new PasswordlessOverrides
            {
                ValidatePhoneNumber = (phone, tenant) => null // accept anything
            }
        };

        var result = recipe.ValidateAndNormalizePhoneNumber("+14155551234");
        Assert.NotNull(result);
        Assert.Equal("+14155551234", result);
    }

    [Fact]
    public void ValidateAndNormalizePhoneNumber_CustomValidatorRejects_Throws()
    {
        var recipe = new PasswordlessRecipe(_coreMock.Object)
        {
            Overrides = new PasswordlessOverrides
            {
                ValidatePhoneNumber = (phone, tenant) => "banned"
            }
        };

        var ex = Assert.Throws<SuperTokensException>(() => recipe.ValidateAndNormalizePhoneNumber("+14155551234"));
        Assert.Contains("banned", ex.Message);
    }

    [Fact]
    public async Task CreateCodeAsync_CallsCore_WithNormalizedPhoneNumber()
    {
        _coreMock.Setup(c => c.CreatePasswordlessCodeAsync(It.IsAny<CreateCodeRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateCodeResponse
            {
                Status = "OK",
                DeviceId = "dev1",
                PreAuthSessionId = "pre1",
                LinkCode = "link1"
            });

        var recipe = new PasswordlessRecipe(_coreMock.Object);
        var (deviceId, preAuthSessionId, linkCode) = await recipe.CreateCodeAsync(phoneNumber: "+14155551234");

        Assert.Equal("dev1", deviceId);
        Assert.Equal("pre1", preAuthSessionId);
        Assert.Equal("link1", linkCode);

        _coreMock.Verify(c => c.CreatePasswordlessCodeAsync(
            It.Is<CreateCodeRequest>(r => r.PhoneNumber == "+14155551234"),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCodeAsync_WithInvalidPhoneNumber_Throws()
    {
        var recipe = new PasswordlessRecipe(_coreMock.Object);
        await Assert.ThrowsAsync<SuperTokensException>(() => recipe.CreateCodeAsync(phoneNumber: "garbage"));
    }

    [Fact]
    public async Task ConsumeCodeAsync_CallsCore_AndReturnsUser()
    {
        var user = new PasswordlessUser { Id = "u1", Email = "a@b.com" };
        _coreMock.Setup(c => c.ConsumePasswordlessCodeAsync(It.IsAny<ConsumeCodeRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConsumeCodeResponse { Status = "OK", User = user });

        var recipe = new PasswordlessRecipe(_coreMock.Object);
        var result = await recipe.ConsumeCodeAsync("pre1", linkCode: "link1");

        Assert.Equal("u1", result.Id);
        _coreMock.Verify(c => c.ConsumePasswordlessCodeAsync(
            It.Is<ConsumeCodeRequest>(r => r.PreAuthSessionId == "pre1" && r.LinkCode == "link1"),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
