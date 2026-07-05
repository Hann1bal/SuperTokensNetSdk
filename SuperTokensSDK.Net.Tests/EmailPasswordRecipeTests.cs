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
    public async Task GetUserByIdAsync_CallsCore_AndReturnsUser()
    {
        var user = new UserResponse { Id = "u4", Email = "d@e.com", TimeJoined = 4 };
        _coreMock.Setup(c => c.GetUserByIdAsync("u4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetUserResponse { Status = "OK", User = user });

        var recipe = new EmailPasswordRecipe(_coreMock.Object);
        var result = await recipe.GetUserByIdAsync("u4");

        Assert.NotNull(result);
        Assert.Equal("d@e.com", result!.Email);
        _coreMock.Verify(c => c.GetUserByIdAsync("u4", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsNull_WhenUserIsNull()
    {
        _coreMock.Setup(c => c.GetUserByIdAsync("u-missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetUserResponse { Status = "OK", User = null });

        var recipe = new EmailPasswordRecipe(_coreMock.Object);
        var result = await recipe.GetUserByIdAsync("u-missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByEmailAsync_CallsCore_AndReturnsUser()
    {
        var user = new UserResponse { Id = "u5", Email = "e@f.com", TimeJoined = 5 };
        _coreMock.Setup(c => c.GetUserByEmailAsync("e@f.com", "public", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetUserResponse { Status = "OK", User = user });

        var recipe = new EmailPasswordRecipe(_coreMock.Object);
        var result = await recipe.GetUserByEmailAsync("e@f.com");

        Assert.NotNull(result);
        Assert.Equal("u5", result!.Id);
        _coreMock.Verify(c => c.GetUserByEmailAsync("e@f.com", "public", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GeneratePasswordResetTokenAsync_CallsCore_AndReturnsToken()
    {
        _coreMock.Setup(c => c.GeneratePasswordResetTokenAsync(It.IsAny<GeneratePasswordResetTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeneratePasswordResetTokenResponse { Status = "OK", Token = "reset-token" });

        var recipe = new EmailPasswordRecipe(_coreMock.Object);
        var token = await recipe.GeneratePasswordResetTokenAsync("u6");

        Assert.Equal("reset-token", token);
        _coreMock.Verify(c => c.GeneratePasswordResetTokenAsync(
            It.Is<GeneratePasswordResetTokenRequest>(r => r.UserId == "u6"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateEmailOrPasswordAsync_CallsCore_WithCorrectRequest()
    {
        _coreMock.Setup(c => c.UpdateEmailOrPasswordAsync(It.IsAny<UpdateEmailOrPasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatusResponse { Status = "OK" });

        var recipe = new EmailPasswordRecipe(_coreMock.Object);
        await recipe.UpdateEmailOrPasswordAsync("u7", "new@example.com", "newpass");

        _coreMock.Verify(c => c.UpdateEmailOrPasswordAsync(
            It.Is<UpdateEmailOrPasswordRequest>(r => r.UserId == "u7" && r.Email == "new@example.com" && r.Password == "newpass"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EmailExistsAsync_CallsCore_AndReturnsExists()
    {
        _coreMock.Setup(c => c.EmailExistsAsync("known@example.com", "public", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailExistsResponse { Status = "OK", Exists = true });

        var recipe = new EmailPasswordRecipe(_coreMock.Object);
        var exists = await recipe.EmailExistsAsync("known@example.com");

        Assert.True(exists);
        _coreMock.Verify(c => c.EmailExistsAsync("known@example.com", "public", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_NullCoreApiClient_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new EmailPasswordRecipe(null!));
    }
}
