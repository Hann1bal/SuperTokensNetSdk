# Troubleshooting

This guide covers common errors, their causes, and how to fix them. Each section explains what the error means, why it happens, and what you can do about it.

## Common Errors

### 401 Unauthorized

**What it means:** The session is invalid, expired, or the access token is missing.

**Possible causes:**

1. The access token has expired. Access tokens are short-lived (typically 1 hour). The frontend needs to refresh the session.
2. The access token is malformed or was tampered with.
3. The cookie was not sent with the request. Check CORS and `withCredentials` settings.
4. The Bearer header is missing or has the wrong format.

**How to fix:**

Check whether the token is present in the request:

```csharp
// In your controller or middleware
var token = Request.Headers.Authorization.FirstOrDefault()
    ?? Request.Cookies["sAccessToken"];

if (string.IsNullOrEmpty(token))
{
    // Token is missing. Check frontend configuration.
    return Unauthorized("No access token provided.");
}
```

If the token is present but expired, the SDK throws `UnauthorizedException` with the message "Access token has expired." The frontend should call the refresh endpoint:

```typescript
// Frontend axios interceptor
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      // Try refreshing the session
      const refresh = await axios.post("/auth/refresh", {}, { withCredentials: true });
      if (refresh.status === 200) {
        return api.request(error.config);  // Retry original request
      }
    }
    return Promise.reject(error);
  }
);
```

See [Migration Guide](./migration.md) for the full frontend setup.

### TRY_REFRESH_TOKEN

**What it means:** The access token has expired and the frontend needs to call the refresh endpoint to get a new one.

**When it happens:** The SDK throws `TryRefreshTokenException` when Core returns the status `TRY_REFRESH_TOKEN` or `NEEDS_REFRESH`. This typically means the access token's `exp` claim is in the past, but the refresh token is still valid.

**How to fix:**

The frontend should have a response interceptor that catches 401 responses with `TRY_REFRESH_TOKEN` status and calls `/auth/refresh`:

```csharp
// Backend refresh endpoint
app.MapPost("/auth/refresh", async (
    SessionRecipe session,
    HttpResponse response) =>
{
    var refreshToken = request.Cookies["sRefreshToken"];
    var antiCsrf = request.Cookies["sAntiCsrf"];

    try
    {
        var container = await session.RefreshSessionAsync(refreshToken!, antiCsrf);
        response.Cookies.Append("sAccessToken", container.AccessToken!);
        response.Cookies.Append("sRefreshToken", container.RefreshToken!);
        return Results.Ok();
    }
    catch (UnauthorizedException)
    {
        return Results.Unauthorized("Refresh token expired. Please sign in again.");
    }
});
```

### TOKEN_THEFT_DETECTED

**What it means:** The refresh token was used more than once. This indicates that an attacker has stolen the refresh token and is trying to use it.

**When it happens:** The SDK throws `TokenTheftDetectedException` when Core returns the status `TOKEN_THEFT_DETECTED`. The exception contains `SessionHandle` and `UserId` properties identifying the compromised session.

**How to fix:**

Revoke the compromised session immediately and force the user to sign in again:

```csharp
catch (TokenTheftDetectedException ex)
{
    // Revoke the compromised session
    await session.RevokeSessionAsync(ex.SessionHandle);

    // Log the incident for security review
    logger.LogWarning("Token theft detected for user {UserId}, session {SessionHandle}",
        ex.UserId, ex.SessionHandle);

    // Clear cookies and return 401
    Response.Cookies.Delete("sAccessToken");
    Response.Cookies.Delete("sRefreshToken");
    Response.Cookies.Delete("sAntiCsrf");

    return Unauthorized("Session compromised. Please sign in again.");
}
```

You should also consider revoking all sessions for the affected user, not just the one identified by the exception.

### INVALID_CLAIMS

**What it means:** Claim validation failed during session verification. The JWT payload contains claims that did not pass validation rules.

**When it happens:** The SDK throws `InvalidClaimException` when Core returns the status `INVALID_CLAIMS`. The exception contains an `InvalidClaims` list with `Id` and `Reason` for each failed claim.

**How to fix:**

Inspect the invalid claims to understand what went wrong:

```csharp
catch (InvalidClaimException ex)
{
    foreach (var claim in ex.InvalidClaims)
    {
        Console.WriteLine($"Claim {claim.Id} failed: {claim.Reason}");
    }
    return Results.Json(new { error = "Invalid claims", ex.InvalidClaims },
        statusCode: 403);
}
```

Common reasons include role requirements not met, email verification required, or custom claim validators failing.

### CDI Version Negotiation Failure

**What it means:** The SDK could not agree on a CDI version with Core.

**Possible causes:**

1. Core is not running or is unreachable.
2. Core is running an incompatible version (the SDK supports CDI 5.0 only).
3. The `CoreUri` is wrong or points to the wrong port.

**Error messages:**

```
SuperTokens Core does not support any of the SDK CDI versions: 5.0
```

This means Core responded to `/apiversion` but does not list 5.0 in its supported versions. Check that you are running Core 11.x.

```
Could not negotiate CDI version with any SuperTokens Core host.
```

This means no host responded. Check that Core is running and the `CoreUri` is correct.

**How to fix:**

Verify Core is running:

```bash
curl http://localhost:3567/hello
# Expected response: Hello
```

Check the CDI version:

```bash
curl http://localhost:3567/apiversion
# Expected response: {"versions":["5.0"]}
```

If the versions list does not include 5.0, upgrade Core to version 11.x.

See [Configuration](./configuration.md) for details on how CDI negotiation works.

### Rate Limit (429)

**What it means:** Core is rate limiting requests. The SDK retries automatically up to 5 times with linear backoff.

**When it happens:** Core returns HTTP 429 when the request rate exceeds the configured limit. The SDK retries with delays of 10ms, 260ms, 510ms, 760ms, and 1010ms.

**How to fix:**

If you see 429 errors in logs, your application is sending too many requests to Core. Options:

1. Reduce the frequency of session verification calls. The middleware verifies on every request by default. Consider caching the result for short periods.
2. Scale up your Core instances. See [Configuration](./configuration.md) for multi-host setup.
3. Check Core's rate limit configuration and increase the limit if appropriate.

The retry behavior is automatic and transparent. You do not need to handle 429 responses in your application code. If all 5 retries are exhausted, the response is returned to the caller and `DeserializeResponseAsync` throws an `HttpRequestException`.

### Multi-Host Failover Not Working

**What it means:** The SDK is not failing over to the next host when one host fails.

**Possible causes:**

1. The `CoreUri` is not formatted correctly. Hosts must be separated by semicolons with no spaces.
2. All hosts are failing for the same reason (network partition, Core down).
3. The failure is not an `HttpRequestException` or `TaskCanceledException`. Other exceptions are not retried.

**How to fix:**

Check the `CoreUri` format:

```csharp
// Correct: semicolons, no spaces
options.CoreUri = "http://core-1:3567;http://core-2:3567;http://core-3:3567";

// Wrong: spaces after semicolons (will be trimmed, but check anyway)
options.CoreUri = "http://core-1:3567; http://core-2:3567";

// Wrong: commas instead of semicolons
options.CoreUri = "http://core-1:3567,http://core-2:3567";
```

Verify that each host is reachable:

```bash
curl http://core-1:3567/hello
curl http://core-2:3567/hello
curl http://core-3:3567/hello
```

Check the logs for failover messages. The SDK logs at Warning level when a host fails:

```
[Warning] CDI request failed for host http://core-1:3567; failing over
```

### JWT Decode Fails

**What it means:** The SDK could not decode the access token JWT payload.

**Possible causes:**

1. The token is not a valid JWT (does not have 3 parts separated by dots).
2. The payload is not valid base64url.
3. The token is expired (the `exp` claim is in the past).

**Error messages:**

```
Failed to decode access token: <inner exception message>
```

**How to fix:**

Inspect the token manually. A JWT has three parts: `header.payload.signature`. Each part is base64url encoded.

```bash
# Decode the payload (second part)
echo "eyJzdWIiOiJ1c2VyLTEyMyIsImV4cCI6MTcwMDAwMDAwMH0" | base64 -d 2>/dev/null
# Output: {"sub":"user-123","exp":1700000000}
```

Check the expiry timestamp:

```bash
# Convert Unix timestamp to human-readable date
date -d @1700000000
```

If the token is expired, the frontend needs to refresh the session. See [TRY_REFRESH_TOKEN](#try_refresh_token) above.

### CORS Errors

**What it means:** The browser is blocking cross-origin requests because CORS is not configured correctly.

**Common error messages in the browser console:**

```
Access to XMLHttpRequest at 'https://api.example.com/auth/signin' from origin 'https://example.com'
has been blocked by CORS policy: The value of the 'Access-Control-Allow-Credentials' header in the
response is '' which must be 'true' when the request's credentials mode is 'include'.
```

**How to fix:**

Configure CORS on the backend to allow credentials:

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://example.com")  // No wildcard
              .AllowCredentials()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();
app.UseCors();
```

Set `withCredentials` on the frontend:

```typescript
axios.defaults.withCredentials = true;
```

You cannot use `AllowAnyOrigin()` with `AllowCredentials()`. The browser rejects this combination. You must specify exact origins.

See [Migration Guide](./migration.md) for the full CORS setup.

### Cookie Not Set

**What it means:** The browser is not storing the cookies that the backend sets in the response.

**Possible causes:**

1. The `Secure` flag is set but the connection is HTTP (not HTTPS).
2. The `SameSite` attribute is too restrictive for the deployment topology.
3. The cookie domain does not match the request origin.
4. The `HttpOnly` flag prevents JavaScript from reading the cookie (this is expected and correct).

**How to fix:**

Check the cookie attributes in the browser DevTools (Application tab > Cookies):

| Attribute | Expected Value | Problem |
|---|---|---|
| `Secure` | `true` in production | If `false` over HTTPS, check your code. If `true` over HTTP, cookies will not be set. |
| `SameSite` | `Lax` or `None` | `Strict` prevents cookies from being sent on cross-site requests. `None` requires `Secure`. |
| `Domain` | `.example.com` or empty | If set to the wrong domain, the browser rejects the cookie. |
| `Path` | `/` | If set to a specific path, cookies are only sent to that path. |

For local development with HTTP:

```csharp
var isDev = builder.Environment.IsDevelopment();
var options = new CookieOptions
{
    HttpOnly = true,
    Secure = !isDev,  // false in dev, true in production
    SameSite = SameSiteMode.Lax,
    Path = "/"
};
```

## Debug Tips

### Inspect a JWT

To decode a JWT without verification (for debugging only):

```csharp
using System.Text;
using System.Text.Json;

public static void InspectJwt(string token)
{
    var parts = token.Split('.');
    if (parts.Length < 2)
    {
        Console.WriteLine("Invalid JWT: not enough parts.");
        return;
    }

    var payload = parts[1]
        .Replace('-', '+')
        .Replace('_', '/');

    switch (payload.Length % 4)
    {
        case 2: payload += "=="; break;
        case 3: payload += "="; break;
    }

    var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
    var doc = JsonDocument.Parse(json);

    Console.WriteLine("JWT Payload:");
    Console.WriteLine(JsonSerializer.Serialize(doc, new JsonSerializerOptions
    {
        WriteIndented = true
    }));

    if (doc.RootElement.TryGetProperty("exp", out var exp))
    {
        var expiry = DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64());
        var isExpired = DateTimeOffset.UtcNow > expiry;
        Console.WriteLine($"Expiry: {expiry} ({(isExpired ? "EXPIRED" : "valid")})");
    }
}
```

### Check Core Health

SuperTokens Core exposes a health endpoint:

```bash
# Basic health check
curl http://localhost:3567/hello
# Expected: Hello

# API version (CDI version)
curl http://localhost:3567/apiversion
# Expected: {"versions":["5.0"]}

# Session count (if Core is running)
curl http://localhost:3567/recipe/session/count -H "rid: session"
```

### Verify CDI Version

The SDK logs the negotiated CDI version at Information level on startup:

```
[Information] Negotiated CDI version 5.0 with http://localhost:3567
```

If you do not see this log, the negotiation failed. Check the Core health and version.

You can also call the endpoint manually:

```bash
curl "http://localhost:3567/apiversion?apiDomain=https://api.example.com&websiteDomain=https://example.com"
```

### Enable Debug Logging

The SDK logs at Debug level for most operations. To see these logs, set the logging level to Debug for the `SuperTokensSDK.Net` namespace:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SuperTokensSDK.Net": "Debug"
    }
  }
}
```

Or in code:

```csharp
builder.Logging.AddFilter("SuperTokensSDK.Net", LogLevel.Debug);
```

Debug logs include:
- CDI request URLs and methods
- Rate limit retry attempts with delays
- Host failover events
- Session verification failures (with exception type)
- Token theft detection (with user ID)

### Inspect Cookies in the Browser

Open DevTools (F12) > Application > Cookies. Check that:
- `sAccessToken` exists and has a value
- `sRefreshToken` exists and has a value
- `sAntiCsrf` exists if anti-CSRF is enabled
- The `HttpOnly` column shows a checkmark (meaning JavaScript cannot read the cookie, which is correct)
- The `Secure` column shows a checkmark in production
- The `SameSite` column shows `Lax` or `None`

## What's Next

- [Configuration](./configuration.md): All options that affect behavior
- [Auth Integration](./auth-integration.md): How the middleware and handler process tokens
- [Migration Guide](./migration.md): Common pitfalls during JWT to cookie migration
- [Getting Started](./getting-started.md): If you are just starting out
