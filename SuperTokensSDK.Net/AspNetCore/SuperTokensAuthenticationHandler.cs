using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;

namespace SuperTokensSDK.Net.AspNetCore;

/// <summary>
/// ASP.NET Core authentication handler that validates SuperTokens access tokens.
/// </summary>
public class SuperTokensAuthenticationHandler : AuthenticationHandler<SuperTokensAuthenticationOptions>
{
    private readonly ICoreApiClient _coreApiClient;

    public SuperTokensAuthenticationHandler(
        IOptionsMonitor<SuperTokensAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ICoreApiClient coreApiClient)
        : base(options, logger, encoder)
    {
        _coreApiClient = coreApiClient ?? throw new ArgumentNullException(nameof(coreApiClient));
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var accessToken = ExtractAccessToken();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return AuthenticateResult.Fail("No access token provided.");
        }

            var antiCsrfToken = Context.Request.Cookies[Options.AntiCsrfCookieName]
                ?? Context.Request.Headers[Core.Constants.HeaderNames.AntiCsrf].FirstOrDefault();

            try
            {
                var verifyResult = await _coreApiClient.VerifySessionAsync(new VerifySessionRequest
                {
                    AccessToken = accessToken,
                    AntiCsrfToken = antiCsrfToken,
                    DoAntiCsrfCheck = !string.IsNullOrEmpty(antiCsrfToken)
                }, Context.RequestAborted);

                if (string.IsNullOrWhiteSpace(verifyResult.Session?.UserId))
                {
                    return AuthenticateResult.Fail("Session verification did not return a user ID.");
                }

                var identity = CreateClaimsIdentity(verifyResult.Session.UserId, verifyResult.Session.UserDataInJWT);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "SuperTokens session verification failed.");
                return AuthenticateResult.Fail("Invalid or expired session.");
            }
    }

    private string? ExtractAccessToken()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        if (Request.Cookies.TryGetValue(Options.AccessTokenCookieName, out var cookieToken) && !string.IsNullOrWhiteSpace(cookieToken))
        {
            return cookieToken;
        }

        if (Request.Path.StartsWithSegments("/hubs") && Request.Query.TryGetValue("access_token", out var queryToken))
        {
            return queryToken.FirstOrDefault();
        }

        return null;
    }

    private static ClaimsIdentity CreateClaimsIdentity(string userId, Dictionary<string, object>? userData)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("sub", userId)
        };

        if (userData != null)
        {
            foreach (var kvp in userData)
            {
                if (kvp.Key.Equals("roles", StringComparison.OrdinalIgnoreCase) && kvp.Value is System.Text.Json.JsonElement rolesElement && rolesElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var role in rolesElement.EnumerateArray())
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role.GetString() ?? string.Empty));
                    }
                }
                else
                {
                    claims.Add(new Claim(kvp.Key, kvp.Value?.ToString() ?? string.Empty));
                }
            }
        }

        return new ClaimsIdentity(claims, "SuperTokens");
    }
}
