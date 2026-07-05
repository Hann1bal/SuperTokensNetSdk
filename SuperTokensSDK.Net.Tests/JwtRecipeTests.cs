using Moq;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;
using SuperTokensSDK.Net.Recipes.Jwt;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class JwtRecipeTests
{
    private readonly Mock<ICoreApiClient> _coreMock = new();

    [Fact]
    public async Task CreateJwtAsync_CallsCore_AndReturnsJwt()
    {
        var payload = new Dictionary<string, object> { ["foo"] = "bar" };
        _coreMock.Setup(c => c.CreateJwtAsync(payload, 3600, null, "public", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateJwtResponse { Status = "OK", Jwt = "jwt-token" });

        var recipe = new JwtRecipe(_coreMock.Object);
        var result = await recipe.CreateJwtAsync(payload);

        Assert.Equal("jwt-token", result);
        _coreMock.Verify(c => c.CreateJwtAsync(payload, 3600, null, "public", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateJwtAsync_Throws_WhenStatusNotOk()
    {
        var payload = new Dictionary<string, object>();
        _coreMock.Setup(c => c.CreateJwtAsync(payload, 3600, null, "public", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateJwtResponse { Status = "GENERAL_ERROR" });

        var recipe = new JwtRecipe(_coreMock.Object);

        await Assert.ThrowsAsync<SuperTokensException>(() => recipe.CreateJwtAsync(payload));
    }

    [Fact]
    public async Task GetJwksAsync_CallsCore_AndReturnsResponse()
    {
        var jwks = new JwksResponse { Keys = [new JsonWebKey { Kid = "k1" }] };
        _coreMock.Setup(c => c.GetJwksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(jwks);

        var recipe = new JwtRecipe(_coreMock.Object);
        var result = await recipe.GetJwksAsync();

        Assert.Single(result.Keys);
        Assert.Equal("k1", result.Keys[0].Kid);
        _coreMock.Verify(c => c.GetJwksAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_NullCoreApiClient_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new JwtRecipe(null!));
    }
}
