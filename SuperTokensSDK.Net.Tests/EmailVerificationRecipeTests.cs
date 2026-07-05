using Moq;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;
using SuperTokensSDK.Net.Recipes.EmailVerification;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class EmailVerificationRecipeTests
{
    private readonly Mock<ICoreApiClient> _coreMock = new();

    [Fact]
    public async Task CreateEmailVerificationTokenAsync_CallsCore_AndReturnsToken()
    {
        _coreMock.Setup(c => c.CreateEmailVerificationTokenAsync("u1", "a@b.com", "public", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateEmailVerificationTokenResponse { Status = "OK", Token = "verify-token" });

        var recipe = new EmailVerificationRecipe(_coreMock.Object);
        var token = await recipe.CreateEmailVerificationTokenAsync("u1", "a@b.com");

        Assert.Equal("verify-token", token);
        _coreMock.Verify(c => c.CreateEmailVerificationTokenAsync("u1", "a@b.com", "public", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateEmailVerificationTokenAsync_Throws_WhenTokenIsNull()
    {
        _coreMock.Setup(c => c.CreateEmailVerificationTokenAsync("u1", null, "public", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateEmailVerificationTokenResponse { Status = "OK" });

        var recipe = new EmailVerificationRecipe(_coreMock.Object);

        await Assert.ThrowsAsync<SuperTokensException>(() => recipe.CreateEmailVerificationTokenAsync("u1"));
    }

    [Fact]
    public async Task VerifyEmailAsync_CallsCore_AndReturnsTrue()
    {
        _coreMock.Setup(c => c.VerifyEmailAsync("token", "public", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerifyEmailResponse { Status = "OK", EmailVerified = true });

        var recipe = new EmailVerificationRecipe(_coreMock.Object);
        var result = await recipe.VerifyEmailAsync("token");

        Assert.True(result);
        _coreMock.Verify(c => c.VerifyEmailAsync("token", "public", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyEmailAsync_ReturnsFalse_WhenNotVerified()
    {
        _coreMock.Setup(c => c.VerifyEmailAsync("bad-token", "public", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerifyEmailResponse { Status = "EMAIL_VERIFICATION_INVALID_TOKEN_ERROR", EmailVerified = false });

        var recipe = new EmailVerificationRecipe(_coreMock.Object);
        var result = await recipe.VerifyEmailAsync("bad-token");

        Assert.False(result);
    }

    [Fact]
    public async Task IsEmailVerifiedAsync_CallsCore_AndReturnsTrue()
    {
        _coreMock.Setup(c => c.IsEmailVerifiedAsync("u1", "a@b.com", "public", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IsEmailVerifiedResponse { Status = "OK", IsVerified = true });

        var recipe = new EmailVerificationRecipe(_coreMock.Object);
        var result = await recipe.IsEmailVerifiedAsync("u1", "a@b.com");

        Assert.True(result);
        _coreMock.Verify(c => c.IsEmailVerifiedAsync("u1", "a@b.com", "public", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeEmailVerificationTokensAsync_CallsCore()
    {
        _coreMock.Setup(c => c.RevokeEmailVerificationTokensAsync("u1", "a@b.com", "public", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var recipe = new EmailVerificationRecipe(_coreMock.Object);
        await recipe.RevokeEmailVerificationTokensAsync("u1", "a@b.com");

        _coreMock.Verify(c => c.RevokeEmailVerificationTokensAsync("u1", "a@b.com", "public", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnverifyEmailAsync_CallsCore()
    {
        _coreMock.Setup(c => c.UnverifyEmailAsync("u1", "a@b.com", "public", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var recipe = new EmailVerificationRecipe(_coreMock.Object);
        await recipe.UnverifyEmailAsync("u1", "a@b.com");

        _coreMock.Verify(c => c.UnverifyEmailAsync("u1", "a@b.com", "public", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_NullCoreApiClient_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new EmailVerificationRecipe(null!));
    }
}
