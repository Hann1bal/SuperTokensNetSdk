# Changelog

All notable changes to SuperTokensSDK.Net will be documented in this file.

## [Unreleased]

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
