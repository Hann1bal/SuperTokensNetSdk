# Examples

This page contains complete, runnable examples for common scenarios. Each example includes full code, an explanation of what it does, and the expected output.

## Example 1: Full Signup, Signin, and Session Verify Flow

This example shows the complete authentication flow: registering a user, signing them in, creating a session, and verifying it on subsequent requests.

### Backend (Program.cs)

```csharp
using SuperTokensSDK.Net.AspNetCore;
using SuperTokensSDK.Net.Configuration;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Recipes.EmailPassword;
using SuperTokensSDK.Net.Recipes.Session;
using SuperTokensSDK.Net.Recipes.UserRoles;

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

// Signup endpoint
app.MapPost("/auth/signup", async (
    EmailPasswordRecipe emailPassword,
    UserRolesRecipe userRoles,
    string email, string password, string role) =>
{
    var user = await emailPassword.SignUpAsync(email, password);
    if (user is null)
        return Results.BadRequest("Signup failed.");

    await userRoles.AddRoleAsync(user.Id!, role);

    return Results.Ok(new { user.Id, user.Email, role });
});

// Signin endpoint
app.MapPost("/auth/signin", async (
    EmailPasswordRecipe emailPassword,
    SessionRecipe session,
    UserRolesRecipe userRoles,
    HttpResponse response,
    string email, string password) =>
{
    var user = await emailPassword.SignInAsync(email, password);
    if (user is null)
        return Results.Unauthorized();

    var roles = await userRoles.GetRolesAsync(user.Id!);

    var container = await session.CreateSessionAsync(
        user.Id!,
        accessTokenPayload: new Dictionary<string, object>
        {
            ["roles"] = roles.ToArray()
        });

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

    return Results.Ok(new
    {
        container.SessionHandle,
        container.UserId,
        roles
    });
});

// Protected endpoint
app.MapGet("/me", async (SessionRecipe session, HttpRequest request) =>
{
    var token = request.Headers.Authorization.FirstOrDefault()
        ?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
        ?? request.Cookies["sAccessToken"];

    if (string.IsNullOrEmpty(token))
        return Results.Unauthorized();

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
}).RequireAuthorization();

app.Run();
```

### Frontend (React)

```typescript
import axios from "axios";

const api = axios.create({
  baseURL: "https://api.example.com",
  withCredentials: true,
});

// Signup
async function signup(email: string, password: string, role: string) {
  const response = await api.post("/auth/signup", null, {
    params: { email, password, role },
  });
  console.log("Signup result:", response.data);
  // Output: { id: "user-abc123", email: "alice@example.com", role: "user" }
}

// Signin
async function signin(email: string, password: string) {
  const response = await api.post("/auth/signin", null, {
    params: { email, password },
  });
  console.log("Signin result:", response.data);
  // Output: { sessionHandle: "sh-xyz", userId: "user-abc123", roles: ["user"] }
}

// Access protected endpoint
async function getMe() {
  const response = await api.get("/me");
  console.log("Me:", response.data);
  // Output: { userId: "user-abc123", roles: ["user"] }
}
```

### Expected Output

1. Signup returns `{ id: "user-abc123", email: "alice@example.com", role: "user" }`
2. Signin returns `{ sessionHandle: "sh-xyz", userId: "user-abc123", roles: ["user"] }` and sets three cookies
3. GET `/me` returns `{ userId: "user-abc123", roles: ["user"] }`

## Example 2: Role-Based Access Control

This example shows how to create a user, assign roles, and enforce role-based authorization in a controller.

```csharp
using SuperTokensSDK.Net.AspNetCore;
using SuperTokensSDK.Net.Configuration;
using SuperTokensSDK.Net.Recipes.EmailPassword;
using SuperTokensSDK.Net.Recipes.Session;
using SuperTokensSDK.Net.Recipes.UserRoles;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSuperTokens(options =>
{
    options.CoreUri = "http://localhost:3567";
    options.AppName = "MyApp";
});

builder.Services
    .AddAuthentication()
    .AddSuperTokensAuthentication();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("admin", policy =>
        policy.RequireRole("admin"));

    options.AddPolicy("staff", policy =>
        policy.RequireRole("admin", "staff"));
});

var app = builder.Build();

app.UseSuperTokensMiddleware();
app.UseAuthentication();
app.UseAuthorization();

// Create a user with admin role
app.MapPost("/admin/create-user", async (
    EmailPasswordRecipe emailPassword,
    UserRolesRecipe userRoles,
    string email, string password, string role) =>
{
    var user = await emailPassword.SignUpAsync(email, password);
    if (user is null)
        return Results.BadRequest("Failed to create user.");

    await userRoles.AddRoleAsync(user.Id!, role);

    return Results.Ok(new { user.Id, user.Email, role });
}).RequireAuthorization("admin");

// Check if a user has a specific role
app.MapGet("/users/{userId}/has-role/{role}", async (
    UserRolesRecipe userRoles,
    string userId, string role) =>
{
    var hasRole = await userRoles.DoesRoleExistAsync(userId, role);
    return Results.Ok(new { userId, role, hasRole });
});

// List all roles for a user
app.MapGet("/users/{userId}/roles", async (
    UserRolesRecipe userRoles,
    string userId) =>
{
    var roles = await userRoles.GetRolesAsync(userId);
    return Results.Ok(new { userId, roles });
});

// Remove a role from a user
app.MapDelete("/users/{userId}/roles/{role}", async (
    UserRolesRecipe userRoles,
    string userId, string role) =>
{
    await userRoles.RemoveRoleAsync(userId, role);
    return Results.Ok(new { userId, role, removed = true });
}).RequireAuthorization("admin");

app.Run();
```

### Expected Output

1. POST `/admin/create-user?email=bob@example.com&password=secret&role=staff` returns `{ id: "user-456", email: "bob@example.com", role: "staff" }`
2. GET `/users/user-456/has-role/staff` returns `{ userId: "user-456", role: "staff", hasRole: true }`
3. GET `/users/user-456/roles` returns `{ userId: "user-456", roles: ["staff"] }`
4. DELETE `/users/user-456/roles/staff` returns `{ userId: "user-456", role: "staff", removed: true }`

## Example 3: User Metadata Management

This example shows how to store and retrieve typed user metadata.

```csharp
using SuperTokensSDK.Net.AspNetCore;
using SuperTokensSDK.Net.Configuration;
using SuperTokensSDK.Net.Recipes.UserMetadata;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSuperTokens(options =>
{
    options.CoreUri = "http://localhost:3567";
    options.AppName = "MyApp";
});

var app = builder.Build();

// Define a typed profile model
public class UserProfile
{
    public string FullName { get; set; } = "";
    public int Age { get; set; }
    public string? Department { get; set; }
    public List<string> Skills { get; set; } = new();
}

// Store metadata
app.MapPost("/users/{userId}/profile", async (
    UserMetadataRecipe metadata,
    string userId,
    UserProfile profile) =>
{
    var update = new Dictionary<string, object>
    {
        ["fullName"] = profile.FullName,
        ["age"] = profile.Age,
        ["department"] = profile.Department ?? "",
        ["skills"] = profile.Skills
    };

    await metadata.UpdateMetadataAsync(userId, update);
    return Results.Ok(new { userId, saved = true });
});

// Retrieve metadata as a dictionary
app.MapGet("/users/{userId}/profile", async (
    UserMetadataRecipe metadata,
    string userId) =>
{
    var data = await metadata.GetMetadataAsync(userId);
    if (data is null)
        return Results.NotFound();

    return Results.Ok(data);
});

// Retrieve metadata as a typed object
app.MapGet("/users/{userId}/profile/typed", async (
    UserMetadataRecipe metadata,
    string userId) =>
{
    var profile = await metadata.GetMetadataAsAsync<UserProfile>(userId);
    if (profile is null)
        return Results.NotFound();

    return Results.Ok(profile);
});

app.Run();
```

### Expected Output

1. POST `/users/user-123/profile` with body `{ "fullName": "Jane Doe", "age": 32, "department": "Engineering", "skills": ["csharp", "react"] }` returns `{ userId: "user-123", saved: true }`
2. GET `/users/user-123/profile` returns `{ "fullName": "Jane Doe", "age": 32, "department": "Engineering", "skills": ["csharp", "react"] }`
3. GET `/users/user-123/profile/typed` returns `{ "fullName": "Jane Doe", "age": 32, "department": "Engineering", "skills": ["csharp", "react"] }`

## Example 4: Session Management

This example shows the full session lifecycle: create, verify, refresh, and revoke.

```csharp
using SuperTokensSDK.Net.AspNetCore;
using SuperTokensSDK.Net.Configuration;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Recipes.Session;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSuperTokens(options =>
{
    options.CoreUri = "http://localhost:3567";
    options.AppName = "MyApp";
});

var app = builder.Build();

// Create a session
app.MapPost("/sessions", async (
    SessionRecipe session,
    HttpResponse response,
    string userId) =>
{
    var container = await session.CreateSessionAsync(
        userId,
        accessTokenPayload: new Dictionary<string, object>
        {
            ["roles"] = new[] { "user" },
            ["createdBy"] = "example-app"
        });

    var cookieOptions = new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/"
    };

    response.Cookies.Append("sAccessToken", container.AccessToken!, cookieOptions);
    response.Cookies.Append("sRefreshToken", container.RefreshToken!, cookieOptions);

    return Results.Ok(new
    {
        container.SessionHandle,
        container.UserId,
        container.AccessTokenExpiry,
        container.RefreshTokenExpiry
    });
});

// Verify a session
app.MapGet("/sessions/verify", async (
    SessionRecipe session,
    HttpRequest request) =>
{
    var token = request.Headers.Authorization.FirstOrDefault()
        ?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
        ?? request.Cookies["sAccessToken"];

    if (string.IsNullOrEmpty(token))
        return Results.Unauthorized();

    try
    {
        var container = await session.VerifySessionAsync(token);
        var createdBy = container.GetClaim<string>("createdBy");

        return Results.Ok(new
        {
            container.SessionHandle,
            container.UserId,
            createdBy,
            container.UserDataInJwt
        });
    }
    catch (UnauthorizedException ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: 401);
    }
});

// Refresh a session
app.MapPost("/sessions/refresh", async (
    SessionRecipe session,
    HttpRequest request,
    HttpResponse response) =>
{
    var refreshToken = request.Cookies["sRefreshToken"];
    var antiCsrf = request.Cookies["sAntiCsrf"];

    if (string.IsNullOrEmpty(refreshToken))
        return Results.Unauthorized("No refresh token.");

    try
    {
        var container = await session.RefreshSessionAsync(refreshToken, antiCsrf);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        };

        response.Cookies.Append("sAccessToken", container.AccessToken!, cookieOptions);
        response.Cookies.Append("sRefreshToken", container.RefreshToken!, cookieOptions);

        return Results.Ok(new
        {
            container.SessionHandle,
            container.AccessTokenExpiry
        });
    }
    catch (UnauthorizedException)
    {
        return Results.Unauthorized("Refresh token expired.");
    }
    catch (TokenTheftDetectedException ex)
    {
        await session.RevokeSessionAsync(ex.SessionHandle);
        response.Cookies.Delete("sAccessToken");
        response.Cookies.Delete("sRefreshToken");
        response.Cookies.Delete("sAntiCsrf");
        return Results.Json(new { error = "Token theft detected." }, statusCode: 401);
    }
});

// Revoke a session
app.MapDelete("/sessions/{sessionHandle}", async (
    SessionRecipe session,
    HttpResponse response,
    string sessionHandle) =>
{
    await session.RevokeSessionAsync(sessionHandle);

    response.Cookies.Delete("sAccessToken");
    response.Cookies.Delete("sRefreshToken");
    response.Cookies.Delete("sAntiCsrf");

    return Results.Ok(new { revoked = true, sessionHandle });
});

app.Run();
```

### Expected Output

1. POST `/sessions?userId=user-123` returns `{ sessionHandle: "sh-abc", userId: "user-123", accessTokenExpiry: "2026-07-05T12:00:00Z", refreshTokenExpiry: "2026-07-12T12:00:00Z" }`
2. GET `/sessions/verify` returns `{ sessionHandle: "sh-abc", userId: "user-123", createdBy: "example-app", userDataInJwt: { ... } }`
3. POST `/sessions/refresh` returns `{ sessionHandle: "sh-abc", accessTokenExpiry: "2026-07-05T13:00:00Z" }`
4. DELETE `/sessions/sh-abc` returns `{ revoked: true, sessionHandle: "sh-abc" }`

## Example 5: MCP Gateway REST Endpoint

This example shows how to expose the MCP gateway as a REST API for AI agents.

```csharp
using SuperTokensSDK.Net.AspNetCore;
using SuperTokensSDK.Net.Configuration;
using SuperTokensSDK.Net.Mcp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSuperTokens(options =>
{
    options.CoreUri = "http://localhost:3567";
    options.AppName = "MyApp";
});

// Register MCP services
builder.Services.AddScoped<McpTools>();
builder.Services.AddScoped<McpGateway>();

var app = builder.Build();

// Tool discovery endpoint
app.MapGet("/mcp/tools", (McpGateway gateway) =>
{
    var definitions = gateway.GetToolDefinitions();
    return Results.Ok(definitions);
});

// Tool execution endpoint
app.MapPost("/mcp/execute", async (
    McpGateway gateway,
    McpToolRequest request) =>
{
    var result = await gateway.ExecuteToolAsync(request);

    if (result.IsError)
        return Results.Json(result, statusCode: 400);

    return Results.Ok(result);
});

app.Run();
```

### AI Agent Calling MCP Tools

```csharp
using System.Net.Http.Json;

var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com") };

// Discover tools
var tools = await httpClient.GetFromJsonAsync<List<McpToolDefinition>>("/mcp/tools");
Console.WriteLine($"Found {tools!.Count} tools:");
foreach (var tool in tools)
    Console.WriteLine($"  {tool.Name}: {tool.Description}");

// Create a user
var createRequest = new McpToolRequest
{
    Name = "create_user",
    Arguments = new Dictionary<string, object>
    {
        ["email"] = "agent@example.com",
        ["password"] = "generated-password",
        ["role"] = "viewer"
    }
};
var createResponse = await httpClient.PostAsJsonAsync("/mcp/execute", createRequest);
var createResult = await createResponse.Content.ReadFromJsonAsync<McpToolResult>();
Console.WriteLine($"Create user: {createResult!.Content[0].Text}");

// Verify a session
var verifyRequest = new McpToolRequest
{
    Name = "verify_session",
    Arguments = new Dictionary<string, object>
    {
        ["token"] = "eyJhbGciOiJSUzI1NiIs..."
    }
};
var verifyResponse = await httpClient.PostAsJsonAsync("/mcp/execute", verifyRequest);
var verifyResult = await verifyResponse.Content.ReadFromJsonAsync<McpToolResult>();
Console.WriteLine($"Verify session: {verifyResult!.Content[0].Text}");

// Get user roles
var rolesRequest = new McpToolRequest
{
    Name = "get_user_roles",
    Arguments = new Dictionary<string, object>
    {
        ["userId"] = "user-123"
    }
};
var rolesResponse = await httpClient.PostAsJsonAsync("/mcp/execute", rolesRequest);
var rolesResult = await rolesResponse.Content.ReadFromJsonAsync<McpToolResult>();
Console.WriteLine($"User roles: {rolesResult!.Content[0].Text}");
```

### Expected Output

```
Found 5 tools:
  create_user: Create a new SuperTokens user with email, password, and optional role.
  verify_session: Verify a SuperTokens access token.
  get_user_roles: Get roles assigned to a SuperTokens user.
  assign_role: Assign a role to a SuperTokens user.
  revoke_session: Revoke a SuperTokens session by session handle.
Create user: {"userId":"user-789","email":"agent@example.com","role":"viewer"}
Verify session: {"userId":"user-123","sessionHandle":"sh-abc","roles":["admin"]}
User roles: {"userId":"user-123","roles":["admin","staff"]}
```

## Example 6: Multi-Host Failover Configuration

This example shows how to configure the SDK with multiple Core instances for high availability.

```csharp
using SuperTokensSDK.Net.AspNetCore;
using SuperTokensSDK.Net.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSuperTokens(options =>
{
    // Three Core instances for high availability
    // The SDK cycles through them round-robin
    // If one fails, it fails over to the next
    options.CoreUri = "http://core-1:3567;http://core-2:3567;http://core-3:3567";

    options.ApiKey = builder.Configuration["SuperTokens:ApiKey"];
    options.AppName = "MyApp";
    options.ApiDomain = "https://api.example.com";
    options.WebsiteDomain = "https://example.com";

    // Custom cookie names to avoid collisions with other apps
    options.AccessTokenCookieName = "myApp_accessToken";
    options.RefreshTokenCookieName = "myApp_refreshToken";
    options.AntiCsrfCookieName = "myApp_antiCsrf";

    options.EnableAntiCsrf = true;
});

builder.Services
    .AddAuthentication()
    .AddSuperTokensAuthentication();

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSuperTokensMiddleware();
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint that verifies Core connectivity
app.MapGet("/health/supertokens", async (HttpClient http) =>
{
    var hosts = new[] { "core-1", "core-2", "core-3" };
    var results = new Dictionary<string, string>();

    foreach (var host in hosts)
    {
        try
        {
            var response = await http.GetAsync($"http://{host}:3567/hello");
            results[host] = response.IsSuccessStatusCode ? "healthy" : "unhealthy";
        }
        catch
        {
            results[host] = "unreachable";
        }
    }

    var allHealthy = results.Values.All(v => v == "healthy");
    return Results.Json(new { status = allHealthy ? "healthy" : "degraded", hosts = results },
        statusCode: allHealthy ? 200 : 503);
});

app.MapControllers();
app.Run();
```

### How failover works in practice

1. Request 1 goes to `core-1`. If it succeeds, the response is returned.
2. Request 2 goes to `core-2` (round-robin).
3. Request 3 goes to `core-3`.
4. Request 4 goes back to `core-1`.
5. If `core-2` throws `HttpRequestException` or times out, the SDK immediately tries `core-3`.
6. If `core-3` also fails, the SDK throws `SuperTokensException` with the message "All SuperTokens Core hosts failed or the request was repeatedly rate limited."

The rate limit retry (5 retries with linear backoff) runs on each host before failing over. This means a single request can trigger up to 5 retries on each of the 3 hosts (15 total attempts) before giving up.

## Example 7: Bridging with ASP.NET Core Identity

This example shows how to use SuperTokens for session management while keeping ASP.NET Core Identity for user storage.

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SuperTokensSDK.Net.AspNetCore;
using SuperTokensSDK.Net.Configuration;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Recipes.EmailPassword;
using SuperTokensSDK.Net.Recipes.Session;
using SuperTokensSDK.Net.Recipes.UserRoles;

// Identity user with a field for the SuperTokens user ID
public class AppUser : IdentityUser
{
    public string? SuperTokensUserId { get; set; }
}

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}

var builder = WebApplication.CreateBuilder(args);

// Identity
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// SuperTokens
builder.Services.AddSuperTokens(options =>
{
    options.CoreUri = "http://localhost:3567";
    options.AppName = "MyApp";
});

builder.Services
    .AddAuthentication()
    .AddSuperTokensAuthentication();

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSuperTokensMiddleware();
app.UseAuthentication();
app.UseAuthorization();

// Register: create user in both Identity and SuperTokens
app.MapPost("/auth/register", async (
    UserManager<AppUser> userManager,
    EmailPasswordRecipe emailPassword,
    UserRolesRecipe userRoles,
    string email, string password) =>
{
    // Create in Identity
    var identityUser = new AppUser { UserName = email, Email = email };
    var result = await userManager.CreateAsync(identityUser, password);
    if (!result.Succeeded)
        return Results.BadRequest(result.Errors);

    // Create in SuperTokens
    var stUser = await emailPassword.SignUpAsync(email, password);
    if (stUser is null)
    {
        // Rollback Identity user
        await userManager.DeleteAsync(identityUser);
        return Results.BadRequest("SuperTokens signup failed.");
    }

    // Link the two
    identityUser.SuperTokensUserId = stUser.Id;
    await userManager.UpdateAsync(identityUser);

    // Assign default role
    await userRoles.AddRoleAsync(stUser.Id!, "user");

    return Results.Ok(new { identityUser.Id, stUser.Id });
});

// Login: verify with Identity, create session with SuperTokens
app.MapPost("/auth/login", async (
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    EmailPasswordRecipe emailPassword,
    SessionRecipe session,
    UserRolesRecipe userRoles,
    HttpResponse response,
    string email, string password) =>
{
    var identityUser = await userManager.FindByEmailAsync(email);
    if (identityUser is null)
        return Results.Unauthorized();

    var result = await signInManager.CheckPasswordSignInAsync(
        identityUser, password, lockoutOnFailure: false);
    if (!result.Succeeded)
        return Results.Unauthorized();

    var stUserId = identityUser.SuperTokensUserId!;
    var roles = await userRoles.GetRolesAsync(stUserId);

    var container = await session.CreateSessionAsync(
        stUserId,
        accessTokenPayload: new Dictionary<string, object>
        {
            ["roles"] = roles.ToArray(),
            ["identityUserId"] = identityUser.Id
        });

    var cookieOptions = new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/"
    };

    response.Cookies.Append("sAccessToken", container.AccessToken!, cookieOptions);
    response.Cookies.Append("sRefreshToken", container.RefreshToken!, cookieOptions);

    return Results.Ok(new
    {
        container.SessionHandle,
        identityUser.Email,
        roles
    });
});

// Protected endpoint that reads from both systems
app.MapGet("/profile", async (
    UserManager<AppUser> userManager,
    UserRolesRecipe userRoles,
    ClaimsPrincipal user) =>
{
    var stUserId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (stUserId is null)
        return Results.Unauthorized();

    var identityUserId = user.FindFirst("identityUserId")?.Value;
    var identityUser = identityUserId is not null
        ? await userManager.FindByIdAsync(identityUserId)
        : null;

    var roles = await userRoles.GetRolesAsync(stUserId);

    return Results.Ok(new
    {
        superTokensUserId = stUserId,
        identityUser?.Email,
        roles
    });
}).RequireAuthorization();

app.Run();
```

### Expected Output

1. POST `/auth/register?email=carol@example.com&password=secret` returns `{ Id: "identity-guid", Id: "st-user-id" }`
2. POST `/auth/login?email=carol@example.com&password=secret` returns `{ sessionHandle: "sh-xyz", Email: "carol@example.com", roles: ["user"] }` and sets cookies
3. GET `/profile` returns `{ superTokensUserId: "st-user-id", Email: "carol@example.com", roles: ["user"] }`

## What's Next

- [Recipes](./recipes.md): Full API reference for all recipe methods
- [Auth Integration](./auth-integration.md): How the middleware and handler work
- [MCP Gateway](./mcp-gateway.md): Tool definitions and execution
- [Configuration](./configuration.md): All options for Core connection and cookies
- [Troubleshooting](./troubleshooting.md): Common errors and solutions
