using Moq;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;
using SuperTokensSDK.Net.Recipes.EmailPassword;
using SuperTokensSDK.Net.Recipes.Passwordless;
using SuperTokensSDK.Net.Recipes.Session;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class RecipeOverridesTests
{
    private readonly Mock<ICoreApiClient> _coreMock = new();

    [Fact]
    public async Task EmailPasswordSignUpAsync_OverrideTakesPrecedence()
    {
        var recipe = new EmailPasswordRecipe(_coreMock.Object)
        {
            Overrides = new EmailPasswordOverrides
            {
                SignUp = (email, password, ct) =>
                    Task.FromResult<UserResponse?>(new UserResponse { Id = "override-id", Email = email, TimeJoined = 1 })
            }
        };

        var result = await recipe.SignUpAsync("a@b.com", "secret");

        Assert.NotNull(result);
        Assert.Equal("override-id", result!.Id);
        _coreMock.Verify(c => c.SignUpAsync(It.IsAny<SignUpRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EmailPasswordSignUpAsync_NoOverride_CallsCore()
    {
        _coreMock.Setup(c => c.SignUpAsync(It.IsAny<SignUpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignUpResponse { Status = "OK", User = new UserResponse { Id = "core-id" } });
        var recipe = new EmailPasswordRecipe(_coreMock.Object);

        var result = await recipe.SignUpAsync("a@b.com", "secret");

        Assert.Equal("core-id", result?.Id);
        _coreMock.Verify(c => c.SignUpAsync(It.IsAny<SignUpRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SessionCreateSessionAsync_OverrideTakesPrecedence()
    {
        var recipe = new SessionRecipe(_coreMock.Object)
        {
            Overrides = new SessionOverrides
            {
                CreateSession = (userId, jwtPayload, sessionData, ct) =>
                    Task.FromResult(new SessionContainer("override-handle", userId, jwtPayload))
            }
        };

        var result = await recipe.CreateSessionAsync("u1", new Dictionary<string, object> { ["x"] = 1 }, new Dictionary<string, object> { ["y"] = 2 });

        Assert.Equal("u1", result.UserId);
        Assert.Equal("override-handle", result.SessionHandle);
        _coreMock.Verify(c => c.CreateSessionAsync(It.IsAny<CreateSessionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SessionVerifySessionAsync_OverrideTakesPrecedence()
    {
        var recipe = new SessionRecipe(_coreMock.Object)
        {
            Overrides = new SessionOverrides
            {
                VerifySession = (token, antiCsrf, ct) =>
                    Task.FromResult(new SessionContainer("sh", "override-user", new Dictionary<string, object>()))
            }
        };

        var result = await recipe.VerifySessionAsync("jwt", "csrf");

        Assert.Equal("override-user", result.UserId);
        _coreMock.Verify(c => c.VerifySessionAsync(It.IsAny<VerifySessionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SessionRevokeSessionAsync_OverrideTakesPrecedence()
    {
        var recipe = new SessionRecipe(_coreMock.Object)
        {
            Overrides = new SessionOverrides
            {
                RevokeSession = (handle, ct) => Task.FromResult(true)
            }
        };

        var result = await recipe.RevokeSessionAsync("sh");

        Assert.True(result);
        _coreMock.Verify(c => c.RevokeMultipleSessionsAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PasswordlessConsumeCodeAsync_OverrideTakesPrecedence()
    {
        var recipe = new PasswordlessRecipe(_coreMock.Object)
        {
            Overrides = new PasswordlessOverrides
            {
                ConsumeCode = (preAuthSessionId, linkCode, deviceId, userInputCode, tenantId, ct) =>
                    Task.FromResult(new PasswordlessUser { Id = "override-user", Email = "a@b.com" })
            }
        };

        var result = await recipe.ConsumeCodeAsync("pre", "lc", "did", "uic");

        Assert.Equal("override-user", result.Id);
        _coreMock.Verify(c => c.ConsumePasswordlessCodeAsync(It.IsAny<ConsumeCodeRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Recipes_ImplementIOverridableRecipe()
    {
        Assert.IsAssignableFrom<IOverridableRecipe>(new EmailPasswordRecipe(_coreMock.Object));
        Assert.IsAssignableFrom<IOverridableRecipe>(new SessionRecipe(_coreMock.Object));
        Assert.IsAssignableFrom<IOverridableRecipe>(new PasswordlessRecipe(_coreMock.Object));
    }

    [Fact]
    public void IOverridableRecipe_SetOverrides_CanSetTypedOverrides()
    {
        var emailOverrides = new EmailPasswordOverrides();
        IOverridableRecipe recipe = new EmailPasswordRecipe(_coreMock.Object)
        {
            Overrides = emailOverrides
        };

        Assert.Same(emailOverrides, recipe.Overrides);
    }
}
