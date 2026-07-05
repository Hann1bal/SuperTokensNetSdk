# Configuration Reference

All configuration lives on the `SuperTokensOptions` class in the `SuperTokensSDK.Net.Configuration` namespace. You set it through the `AddSuperTokens` extension method during DI registration.

## SuperTokensOptions Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `CoreUri` | `string?` | `null` | Base URI of the SuperTokens Core service. Separate multiple hosts with semicolons for round-robin failover. Required. |
| `ApiKey` | `string?` | `null` | API key sent in the `api-key` header to Core. Set this if your Core requires authentication. |
| `AppName` | `string?` | `null` | Application name used by Core. Required. |
| `ApiDomain` | `string?` | `null` | Connection URI for the frontend (API domain). Sent as a query parameter during CDI version negotiation. |
| `WebsiteDomain` | `string?` | `null` | Website domain used for cookie and CSRF handling. Sent as a query parameter during CDI version negotiation. |
| `AccessTokenCookieName` | `string` | `"sAccessToken"` | Name of the access token cookie. |
| `RefreshTokenCookieName` | `string` | `"sRefreshToken"` | Name of the refresh token cookie. |
| `AntiCsrfCookieName` | `string` | `"sAntiCsrf"` | Name of the anti-CSRF token cookie or header. |
| `EnableAntiCsrf` | `bool` | `true` | Turns anti-CSRF protection on for cookie-based sessions. |

## Basic Configuration

The minimum setup requires `CoreUri` and `AppName`. Everything else has sensible defaults.

```csharp
builder.Services.AddSuperTokens(options =>
{
    options.CoreUri = "http://localhost:3567";
    options.AppName = "MyApp";
});
```

## Multi-Host Failover

SuperTokens Core supports horizontal scaling. You can point the SDK at multiple Core instances and it will cycle through them round-robin style. If a host throws an `HttpRequestException` or times out, the client fails over to the next one.

Separate hosts with semicolons:

```csharp
builder.Services.AddSuperTokens(options =>
{
    options.CoreUri = "http://core-1:3567;http://core-2:3567;http://core-3:3567";
    options.AppName = "MyApp";
});
```

### How failover works

The `CoreApiClient` parses `CoreUri` into a list of `Uri` objects at construction time. On every request, it picks the next host using an atomic counter (`Interlocked.Increment` modulo host count). If the HTTP call throws `HttpRequestException` or `TaskCanceledException` (timeout), the client breaks out of the retry loop for that host and tries the next one.

If all hosts fail, the client throws a `SuperTokensException` with the message "All SuperTokens Core hosts failed or the request was repeatedly rate limited."

### Round-robin selection

The host selection is purely round-robin. There is no health checking or latency-based routing. Each request picks the next host in sequence, which distributes load evenly across instances.

```csharp
// The internal counter starts at -1 and increments before use.
// First request:  host index 0
// Second request: host index 1
// Third request:  host index 2
// Fourth request: host index 0 (wraps around)
```

## API Key Authentication

If your Core requires an API key, set `ApiKey`. The client adds it to every request as the `api-key` header during construction.

```csharp
builder.Services.AddSuperTokens(options =>
{
    options.CoreUri = "http://localhost:3567";
    options.ApiKey = "your-secret-api-key";
    options.AppName = "MyApp";
});
```

The key is added once in the `CoreApiClient` constructor:

```csharp
if (!string.IsNullOrWhiteSpace(_options.ApiKey))
{
    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("api-key", _options.ApiKey);
}
```

It applies to all subsequent requests, including CDI version negotiation.

## Cookie Name Customization

You can rename the three cookies the SDK looks for. This is useful if you run multiple SuperTokens-backed apps on the same domain and need to avoid cookie collisions.

```csharp
builder.Services.AddSuperTokens(options =>
{
    options.CoreUri = "http://localhost:3567";
    options.AppName = "MyApp";
    options.AccessTokenCookieName = "myApp_accessToken";
    options.RefreshTokenCookieName = "myApp_refreshToken";
    options.AntiCsrfCookieName = "myApp_antiCsrf";
});
```

The middleware and authentication handler both read from these properties, so renaming is consistent across the entire pipeline.

## Anti-CSRF Configuration

Anti-CSRF protection is on by default. It prevents cross-site request forgery attacks on cookie-based sessions.

When enabled, the middleware checks for an anti-CSRF token in the cookie named `AntiCsrfCookieName` or in the `anti-csrf` header. If a token is present, it gets sent to Core during session verification with `DoAntiCsrfCheck = true`. If no token is present (typical for Bearer token API clients), the middleware skips the anti-CSRF check.

```csharp
builder.Services.AddSuperTokens(options =>
{
    options.CoreUri = "http://localhost:3567";
    options.AppName = "MyApp";
    options.EnableAntiCsrf = true;  // default
});
```

To disable anti-CSRF (not recommended for browser-based apps):

```csharp
options.EnableAntiCsrf = false;
```

## CDI Version Negotiation

The SDK supports CDI version 5.0. On the first request to Core, the client negotiates the CDI version by calling the `/apiversion` endpoint.

### How it works

1. The client sends `GET /apiversion?apiDomain=...&websiteDomain=...` to the first available host.
2. Core responds with a list of supported CDI versions.
3. The client intersects that list with its own supported versions (`["5.0"]`).
4. It picks the highest matching version using semantic versioning (major.minor).
5. The negotiated version is cached in a private field and reused for all subsequent requests.

### Caching and thread safety

Negotiation happens once per `CoreApiClient` instance. A `SemaphoreSlim` guards the negotiation so that concurrent requests do not trigger multiple version checks. After the first successful negotiation, the semaphore is released and all subsequent calls return the cached version immediately.

```csharp
// Pseudocode of the caching logic
if (_cdiVersion is not null)
    return _cdiVersion;

await _versionSemaphore.WaitAsync();
try
{
    if (_cdiVersion is not null)  // double-check after acquiring lock
        return _cdiVersion;

    _cdiVersion = await NegotiateCdiVersionAsync();
    return _cdiVersion;
}
finally
{
    _versionSemaphore.Release();
}
```

### Version header

Every recipe request includes the negotiated version in the `cdi-version` header. Recipe requests also include the `rid` (recipe ID) header, which tells Core which recipe the request targets.

### Negotiation failure

If Core does not support any of the SDK's CDI versions, the client throws:

```
SuperTokens Core does not support any of the SDK CDI versions: 5.0
```

If no host responds, the client throws:

```
Could not negotiate CDI version with any SuperTokens Core host.
```

## Rate Limit Retry

When Core returns HTTP 429 (Too Many Requests), the client retries the request automatically. The retry behavior is controlled by two constants in the `Constants` class.

| Constant | Value | Description |
|---|---|---|
| `RateLimitStatusCode` | `429` | HTTP status code that triggers a retry. |
| `RateLimitRetries` | `5` | Maximum number of retries per host. |

### Backoff strategy

The delay between retries follows a linear backoff formula: `10 + (250 * retry)` milliseconds.

| Retry | Delay |
|---|---|
| 0 (first 429) | 10 ms |
| 1 | 260 ms |
| 2 | 510 ms |
| 3 | 760 ms |
| 4 | 1010 ms |
| 5 (last allowed) | No retry, returns the 429 response |

After exhausting retries on one host, the client fails over to the next host (if multi-host is configured). If all hosts are exhausted, it throws a `SuperTokensException`.

### Example retry trace

```
[Debug] CDI POST /recipe/session
[Debug] CDI request to http://localhost:3567/recipe/session
[Debug] CDI rate limited; retrying in 10ms
[Debug] CDI rate limited; retrying in 260ms
[Debug] CDI rate limited; retrying in 510ms
[Debug] CDI rate limited; retrying in 760ms
[Debug] CDI rate limited; retrying in 1010ms
```

After 5 retries, the response is returned to the caller. The `DeserializeResponseAsync` method then checks the status code and throws an `HttpRequestException` if the response is not successful.

## Full Configuration Example

```csharp
builder.Services.AddSuperTokens(options =>
{
    // Required
    options.CoreUri = "http://core-1:3567;http://core-2:3567";
    options.AppName = "MyApp";

    // Authentication
    options.ApiKey = Environment.GetEnvironmentVariable("SUPERTOKENS_API_KEY");

    // CDI negotiation parameters
    options.ApiDomain = "https://api.example.com";
    options.WebsiteDomain = "https://example.com";

    // Cookie names (custom to avoid collisions)
    options.AccessTokenCookieName = "myApp_accessToken";
    options.RefreshTokenCookieName = "myApp_refreshToken";
    options.AntiCsrfCookieName = "myApp_antiCsrf";

    // Security
    options.EnableAntiCsrf = true;
});
```

## What's Next

- [Getting Started](./getting-started.md): Quick start guide with 5-minute setup
- [Troubleshooting](./troubleshooting.md): Common errors, debug tips, and solutions
- [Auth Integration](./auth-integration.md): How the options flow through the authentication pipeline
- [Examples](./examples.md): Example 6 shows multi-host failover in action
