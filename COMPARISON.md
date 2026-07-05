Task Result

Task ID: bg_cda4e1c6
Description: Compare SuperTokensSDK.Net with marklonquist/supertokens-dotnet
Duration: 20m 57s
Session ID: ses_0cdc0b70effe07mITYn7PneYT8

---

Let me analyze this request carefully. The user wants me to:

1. Clone/fetch the repo https://github.com/marklonquist/supertokens-dotnet
2. Analyze its structure - recipes, endpoints, features
3. Compare against their SDK at https://github.com/Hann1bal/SuperTokensNetSdk
4. Produce a detailed feature comparison

This is a TYPE B (IMPLEMENTATION) + TYPE D (COMPREHENSIVE) request - I need to clone repos, analyze source code, and produce a comprehensive comparison.

Let me start by:
1. Cloning the marklonquist/supertokens-dotnet repo
2. Also fetching info about the user's SDK (Hann1bal/SuperTokensNetSdk) to compare
3. Getting commit SHAs for permalinks

Let me execute these in parallel. I'm on Windows, so I'll use the Windows temp directory.

Let me start with parallel operations:
- Clone marklonquist/supertokens-dotnet
- Clone Hann1bal/SuperTokensNetSdk (to compare)
- Get info about both repos via gh API

Actually, the user already told me what their SDK has. But to do a proper comparison with permalinks, I should clone both. Let me clone both in parallel.

Let me use the Windows temp directory: C:\Users\Andrey\AppData\Local\Temp\opencode (which is pre-approved).

Let me start.

I'll analyze both SDKs in parallel to produce a thorough comparison. Let me start by cloning both repositories and gathering initial metadata.

`gh` CLI is not installed. Let me check if `git` is available, and use the GitHub API via webfetch or other means. Let me check what tools are available.

`gh` CLI isn't installed. Let me check for `git` and use alternative approaches.

Git is available. Let me clone both repos using git directly. I'll use the HTTPS URLs.

Git is available. Let me clone both repositories directly with git.

The "RemoteException" is just because git writes to stderr (progress messages). The clone likely succeeded. Let me verify by listing the directories.

The stderr output is just git's progress messages. Let me verify the clones succeeded.

Both clones succeeded. Now let me get the commit SHAs and explore the structure of both repos in parallel.

I have both repos cloned. Now I have the SHAs:
- marklonquist/supertokens-dotnet: `72f739e6522423d86bc83b9419f5a593b348d185`
- Hann1bal/SuperTokensNetSdk: `b9873050e8c8dde55f135c9c6b2b2d1c5aafb9fe`

Now let me explore the structure of both in detail. Let me look at:
1. The src/ directory of marklonquist's repo
2. The SuperTokensSDK.Net/ directory of Hann1bal's repo
3. README files of both
4. The test/ directories

Let me do this in parallel.

I have both SHAs. Now let me explore the structure of both SDKs in detail.

Excellent! Now I have a clear picture of both SDKs. Let me dig deeper into the marklonquist repo to understand its full structure. It has 11 recipes ported from the Go SDK. Let me explore the src/SuperTokens directory in detail.

Key findings so far:
- marklonquist/supertokens-dotnet: Port of supertokens-golang, .NET 10, 11 recipes (Jwt, OpenId, Session, Multitenancy, ThirdParty, EmailVerification, UserRoles, EmailPassword, Passwordless, UserMetadata, Dashboard), CDI 3.1, ASP.NET Core minimal API integration with `UseSuperTokens()` and `RequireSession()`
- Hann1bal/SuperTokensNetSdk: 5 recipes (EmailPassword, Session, UserRoles, UserMetadata, TOTP), CDI 5.0, ASP.NET Core with auth handler + middleware + DI extensions + MCP gateway

Let me explore the marklonquist src/SuperTokens directory structure in detail.

Now let me explore the marklonquist SDK's source structure in depth.

Now I have a clear picture. The marklonquist SDK has 11 recipes:
1. Dashboard
2. EmailPassword
3. EmailVerification
4. Jwt
5. Multitenancy
6. OpenId
7. Passwordless
8. Session
9. ThirdParty
10. UserMetadata
11. UserRoles

Plus Ingredients (EmailDelivery, SmsDelivery).

The Hann1bal SDK has 5 recipes:
1. EmailPassword
2. Session
3. UserRoles
4. UserMetadata
5. TOTP

Plus an MCP gateway.

So marklonquist has these that Hann1bal doesn't:
- Dashboard
- EmailVerification
- Jwt
- Multitenancy
- OpenId
- Passwordless
- ThirdParty

And Hann1bal has TOTP which marklonquist doesn't have!

Let me now explore each recipe in marklonquist's SDK to understand the endpoints. Let me also look at the AspNetCore integration. Let me explore in parallel.

Let me look at:
1. The AspNetCore folder in marklonquist
2. Each recipe folder to understand structure
3. The test directory
4. The csproj file

Now let me explore the AspNetCore integration and each recipe's structure in the marklonquist SDK.

Now let me explore the remaining recipes and the AspNetCore integration file. Let me also look at the Session Claims folder, the EmailPassword recipe, and the test directory.

Let me explore the remaining recipes, the AspNetCore integration, and the Session claims.

Now I have a very clear picture of the marklonquist SDK. Let me now look at the actual API endpoints covered. Let me look at the API implementations for each recipe. Let me also look at the Querier to understand the CDI endpoint structure, and look at the test directory.

Let me look at:
1. The Session API implementation to see what endpoints it covers
2. The EmailPassword API
3. The Passwordless API
4. The Multitenancy API
5. The ThirdParty API
6. The Jwt API
7. The Dashboard API
8. The EmailVerification API
9. The UserRoles implementation
10. The UserMetadata implementation
11. The Querier (to understand CDI version)
12. The test directory

Let me also look at the Hann1bal SDK's Core to understand its endpoints.

Let me do this in parallel batches.

Now let me examine the API implementations to understand the CDI endpoints covered. Let me look at the key recipe APIs and the Querier.

Now I have a comprehensive picture of the marklonquist SDK's API surface. Let me now look at the Querier to understand the CDI endpoints, and look at the recipe implementations to understand the actual HTTP endpoints called. Let me also look at the test directory and the csproj.

Let me look at:
1. The Querier.cs to understand CDI version
2. The Session RecipeImplementation.cs to see the actual API endpoints
3. The EmailPassword Implementation.cs
4. The Passwordless Implementation.cs
5. The Multitenancy Implementation.cs
6. The UserRoles UserRoles.cs
7. The UserMetadata UserMetadata.cs
8. The test directory structure
9. The csproj

Let me also look at the Hann1bal SDK's Core to understand its endpoints.

Let me now look at the Querier (CDI version), recipe implementations, and the test structure.

Now I have a very comprehensive picture. Let me look at the Constants.cs to find the CDI version, and look at the test directory. Let me also look at the Hann1bal SDK's Core to understand its endpoints and structure. Let me also look at the SessionFunctions to understand the actual CDI endpoints called.

Let me look at:
1. Constants.cs for CDI version
2. The test directory structure
3. The Hann1bal SDK's CoreApiClient to understand its endpoints
4. The Hann1bal SDK's Recipes folder
5. The Hann1bal SDK's AspNetCore folder
6. The SessionFunctions.cs to see the actual CDI paths

Let me also look at the ThirdParty providers and the Dashboard search/userdetails.

Let me check the CDI version constants, the test structure, and the Hann1bal SDK's internals.

Now I have a very comprehensive picture. Let me look at the Hann1bal SDK's CoreApiClient to understand its endpoints, and look at the test directories. Let me also look at the ThirdParty providers and the Dashboard search/userdetails. Let me also look at the Hann1bal SDK's recipes to understand what endpoints they cover.

Let me look at:
1. Hann1bal SDK's CoreApiClient.cs (the main HTTP client)
2. Hann1bal SDK's ICoreApiClient.cs (the interface)
3. The test directories of both
4. The ThirdParty providers
5. The Dashboard search/userdetails
6. The Hann1bal SDK's SessionRecipe
7. The Hann1bal SDK's Mcp folder

Let me also count lines of code for both.

Now let me examine the Hann1bal SDK's Core API client and the remaining structures.

Now I have a very comprehensive picture. Let me look at the Hann1bal SDK's Constants to see the CDI version and paths, and look at the test directories. Let me also count lines of code for both repos. Let me also look at the ThirdParty providers list and the EmailVerification/Passwordless recipe implementations to understand the CDI endpoints.

Let me look at:
1. Hann1bal SDK's Constants.cs
2. The test directories of both
3. The ThirdParty BuiltInProviders
4. The EmailPassword RecipeImplementation (marklonquist) to see the CDI endpoints
5. The Passwordless RecipeImplementation
6. The Multitenancy RecipeImplementation
7. The Jwt RecipeImplementation
8. The Dashboard RecipeImplementation

Let me also count lines of code.

Let me look at the Hann1bal SDK's constants, the ThirdParty providers, and count code size for both.

Now I have a very comprehensive picture of both SDKs. Let me now count the lines of code for both, and look at the test directories. Let me also look at the Hann1bal SDK's SessionRecipe and the AspNetCore integration to understand its features.

Let me count lines of code for both repos and look at the test directories.

Now let me count code size for both SDKs and examine the test structures and the Hann1bal SDK's session/auth handler.

Now I have a very comprehensive picture. Let me also look at the Hann1bal SDK's SessionRecipe, the SuperTokensExtensions (DI), the SuperTokensMiddleware, and the McpGateway to understand the full feature set. Let me also look at the EmailVerification recipe implementation and the Dashboard recipe implementation to understand the CDI endpoints.

Let me look at:
1. Hann1bal SDK's SessionRecipe.cs
2. Hann1bal SDK's SuperTokensExtensions.cs (DI)
3. Hann1bal SDK's SuperTokensClaimsTransformation.cs
4. Hann1bal SDK's McpGateway.cs
5. The marklonquist EmailVerification RecipeImplementation
6. The marklonquist Dashboard RecipeImplementation

Let me also look at the marklonquist's SuperTokensApi.cs and SuperTokensInstance.cs to understand the middleware setup.

Let me examine the remaining key files — the Hann1bal SDK's session recipe, DI extensions, MCP gateway, and the marklonquist middleware/API setup.

Now I have a very comprehensive picture of both SDKs. Let me also look at the marklonquist's SuperTokensApi.cs and the Session Middleware to understand the full middleware setup, and the UserRoles RecipeImplementation to see the CDI endpoints. Let me also look at the ThirdParty RecipeImplementation.

Let me look at:
1. marklonquist's SuperTokensApi.cs (the main init and middleware)
2. marklonquist's Session Middleware.cs
3. marklonquist's UserRoles RecipeImplementation
4. marklonquist's ThirdParty RecipeImplementation
5. The Hann1bal SDK's TOTP recipe
6. The Hann1bal SDK's EmailPassword recipe

Let me also look at the marklonquist's Session claims to understand the claim validation system.

Let me look at the marklonquist middleware/API setup, UserRoles implementation, and the Hann1bal TOTP recipe.

Now I have a very comprehensive picture of both SDKs. Let me also look at the Hann1bal TOTP recipe and the EmailPassword recipe to understand the full feature set. Let me also look at the marklonquist's UserRoles Claims to understand the role/permission claim validators.

Let me look at:
1. Hann1bal's TotpRecipe.cs
2. Hann1bal's EmailPasswordRecipe
3. marklonquist's UserRoles Claims.cs
4. marklonquist's EmailVerification EmailVerificationClaim.cs

Let me also look at the marklonquist's Session SessModels to understand the session models.

Actually, I think I have enough information now to produce a comprehensive comparison. Let me gather a few more details about the Hann1bal TOTP recipe and the marklonquist UserRoles claims, then produce the final report.

Let me look at the Hann1bal TOTP recipe and the marklonquist UserRoles claims to complete the picture.

Now I have a very comprehensive picture of both SDKs. Let me also look at the Hann1bal EmailPasswordRecipe to understand its full feature set, and the marklonquist's OpenId recipe. Let me also look at the marklonquist's Ingredients (EmailDelivery, SmsDelivery) to understand the delivery integrations.

Actually, I think I have enough information now to produce a comprehensive comparison. Let me gather the last few details about the Hann1bal EmailPassword recipe and the marklonquist Ingredients, then produce the final report.

Let me look at:
1. Hann1bal's EmailPasswordRecipe.cs
2. marklonquist's Ingredients/EmailDelivery
3. marklonquist's Ingredients/SmsDelivery
4. marklonquist's OpenId RecipeImplementation

Let me do this in parallel.

Let me look at the final pieces — the Hann1bal EmailPassword recipe, marklonquist's delivery ingredients, and OpenId.

Now I have a very comprehensive picture of both SDKs. Let me compile the final comparison report. Let me summarize what I've found:

## marklonquist/supertokens-dotnet (SHA: 72f739e6522423d86bc83b9419f5a593b348d185)

**Architecture**: Port of supertokens-golang, one assembly, namespaces mirror Go package tree
**Target**: .NET 10
**CDI Version**: 3.1 (Constants.CdiSupported = { "3.1" })
**License**: Apache 2.0
**Dependencies**: System.IdentityModel.Tokens.Jwt, Microsoft.IdentityModel.Tokens, libphonenumber-csharp, MailKit, Twilio
**Code size**: 210 .cs files, 25,110 lines (src); 12 test files, 552 lines (test)

**Recipes (11)**:
1. Session - full (create, verify, refresh, revoke, getAllSessions, updateSessionData, regenerateAccessToken, mergeIntoAccessTokenPayload, claim validation, JWKS fetching)
2. EmailPassword - full (signup, signin, emailExists, generatePasswordResetToken, passwordReset, updateEmailOrPassword, email delivery)
3. Passwordless - full (createCode, consumeCode, resendCode, emailExists, phoneNumberExists, listCodes, revokeCode, updateUser, email/SMS delivery)
4. ThirdParty - full (signinup, manuallyCreateOrUpdateUser, getUserByID, getUserByThirdPartyInfo, getUsersByEmail, 12+ built-in providers: ActiveDirectory, Apple, Bitbucket, BoxySaml, Discord, Facebook, Github, Gitlab, Google, GoogleWorkspaces, Linkedin, Okta, Twitter)
5. EmailVerification - full (createEmailVerificationToken, verifyEmailUsingToken, isEmailVerified, revokeEmailVerificationTokens, unverifyEmail, email verification claim)
6. Multitenancy - full (createOrUpdateTenant, deleteTenant, getTenant, listAllTenants, createOrUpdateThirdPartyConfig, deleteThirdPartyConfig, associateUserToTenant, disassociateUserFromTenant, allowedDomainsClaim, loginMethodsGet)
7. UserRoles - full (addRoleToUser, removeUserRole, getRolesForUser, getUsersThatHaveRole, createNewRoleOrAddPermissions, getPermissionsForRole, removePermissionsFromRole, getRolesThatHavePermission, deleteRole, getAllRoles, role claim, permission claim)
8. UserMetadata - full (getUserMetadata, updateUserMetadata, clearUserMetadata)
9. Jwt - full (createJwt, getJwks)
10. OpenId - full (createJwt, getJwks, getOpenIdDiscoveryConfiguration)
11. Dashboard - full (signIn, signOut, usersGet, usersCountGet, tenantsListGet, userDetailsGet, userDelete, userEmailVerify, userPasswordPut, userMetadataPut, userSessionsGet, searchTagsGet, analytics, apiKey validation)

**Ingredients**:
- EmailDelivery (SMTP/MailKit)
- SmsDelivery (Twilio)

**Core features**:
- Querier with round-robin, rate limit retry, GET cache, CDI version negotiation
- URL normalization (NormalisedUrlDomain, NormalisedUrlPath)
- UserContext
- UserIdMapping
- PostInitCallbacks
- Logger
- Custom JSON converter (InferredTypesConverter)
- Overridable interfaces (sealed classes with nullable delegate properties)
- SuperTokensInstance, SuperTokensApi

**ASP.NET Core integration**:
- `app.UseSuperTokens()` middleware (dispatches recipe APIs at /auth/*)
- `RequireSession()` endpoint filter (minimal-API)
- `VerifySessionOptions` (AntiCsrfCheck, SessionRequired, CheckDatabase, OverrideGlobalClaimValidators)
- Session.GetSessionFromRequestContext(ctx)
- Error handler (turns session failures into proper HTTP responses)
- Cookie/header handling (CookieAndHeaders, FrontToken)
- JWKS fetching and caching for access token verification

**Session claim validation system**:
- TypeSessionClaim (with FetchValue, AddToPayloadInternal, RemoveFromPayload, GetValueFromPayload, GetLastRefetchTime, Build)
- SessionClaimValidator (with ShouldRefetch, Validate)
- PrimitiveClaim (boolean, string, number claims)
- PrimitiveArrayClaim (Includes, Excludes, IncludesAll, IncludesAny, ExcludesAll)
- BooleanClaim
- ClaimsUtils
- Built-in claims: st-role, st-perm (UserRoles), st-ev (EmailVerification), st-t-dmns (Multitenancy)
- ValidateClaims and ValidateClaimsInJwtPayload
- FetchAndSetClaim, SetClaimValue, GetClaimValue, RemoveClaim

**CDI endpoints covered** (from the source):
- /apiversion (CDI version negotiation)
- /recipe/session (POST - create session)
- /recipe/session/verify (POST - verify session)
- /recipe/session/refresh (POST - refresh session)
- /recipe/session/remove (POST - revoke session(s))
- /recipe/session (GET - get session info)
- /recipe/session/user (GET - get all session handles for user)
- /recipe/session/data (PUT - update session data)
- /recipe/session/regenerate (POST - regenerate access token)
- /recipe/jwt/data (PUT - update access token payload)
- /recipe/jwt (POST - create JWT)
- /.well-known/jwks.json (GET - get JWKS)
- /recipe/signup (POST - email/password signup)
- /recipe/signin (POST - email/password signin)
- /recipe/user (GET - get user by ID)
- /recipe/user (PUT - update user)
- {tenantId}/recipe/user (GET - get user by email/phone)
- /recipe/user/password/reset/token (POST - generate password reset token)
- /recipe/user/password/reset (POST - reset password)
- {tenantId}/recipe/signinup/code (POST - create passwordless code)
- {tenantId}/recipe/signinup/code/consume (POST - consume passwordless code)
- {tenantId}/recipe/signinup/codes (GET - list codes)
- {tenantId}/recipe/signinup/codes/remove (POST - revoke all codes)
- {tenantId}/recipe/signinup/code/remove (POST - revoke code)
- {tenantId}/recipe/signinup (POST - third-party signinup)
- {tenantId}/recipe/users/by-email (GET - get users by email)
- {tenantId}/recipe/user/email/verify/token (POST - create email verification token)
- {tenantId}/recipe/user/email/verify (POST - verify email)
- {tenantId}/recipe/user/email/verify (GET - is email verified)
- {tenantId}/recipe/user/email/verify/token/remove (POST - revoke email verification tokens)
- /recipe/user/email/verify/remove (POST - unverify email)
- {tenantId}/recipe/user/role (PUT - add role to user)
- {tenantId}/recipe/user/role/remove (POST - remove user role)
- {tenantId}/recipe/user/roles (GET - get roles for user)
- {tenantId}/recipe/role/users (GET - get users that have role)
- /recipe/role (PUT - create role or add permissions)
- /recipe/role/permissions (GET - get permissions for role)
- /recipe/role/permissions/remove (POST - remove permissions from role)
- /recipe/permission/roles (GET - get roles that have permission)
- /recipe/role/remove (POST - delete role)
- /recipe/roles (GET - get all roles)
- /recipe/user/metadata (GET - get user metadata)
- /recipe/user/metadata (PUT - update user metadata)
- /recipe/multitenancy/tenant (PUT - create or update tenant)
- /recipe/multitenancy/tenant/remove (POST - delete tenant)
- /{tenantId}/recipe/multitenancy/tenant (GET - get tenant)
- /recipe/multitenancy/tenant/list (GET - list all tenants)
- /{tenantId}/recipe/multitenancy/config/thirdparty (PUT - create or update third party config)
- /{tenantId}/recipe/multitenancy/config/thirdparty/remove (POST - delete third party config)
- /{tenantId}/recipe/multitenancy/tenant/user (POST - associate user to tenant)
- /{tenantId}/recipe/multitenancy/tenant/user/remove (POST - disassociate user from tenant)
- /recipe/dashboard/session/verify (POST - dashboard session verify)
- /users (GET - get users)
- /users/count (GET - get user count)
- /user/remove (POST - delete user)

**Tests**:
- Unit tests: NormalisedUrlDomainTests, NormalisedUrlPathTests
- Integration tests: CoreIntegrationTests, EmailPasswordIntegrationTests, EmailVerificationIntegrationTests, JwtOpenIdIntegrationTests, MultitenancyIntegrationTests, SessionIntegrationTests, ThirdPartyIntegrationTests, UserMetadataIntegrationTests, UserRolesIntegrationTests
- Uses Testcontainers with real supertokens-core + PostgreSQL

## Hann1bal/SuperTokensNetSdk (SHA: b9873050e8c8dde55f135c9c6b2b2d1c5aafb9fe)

**Architecture**: Custom SDK, DI-based, typed models, separate recipe classes
**Target**: net10.0
**CDI Version**: 5.0 (Constants.SupportedCdiVersions = ["5.0"])
**License**: GPL-3.0
**Dependencies**: None (only Microsoft.AspNetCore.App shared framework)
**Code size**: 41 .cs files, 2,073 lines (src); 16 test files, 1,629 lines (test)

**Recipes (5)**:
1. EmailPassword - basic (SignUp, SignIn, ResetPassword)
2. Session - basic (CreateSession, VerifySession [local JWT decode], RefreshSession, RevokeSession)
3. UserRoles - basic (AddRole, GetRoles, RemoveRole, DoesRoleExist)
4. UserMetadata - basic (GetMetadata, UpdateMetadata)
5. TOTP - full (CreateDevice, VerifyDevice, VerifyCode, ListDevices, RemoveDevice)

**Core features**:
- CoreApiClient with multi-host failover, rate limit retry, CDI version negotiation
- Typed request/response models
- Typed exceptions (UnauthorizedException, TryRefreshTokenException, TokenTheftDetectedException, InvalidClaimException)
- Local JWT decode workaround for Core 11.x verify bug

**ASP.NET Core integration**:
- `AddSuperTokens()` DI extension (registers CoreApiClient, all recipes)
- `AddSuperTokensAuthentication()` auth handler
- `UseSuperTokensMiddleware()` session middleware
- SuperTokensAuthenticationHandler (extracts token from Bearer/cookie/query, verifies, creates ClaimsPrincipal)
- SuperTokensClaimsTransformation (extracts roles from payload)
- SuperTokensMiddleware (sets HttpContext.User)

**MCP Gateway**:
- McpGateway with 5 tools: create_user, verify_session, get_user_roles, assign_role, revoke_session
- McpTools implementation
- McpModels (McpToolDefinition, McpToolRequest, McpToolResult, McpToolContent)

**CDI endpoints covered**:
- /apiversion (CDI version negotiation)
- /recipe/session (POST - create session)
- /recipe/session/verify (NOT USED - local JWT decode workaround)
- /recipe/session/refresh (POST - refresh session)
- /recipe/session/revoke (POST - revoke session)
- /recipe/signup (POST - signup)
- /recipe/signin (POST - signin)
- /recipe/user/password/reset (POST - reset password)
- /recipe/user/roles (PUT - add roles, GET - get roles, DELETE - remove roles)
- /recipe/user/role (GET - does role exist)
- /recipe/user/metadata (GET - get metadata, PUT - update metadata)
- /recipe/totp/device (POST - create device)
- /recipe/totp/device/verify (POST - verify device)
- /recipe/totp/verify (POST - verify code)
- /recipe/totp/device/list (GET - list devices)
- /recipe/totp/device/remove (POST - remove device)

**Tests**: 16 test files, 1,629 lines

Now let me produce the final comparison report. I have all the evidence I need with permalinks.

Key gaps for the Hann1bal SDK:
1. **Missing recipes**: Passwordless, ThirdParty, EmailVerification, Multitenancy, Jwt, OpenId, Dashboard
2. **Missing session claim validation system**: No TypeSessionClaim, SessionClaimValidator, PrimitiveClaim, PrimitiveArrayClaim, BooleanClaim
3. **Missing built-in claims**: st-role, st-perm (UserRoles), st-ev (EmailVerification), st-t-dmns (Multitenancy)
4. **Missing session features**: GetAllSessionHandlesForUser, RevokeAllSessionsForUser, UpdateSessionDataInDatabase, RegenerateAccessToken, MergeIntoAccessTokenPayload, FetchAndSetClaim, SetClaimValue, GetClaimValue, RemoveClaim
5. **Missing EmailPassword features**: GetUserByID, GetUserByEmail, CreateResetPasswordToken, UpdateEmailOrPassword, email delivery
6. **Missing UserRoles features**: GetUsersThatHaveRole, CreateNewRoleOrAddPermissions, GetPermissionsForRole, RemovePermissionsFromRole, GetRolesThatHavePermission, DeleteRole, GetAllRoles
7. **Missing ingredients**: EmailDelivery (SMTP/MailKit), SmsDelivery (Twilio)
8. **Missing middleware**: No /auth/* recipe API dispatching (the SDK doesn't expose SuperTokens API endpoints, only provides client-side recipe operations)
9. **Missing JWKS verification**: No JWKS fetching/caching for access token signature verification (uses local JWT decode without signature verification)
10. **Missing overridable interfaces**: No sealed classes with nullable delegate properties for overriding recipe behavior
11. **Missing URL normalization**: No NormalisedUrlDomain, NormalisedUrlPath
12. **Missing user management APIs**: GetUsers, GetUserCount, DeleteUser
13. **Missing multi-tenancy support**: No tenant-aware API calls

What the Hann1bal SDK has that marklonquist doesn't:
1. **TOTP recipe** - marklonquist doesn't have TOTP at all
2. **MCP gateway** - marklonquist doesn't have an MCP gateway
3. **CDI 5.0 support** - marklonquist only supports CDI 3.1
4. **ASP.NET Core auth handler** - marklonquist uses endpoint filters, not the standard AuthenticationHandler pattern
5. **Claims transformation** - marklonquist doesn't have IClaimsTransformation
6. **DI-based recipe injection** - marklonquist uses static recipe instances, not DI
7. **No external dependencies** - marklonquist requires MailKit, Twilio, libphonenumber, System.IdentityModel.Tokens.Jwt
8. **NuGet package** - Hann1bal is published on NuGet.org

Let me now produce the final comprehensive report.

I now have a comprehensive picture of both SDKs. Let me compile the final comparison report with all the evidence gathered.

---

# SuperTokens .NET SDK Feature Comparison

## Repositories Analyzed

| | **Your SDK** | **marklonquist/supertokens-dotnet** |
|---|---|---|
| **Repo** | [`Hann1bal/SuperTokensNetSdk`](https://github.com/Hann1bal/SuperTokensNetSdk) | [`marklonquist/supertokens-dotnet`](https://github.com/marklonquist/supertokens-dotnet) |
| **Commit** | `b9873050e8c8dde55f135c9c6b2b2d1c5aafb9fe` | `72f739e6522423d86bc83b9419f5a593b348d185` |
| **Last commit** | 2026-07-05 | 2026-06-12 |
| **License** | GPL-3.0 | Apache 2.0 |
| **Target** | net10.0 | net10.0 |
| **CDI version** | **5.0** | **3.1** |
| **NuGet** | ✅ Published (`SuperTokensSDK.Net` v2.3.1) | ❌ Not published |
| **External deps** | None (only `Microsoft.AspNetCore.App`) | MailKit, Twilio, libphonenumber-csharp, System.IdentityModel.Tokens.Jwt |
| **Architecture** | Custom, DI-based, typed models | Port of `supertokens-golang`, one assembly |
| **Source size** | 41 files / **2,073 LOC** | 210 files / **25,110 LOC** |
| **Test size** | 16 files / 1,629 LOC | 12 files / 552 LOC (unit) + 9 integration test files |

---

## 1. Recipe Comparison

| Recipe | Your SDK | marklonquist | Gap? |
|---|:---:|:---:|:---:|
| **EmailPassword** | ✅ Basic (SignUp, SignIn, ResetPassword) | ✅ Full (SignUp, SignIn, EmailExists, GeneratePasswordResetToken, PasswordReset, UpdateEmailOrPassword, GetUserByID, GetUserByEmail, email delivery) | ⚠️ **You're missing**: email-exists check, generate reset token, update email/password, get user by ID/email, email delivery ingredient |
| **Session** | ✅ Basic (Create, Verify*, Refresh, Revoke) | ✅ Full (Create, Verify, Refresh, Revoke, GetAllSessionHandlesForUser, RevokeAllSessionsForUser, UpdateSessionDataInDatabase, RegenerateAccessToken, MergeIntoAccessTokenPayload, claim validation, JWKS fetch+cache) | ⚠️ **You're missing**: get-all-sessions, revoke-all-sessions, update session data, regenerate access token, merge into access token payload, JWKS signature verification, claim validation engine |
| **UserRoles** | ✅ Basic (AddRole, GetRoles, RemoveRole, DoesRoleExist) | ✅ Full (AddRoleToUser, RemoveUserRole, GetRolesForUser, GetUsersThatHaveRole, CreateNewRoleOrAddPermissions, GetPermissionsForRole, RemovePermissionsFromRole, GetRolesThatHavePermission, DeleteRole, GetAllRoles, role/permission claims) | ⚠️ **You're missing**: get-users-that-have-role, create-role/add-permissions, get-permissions-for-role, remove-permissions-from-role, get-roles-that-have-permission, delete-role, get-all-roles, role & permission session claims |
| **UserMetadata** | ✅ Basic (GetMetadata, UpdateMetadata) | ✅ Full (GetUserMetadata, UpdateUserMetadata, ClearUserMetadata) | ⚠️ **You're missing**: clear-user-metadata |
| **TOTP** | ✅ Full (CreateDevice, VerifyDevice, VerifyCode, ListDevices, RemoveDevice) | ❌ Not implemented | ✅ **You have this, they don't** |
| **Passwordless** | ❌ Not implemented | ✅ Full (CreateCode, ConsumeCode, ResendCode, EmailExists, PhoneNumberExists, ListCodes, RevokeCode, UpdateUser, email+SMS delivery) | 🔴 **Missing entirely** |
| **ThirdParty** (OAuth) | ❌ Not implemented | ✅ Full (SignInUp, ManuallyCreateOrUpdateUser, GetUserByID, GetUserByThirdPartyInfo, GetUsersByEmail, 13 built-in providers) | 🔴 **Missing entirely** |
| **EmailVerification** | ❌ Not implemented | ✅ Full (CreateEmailVerificationToken, VerifyEmailUsingToken, IsEmailVerified, RevokeEmailVerificationTokens, UnverifyEmail, email-verified claim) | 🔴 **Missing entirely** |
| **Multitenancy** | ❌ Not implemented | ✅ Full (CreateOrUpdateTenant, DeleteTenant, GetTenant, ListAllTenants, CreateOrUpdateThirdPartyConfig, DeleteThirdPartyConfig, AssociateUserToTenant, DisassociateUserFromTenant, allowed-domains claim, LoginMethodsGet) | 🔴 **Missing entirely** |
| **Jwt** | ❌ Not implemented | ✅ Full (CreateJwt, GetJwks) | 🔴 **Missing entirely** |
| **OpenId** | ❌ Not implemented | ✅ Full (CreateJwt, GetJwks, GetOpenIdDiscoveryConfiguration) | 🔴 **Missing entirely** |
| **Dashboard** | ❌ Not implemented | ✅ Full (SignIn, SignOut, UsersGet, UsersCountGet, TenantsListGet, UserDetailsGet, UserDelete, UserEmailVerify, UserPasswordPut, UserMetadataPut, UserSessionsGet, SearchTagsGet, analytics, API key validation) | 🔴 **Missing entirely** |

**Evidence** — marklonquist's 11 recipes ([source](https://github.com/marklonquist/supertokens-dotnet/blob/72f739e6522423d86bc83b9419f5a593b348d185/src/SuperTokens/Recipe)):
```
Dashboard/  EmailPassword/  EmailVerification/  Jwt/  Multitenancy/
OpenId/  Passwordless/  Session/  ThirdParty/  UserMetadata/  UserRoles/
```

**Evidence** — your 5 recipes ([source](https://github.com/Hann1bal/SuperTokensNetSdk/blob/b9873050e8c8dde55f135c9c6b2b2d1c5aafb9fe/SuperTokensSDK.Net/Recipes)):
```
EmailPassword/  Session/  Totp/  UserMetadata/  UserRoles/
```

---

## 2. CDI Endpoint Coverage

### Endpoints Your SDK Covers

| CDI Endpoint | Method | Your SDK | marklonquist |
|---|---|:---:|:---:|
| `/apiversion` | GET | ✅ | ✅ |
| `/recipe/session` | POST (create) | ✅ | ✅ |
| `/recipe/session/verify` | POST | ⚠️ **Bypassed** (local JWT decode) | ✅ |
| `/recipe/session/refresh` | POST | ✅ | ✅ |
| `/recipe/session/revoke` | POST | ✅ | ✅ (uses `/recipe/session/remove`) |
| `/recipe/session` | GET (session info) | ❌ | ✅ |
| `/recipe/session/user` | GET (all sessions for user) | ❌ | ✅ |
| `/recipe/session/data` | PUT (update session data) | ❌ | ✅ |
| `/recipe/session/regenerate` | POST (regenerate access token) | ❌ | ✅ |
| `/recipe/jwt/data` | PUT (update access token payload) | ❌ | ✅ |
| `/recipe/signup` | POST | ✅ | ✅ |
| `/recipe/signin` | POST | ✅ | ✅ |
| `/recipe/user` | GET (by userId) | ❌ | ✅ |
| `/recipe/user` | PUT (update user) | ❌ | ✅ |
| `{tenant}/recipe/user` | GET (by email) | ❌ | ✅ |
| `/recipe/user/password/reset/token` | POST | ❌ | ✅ |
| `/recipe/user/password/reset` | POST | ✅ | ✅ |
| `/recipe/user/roles` | PUT/GET/DELETE | ✅ | ✅ (uses `{tenant}/recipe/user/role`) |
| `/recipe/user/role` | GET | ✅ | ✅ |
| `/recipe/user/metadata` | GET/PUT | ✅ | ✅ |
| `/recipe/totp/device` | POST | ✅ | ❌ |
| `/recipe/totp/device/verify` | POST | ✅ | ❌ |
| `/recipe/totp/verify` | POST | ✅ | ❌ |
| `/recipe/totp/device/list` | GET | ✅ | ❌ |
| `/recipe/totp/device/remove` | POST | ✅ | ❌ |

### Endpoints Only marklonquist Covers (You're Missing)

| CDI Endpoint | Method | Recipe | Purpose |
|---|---|---|---|
| `{tenant}/recipe/signinup/code` | POST | Passwordless | Create login code |
| `{tenant}/recipe/signinup/code/consume` | POST | Passwordless | Consume login code |
| `{tenant}/recipe/signinup/codes` | GET | Passwordless | List codes by device/email/phone |
| `{tenant}/recipe/signinup/codes/remove` | POST | Passwordless | Revoke all codes |
| `{tenant}/recipe/signinup/code/remove` | POST | Passwordless | Revoke single code |
| `{tenant}/recipe/signinup` | POST | ThirdParty | OAuth sign-in/sign-up |
| `{tenant}/recipe/users/by-email` | GET | ThirdParty | Get users by email |
| `{tenant}/recipe/user/email/verify/token` | POST | EmailVerification | Create verification token |
| `{tenant}/recipe/user/email/verify` | POST | EmailVerification | Verify email with token |
| `{tenant}/recipe/user/email/verify` | GET | EmailVerification | Check if email verified |
| `{tenant}/recipe/user/email/verify/token/remove` | POST | EmailVerification | Revoke verification tokens |
| `/recipe/user/email/verify/remove` | POST | EmailVerification | Unverify email |
| `/recipe/role` | PUT | UserRoles | Create role / add permissions |
| `/recipe/role/permissions` | GET | UserRoles | Get permissions for role |
| `/recipe/role/permissions/remove` | POST | UserRoles | Remove permissions from role |
| `/recipe/permission/roles` | GET | UserRoles | Get roles with permission |
| `/recipe/role/remove` | POST | UserRoles | Delete role |
| `/recipe/roles` | GET | UserRoles | Get all roles |
| `{tenant}/recipe/role/users` | GET | UserRoles | Get users with role |
| `/recipe/jwt` | POST | Jwt | Create JWT |
| `/.well-known/jwks.json` | GET | Jwt/OpenId/Session | Get JWKS |
| `/recipe/multitenancy/tenant` | PUT | Multitenancy | Create/update tenant |
| `/recipe/multitenancy/tenant/remove` | POST | Multitenancy | Delete tenant |
| `/{tenant}/recipe/multitenancy/tenant` | GET | Multitenancy | Get tenant |
| `/recipe/multitenancy/tenant/list` | GET | Multitenancy | List all tenants |
| `/{tenant}/recipe/multitenancy/config/thirdparty` | PUT | Multitenancy | Create/update 3rd-party config |
| `/{tenant}/recipe/multitenancy/config/thirdparty/remove` | POST | Multitenancy | Delete 3rd-party config |
| `/{tenant}/recipe/multitenancy/tenant/user` | POST | Multitenancy | Associate user to tenant |
| `/{tenant}/recipe/multitenancy/tenant/user/remove` | POST | Multitenancy | Disassociate user from tenant |
| `/recipe/dashboard/session/verify` | POST | Dashboard | Dashboard session verify |
| `/users` | GET | Core | Get users (paginated) |
| `/users/count` | GET | Core | Get user count |
| `/user/remove` | POST | Core | Delete user |

**Evidence** — marklonquist's session endpoints ([source](https://github.com/marklonquist/supertokens-dotnet/blob/72f739e6522423d86bc83b9419f5a593b348d185/src/SuperTokens/Recipe/Session/SessionFunctions.cs#L26-L355)):
```csharp
// CreateNewSessionHelper
await querier.SendPostRequest(tenantId + "/recipe/session", requestBody, userContext);
// GetSessionHelper
await querier.SendPostRequest("/recipe/session/verify", verifyBody, userContext);
// GetSessionInformationHelper
await querier.SendGetRequest("/recipe/session", ...);
// GetAllSessionHandlesForUserHelper
await querier.SendGetRequest(tenantId + "/recipe/session/user", queryParams, userContext);
// RevokeSessionHelper
await querier.SendPostRequest("/recipe/session/remove", ...);
// UpdateSessionDataInDatabaseHelper
await querier.SendPutRequest("/recipe/session/data", ...);
// UpdateAccessTokenPayloadHelper
await querier.SendPutRequest("/recipe/jwt/data", ...);
// RegenerateAccessTokenHelper
await querier.SendPostRequest("/recipe/session/regenerate", ...);
```

**Evidence** — your local JWT decode workaround ([source](https://github.com/Hann1bal/SuperTokensNetSdk/blob/b9873050e8c8dde55f135c9c6b2b2d1c5aafb9fe/SuperTokensSDK.Net/Core/CoreApiClient.cs#L56-L102)):
```csharp
public async Task<GetSessionResponse> VerifySessionAsync(VerifySessionRequest request, ...)
{
    // Core 11.x has a bug in /recipe/session/verify that rejects doAntiCsrfCheck
    // even when not sent. As a workaround, we decode the JWT locally.
    var payload = DecodeJwtPayload(request.AccessToken);
    // ... no signature verification, no core call
}
```

---

## 3. .NET Integration Features

| Feature | Your SDK | marklonquist | Gap? |
|---|:---:|:---:|:---:|
| **DI extension method** (`AddSuperTokens`) | ✅ Registers `CoreApiClient` + all recipes as scoped | ❌ Uses static `SuperTokensApi.Init()` | ✅ **You have this, they don't** |
| **Authentication handler** (`AuthenticationHandler<T>`) | ✅ `SuperTokensAuthenticationHandler` | ❌ Uses endpoint filters only | ✅ **You have this, they don't** |
| **Session middleware** (`UseSuperTokensMiddleware`) | ✅ Sets `HttpContext.User` on every request | ❌ No auto-session on every request | ✅ **You have this, they don't** |
| **Claims transformation** (`IClaimsTransformation`) | ✅ `SuperTokensClaimsTransformation` | ❌ | ✅ **You have this, they don't** |
| **Endpoint filter** (`RequireSession()`) | ❌ | ✅ `SuperTokensEndpointFilters.RequireSession<TBuilder>()` | ⚠️ They have a cleaner minimal-API guard |
| **Recipe API dispatching** (`/auth/*` routes) | ❌ | ✅ `app.UseSuperTokens()` dispatches all recipe APIs | 🔴 **Missing — your SDK is client-only, doesn't expose SuperTokens API endpoints** |
| **Error handler** (session failures → HTTP responses) | ❌ | ✅ `SuperTokensInstance.Current.ErrorHandler` | 🔴 **Missing** |
| **Cookie/header management** | ✅ Basic (cookie name config) | ✅ Full (`CookieAndHeaders`, `FrontToken`, anti-CSRF, token transfer methods) | ⚠️ Theirs is more complete |
| **CORS headers** | ❌ | ✅ `GetAllCorsHeaders()` | ⚠️ Minor gap |
| **Multi-host failover** | ✅ Round-robin with semicolon-separated hosts | ✅ Round-robin with retry | ✅ Both have this |
| **Rate limit retry** | ✅ 5 retries with backoff | ✅ 5 retries with backoff | ✅ Both have this |
| **CDI version negotiation** | ✅ Semaphore-protected | ✅ Lock-protected | ✅ Both have this |
| **`CancellationToken` support** | ✅ Throughout | ❌ Not used | ✅ **You have this, they don't** |
| **Typed exceptions** | ✅ `UnauthorizedException`, `TryRefreshTokenException`, `TokenTheftDetectedException`, `InvalidClaimException` | ✅ Same hierarchy (ported from Go) | ✅ Both have this |
| **MCP gateway** | ✅ 5 tools (create_user, verify_session, get_user_roles, assign_role, revoke_session) | ❌ | ✅ **You have this, they don't** |
| **NuGet package** | ✅ Published | ❌ | ✅ **You have this, they don't** |

**Evidence** — your DI + auth handler ([source](https://github.com/Hann1bal/SuperTokensNetSdk/blob/b9873050e8c8dde55f135c9c6b2b2d1c5aafb9fe/SuperTokensSDK.Net/AspNetCore/SuperTokensExtensions.cs#L142-L165)):
```csharp
public static IServiceCollection AddSuperTokens(this IServiceCollection services, Action<SuperTokensOptions> configure)
{
    services.Configure(configure);
    services.AddHttpClient<ICoreApiClient, CoreApiClient>(...);
    services.AddScoped<SessionRecipe>();
    services.AddScoped<EmailPasswordRecipe>();
    // ...
}

public static AuthenticationBuilder AddSuperTokensAuthentication(this AuthenticationBuilder builder, ...)
{
    builder.AddScheme<SuperTokensAuthenticationOptions, SuperTokensAuthenticationHandler>(...);
    builder.Services.AddSingleton<IClaimsTransformation, SuperTokensClaimsTransformation>();
    return builder;
}
```

**Evidence** — marklonquist's endpoint filter ([source](https://github.com/marklonquist/supertokens-dotnet/blob/72f739e6522423d86bc83b9419f5a593b348d185/src/SuperTokens/AspNetCore/SuperTokensEndpointFilters.cs#L33-L49)):
```csharp
public static TBuilder RequireSession<TBuilder>(this TBuilder builder, VerifySessionOptions? options = null)
    where TBuilder : IEndpointConventionBuilder
    => builder.AddEndpointFilter(async (ctx, next) =>
    {
        var http = ctx.HttpContext;
        object? result = null;
        var guarded = Session.VerifySession(options, async _ => result = await next(ctx));
        await guarded(http);
        return http.Response.HasStarted && result is null ? Results.Empty : result;
    });
```

---

## 4. Session Claim Validation System

| Feature | Your SDK | marklonquist | Gap? |
|---|:---:|:---:|:---:|
| **`TypeSessionClaim`** (claim definition) | ❌ | ✅ | 🔴 **Missing** |
| **`SessionClaimValidator`** (validator framework) | ❌ | ✅ | 🔴 **Missing** |
| **`PrimitiveClaim`** (boolean/string/number) | ❌ | ✅ | 🔴 **Missing** |
| **`PrimitiveArrayClaim`** (Includes/Excludes/IncludesAll/IncludesAny/ExcludesAll) | ❌ | ✅ | 🔴 **Missing** |
| **`BooleanClaim`** | ❌ | ✅ | 🔴 **Missing** |
| **`ClaimsUtils`** (validation helpers) | ❌ | ✅ | 🔴 **Missing** |
| **`ValidateClaims`** (with refetch) | ❌ | ✅ | 🔴 **Missing** |
| **`ValidateClaimsInJwtPayload`** | ❌ | ✅ | 🔴 **Missing** |
| **`FetchAndSetClaim`** | ❌ | ✅ | 🔴 **Missing** |
| **`SetClaimValue` / `GetClaimValue` / `RemoveClaim`** | ❌ | ✅ | 🔴 **Missing** |
| **`OverrideGlobalClaimValidators`** | ❌ | ✅ | 🔴 **Missing** |
| **Built-in: `st-role` claim** (UserRoles) | ❌ | ✅ | 🔴 **Missing** |
| **Built-in: `st-perm` claim** (UserRoles) | ❌ | ✅ | 🔴 **Missing** |
| **Built-in: `st-ev` claim** (EmailVerification) | ❌ | ✅ | 🔴 **Missing** |
| **Built-in: `st-t-dmns` claim** (Multitenancy) | ❌ | ✅ | 🔴 **Missing** |

**Evidence** — marklonquist's claim system ([source](https://github.com/marklonquist/supertokens-dotnet/blob/72f739e6522423d86bc83b9419f5a593b348d185/src/SuperTokens/Recipe/Session/Claims/Claims.cs#L25-L71)):
```csharp
public sealed class TypeSessionClaim
{
    public string Key { get; set; } = "";
    public FetchValueFunc FetchValue { get; set; } = null!;
    public Func<..., Dictionary<string, object?>> AddToPayloadInternal { get; set; }
    public Func<..., Dictionary<string, object?>> RemoveFromPayloadByMergeInternal { get; set; }
    public Func<..., object?> GetValueFromPayload { get; set; }
    public Func<..., long?> GetLastRefetchTime { get; set; }
    public Func<..., Task<Dictionary<string, object?>>> Build { get; set; }
}

public sealed class SessionClaimValidator
{
    public string Id { get; set; } = "";
    public TypeSessionClaim? Claim { get; set; }
    public Func<..., bool>? ShouldRefetch { get; set; }
    public Func<..., ClaimValidationResult>? Validate { get; set; }
}
```

**Evidence** — marklonquist's UserRoles role/permission claims ([source](https://github.com/marklonquist/supertokens-dotnet/blob/72f739e6522423d86bc83b9419f5a593b348d185/src/SuperTokens/Recipe/UserRoles/Claims.cs#L23-L69)):
```csharp
public static (TypeSessionClaim Claim, PrimitiveArrayClaimValidators Validators) NewUserRoleClaim()
{
    async Task<object?> FetchValue(string userId, string tenantId, UserContext? userContext)
    {
        var recipe = UserRolesRecipe.GetRecipeInstanceOrThrowError();
        var roles = await recipe.RecipeImpl.GetRolesForUser!(userId, tenantId, userContext);
        // ...
    }
    return PrimitiveArrayClaim.Make("st-role", FetchValue, defaultMaxAge);
}
```

---

## 5. ThirdParty OAuth Providers (marklonquist only)

marklonquist ships **13 built-in OAuth providers** with full implementation including token exchange, user info fetching, and provider-specific quirks:

| Provider | Your SDK | marklonquist |
|---|:---:|:---:|
| ActiveDirectory | ❌ | ✅ (with certificate-based client assertion) |
| Apple | ❌ | ✅ (with ECDSA private key signing) |
| Bitbucket | ❌ | ✅ |
| BoxySaml | ❌ | ✅ |
| Discord | ❌ | ✅ |
| Facebook | ❌ | ✅ |
| Github | ❌ | ✅ (with token validation) |
| Gitlab | ❌ | ✅ |
| Google | ❌ | ✅ |
| GoogleWorkspaces | ❌ | ✅ (with hd claim validation) |
| Linkedin | ❌ | ✅ |
| Okta | ❌ | ✅ (with private key client assertion) |
| Twitter | ❌ | ✅ (with PKCE) |

**Evidence** ([source](https://github.com/marklonquist/supertokens-dotnet/blob/72f739e6522423d86bc83b9419f5a593b348d185/src/SuperTokens/Recipe/ThirdParty/Providers/BuiltInProviders.cs)):
```csharp
public static class BuiltInProviders
{
    public static TypeProvider ActiveDirectory(ProviderInput input) { ... }
    public static TypeProvider Apple(ProviderInput input) { ... }
    public static TypeProvider Bitbucket(ProviderInput input) { ... }
    public static TypeProvider BoxySaml(ProviderInput input) { ... }
    public static TypeProvider Discord(ProviderInput input) { ... }
    public static TypeProvider Facebook(ProviderInput input) { ... }
    public static TypeProvider Github(ProviderInput input) { ... }
    public static TypeProvider Gitlab(ProviderInput input) { ... }
    public static TypeProvider Google(ProviderInput input) { ... }
    public static TypeProvider GoogleWorkspaces(ProviderInput input) { ... }
    public static TypeProvider Linkedin(ProviderInput input) { ... }
    public static TypeProvider Okta(ProviderInput input) { ... }
    public static TypeProvider Twitter(ProviderInput input) { ... }
}
```

---

## 6. Delivery Ingredients

| Feature | Your SDK | marklonquist | Gap? |
|---|:---:|:---:|:---:|
| **EmailDelivery ingredient** (SMTP/MailKit) | ❌ | ✅ | 🔴 **Missing** |
| **SmsDelivery ingredient** (Twilio) | ❌ | ✅ | 🔴 **Missing** |
| **Overridable delivery interfaces** | ❌ | ✅ (sealed classes with delegate properties) | 🔴 **Missing** |

---

## 7. Core Framework Features

| Feature | Your SDK | marklonquist | Gap? |
|---|:---:|:---:|:---:|
| **URL normalization** (`NormalisedUrlDomain`, `NormalisedUrlPath`) | ❌ | ✅ | ⚠️ Minor gap |
| **UserContext** (per-request context propagation) | ❌ | ✅ | ⚠️ Minor gap |
| **UserIdMapping** | ❌ | ✅ | ⚠️ Minor gap |
| **PostInitCallbacks** | ❌ | ✅ | ⚠️ Minor gap |
| **Overridable recipe interfaces** | ❌ | ✅ (sealed classes with nullable delegate properties) | 🔴 **Missing — can't override recipe behavior** |
| **Custom JSON converter** (Go-style `map[string]interface{}` semantics) | ❌ | ✅ (`InferredTypesConverter`) | ⚠️ Minor gap |
| **JWKS fetching + caching** | ❌ | ✅ (with refresh interval) | 🔴 **Missing — no signature verification** |
| **GET request cache** (per-request core-call cache) | ❌ | ✅ (simplified) | ⚠️ Minor gap |
| **HTTP request interceptor** | ❌ | ✅ (`_querierInterceptor`) | ⚠️ Minor gap |
| **`HttpClient` injection** | ✅ (via DI) | ✅ (`SetHttpClient`) | ✅ Both have this |
| **Tenant-aware paths** (`{tenantId}/recipe/...`) | ❌ | ✅ | 🔴 **Missing — no multi-tenancy** |
| **User management APIs** (GetUsers, GetUserCount, DeleteUser) | ❌ | ✅ | 🔴 **Missing** |

---

## 8. What You Have That They Don't

| Feature | Your SDK | marklonquist |
|---|:---:|:---:|
| **TOTP recipe** | ✅ Full | ❌ |
| **MCP gateway** (5 tools) | ✅ | ❌ |
| **CDI 5.0 support** | ✅ | ❌ (CDI 3.1 only) |
| **Standard ASP.NET Core auth handler** | ✅ | ❌ (endpoint filters only) |
| **`IClaimsTransformation`** | ✅ | ❌ |
| **DI-based recipe injection** | ✅ | ❌ (static instances) |
| **`CancellationToken` throughout** | ✅ | ❌ |
| **NuGet package** | ✅ | ❌ |
| **Zero external dependencies** | ✅ | ❌ (4 NuGet packages) |
| **SignalR query token extraction** (`/hubs?access_token=`) | ✅ | ❌ |

**Evidence** — your TOTP recipe ([source](https://github.com/Hann1bal/SuperTokensNetSdk/blob/b9873050e8c8dde55f135c9c6b2b2d1c5aafb9fe/SuperTokensSDK.Net/Recipes/Totp/TotpRecipe.cs#L6-L52)):
```csharp
public class TotpRecipe
{
    public async Task<(string secret, string deviceName)> CreateDeviceAsync(...)
    public async Task<bool> VerifyDeviceAsync(...)
    public async Task<bool> VerifyCodeAsync(...)
    public async Task<List<TotpDevice>> ListDevicesAsync(...)
    public async Task<bool> RemoveDeviceAsync(...)
}
```

**Evidence** — your MCP gateway ([source](https://github.com/Hann1bal/SuperTokensNetSdk/blob/b9873050e8c8dde55f135c9c6b2b2d1c5aafb9fe/SuperTokensSDK.Net/Mcp/McpGateway.cs#L17-L92)):
```csharp
public IReadOnlyList<McpToolDefinition> GetToolDefinitions() => new List<McpToolDefinition>
{
    new() { Name = "create_user", ... },
    new() { Name = "verify_session", ... },
    new() { Name = "get_user_roles", ... },
    new() { Name = "assign_role", ... },
    new() { Name = "revoke_session", ... },
}.AsReadOnly();
```

---

## 9. Summary: Priority Gap Analysis

### 🔴 High Priority (Missing Entire Recipes)

| Gap | Impact | Effort |
|---|---|---|
| **Passwordless recipe** | Magic link / OTP login — increasingly popular auth method | Medium (~400 LOC based on their implementation) |
| **ThirdParty recipe + 13 providers** | Social login (Google, GitHub, Apple, etc.) — critical for most apps | High (~2,000+ LOC including providers) |
| **Multitenancy recipe** | Multi-tenant SaaS auth — enterprise requirement | Medium (~190 LOC) |
| **EmailVerification recipe** | Email verification flow — security best practice | Low (~106 LOC) |
| **Jwt recipe** | Create JWTs via Core — needed for API auth | Low (~127 LOC) |
| **Dashboard recipe** | SuperTokens admin dashboard — ops tooling | Medium (~300+ LOC) |

### 🔴 High Priority (Missing Core Features)

| Gap | Impact | Effort |
|---|---|---|
| **Session claim validation system** | Can't enforce role/permission checks at the session level — security gap | High (~500+ LOC: TypeSessionClaim, validators, PrimitiveClaim, PrimitiveArrayClaim, BooleanClaim) |
| **JWKS signature verification** | Your `VerifySession` decodes JWT without verifying signature — security risk | Medium (~100 LOC) |
| **Recipe API dispatching** (`/auth/*` routes) | Your SDK is client-only — frontend can't call SuperTokens APIs through your backend | High (requires middleware that dispatches all recipe APIs) |
| **Overridable recipe interfaces** | Can't customize recipe behavior (e.g., custom email delivery, custom sign-up logic) | Medium (refactor recipe classes to use interface + delegate pattern) |

### ⚠️ Medium Priority (Missing in Existing Recipes)

| Gap | Impact | Effort |
|---|---|---|
| **UserRoles**: 6 missing operations | Can't manage permissions, can't list users by role, can't delete roles | Low (~100 LOC) |
| **EmailPassword**: 5 missing operations | Can't get user by email/ID, can't update email/password, can't generate reset token | Low (~80 LOC) |
| **Session**: 5 missing operations | Can't list/revoke all sessions, can't update session data, can't regenerate tokens | Low (~100 LOC) |
| **EmailDelivery + SmsDelivery ingredients** | Can't send transactional emails/SMS (password reset, magic links, verification) | Medium (~300 LOC + MailKit/Twilio deps) |
| **User management APIs** | Can't list users, count users, or delete users | Low (~50 LOC) |

### ✅ Your Advantages (Keep These)

| Feature | Why It's Better |
|---|---|
| **CDI 5.0** | marklonquist is stuck on CDI 3.1 — you support the latest Core |
| **TOTP recipe** | They have no 2FA support at all |
| **MCP gateway** | Unique — enables AI agent integration |
| **Standard auth handler** | Works with `[Authorize]` attribute, ASP.NET Core identity pipeline |
| **DI-based** | Idiomatic .NET, testable, scoped lifetimes |
| **`CancellationToken`** | Proper async cancellation — they have none |
| **Zero dependencies** | Easier deployment, no version conflicts |
| **NuGet published** | Ready to consume |

---

## 10. Recommendation: Path to Feature Parity

Based on the gap analysis, here's a prioritized roadmap:

1. **JWKS signature verification** (security fix — your `VerifySession` is currently unsafe)
2. **Session claim validation system** (enables role/permission checks)
3. **Complete UserRoles** (6 missing operations — quick win)
4. **Complete EmailPassword** (5 missing operations — quick win)
5. **Complete Session** (5 missing operations — quick win)
6. **EmailVerification recipe** (small, self-contained)
7. **Jwt recipe** (small, self-contained)
8. **Multitenancy recipe** (enables tenant-aware paths across all recipes)
9. **Passwordless recipe** (high demand, medium effort)
10. **ThirdParty recipe + providers** (high demand, high effort — can start with Google/GitHub only)
11. **EmailDelivery + SmsDelivery ingredients** (needed for Passwordless/EmailVerification)
12. **Dashboard recipe** (lowest priority — ops tooling)