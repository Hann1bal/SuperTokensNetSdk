# SuperTokensSDK.Net API Reference

**Package**: `SuperTokensSDK.Net` 2.2.0
**Target framework**: net10.0
**Dependencies**: none (only `Microsoft.AspNetCore.App` shared framework reference)

## Overview

SuperTokensSDK.Net is a CDI 5.0 client for the SuperTokens Core service, built for ASP.NET Core. The SDK has four parts:

1. **CoreApiClient** talks to SuperTokens Core over HTTP using the Core Driver Interface. It handles CDI version negotiation, multi-host failover, rate-limit retries, and typed error responses.
2. **Recipes** wrap the CoreApiClient with high-level operations for EmailPassword, Session, UserRoles, and UserMetadata.
3. **ASP.NET Core integration** provides an authentication handler, a claims transformation, and a session validation middleware that populates `HttpContext.User` on every request.
4. **MCP gateway** exposes five SuperTokens operations as Model Context Protocol tool definitions.

The SDK follows the SuperTokens recipe pattern. Each recipe is a scoped service that depends on `ICoreApiClient`. You inject recipes into your controllers and endpoints directly.

## Installation

The package ships as a local NuGet feed artifact. Place the `.nupkg` in your feed directory and reference it in `nuget.config`:

```xml
<configuration>
  <packageSources>
    <add key="LocalPackages" value="./local-packages" />
  </packageSources>
</configuration>
```

```bash
dotnet add package SuperTokensSDK.Net --version 2.2.0
```

## Configuration

### `SuperTokensOptions`

Namespace: `SuperTokensSDK.Net.Configuration`

File: `Configuration/SuperTokensOptions.cs`

| Property | Type | Default | Description |
|---|---|---|---|
| `CoreUri` | `string?` | `null` | Base URI of the SuperTokens Core service. Separate multiple hosts with semicolons for round-robin failover. Required. |
| `ApiKey` | `string?` | `null` | API key sent in the `api-key` header on every Core request. |
| `AppName` | `string?` | `null` | Application name registered with Core. Required. |
| `ApiDomain` | `string?` | `null` | API domain sent as a query parameter during CDI version negotiation. |
| `WebsiteDomain` | `string?` | `null` | Website domain used for cookie and CSRF handling, sent during CDI version negotiation. |
| `AccessTokenCookieName` | `string` | `"sAccessToken"` | Cookie name the middleware reads for the access token. |
| `RefreshTokenCookieName` | `string` | `"sRefreshToken"` | Cookie name for the refresh token. |
| `AntiCsrfCookieName` | `string` | `"sAntiCsrf"` | Cookie name for the anti-CSRF token. |
| `EnableAntiCsrf` | `bool` | `true` | When true, the middleware sends the anti-CSRF token for cookie-based sessions. |

#### Multi-host failover

`CoreUri` accepts a semicolon-separated list of Core hosts. The `CoreApiClient` parses them into an internal list and cycles through them round-robin using `Interlocked.Increment`. When a host throws an `HttpRequestException` or the request times out, the client moves to the next host. If every host fails, it throws a `SuperTokensException`.

```csharp
options.CoreUri = "http://core-1:3567;http://core-2:3567;http://core-3:3567";
```

## DI registration

### `SuperTokensExtensions`

Namespace: `SuperTokensSDK.Net.AspNetCore`

File: `AspNetCore/SuperTokensExtensions.cs`

#### `AddSuperTokens(Action<SuperTokensOptions> configure)`

Registers the options, the `CoreApiClient` as an `HttpClient`-backed `ICoreApiClient`, and all four recipes as scoped services.

```csharp
builder.Services.AddSuperTokens(options =>
{
    options.CoreUri = "http://localhost:3567";
    options.ApiKey = "your-core-api-key";
    options.AppName = "MyApp";
    options.ApiDomain = "https://api.example.com";
    options.WebsiteDomain = "https://example.com";
});
```

What gets registered:

| Service | Lifetime | Implementation |
|---|---|---|
| `IOptions<SuperTokensOptions>` | Singleton | Options pattern |
| `ICoreApiClient` | HttpClient | `CoreApiClient` (30s timeout) |
| `SessionRecipe` | Scoped | `SessionRecipe` |
| `EmailPasswordRecipe` | Scoped | `EmailPasswordRecipe` |
| `UserRolesRecipe` | Scoped | `UserRolesRecipe` |
| `UserMetadataRecipe` | Scoped | `UserMetadataRecipe` |

#### `AddSuperTokensAuthentication(string scheme = "SuperTokens")`

Extension on `AuthenticationBuilder`. Registers the `SuperTokensAuthenticationHandler` for the given scheme and adds `SuperTokensClaimsTransformation` as a singleton `IClaimsTransformation`.

```csharp
builder.Services
    .AddAuthentication()
    .AddSuperTokensAuthentication();
```

#### `UseSuperTokensMiddleware()`

Extension on `IApplicationBuilder` (lives in `SuperTokensMiddlewareExtensions`). Registers `SuperTokensMiddleware`, which validates the session on every request and sets `HttpContext.User` when a valid access token is present.

```csharp
app.UseSuperTokensMiddleware();
```

## CoreApiClient

### `ICoreApiClient`

Namespace: `SuperTokensSDK.Net.Core`

File: `Core/ICoreApiClient.cs`

Interface with 13 methods that map to CDI 5.0 endpoints.

### `CoreApiClient`

Namespace: `SuperTokensSDK.Net.Core`

File: `Core/CoreApiClient.cs`

#### Constructor

```csharp
public CoreApiClient(HttpClient httpClient, IOptions<SuperTokensOptions> options, ILogger<CoreApiClient> logger)
```

- `httpClient` is injected by `AddHttpClient` with a 30-second timeout.
- `options` provides `CoreUri`, `ApiKey`, `ApiDomain`, `WebsiteDomain`.
- `logger` is the standard `ILogger<CoreApiClient>`.

On construction, the client parses `CoreUri` into a list of `Uri` hosts and adds the `api-key` header to the default request headers if `ApiKey` is set.

#### CDI version negotiation

The client negotiates the CDI version lazily on the first request. It calls `GET /apiversion?apiDomain=...&websiteDomain=...` and picks the highest version from `Constants.SupportedCdiVersions` that the server supports. The result is cached behind a `SemaphoreSlim` so negotiation runs once, thread-safely.

If no version matches, the client throws:

> `SuperTokensException`: SuperTokens Core does not support any of the SDK CDI versions: 5.0

#### Multi-host failover

The client stores hosts as an `IReadOnlyList<Uri>`. `GetNextHost()` uses `Interlocked.Increment` on a `long` counter and takes the modulo to pick a host. On `HttpRequestException` or `TaskCanceledException` (timeout), the client breaks out of the retry loop and tries the next host.

#### Rate limit retry

When Core returns HTTP 429, the client retries up to `Constants.RateLimitRetries` (5) times with backoff. The delay is `10 + (250 * retry)` milliseconds, so retries wait 10ms, 260ms, 510ms, 760ms, 1010ms.

#### Local JWT verification workaround

SuperTokens Core 11.x has a bug in `/recipe/session/verify` that rejects `doAntiCsrfCheck` even when it is not sent. As a workaround, `CoreApiClient.VerifySessionAsync` does not call the Core endpoint. Instead, it decodes the JWT payload locally, checks the `exp` claim, extracts the `sub` (userId) and `sessionHandle`, and strips protected fields to build `UserDataInJWT`.

Protected fields excluded from `UserDataInJWT`:

`sub`, `iat`, `exp`, `sessionHandle`, `parentRefreshTokenHash1`, `refreshTokenHash1`, `antiCsrfToken`, `rsub`, `tId`

#### Method reference

| # | Method | HTTP | CDI path | `rid` header | Returns |
|---|---|---|---|---|---|
| 1 | `CreateSessionAsync(CreateSessionRequest, CancellationToken)` | POST | `/recipe/session` | `session` | `CreateOrRefreshAPIResponse` |
| 2 | `VerifySessionAsync(VerifySessionRequest, CancellationToken)` | (local JWT decode) | (not sent to Core) | `session` | `GetSessionResponse` |
| 3 | `RefreshSessionAsync(RefreshSessionRequest, CancellationToken)` | POST | `/recipe/session/refresh` | `session` | `CreateOrRefreshAPIResponse` |
| 4 | `RevokeSessionAsync(RevokeSessionRequest, CancellationToken)` | POST | `/recipe/session/revoke` | `session` | `RevokeSessionResponse` |
| 5 | `SignUpAsync(SignUpRequest, CancellationToken)` | POST | `/recipe/signup` | `emailpassword` | `SignUpResponse` |
| 6 | `SignInAsync(SignUpRequest, CancellationToken)` | POST | `/recipe/signin` | `emailpassword` | `SignUpResponse` |
| 7 | `ResetPasswordAsync(PasswordResetRequest, CancellationToken)` | POST | `/recipe/user/password/reset` | `emailpassword` | `StatusResponse` |
| 8 | `AddUserRolesAsync(UserRolesRequest, CancellationToken)` | PUT | `/recipe/user/roles` | `userroles` | `StatusResponse` |
| 9 | `GetUserRolesAsync(string userId, CancellationToken)` | GET | `/recipe/user/roles?userId=...` | `userroles` | `UserRolesResponse` |
| 10 | `RemoveUserRolesAsync(UserRolesRequest, CancellationToken)` | DELETE | `/recipe/user/roles` | `userroles` | `StatusResponse` |
| 11 | `DoesRoleExistAsync(string userId, string role, CancellationToken)` | GET | `/recipe/user/role?userId=...&role=...` | `userroles` | `RoleExistsResponse` |
| 12 | `GetUserMetadataAsync(string userId, CancellationToken)` | GET | `/recipe/user/metadata?userId=...` | `usermetadata` | `UserMetadataResponse` |
| 13 | `UpdateUserMetadataAsync(UserMetadataUpdateRequest, CancellationToken)` | PUT | `/recipe/user/metadata` | `usermetadata` | `StatusResponse` |

Every request includes the `cdi-version` header. Recipe paths (those starting with `/recipe/`) also include the `rid` header.

#### Response handling

The client deserializes responses with camelCase JSON options. It checks the `status` field in the response body and throws typed exceptions for non-OK statuses. See the Exception hierarchy section below.

## Recipes

### EmailPasswordRecipe

Namespace: `SuperTokensSDK.Net.Recipes.EmailPassword`

File: `Recipes/EmailPassword/EmailPasswordRecipe.cs`

Constructor takes `ICoreApiClient`.

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `SignUpAsync` | `string email`, `string password`, `CancellationToken` | `Task<UserResponse?>` | Creates a new user. Returns the user or null. |
| `SignInAsync` | `string email`, `string password`, `CancellationToken` | `Task<UserResponse?>` | Signs in an existing user. Returns the user or null. |
| `ResetPasswordAsync` | `string userId`, `string newPassword`, `CancellationToken` | `Task` | Resets the password for a user. |

#### `UserResponse`

Namespace: `SuperTokensSDK.Net.Core.Models`

| Property | Type | JSON key |
|---|---|---|
| `Id` | `string?` | `id` |
| `Email` | `string?` | `email` |
| `TimeJoined` | `long` | `timeJoined` |

### SessionRecipe

Namespace: `SuperTokensSDK.Net.Recipes.Session`

File: `Recipes/Session/SessionRecipe.cs`

Constructor takes `ICoreApiClient`.

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `CreateSessionAsync` | `string userId`, `Dictionary<string, object>? accessTokenPayload = null`, `Dictionary<string, object>? sessionData = null`, `CancellationToken` | `Task<SessionContainer>` | Creates a new session. `accessTokenPayload` becomes `UserDataInJWT`. `sessionData` becomes `UserDataInDatabase`. Anti-CSRF is enabled by default. |
| `VerifySessionAsync` | `string accessToken`, `string? antiCsrfToken = null`, `CancellationToken` | `Task<SessionContainer>` | Verifies an access token. Decodes the JWT locally (Core 11.x workaround). Throws `UnauthorizedException` if the token is invalid or expired. |
| `RefreshSessionAsync` | `string refreshToken`, `string? antiCsrfToken = null`, `CancellationToken` | `Task<SessionContainer>` | Refreshes a session using the refresh token. Anti-CSRF is enabled only when `antiCsrfToken` is provided. |
| `RevokeSessionAsync` | `string sessionHandle`, `CancellationToken` | `Task` | Revokes a session by its handle. |

### SessionContainer

Namespace: `SuperTokensSDK.Net.Recipes.Session`

File: `Recipes/Session/SessionContainer.cs`

Wraps the result of session creation, verification, refresh, and revocation.

#### Properties

| Property | Type | Description |
|---|---|---|
| `SessionHandle` | `string` | Unique session identifier from Core. |
| `UserId` | `string` | The user this session belongs to. |
| `AccessToken` | `string?` | The access token string. Set on create and refresh. Set on verify from the input token. |
| `RefreshToken` | `string?` | The refresh token string. Set on create and refresh. |
| `AntiCsrfToken` | `string?` | Anti-CSRF token from Core. Set on create and refresh. |
| `AccessTokenExpiry` | `DateTime` | UTC expiry of the access token. Parsed from the Core response in milliseconds. |
| `RefreshTokenExpiry` | `DateTime` | UTC expiry of the refresh token. Parsed from the Core response in milliseconds. |
| `UserDataInJwt` | `Dictionary<string, object>` | Custom claims from the JWT payload. Excludes protected fields. |

#### Methods

##### `GetClaimsPrincipal()`

```csharp
public ClaimsPrincipal GetClaimsPrincipal()
```

Builds a `ClaimsPrincipal` from the session. The `ClaimsIdentity` has authentication type `"SuperTokens"` and includes:

- `ClaimTypes.NameIdentifier` claim with the `UserId`
- `sub` claim with the `UserId`
- One `ClaimTypes.Role` claim for each entry in the `roles` array inside `UserDataInJwt` (when the value is a JSON array)
- One claim per other key in `UserDataInJwt`, using the key as the claim type

##### `GetClaim<T>(string key, T? defaultValue = default)`

```csharp
public T? GetClaim<T>(string key, T? defaultValue = default)
```

Returns a typed value from `UserDataInJwt`. If the stored value is a `JsonElement`, it deserializes it to `T`. Returns `defaultValue` if the key is missing or the value cannot be converted.

### UserRolesRecipe

Namespace: `SuperTokensSDK.Net.Recipes.UserRoles`

File: `Recipes/UserRoles/UserRolesRecipe.cs`

Constructor takes `ICoreApiClient`.

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `AddRoleAsync` | `string userId`, `string role`, `CancellationToken` | `Task` | Adds a single role to a user. |
| `AddRolesAsync` | `string userId`, `IEnumerable<string> roles`, `CancellationToken` | `Task` | Adds multiple roles to a user. |
| `GetRolesAsync` | `string userId`, `CancellationToken` | `Task<IReadOnlyList<string>>` | Returns all roles assigned to a user. |
| `RemoveRoleAsync` | `string userId`, `string role`, `CancellationToken` | `Task` | Removes a single role from a user. |
| `RemoveRolesAsync` | `string userId`, `IEnumerable<string> roles`, `CancellationToken` | `Task` | Removes multiple roles from a user. |
| `DoesRoleExistAsync` | `string userId`, `string role`, `CancellationToken` | `Task<bool>` | Checks whether a specific role is assigned to a user. |

### UserMetadataRecipe

Namespace: `SuperTokensSDK.Net.Recipes.UserMetadata`

File: `Recipes/UserMetadata/UserMetadataRecipe.cs`

Constructor takes `ICoreApiClient`.

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `GetMetadataAsync` | `string userId`, `CancellationToken` | `Task<Dictionary<string, object>?>` | Returns the metadata dictionary for a user, or null. |
| `UpdateMetadataAsync` | `string userId`, `Dictionary<string, object> update`, `CancellationToken` | `Task` | Updates metadata for a user. |
| `GetMetadataAsAsync<T>` | `string userId`, `CancellationToken` | `Task<T?>` | Returns metadata deserialized to a typed object. `T` must be a class with a parameterless constructor. Serializes the dictionary to JSON, then deserializes to `T`. |

## ASP.NET Core integration

### SuperTokensAuthenticationHandler

Namespace: `SuperTokensSDK.Net.AspNetCore`

File: `AspNetCore/SuperTokensAuthenticationHandler.cs`

Inherits `AuthenticationHandler<SuperTokensAuthenticationOptions>`.

#### Token extraction order

`HandleAuthenticateAsync` calls `ExtractAccessToken`, which checks three sources in order:

1. **Authorization header**. If the `Authorization` header starts with `Bearer ` (case-insensitive), the token is the part after `Bearer `.
2. **Cookie**. If the `AccessTokenCookieName` cookie is present and non-empty, the token is the cookie value.
3. **Query string**. Only for SignalR hub requests (path starts with `/hubs`). If the `access_token` query parameter is present, the token is the query value.

If no token is found, authentication fails with `"No access token provided."`.

#### Verification

The handler calls `ICoreApiClient.VerifySessionAsync` with the extracted access token. The anti-CSRF token is read from the cookie or the `anti-csrf` header. `DoAntiCsrfCheck` is set to true only when an anti-CSRF token is present.

On success, the handler builds a `ClaimsIdentity` with:

- `ClaimTypes.NameIdentifier` and `sub` claims set to the userId
- `ClaimTypes.Role` claims for each entry in the `roles` array inside `UserDataInJWT`
- One claim per other key in `UserDataInJWT`

On any exception, authentication fails with `"Invalid or expired session."`.

### SuperTokensClaimsTransformation

Namespace: `SuperTokensSDK.Net.AspNetCore`

File: `AspNetCore/SuperTokensClaimsTransformation.cs`

Implements `IClaimsTransformation`. Registered as a singleton by `AddSuperTokensAuthentication`.

If the principal already has a `ClaimTypes.Role` claim, it returns unchanged. Otherwise, it looks for a `roles` or `role` claim, splits its value by commas, and adds each as a `ClaimTypes.Role` claim.

### SuperTokensMiddleware

Namespace: `SuperTokensSDK.Net.AspNetCore`

File: `AspNetCore/SuperTokensExtensions.cs` (same file as the extensions class)

Registered by `UseSuperTokensMiddleware()`.

#### Behavior

On every request, the middleware extracts the access token using the same order as the authentication handler (Bearer header, cookie, query string for `/hubs`). If a token is present, it calls `ICoreApiClient.VerifySessionAsync` and sets `HttpContext.User` to a `ClaimsPrincipal` when verification succeeds.

The middleware catches typed exceptions and logs them at debug or warning level:

| Exception | Log level | Behavior |
|---|---|---|
| `UnauthorizedException` | Debug | Request continues anonymously. |
| `TryRefreshTokenException` | Debug | Request continues anonymously. |
| `TokenTheftDetectedException` | Warning | Request continues anonymously. Logs the `UserId`. |
| `InvalidClaimException` | Debug | Request continues anonymously. Logs the invalid claim IDs. |
| Any other `Exception` | Debug | Request continues anonymously. |

The middleware always calls `_next(context)`. It never short-circuits the request. Protected endpoints should use standard ASP.NET Core authorization (`[Authorize]`, role policies, etc.) to reject anonymous requests.

### SuperTokensAuthenticationOptions

Namespace: `SuperTokensSDK.Net.AspNetCore`

File: `AspNetCore/SuperTokensAuthenticationOptions.cs`

Inherits `AuthenticationSchemeOptions`.

| Property | Type | Default |
|---|---|---|
| `AccessTokenCookieName` | `string` | `"sAccessToken"` |
| `RefreshTokenCookieName` | `string` | `"sRefreshToken"` |
| `AntiCsrfCookieName` | `string` | `"sAntiCsrf"` |

## MCP gateway

### `McpGateway`

Namespace: `SuperTokensSDK.Net.Mcp`

File: `Mcp/McpGateway.cs`

Constructor takes `McpTools`.

#### `GetToolDefinitions()`

```csharp
public IReadOnlyList<McpToolDefinition> GetToolDefinitions()
```

Returns five tool definitions. Each `McpToolDefinition` has a `Name`, `Description`, and `InputSchema` (a JSON schema dictionary).

| Tool | Description | Required arguments | Optional arguments |
|---|---|---|---|
| `create_user` | Create a new SuperTokens user with email, password, and optional role. | `email`, `password` | `role` |
| `verify_session` | Verify a SuperTokens access token. | `token` | |
| `get_user_roles` | Get roles assigned to a SuperTokens user. | `userId` | |
| `assign_role` | Assign a role to a SuperTokens user. | `userId`, `role` | |
| `revoke_session` | Revoke a SuperTokens session by session handle. | `sessionHandle` | |

#### `ExecuteToolAsync(McpToolRequest request, CancellationToken cancellationToken = default)`

```csharp
public async Task<McpToolResult> ExecuteToolAsync(McpToolRequest request, CancellationToken cancellationToken = default)
```

Dispatches the tool by name (case-insensitive). Returns an `McpToolResult` with `IsError = true` and a text message for unknown tools or exceptions.

### `McpTools`

Namespace: `SuperTokensSDK.Net.Mcp`

File: `Mcp/McpTools.cs`

Backing implementations for the five tools. Constructor takes `EmailPasswordRecipe`, `SessionRecipe`, and `UserRolesRecipe`.

| Method | What it does |
|---|---|
| `CreateUserAsync` | Calls `EmailPasswordRecipe.SignUpAsync`, then `UserRolesRecipe.AddRoleAsync` if a role was provided. Returns the userId, email, and role. |
| `VerifySessionAsync` | Calls `SessionRecipe.VerifySessionAsync`. Returns userId, sessionHandle, and roles from the JWT payload. |
| `GetUserRolesAsync` | Calls `UserRolesRecipe.GetRolesAsync`. Returns userId and roles. |
| `AssignRoleAsync` | Calls `UserRolesRecipe.AddRoleAsync`. Returns userId, role, and status. |
| `RevokeSessionAsync` | Calls `SessionRecipe.RevokeSessionAsync`. Returns sessionHandle and status. |

### MCP models

Namespace: `SuperTokensSDK.Net.Mcp`

File: `Mcp/McpModels.cs`

#### `McpToolDefinition`

| Property | Type | JSON key |
|---|---|---|
| `Name` | `string` | `name` |
| `Description` | `string` | `description` |
| `InputSchema` | `Dictionary<string, object>` | `inputSchema` |

#### `McpToolRequest`

| Property | Type | JSON key |
|---|---|---|
| `Name` | `string` | `name` |
| `Arguments` | `Dictionary<string, object>?` | `arguments` |

#### `McpToolResult`

| Property | Type | JSON key |
|---|---|---|
| `Content` | `List<McpToolContent>` | `content` |
| `IsError` | `bool` | `isError` |

#### `McpToolContent`

| Property | Type | JSON key | Default |
|---|---|---|---|
| `Type` | `string` | `type` | `"text"` |
| `Text` | `string` | `text` | `""` |

## Exception hierarchy

Namespace: `SuperTokensSDK.Net.Core`

### `SuperTokensException`

File: `Core/SuperTokensException.cs`

Base exception for all SDK errors. Inherits `Exception`.

```csharp
public class SuperTokensException : Exception
{
    public SuperTokensException(string message);
    public SuperTokensException(string message, Exception innerException);
}
```

### `UnauthorizedException`

File: `Core/Exceptions.cs`

Thrown when the session is invalid or expired. Maps to the `UNAUTHORISED` status from Core.

### `TryRefreshTokenException`

File: `Core/Exceptions.cs`

Thrown when the access token has expired and the frontend should refresh. Maps to the `TRY_REFRESH_TOKEN` and `NEEDS_REFRESH` statuses from Core.

### `TokenTheftDetectedException`

File: `Core/Exceptions.cs`

Thrown when refresh token reuse is detected. Maps to the `TOKEN_THEFT_DETECTED` status from Core. All sessions for the user should be revoked.

```csharp
public class TokenTheftDetectedException : SuperTokensException
{
    public string SessionHandle { get; }
    public string UserId { get; }
}
```

### `InvalidClaimException`

File: `Core/Exceptions.cs`

Thrown when claim validation fails. Maps to the `INVALID_CLAIMS` status from Core.

```csharp
public class InvalidClaimException : SuperTokensException
{
    public IReadOnlyList<InvalidClaim> InvalidClaims { get; }
}
```

### `InvalidClaim`

File: `Core/Exceptions.cs`

| Property | Type | JSON key | Description |
|---|---|---|---|
| `Id` | `string` | `id` | Identifier of the claim that failed. |
| `Reason` | `string` | `reason` | Why the claim failed validation. |

## Constants

Namespace: `SuperTokensSDK.Net.Core`

File: `Core/Constants.cs`

### `SupportedCdiVersions`

```csharp
public static readonly string[] SupportedCdiVersions = ["5.0"];
```

The SDK supports CDI 5.0 only. The client negotiates the highest matching version with Core.

### `RecipeIds`

| Constant | Value | Used by |
|---|---|---|
| `Session` | `"session"` | SessionRecipe |
| `EmailPassword` | `"emailpassword"` | EmailPasswordRecipe |
| `UserRoles` | `"userroles"` | UserRolesRecipe |
| `UserMetadata` | `"usermetadata"` | UserMetadataRecipe |

### `CookieNames`

| Constant | Value |
|---|---|
| `AccessToken` | `"sAccessToken"` |
| `RefreshToken` | `"sRefreshToken"` |
| `IdRefreshToken` | `"sIdRefreshToken"` |
| `AntiCsrf` | `"sAntiCsrf"` |

### `HeaderNames`

| Constant | Value |
|---|---|
| `AccessToken` | `"st-access-token"` |
| `RefreshToken` | `"st-refresh-token"` |
| `AntiCsrf` | `"anti-csrf"` |
| `FrontToken` | `"front-token"` |
| `AuthMode` | `"st-auth-mode"` |
| `Rid` | `"rid"` |
| `CdiVersion` | `"cdi-version"` |
| `ApiKey` | `"api-key"` |

### `Paths`

| Constant | Value |
|---|---|
| `ApiVersion` | `/apiversion` |
| `RecipeSession` | `/recipe/session` |
| `RecipeSessionVerify` | `/recipe/session/verify` |
| `RecipeSessionRefresh` | `/recipe/session/refresh` |
| `RecipeSessionRevoke` | `/recipe/session/revoke` |
| `RecipeSignUp` | `/recipe/signup` |
| `RecipeSignIn` | `/recipe/signin` |
| `RecipeUserPasswordReset` | `/recipe/user/password/reset` |
| `RecipeUserRoles` | `/recipe/user/roles` |
| `RecipeUserRole` | `/recipe/user/role` |
| `RecipeUserMetadata` | `/recipe/user/metadata` |

### Other constants

| Constant | Value | Description |
|---|---|---|
| `DefaultTenantId` | `"public"` | Default tenant ID used by SuperTokens. |
| `RateLimitStatusCode` | `429` | HTTP status returned by Core when rate limited. |
| `RateLimitRetries` | `5` | Number of retries on 429. |

### `Status`

| Constant | Value |
|---|---|
| `Ok` | `"OK"` |
| `Unauthorized` | `"UNAUTHORISED"` |
| `TryRefreshToken` | `"TRY_REFRESH_TOKEN"` |
| `TokenTheftDetected` | `"TOKEN_THEFT_DETECTED"` |
| `InvalidClaims` | `"INVALID_CLAIMS"` |

## Request and response models

All models live in `SuperTokensSDK.Net.Core.Models`.

### `CreateSessionRequest`

| Property | Type | JSON key | Default |
|---|---|---|---|
| `UserId` | `string` | `userId` | `""` |
| `UserDataInJWT` | `Dictionary<string, object>?` | `userDataInJWT` | `null` |
| `UserDataInDatabase` | `Dictionary<string, object>?` | `userDataInDatabase` | `null` |
| `EnableAntiCsrf` | `bool` | `enableAntiCsrf` | `true` |

### `VerifySessionRequest`

| Property | Type | JSON key | Default |
|---|---|---|---|
| `AccessToken` | `string` | `accessToken` | `""` |
| `AntiCsrfToken` | `string?` | `antiCsrfToken` | `null` |
| `DoAntiCsrfCheck` | `bool` | `doAntiCsrfCheck` | `true` |
| `CheckDatabase` | `bool` | `checkDatabase` | `false` |

### `RefreshSessionRequest`

| Property | Type | JSON key | Default |
|---|---|---|---|
| `RefreshToken` | `string` | `refreshToken` | `""` |
| `AntiCsrfToken` | `string?` | `antiCsrfToken` | `null` |
| `EnableAntiCsrf` | `bool` | `enableAntiCsrf` | `true` |

### `RevokeSessionRequest`

| Property | Type | JSON key | Default |
|---|---|---|---|
| `SessionHandle` | `string` | `sessionHandle` | `""` |

### `SignUpRequest`

Used for both signup and signin.

| Property | Type | JSON key | Default |
|---|---|---|---|
| `Email` | `string` | `email` | `""` |
| `Password` | `string` | `password` | `""` |

### `PasswordResetRequest`

| Property | Type | JSON key | Default |
|---|---|---|---|
| `UserId` | `string` | `userId` | `""` |
| `NewPassword` | `string` | `newPassword` | `""` |

### `UserRolesRequest`

| Property | Type | JSON key | Default |
|---|---|---|---|
| `UserId` | `string` | `userId` | `""` |
| `Roles` | `List<string>` | `roles` | `[]` |

### `UserMetadataUpdateRequest`

| Property | Type | JSON key | Default |
|---|---|---|---|
| `UserId` | `string` | `userId` | `""` |
| `MetadataUpdate` | `Dictionary<string, object>?` | `metadataUpdate` | `null` |

### `SessionStruct`

| Property | Type | JSON key | Default |
|---|---|---|---|
| `Handle` | `string` | `handle` | `""` |
| `UserId` | `string` | `userId` | `""` |
| `UserDataInJWT` | `Dictionary<string, object>` | `userDataInJWT` | `new()` |
| `ExpiryTime` | `long` | `expiryTime` | `0` |
| `TenantId` | `string` | `tenantId` | `"public"` |

### `TokenInfo`

| Property | Type | JSON key |
|---|---|---|
| `Token` | `string` | `token` |
| `Expiry` | `long` | `expiry` |
| `CreatedTime` | `long` | `createdTime` |

### `CreateOrRefreshAPIResponse`

Returned by `CreateSessionAsync` and `RefreshSessionAsync`.

| Property | Type | JSON key |
|---|---|---|
| `Status` | `string` | `status` |
| `Session` | `SessionStruct` | `session` |
| `AccessToken` | `TokenInfo?` | `accessToken` |
| `RefreshToken` | `TokenInfo?` | `refreshToken` |
| `AntiCsrfToken` | `string?` | `antiCsrfToken` |

### `GetSessionResponse`

Returned by `VerifySessionAsync`.

| Property | Type | JSON key |
|---|---|---|
| `Status` | `string` | `status` |
| `Session` | `SessionStruct?` | `session` |
| `AccessToken` | `TokenInfo?` | `accessToken` |

### `SignUpResponse`

Returned by `SignUpAsync` and `SignInAsync`.

| Property | Type | JSON key |
|---|---|---|
| `Status` | `string?` | `status` |
| `User` | `UserResponse?` | `user` |

### `UserResponse`

| Property | Type | JSON key |
|---|---|---|
| `Id` | `string?` | `id` |
| `Email` | `string?` | `email` |
| `TimeJoined` | `long` | `timeJoined` |

### `RevokeSessionResponse`

| Property | Type | JSON key |
|---|---|---|
| `Status` | `string?` | `status` |

### `StatusResponse`

Generic response used by `ResetPasswordAsync`, `AddUserRolesAsync`, `RemoveUserRolesAsync`, `UpdateUserMetadataAsync`.

| Property | Type | JSON key |
|---|---|---|
| `Status` | `string?` | `status` |

### `UserRolesResponse`

| Property | Type | JSON key | Default |
|---|---|---|---|
| `Status` | `string?` | `status` | `null` |
| `Roles` | `List<string>` | `roles` | `[]` |

### `RoleExistsResponse`

| Property | Type | JSON key |
|---|---|---|
| `Status` | `string?` | `status` |
| `DoesRoleExist` | `bool` | `doesRoleExist` |

### `UserMetadataResponse`

| Property | Type | JSON key |
|---|---|---|
| `Status` | `string?` | `status` |
| `Metadata` | `Dictionary<string, object>?` | `metadata` |

## Code examples

### Program.cs setup

```csharp
using SuperTokensSDK.Net.AspNetCore;
using SuperTokensSDK.Net.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSuperTokens(options =>
{
    options.CoreUri = builder.Configuration["SuperTokens:CoreUri"];
    options.ApiKey = builder.Configuration["SuperTokens:ApiKey"];
    options.AppName = builder.Configuration["SuperTokens:AppName"];
    options.ApiDomain = builder.Configuration["SuperTokens:ApiDomain"];
    options.WebsiteDomain = builder.Configuration["SuperTokens:WebsiteDomain"];
});

builder.Services
    .AddAuthentication()
    .AddSuperTokensAuthentication();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
});

var app = builder.Build();

app.UseSuperTokensMiddleware();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

### Signup with role assignment

```csharp
using SuperTokensSDK.Net.Recipes.EmailPassword;
using SuperTokensSDK.Net.Recipes.UserRoles;

app.MapPost("/auth/signup", async (
    EmailPasswordRecipe emailPassword,
    UserRolesRecipe userRoles,
    SignupRequest request) =>
{
    var user = await emailPassword.SignUpAsync(request.Email, request.Password);
    if (user is null)
        return Results.BadRequest("Signup failed.");

    if (!string.IsNullOrEmpty(request.Role))
        await userRoles.AddRoleAsync(user.Id!, request.Role);

    return Results.Ok(new { user.Id, user.Email });
});

public record SignupRequest(string Email, string Password, string? Role);
```

### Signin with session creation

```csharp
using SuperTokensSDK.Net.Recipes.EmailPassword;
using SuperTokensSDK.Net.Recipes.Session;

app.MapPost("/auth/signin", async (
    EmailPasswordRecipe emailPassword,
    SessionRecipe session,
    SigninRequest request) =>
{
    var user = await emailPassword.SignInAsync(request.Email, request.Password);
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
        container.AntiCsrfToken,
        container.SessionHandle,
        container.AccessTokenExpiry,
        container.RefreshTokenExpiry
    });
});

public record SigninRequest(string Email, string Password);
```

### Session verify with claims

```csharp
using SuperTokensSDK.Net.Recipes.Session;
using SuperTokensSDK.Net.Core;
using System.Security.Claims;

app.MapGet("/me", async (SessionRecipe session, HttpRequest request) =>
{
    var token = request.Headers.Authorization.ToString().Replace("Bearer ", "");
    if (string.IsNullOrEmpty(token))
        return Results.Unauthorized();

    try
    {
        var container = await session.VerifySessionAsync(token);
        var principal = container.GetClaimsPrincipal();

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var customClaim = container.GetClaim<string>("department");

        return Results.Ok(new { userId, roles, customClaim });
    }
    catch (UnauthorizedException)
    {
        return Results.Unauthorized();
    }
    catch (TryRefreshTokenException)
    {
        return Results.Problem("Access token expired. Refresh required.", statusCode: 401);
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

app.MapPost("/users/{userId}/roles/{role}", async (UserRolesRecipe userRoles, string userId, string role) =>
{
    await userRoles.AddRoleAsync(userId, role);
    return Results.Ok();
});

app.MapPost("/users/{userId}/roles", async (UserRolesRecipe userRoles, string userId, string[] roles) =>
{
    await userRoles.AddRolesAsync(userId, roles);
    return Results.Ok();
});

app.MapDelete("/users/{userId}/roles/{role}", async (UserRolesRecipe userRoles, string userId, string role) =>
{
    await userRoles.RemoveRoleAsync(userId, role);
    return Results.Ok();
});

app.MapGet("/users/{userId}/roles/{role}/exists", async (UserRolesRecipe userRoles, string userId, string role) =>
{
    var exists = await userRoles.DoesRoleExistAsync(userId, role);
    return Results.Ok(exists);
});
```

### Metadata management

```csharp
using SuperTokensSDK.Net.Recipes.UserMetadata;

app.MapGet("/users/{userId}/metadata", async (UserMetadataRecipe metadata, string userId) =>
{
    var data = await metadata.GetMetadataAsync(userId);
    return Results.Ok(data);
});

app.MapPut("/users/{userId}/metadata", async (
    UserMetadataRecipe metadata,
    string userId,
    Dictionary<string, object> update) =>
{
    await metadata.UpdateMetadataAsync(userId, update);
    return Results.Ok();
});

// Typed metadata retrieval
app.MapGet("/users/{userId}/profile", async (UserMetadataRecipe metadata, string userId) =>
{
    var profile = await metadata.GetMetadataAsAsync<UserProfile>(userId);
    return Results.Ok(profile);
});

public class UserProfile
{
    public string DisplayName { get; set; } = "";
    public string Department { get; set; } = "";
}
```

### Error handling patterns

```csharp
using SuperTokensSDK.Net.Core;

// Pattern 1: Endpoint-level error handling
app.MapPost("/auth/refresh", async (SessionRecipe session, string refreshToken) =>
{
    try
    {
        var container = await session.RefreshSessionAsync(refreshToken);
        return Results.Ok(new { container.AccessToken, container.RefreshToken });
    }
    catch (TokenTheftDetectedException ex)
    {
        // Refresh token reuse. Revoke all sessions for the user.
        // ex.SessionHandle identifies the compromised session.
        // ex.UserId identifies the user whose token was stolen.
        return Results.Problem("Token theft detected.", statusCode: 401);
    }
    catch (UnauthorizedException)
    {
        return Results.Unauthorized();
    }
    catch (TryRefreshTokenException)
    {
        return Results.Problem("Refresh token expired. Re-login required.", statusCode: 401);
    }
    catch (SuperTokensException ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

// Pattern 2: Global exception handler
app.UseExceptionHandler(app =>
{
    app.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        context.Response.ContentType = "application/json";

        switch (exception)
        {
            case UnauthorizedException:
                context.Response.StatusCode = 401;
                break;
            case TryRefreshTokenException:
                context.Response.StatusCode = 401;
                break;
            case TokenTheftDetectedException:
                context.Response.StatusCode = 401;
                break;
            case InvalidClaimException ex:
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(ex.InvalidClaims);
                return;
            case SuperTokensException:
                context.Response.StatusCode = 500;
                break;
        }

        await context.Response.WriteAsync(exception?.Message ?? "An error occurred.");
    });
});
```
