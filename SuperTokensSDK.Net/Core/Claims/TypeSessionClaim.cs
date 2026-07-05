namespace SuperTokensSDK.Net.Core.Claims;

/// <summary>
/// Describes a session claim that can be fetched from Core and stored in the
/// access token payload.
/// </summary>
public class TypeSessionClaim
{
    public string Key { get; init; } = "";

    public Func<string, string, CancellationToken, Task<object?>> FetchValue { get; init; } = null!;

    public Func<Dictionary<string, object?>, string, Dictionary<string, object?>> AddToPayloadInternal { get; init; } = null!;

    public Func<Dictionary<string, object?>, string, Dictionary<string, object?>> RemoveFromPayloadByMergeInternal { get; init; } = null!;

    public Func<Dictionary<string, object?>, string, object?> GetValueFromPayload { get; init; } = null!;

    public Func<Dictionary<string, object?>, string, long?> GetLastRefetchTime { get; init; } = null!;

    /// <summary>
    /// Maximum age of the claim value before it should be refetched.
    /// </summary>
    public TimeSpan MaxAge { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Adds the claim value and its metadata to a payload copy.
    /// </summary>
    public Dictionary<string, object?> AddToPayload(Dictionary<string, object?> payload, object? value)
    {
        var updated = AddToPayloadInternal(payload, Key);
        updated[Key] = value;
        return updated;
    }

    /// <summary>
    /// Removes the claim value and its metadata from a payload copy.
    /// </summary>
    public Dictionary<string, object?> RemoveFromPayload(Dictionary<string, object?> payload)
    {
        return RemoveFromPayloadByMergeInternal(payload, Key);
    }
}
