using System.Text;
using System.Text.Json;
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

namespace SuperTokensSDK.Net.AspNetCore;

/// <summary>
/// Proxies frontend /auth API calls to the appropriate SuperTokens Core CDI endpoint.
/// Implements EmailPassword /auth/signup and /auth/signin end-to-end so the SDK can
/// transform FDI requests into CDI calls, create sessions and attach cookies.
/// This middleware is opt-in: register it with <see cref="SuperTokensApiExtensions.UseSuperTokensApi"/>.
/// </summary>
public class SuperTokensApiMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SuperTokensOptions _options;
    private readonly ILogger<SuperTokensApiMiddleware> _logger;
    private readonly HashSet<string> _allowedOrigins = new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions FdiJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly Dictionary<(string Path, string Method), (string CdiPath, string RecipeId)> RouteMap = new()
    {
        // EmailPassword (signup/signin are handled end-to-end below)
        { ("/auth/reset-password", "POST"), ("/recipe/user/password/reset", "emailpassword") },
        { ("/auth/signup/email/exists", "GET"), ("/recipe/signup/email/exists", "emailpassword") },

        // Session (refresh is handled end-to-end below)
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

    public SuperTokensApiMiddleware(RequestDelegate next, IOptions<SuperTokensOptions> options, ILogger<SuperTokensApiMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        foreach (var origin in _options.AllowedOrigins)
        {
            if (!string.IsNullOrWhiteSpace(origin))
            {
                _allowedOrigins.Add(origin.Trim());
            }
        }
    }

    public async Task InvokeAsync(HttpContext context, ICoreApiClient coreApiClient)
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

        if (path == "/auth/signup" && HttpMethods.IsPost(context.Request.Method))
        {
            await HandleSignUpAsync(context);
            return;
        }

        if (path == "/auth/signin" && HttpMethods.IsPost(context.Request.Method))
        {
            await HandleSignInAsync(context);
            return;
        }

        if (path == "/auth/session/refresh" && HttpMethods.IsPost(context.Request.Method))
        {
            await HandleRefreshAsync(context);
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
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            var cdiPath = route.CdiPath;
            if (context.Request.QueryString.HasValue)
                cdiPath += context.Request.QueryString.Value;

            var response = await coreApiClient.ProxyToCoreAsync(context.Request.Method, cdiPath, body, route.RecipeId, context.RequestAborted);

            context.Response.StatusCode = (int)response.StatusCode;
            context.Response.ContentType = "application/json";

            // Forward Set-Cookie headers (and any other response headers) from SuperTokens Core.
            // Without this, sAccessToken / sRefreshToken never reach the browser.
            foreach (var header in response.Headers)
            {
                if (header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var value in header.Value)
                    {
                        context.Response.Headers.Append("Set-Cookie", value);
                    }
                }
                else if (!context.Response.Headers.ContainsKey(header.Key) &&
                         !header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) &&
                         !header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) &&
                         !header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Headers.Append(header.Key, header.Value.ToArray());
                }
            }

            await context.Response.WriteAsync(await response.Content.ReadAsStringAsync(context.RequestAborted));
            return;
        }

        await _next(context);
    }

    private async Task HandleSignUpAsync(HttpContext context)
    {
        var (email, password, fieldErrors) = await ExtractEmailPasswordAsync(context);
        if (email is null)
        {
            await WriteJsonAsync(context, 400, new { status = "FIELD_ERROR", formFields = fieldErrors });
            return;
        }

        var emailPassword = context.RequestServices.GetRequiredService<EmailPasswordRecipe>();
        var sessionRecipe = context.RequestServices.GetRequiredService<SessionRecipe>();

        try
        {
            var user = await emailPassword.SignUpAsync(email!, password!, context.RequestAborted);
            if (user is null)
            {
                await WriteJsonAsync(context, 400, new { status = "SIGN_UP_NOT_ALLOWED", message = "Sign up failed" });
                return;
            }

            var container = await sessionRecipe.CreateSessionAsync(
                user.Id!,
                accessTokenPayload: new Dictionary<string, object>(),
                cancellationToken: context.RequestAborted);

            AttachSessionCookies(context, container);
            await WriteJsonAsync(context, 200, new { status = "OK", user = new { user.Id, user.Email, user.TimeJoined } });
        }
        catch (SuperTokensException ex) when (IsCoreStatus(ex, "EMAIL_ALREADY_EXISTS_ERROR"))
        {
            _logger.LogWarning("Sign-up failed for {Email}: email already exists", email);
            await WriteJsonAsync(context, 200, new
            {
                status = "FIELD_ERROR",
                formFields = new[] { new { id = "email", error = "This email already exists. Please sign in instead." } }
            });
        }
        catch (SuperTokensException ex)
        {
            _logger.LogWarning(ex, "Sign-up failed for {Email}", email);
            await WriteJsonAsync(context, 400, new { status = "SIGN_UP_NOT_ALLOWED", message = ex.Message });
        }
    }

    private async Task HandleSignInAsync(HttpContext context)
    {
        var (email, password, fieldErrors) = await ExtractEmailPasswordAsync(context);
        if (email is null)
        {
            await WriteJsonAsync(context, 400, new { status = "FIELD_ERROR", formFields = fieldErrors });
            return;
        }

        var emailPassword = context.RequestServices.GetRequiredService<EmailPasswordRecipe>();
        var sessionRecipe = context.RequestServices.GetRequiredService<SessionRecipe>();

        try
        {
            var user = await emailPassword.SignInAsync(email!, password!, context.RequestAborted);
            if (user is null)
            {
                await WriteJsonAsync(context, 401, new { status = "WRONG_CREDENTIALS_ERROR" });
                return;
            }

            var container = await sessionRecipe.CreateSessionAsync(
                user.Id!,
                accessTokenPayload: new Dictionary<string, object>(),
                cancellationToken: context.RequestAborted);

            AttachSessionCookies(context, container);
            await WriteJsonAsync(context, 200, new { status = "OK", user = new { user.Id, user.Email, user.TimeJoined } });
        }
        catch (SuperTokensException ex) when (IsCoreStatus(ex, "WRONG_CREDENTIALS_ERROR"))
        {
            _logger.LogWarning("Sign-in failed for {Email}: wrong credentials", email);
            await WriteJsonAsync(context, 401, new { status = "WRONG_CREDENTIALS_ERROR" });
        }
        catch (SuperTokensException ex)
        {
            _logger.LogWarning(ex, "Sign-in failed for {Email}", email);
            await WriteJsonAsync(context, 401, new { status = "WRONG_CREDENTIALS_ERROR" });
        }
    }

    private async Task HandleRefreshAsync(HttpContext context)
    {
        var refreshToken = context.Request.Cookies[_options.RefreshTokenCookieName];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogDebug("Session refresh rejected: no refresh token cookie.");
            await WriteJsonAsync(context, 401, new { status = "UNAUTHORISED", message = "Refresh token is missing." });
            return;
        }

        string? antiCsrfToken = null;
        if (_options.EnableAntiCsrf)
        {
            antiCsrfToken = context.Request.Headers[Core.Constants.HeaderNames.AntiCsrf].FirstOrDefault()
                ?? context.Request.Cookies[_options.AntiCsrfCookieName];

            if (string.IsNullOrWhiteSpace(antiCsrfToken))
            {
                _logger.LogDebug("Session refresh rejected: anti-CSRF token required but missing.");
                await WriteJsonAsync(context, 401, new { status = "UNAUTHORISED", message = "Anti-CSRF token is missing." });
                return;
            }
        }

        var sessionRecipe = context.RequestServices.GetRequiredService<SessionRecipe>();

        try
        {
            var container = await sessionRecipe.RefreshSessionAsync(
                refreshToken,
                antiCsrfToken,
                context.RequestAborted);

            AttachSessionCookies(context, container);
            await WriteJsonAsync(context, 200, new { status = "OK" });
        }
        catch (SuperTokensException ex)
        {
            _logger.LogWarning(ex, "Session refresh failed.");
            await WriteJsonAsync(context, 401, new { status = "UNAUTHORISED", message = ex.Message });
        }
    }

    private async Task<(string? Email, string? Password, object[] FieldErrors)> ExtractEmailPasswordAsync(HttpContext context)
    {
        context.Request.EnableBuffering();
        string body;
        using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync();
        }
        context.Request.Body.Position = 0;

        FormFieldsRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<FormFieldsRequest>(body, FdiJsonOptions);
        }
        catch (JsonException)
        {
            request = null;
        }

        var errors = new List<object>();
        var formFields = request?.FormFields;

        if (formFields is null || formFields.Count == 0)
        {
            errors.Add(new { id = "email", error = "Field is required" });
            errors.Add(new { id = "password", error = "Field is required" });
            return (null, null, errors.ToArray());
        }

        var fieldMap = formFields
            .Where(f => !string.IsNullOrEmpty(f.Id))
            .ToDictionary(f => f.Id!, f => f.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        if (!fieldMap.TryGetValue("email", out var email) || string.IsNullOrWhiteSpace(email))
        {
            errors.Add(new { id = "email", error = "Field is required" });
        }

        if (!fieldMap.TryGetValue("password", out var password) || string.IsNullOrWhiteSpace(password))
        {
            errors.Add(new { id = "password", error = "Field is required" });
        }

        if (errors.Count > 0)
        {
            return (null, null, errors.ToArray());
        }

        return (email, password, Array.Empty<object>());
    }

    private void AttachSessionCookies(HttpContext context, SessionContainer container)
    {
        var secure = _options.UseSecureCookies;
        var sameSite = SameSiteMode.Lax;

        var accessCookieOptions = new CookieOptions
        {
            HttpOnly = false,
            Secure = secure,
            SameSite = sameSite,
            Path = "/",
            Expires = container.AccessTokenExpiry
        };

        var refreshCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Path = "/auth/session/refresh",
            Expires = container.RefreshTokenExpiry
        };

        if (!string.IsNullOrEmpty(container.AccessToken))
        {
            context.Response.Cookies.Append(_options.AccessTokenCookieName, container.AccessToken, accessCookieOptions);
        }

        if (!string.IsNullOrEmpty(container.RefreshToken))
        {
            context.Response.Cookies.Append(_options.RefreshTokenCookieName, container.RefreshToken, refreshCookieOptions);
        }

        if (_options.EnableAntiCsrf && !string.IsNullOrEmpty(container.AntiCsrfToken))
        {
            var antiCsrfOptions = new CookieOptions
            {
                HttpOnly = false,
                Secure = secure,
                SameSite = sameSite,
                Path = "/",
                Expires = container.RefreshTokenExpiry
            };
            context.Response.Cookies.Append(_options.AntiCsrfCookieName, container.AntiCsrfToken, antiCsrfOptions);
        }

        var frontToken = BuildFrontToken(container);
        if (!string.IsNullOrEmpty(frontToken))
        {
            context.Response.Headers[Core.Constants.HeaderNames.FrontToken] = frontToken;
        }
    }

    private string? BuildFrontToken(SessionContainer container)
    {
        if (string.IsNullOrEmpty(container.AccessToken) ||
            string.IsNullOrEmpty(container.UserId) ||
            container.AccessTokenExpiry == DateTime.MinValue)
        {
            return null;
        }

        var expiryMs = new DateTimeOffset(container.AccessTokenExpiry).ToUnixTimeMilliseconds();
        var payload = new
        {
            uid = container.UserId,
            ate = expiryMs,
            up = container.UserDataInJwt
        };

        var json = JsonSerializer.Serialize(payload, FdiJsonOptions);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private static async Task WriteJsonAsync(HttpContext context, int statusCode, object value)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, value, FdiJsonOptions, context.RequestAborted);
    }

    private static bool IsCoreStatus(SuperTokensException exception, string status)
    {
        return exception.Message.Contains(status, StringComparison.OrdinalIgnoreCase);
    }

    private void AddCorsHeaders(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.FirstOrDefault();
        if (!string.IsNullOrEmpty(origin))
        {
            // When an allowlist is configured, only reflect origins that match.
            // An empty allowlist preserves backward-compatible (insecure) behavior
            // and is logged once per middleware instance to surface the risk.
            if (_allowedOrigins.Count > 0)
            {
                if (_allowedOrigins.Contains(origin))
                {
                    context.Response.Headers.AccessControlAllowOrigin = origin;
                    context.Response.Headers.AccessControlAllowCredentials = "true";
                    context.Response.Headers.AccessControlExposeHeaders = "rid, fdi-version, anti-csrf, front-token";
                }
                else
                {
                    _logger.LogWarning(
                        "CORS request from origin '{Origin}' rejected because it is not in SuperTokensOptions.AllowedOrigins.",
                        origin);
                }
            }
            else
            {
                _logger.LogWarning(
                    "SuperTokensOptions.AllowedOrigins is empty; reflecting CORS origin '{Origin}' with credentials. " +
                    "Configure AllowedOrigins in production to prevent credential leakage.",
                    origin);
                context.Response.Headers.AccessControlAllowOrigin = origin;
                context.Response.Headers.AccessControlAllowCredentials = "true";
                context.Response.Headers.AccessControlExposeHeaders = "rid, fdi-version, anti-csrf, front-token";
            }
        }

        if (context.Request.Method == "OPTIONS")
        {
            context.Response.Headers.AccessControlAllowMethods = "GET, POST, PUT, DELETE, OPTIONS";
            context.Response.Headers.AccessControlAllowHeaders = "content-type, rid, fdi-version, anti-csrf";
            context.Response.StatusCode = 200;
        }
    }

    private sealed class FormField
    {
        public string Id { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    private sealed class FormFieldsRequest
    {
        public List<FormField> FormFields { get; set; } = new();
    }
}

public static class SuperTokensApiExtensions
{
    public static IApplicationBuilder UseSuperTokensApi(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SuperTokensApiMiddleware>();
    }
}
