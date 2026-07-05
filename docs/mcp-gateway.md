# MCP Gateway

The SDK includes a Model Context Protocol (MCP) gateway that exposes SuperTokens operations as tool definitions. AI agents can call these tools to create users, verify sessions, manage roles, and revoke sessions without needing to know the SuperTokens CDI API.

## What is MCP

The Model Context Protocol is a standard way for AI agents to discover and call tools exposed by a server. An MCP server publishes a list of tool definitions (name, description, input schema). The agent picks a tool, sends arguments, and receives a structured result.

The SDK's MCP gateway wraps the existing recipe methods and exposes them through a uniform interface. You can host the gateway behind any transport (REST, WebSocket, stdio) depending on your agent setup.

## Available Tools

The gateway exposes 5 tools:

| Tool | Description | Required Arguments |
|---|---|---|
| `create_user` | Create a new SuperTokens user with email, password, and optional role | `email`, `password` |
| `verify_session` | Verify a SuperTokens access token | `token` |
| `get_user_roles` | Get roles assigned to a SuperTokens user | `userId` |
| `assign_role` | Assign a role to a SuperTokens user | `userId`, `role` |
| `revoke_session` | Revoke a SuperTokens session by session handle | `sessionHandle` |

## McpGateway

The `McpGateway` class is the entry point. It holds a reference to `McpTools`, which in turn depends on the recipe classes.

### GetToolDefinitions()

Returns the list of tool definitions with their input schemas.

```csharp
public IReadOnlyList<McpToolDefinition> GetToolDefinitions()
```

Each definition contains a name, description, and JSON Schema input specification. The schema follows the MCP convention of `type: "object"` with `properties` and `required` arrays.

**Example output:**

```json
[
  {
    "name": "create_user",
    "description": "Create a new SuperTokens user with email, password, and optional role.",
    "inputSchema": {
      "type": "object",
      "properties": {
        "email": { "type": "string", "description": "User email address" },
        "password": { "type": "string", "description": "User password" },
        "role": { "type": "string", "description": "Optional role (admin, staff, user, viewer)" }
      },
      "required": ["email", "password"]
    }
  },
  {
    "name": "verify_session",
    "description": "Verify a SuperTokens access token.",
    "inputSchema": {
      "type": "object",
      "properties": {
        "token": { "type": "string", "description": "Access token to verify" }
      },
      "required": ["token"]
    }
  }
]
```

### ExecuteToolAsync()

Dispatches a tool call to the appropriate recipe method.

```csharp
public Task<McpToolResult> ExecuteToolAsync(
    McpToolRequest request,
    CancellationToken cancellationToken = default)
```

The method lowercases the tool name and switches on it. If the tool name is unknown, it returns an error result. If the tool throws an exception, the exception is caught and returned as an error result (the gateway never throws).

**Example:**

```csharp
var request = new McpToolRequest
{
    Name = "create_user",
    Arguments = new Dictionary<string, object>
    {
        ["email"] = "alice@example.com",
        ["password"] = "secure-password",
        ["role"] = "admin"
    }
};

var result = await gateway.ExecuteToolAsync(request);
if (result.IsError)
{
    Console.WriteLine($"Error: {result.Content[0].Text}");
}
else
{
    Console.WriteLine($"Result: {result.Content[0].Text}");
}
```

## Models

### McpToolDefinition

Describes a tool that the gateway exposes.

| Property | Type | JSON Key | Description |
|---|---|---|---|
| `Name` | `string` | `name` | Tool name (unique identifier). |
| `Description` | `string` | `description` | Human-readable description of what the tool does. |
| `InputSchema` | `Dictionary<string, object>` | `inputSchema` | JSON Schema describing the expected arguments. |

### McpToolRequest

A tool call request from an agent.

| Property | Type | JSON Key | Description |
|---|---|---|---|
| `Name` | `string` | `name` | Name of the tool to call. |
| `Arguments` | `Dictionary<string, object>?` | `arguments` | Arguments matching the tool's input schema. |

### McpToolResult

The result of a tool call.

| Property | Type | JSON Key | Description |
|---|---|---|---|
| `Content` | `List<McpToolContent>` | `content` | List of content items (typically one text item). |
| `IsError` | `bool` | `isError` | `true` if the tool call failed, `false` on success. |

### McpToolContent

A single content item in a tool result.

| Property | Type | JSON Key | Default | Description |
|---|---|---|---|---|
| `Type` | `string` | `type` | `"text"` | Content type. Currently only `"text"` is supported. |
| `Text` | `string` | `text` | `""` | The content text. |

## McpTools

The `McpTools` class implements the actual tool logic. It depends on `EmailPasswordRecipe`, `SessionRecipe`, and `UserRolesRecipe`.

### Tool Implementations

#### CreateUserAsync

Creates a user via `EmailPasswordRecipe.SignUpAsync`. If a role is provided, assigns it via `UserRolesRecipe.AddRoleAsync`.

```csharp
var user = await _emailPassword.SignUpAsync(email, password);
if (!string.IsNullOrWhiteSpace(role))
{
    await _userRoles.AddRoleAsync(user.Id!, role);
}
return SuccessResult(new { userId = user.Id, email = user.Email, role });
```

#### VerifySessionAsync

Verifies a token via `SessionRecipe.VerifySessionAsync`. Returns the user ID, session handle, and roles from the JWT payload.

#### GetUserRolesAsync

Calls `UserRolesRecipe.GetRolesAsync` and returns the role list.

#### AssignRoleAsync

Calls `UserRolesRecipe.AddRoleAsync` and returns a success status.

#### RevokeSessionAsync

Calls `SessionRecipe.RevokeSessionAsync` and returns a success status.

## Code Example: REST Endpoint Exposing MCP Tools

This example shows how to host the MCP gateway behind a REST API. AI agents can discover tools via `GET /mcp/tools` and call them via `POST /mcp/execute`.

```csharp
using SuperTokensSDK.Net.Mcp;

// Register MCP services
builder.Services.AddScoped<McpTools>();
builder.Services.AddScoped<McpGateway>();

var app = builder.Build();

// Tool discovery
app.MapGet("/mcp/tools", (McpGateway gateway) =>
{
    var definitions = gateway.GetToolDefinitions();
    return Results.Ok(definitions);
});

// Tool execution
app.MapPost("/mcp/execute", async (
    McpGateway gateway,
    McpToolRequest request) =>
{
    var result = await gateway.ExecuteToolAsync(request);
    return Results.Ok(result);
});

app.Run();
```

### Sample request and response

**Request:**

```bash
POST /mcp/execute
Content-Type: application/json

{
  "name": "create_user",
  "arguments": {
    "email": "bob@example.com",
    "password": "hunter2",
    "role": "staff"
  }
}
```

**Response:**

```json
{
  "content": [
    {
      "type": "text",
      "text": "{\"userId\":\"user-123\",\"email\":\"bob@example.com\",\"role\":\"staff\"}"
    }
  ],
  "isError": false
}
```

## Code Example: AI Agent Calling MCP Tools

This example shows an AI agent (or any HTTP client) discovering and calling MCP tools.

```csharp
using System.Net.Http.Json;

var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("https://api.example.com");

// Step 1: Discover available tools
var tools = await httpClient.GetFromJsonAsync<List<McpToolDefinition>>("/mcp/tools");

Console.WriteLine("Available tools:");
foreach (var tool in tools!)
{
    Console.WriteLine($"  {tool.Name}: {tool.Description}");
}

// Step 2: Call a tool
var request = new McpToolRequest
{
    Name = "verify_session",
    Arguments = new Dictionary<string, object>
    {
        ["token"] = "eyJhbGciOiJSUzI1NiIs..."
    }
};

var response = await httpClient.PostAsJsonAsync("/mcp/execute", request);
var result = await response.Content.ReadFromJsonAsync<McpToolResult>();

if (result!.IsError)
{
    Console.WriteLine($"Tool failed: {result.Content[0].Text}");
}
else
{
    Console.WriteLine($"Tool result: {result.Content[0].Text}");
    // Output: {"userId":"user-123","sessionHandle":"abc...","roles":["admin"]}
}
```

## Error Handling

The gateway catches all exceptions and returns them as error results. The `IsError` flag on `McpToolResult` indicates failure. The error message is in `Content[0].Text`.

Common error scenarios:

| Scenario | Error Message |
|---|---|
| Unknown tool name | `Unknown tool: <name>` |
| Missing required argument | `Failed to create user.` (or similar) |
| Core unreachable | `Tool execution failed: All SuperTokens Core hosts failed...` |
| Invalid token | `Tool execution failed: Access token has expired.` |

The gateway never throws. This makes it safe to call from any context, including AI agent loops that do not handle exceptions well.

## DI Registration

The MCP classes are not registered by `AddSuperTokens`. You need to register them separately:

```csharp
builder.Services.AddSuperTokens(options =>
{
    options.CoreUri = "http://localhost:3567";
    options.AppName = "MyApp";
});

// Register MCP services
builder.Services.AddScoped<McpTools>();
builder.Services.AddScoped<McpGateway>();
```

`McpTools` depends on `EmailPasswordRecipe`, `SessionRecipe`, and `UserRolesRecipe`, which are already registered by `AddSuperTokens`.

## What's Next

- [Recipes](./recipes.md): The recipe methods that power the MCP tools
- [Examples](./examples.md): Example 5 shows a complete MCP gateway REST endpoint
- [Getting Started](./getting-started.md): How to set up the SDK
- [Configuration](./configuration.md): Core connection and options
