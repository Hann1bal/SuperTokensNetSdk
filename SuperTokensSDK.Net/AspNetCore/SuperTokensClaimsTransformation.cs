using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace SuperTokensSDK.Net.AspNetCore;

/// <summary>
/// Claims transformation that extracts roles from SuperTokens session payload.
/// </summary>
public class SuperTokensClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || identity.HasClaim(c => c.Type == ClaimTypes.Role))
        {
            return Task.FromResult(principal);
        }

        var roleClaim = identity.FindFirst("roles") ?? identity.FindFirst("role");
        if (roleClaim != null && !string.IsNullOrWhiteSpace(roleClaim.Value))
        {
            var roles = roleClaim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var role in roles)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }

        return Task.FromResult(principal);
    }
}
