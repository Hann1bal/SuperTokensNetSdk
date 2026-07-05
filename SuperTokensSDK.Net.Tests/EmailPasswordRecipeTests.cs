using Moq;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;
using SuperTokensSDK.Net.Recipes.EmailPassword;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class EmailPasswordRecipeTests
{
    private readonly Mock<ICoreApiClient> _coreMock = new();

    [Fact]
    public async Task SignUpAsync_CallsCore_WithEmailAndPassword()
    {
        var user = new UserResponse { Id = "u1", Email = "a@b.com", TimeJoined = 1 };
        _coreMock.Setup(c => c.SignUpAsync(It.IsAny<SignUpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignUpResponse { Status = "OK", User = user });

        var recipe = new EmailPasswordRecipe(_coreMock.Object);
        var result = await recipe.SignUpAsync("a@b.com", "password123");

        Assert.NotNull(result);
        Assert.Equal("u1", result!.Id);
        Assert.Equal("a@b.com", result.Email);
        _coreMock.Verify(c => c.SignUpAsync(
            It.Is<SignUpRequest>(r => r.Email == "a@b.com" && r.Password == "password123"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SignUpAsync_ReturnsNull_WhenUserIsNull()
    {
        _coreMock.Setup(c => c.SignUpAsync(It.IsAny<SignUpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignUpResponse { Status = "OK", User = null });

        var recipe = new EmailPasswordRecipe(_coreMock.Object);
        var result = await recipe.SignUpAsync("a@b.com", "password123");

        Assert.Null(result);
    }

    [Fact]
    public async Task SignInAsync_CallsCore_AndReturnsUser()
    {
        var user = new UserResponse { Id = "u2", Email = "c@d.com", TimeJoined = 2 };
        _coreMock.Setup(c => c.SignInAsync(It.IsAny<SignUpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignUpResponse { Status = "OK", User = user });

        var recipe = new EmailPasswordRecipe(_coreMock.Object);
        var result = await recipe.SignInAsync("c@d.com", "secret");

        Assert.NotNull(result);
        Assert.Equal("u2", result!.Id);
        _coreMock.Verify(c => c.SignInAsync(
            It.Is<SignUpRequest>(r => r.Email == "c@d.com" && r.Password == "secret"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_CallsCore_WithUserIdAndPassword()
    {
        _coreMock.Setup(c => c.ResetPasswordAsync(It.IsAny<PasswordResetRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatusResponse { Status = "OK" });

        var recipe = new EmailPasswordRecipe(_coreMock.Object);
        await recipe.ResetPasswordAsync("u3", "newpass");

        _coreMock.Verify(c => c.ResetPasswordAsync(
            It.Is<PasswordResetRequest>(r => r.UserId == "u3" && r.NewPassword == "newpass"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_NullCoreApiClient_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new EmailPasswordRecipe(null!));
    }
}
