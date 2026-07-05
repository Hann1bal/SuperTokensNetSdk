using System.Security.Claims;
using SuperTokensSDK.Net.AspNetCore;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class SuperTokensClaimsTransformationTests
{
    private readonly SuperTokensClaimsTransformation _transformation = new();

    [Fact]
    public async Task TransformAsync_NoRoles_ReturnsPrincipalUnchanged()
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "u1") }, "Test");
        var principal = new ClaimsPrincipal(identity);

        var result = await _transformation.TransformAsync(principal);

        Assert.Same(principal, result);
        Assert.Empty(result.FindAll(ClaimTypes.Role));
    }

    [Fact]
    public async Task TransformAsync_AlreadyHasRole_ReturnsPrincipalUnchanged()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "u1"),
            new Claim(ClaimTypes.Role, "admin")
        }, "Test");
        var principal = new ClaimsPrincipal(identity);

        var result = await _transformation.TransformAsync(principal);

        Assert.Same(principal, result);
        Assert.Single(result.FindAll(ClaimTypes.Role));
    }

    [Fact]
    public async Task TransformAsync_RolesClaim_AddsRoleClaims()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "u1"),
            new Claim("roles", "admin, editor, viewer")
        }, "Test");
        var principal = new ClaimsPrincipal(identity);

        var result = await _transformation.TransformAsync(principal);

        var roles = result.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Equal(new[] { "admin", "editor", "viewer" }, roles);
    }

    [Fact]
    public async Task TransformAsync_RoleClaim_AddsSingleRoleClaim()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "u1"),
            new Claim("role", "user")
        }, "Test");
        var principal = new ClaimsPrincipal(identity);

        var result = await _transformation.TransformAsync(principal);

        var roles = result.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Equal(new[] { "user" }, roles);
    }
}
