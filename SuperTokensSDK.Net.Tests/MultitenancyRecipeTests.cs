using Moq;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;
using SuperTokensSDK.Net.Recipes.Multitenancy;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class MultitenancyRecipeTests
{
    private readonly Mock<ICoreApiClient> _coreMock = new();

    [Fact]
    public async Task CreateOrUpdateTenantAsync_CallsCore_AndReturnsConfig()
    {
        var config = new TenantConfig
        {
            TenantId = "t1",
            CoreConfig = new Dictionary<string, object> { ["key"] = "value" }
        };
        _coreMock.Setup(c => c.CreateOrUpdateTenantAsync("t1", config, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateOrUpdateTenantResponse { Status = "OK", CreatedNew = true });

        var recipe = new MultitenancyRecipe(_coreMock.Object);
        var (resultConfig, createdNew) = await recipe.CreateOrUpdateTenantAsync("t1", config);

        Assert.Equal("t1", resultConfig.TenantId);
        Assert.True(createdNew);
        _coreMock.Verify(c => c.CreateOrUpdateTenantAsync("t1", config, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdateTenantAsync_Throws_WhenStatusNotOk()
    {
        var config = new TenantConfig { TenantId = "t1" };
        _coreMock.Setup(c => c.CreateOrUpdateTenantAsync("t1", config, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateOrUpdateTenantResponse { Status = "TENANT_ID_ALREADY_EXISTS" });

        var recipe = new MultitenancyRecipe(_coreMock.Object);

        await Assert.ThrowsAsync<SuperTokensException>(() => recipe.CreateOrUpdateTenantAsync("t1", config));
    }

    [Fact]
    public async Task DeleteTenantAsync_CallsCore_AndReturnsTrue()
    {
        _coreMock.Setup(c => c.DeleteTenantAsync("t1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteTenantResponse { Status = "OK", DidExist = true });

        var recipe = new MultitenancyRecipe(_coreMock.Object);
        var result = await recipe.DeleteTenantAsync("t1", true);

        Assert.True(result);
        _coreMock.Verify(c => c.DeleteTenantAsync("t1", true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTenantAsync_CallsCore_AndReturnsConfig()
    {
        var config = new TenantConfig { TenantId = "t1" };
        _coreMock.Setup(c => c.GetTenantAsync("t1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTenantResponse { Status = "OK", TenantConfig = config });

        var recipe = new MultitenancyRecipe(_coreMock.Object);
        var result = await recipe.GetTenantAsync("t1");

        Assert.NotNull(result);
        Assert.Equal("t1", result!.TenantId);
    }

    [Fact]
    public async Task ListAllTenantsAsync_CallsCore_AndReturnsTenants()
    {
        var tenants = new List<TenantConfig> { new() { TenantId = "t1" }, new() { TenantId = "t2" } };
        _coreMock.Setup(c => c.ListAllTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListTenantsResponse { Status = "OK", Tenants = tenants });

        var recipe = new MultitenancyRecipe(_coreMock.Object);
        var result = await recipe.ListAllTenantsAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task CreateOrUpdateThirdPartyConfigAsync_CallsCore()
    {
        var config = new Dictionary<string, object> { ["thirdPartyId"] = "google" };
        _coreMock.Setup(c => c.CreateOrUpdateThirdPartyConfigAsync("t1", config, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var recipe = new MultitenancyRecipe(_coreMock.Object);
        await recipe.CreateOrUpdateThirdPartyConfigAsync("t1", config);

        _coreMock.Verify(c => c.CreateOrUpdateThirdPartyConfigAsync("t1", config, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteThirdPartyConfigAsync_CallsCore()
    {
        _coreMock.Setup(c => c.DeleteThirdPartyConfigAsync("t1", "google", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var recipe = new MultitenancyRecipe(_coreMock.Object);
        await recipe.DeleteThirdPartyConfigAsync("t1", "google");

        _coreMock.Verify(c => c.DeleteThirdPartyConfigAsync("t1", "google", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssociateUserToTenantAsync_CallsCore_AndReturnsResponse()
    {
        _coreMock.Setup(c => c.AssociateUserToTenantAsync("t1", "u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssociateUserResponse { Status = "OK", DidTenantExist = true });

        var recipe = new MultitenancyRecipe(_coreMock.Object);
        var result = await recipe.AssociateUserToTenantAsync("t1", "u1");

        Assert.True(result.DidTenantExist);
        _coreMock.Verify(c => c.AssociateUserToTenantAsync("t1", "u1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisassociateUserFromTenantAsync_CallsCore_AndReturnsTrue()
    {
        _coreMock.Setup(c => c.DisassociateUserFromTenantAsync("t1", "u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var recipe = new MultitenancyRecipe(_coreMock.Object);
        var result = await recipe.DisassociateUserFromTenantAsync("t1", "u1");

        Assert.True(result);
        _coreMock.Verify(c => c.DisassociateUserFromTenantAsync("t1", "u1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_NullCoreApiClient_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MultitenancyRecipe(null!));
    }
}
