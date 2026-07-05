using SuperTokensSDK.Net.Core;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class ExceptionsTests
{
    [Fact]
    public void SuperTokensException_WithMessage_PreservesMessage()
    {
        var ex = new SuperTokensException("base message");
        Assert.Equal("base message", ex.Message);
    }

    [Fact]
    public void SuperTokensException_WithInnerException_PreservesInner()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new SuperTokensException("base message", inner);
        Assert.Equal("base message", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void UnauthorizedException_InheritsFromSuperTokensException()
    {
        var ex = new UnauthorizedException("unauthorized");
        Assert.IsAssignableFrom<SuperTokensException>(ex);
        Assert.Equal("unauthorized", ex.Message);
    }

    [Fact]
    public void TryRefreshTokenException_InheritsFromSuperTokensException()
    {
        var ex = new TryRefreshTokenException("refresh");
        Assert.IsAssignableFrom<SuperTokensException>(ex);
        Assert.Equal("refresh", ex.Message);
    }

    [Fact]
    public void TokenTheftDetectedException_HasPropertiesAndMessage()
    {
        var ex = new TokenTheftDetectedException("handle-1", "user-1");
        Assert.IsAssignableFrom<SuperTokensException>(ex);
        Assert.Equal("Token theft detected.", ex.Message);
        Assert.Equal("handle-1", ex.SessionHandle);
        Assert.Equal("user-1", ex.UserId);
    }

    [Fact]
    public void InvalidClaimException_HasClaimsAndMessage()
    {
        var claims = new List<InvalidClaim>
        {
            new() { Id = "email", Reason = "missing" },
            new() { Id = "age", Reason = "too low" }
        };
        var ex = new InvalidClaimException(claims);
        Assert.IsAssignableFrom<SuperTokensException>(ex);
        Assert.Equal("Invalid claims detected.", ex.Message);
        Assert.Equal(2, ex.InvalidClaims.Count);
        Assert.Equal("email", ex.InvalidClaims[0].Id);
        Assert.Equal("missing", ex.InvalidClaims[0].Reason);
        Assert.Equal("age", ex.InvalidClaims[1].Id);
        Assert.Equal("too low", ex.InvalidClaims[1].Reason);
    }

    [Fact]
    public void InvalidClaim_HasIdAndReason()
    {
        var claim = new InvalidClaim { Id = "x", Reason = "y" };
        Assert.Equal("x", claim.Id);
        Assert.Equal("y", claim.Reason);
    }
}
