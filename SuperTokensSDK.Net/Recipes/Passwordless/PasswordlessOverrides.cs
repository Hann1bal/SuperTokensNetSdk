using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;

namespace SuperTokensSDK.Net.Recipes.Passwordless;

/// <summary>
/// Nullable delegate overrides for the Passwordless recipe.
/// Set any delegate to replace the default implementation.
/// </summary>
public sealed class PasswordlessOverrides : RecipeOverrides
{
    /// <summary>
    /// Validates a phone number. Return null if valid, or an error message string if invalid.
    /// If not set, the default libphonenumber validator is used.
    /// Parameters: (phoneNumber, tenantId)
    /// </summary>
    public Func<string?, string, string?>? ValidatePhoneNumber { get; set; }

    public Func<string?, string?, string?, string, CancellationToken, Task<(string DeviceId, string PreAuthSessionId, string LinkCode)>>? CreateCode { get; set; }
    public Func<string, string?, string?, string?, string, CancellationToken, Task<PasswordlessUser>>? ConsumeCode { get; set; }
}
