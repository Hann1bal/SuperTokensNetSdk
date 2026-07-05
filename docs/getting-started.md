# Getting Started with SuperTokensSDK.Net

## What is SuperTokensSDK.Net

SuperTokensSDK.Net is a CDI 5.0 client for the SuperTokens Core. It gives ASP.NET Core applications a native way to manage authentication, sessions, user roles, and user metadata through the SuperTokens Core Driver Interface.

The SDK ships four recipe wrappers (EmailPassword, Session, UserRoles, UserMetadata), an ASP.NET Core authentication handler, per-request session middleware, claims transformation, and an MCP gateway for AI agent integration. It targets .NET 10 and depends only on the `Microsoft.AspNetCore.App` shared framework. No external NuGet packages required.

## Minimum Requirements

| Requirement | Version |
|---|---|
| .NET SDK | 10.0 or later |
| ASP.NET Core | 10.0 or later |
| SuperTokens Core | 11.x |
| CDI version | 5.0 |

The SDK negotiates CDI version 5.0 with the Core at startup. Core 11.x is the matching release because it exposes the CDI 5.0 API surface.

## Installation

The package is published to GitHub Packages. You need a GitHub Personal Access Token with `read:packages` scope to pull it.

### Step 1: Add the GitHub Packages source

Create or edit `nuget.config` in your project root:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="github" value="https://nuget.pkg.github.com/Hann1bal/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="YOUR_GITHUB_USERNAME" />
      <add key="ClearTextPassword" value="YOUR_PERSONAL_ACCESS_TOKEN" />
    </github>
  </packageSourceCredentials>
</configuration>
```

### Step 2: Install the package

```bash
dotnet add package SuperTokensSDK.Net --version 2.2.0
```

Or via the Package Manager Console:

```powershell
Install-Package SuperTokensSDK.Net -Version 2.2.0
```

## 5-Minute Quickstart

This quickstart wires up the SDK, creates a user, signs them in, and verifies their session. You will need a running SuperTokens Core instance on port 3567.

### 1. Register the SDK in Program.cs

Three calls wire up everything: the Core client, the recipes, the authentication scheme, and the session middleware.

```csharp
using SuperTokensSDK.Net.AspNetCore;
using SuperTokensSDK.Net.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSuperTokens(options =>
{
    options.CoreUri = "http://localhost:3567";
    options.ApiKey = "your-core-api-key";
    options.AppName = "MyApp";
    options.ApiDomain = "https://api.example.com";
    options.WebsiteDomain = "https://example.com";
});

builder.Services
    .AddAuthentication()
    .AddSuperTokensAuthentication();

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSuperTokensMiddleware();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

The `AddSuperTokens` call registers `CoreApiClient`, `SessionRecipe`, `EmailPasswordRecipe`, `UserRolesRecipe`, and `UserMetadataRecipe` in the DI container. The `AddSuperTokensAuthentication` call registers the authentication handler and claims transformation. The `UseSuperTokensMiddleware` call adds per-request session validation.

### 2. Sign up a user

```csharp
using SuperTokensSDK.Net.Recipes.EmailPassword;
using SuperTokensSDK.Net.Recipes.UserRoles;

app.MapPost("/auth/signup", async (
    EmailPasswordRecipe emailPassword,
    UserRolesRecipe userRoles,
    string email, string password, string role) =>
{
    var user = await emailPassword.SignUpAsync(email, password);
    if (user is null)
        return Results.BadRequest("Signup failed.");

    await userRoles.AddRoleAsync(user.Id!, role);
    return Results.Ok(new { user.Id, user.Email });
});
```

### 3. Sign in and create a session

```csharp
using SuperTokensSDK.Net.Recipes.Session;

app.MapPost("/auth/signin", async (
    EmailPasswordRecipe emailPassword,
    SessionRecipe session,
    string email, string password) =>
{
    var user = await emailPassword.SignInAsync(email, password);
    if (user is null)
        return Results.Unauthorized();

    var container = await session.CreateSessionAsync(
        user.Id!,
        accessTokenPayload: new Dictionary<string, object>
        {
            ["roles"] = new[] { "user" }
        });

    return Results.Ok(new
    {
        container.AccessToken,
        container.RefreshToken,
        container.SessionHandle
    });
});
```

### 4. Verify a session

```csharp
using SuperTokensSDK.Net.Core;

app.MapGet("/me", async (SessionRecipe session, HttpRequest request) =>
{
    var token = request.Headers.Authorization.ToString()
        .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);

    try
    {
        var container = await session.VerifySessionAsync(token);
        var principal = container.GetClaimsPrincipal();
        var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var roles = principal.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value);
        return Results.Ok(new { userId, roles });
    }
    catch (UnauthorizedException)
    {
        return Results.Unauthorized();
    }
    catch (TryRefreshTokenException)
    {
        return Results.Json(new { status = "TRY_REFRESH_TOKEN" }, statusCode: 401);
    }
});
```

The `VerifySessionAsync` method decodes the JWT locally instead of calling the Core verify endpoint. This works around a bug in Core 11.x where the verify endpoint rejects requests even when anti-CSRF checking is disabled. The access token is a standard RS256 JWT signed by Core, so local decoding is safe. Expiry is checked during decoding.

## How the pieces fit together

```
Browser  -->  ASP.NET Core  -->  SuperTokensMiddleware  -->  Your controllers
                   |                      |
                   v                      v
          AuthenticationHandler    CoreApiClient (CDI 5.0)
                   |                      |
                   v                      v
          ClaimsTransformation    SuperTokens Core (port 3567)
```

The middleware runs on every request. It extracts the access token, verifies it, and sets `HttpContext.User` to a claims principal built from the session data. The authentication handler does the same thing but through the standard ASP.NET Core authentication pipeline, which means `[Authorize]` attributes work out of the box.

## What's Next

- [Configuration](./configuration.md): All 9 options, multi-host failover, CDI version negotiation, rate limit retry
- [Recipes](./recipes.md): Full reference for EmailPassword, Session, UserRoles, and UserMetadata recipes
- [Auth Integration](./auth-integration.md): ASP.NET Core authentication handler, middleware, claims transformation, dual auth setup
- [Examples](./examples.md): Complete worked examples for signup, RBAC, metadata, sessions, MCP, and more
