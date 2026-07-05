using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SuperTokensSDK.Net.AspNetCore;
using SuperTokensSDK.Net.Core;

using Xunit;

#pragma warning disable ASPDEPR004, ASPDEPR008

namespace SuperTokensSDK.Net.Tests;

public class SuperTokensApiMiddlewareTests
{
    private static TestServer CreateServer(Mock<ICoreApiClient> coreMock)
    {
        return new TestServer(new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddScoped(_ => coreMock.Object);
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
    public async Task SignUpRoute_ProxiesToRecipeSignup()
    {
        var coreMock = new Mock<ICoreApiClient>();
        coreMock.Setup(c => c.ProxyToCoreAsync(
                "POST",
                "/recipe/signup",
                It.IsAny<string>(),
                "emailpassword",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProxyResponse(HttpStatusCode.OK, "{\"status\":\"OK\"}"));

        var server = CreateServer(coreMock);
        var client = server.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/signup", new { email = "a@b.com", password = "secret" });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("OK", body);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        coreMock.Verify(c => c.ProxyToCoreAsync(
            "POST",
            "/recipe/signup",
            It.Is<string?>(b => b != null && b.Contains("a@b.com") && b.Contains("secret")),
            "emailpassword",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SessionRefreshRoute_ProxiesToRecipeSessionRefresh()
    {
        var coreMock = new Mock<ICoreApiClient>();
        coreMock.Setup(c => c.ProxyToCoreAsync(
                "POST",
                "/recipe/session/refresh",
                It.IsAny<string>(),
                "session",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProxyResponse(HttpStatusCode.OK, "{\"status\":\"OK\"}"));

        var server = CreateServer(coreMock);
        var client = server.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/session/refresh", new { refreshToken = "rt" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        coreMock.Verify(c => c.ProxyToCoreAsync(
            "POST",
            "/recipe/session/refresh",
            It.IsAny<string>(),
            "session",
            It.IsAny<CancellationToken>()), Times.Once);
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
        coreMock.Setup(c => c.ProxyToCoreAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProxyResponse(HttpStatusCode.OK, "{\"status\":\"OK\"}"));

        var server = CreateServer(coreMock);
        var client = server.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/signup")
        {
            Content = JsonContent.Create(new { email = "a@b.com", password = "secret" })
        };
        request.Headers.Add("Origin", "http://localhost:3000");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("http://localhost:3000", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("true", response.Headers.GetValues("Access-Control-Allow-Credentials"));
        Assert.Contains("rid, fdi-version, anti-csrf", response.Headers.GetValues("Access-Control-Expose-Headers"));
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
