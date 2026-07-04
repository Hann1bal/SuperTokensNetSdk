using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Core;

/// <summary>
/// Thrown when the session is invalid or expired. Maps to HTTP 401.
/// </summary>
public class UnauthorizedException : SuperTokensException
{
    public UnauthorizedException(string message) : base(message) { }
}

/// <summary>
/// Thrown when the access token has expired and the frontend should refresh.
/// </summary>
public class TryRefreshTokenException : SuperTokensException
{
    public TryRefreshTokenException(string message) : base(message) { }
}

/// <summary>
/// Thrown when refresh token reuse is detected. All sessions for the user should be revoked.
/// </summary>
public class TokenTheftDetectedException : SuperTokensException
{
    public string SessionHandle { get; }
    public string UserId { get; }

    public TokenTheftDetectedException(string sessionHandle, string userId)
        : base("Token theft detected.")
    {
        SessionHandle = sessionHandle;
        UserId = userId;
    }
}

/// <summary>
/// Thrown when claim validation fails.
/// </summary>
public class InvalidClaimException : SuperTokensException
{
    public IReadOnlyList<InvalidClaim> InvalidClaims { get; }

    public InvalidClaimException(IEnumerable<InvalidClaim> invalidClaims)
        : base("Invalid claims detected.")
    {
        InvalidClaims = invalidClaims.ToList().AsReadOnly();
    }
}

/// <summary>
/// Details of a claim that failed validation.
/// </summary>
public class InvalidClaim
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "";
}
