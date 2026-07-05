using System.Security.Claims;
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

using Xunit;

#pragma warning disable ASPDEPR004, ASPDEPR008

namespace SuperTokensSDK.Net.Tests;

public class SuperTokensMiddlewareTests
{
    private static TestServer CreateServer(Mock<ICoreApiClient> coreMock)
    {
        return new TestServer(new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.Configure<SuperTokensOptions>(options =>
                {
                    options.AccessTokenCookieName = "sAccessToken";
                    options.AntiCsrfCookieName = "sAntiCsrf";
                    options.EnableAntiCsrf = false;
                });
                services.AddScoped(_ => coreMock.Object);
            })
            .Configure(app =>
            {
                app.UseSuperTokensMiddleware();
                app.Run(async ctx =>
                {
                    var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                    await ctx.Response.WriteAsync(userId);
                });
            }));
    }

    [Fact]
    public async Task Middleware_NoToken_ContinuesAnonymously()
    {
        var coreMock = new Mock<ICoreApiClient>();
        var server = CreateServer(coreMock);
        var client = server.CreateClient();

        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal("anonymous", body);
        coreMock.Verify(c => c.VerifySessionAsync(It.IsAny<VerifySessionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Middleware_ValidBearerToken_SetsUser()
    {
        var coreMock = new Mock<ICoreApiClient>();
        var jwt = TestJwtHelper.CreateJwt(new Dictionary<string, object>
        {
            ["sub"] = "u-mw",
            ["sessionHandle"] = "sh-mw"
        });
        coreMock.Setup(c => c.VerifySessionAsync(It.Is<VerifySessionRequest>(r => r.AccessToken == jwt), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSessionResponse
            {
                Status = "OK",
                Session = new SessionStruct { Handle = "sh-mw", UserId = "u-mw", UserDataInJWT = new() }
            });

        var server = CreateServer(coreMock);
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal("u-mw", body);
    }

    [Fact]
    public async Task Middleware_InvalidBearerToken_ContinuesAnonymously()
    {
        var coreMock = new Mock<ICoreApiClient>();
        coreMock.Setup(c => c.VerifySessionAsync(It.IsAny<VerifySessionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedException("invalid"));

        var server = CreateServer(coreMock);
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "bad-token");

        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal("anonymous", body);
    }

    [Fact]
    public async Task Middleware_CookieToken_SetsUser()
    {
        var coreMock = new Mock<ICoreApiClient>();
        var jwt = TestJwtHelper.CreateJwt(new Dictionary<string, object>
        {
            ["sub"] = "u-cookie",
            ["sessionHandle"] = "sh-cookie"
        });
        coreMock.Setup(c => c.VerifySessionAsync(It.Is<VerifySessionRequest>(r => r.AccessToken == jwt), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSessionResponse
            {
                Status = "OK",
                Session = new SessionStruct { Handle = "sh-cookie", UserId = "u-cookie", UserDataInJWT = new() }
            });

        var server = CreateServer(coreMock);
        var client = server.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Cookie", $"sAccessToken={jwt}");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal("u-cookie", body);
    }

    [Fact]
    public async Task Middleware_HubsQueryToken_SetsUser()
    {
        var coreMock = new Mock<ICoreApiClient>();
        var jwt = TestJwtHelper.CreateJwt(new Dictionary<string, object>
        {
            ["sub"] = "u-hub",
            ["sessionHandle"] = "sh-hub"
        });
        coreMock.Setup(c => c.VerifySessionAsync(It.Is<VerifySessionRequest>(r => r.AccessToken == jwt), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSessionResponse
            {
                Status = "OK",
                Session = new SessionStruct { Handle = "sh-hub", UserId = "u-hub", UserDataInJWT = new() }
            });

        var server = CreateServer(coreMock);
        var client = server.CreateClient();

        var response = await client.GetAsync($"/hubs?access_token={jwt}");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal("u-hub", body);
    }
}
