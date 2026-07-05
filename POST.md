I needed SuperTokens integration for an ASP.NET Core project and couldn't find a solid .NET SDK, so I built one. Sharing it here in case anyone else needs it.

**What it does:**
- CDI 5.0 client (talking directly to SuperTokens Core)
- Cookie-based auth with local JWT verification (works around a Core 11.x bug)
- 5 recipes: EmailPassword, Session, UserRoles, UserMetadata, TOTP
- ASP.NET Core auth handler + session middleware
- MCP gateway (so AI agents can call SuperTokens too)
- Zero external dependencies (just Microsoft.AspNetCore.App framework)

**What it doesn't do (yet):**
- Passwordless
- Multi-tenancy
- JWT signing (verification only)

Published on NuGet as `SuperTokensSDK.Net`. Built it for myself, but if it saves someone a week of work, that's enough.

Repo: https://github.com/Hann1bal/SuperTokensNetSdk

Also — does anyone know if the SuperTokens team is okay with community SDKs like this? Don't want to step on anything. cc @supertokens
