> **Warning: Work in Progress**
>
> This SDK is under active development and has not been audited or certified by SuperTokens.
> While unit tests cover many code paths, the SDK has not been tested against a live
> SuperTokens Core in all scenarios. Security-critical flows (session verification, JWT
> validation, anti-CSRF) are still being hardened. Do not use in production without thorough
> testing of your specific integration.
>
> Feedback, bug reports, and test contributions are very welcome. See [Contributing](#contributing)
> below or open an issue on [GitHub](https://github.com/Hann1bal/SuperTokensNetSdk).

# SuperTokensSDK.Net

SuperTokens integration for ASP.NET Core. CDI 5.0 client, authentication handler, session middleware, twelve recipe wrappers (EmailPassword, Session, UserRoles, UserMetadata, TOTP, Passwordless, EmailVerification, Jwt, Multitenancy, ThirdParty, Dashboard), EmailDelivery and SmsDelivery ingredients, an API dispatching middleware, and an MCP gateway.

- **Package**: `SuperTokensSDK.Net`
- **Version**: 2.7.5
- **Target framework**: net10.0
- **Dependencies**: MailKit, libphonenumber-csharp, Microsoft.IdentityModel.Tokens, System.IdentityModel.Tokens.Jwt. All are widely-used, well-maintained packages.

## Installation

The package is available on NuGet.org:

```bash
dotnet add package SuperTokensSDK.Net --version 2.7.5
```

Or via the Package Manager Console:

```powershell
Install-Package SuperTokensSDK.Net -Version 2.7.5
```

Or in the .csproj:

```xml
<PackageReference Include="SuperTokensSDK.Net" Version="2.7.5" />
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
app.UseSuperTokensApi();
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

### ThirdParty OAuth signin

```csharp
using SuperTokensSDK.Net.Recipes.ThirdParty;

app.MapPost("/auth/signinup", async (
    ThirdPartyRecipe thirdParty,
    string providerId,
    string code) =>
{
    var response = await thirdParty.SignInUpAsync(
        providerId,
        code,
        redirectURI: "https://example.com/callback");

    if (response is null)
        return Results.Unauthorized();

    return Results.Ok(new
    {
        response.User.Id,
        response.User.Email
    });
});
```

### Dashboard user listing

```csharp
using SuperTokensSDK.Net.Recipes.Dashboard;

app.MapGet("/admin/users", async (
    DashboardRecipe dashboard,
    int? limit,
    string? paginationToken) =>
{
    var result = await dashboard.GetUsersAsync(
        limit: limit ?? 100,
        paginationToken: paginationToken);
    return Results.Ok(result);
}).RequireAuthorization("admin");
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

## API dispatching middleware

`SuperTokensApiMiddleware` exposes the SuperTokens Frontend Driver Interface (FDI) on your API domain so `supertokens-web-js` can talk to your backend instead of directly to Core. Call `app.UseSuperTokensApi()` after `app.UseSuperTokensMiddleware()`.

The middleware handles:

- **EmailPassword** — `POST /auth/signup` and `POST /auth/signin` are implemented end-to-end:
  - Parses the FDI `formFields` body.
  - Calls `EmailPasswordRecipe.SignUpAsync` / `SignInAsync`.
  - Creates a session via `SessionRecipe.CreateSessionAsync`.
  - Sets `sAccessToken`, `sRefreshToken`, `sAntiCsrf` cookies and the `front-token` header.
- **Session refresh** — `POST /auth/session/refresh` is implemented end-to-end:
  - Reads `sRefreshToken` from the request cookie.
  - Reads `sAntiCsrf` from the cookie or `anti-csrf` header.
  - Calls `SessionRecipe.RefreshSessionAsync`, building the CDI body exactly like the official SuperTokens backend SDKs.
  - Returns `401 { "status": "UNAUTHORISED" }` when the refresh token is missing, which stops pre-login infinite refresh loops in the browser SDK.
  - Attaches refreshed cookies and the `front-token` header.
- **Other recipes** — Passwordless, ThirdParty, EmailVerification, TOTP, and session sign-out/sign-out are proxied to Core CDI.
- **CORS** — Preflight responses include `Access-Control-Allow-Origin`, credentials, and exposed headers (`rid`, `fdi-version`, `anti-csrf`, `front-token`).

```csharp
var app = builder.Build();

app.UseSuperTokensMiddleware();
app.UseSuperTokensApi();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

## Overridable recipe interfaces

Each recipe has a matching override class with nullable delegate properties. Set a delegate to replace the default behavior. Recipes check the override before calling Core.

Override classes live in `SuperTokensSDK.Net.Core`:

- `EmailPasswordOverrides`
- `SessionOverrides`
- `PasswordlessOverrides`
- `ThirdPartyOverrides`

```csharp
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Recipes.EmailPassword;

builder.Services.AddSuperTokens(options =>
{
    options.CoreUri = "http://localhost:3567";
    options.ApiKey = "your-core-api-key";
    options.AppName = "MyApp";
    options.ApiDomain = "https://api.example.com";
    options.WebsiteDomain = "https://example.com";
});

// Replace the default SignUpAsync with a custom implementation
builder.Services.Configure<EmailPasswordOverrides>(overrides =>
{
    overrides.SignUp = async (email, password, ct) =>
    {
        // Custom logic here, then return a UserResponse or null
        return null;
    };
});
```

When a delegate is null, the recipe falls back to its default Core call. This lets you override one method without reimplementing the rest.

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
| ThirdParty recipe | `ThirdPartyRecipe` | `SuperTokensSDK.Net.Recipes.ThirdParty` |
| Dashboard recipe | `DashboardRecipe` | `SuperTokensSDK.Net.Recipes.Dashboard` |
| API middleware | `SuperTokensApiMiddleware` | `SuperTokensSDK.Net.AspNetCore` |
| Email delivery | `IEmailDelivery`, `SmtpEmailDelivery` | `SuperTokensSDK.Net.Ingredients.EmailDelivery` |
| SMS delivery | `ISmsDelivery`, `TwilioSmsDelivery` | `SuperTokensSDK.Net.Ingredients.SmsDelivery` |
| Recipe overrides | `RecipeOverrides`, `IOverridableRecipe` | `SuperTokensSDK.Net.Core` |
| Auth handler | `SuperTokensAuthenticationHandler` | `SuperTokensSDK.Net.AspNetCore` |
| Session middleware | `SuperTokensMiddleware` | `SuperTokensSDK.Net.AspNetCore` |
| MCP gateway | `McpGateway`, `McpTools` | `SuperTokensSDK.Net.Mcp` |
| Exceptions | `SuperTokensException` and subclasses | `SuperTokensSDK.Net.Core` |
| Constants | `Constants` | `SuperTokensSDK.Net.Core` |

## Contributing

This is a community-driven SDK, not an official SuperTokens product. It is developed in the open and contributions are welcome.

### Areas that need help

- **Integration tests** against a live SuperTokens Core (Docker-based test harness)
- **Security review** of the session verification flow (JWKS, anti-CSRF, token refresh)
- **Recipe coverage** - several recipes lack comprehensive tests (TotpRecipe, PasswordlessRecipe, JwksClient, BooleanClaim, PrimitiveClaim)
- **Documentation** - code examples for ThirdParty OAuth providers, Dashboard recipe, and overridable recipe interfaces

### How to contribute

1. Fork the [repository](https://github.com/Hann1bal/SuperTokensNetSdk)
2. Create a feature branch (`git checkout -b feat/my-feature`)
3. Write tests for your changes
4. Ensure `dotnet build` and `dotnet test` pass with 0 errors
5. Open a pull request with a clear description

### Reporting issues

Found a bug or security concern? Please [open an issue on GitHub](https://github.com/Hann1bal/SuperTokensNetSdk/issues) with:
- SDK version
- SuperTokens Core version and CDI version
- Minimal reproduction steps
- Expected vs actual behavior
