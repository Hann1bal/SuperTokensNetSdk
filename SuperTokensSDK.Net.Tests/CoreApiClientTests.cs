using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SuperTokensSDK.Net.Configuration;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class CoreApiClientTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly HttpClient _httpClient;

    public CoreApiClientTests()
    {
        _server = WireMockServer.Start();
        _httpClient = new HttpClient();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _server.Dispose();
        GC.SuppressFinalize(this);
    }

    private CoreApiClient CreateClient(SuperTokensOptions? options = null, JwksClient? jwksClient = null)
    {
        var opts = options ?? new SuperTokensOptions { CoreUri = _server.Url };
        return jwksClient != null
            ? new CoreApiClient(_httpClient, Options.Create(opts), NullLogger<CoreApiClient>.Instance, jwksClient)
            : new CoreApiClient(_httpClient, Options.Create(opts), NullLogger<CoreApiClient>.Instance);
    }

    private void StubApiVersion()
    {
        _server.Given(Request.Create().WithPath(Constants.Paths.ApiVersion).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { versions = new[] { "5.0" } }));
    }

    private static string Json(object value) => System.Text.Json.JsonSerializer.Serialize(value, new System.Text.Json.JsonSerializerOptions
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    });

    [Fact]
    public async Task CreateSessionAsync_PostsToRecipeSession_WithSessionRid()
    {
        StubApiVersion();
        var response = new CreateOrRefreshAPIResponse
        {
            Status = "OK",
            Session = new SessionStruct { Handle = "sh1", UserId = "u1", UserDataInJWT = new() },
            AccessToken = new TokenInfo { Token = "at", Expiry = 1893456000000 }
        };
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeSession).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(response));

        var client = CreateClient();
        var result = await client.CreateSessionAsync(new CreateSessionRequest { UserId = "u1" });

        Assert.Equal("OK", result.Status);
        Assert.Equal("u1", result.Session.UserId);

        var request = _server.LogEntries.Last(le => le.RequestMessage.Path == Constants.Paths.RecipeSession).RequestMessage;
        Assert.NotNull(request);
        Assert.NotNull(request.Headers);
        Assert.Equal("POST", request.Method);
        Assert.True(request.Headers.ContainsKey("rid"));
        Assert.Contains("session", request.Headers["rid"]!);
        Assert.True(request.Headers.ContainsKey("cdi-version"));
    }

    [Fact]
    public async Task SignUpAsync_PostsToRecipeSignup_WithEmailPasswordRid()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeSignUp).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new SignUpResponse { Status = "OK", User = new UserResponse { Id = "u1" } }));

        var client = CreateClient();
        var result = await client.SignUpAsync(new SignUpRequest { Email = "a@b.com", Password = "p" });

        Assert.Equal("u1", result.User?.Id);
        var request = _server.LogEntries.Last(le => le.RequestMessage.Path == Constants.Paths.RecipeSignUp).RequestMessage;
        Assert.NotNull(request);
        Assert.NotNull(request.Headers);
        Assert.Contains("emailpassword", request.Headers["rid"]!);
    }

    [Fact]
    public async Task SignInAsync_PostsToRecipeSignin()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeSignIn).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new SignUpResponse { Status = "OK", User = new UserResponse { Id = "u2" } }));

        var client = CreateClient();
        var result = await client.SignInAsync(new SignUpRequest { Email = "c@d.com", Password = "p" });

        Assert.Equal("u2", result.User?.Id);
    }

    [Fact]
    public async Task ResetPasswordAsync_PostsToPasswordReset()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeUserPasswordReset).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new StatusResponse { Status = "OK" }));

        var client = CreateClient();
        var result = await client.ResetPasswordAsync(new PasswordResetRequest { UserId = "u3", NewPassword = "p" });

        Assert.Equal("OK", result.Status);
    }

    [Fact]
    public async Task RefreshSessionAsync_PostsToSessionRefresh()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeSessionRefresh).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new CreateOrRefreshAPIResponse
            {
                Status = "OK",
                Session = new SessionStruct { Handle = "sh2", UserId = "u", UserDataInJWT = new() }
            }));

        var client = CreateClient();
        var result = await client.RefreshSessionAsync(new RefreshSessionRequest { RefreshToken = "rt" });

        Assert.Equal("sh2", result.Session.Handle);
    }

    [Fact]
    public async Task RevokeSessionAsync_PostsToSessionRevoke()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeSessionRevoke).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new RevokeSessionResponse { Status = "OK" }));

        var client = CreateClient();
        var result = await client.RevokeSessionAsync(new RevokeSessionRequest { SessionHandle = "sh3" });

        Assert.Equal("OK", result.Status);
    }

    [Fact]
    public async Task AddUserRolesAsync_PutsToUserRoles()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeUserRoles).UsingPut())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new StatusResponse { Status = "OK" }));

        var client = CreateClient();
        var result = await client.AddUserRolesAsync(new UserRolesRequest { UserId = "u4", Roles = new List<string> { "admin" } });

        Assert.Equal("OK", result.Status);
    }

    [Fact]
    public async Task GetUserRolesAsync_GetsWithQuery()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeUserRoles).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new UserRolesResponse { Status = "OK", Roles = new List<string> { "admin" } }));

        var client = CreateClient();
        var result = await client.GetUserRolesAsync("u5");

        Assert.Equal("admin", result.Roles.Single());
        var request = _server.LogEntries.Last(le => le.RequestMessage.Path == Constants.Paths.RecipeUserRoles && le.RequestMessage.Method == "GET").RequestMessage;
        Assert.Contains("userId=u5", request.AbsoluteUrl);
    }

    [Fact]
    public async Task RemoveUserRolesAsync_DeletesUserRoles()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeUserRoles).UsingDelete())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new StatusResponse { Status = "OK" }));

        var client = CreateClient();
        var result = await client.RemoveUserRolesAsync(new UserRolesRequest { UserId = "u6", Roles = new List<string> { "admin" } });

        Assert.Equal("OK", result.Status);
    }

    [Fact]
    public async Task DoesRoleExistAsync_GetsRoleWithQuery()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeUserRole).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new RoleExistsResponse { Status = "OK", DoesRoleExist = true }));

        var client = CreateClient();
        var result = await client.DoesRoleExistAsync("u7", "admin");

        Assert.True(result.DoesRoleExist);
        var request = _server.LogEntries.Last(le => le.RequestMessage.Path == Constants.Paths.RecipeUserRole).RequestMessage;
        Assert.Contains("userId=u7", request.AbsoluteUrl);
        Assert.Contains("role=admin", request.AbsoluteUrl);
    }

    [Fact]
    public async Task GetUserMetadataAsync_GetsWithQuery()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeUserMetadata).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new UserMetadataResponse { Status = "OK", Metadata = new Dictionary<string, object> { ["theme"] = "dark" } }));

        var client = CreateClient();
        var result = await client.GetUserMetadataAsync("u8");

        Assert.Equal("dark", result.Metadata!["theme"].ToString());
    }

    [Fact]
    public async Task UpdateUserMetadataAsync_PutsToUserMetadata()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeUserMetadata).UsingPut())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new StatusResponse { Status = "OK" }));

        var client = CreateClient();
        var result = await client.UpdateUserMetadataAsync(new UserMetadataUpdateRequest { UserId = "u9", MetadataUpdate = new Dictionary<string, object> { ["theme"] = "light" } });

        Assert.Equal("OK", result.Status);
    }

    [Fact]
    public async Task VerifySessionAsync_CallsCoreVerifyEndpoint()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeSessionVerify).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                status = "OK",
                session = new
                {
                    handle = "test-handle",
                    userId = "user123",
                    userDataInJWT = new { role = "admin" },
                    expiryTime = 1234567890L,
                    tenantId = "public"
                }
            }));

        var client = CreateClient();
        var result = await client.VerifySessionAsync(new VerifySessionRequest { AccessToken = "some-jwt" });

        Assert.Equal("OK", result.Status);
        Assert.Equal("test-handle", result.Session!.Handle);
        Assert.Equal("user123", result.Session.UserId);
        Assert.Equal("admin", result.Session.UserDataInJWT["role"].ToString());
        Assert.Equal(1234567890L, result.Session.ExpiryTime);
        Assert.Equal("public", result.Session.TenantId);
    }

    [Fact]
    public async Task VerifySessionAsync_ThrowsUnauthorizedOnExpiredToken()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeSessionVerify).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                status = "UNAUTHORISED",
                message = "Access token has expired"
            }));

        var client = CreateClient();
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            client.VerifySessionAsync(new VerifySessionRequest { AccessToken = "expired-jwt" }));
        Assert.Contains("expired", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifySessionAsync_ThrowsUnauthorizedOnMissingUserId()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeSessionVerify).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                status = "UNAUTHORISED",
                message = "Access token does not contain a valid userId"
            }));

        var client = CreateClient();
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            client.VerifySessionAsync(new VerifySessionRequest { AccessToken = "no-sub-jwt" }));
        Assert.Contains("userId", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifySessionAsync_ThrowsUnauthorizedOnMalformedToken()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeSessionVerify).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                status = "UNAUTHORISED",
                message = "Malformed access token"
            }));

        var client = CreateClient();
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            client.VerifySessionAsync(new VerifySessionRequest { AccessToken = "not-a-jwt" }));
        Assert.Contains("Malformed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifySessionAsync_ReturnsUserDataFromCore()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeSessionVerify).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                status = "OK",
                session = new
                {
                    handle = "sh-custom",
                    userId = "user-custom",
                    userDataInJWT = new { role = "admin", tenant = "org1", custom = "keep" },
                    expiryTime = 9999999999L,
                    tenantId = "public"
                }
            }));

        var client = CreateClient();
        var result = await client.VerifySessionAsync(new VerifySessionRequest { AccessToken = "jwt-with-claims" });

        Assert.Equal("user-custom", result.Session!.UserId);
        Assert.Equal("admin", result.Session.UserDataInJWT["role"].ToString());
        Assert.Equal("org1", result.Session.UserDataInJWT["tenant"].ToString());
        Assert.Equal("keep", result.Session.UserDataInJWT["custom"].ToString());
    }

    [Fact]
    public async Task VerifySessionAsync_SendsAntiCsrfTokenWhenPresent()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeSessionVerify).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                status = "OK",
                session = new
                {
                    handle = "sh-csrf",
                    userId = "u-csrf",
                    userDataInJWT = new { },
                    expiryTime = 1234567890L,
                    tenantId = "public"
                }
            }));

        var client = CreateClient();
        await client.VerifySessionAsync(new VerifySessionRequest
        {
            AccessToken = "jwt",
            AntiCsrfToken = "csrf-abc",
            DoAntiCsrfCheck = true,
            EnableAntiCsrf = true
        });

        var request = _server.LogEntries.Last(le => le.RequestMessage.Path == Constants.Paths.RecipeSessionVerify).RequestMessage;
        Assert.NotNull(request.Body);
        using var body = System.Text.Json.JsonDocument.Parse(request.Body);
        Assert.Equal("csrf-abc", body.RootElement.GetProperty("antiCsrfToken").GetString());
        Assert.True(body.RootElement.GetProperty("doAntiCsrfCheck").GetBoolean());
        Assert.True(body.RootElement.GetProperty("enableAntiCsrf").GetBoolean());
    }

    [Fact]
    public async Task VerifySessionAsync_ThrowsUnauthorizedOnInvalidSignature()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeSessionVerify).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                status = "UNAUTHORISED",
                message = "Access token signature verification failed"
            }));

        var client = CreateClient();
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            client.VerifySessionAsync(new VerifySessionRequest { AccessToken = "bad-sig-jwt" }));
        Assert.Contains("signature", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CdiVersionNegotiation_CallsApiVersion_ThenCachesVersion()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeSession).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new CreateOrRefreshAPIResponse
            {
                Status = "OK",
                Session = new SessionStruct { Handle = "sh", UserId = "u", UserDataInJWT = new() }
            }));

        var client = CreateClient();
        await client.CreateSessionAsync(new CreateSessionRequest { UserId = "u" });
        await client.CreateSessionAsync(new CreateSessionRequest { UserId = "u" });

        var apiVersionCount = _server.LogEntries.Count(le => le.RequestMessage.Path == Constants.Paths.ApiVersion);
        var sessionCount = _server.LogEntries.Count(le => le.RequestMessage.Path == Constants.Paths.RecipeSession);

        Assert.Equal(1, apiVersionCount);
        Assert.Equal(2, sessionCount);
    }

    [Fact]
    public async Task RateLimitRetry_RetriesUpToMaxTimes()
    {
        StubApiVersion();

        const string scenario = "rate-limit";
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeSession).UsingPost())
            .InScenario(scenario).WillSetStateTo("1")
            .RespondWith(Response.Create().WithStatusCode(429));

        for (var i = 1; i <= 4; i++)
        {
            var next = (i + 1).ToString();
            _server.Given(Request.Create().WithPath(Constants.Paths.RecipeSession).UsingPost())
                .InScenario(scenario).WhenStateIs(i.ToString()).WillSetStateTo(next)
                .RespondWith(Response.Create().WithStatusCode(429));
        }

        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeSession).UsingPost())
            .InScenario(scenario).WhenStateIs("5")
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new CreateOrRefreshAPIResponse
            {
                Status = "OK",
                Session = new SessionStruct { Handle = "sh", UserId = "u", UserDataInJWT = new() }
            }));

        var client = CreateClient();
        var result = await client.CreateSessionAsync(new CreateSessionRequest { UserId = "u" });

        Assert.Equal("OK", result.Status);
        var sessionAttempts = _server.LogEntries.Count(le => le.RequestMessage.Path == Constants.Paths.RecipeSession && le.RequestMessage.Method == "POST");
        Assert.Equal(6, sessionAttempts);
    }

    [Fact]
    public async Task ErrorMapping_Unauthorized_ThrowsUnauthorizedException()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeSession).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { status = "UNAUTHORISED", message = "no session" }));

        var client = CreateClient();
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => client.CreateSessionAsync(new CreateSessionRequest { UserId = "u" }));
        Assert.Contains("no session", ex.Message);
    }

    [Fact]
    public async Task ErrorMapping_TryRefreshToken_ThrowsTryRefreshTokenException()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeSession).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { status = "TRY_REFRESH_TOKEN", message = "refresh" }));

        var client = CreateClient();
        var ex = await Assert.ThrowsAsync<TryRefreshTokenException>(() => client.CreateSessionAsync(new CreateSessionRequest { UserId = "u" }));
        Assert.Equal("refresh", ex.Message);
    }

    [Fact]
    public async Task ErrorMapping_TokenTheftDetected_ThrowsTokenTheftDetectedException()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeSession).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                status = "TOKEN_THEFT_DETECTED",
                payload = new { sessionHandle = "sh-theft", userId = "u-theft" }
            }));

        var client = CreateClient();
        var ex = await Assert.ThrowsAsync<TokenTheftDetectedException>(() => client.CreateSessionAsync(new CreateSessionRequest { UserId = "u" }));
        Assert.Equal("sh-theft", ex.SessionHandle);
        Assert.Equal("u-theft", ex.UserId);
        Assert.Equal("Token theft detected.", ex.Message);
    }

    [Fact]
    public async Task ErrorMapping_InvalidClaims_ThrowsInvalidClaimException()
    {
        StubApiVersion();
        _server.Given(Request.Create().WithPath(Constants.Paths.RecipeSession).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                status = "INVALID_CLAIMS",
                invalidClaims = new[] { new { id = "email", reason = "missing" } }
            }));

        var client = CreateClient();
        var ex = await Assert.ThrowsAsync<InvalidClaimException>(() => client.CreateSessionAsync(new CreateSessionRequest { UserId = "u" }));
        Assert.Single(ex.InvalidClaims);
        Assert.Equal("email", ex.InvalidClaims[0].Id);
        Assert.Equal("missing", ex.InvalidClaims[0].Reason);
    }

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CoreApiClient(
            null!,
            Options.Create(new SuperTokensOptions { CoreUri = "http://localhost" }),
            NullLogger<CoreApiClient>.Instance));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CoreApiClient(
            new HttpClient(),
            null!,
            NullLogger<CoreApiClient>.Instance));
    }

    [Fact]
    public void Constructor_NullCoreUri_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => new CoreApiClient(
            new HttpClient(),
            Options.Create(new SuperTokensOptions()),
            NullLogger<CoreApiClient>.Instance));
        Assert.Contains("CoreUri", ex.Message);
    }
}
