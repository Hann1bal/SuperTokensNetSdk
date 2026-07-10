# Changelog

All notable changes to SuperTokensSDK.Net will be documented in this file.

## [Unreleased]

## [2.7.5] - 2026-07-10

### Fixed
- `SuperTokensApiMiddleware` now handles `POST /auth/session/refresh` end-to-end instead of proxying it with an empty body.
  - Reads `sRefreshToken` from the request cookie and `sAntiCsrf` from cookie/header.
  - Returns `401 { "status": "UNAUTHORISED" }` when the refresh token is missing, preventing pre-login infinite refresh loops.
  - Calls `SessionRecipe.RefreshSessionAsync`, which builds the CDI body exactly like the official Go SDK (`refreshToken`, `enableAntiCsrf`, `useDynamicSigningKey`).
  - Attaches new `sAccessToken`, `sRefreshToken`, and `sAntiCsrf` cookies from the refreshed session.
  - Sets the `front-token` response header used by `supertokens-web-js` to track session info on the client.
- Updated `Access-Control-Expose-Headers` to include `front-token`.
- Updated `SuperTokensApiMiddlewareTests` to cover successful refresh, missing refresh token, and missing anti-CSRF token scenarios.

## [2.7.4] - 2026-07-10

### Fixed
- `SuperTokensApiMiddleware` now handles `/auth/signup` and `/auth/signin` end-to-end instead of proxying them to Core.
  - Parses FDI `formFields` array and converts it to CDI `email`/`password`.
  - Calls `EmailPasswordRecipe.SignUpAsync`/`SignInAsync`, then creates a session via `SessionRecipe.CreateSessionAsync`.
  - Attaches `sAccessToken`, `sRefreshToken`, and `sAntiCsrf` cookies with correct attributes (HttpOnly, Secure, SameSite, paths).
  - Returns FDI-shaped responses: `OK`, `FIELD_ERROR`, and `WRONG_CREDENTIALS_ERROR`.
- Removed `/auth/signup` and `/auth/signin` from the generic proxy route map.
- Updated `SuperTokensApiMiddlewareTests` with FDI signup/signin coverage, including missing fields, wrong credentials, and email-already-exists cases.

## [2.7.3] - 2026-07-10

### Fixed
- `SuperTokensApiMiddleware` forwards `Set-Cookie` headers from SuperTokens Core responses to the browser.
- Added `SuperTokensOptions.AllowedOrigins` for explicit CORS origin allowlist.

## [2.6.0] - 2026-07-05

### Security
- `CoreApiClient.VerifySessionAsync` now verifies JWT signatures using the JWKS published by SuperTokens Core before falling back to a local decode. This closes the previous gap where access tokens were accepted without signature verification.

### Added
- `JwksClient` singleton for caching and refreshing Core's `/.well-known/jwks.json`.
- Session claim validation primitives: `TypeSessionClaim`, `SessionClaimValidator`, `ClaimValidationResult`, `PrimitiveClaim`, `PrimitiveArrayClaim`, and `BooleanClaim`.
- Built-in UserRoles claims: `st-role` and `st-perm` via `UserRolesClaims`.
- `SessionRecipe` claim operations: `ValidateClaimsAsync`, `FetchAndSetClaimAsync`, `SetClaimValueAsync`, `GetClaimValueAsync`, and `RemoveClaimAsync`.
- New CDI helper `CoreApiClient.UpdateJwtDataAsync` for mutating the access token payload.
- `SuperTokensExtensions` registers `JwksClient` in DI and wires it into `CoreApiClient`.
- Updated `CoreApiClientTests` and `SessionRecipeTests` to exercise the JWKS verification path; `SuperTokensAuthenticationHandlerTests` now use a mocked `ICoreApiClient` for deterministic behavior.
- Added `SuperTokensApiMiddleware` with `app.UseSuperTokensApi()` to proxy `/auth/*` frontend API calls to SuperTokens Core CDI endpoints, plus CORS preflight support.
- Added overridable recipe pattern: `RecipeOverrides`, `IOverridableRecipe`, and per-recipe override classes (`EmailPasswordOverrides`, `SessionOverrides`, `PasswordlessOverrides`, `ThirdPartyOverrides`) allowing nullable delegate replacements for recipe methods.
- Added `ICoreApiClient.ProxyToCoreAsync` and `CoreApiClient.ProxyToCoreAsync` for raw request forwarding.
- Registered override classes as scoped services in `SuperTokensExtensions`.
- Added tests: `SuperTokensApiMiddlewareTests` and `RecipeOverridesTests`.
- `ThirdPartyRecipe` with `SignInUpAsync`, `ManuallyCreateOrUpdateUserAsync`, `GetUserByIdAsync`, `GetUserByThirdPartyInfoAsync`, and `GetUsersByEmailAsync` methods for OAuth and social login.
- Six built-in OAuth providers: Google, GitHub, Apple, Discord, Facebook, and GitLab, exposed through `BuiltInProviders`.
- `TypeProvider` and `TypeProviderConfig` classes for registering custom OAuth providers alongside the built-in set.
- `DashboardRecipe` with 13 methods for the dashboard API: `SignInAsync`, `SignOutAsync`, `GetUsersAsync`, `GetUsersCountAsync`, `ListTenantsAsync`, `GetUserDetailsAsync`, `RemoveUserAsync`, `VerifyUserEmailAsync`, `UpdateUserPasswordAsync`, `UpdateUserMetadataAsync`, `GetUserSessionsAsync`, `SearchTagsAsync`, and `GetAnalyticsAsync`.
- `IEmailDelivery` interface and `SmtpEmailDelivery` implementation using `System.Net.Mail`, with no new NuGet dependencies.
- `ISmsDelivery` interface and `TwilioSmsDelivery` implementation using `HttpClient`, with no Twilio SDK dependency.
- All new recipes, ingredients, and override classes registered in `SuperTokensExtensions` DI.
