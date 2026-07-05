# Migration Guide: JWT Tokens to SuperTokens Cookies

This guide walks through migrating a frontend and backend from JWT bearer token authentication to SuperTokens cookie-based sessions. The SDK was designed to make this transition straightforward.

## Before and After

### Before: JWT in localStorage

```
Frontend                          Backend
  |                                 |
  |  POST /auth/login               |
  |  { email, password }            |
  | ------------------------------->|
  |                                 |  Verify password
  |                                 |  Generate JWT
  |  { token: "eyJhbG..." }        |
  |<-------------------------------|
  |                                 |
  |  Store token in localStorage    |
  |                                 |
  |  GET /api/data                  |
  |  Authorization: Bearer eyJhbG.. |
  | ------------------------------->|
  |                                 |  Verify JWT signature
  |  { data }                       |
  |<-------------------------------|
```

### After: SuperTokens cookies

```
Frontend                          Backend
  |                                 |
  |  POST /auth/signin              |
  |  { email, password }            |
  |  withCredentials: true          |
  | ------------------------------->|
  |                                 |  Verify password via Core
  |                                 |  Create session via Core
  |                                 |  Set cookies: sAccessToken,
  |                                 |    sRefreshToken, sAntiCsrf
  |  200 OK (Set-Cookie)            |
  |<-------------------------------|
  |                                 |
  |  Cookies stored by browser      |
  |                                 |
  |  GET /api/data                  |
  |  withCredentials: true          |
  |  (cookies sent automatically)   |
  | ------------------------------->|
  |                                 |  Middleware verifies session
  |                                 |  Sets HttpContext.User
  |  { data }                       |
  |<-------------------------------|
```

## Frontend Changes

### AuthContext: getToken()

The `getToken()` function changes from returning a JWT to returning a placeholder. The actual token is now in an HTTP-only cookie that JavaScript cannot read.

**Before:**

```typescript
function getToken(): string | null {
  return localStorage.getItem("auth_token");
}

function staffHeaders(): Record<string, string> {
  const token = getToken();
  if (!token) return {};
  return { Authorization: `Bearer ${token}` };
}
```

**After:**

```typescript
function getToken(): string | null {
  // The token is in an HTTP-only cookie. We return a placeholder
  // so that code checking for a token still works.
  const hasSession = document.cookie.includes("sAccessToken");
  return hasSession ? "cookie" : null;
}

function staffHeaders(): Record<string, string> {
  // Cookies are sent automatically by the browser.
  // No Authorization header needed.
  return {};
}
```

The `getToken()` guard pattern is important. Many codebases check `if (getToken())` before rendering authenticated UI. Returning `"cookie"` instead of `null` keeps these checks working without modification.

### Axios: withCredentials

Every axios instance that talks to the backend needs `withCredentials: true`. This tells the browser to send cookies with cross-origin requests.

**Before:**

```typescript
const api = axios.create({
  baseURL: "https://api.example.com",
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem("auth_token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});
```

**After:**

```typescript
const api = axios.create({
  baseURL: "https://api.example.com",
  withCredentials: true,  // Send cookies with every request
});

// No need to set Authorization header.
// The browser sends sAccessToken cookie automatically.

// Add a response interceptor to handle token refresh
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      const status = error.response.data?.status;
      if (status === "TRY_REFRESH_TOKEN") {
        // Refresh the session
        const refreshResponse = await axios.post(
          "https://api.example.com/auth/refresh",
          {},
          { withCredentials: true }
        );
        if (refreshResponse.status === 200) {
          // Retry the original request
          return api.request(error.config);
        }
      }
    }
    return Promise.reject(error);
  }
);
```

### AuthContext: Full Example

```typescript
import { createContext, useContext, useState, useEffect, ReactNode } from "react";
import axios from "axios";

interface AuthContextType {
  isAuthenticated: boolean;
  user: { id: string; email: string } | null;
  signin: (email: string, password: string) => Promise<void>;
  signout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType>(null!);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [user, setUser] = useState<{ id: string; email: string } | null>(null);

  useEffect(() => {
    // Check if the user has a session cookie
    if (getToken()) {
      // Validate the session with the backend
      api.get("/me").then((response) => {
        setUser(response.data);
        setIsAuthenticated(true);
      }).catch(() => {
        setIsAuthenticated(false);
        setUser(null);
      });
    }
  }, []);

  const signin = async (email: string, password: string) => {
    const response = await axios.post(
      "/auth/signin",
      { email, password },
      { withCredentials: true }
    );
    setUser(response.data);
    setIsAuthenticated(true);
  };

  const signout = async () => {
    await axios.post("/auth/logout", {}, { withCredentials: true });
    setIsAuthenticated(false);
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ isAuthenticated, user, signin, signout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  return useContext(AuthContext);
}
```

## Backend Changes

### SuperTokensAuthController

Create a controller that handles signin, signout, and session refresh.

```csharp
using Microsoft.AspNetCore.Mvc;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Recipes.EmailPassword;
using SuperTokensSDK.Net.Recipes.Session;
using SuperTokensSDK.Net.Recipes.UserRoles;

[ApiController]
[Route("auth")]
public class SuperTokensAuthController : ControllerBase
{
    private readonly EmailPasswordRecipe _emailPassword;
    private readonly SessionRecipe _session;
    private readonly UserRolesRecipe _userRoles;

    public SuperTokensAuthController(
        EmailPasswordRecipe emailPassword,
        SessionRecipe session,
        UserRolesRecipe userRoles)
    {
        _emailPassword = emailPassword;
        _session = session;
        _userRoles = userRoles;
    }

    [HttpPost("signin")]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request)
    {
        var user = await _emailPassword.SignInAsync(request.Email, request.Password);
        if (user is null)
            return Unauthorized();

        var roles = await _userRoles.GetRolesAsync(user.Id!);

        var container = await _session.CreateSessionAsync(
            user.Id!,
            accessTokenPayload: new Dictionary<string, object>
            {
                ["roles"] = roles.ToArray()
            });

        SetSessionCookies(container.AccessToken!, container.RefreshToken!, container.AntiCsrfToken);
        return Ok(new { user.Id, user.Email, roles });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["sRefreshToken"];
        var antiCsrf = Request.Cookies["sAntiCsrf"];

        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized();

        try
        {
            var container = await _session.RefreshSessionAsync(refreshToken, antiCsrf);
            SetSessionCookies(container.AccessToken!, container.RefreshToken!, container.AntiCsrfToken);
            return Ok();
        }
        catch (TokenTheftDetectedException ex)
        {
            await _session.RevokeSessionAsync(ex.SessionHandle);
            ClearSessionCookies();
            return Unauthorized("Token theft detected.");
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var sessionHandle = Request.Headers["x-session-handle"].FirstOrDefault();
        if (!string.IsNullOrEmpty(sessionHandle))
        {
            await _session.RevokeSessionAsync(sessionHandle);
        }
        ClearSessionCookies();
        return Ok();
    }

    private void SetSessionCookies(string accessToken, string refreshToken, string? antiCsrf)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        };
        Response.Cookies.Append("sAccessToken", accessToken, options);
        Response.Cookies.Append("sRefreshToken", refreshToken, options);
        if (antiCsrf is not null)
            Response.Cookies.Append("sAntiCsrf", antiCsrf, options);
    }

    private void ClearSessionCookies()
    {
        Response.Cookies.Delete("sAccessToken");
        Response.Cookies.Delete("sRefreshToken");
        Response.Cookies.Delete("sAntiCsrf");
    }
}

public record SignInRequest(string Email, string Password);
```

### Program.cs: Dual Auth

During migration, you may need to support both JWT and SuperTokens simultaneously. The dual auth setup accepts either scheme.

```csharp
using SuperTokensSDK.Net.AspNetCore;
using SuperTokensSDK.Net.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSuperTokens(options =>
{
    options.CoreUri = "http://localhost:3567";
    options.ApiKey = builder.Configuration["SuperTokens:ApiKey"];
    options.AppName = "MyApp";
    options.ApiDomain = "https://api.example.com";
    options.WebsiteDomain = "https://example.com";
});

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
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes("Bearer", "SuperTokens")
        .Build();
});

var app = builder.Build();

app.UseSuperTokensMiddleware();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

### RoleClaimsTransformation

If you were using a custom `IClaimsTransformation` to add role claims from a database, you can replace it with `SuperTokensClaimsTransformation`. The SDK version reads roles from the JWT payload, which means roles are set during session creation and do not require a database lookup on every request.

**Before (custom transformation):**

```csharp
public class DbRoleClaimsTransformation : IClaimsTransformation
{
    private readonly AppDbContext _db;

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return principal;

        var roles = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

        var identity = (ClaimsIdentity)principal.Identity!;
        foreach (var role in roles)
            identity.AddClaim(new Claim(ClaimTypes.Role, role));

        return principal;
    }
}
```

**After (SuperTokens transformation):**

```csharp
// Registered automatically by AddSuperTokensAuthentication()
// Reads roles from the JWT payload, no database call needed.
```

The `SuperTokensClaimsTransformation` is registered as a singleton by `AddSuperTokensAuthentication`. It handles both JSON array roles (from the authentication handler) and comma-separated string roles (from legacy tokens).

## Common Pitfalls

### 1. getToken() guards break

Many frontend codebases have guards like:

```typescript
if (!getToken()) redirect("/login");
```

If `getToken()` returns `null` after migration, users get redirected even when they have a valid session cookie. Fix this by returning a placeholder string when the cookie exists:

```typescript
function getToken(): string | null {
  return document.cookie.includes("sAccessToken") ? "cookie" : null;
}
```

### 2. CORS credentials not sent

If your frontend and backend are on different origins, the browser will not send cookies unless you configure CORS to allow credentials.

**Backend (Program.cs):**

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

**Frontend (axios):**

```typescript
axios.defaults.withCredentials = true;
```

You cannot use `AllowAnyOrigin()` with `AllowCredentials()`. The browser rejects this combination. You must specify exact origins.

### 3. Cookie domain mismatch

If your frontend is on `app.example.com` and your API is on `api.example.com`, you need to set the cookie domain to `.example.com` (with leading dot) so the browser sends it to both subdomains.

```csharp
var options = new CookieOptions
{
    HttpOnly = true,
    Secure = true,
    SameSite = SameSiteMode.Lax,
    Domain = ".example.com",  // Share across subdomains
    Path = "/"
};
```

### 4. Secure flag on localhost

The `Secure = true` flag prevents cookies from being sent over HTTP. During local development (where you use `http://localhost`), this can cause cookies to not be set at all.

For local development, either:
- Use HTTPS with a dev certificate (`dotnet dev-certs https`)
- Set `Secure = false` in development (not recommended for production)

```csharp
var isDev = builder.Environment.IsDevelopment();
var options = new CookieOptions
{
    HttpOnly = true,
    Secure = !isDev,  // false in dev, true in production
    SameSite = isDev ? SameSiteMode.Lax : SameSiteMode.Lax,
    Path = "/"
};
```

### 5. SameSite=None requires Secure

If your frontend and backend are on different sites (different scheme, port, or registrable domain), you need `SameSite = SameSiteMode.None`. This requires `Secure = true`, which means it only works over HTTPS.

```csharp
var options = new CookieOptions
{
    HttpOnly = true,
    Secure = true,
    SameSite = SameSiteMode.None,  // Cross-site cookies
    Path = "/"
};
```

### 6. SignalR query token still needed

SignalR WebSocket connections cannot set custom headers in the browser. If you use SignalR, keep passing the access token as a query string parameter. The SDK's middleware and authentication handler both check for `?access_token=` on paths starting with `/hubs`.

```typescript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://api.example.com/hubs/notifications", {
    accessTokenFactory: () => getAccessTokenForSignalR()
  })
  .build();
```

The `getAccessTokenForSignalR()` function needs to return the actual access token. Since the token is in an HTTP-only cookie that JavaScript cannot read, you need an endpoint that reads the cookie and returns the token for SignalR use:

```csharp
[HttpGet("auth/signalr-token")]
[Authorize]
public IActionResult GetSignalRToken(IHttpContextAccessor httpContextAccessor)
{
    var token = httpContextAccessor.HttpContext!.Request.Cookies["sAccessToken"];
    if (string.IsNullOrEmpty(token))
        return Unauthorized();

    return Ok(new { token });
}
```

## What's Next

- [Auth Integration](./auth-integration.md): Full reference for the authentication handler and middleware
- [Troubleshooting](./troubleshooting.md): Common errors during and after migration
- [Configuration](./configuration.md): Cookie names, anti-CSRF, and Core connection
- [Examples](./examples.md): Example 1 shows the full signup, signin, verify flow
