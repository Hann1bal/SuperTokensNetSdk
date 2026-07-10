using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using SuperTokensSDK.Net.AspNetCore;
using SuperTokensSDK.Net.Configuration;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;
using SuperTokensSDK.Net.Recipes.EmailPassword;
using SuperTokensSDK.Net.Recipes.Session;

using Xunit;

#pragma warning disable ASPDEPR004, ASPDEPR008

namespace SuperTokensSDK.Net.Tests;

public class SuperTokensApiMiddlewareTests
{
    private static TestServer CreateServer(
        Mock<ICoreApiClient> coreMock,
        Action<IServiceCollection>? configureServices = null)
    {
        return new TestServer(new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.Configure<SuperTokensOptions>(_ => { });
                services.AddScoped(_ => coreMock.Object);
                services.AddScoped<EmailPasswordRecipe>();
                services.AddScoped<SessionRecipe>();
                configureServices?.Invoke(services);
            })
            .Configure(app =>
            {
                app.UseSuperTokensApi();
                app.Run(async ctx =>
                {
                    ctx.Response.StatusCode = 200;
                    await ctx.Response.WriteAsync("next");
                });
            }));
    }

    private static HttpResponseMessage CreateProxyResponse(HttpStatusCode statusCode, string body)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body)
        };
    }

    [Fact]
    public async Task NonAuthRequest_PassesThroughToNextMiddleware()
    {
        var coreMock = new Mock<ICoreApiClient>();
        var server = CreateServer(coreMock);
        var client = server.CreateClient();

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("next", body);
        coreMock.Verify(c => c.ProxyToCoreAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnknownAuthRoute_PassesThroughToNextMiddleware()
    {
        var coreMock = new Mock<ICoreApiClient>();
        var server = CreateServer(coreMock);
        var client = server.CreateClient();

        var response = await client.GetAsync("/auth/unknown-route");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal("next", body);
        coreMock.Verify(c => c.ProxyToCoreAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SignUpRoute_WithFormFields_CreatesUserAndSessionAndSetsCookies()
    {
        var coreMock = new Mock<ICoreApiClient>();
        coreMock.Setup(c => c.SignUpAsync(
                It.Is<SignUpRequest>(r => r.Email == "a@b.com" && r.Password == "secret"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignUpResponse
            {
                Status = "OK",
                User = new UserResponse { Id = "user-id", Email = "a@b.com", TimeJoined = 1234567890 }
            });

        coreMock.Setup(c => c.CreateSessionAsync(
                It.Is<CreateSessionRequest>(r => r.UserId == "user-id"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateOrRefreshAPIResponse
            {
                Status = "OK",
                Session = new SessionStruct { Handle = "handle", UserId = "user-id", UserDataInJWT = new() },
                AccessToken = new TokenInfo { Token = "access-token", Expiry = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeMilliseconds() },
                RefreshToken = new TokenInfo { Token = "refresh-token", Expiry = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeMilliseconds() },
                AntiCsrfToken = "anti-csrf-token"
            });

        var server = CreateServer(coreMock);
        var client = server.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/signup", new
        {
            formFields = new[]
            {
                new { id = "email", value = "a@b.com" },
                new { id = "password", value = "secret" }
            }
        });
        var body = await response.Content.ReadAsStringAsync();
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"status\":\"OK\"", body);
        Assert.Contains("\"email\":\"a@b.com\"", body);
        Assert.Contains(cookies, c => c.StartsWith("sAccessToken=access-token"));
        Assert.Contains(cookies, c => c.StartsWith("sRefreshToken=refresh-token"));
        Assert.Contains(cookies, c => c.StartsWith("sAntiCsrf=anti-csrf-token"));
    }

    [Fact]
    public async Task SignInRoute_WithFormFields_CreatesSessionAndSetsCookies()
    {
        var coreMock = new Mock<ICoreApiClient>();
        coreMock.Setup(c => c.SignInAsync(
                It.Is<SignUpRequest>(r => r.Email == "a@b.com" && r.Password == "secret"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignUpResponse
            {
                Status = "OK",
                User = new UserResponse { Id = "user-id", Email = "a@b.com", TimeJoined = 1234567890 }
            });

        coreMock.Setup(c => c.CreateSessionAsync(
                It.Is<CreateSessionRequest>(r => r.UserId == "user-id"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateOrRefreshAPIResponse
            {
                Status = "OK",
                Session = new SessionStruct { Handle = "handle", UserId = "user-id", UserDataInJWT = new() },
                AccessToken = new TokenInfo { Token = "access-token", Expiry = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeMilliseconds() },
                RefreshToken = new TokenInfo { Token = "refresh-token", Expiry = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeMilliseconds() },
                AntiCsrfToken = "anti-csrf-token"
            });

        var server = CreateServer(coreMock);
        var client = server.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/signin", new
        {
            formFields = new[]
            {
                new { id = "email", value = "a@b.com" },
                new { id = "password", value = "secret" }
            }
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"status\":\"OK\"", body);
        Assert.Contains("\"email\":\"a@b.com\"", body);
    }

    [Fact]
    public async Task SignUpRoute_MissingFields_ReturnsFieldError()
    {
        var coreMock = new Mock<ICoreApiClient>();
        var server = CreateServer(coreMock);
        var client = server.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/signup", new { formFields = new[] { new { id = "email", value = "a@b.com" } } });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"status\":\"FIELD_ERROR\"", body);
        Assert.Contains("\"id\":\"password\"", body);
    }

    [Fact]
    public async Task SignInRoute_WrongCredentials_ReturnsWrongCredentialsError()
    {
        var coreMock = new Mock<ICoreApiClient>();
        coreMock.Setup(c => c.SignInAsync(
                It.IsAny<SignUpRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SuperTokensException("SuperTokens Core returned status WRONG_CREDENTIALS_ERROR: "));

        var server = CreateServer(coreMock);
        var client = server.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/signin", new
        {
            formFields = new[]
            {
                new { id = "email", value = "a@b.com" },
                new { id = "password", value = "wrong" }
            }
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("\"status\":\"WRONG_CREDENTIALS_ERROR\"", body);
    }

    [Fact]
    public async Task SignUpRoute_EmailAlreadyExists_ReturnsFieldError()
    {
        var coreMock = new Mock<ICoreApiClient>();
        coreMock.Setup(c => c.SignUpAsync(
                It.IsAny<SignUpRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SuperTokensException("SuperTokens Core returned status EMAIL_ALREADY_EXISTS_ERROR: "));

        var server = CreateServer(coreMock);
        var client = server.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/signup", new
        {
            formFields = new[]
            {
                new { id = "email", value = "a@b.com" },
                new { id = "password", value = "secret" }
            }
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"status\":\"FIELD_ERROR\"", body);
        Assert.Contains("already exists", body);
    }

    [Fact]
    public async Task SessionRefreshRoute_WithCookies_RefreshesSessionAndSetsCookies()
    {
        var coreMock = new Mock<ICoreApiClient>();
        coreMock.Setup(c => c.RefreshSessionAsync(
                It.Is<RefreshSessionRequest>(r => r.RefreshToken == "refresh-token" && r.AntiCsrfToken == "anti-csrf-token"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateOrRefreshAPIResponse
            {
                Status = "OK",
                Session = new SessionStruct { Handle = "new-handle", UserId = "user-id", UserDataInJWT = new() },
                AccessToken = new TokenInfo { Token = "new-access-token", Expiry = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeMilliseconds() },
                RefreshToken = new TokenInfo { Token = "new-refresh-token", Expiry = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeMilliseconds() },
                AntiCsrfToken = "new-anti-csrf-token"
            });

        var server = CreateServer(coreMock);
        var client = server.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/session/refresh");
        request.Headers.Add("Cookie", "sRefreshToken=refresh-token; sAntiCsrf=anti-csrf-token");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"status\":\"OK\"", body);
        Assert.Contains(cookies, c => c.StartsWith("sAccessToken=new-access-token"));
        Assert.Contains(cookies, c => c.StartsWith("sRefreshToken=new-refresh-token"));
        Assert.Contains(cookies, c => c.StartsWith("sAntiCsrf=new-anti-csrf-token"));
        Assert.True(response.Headers.Contains("front-token"));
        coreMock.Verify(c => c.ProxyToCoreAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SessionRefreshRoute_MissingRefreshToken_ReturnsUnauthorised()
    {
        var coreMock = new Mock<ICoreApiClient>();
        var server = CreateServer(coreMock);
        var client = server.CreateClient();

        var response = await client.PostAsync("/auth/session/refresh", null);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("\"status\":\"UNAUTHORISED\"", body);
        coreMock.Verify(c => c.RefreshSessionAsync(It.IsAny<RefreshSessionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SessionRefreshRoute_MissingAntiCsrfToken_ReturnsUnauthorised()
    {
        var coreMock = new Mock<ICoreApiClient>();
        var server = CreateServer(coreMock);
        var client = server.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/session/refresh");
        request.Headers.Add("Cookie", "sRefreshToken=refresh-token");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("\"status\":\"UNAUTHORISED\"", body);
        coreMock.Verify(c => c.RefreshSessionAsync(It.IsAny<RefreshSessionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EmailExistsRoute_ForwardsQueryString()
    {
        var coreMock = new Mock<ICoreApiClient>();
        coreMock.Setup(c => c.ProxyToCoreAsync(
                "GET",
                It.Is<string>(p => p.StartsWith("/recipe/signup/email/exists?")),
                null,
                "emailpassword",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProxyResponse(HttpStatusCode.OK, "{\"status\":\"OK\",\"exists\":true}"));

        var server = CreateServer(coreMock);
        var client = server.CreateClient();

        var response = await client.GetAsync("/auth/signup/email/exists?email=a@b.com");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("exists", body);
        coreMock.Verify(c => c.ProxyToCoreAsync(
            "GET",
            "/recipe/signup/email/exists?email=a@b.com",
            null,
            "emailpassword",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cors_PreflightRequest_Returns200WithHeaders()
    {
        var coreMock = new Mock<ICoreApiClient>();
        var server = CreateServer(coreMock);
        var client = server.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "/auth/signup");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("http://localhost:3000", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("true", response.Headers.GetValues("Access-Control-Allow-Credentials"));
        Assert.Contains("GET, POST, PUT, DELETE, OPTIONS", response.Headers.GetValues("Access-Control-Allow-Methods"));
        coreMock.Verify(c => c.ProxyToCoreAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cors_ActualRequest_AddsCorsHeaders()
    {
        var coreMock = new Mock<ICoreApiClient>();
        coreMock.Setup(c => c.SignUpAsync(
                It.IsAny<SignUpRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignUpResponse
            {
                Status = "OK",
                User = new UserResponse { Id = "user-id", Email = "a@b.com", TimeJoined = 1234567890 }
            });

        coreMock.Setup(c => c.CreateSessionAsync(
                It.IsAny<CreateSessionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateOrRefreshAPIResponse
            {
                Status = "OK",
                Session = new SessionStruct { Handle = "handle", UserId = "user-id", UserDataInJWT = new() },
                AccessToken = new TokenInfo { Token = "access-token", Expiry = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeMilliseconds() },
                RefreshToken = new TokenInfo { Token = "refresh-token", Expiry = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeMilliseconds() }
            });

        var server = CreateServer(coreMock);
        var client = server.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/signup")
        {
            Content = JsonContent.Create(new
            {
                formFields = new[]
                {
                    new { id = "email", value = "a@b.com" },
                    new { id = "password", value = "secret" }
                }
            })
        };
        request.Headers.Add("Origin", "http://localhost:3000");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("http://localhost:3000", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("true", response.Headers.GetValues("Access-Control-Allow-Credentials"));
        Assert.Contains("rid, fdi-version, anti-csrf, front-token", response.Headers.GetValues("Access-Control-Expose-Headers"));
    }

    [Fact]
    public async Task EmailVerification_GetAndPost_AreMappedSeparately()
    {
        var coreMock = new Mock<ICoreApiClient>();
        coreMock.Setup(c => c.ProxyToCoreAsync(
                "GET",
                It.Is<string>(p => p.StartsWith("/recipe/user/email/verify")),
                null,
                "emailverification",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProxyResponse(HttpStatusCode.OK, "{\"status\":\"OK\",\"isVerified\":true}"));

        coreMock.Setup(c => c.ProxyToCoreAsync(
                "POST",
                It.Is<string>(p => p.StartsWith("/recipe/user/email/verify")),
                It.IsAny<string>(),
                "emailverification",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProxyResponse(HttpStatusCode.OK, "{\"status\":\"OK\"}"));

        var server = CreateServer(coreMock);
        var client = server.CreateClient();

        var getResponse = await client.GetAsync("/auth/user/email/verify?userId=u");
        var postResponse = await client.PostAsJsonAsync("/auth/user/email/verify", new { token = "t" });

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        coreMock.Verify(c => c.ProxyToCoreAsync("GET", It.Is<string>(p => p.StartsWith("/recipe/user/email/verify")), null, "emailverification", It.IsAny<CancellationToken>()), Times.Once);
        coreMock.Verify(c => c.ProxyToCoreAsync("POST", It.Is<string>(p => p.StartsWith("/recipe/user/email/verify")), It.IsAny<string>(), "emailverification", It.IsAny<CancellationToken>()), Times.Once);
    }
}
