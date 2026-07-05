# SuperTokensSDK.Net

SuperTokens integration for ASP.NET Core. CDI 5.0 client, authentication handler, session middleware, six recipe wrappers (EmailPassword, Session, UserRoles, UserMetadata, TOTP, Passwordless) and an MCP gateway.

- **Package**: `SuperTokensSDK.Net`
- **Version**: 2.4.1
- **Target framework**: net10.0
- **Dependencies**: none. The package references only the `Microsoft.AspNetCore.App` shared framework. No external NuGet packages.

## Installation

The package is available on NuGet.org:

```bash
dotnet add package SuperTokensSDK.Net --version 2.4.1
```

Or via the Package Manager Console:

```powershell
Install-Package SuperTokensSDK.Net -Version 2.4.1
```

Or in the .csproj:

```xml
<PackageReference Include="SuperTokensSDK.Net" Version="2.4.1" />
```

## Quick start

Register the SDK in `Program.cs`. Three calls wire up the Core client, the recipes, the authentication scheme, and the session middleware.

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

## Configuration options

All options live on `SuperTokensOptions` in the `SuperTokensSDK.Net.Configuration` namespace.

| Property | Type | Default | Description |
|---|---|---|---|
| `CoreUri` | `string?` | `null` | Base URI of the SuperTokens Core service. Separate multiple hosts with a semicolon for round-robin failover. Required. |
| `ApiKey` | `string?` | `null` | API key sent in the `api-key` header to Core. |
| `AppName` | `string?` | `null` | Application name used by Core. Required. |
| `ApiDomain` | `string?` | `null` | API domain sent during CDI version negotiation. |
| `WebsiteDomain` | `string?` | `null` | Website domain used for cookie and CSRF handling. |
| `AccessTokenCookieName` | `string` | `"sAccessToken"` | Name of the access token cookie. |
| `RefreshTokenCookieName` | `string` | `"sRefreshToken"` | Name of the refresh token cookie. |
| `AntiCsrfCookieName` | `string` | `"sAntiCsrf"` | Name of the anti-CSRF token cookie. |
| `EnableAntiCsrf` | `bool` | `true` | Turns anti-CSRF protection on for cookie-based sessions. |

### Multi-host failover

`CoreUri` accepts multiple hosts separated by semicolons. The client cycles through them round-robin style and fails over when a host throws an `HttpRequestException` or times out:

```
options.CoreUri = "http://core-1:3567;http://core-2:3567;http://core-3:3567";
```

## Code examples

### Signup with role assignment

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

### Signin with session creation

```csharp
using SuperTokensSDK.Net.Recipes.EmailPassword;
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

### Session verify with claims

```csharp
using SuperTokensSDK.Net.Recipes.Session;
using SuperTokensSDK.Net.Core;

app.MapGet("/me", async (SessionRecipe session, string token) =>
{
    try
    {
        var container = await session.VerifySessionAsync(token);
        var principal = container.GetClaimsPrincipal();
        var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var roles = principal.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value);
        return Results.Ok(new { userId, roles });
    }
    catch (UnauthorizedException ex)
    {
        return Results.Unauthorized();
    }
});
```

### Role management

```csharp
using SuperTokensSDK.Net.Recipes.UserRoles;

app.MapGet("/users/{userId}/roles", async (UserRolesRecipe userRoles, string userId) =>
{
    var roles = await userRoles.GetRolesAsync(userId);
    return Results.Ok(roles);
});

app.MapPost("/users/{userId}/roles", async (UserRolesRecipe userRoles, string userId, string role) =>
{
    await userRoles.AddRoleAsync(userId, role);
    return Results.Ok();
});

app.MapDelete("/users/{userId}/roles", async (UserRolesRecipe userRoles, string userId, string role) =>
{
    await userRoles.RemoveRoleAsync(userId, role);
    return Results.Ok();
});
```

### Metadata management

```csharp
using SuperTokensSDK.Net.Recipes.UserMetadata;

app.MapPost("/users/{userId}/metadata", async (
    UserMetadataRecipe metadata,
    string userId,
    Dictionary<string, object> update) =>
{
    await metadata.UpdateMetadataAsync(userId, update);
    return Results.Ok();
});

app.MapGet("/users/{userId}/metadata", async (UserMetadataRecipe metadata, string userId) =>
{
    var data = await metadata.GetMetadataAsync(userId);
    return Results.Ok(data);
});
```

### Error handling

The SDK throws typed exceptions for every non-OK status returned by Core. Catch them to send the right HTTP response.

```csharp
using SuperTokensSDK.Net.Core;

try
{
    var container = await session.VerifySessionAsync(token);
}
catch (UnauthorizedException)
{
    // Token is invalid or expired. Return 401.
}
catch (TryRefreshTokenException)
{
    // Access token expired. Tell the client to refresh.
}
catch (TokenTheftDetectedException ex)
{
    // Refresh token reuse detected. Revoke all sessions for ex.UserId.
    // ex.SessionHandle identifies the compromised session.
}
catch (InvalidClaimException ex)
{
    // Claim validation failed. ex.InvalidClaims lists what went wrong.
    foreach (var claim in ex.InvalidClaims)
        Console.WriteLine($"{claim.Id}: {claim.Reason}");
}
catch (SuperTokensException ex)
{
    // Catch-all for any other SDK error.
}
```

## API reference

The full API reference covers every class, method, model, and constant in the SDK:

[Full API reference](./docs/supertokens-sdk.md)

## What the SDK provides

| Area | Class | Namespace |
|---|---|---|
| Configuration | `SuperTokensOptions` | `SuperTokensSDK.Net.Configuration` |
| DI registration | `SuperTokensExtensions` | `SuperTokensSDK.Net.AspNetCore` |
| Core HTTP client | `CoreApiClient`, `ICoreApiClient` | `SuperTokensSDK.Net.Core` |
| EmailPassword recipe | `EmailPasswordRecipe` | `SuperTokensSDK.Net.Recipes.EmailPassword` |
| Session recipe | `SessionRecipe`, `SessionContainer` | `SuperTokensSDK.Net.Recipes.Session` |
| UserRoles recipe | `UserRolesRecipe` | `SuperTokensSDK.Net.Recipes.UserRoles` |
| UserMetadata recipe | `UserMetadataRecipe` | `SuperTokensSDK.Net.Recipes.UserMetadata` |
| Auth handler | `SuperTokensAuthenticationHandler` | `SuperTokensSDK.Net.AspNetCore` |
| Session middleware | `SuperTokensMiddleware` | `SuperTokensSDK.Net.AspNetCore` |
| MCP gateway | `McpGateway`, `McpTools` | `SuperTokensSDK.Net.Mcp` |
| Exceptions | `SuperTokensException` and subclasses | `SuperTokensSDK.Net.Core` |
| Constants | `Constants` | `SuperTokensSDK.Net.Core` |
