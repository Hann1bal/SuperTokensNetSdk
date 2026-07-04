using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;
using SuperTokensSDK.Net.DataClasses;

namespace SuperTokensSDK.Net.Recipes.Session;

/// <summary>
/// Wraps session creation/verification/refresh/revocation results.
/// </summary>
public class SessionContainer
{
    public string SessionHandle { get; set; } = "";
    public string UserId { get; set; } = "";
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? FrontToken { get; set; }
    public DateTime AccessTokenExpiry { get; set; }
    public DateTime RefreshTokenExpiry { get; set; }
    public Dictionary<string, object> UserDataInJwt { get; set; } = new();
}
