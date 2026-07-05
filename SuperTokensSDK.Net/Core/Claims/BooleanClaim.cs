namespace SuperTokensSDK.Net.Core.Claims;

/// <summary>
/// Factory for boolean session claims.
/// </summary>
public static class BooleanClaim
{
    /// <summary>
    /// Creates a session claim for a boolean value.
    /// </summary>
    public static TypeSessionClaim Create(
        string key,
        Func<string, string, CancellationToken, Task<bool>> fetchValue,
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
            {
                if (!payload.TryGetValue(claimKey, out var value))
                {
                    return null;
                }

                return ClaimPayloadHelper.ConvertValue<bool>(value);
            },
            GetLastRefetchTime = (payload, claimKey) => ClaimPayloadHelper.GetLastRefetchTime(payload, claimKey)
        };
    }
}
