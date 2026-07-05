using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Claims;
using SuperTokensSDK.Net.Core.Models;

namespace SuperTokensSDK.Net.Recipes.Session;

/// <summary>
/// SuperTokens Session recipe operations.
/// </summary>
public class SessionRecipe : IOverridableRecipe
{
    private readonly ICoreApiClient _coreApiClient;

    public SessionRecipe(ICoreApiClient coreApiClient)
    {
        _coreApiClient = coreApiClient ?? throw new ArgumentNullException(nameof(coreApiClient));
    }

    public SessionOverrides? Overrides { get; set; }

    RecipeOverrides? IOverridableRecipe.Overrides
    {
        get => Overrides;
        set => Overrides = (SessionOverrides?)value;
    }

    public async Task<SessionContainer> CreateSessionAsync(
        string userId,
        Dictionary<string, object>? accessTokenPayload = null,
        Dictionary<string, object>? sessionData = null,
        CancellationToken cancellationToken = default)
    {
        if (Overrides?.CreateSession != null)
            return await Overrides.CreateSession(userId, accessTokenPayload, sessionData, cancellationToken);

        var response = await _coreApiClient.CreateSessionAsync(new CreateSessionRequest
        {
            UserId = userId,
            UserDataInJWT = accessTokenPayload ?? new Dictionary<string, object>(),
            UserDataInDatabase = sessionData ?? new Dictionary<string, object>(),
            EnableAntiCsrf = true
        }, cancellationToken);

        return CreateContainer(response);
    }

    public async Task<SessionContainer> VerifySessionAsync(string accessToken, string? antiCsrfToken = null, CancellationToken cancellationToken = default)
    {
        if (Overrides?.VerifySession != null)
            return await Overrides.VerifySession(accessToken, antiCsrfToken, cancellationToken);

        var response = await _coreApiClient.VerifySessionAsync(new VerifySessionRequest
        {
            AccessToken = accessToken,
            AntiCsrfToken = antiCsrfToken,
            DoAntiCsrfCheck = !string.IsNullOrEmpty(antiCsrfToken)
        }, cancellationToken);

        if (string.IsNullOrEmpty(response?.Session?.UserId))
        {
            throw new UnauthorizedException("Access token does not contain a valid userId.");
        }

        return new SessionContainer(
            response!.Session.Handle,
            response.Session.UserId,
            response.Session.UserDataInJWT)
        {
            AccessToken = accessToken
        };
    }

    public async Task<SessionContainer> RefreshSessionAsync(string refreshToken, string? antiCsrfToken = null, CancellationToken cancellationToken = default)
    {
        if (Overrides?.RefreshSession != null)
            return await Overrides.RefreshSession(refreshToken, antiCsrfToken, cancellationToken);

        var response = await _coreApiClient.RefreshSessionAsync(new RefreshSessionRequest
        {
            RefreshToken = refreshToken,
            AntiCsrfToken = antiCsrfToken,
            EnableAntiCsrf = !string.IsNullOrEmpty(antiCsrfToken)
        }, cancellationToken);

        return CreateContainer(response);
    }

    public async Task<List<SessionInfo>> GetActiveSessionsAsync(string userId, string tenantId = "public", CancellationToken ct = default)
    {
        if (Overrides?.GetActiveSessions != null)
            return await Overrides.GetActiveSessions(userId, tenantId, ct);

        var handles = await _coreApiClient.GetAllSessionHandlesForUserAsync(userId, tenantId, false, ct);
        if (handles.Count == 0)
        {
            return new List<SessionInfo>();
        }

        var tasks = handles.Select(h => _coreApiClient.GetSessionInformationAsync(h, ct)).ToArray();
        var results = await Task.WhenAll(tasks);
        return results.Where(r => r != null).ToList()!;
    }

    public async Task<int> RevokeAllSessionsAsync(string userId, string tenantId = "public", CancellationToken ct = default)
    {
        var revoked = await _coreApiClient.RevokeAllSessionsForUserAsync(userId, tenantId, false, ct);
        return revoked.Count;
    }

    public async Task<bool> RevokeSessionAsync(string sessionHandle, CancellationToken ct = default)
    {
        if (Overrides?.RevokeSession != null)
            return await Overrides.RevokeSession(sessionHandle, ct);

        var revoked = await _coreApiClient.RevokeMultipleSessionsAsync(new List<string> { sessionHandle }, ct);
        return revoked.Contains(sessionHandle);
    }

    public async Task<ClaimValidationResult[]> ValidateClaimsAsync(
        string sessionHandle,
        List<SessionClaimValidator> validators,
        CancellationToken cancellationToken = default)
    {
        var info = await _coreApiClient.GetSessionInformationAsync(sessionHandle, cancellationToken);
        if (info == null)
        {
            return new[] { new ClaimValidationResult { IsValid = false, Reason = "Session not found" } };
        }

        var payload = ToNullableDictionary(info.UserDataInJWT);
        var results = new List<ClaimValidationResult>(validators.Count);

        foreach (var validator in validators)
        {
            if (validator.Claim != null)
            {
                var shouldRefetch = validator.ShouldRefetch?.Invoke(payload) ?? false;
                if (shouldRefetch)
                {
                    var value = await validator.Claim.FetchValue(info.UserId, info.TenantId, cancellationToken);
                    if (value != null)
                    {
                        await SetClaimValueAsync(sessionHandle, validator.Claim, value, cancellationToken);
                        payload = validator.Claim.AddToPayload(payload, value);
                    }
                }
            }

            results.Add(validator.Validate(payload));
        }

        return results.ToArray();
    }

    public async Task FetchAndSetClaimAsync(string sessionHandle, TypeSessionClaim claim, CancellationToken cancellationToken = default)
    {
        var info = await _coreApiClient.GetSessionInformationAsync(sessionHandle, cancellationToken);
        if (info == null) return;

        var value = await claim.FetchValue(info.UserId, info.TenantId, cancellationToken);
        if (value != null)
        {
            await SetClaimValueAsync(sessionHandle, claim, value, cancellationToken);
        }
    }

    public async Task SetClaimValueAsync(
        string sessionHandle,
        TypeSessionClaim claim,
        object? value,
        CancellationToken cancellationToken = default)
    {
        var info = await _coreApiClient.GetSessionInformationAsync(sessionHandle, cancellationToken);
        if (info == null) return;

        var payload = claim.AddToPayload(ToNullableDictionary(info.UserDataInJWT), value);
        await _coreApiClient.UpdateJwtDataAsync(new UpdateJwtDataRequest
        {
            SessionHandle = sessionHandle,
            UserDataInJWT = payload
        }, cancellationToken);
    }

    public async Task<T?> GetClaimValueAsync<T>(string sessionHandle, TypeSessionClaim claim, CancellationToken cancellationToken = default)
    {
        var info = await _coreApiClient.GetSessionInformationAsync(sessionHandle, cancellationToken);
        if (info == null) return default;

        var payload = ToNullableDictionary(info.UserDataInJWT);
        var value = claim.GetValueFromPayload(payload, claim.Key);
        return ClaimPayloadHelper.ConvertValue<T>(value);
    }

    public async Task RemoveClaimAsync(string sessionHandle, TypeSessionClaim claim, CancellationToken cancellationToken = default)
    {
        var info = await _coreApiClient.GetSessionInformationAsync(sessionHandle, cancellationToken);
        if (info == null) return;

        var payload = claim.RemoveFromPayload(ToNullableDictionary(info.UserDataInJWT));
        await _coreApiClient.UpdateJwtDataAsync(new UpdateJwtDataRequest
        {
            SessionHandle = sessionHandle,
            UserDataInJWT = payload
        }, cancellationToken);
    }

    private static SessionContainer CreateContainer(CreateOrRefreshAPIResponse response)
    {
        return new SessionContainer(
            response.Session.Handle,
            response.Session.UserId,
            response.Session.UserDataInJWT)
        {
            AccessToken = response.AccessToken?.Token,
            RefreshToken = response.RefreshToken?.Token,
            AntiCsrfToken = response.AntiCsrfToken,
            AccessTokenExpiry = ConvertExpiryToDateTime(response.AccessToken?.Expiry),
            RefreshTokenExpiry = ConvertExpiryToDateTime(response.RefreshToken?.Expiry)
        };
    }

    private static DateTime ConvertExpiryToDateTime(long? expiryMs)
    {
        if (!expiryMs.HasValue) return DateTime.MinValue;
        return DateTimeOffset.FromUnixTimeMilliseconds(expiryMs.Value).UtcDateTime;
    }

    private static Dictionary<string, object?> ToNullableDictionary(Dictionary<string, object> source)
    {
        return source.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value);
    }
}
