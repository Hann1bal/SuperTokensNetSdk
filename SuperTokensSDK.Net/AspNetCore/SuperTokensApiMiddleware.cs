using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SuperTokensSDK.Net.AspNetCore;

/// <summary>
/// Proxies frontend /auth API calls to the appropriate SuperTokens Core CDI endpoint.
/// This middleware is opt-in: register it with <see cref="SuperTokensApiExtensions.UseSuperTokensApi"/>.
/// </summary>
public class SuperTokensApiMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly Dictionary<(string Path, string Method), (string CdiPath, string RecipeId)> RouteMap = new()
    {
        // EmailPassword
        { ("/auth/signup", "POST"), ("/recipe/signup", "emailpassword") },
        { ("/auth/signin", "POST"), ("/recipe/signin", "emailpassword") },
        { ("/auth/reset-password", "POST"), ("/recipe/user/password/reset", "emailpassword") },
        { ("/auth/signup/email/exists", "GET"), ("/recipe/signup/email/exists", "emailpassword") },

        // Session
        { ("/auth/session/refresh", "POST"), ("/recipe/session/refresh", "session") },
        { ("/auth/signout", "POST"), ("/recipe/session/revoke", "session") },
        { ("/auth/session", "GET"), ("/recipe/session", "session") },

        // Passwordless
        { ("/auth/signinup/code", "POST"), ("/recipe/signinup/code", "passwordless") },
        { ("/auth/signinup/code/consume", "POST"), ("/recipe/signinup/code/consume", "passwordless") },
        { ("/auth/signinup/email/exists", "GET"), ("/recipe/passwordless/email/exists", "passwordless") },
        { ("/auth/signinup/phonenumber/exists", "GET"), ("/recipe/passwordless/phonenumber/exists", "passwordless") },

        // ThirdParty
        { ("/auth/signinup", "POST"), ("/recipe/signinup", "thirdparty") },
        { ("/auth/authorisationurl", "GET"), ("/recipe/signinup", "thirdparty") },

        // EmailVerification
        { ("/auth/user/email/verify", "POST"), ("/recipe/user/email/verify", "emailverification") },
        { ("/auth/user/email/verify/token", "POST"), ("/recipe/user/email/verify/token", "emailverification") },
        { ("/auth/user/email/verify", "GET"), ("/recipe/user/email/verify", "emailverification") },

        // TOTP
        { ("/auth/totp/device", "POST"), ("/recipe/totp/device", "totp") },
        { ("/auth/totp/device/verify", "POST"), ("/recipe/totp/device/verify", "totp") },
        { ("/auth/totp/verify", "POST"), ("/recipe/totp/verify", "totp") },
        { ("/auth/totp/device/list", "GET"), ("/recipe/totp/device/list", "totp") },
    };

    public SuperTokensApiMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context, Core.ICoreApiClient coreApiClient)
    {
        var path = context.Request.Path.Value?.TrimEnd('/') ?? "";

        if (!path.StartsWith("/auth"))
        {
            await _next(context);
            return;
        }

        AddCorsHeaders(context);

        if (context.Request.Method == "OPTIONS")
        {
            return;
        }

        if (RouteMap.TryGetValue((path, context.Request.Method), out var route))
        {
            string? body = null;
            if (!HttpMethods.IsGet(context.Request.Method) &&
                !HttpMethods.IsHead(context.Request.Method) &&
                !HttpMethods.IsDelete(context.Request.Method))
            {
                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body);
                body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            var cdiPath = route.CdiPath;
            if (context.Request.QueryString.HasValue)
                cdiPath += context.Request.QueryString.Value;

            var response = await coreApiClient.ProxyToCoreAsync(context.Request.Method, cdiPath, body, route.RecipeId, context.RequestAborted);

            context.Response.StatusCode = (int)response.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(await response.Content.ReadAsStringAsync(context.RequestAborted));
            return;
        }

        await _next(context);
    }

    private static void AddCorsHeaders(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.FirstOrDefault();
        if (!string.IsNullOrEmpty(origin))
        {
            context.Response.Headers.AccessControlAllowOrigin = origin;
            context.Response.Headers.AccessControlAllowCredentials = "true";
            context.Response.Headers.AccessControlExposeHeaders = "rid, fdi-version, anti-csrf";
        }

        if (context.Request.Method == "OPTIONS")
        {
            context.Response.Headers.AccessControlAllowMethods = "GET, POST, PUT, DELETE, OPTIONS";
            context.Response.Headers.AccessControlAllowHeaders = "content-type, rid, fdi-version, anti-csrf";
            context.Response.StatusCode = 200;
        }
    }
}

public static class SuperTokensApiExtensions
{
    public static IApplicationBuilder UseSuperTokensApi(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SuperTokensApiMiddleware>();
    }
}
