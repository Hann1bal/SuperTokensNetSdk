using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;

namespace SuperTokensSDK.Net.Recipes.Session;

/// <summary>
/// SuperTokens Session recipe operations.
/// </summary>
public class SessionRecipe
{
    private readonly ICoreApiClient _coreApiClient;

    public SessionRecipe(ICoreApiClient coreApiClient)
    {
        _coreApiClient = coreApiClient ?? throw new ArgumentNullException(nameof(coreApiClient));
    }

    public async Task<SessionContainer> CreateSessionAsync(
        string userId,
        Dictionary<string, object>? accessTokenPayload = null,
        Dictionary<string, object>? sessionData = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _coreApiClient.CreateSessionAsync(new CreateSessionRequest
        {
            UserId = userId,
            AccessTokenPayload = accessTokenPayload,
            SessionData = sessionData
        }, cancellationToken);

        return CreateContainer(response);
    }

    public async Task<SessionContainer> VerifySessionAsync(string accessToken, string? antiCsrfToken = null, CancellationToken cancellationToken = default)
    {
        var response = await _coreApiClient.VerifySessionAsync(new VerifySessionRequest
        {
            AccessToken = accessToken,
            AntiCsrfToken = antiCsrfToken,
            DoAntiCsrfCheck = !string.IsNullOrEmpty(antiCsrfToken)
        }, cancellationToken);

        if (response.Session == null)
        {
            throw new UnauthorizedException("Session verification did not return a session.");
        }

        return new SessionContainer(
            response.Session.Handle,
            response.Session.UserId,
            response.Session.UserDataInJWT)
        {
            AccessToken = response.AccessToken?.Token
        };
    }

    public async Task<SessionContainer> RefreshSessionAsync(string refreshToken, string? antiCsrfToken = null, CancellationToken cancellationToken = default)
    {
        var response = await _coreApiClient.RefreshSessionAsync(new RefreshSessionRequest
        {
            RefreshToken = refreshToken,
            AntiCsrfToken = antiCsrfToken,
            EnableAntiCsrf = !string.IsNullOrEmpty(antiCsrfToken)
        }, cancellationToken);

        return CreateContainer(response);
    }

    public async Task RevokeSessionAsync(string sessionHandle, CancellationToken cancellationToken = default)
    {
        await _coreApiClient.RevokeSessionAsync(new RevokeSessionRequest { SessionHandle = sessionHandle }, cancellationToken);
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
}
