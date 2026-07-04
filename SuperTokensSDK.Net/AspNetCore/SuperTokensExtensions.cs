using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperTokensSDK.Net.Configuration;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;
using SuperTokensSDK.Net.Recipes.EmailPassword;
using SuperTokensSDK.Net.Recipes.Session;
using SuperTokensSDK.Net.Recipes.UserMetadata;
using SuperTokensSDK.Net.Recipes.UserRoles;

namespace SuperTokensSDK.Net.AspNetCore;

/// <summary>
/// Validates SuperTokens session and sets HttpContext.User.
/// Uses typed SuperTokens exceptions to decide whether to continue anonymously.
/// </summary>
public class SuperTokensMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SuperTokensMiddleware> _logger;

    public SuperTokensMiddleware(RequestDelegate next, ILogger<SuperTokensMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, ICoreApiClient coreApiClient, IOptions<SuperTokensOptions> options)
    {
        var opts = options.Value;
        var accessToken = ExtractAccessToken(context, opts);

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            var antiCsrfToken = context.Request.Cookies[opts.AntiCsrfCookieName]
                ?? context.Request.Headers[Core.Constants.HeaderNames.AntiCsrf].FirstOrDefault();

            try
            {
                var verifyRequest = new VerifySessionRequest
                {
                    AccessToken = accessToken,
                    DoAntiCsrfCheck = false,  // API clients (Bearer token) don't use anti-CSRF
                };

                // Only send antiCsrfToken for cookie-based sessions (browser clients)
                if (opts.EnableAntiCsrf && !string.IsNullOrEmpty(antiCsrfToken))
                {
                    verifyRequest.AntiCsrfToken = antiCsrfToken;
                    verifyRequest.DoAntiCsrfCheck = true;
                }

                var verifyResult = await coreApiClient.VerifySessionAsync(verifyRequest, context.RequestAborted);

                if (!string.IsNullOrWhiteSpace(verifyResult.Session?.UserId))
                {
                    var identity = CreateClaimsIdentity(verifyResult.Session.UserId, verifyResult.Session.UserDataInJWT);
                    context.User = new ClaimsPrincipal(identity);
                }
            }
            catch (UnauthorizedException)
            {
                _logger.LogDebug("SuperTokens middleware: session is unauthorised.");
            }
            catch (TryRefreshTokenException)
            {
                _logger.LogDebug("SuperTokens middleware: access token expired, refresh required.");
            }
            catch (TokenTheftDetectedException ex)
            {
                _logger.LogWarning("SuperTokens middleware: token theft detected for user {UserId}.", ex.UserId);
            }
            catch (InvalidClaimException ex)
            {
                _logger.LogDebug("SuperTokens middleware: invalid claims detected: {Claims}",
                    string.Join(", ", ex.InvalidClaims.Select(c => c.Id)));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "SuperTokens middleware could not validate session.");
            }
        }

        await _next(context);
    }

    private static string? ExtractAccessToken(HttpContext context, SuperTokensOptions options)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        if (context.Request.Cookies.TryGetValue(options.AccessTokenCookieName, out var cookieToken) && !string.IsNullOrWhiteSpace(cookieToken))
        {
            return cookieToken;
        }

        if (context.Request.Path.StartsWithSegments("/hubs") && context.Request.Query.TryGetValue("access_token", out var queryToken))
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
                claims.Add(new Claim(kvp.Key, kvp.Value?.ToString() ?? string.Empty));
            }
        }

        return new ClaimsIdentity(claims, "SuperTokens");
    }
}

public static class SuperTokensMiddlewareExtensions
{
    public static IApplicationBuilder UseSuperTokensMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SuperTokensMiddleware>();
    }
}

public static class SuperTokensExtensions
{
    public static IServiceCollection AddSuperTokens(this IServiceCollection services, Action<SuperTokensOptions> configure)
    {
        services.Configure(configure);
        services.AddHttpClient<ICoreApiClient, CoreApiClient>((provider, client) =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<SessionRecipe>();
        services.AddScoped<EmailPasswordRecipe>();
        services.AddScoped<UserRolesRecipe>();
        services.AddScoped<UserMetadataRecipe>();
        return services;
    }

    public static AuthenticationBuilder AddSuperTokensAuthentication(this AuthenticationBuilder builder, string scheme = "SuperTokens")
    {
        builder.AddScheme<SuperTokensAuthenticationOptions, SuperTokensAuthenticationHandler>(scheme, scheme, _ => { });
        builder.Services.AddSingleton<IClaimsTransformation, SuperTokensClaimsTransformation>();
        return builder;
    }
}
