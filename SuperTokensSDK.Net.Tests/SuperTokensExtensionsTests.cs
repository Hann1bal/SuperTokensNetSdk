using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SuperTokensSDK.Net.AspNetCore;
using SuperTokensSDK.Net.Configuration;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Recipes.EmailPassword;
using SuperTokensSDK.Net.Recipes.EmailVerification;
using SuperTokensSDK.Net.Recipes.Jwt;
using SuperTokensSDK.Net.Recipes.Multitenancy;
using SuperTokensSDK.Net.Recipes.Session;
using SuperTokensSDK.Net.Recipes.UserManagement;
using SuperTokensSDK.Net.Recipes.UserMetadata;
using SuperTokensSDK.Net.Recipes.UserRoles;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class SuperTokensExtensionsTests
{
    [Fact]
    public void AddSuperTokens_RegistersServices()
    {
        var services = new ServiceCollection();
        services.AddSuperTokens(options =>
        {
            options.CoreUri = "http://localhost:3567";
            options.AppName = "Test";
        });

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IOptions<SuperTokensOptions>>());
        Assert.NotNull(provider.GetService<ICoreApiClient>());
        Assert.NotNull(provider.GetService<SessionRecipe>());
        Assert.NotNull(provider.GetService<EmailPasswordRecipe>());
        Assert.NotNull(provider.GetService<UserRolesRecipe>());
        Assert.NotNull(provider.GetService<UserMetadataRecipe>());
        Assert.NotNull(provider.GetService<UserManagementRecipe>());
        Assert.NotNull(provider.GetService<EmailVerificationRecipe>());
        Assert.NotNull(provider.GetService<JwtRecipe>());
        Assert.NotNull(provider.GetService<MultitenancyRecipe>());
    }

    [Fact]
    public void AddSuperTokens_RegistersRecipesAsScoped()
    {
        var services = new ServiceCollection();
        services.AddSuperTokens(options =>
        {
            options.CoreUri = "http://localhost:3567";
            options.AppName = "Test";
        });

        Assert.All(new[]
        {
            typeof(SessionRecipe),
            typeof(EmailPasswordRecipe),
            typeof(UserRolesRecipe),
            typeof(UserMetadataRecipe),
            typeof(UserManagementRecipe),
            typeof(EmailVerificationRecipe),
            typeof(JwtRecipe),
            typeof(MultitenancyRecipe)
        }, type =>
        {
            var descriptor = services.FirstOrDefault(s => s.ServiceType == type);
            Assert.NotNull(descriptor);
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        });
    }

    [Fact]
    public void AddSuperTokensAuthentication_RegistersClaimsTransformation()
    {
        var services = new ServiceCollection();
        services.AddAuthentication("SuperTokens")
            .AddSuperTokensAuthentication();

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IClaimsTransformation>());
        Assert.IsType<SuperTokensClaimsTransformation>(provider.GetService<IClaimsTransformation>());
    }
}
