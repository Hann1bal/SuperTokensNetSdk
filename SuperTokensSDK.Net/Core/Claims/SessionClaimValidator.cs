namespace SuperTokensSDK.Net.Core.Claims;

/// <summary>
/// Validates a session claim value in the access token payload.
/// </summary>
public class SessionClaimValidator
{
    public string Id { get; init; } = "";

    public TypeSessionClaim? Claim { get; init; }

    public Func<Dictionary<string, object?>, bool>? ShouldRefetch { get; init; }

    public Func<Dictionary<string, object?>, ClaimValidationResult> Validate { get; init; } = null!;
}

/// <summary>
/// Result of validating a session claim.
/// </summary>
public class ClaimValidationResult
{
    public bool IsValid { get; init; }

    public string Reason { get; init; } = "";
}
