namespace SuperTokensSDK.Net.Core.Claims;

/// <summary>
/// Factory for array-of-primitive session claims (e.g. roles, permissions).
/// </summary>
public static class PrimitiveArrayClaim
{
    /// <summary>
    /// Creates a session claim for an array of strings.
    /// </summary>
    public static TypeSessionClaim Create(
        string key,
        Func<string, string, CancellationToken, Task<string[]?>> fetchValue,
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

                var array = ClaimPayloadHelper.GetStringArray(value);
                return array.Length == 0 ? null : array;
            },
            GetLastRefetchTime = (payload, claimKey) => ClaimPayloadHelper.GetLastRefetchTime(payload, claimKey)
        };
    }

    /// <summary>
    /// Validates that the claim array contains <paramref name="value"/>.
    /// </summary>
    public static SessionClaimValidator Includes(this TypeSessionClaim claim, string value)
    {
        return new SessionClaimValidator
        {
            Id = $"{claim.Key}-includes-{value}",
            Claim = claim,
            ShouldRefetch = CreateShouldRefetch(claim, claim.MaxAge),
            Validate = payload =>
            {
                var array = ClaimPayloadHelper.GetStringArray(claim.GetValueFromPayload(payload, claim.Key));
                return array.Contains(value)
                    ? new ClaimValidationResult { IsValid = true }
                    : new ClaimValidationResult { IsValid = false, Reason = $"Claim '{claim.Key}' does not include '{value}'." };
            }
        };
    }

    /// <summary>
    /// Validates that the claim array does not contain <paramref name="value"/>.
    /// </summary>
    public static SessionClaimValidator Excludes(this TypeSessionClaim claim, string value)
    {
        return new SessionClaimValidator
        {
            Id = $"{claim.Key}-excludes-{value}",
            Claim = claim,
            ShouldRefetch = CreateShouldRefetch(claim, claim.MaxAge),
            Validate = payload =>
            {
                var array = ClaimPayloadHelper.GetStringArray(claim.GetValueFromPayload(payload, claim.Key));
                return !array.Contains(value)
                    ? new ClaimValidationResult { IsValid = true }
                    : new ClaimValidationResult { IsValid = false, Reason = $"Claim '{claim.Key}' should not include '{value}'." };
            }
        };
    }

    /// <summary>
    /// Validates that the claim array contains all of the supplied values.
    /// </summary>
    public static SessionClaimValidator IncludesAll(this TypeSessionClaim claim, params string[] values)
    {
        return new SessionClaimValidator
        {
            Id = $"{claim.Key}-includes-all",
            Claim = claim,
            ShouldRefetch = CreateShouldRefetch(claim, claim.MaxAge),
            Validate = payload =>
            {
                var array = ClaimPayloadHelper.GetStringArray(claim.GetValueFromPayload(payload, claim.Key));
                var missing = values.Where(v => !array.Contains(v)).ToArray();
                return missing.Length == 0
                    ? new ClaimValidationResult { IsValid = true }
                    : new ClaimValidationResult { IsValid = false, Reason = $"Claim '{claim.Key}' is missing: {string.Join(", ", missing)}." };
            }
        };
    }

    /// <summary>
    /// Validates that the claim array contains at least one of the supplied values.
    /// </summary>
    public static SessionClaimValidator IncludesAny(this TypeSessionClaim claim, params string[] values)
    {
        return new SessionClaimValidator
        {
            Id = $"{claim.Key}-includes-any",
            Claim = claim,
            ShouldRefetch = CreateShouldRefetch(claim, claim.MaxAge),
            Validate = payload =>
            {
                var array = ClaimPayloadHelper.GetStringArray(claim.GetValueFromPayload(payload, claim.Key));
                return values.Any(v => array.Contains(v))
                    ? new ClaimValidationResult { IsValid = true }
                    : new ClaimValidationResult { IsValid = false, Reason = $"Claim '{claim.Key}' does not include any of the required values." };
            }
        };
    }

    /// <summary>
    /// Validates that the claim array contains none of the supplied values.
    /// </summary>
    public static SessionClaimValidator ExcludesAll(this TypeSessionClaim claim, params string[] values)
    {
        return new SessionClaimValidator
        {
            Id = $"{claim.Key}-excludes-all",
            Claim = claim,
            ShouldRefetch = CreateShouldRefetch(claim, claim.MaxAge),
            Validate = payload =>
            {
                var array = ClaimPayloadHelper.GetStringArray(claim.GetValueFromPayload(payload, claim.Key));
                var present = values.Where(v => array.Contains(v)).ToArray();
                return present.Length == 0
                    ? new ClaimValidationResult { IsValid = true }
                    : new ClaimValidationResult { IsValid = false, Reason = $"Claim '{claim.Key}' should not include: {string.Join(", ", present)}." };
            }
        };
    }

    private static Func<Dictionary<string, object?>, bool> CreateShouldRefetch(TypeSessionClaim claim, TimeSpan maxAge)
    {
        return payload =>
        {
            var value = claim.GetValueFromPayload(payload, claim.Key);
            if (value is null)
            {
                return true;
            }

            var lastRefetch = claim.GetLastRefetchTime(payload, claim.Key);
            if (!lastRefetch.HasValue)
            {
                return true;
            }

            var age = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - lastRefetch.Value;
            return age > maxAge.TotalMilliseconds;
        };
    }
}
