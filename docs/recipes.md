# Recipe Reference

The SDK implements four SuperTokens recipes. Each recipe is a thin wrapper around `CoreApiClient` that handles request construction and response parsing. All recipes are registered as scoped services by `AddSuperTokens` and can be injected into controllers, minimal APIs, or background services.

| Recipe | Namespace | Purpose |
|---|---|---|
| `EmailPasswordRecipe` | `SuperTokensSDK.Net.Recipes.EmailPassword` | User signup, signin, password reset |
| `SessionRecipe` | `SuperTokensSDK.Net.Recipes.Session` | Session creation, verification, refresh, revocation |
| `UserRolesRecipe` | `SuperTokensSDK.Net.Recipes.UserRoles` | Role assignment, removal, querying |
| `UserMetadataRecipe` | `SuperTokensSDK.Net.Recipes.UserMetadata` | Per-user metadata storage and retrieval |

## EmailPasswordRecipe

Handles email and password based authentication. Maps to the `emailpassword` recipe ID in Core.

### SignUpAsync

Creates a new user with an email and password.

```csharp
public Task<UserResponse?> SignUpAsync(
    string email,
    string password,
    CancellationToken cancellationToken = default)
```

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| `email` | `string` | User email address. |
| `password` | `string` | User password (plaintext, sent over HTTPS to Core). |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

**Returns:** `Task<UserResponse?>` where `UserResponse` contains `Id`, `Email`, and `TimeJoined`. Returns `null` if Core does not return a user object.

**Example:**

```csharp
using SuperTokensSDK.Net.Recipes.EmailPassword;

app.MapPost("/auth/signup", async (
    EmailPasswordRecipe emailPassword,
    string email, string password) =>
{
    var user = await emailPassword.SignUpAsync(email, password);
    if (user is null)
        return Results.BadRequest("Signup failed.");

    return Results.Ok(new { user.Id, user.Email });
});
```

### SignInAsync

Authenticates a user by email and password.

```csharp
public Task<UserResponse?> SignInAsync(
    string email,
    string password,
    CancellationToken cancellationToken = default)
```

**Parameters:** Same as `SignUpAsync`.

**Returns:** `Task<UserResponse?>` with the user object on success, `null` if Core returns no user.

**Example:**

```csharp
var user = await emailPassword.SignInAsync(email, password);
if (user is null)
    return Results.Unauthorized();

// Proceed to create a session
var container = await session.CreateSessionAsync(user.Id!);
```

### ResetPasswordAsync

Resets the password for a given user ID. This is an admin operation that bypasses the normal email-based reset flow.

```csharp
public Task ResetPasswordAsync(
    string userId,
    string newPassword,
    CancellationToken cancellationToken = default)
```

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| `userId` | `string` | The SuperTokens user ID. |
| `newPassword` | `string` | The new password to set. |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

**Returns:** `Task` (no return value). Throws `SuperTokensException` on failure.

**Example:**

```csharp
app.MapPost("/admin/users/{userId}/reset-password", async (
    EmailPasswordRecipe emailPassword,
    string userId,
    string newPassword) =>
{
    await emailPassword.ResetPasswordAsync(userId, newPassword);
    return Results.Ok();
}).RequireAuthorization("admin");
```

## SessionRecipe

Manages session lifecycle. Maps to the `session` recipe ID in Core.

### CreateSessionAsync

Creates a new session for a user.

```csharp
public Task<SessionContainer> CreateSessionAsync(
    string userId,
    Dictionary<string, object>? accessTokenPayload = null,
    Dictionary<string, object>? sessionData = null,
    CancellationToken cancellationToken = default)
```

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| `userId` | `string` | The SuperTokens user ID. |
| `accessTokenPayload` | `Dictionary<string, object>?` | Custom claims embedded in the JWT. Defaults to empty. |
| `sessionData` | `Dictionary<string, object>?` | Data stored in the Core database (not in the JWT). Defaults to empty. |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

**Returns:** `Task<SessionContainer>` with access token, refresh token, anti-CSRF token, and expiry times.

**Example:**

```csharp
var container = await session.CreateSessionAsync(
    user.Id!,
    accessTokenPayload: new Dictionary<string, object>
    {
        ["roles"] = new[] { "admin", "staff" },
        ["department"] = "engineering"
    });

// Set cookies in the HTTP response
Response.Cookies.Append("sAccessToken", container.AccessToken!);
Response.Cookies.Append("sRefreshToken", container.RefreshToken!);
```

### VerifySessionAsync

Verifies an access token and returns the session data.

```csharp
public Task<SessionContainer> VerifySessionAsync(
    string accessToken,
    string? antiCsrfToken = null,
    CancellationToken cancellationToken = default)
```

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| `accessToken` | `string` | The JWT access token to verify. |
| `antiCsrfToken` | `string?` | Optional anti-CSRF token for cookie-based sessions. |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

**Returns:** `Task<SessionContainer>` with the user ID, session handle, and custom claims from the JWT payload.

**Important:** This method decodes the JWT locally instead of calling the Core verify endpoint. Core 11.x has a bug in `/recipe/session/verify` that rejects `doAntiCsrfCheck` even when it is not sent. The workaround decodes the RS256 JWT payload, checks the `exp` claim for expiry, and extracts the `sub` (userId), `sessionHandle`, and custom claims.

**Exceptions thrown:**

| Exception | When |
|---|---|
| `UnauthorizedException` | Token is malformed, expired, or missing a userId. |

**Example:**

```csharp
try
{
    var container = await session.VerifySessionAsync(accessToken);
    Console.WriteLine($"User: {container.UserId}");
    Console.WriteLine($"Session: {container.SessionHandle}");
}
catch (UnauthorizedException)
{
    return Results.Unauthorized();
}
```

### RefreshSessionAsync

Exchanges a refresh token for a new access token and refresh token.

```csharp
public Task<SessionContainer> RefreshSessionAsync(
    string refreshToken,
    string? antiCsrfToken = null,
    CancellationToken cancellationToken = default)
```

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| `refreshToken` | `string` | The refresh token from a previous session creation or refresh. |
| `antiCsrfToken` | `string?` | Optional anti-CSRF token. If provided, anti-CSRF checking is enabled. |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

**Returns:** `Task<SessionContainer>` with new access and refresh tokens.

**Exceptions thrown:**

| Exception | When |
|---|---|
| `UnauthorizedException` | Refresh token is invalid or expired. |
| `TokenTheftDetectedException` | Refresh token reuse detected. Contains `SessionHandle` and `UserId`. |

**Example:**

```csharp
app.MapPost("/auth/refresh", async (
    SessionRecipe session,
    HttpRequest request) =>
{
    var refreshToken = request.Cookies["sRefreshToken"];
    var antiCsrf = request.Cookies["sAntiCsrf"];

    try
    {
        var container = await session.RefreshSessionAsync(refreshToken!, antiCsrf);
        Response.Cookies.Append("sAccessToken", container.AccessToken!);
        Response.Cookies.Append("sRefreshToken", container.RefreshToken!);
        return Results.Ok(new { container.AccessToken });
    }
    catch (TokenTheftDetectedException ex)
    {
        // Revoke all sessions for this user
        await session.RevokeSessionAsync(ex.SessionHandle);
        return Results.Json(new { error = "Token theft detected" }, statusCode: 401);
    }
});
```

### RevokeSessionAsync

Revokes a session by its handle.

```csharp
public Task RevokeSessionAsync(
    string sessionHandle,
    CancellationToken cancellationToken = default)
```

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| `sessionHandle` | `string` | The session handle from `SessionContainer.SessionHandle`. |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

**Returns:** `Task` (no return value).

**Example:**

```csharp
app.MapPost("/auth/logout", async (
    SessionRecipe session,
    string sessionHandle) =>
{
    await session.RevokeSessionAsync(sessionHandle);
    Response.Cookies.Delete("sAccessToken");
    Response.Cookies.Delete("sRefreshToken");
    Response.Cookies.Delete("sAntiCsrf");
    return Results.Ok();
});
```

## SessionContainer

Returned by `CreateSessionAsync`, `VerifySessionAsync`, and `RefreshSessionAsync`. Wraps all session data and provides helpers for ASP.NET Core integration.

### Properties

| Property | Type | Description |
|---|---|---|
| `SessionHandle` | `string` | Unique session identifier used for revocation. |
| `UserId` | `string` | The SuperTokens user ID. |
| `AccessToken` | `string?` | The JWT access token (null after `VerifySessionAsync` returns the original token). |
| `RefreshToken` | `string?` | The refresh token (null after verify). |
| `AntiCsrfToken` | `string?` | Anti-CSRF token for cookie-based sessions. |
| `AccessTokenExpiry` | `DateTime` | UTC expiry time of the access token. `DateTime.MinValue` if not set. |
| `RefreshTokenExpiry` | `DateTime` | UTC expiry time of the refresh token. `DateTime.MinValue` if not set. |
| `UserDataInJwt` | `Dictionary<string, object>` | Custom claims from the JWT payload. |

### GetClaimsPrincipal()

Builds a `ClaimsPrincipal` from the session data. Useful for setting `HttpContext.User` or for manual authorization checks.

```csharp
public ClaimsPrincipal GetClaimsPrincipal()
```

The method creates a `ClaimsIdentity` with authentication type `"SuperTokens"`. It adds:
- `ClaimTypes.NameIdentifier` claim with the user ID
- `sub` claim with the user ID
- All custom claims from `UserDataInJwt`

If `UserDataInJwt` contains a `roles` key with a JSON array, each role is added as a separate `ClaimTypes.Role` claim. This makes `[Authorize(Roles = "admin")]` work out of the box.

**Example:**

```csharp
var container = await session.VerifySessionAsync(token);
var principal = container.GetClaimsPrincipal();

var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
var is_admin = principal.IsInRole("admin");
```

### GetClaim\<T\>()

Returns a typed value from the JWT payload.

```csharp
public T? GetClaim<T>(string key, T? defaultValue = default)
```

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| `key` | `string` | The claim key in `UserDataInJwt`. |
| `defaultValue` | `T?` | Value to return if the key is missing or cannot be converted. |

**Returns:** `T?` with the typed claim value, or `defaultValue`.

The method handles `JsonElement` values by deserializing them to the target type. This is necessary because `System.Text.Json` stores dictionary values as `JsonElement` when deserializing from JSON.

**Example:**

```csharp
var container = await session.VerifySessionAsync(token);

// Get a string claim
var department = container.GetClaim<string>("department");

// Get an integer claim
var level = container.GetClaim<int>("accessLevel");

// Get a custom type
var profile = container.GetClaim<UserProfile>("profile");
```

## UserRolesRecipe

Manages role assignments for users. Maps to the `userroles` recipe ID in Core.

### AddRoleAsync

Assigns a single role to a user.

```csharp
public Task AddRoleAsync(
    string userId,
    string role,
    CancellationToken cancellationToken = default)
```

**Example:**

```csharp
await userRoles.AddRoleAsync(userId, "admin");
```

### AddRolesAsync

Assigns multiple roles to a user in one call.

```csharp
public Task AddRolesAsync(
    string userId,
    IEnumerable<string> roles,
    CancellationToken cancellationToken = default)
```

**Example:**

```csharp
await userRoles.AddRolesAsync(userId, new[] { "admin", "staff", "editor" });
```

### GetRolesAsync

Returns all roles assigned to a user.

```csharp
public Task<IReadOnlyList<string>> GetRolesAsync(
    string userId,
    CancellationToken cancellationToken = default)
```

**Returns:** `Task<IReadOnlyList<string>>` with the role names.

**Example:**

```csharp
var roles = await userRoles.GetRolesAsync(userId);
foreach (var role in roles)
    Console.WriteLine(role);
```

### RemoveRoleAsync

Removes a single role from a user.

```csharp
public Task RemoveRoleAsync(
    string userId,
    string role,
    CancellationToken cancellationToken = default)
```

**Example:**

```csharp
await userRoles.RemoveRoleAsync(userId, "admin");
```

### RemoveRolesAsync

Removes multiple roles from a user.

```csharp
public Task RemoveRolesAsync(
    string userId,
    IEnumerable<string> roles,
    CancellationToken cancellationToken = default)
```

**Example:**

```csharp
await userRoles.RemoveRolesAsync(userId, new[] { "admin", "editor" });
```

### DoesRoleExistAsync

Checks whether a user has a specific role.

```csharp
public Task<bool> DoesRoleExistAsync(
    string userId,
    string role,
    CancellationToken cancellationToken = default)
```

**Returns:** `Task<bool>`, `true` if the user has the role, `false` otherwise.

**Example:**

```csharp
var isAdmin = await userRoles.DoesRoleExistAsync(userId, "admin");
if (!isAdmin)
    return Results.Forbid();
```

## UserMetadataRecipe

Stores and retrieves per-user metadata. Maps to the `usermetadata` recipe ID in Core. Metadata is stored in the Core database, not in the JWT.

### GetMetadataAsync

Returns the metadata dictionary for a user.

```csharp
public Task<Dictionary<string, object>?> GetMetadataAsync(
    string userId,
    CancellationToken cancellationToken = default)
```

**Returns:** `Task<Dictionary<string, object>?>` with the metadata, or `null` if no metadata exists.

**Example:**

```csharp
var metadata = await userMetadata.GetMetadataAsync(userId);
if (metadata != null)
{
    var name = metadata.GetValueOrDefault("fullName")?.ToString();
    var age = metadata.GetValueOrDefault("age")?.ToString();
}
```

### UpdateMetadataAsync

Updates metadata for a user. Existing keys are merged, new keys are added.

```csharp
public Task UpdateMetadataAsync(
    string userId,
    Dictionary<string, object> update,
    CancellationToken cancellationToken = default)
```

**Example:**

```csharp
await userMetadata.UpdateMetadataAsync(userId, new Dictionary<string, object>
{
    ["fullName"] = "Jane Doe",
    ["age"] = 32,
    ["preferences"] = new { theme = "dark", notifications = true }
});
```

### GetMetadataAsAsync\<T\>

Retrieves metadata and deserializes it into a typed object.

```csharp
public Task<T?> GetMetadataAsAsync<T>(
    string userId,
    CancellationToken cancellationToken = default) where T : class, new()
```

**Type parameter:** `T` must be a reference type with a parameterless constructor.

**Returns:** `Task<T?>` with the deserialized metadata, or `null` if no metadata exists.

**Example:**

```csharp
public class UserProfile
{
    public string FullName { get; set; } = "";
    public int Age { get; set; }
    public string? Department { get; set; }
}

var profile = await userMetadata.GetMetadataAsAsync<UserProfile>(userId);
if (profile != null)
{
    Console.WriteLine($"{profile.FullName}, {profile.Age}, {profile.Department}");
}
```

The method serializes the metadata dictionary to JSON, then deserializes it to `T`. This means property names must match the metadata keys (case-insensitive due to `JsonSerializerOptions`).

## Missing Recipes

The official SuperTokens SDK supports additional recipes that this SDK does not implement yet:

- **ThirdPartyEmailPassword**: OAuth and social login (Google, GitHub, Apple, etc.)
- **Passwordless**: Email and phone based magic link or OTP login
- **MultiTenancy**: Multi-tenant configuration and login methods
- **PhoneNumber**: Phone number based authentication
- **JWT**: JWT creation and verification outside of sessions

If you need any of these recipes, you can call the Core API directly through `ICoreApiClient` or extend the SDK by adding new recipe classes.

## What's Next

- [Auth Integration](./auth-integration.md): How to use recipes in ASP.NET Core controllers and middleware
- [Configuration](./configuration.md): All options for Core connection, cookies, and anti-CSRF
- [Examples](./examples.md): Full worked examples for each recipe
- [MCP Gateway](./mcp-gateway.md): How recipes power the MCP tool gateway
- [Troubleshooting](./troubleshooting.md): Common recipe errors and solutions
