namespace SuperTokensSDK.Net.Core.Claims;

/// <summary>
/// Factory for primitive session claims (single values such as strings, numbers,
/// booleans, etc.).
/// </summary>
public static class PrimitiveClaim
{
    /// <summary>
    /// Creates a session claim for a single primitive value.
    /// </summary>
    public static TypeSessionClaim Create<T>(
        string key,
        Func<string, string, CancellationToken, Task<T?>> fetchValue,
        TimeSpan maxAge)
    {
        return new TypeSessionClaim
        {
            Key = key,
            MaxAge = maxAge,
            FetchValue = async (userId, tenantId, ct) => await fetchValue(userId, tenantId, ct),
            AddToPayloadInternal = (payload, claimKey) =>
            {
                var updated = new Dictionary<string, object?>(payload);
                updated[ClaimPayloadHelper.MetadataKey(claimKey)] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                return updated;
            },
            RemoveFromPayloadByMergeInternal = (payload, claimKey) =>
            {
                var updated = new Dictionary<string, object?>(payload);
                updated.Remove(claimKey);
                updated.Remove(ClaimPayloadHelper.MetadataKey(claimKey));
                return updated;
            },
            GetValueFromPayload = (payload, claimKey) =>
                payload.TryGetValue(claimKey, out var value) ? value : null,
            GetLastRefetchTime = (payload, claimKey) => ClaimPayloadHelper.GetLastRefetchTime(payload, claimKey)
        };
    }
}
