using SuperTokensSDK.Net.Core.Claims;

namespace SuperTokensSDK.Net.Recipes.UserRoles;

/// <summary>
/// Built-in session claims for the UserRoles recipe.
/// </summary>
public static class UserRolesClaims
{
    public const string RoleClaimKey = "st-role";
    public const string PermissionClaimKey = "st-perm";

    /// <summary>
    /// Creates the <c>st-role</c> session claim backed by the UserRoles recipe.
    /// </summary>
    public static TypeSessionClaim CreateRoleClaim(UserRolesRecipe recipe)
    {
        return PrimitiveArrayClaim.Create(
            RoleClaimKey,
            async (userId, tenantId, ct) =>
            {
                var roles = await recipe.GetRolesAsync(userId, ct);
                return roles.ToArray();
            },
            TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Creates the <c>st-perm</c> session claim backed by the UserRoles recipe.
    /// </summary>
    public static TypeSessionClaim CreatePermissionClaim(UserRolesRecipe recipe)
    {
        return PrimitiveArrayClaim.Create(
            PermissionClaimKey,
            async (userId, tenantId, ct) =>
            {
                var permissions = await recipe.GetPermissionsAsync(userId, ct);
                return permissions.ToArray();
            },
            TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Creates validators for the role claim.
    /// </summary>
    public static (TypeSessionClaim Claim, SessionClaimValidator[] Validators) CreateRoleValidators(
        TypeSessionClaim roleClaim)
    {
        return (roleClaim, new[]
        {
            new SessionClaimValidator
            {
                Id = "st-role-exists",
                Claim = roleClaim,
                ShouldRefetch = payload => roleClaim.GetValueFromPayload(payload, RoleClaimKey) is null,
                Validate = payload => roleClaim.GetValueFromPayload(payload, RoleClaimKey) != null
                    ? new ClaimValidationResult { IsValid = true }
                    : new ClaimValidationResult { IsValid = false, Reason = "Role claim not set" }
            }
        });
    }

    /// <summary>
    /// Creates validators for the permission claim.
    /// </summary>
    public static (TypeSessionClaim Claim, SessionClaimValidator[] Validators) CreatePermissionValidators(
        TypeSessionClaim permissionClaim)
    {
        return (permissionClaim, new[]
        {
            new SessionClaimValidator
            {
                Id = "st-perm-exists",
                Claim = permissionClaim,
                ShouldRefetch = payload => permissionClaim.GetValueFromPayload(payload, PermissionClaimKey) is null,
                Validate = payload => permissionClaim.GetValueFromPayload(payload, PermissionClaimKey) != null
                    ? new ClaimValidationResult { IsValid = true }
                    : new ClaimValidationResult { IsValid = false, Reason = "Permission claim not set" }
            }
        });
    }
}
