using Moq;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;
using SuperTokensSDK.Net.Recipes.ThirdParty;
using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class ThirdPartyRecipeTests
{
    private readonly Mock<ICoreApiClient> _coreMock = new();

    [Fact]
    public async Task SignInUpAsync_CallsCore_AndReturnsUser()
    {
        var thirdPartyInfo = new ThirdPartyInfo { ThirdPartyId = "google", ThirdPartyUserId = "g123", Email = "a@b.com" };
        var expectedUser = new ThirdPartyUser { Id = "u1", Email = "a@b.com" };
        _coreMock.Setup(c => c.ThirdPartySignInUpAsync(
                It.Is<SignInUpRequest>(r => r.ThirdParty == thirdPartyInfo && r.OauthTokens == "token"),
                "public",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignInUpResponse { Status = "OK", CreatedNewUser = true, User = expectedUser });

        var recipe = new ThirdPartyRecipe(_coreMock.Object);
        var (user, createdNewUser) = await recipe.SignInUpAsync(thirdPartyInfo, "token");

        Assert.Equal(expectedUser, user);
        Assert.True(createdNewUser);
    }

    [Fact]
    public async Task SignInUpAsync_Throws_WhenStatusNotOk()
    {
        var thirdPartyInfo = new ThirdPartyInfo { ThirdPartyId = "google", ThirdPartyUserId = "g123" };
        _coreMock.Setup(c => c.ThirdPartySignInUpAsync(It.IsAny<SignInUpRequest>(), "public", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignInUpResponse { Status = "NO_EMAIL_GIVEN" });

        var recipe = new ThirdPartyRecipe(_coreMock.Object);

        await Assert.ThrowsAsync<SuperTokensException>(() => recipe.SignInUpAsync(thirdPartyInfo, "token"));
    }

    [Fact]
    public async Task ManuallyCreateOrUpdateUserAsync_CallsCore_AndReturnsUser()
    {
        var expectedUser = new ThirdPartyUser { Id = "u1", Email = "a@b.com" };
        _coreMock.Setup(c => c.ManuallyCreateOrUpdateThirdPartyUserAsync(
                It.Is<ManuallyCreateOrUpdateUserRequest>(r => r.ThirdPartyId == "google" && r.ThirdPartyUserId == "g123" && r.Email == "a@b.com"),
                "public",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ManuallyCreateOrUpdateUserResponse { Status = "OK", CreatedNewUser = true, User = expectedUser });

        var recipe = new ThirdPartyRecipe(_coreMock.Object);
        var (user, createdNewUser) = await recipe.ManuallyCreateOrUpdateUserAsync("google", "g123", "a@b.com");

        Assert.Equal(expectedUser, user);
        Assert.True(createdNewUser);
    }

    [Fact]
    public async Task ManuallyCreateOrUpdateUserAsync_Throws_WhenStatusNotOk()
    {
        _coreMock.Setup(c => c.ManuallyCreateOrUpdateThirdPartyUserAsync(It.IsAny<ManuallyCreateOrUpdateUserRequest>(), "public", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ManuallyCreateOrUpdateUserResponse { Status = "EMAIL_ALREADY_EXISTS_ERROR" });

        var recipe = new ThirdPartyRecipe(_coreMock.Object);

        await Assert.ThrowsAsync<SuperTokensException>(() => recipe.ManuallyCreateOrUpdateUserAsync("google", "g123", "a@b.com"));
    }

    [Fact]
    public async Task GetUserByIdAsync_CallsCore_AndReturnsUser()
    {
        var expectedUser = new ThirdPartyUser { Id = "u1" };
        _coreMock.Setup(c => c.GetThirdPartyUserByIdAsync("u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUser);

        var recipe = new ThirdPartyRecipe(_coreMock.Object);
        var result = await recipe.GetUserByIdAsync("u1");

        Assert.Equal(expectedUser, result);
    }

    [Fact]
    public async Task GetUserByThirdPartyInfoAsync_CallsCore_AndReturnsUser()
    {
        var expectedUser = new ThirdPartyUser { Id = "u1" };
        _coreMock.Setup(c => c.GetThirdPartyUserByThirdPartyInfoAsync(
                It.Is<ThirdPartyInfo>(i => i.ThirdPartyId == "google" && i.ThirdPartyUserId == "g123"),
                "public",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUser);

        var recipe = new ThirdPartyRecipe(_coreMock.Object);
        var result = await recipe.GetUserByThirdPartyInfoAsync("google", "g123");

        Assert.Equal(expectedUser, result);
    }

    [Fact]
    public async Task GetUsersByEmailAsync_CallsCore_AndReturnsUsers()
    {
        var users = new List<UserByEmailItem> { new() { RecipeId = "thirdparty", User = new ThirdPartyUser { Id = "u1" } } };
        _coreMock.Setup(c => c.GetThirdPartyUsersByEmailAsync("a@b.com", "public", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetUsersByEmailResponse { Status = "OK", Users = users });

        var recipe = new ThirdPartyRecipe(_coreMock.Object);
        var result = await recipe.GetUsersByEmailAsync("a@b.com");

        Assert.Single(result);
        Assert.Equal("u1", result[0].User.Id);
    }

    [Fact]
    public async Task GetUsersByEmailAsync_Throws_WhenStatusNotOk()
    {
        _coreMock.Setup(c => c.GetThirdPartyUsersByEmailAsync("a@b.com", "public", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetUsersByEmailResponse { Status = "ERROR" });

        var recipe = new ThirdPartyRecipe(_coreMock.Object);

        await Assert.ThrowsAsync<SuperTokensException>(() => recipe.GetUsersByEmailAsync("a@b.com"));
    }

    [Theory]
    [InlineData("google", "https://accounts.google.com/o/oauth2/v2/auth")]
    [InlineData("github", "https://github.com/login/oauth/authorize")]
    [InlineData("apple", "https://appleid.apple.com/auth/authorize")]
    [InlineData("discord", "https://discord.com/api/oauth2/authorize")]
    [InlineData("facebook", "https://www.facebook.com/v18.0/dialog/oauth")]
    [InlineData("gitlab", "https://gitlab.com/oauth/authorize")]
    public void GetProvider_BuiltInProviders_ReturnsCorrectEndpoints(string providerId, string expectedAuthorizationEndpoint)
    {
        var recipe = new ThirdPartyRecipe(_coreMock.Object);
        var provider = recipe.GetProvider(providerId, new TypeProviderConfig { ClientId = "id" });

        Assert.Equal(providerId, provider.Id);
        Assert.Equal(expectedAuthorizationEndpoint, provider.Config.AuthorizationEndpoint);
        Assert.Equal("id", provider.Config.ClientId);
    }

    [Fact]
    public void GetProvider_UnknownId_ReturnsCustomProvider()
    {
        var recipe = new ThirdPartyRecipe(_coreMock.Object);
        var provider = recipe.GetProvider("custom", new TypeProviderConfig { ClientId = "id" });

        Assert.Equal("custom", provider.Id);
        Assert.Equal("id", provider.Config.ClientId);
    }

    [Fact]
    public void Constructor_NullCoreApiClient_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ThirdPartyRecipe(null!));
    }
}
