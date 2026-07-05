# SuperTokensSDK.Net

UNOFFICIAL! SuperTokens integration package for ASP.NET Core applications.

## Features

- Easy integration with SuperTokens authentication service
- Support for session management
- Built-in token validation
- ASP.NET Core middleware support
- Compatible with .NET 8.0

## Installation

Install the package from NuGet:

```bash
dotnet add package SuperTokensSDK.Net --version 1.0.0
```

Or via Package Manager Console:

```powershell
Install-Package SuperTokensSDK.Net -Version 1.0.0
```
Quick Start
Add the service to your ASP.NET Core application:

```csharp
// Program.cs
builder.Services.AddSuperTokens(options =>
{
    options.ConnectionURI = "https://your-supertokens-instance.com";
    options.ApiKey = "your-api-key";
    options.CookieSecure = true;
});
```
Use the middleware:

```csharp
app.UseSuperTokensMiddleware();
```
Protect your endpoints:

```csharp
[SuperTokensAuthorize]
[HttpGet("protected")]
public IActionResult GetProtectedData()
{
    var userId = HttpContext.GetSuperTokensUserId();
    return Ok(new { message = $"Hello {userId}" });
}
```
# Configuration Options
| Option |	Type |	Description |	Default |
|--------|-------|--------------|-----------|
| ConnectionURI |	string |	SuperTokens | instance | URL |	Required |
| ApiKey |	string |	API key for authentication |	null |
| CookieSecure |	bool |	Secure flag for cookies |	true |
| CookieSameSite |	SameSiteMode |	SameSite policy for cookies	| SameSiteMode.Lax |

# Dependencies
+ Microsoft.AspNetCore.Mvc (>= 2.3.0)

+ Microsoft.AspNetCore.Mvc.Core (>= 2.3.0)

+ Microsoft.Extensions.Http (>= 9.0.3)

+ System.Net.Http (>= 4.3.4)
