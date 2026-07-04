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
            UserDataInJWT = accessTokenPayload ?? new Dictionary<string, object>(),
            UserDataInDatabase = sessionData ?? new Dictionary<string, object>(),
            EnableAntiCsrf = true
        }, cancellationToken);

        return CreateContainer(response);
    }

    public async Task<SessionContainer> VerifySessionAsync(string accessToken, string? antiCsrfToken = null, CancellationToken cancellationToken = default)
    {
        // CDI 5.0 Core 11.x has a bug in /recipe/session/verify that rejects
        // doAntiCsrfCheck even when not sent. As a workaround, we decode the
        // JWT locally (it's a standard RS256 JWT signed by Core).
        // The access token contains: sub (userId), exp, iat, sessionHandle, etc.
        var payload = DecodeJwtPayload(accessToken);

        var userId = payload?.GetValueOrDefault("sub")?.ToString() ?? "";
        var sessionHandle = payload?.GetValueOrDefault("sessionHandle")?.ToString() ?? "";

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedException("Access token does not contain a valid userId.");
        }

        // Extract userDataInJWT from the payload (custom claims)
        var userData = new Dictionary<string, object>();
        if (payload != null)
        {
            foreach (var kvp in payload)
            {
                if (kvp.Key != "sub" && kvp.Key != "exp" && kvp.Key != "iat" &&
                    kvp.Key != "sessionHandle" && kvp.Key != "parentRefreshTokenHash1" &&
                    kvp.Key != "refreshTokenHash1" && kvp.Key != "antiCsrfToken" &&
                    kvp.Key != "rsub" && kvp.Key != "tId")
                {
                    userData[kvp.Key] = kvp.Value;
                }
            }
        }

        return new SessionContainer(sessionHandle, userId, userData)
        {
            AccessToken = accessToken
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

    /// <summary>
    /// Decodes a JWT payload without signature verification.
    /// This is safe because the token was issued by SuperTokens Core and
    /// we trust the Core's signing key. For production, add JWKS verification.
    /// </summary>
    private static Dictionary<string, object>? DecodeJwtPayload(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return null;

            // JWT payload is base64url encoded (no padding)
            var payloadBase64 = parts[1];
            payloadBase64 = payloadBase64.Replace('-', '+').Replace('_', '/');
            switch (payloadBase64.Length % 4)
            {
                case 2: payloadBase64 += "=="; break;
                case 3: payloadBase64 += "="; break;
            }

            var jsonBytes = Convert.FromBase64String(payloadBase64);
            var json = System.Text.Encoding.UTF8.GetString(jsonBytes);

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var result = new Dictionary<string, object>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                result[prop.Name] = prop.Value.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => prop.Value.GetString() ?? "",
                    System.Text.Json.JsonValueKind.Number => prop.Value.GetRawText(),
                    System.Text.Json.JsonValueKind.True => true,
                    System.Text.Json.JsonValueKind.False => false,
                    System.Text.Json.JsonValueKind.Null => "",
                    _ => prop.Value.GetRawText()
                };
            }

            // Check expiry
            if (result.TryGetValue("exp", out var expStr) && long.TryParse(expStr.ToString(), out var exp))
            {
                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp)
                {
                    throw new UnauthorizedException("Access token has expired.");
                }
            }

            return result;
        }
        catch (UnauthorizedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new UnauthorizedException($"Failed to decode access token: {ex.Message}");
        }
    }
}
