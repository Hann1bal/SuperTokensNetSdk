using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;

namespace SuperTokensSDK.Net.Recipes.ThirdParty;

/// <summary>
/// Nullable delegate overrides for the ThirdParty recipe.
/// </summary>
public sealed class ThirdPartyOverrides : RecipeOverrides
{
    public Func<ThirdPartyInfo, string, string, CancellationToken, Task<(ThirdPartyUser User, bool CreatedNewUser)>>? SignInUp { get; set; }
    public Func<string, CancellationToken, Task<ThirdPartyUser?>>? GetUserById { get; set; }
}
