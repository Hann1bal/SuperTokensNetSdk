using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SuperTokensSDK.Net.AspNetCore;
using SuperTokensSDK.Net.Configuration;

using Xunit;

#pragma warning disable ASPDEPR004, ASPDEPR008

namespace SuperTokensSDK.Net.Tests;

public class SuperTokensAuthenticationHandlerTests
{
    private static TestServer CreateServer()
    {
        return new TestServer(new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSuperTokens(options =>
                {
                    options.CoreUri = "http://localhost:3567";
                    options.AppName = "Test";
                });
                services.AddAuthentication("SuperTokens").AddSuperTokensAuthentication();
            })
            .Configure(app =>
            {
                app.UseAuthentication();
                app.Run(ctx =>
                {
                    if (ctx.User.Identity?.IsAuthenticated == true)
                    {
                        ctx.Response.StatusCode = 200;
                        return ctx.Response.WriteAsync(ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "ok");
                    }

                    ctx.Response.StatusCode = 401;
                    return Task.CompletedTask;
                });
            }));
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ValidBearerToken_AuthenticatesUser()
    {
        var jwt = TestJwtHelper.CreateJwt(new Dictionary<string, object>
        {
            ["sub"] = "u-auth",
            ["sessionHandle"] = "sh-auth"
        });

        var server = CreateServer();
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

        var response = await client.GetAsync("/");
        Assert.Equal(200, (int)response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("u-auth", body);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_NoToken_ReturnsUnauthorized()
    {
        var server = CreateServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("/");
        Assert.Equal(401, (int)response.StatusCode);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_InvalidBearerToken_ReturnsUnauthorized()
    {
        var server = CreateServer();
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "malformed-token");

        var response = await client.GetAsync("/");
        Assert.Equal(401, (int)response.StatusCode);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_CookieToken_AuthenticatesUser()
    {
        var jwt = TestJwtHelper.CreateJwt(new Dictionary<string, object>
        {
            ["sub"] = "u-cookie-auth",
            ["sessionHandle"] = "sh-cookie-auth"
        });

        var server = CreateServer();
        var client = server.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Cookie", $"sAccessToken={jwt}");

        var response = await client.SendAsync(request);
        Assert.Equal(200, (int)response.StatusCode);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_HubsQueryToken_AuthenticatesUser()
    {
        var jwt = TestJwtHelper.CreateJwt(new Dictionary<string, object>
        {
            ["sub"] = "u-hub-auth",
            ["sessionHandle"] = "sh-hub-auth"
        });

        var server = CreateServer();
        var client = server.CreateClient();

        var response = await client.GetAsync($"/hubs?access_token={jwt}");
        Assert.Equal(200, (int)response.StatusCode);
    }

    [Fact]
    public async Task AddSuperTokensAuthentication_RegistersHandlerScheme()
    {
        var server = CreateServer();
        var services = server.Services;
        var schemeProvider = services.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = await schemeProvider.GetSchemeAsync("SuperTokens");

        Assert.NotNull(scheme);
        Assert.Equal(typeof(SuperTokensAuthenticationHandler), scheme.HandlerType);
    }
}
