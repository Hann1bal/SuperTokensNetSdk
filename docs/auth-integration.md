# ASP.NET Core Authentication Integration

The SDK provides three integration points for ASP.NET Core: DI registration, an authentication handler, and per-request middleware. Together they wire SuperTokens into the standard ASP.NET Core auth pipeline so that `[Authorize]` attributes, role checks, and claims-based authorization all work.

## DI Registration

### AddSuperTokens()

Registers the Core client and all four recipes as scoped services.

```csharp
public static IServiceCollection AddSuperTokens(
    this IServiceCollection services,
    Action<SuperTokensOptions> configure)
```

This call:
1. Binds `SuperTokensOptions` from the configuration delegate
2. Registers `CoreApiClient` as `ICoreApiClient` using `IHttpClientFactory` (30 second timeout)
3. Registers `SessionRecipe`, `EmailPasswordRecipe`, `UserRolesRecipe`, `UserMetadataRecipe` as scoped

```csharp
builder.Services.AddSuperTokens(options =>
{
    options.CoreUri = "http://localhost:3567";
    options.AppName = "MyApp";
    options.ApiKey = "your-key";
});
```

### AddSuperTokensAuthentication()

Registers the SuperTokens authentication scheme and claims transformation.

```csharp
public static AuthenticationBuilder AddSuperTokensAuthentication(
    this AuthenticationBuilder builder,
    string scheme = "SuperTokens")
```

This call:
1. Registers `SuperTokensAuthenticationHandler` with the given scheme name (default: `"SuperTokens"`)
2. Registers `SuperTokensClaimsTransformation` as a singleton `IClaimsTransformation`

```csharp
builder.Services
    .AddAuthentication()
    .AddSuperTokensAuthentication();
```

### UseSuperTokensMiddleware()

Adds the `SuperTokensMiddleware` to the request pipeline.

```csharp
public static IApplicationBuilder UseSuperTokensMiddleware(
    this IApplicationBuilder builder)
```

The middleware runs before `UseAuthentication` and validates the session on every request. If the session is valid, it sets `HttpContext.User` to a claims principal. If the session is invalid or missing, it calls the next middleware without setting the principal.

```csharp
app.UseSuperTokensMiddleware();
app.UseAuthentication();
app.UseAuthorization();
```

## SuperTokensAuthenticationHandler

The authentication handler integrates SuperTokens with the ASP.NET Core authentication pipeline. It is registered as a named scheme (default: `"SuperTokens"`) and can be used with `[Authorize]` attributes.

### Token Extraction Order

The handler extracts the access token from three sources, in this order:

| Priority | Source | Condition |
|---|---|---|
| 1 | Bearer header | `Authorization: Bearer <token>` |
| 2 | Cookie | Cookie named `sAccessToken` (configurable) |
| 3 | SignalR query string | `?access_token=<token>` on paths starting with `/hubs` |

```csharp
private string? ExtractAccessToken()
{
    // 1. Bearer header
    var authHeader = Request.Headers.Authorization.FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(authHeader)
        && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        return authHeader.Substring("Bearer ".Length).Trim();
    }

    // 2. Cookie
    if (Request.Cookies.TryGetValue(Options.AccessTokenCookieName, out var cookieToken)
        && !string.IsNullOrWhiteSpace(cookieToken))
    {
        return cookieToken;
    }

    // 3. SignalR query string (for /hubs paths only)
    if (Request.Path.StartsWithSegments("/hubs")
        && Request.Query.TryGetValue("access_token", out var queryToken))
    {
        return queryToken.FirstOrDefault();
    }

    return null;
}
```

The SignalR fallback exists because browsers cannot set custom headers on WebSocket connections. SignalR clients pass the token as a query string parameter instead.

### Session Verification

After extracting the token, the handler calls `CoreApiClient.VerifySessionAsync` with the token and optional anti-CSRF token. If verification succeeds, it builds a `ClaimsIdentity` and returns `AuthenticateResult.Success`.

If verification fails for any reason (invalid token, expired, network error), the handler returns `AuthenticateResult.Fail("Invalid or expired session.")`.

### Claims Identity Construction

The handler builds claims from the session data:

```csharp
var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, userId),
    new Claim("sub", userId)
};

foreach (var kvp in userData)
{
    if (kvp.Key.Equals("roles", StringComparison.OrdinalIgnoreCase)
        && kvp.Value is JsonElement { ValueKind: JsonValueKind.Array } rolesElement)
    {
        foreach (var role in rolesElement.EnumerateArray())
        {
            claims.Add(new Claim(ClaimTypes.Role, role.GetString() ?? ""));
        }
    }
    else
    {
        claims.Add(new Claim(kvp.Key, kvp.Value?.ToString() ?? ""));
    }
}

return new ClaimsIdentity(claims, "SuperTokens");
```

The `roles` array in the JWT payload gets expanded into individual `ClaimTypes.Role` claims. This means `[Authorize(Roles = "admin")]` works without any additional configuration.

## SuperTokensClaimsTransformation

The claims transformation runs after the authentication handler. It looks for a `roles` or `role` claim (comma-separated string) and expands it into individual `ClaimTypes.Role` claims.

```csharp
public class SuperTokensClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity
            || identity.HasClaim(c => c.Type == ClaimTypes.Role))
        {
            return Task.FromResult(principal);
        }

        var roleClaim = identity.FindFirst("roles") ?? identity.FindFirst("role");
        if (roleClaim != null && !string.IsNullOrWhiteSpace(roleClaim.Value))
        {
            var roles = roleClaim.Value.Split(',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var role in roles)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }

        return Task.FromResult(principal);
    }
}
```

This handles the case where roles are stored as a comma-separated string claim rather than a JSON array. The transformation is idempotent: it skips principals that already have role claims.

## SuperTokensMiddleware

The middleware validates the session on every request, independent of the authentication pipeline. It runs before `UseAuthentication` and sets `HttpContext.User` if the session is valid.

### Per-Request Session Validation

```csharp
public async Task InvokeAsync(
    HttpContext context,
    ICoreApiClient coreApiClient,
    IOptions<SuperTokensOptions> options)
```

The middleware:
1. Extracts the access token (same order as the auth handler)
2. Extracts the anti-CSRF token from cookie or header
3. Calls `VerifySessionAsync` on the Core client
4. Sets `HttpContext.User` if verification succeeds

### Exception Handling

The middleware catches all SuperTokens exceptions and logs them at Debug level. It does not short-circuit the request. Instead, it lets the request continue without an authenticated principal, which means downstream middleware and controllers can decide how to handle unauthenticated requests.

| Exception | Log Level | Behavior |
|---|---|---|
| `UnauthorizedException` | Debug | Continue without principal |
| `TryRefreshTokenException` | Debug | Continue without principal |
| `TokenTheftDetectedException` | Warning | Continue without principal, logs user ID |
| `InvalidClaimException` | Debug | Continue without principal, logs claim IDs |
| `Exception` (catch-all) | Debug | Continue without principal |

This design means the middleware never blocks a request. If you want to reject unauthenticated requests, use `[Authorize]` on your controllers or endpoints.

## Cookie Management

The SDK does not automatically set or clear cookies. You are responsible for writing cookies to the HTTP response after session creation, refresh, or revocation.

### Setting cookies after signin

```csharp
app.MapPost("/auth/signin", async (
    EmailPasswordRecipe emailPassword,
    SessionRecipe session,
    HttpResponse response,
    string email, string password) =>
{
    var user = await emailPassword.SignInAsync(email, password);
    if (user is null)
        return Results.Unauthorized();

    var container = await session.CreateSessionAsync(user.Id!);

    var cookieOptions = new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/"
    };

    response.Cookies.Append("sAccessToken", container.AccessToken!, cookieOptions);
    response.Cookies.Append("sRefreshToken", container.RefreshToken!, cookieOptions);

    if (container.AntiCsrfToken is not null)
        response.Cookies.Append("sAntiCsrf", container.AntiCsrfToken, cookieOptions);

    return Results.Ok(new { container.SessionHandle });
});
```

### Clearing cookies after logout

```csharp
app.MapPost("/auth/logout", async (
    SessionRecipe session,
    HttpResponse response,
    string sessionHandle) =>
{
    await session.RevokeSessionAsync(sessionHandle);

    response.Cookies.Delete("sAccessToken");
    response.Cookies.Delete("sRefreshToken");
    response.Cookies.Delete("sAntiCsrf");

    return Results.Ok();
});
```

## Full Program.cs Example with Dual Auth

This example shows how to run JWT bearer authentication alongside SuperTokens. Requests can authenticate with either scheme.

```csharp
using SuperTokensSDK.Net.AspNetCore;
using SuperTokensSDK.Net.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// SuperTokens
builder.Services.AddSuperTokens(options =>
{
    options.CoreUri = "http://localhost:3567";
    options.ApiKey = builder.Configuration["SuperTokens:ApiKey"];
    options.AppName = "MyApp";
    options.ApiDomain = "https://api.example.com";
    options.WebsiteDomain = "https://example.com";
});

// Dual auth: JWT bearer + SuperTokens
builder.Services
    .AddAuthentication()
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    })
    .AddSuperTokensAuthentication("SuperTokens");

builder.Services.AddAuthorization(options =>
{
    // Default policy: accept either scheme
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes("Bearer", "SuperTokens")
        .Build();

    // Admin policy: require admin role
    options.AddPolicy("admin", policy =>
        policy.RequireAuthenticatedUser()
              .AddAuthenticationSchemes("Bearer", "SuperTokens")
              .RequireRole("admin"));
});

var app = builder.Build();

app.UseSuperTokensMiddleware();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

## Bridging with ASP.NET Core Identity

You can bridge SuperTokens with ASP.NET Core Identity by using SuperTokens as the session provider and Identity for user management. The key is to sync user IDs between the two systems.

### Step 1: Register Identity and SuperTokens

```csharp
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddSuperTokens(options =>
{
    options.CoreUri = "http://localhost:3567";
    options.AppName = "MyApp";
});

builder.Services
    .AddAuthentication()
    .AddSuperTokensAuthentication();
```

### Step 2: Create users in both systems

```csharp
app.MapPost("/auth/register", async (
    UserManager<IdentityUser> userManager,
    EmailPasswordRecipe emailPassword,
    UserRolesRecipe userRoles,
    string email, string password) =>
{
    // Create in Identity
    var identityUser = new IdentityUser { UserName = email, Email = email };
    var result = await userManager.CreateAsync(identityUser, password);
    if (!result.Succeeded)
        return Results.BadRequest(result.Errors);

    // Create in SuperTokens
    var stUser = await emailPassword.SignUpAsync(email, password);
    if (stUser is null)
        return Results.BadRequest("SuperTokens signup failed.");

    // Link the two by storing the SuperTokens user ID on the Identity user
    identityUser.SecurityStamp = stUser.Id;
    await userManager.UpdateAsync(identityUser);

    await userRoles.AddRoleAsync(stUser.Id!, "user");

    return Results.Ok(new { identityUser.Id, stUser.Id });
});
```

### Step 3: Use SuperTokens for session management

When a user signs in, verify credentials through Identity and create a session through SuperTokens:

```csharp
app.MapPost("/auth/login", async (
    SignInManager<IdentityUser> signInManager,
    EmailPasswordRecipe emailPassword,
    SessionRecipe session,
    UserRolesRecipe userRoles,
    HttpResponse response,
    string email, string password) =>
{
    var identityUser = await signInManager.UserManager.FindByEmailAsync(email);
    if (identityUser is null)
        return Results.Unauthorized();

    var result = await signInManager.CheckPasswordSignInAsync(
        identityUser, password, lockoutOnFailure: false);
    if (!result.Succeeded)
        return Results.Unauthorized();

    // Use the SuperTokens user ID stored on the Identity user
    var stUserId = identityUser.SecurityStamp!;
    var roles = await userRoles.GetRolesAsync(stUserId);

    var container = await session.CreateSessionAsync(
        stUserId,
        accessTokenPayload: new Dictionary<string, object>
        {
            ["roles"] = roles.ToArray()
        });

    response.Cookies.Append("sAccessToken", container.AccessToken!);
    response.Cookies.Append("sRefreshToken", container.RefreshToken!);

    return Results.Ok(new { container.SessionHandle });
});
```

## What's Next

- [Recipes](./recipes.md): Full reference for session creation, verification, and revocation
- [Configuration](./configuration.md): All options for cookies, anti-CSRF, and Core connection
- [Migration](./migration.md): How to migrate from JWT tokens to SuperTokens cookies
- [Examples](./examples.md): Example 7 shows a complete Identity bridge
- [Troubleshooting](./troubleshooting.md): Common auth integration errors
