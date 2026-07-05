using Moq;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;
using SuperTokensSDK.Net.Recipes.UserMetadata;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class UserMetadataRecipeTests
{
    private readonly Mock<ICoreApiClient> _coreMock = new();

    [Fact]
    public async Task GetMetadataAsync_CallsCore_AndReturnsMetadata()
    {
        var metadata = new Dictionary<string, object> { ["theme"] = "dark" };
        _coreMock.Setup(c => c.GetUserMetadataAsync("u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserMetadataResponse { Status = "OK", Metadata = metadata });

        var recipe = new UserMetadataRecipe(_coreMock.Object);
        var result = await recipe.GetMetadataAsync("u1");

        Assert.NotNull(result);
        Assert.Equal("dark", result!["theme"]);
        _coreMock.Verify(c => c.GetUserMetadataAsync("u1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMetadataAsync_ReturnsNull_WhenMetadataIsNull()
    {
        _coreMock.Setup(c => c.GetUserMetadataAsync("u2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserMetadataResponse { Status = "OK", Metadata = null });

        var recipe = new UserMetadataRecipe(_coreMock.Object);
        var result = await recipe.GetMetadataAsync("u2");

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateMetadataAsync_CallsCore_WithCorrectRequest()
    {
        _coreMock.Setup(c => c.UpdateUserMetadataAsync(It.IsAny<UserMetadataUpdateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatusResponse { Status = "OK" });

        var recipe = new UserMetadataRecipe(_coreMock.Object);
        await recipe.UpdateMetadataAsync("u3", new Dictionary<string, object> { ["newKey"] = "newValue" });

        _coreMock.Verify(c => c.UpdateUserMetadataAsync(
            It.Is<UserMetadataUpdateRequest>(r => r.UserId == "u3" && r.MetadataUpdate != null && (string)r.MetadataUpdate["newKey"]! == "newValue"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMetadataAsAsync_DeserializesMetadata()
    {
        _coreMock.Setup(c => c.GetUserMetadataAsync("u4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserMetadataResponse
            {
                Status = "OK",
                Metadata = new Dictionary<string, object> { ["Name"] = "Alice", ["Age"] = 30 }
            });

        var recipe = new UserMetadataRecipe(_coreMock.Object);
        var result = await recipe.GetMetadataAsAsync<UserProfile>("u4");

        Assert.NotNull(result);
        Assert.Equal("Alice", result!.Name);
        Assert.Equal(30, result.Age);
    }

    [Fact]
    public async Task GetMetadataAsAsync_ReturnsNull_WhenMetadataIsNull()
    {
        _coreMock.Setup(c => c.GetUserMetadataAsync("u5", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserMetadataResponse { Status = "OK", Metadata = null });

        var recipe = new UserMetadataRecipe(_coreMock.Object);
        var result = await recipe.GetMetadataAsAsync<UserProfile>("u5");

        Assert.Null(result);
    }

    [Fact]
    public void Constructor_NullCoreApiClient_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new UserMetadataRecipe(null!));
    }

    private class UserProfile
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }
}
