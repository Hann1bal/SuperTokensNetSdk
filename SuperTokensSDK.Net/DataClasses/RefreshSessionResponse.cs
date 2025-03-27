using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperTokensSDK.Net.DataClasses;

/// <summary>
/// Represents the response of a session refresh operation.
/// </summary>
public class RefreshSessionResponse
{
    /// <summary>
    /// Gets or sets the user ID associated with the session.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the access token associated with the session.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the refresh token associated with the session.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Gets or sets the claims associated with the session.
    /// </summary>
    public Dictionary<string, object> Claims { get; set; } = new();
}
