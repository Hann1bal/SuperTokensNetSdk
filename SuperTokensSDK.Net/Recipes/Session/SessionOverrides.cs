using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;

namespace SuperTokensSDK.Net.Recipes.Session;

/// <summary>
/// Nullable delegate overrides for the Session recipe.
/// </summary>
public sealed class SessionOverrides : RecipeOverrides
{
    public Func<string, Dictionary<string, object>?, Dictionary<string, object>?, CancellationToken, Task<SessionContainer>>? CreateSession { get; set; }
    public Func<string, string?, CancellationToken, Task<SessionContainer>>? VerifySession { get; set; }
    public Func<string, string?, CancellationToken, Task<SessionContainer>>? RefreshSession { get; set; }
    public Func<string, CancellationToken, Task<bool>>? RevokeSession { get; set; }
    public Func<string, string, CancellationToken, Task<List<SessionInfo>>>? GetActiveSessions { get; set; }
}
