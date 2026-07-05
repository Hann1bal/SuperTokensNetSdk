using System.Security.Claims;
using System.Text.Json;
using SuperTokensSDK.Net.Recipes.Session;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class SessionContainerTests
{
    [Fact]
    public void GetClaimsPrincipal_BuildsCorrectClaims_FromUserData()
    {
        using var rolesDoc = JsonDocument.Parse("[\"admin\", \"editor\"]");
        var container = new SessionContainer("sh", "user-1", new Dictionary<string, object>
        {
            ["roles"] = rolesDoc.RootElement.Clone(),
            ["custom"] = "value"
        });

        var principal = container.GetClaimsPrincipal();
        var identity = Assert.IsType<ClaimsIdentity>(principal.Identity);

        Assert.Equal("user-1", identity.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("user-1", identity.FindFirst("sub")?.Value);

        var roleClaims = identity.FindAll(ClaimTypes.Role).ToList();
        Assert.Equal(2, roleClaims.Count);
        Assert.Contains(roleClaims, c => c.Value == "admin");
        Assert.Contains(roleClaims, c => c.Value == "editor");

        Assert.Equal("value", identity.FindFirst("custom")?.Value);
    }

    [Fact]
    public void GetClaimsPrincipal_WorksWithNullUserData()
    {
        var container = new SessionContainer("sh", "user-2", userDataInJwt: null);
        var principal = container.GetClaimsPrincipal();
        var identity = Assert.IsType<ClaimsIdentity>(principal.Identity);

        Assert.Equal("user-2", identity.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Empty(identity.FindAll(ClaimTypes.Role));
    }

    [Fact]
    public void GetClaimsPrincipal_WorksWithEmptyUserData()
    {
        var container = new SessionContainer("sh", "user-3", new Dictionary<string, object>());
        var principal = container.GetClaimsPrincipal();
        Assert.NotNull(principal.Identity);
    }

    [Fact]
    public void GetClaimsPrincipal_WorksWithSingleStringRole()
    {
        var container = new SessionContainer("sh", "user-4", new Dictionary<string, object>
        {
            ["roles"] = "single-role"
        });
        var principal = container.GetClaimsPrincipal();
        var identity = Assert.IsType<ClaimsIdentity>(principal.Identity);

        Assert.DoesNotContain(identity.FindAll(ClaimTypes.Role), c => true);
        Assert.Equal("single-role", identity.FindFirst("roles")?.Value);
    }

    [Fact]
    public void GetClaim_ReturnsValue_ForExistingKey()
    {
        var container = new SessionContainer("sh", "user-5", new Dictionary<string, object>
        {
            ["name"] = "Alice"
        });

        Assert.Equal("Alice", container.GetClaim<string>("name"));
    }

    [Fact]
    public void GetClaim_ReturnsDefault_ForMissingKey()
    {
        var container = new SessionContainer("sh", "user-6");
        Assert.Null(container.GetClaim<string>("missing"));
        Assert.Equal(42, container.GetClaim("missing", 42));
    }

    [Fact]
    public void GetClaim_DeserializesJsonElement_ForComplexType()
    {
        using var doc = JsonDocument.Parse("{\"Name\":\"Bob\",\"Age\":30}");
        var container = new SessionContainer("sh", "user-7", new Dictionary<string, object>
        {
            ["profile"] = doc.RootElement.Clone()
        });

        var profile = container.GetClaim<TestProfile>("profile");
        Assert.NotNull(profile);
        Assert.Equal("Bob", profile!.Name);
        Assert.Equal(30, profile.Age);
    }

    [Fact]
    public void Properties_AreSettableAndGettable()
    {
        var container = new SessionContainer("sh1", "user-8")
        {
            SessionHandle = "sh2",
            UserId = "user-8",
            AccessToken = "at",
            RefreshToken = "rt",
            AntiCsrfToken = "csrf",
            AccessTokenExpiry = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            RefreshTokenExpiry = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            UserDataInJwt = new Dictionary<string, object> { ["x"] = 1 }
        };

        Assert.Equal("sh2", container.SessionHandle);
        Assert.Equal("user-8", container.UserId);
        Assert.Equal("at", container.AccessToken);
        Assert.Equal("rt", container.RefreshToken);
        Assert.Equal("csrf", container.AntiCsrfToken);
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), container.AccessTokenExpiry);
        Assert.Equal(new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), container.RefreshTokenExpiry);
        Assert.Single(container.UserDataInJwt);
    }

    private class TestProfile
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }
}
