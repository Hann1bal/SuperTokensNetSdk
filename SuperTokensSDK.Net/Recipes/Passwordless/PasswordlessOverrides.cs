using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;

namespace SuperTokensSDK.Net.Recipes.Passwordless;

/// <summary>
/// Nullable delegate overrides for the Passwordless recipe.
/// </summary>
public sealed class PasswordlessOverrides : RecipeOverrides
{
    public Func<string?, string?, string?, string, CancellationToken, Task<(string DeviceId, string PreAuthSessionId, string LinkCode)>>? CreateCode { get; set; }
    public Func<string, string?, string?, string?, string, CancellationToken, Task<PasswordlessUser>>? ConsumeCode { get; set; }
}
