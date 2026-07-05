using System.Reflection;
using Microsoft.IdentityModel.Tokens;
using SuperTokensSDK.Net.Core;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class JwksClientTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly HttpClient _httpClient;

    public JwksClientTests()
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

    private static string ValidJwksJson()
    {
        var (_, jwksJson) = TestJwtHelper.CreateSignedJwt(new Dictionary<string, object> { ["sub"] = "u" });
        return jwksJson;
    }

    private static void SetLastFetch(JwksClient client, DateTime value)
    {
        var field = typeof(JwksClient).GetField("_lastFetch", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        field!.SetValue(client, value);
    }

    [Fact]
    public async Task GetKeysAsync_ReturnsKeys_FromCore()
    {
        var jwksJson = ValidJwksJson();
        _server.Given(Request.Create().WithPath("/.well-known/jwks.json").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(jwksJson));

        var client = new JwksClient(_httpClient);
        var result = await client.GetKeysAsync(_server.Url!);

        Assert.NotNull(result);
        Assert.NotEmpty(result!.Keys);
    }

    [Fact]
    public async Task GetKeysAsync_CachesKeys_ForRefreshInterval()
    {
        var jwksJson = ValidJwksJson();
        _server.Given(Request.Create().WithPath("/.well-known/jwks.json").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(jwksJson));

        var client = new JwksClient(_httpClient);
        var first = await client.GetKeysAsync(_server.Url!);
        var second = await client.GetKeysAsync(_server.Url!);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Same(first, second);

        var fetchCount = _server.LogEntries.Count(le => le.RequestMessage.Path == "/.well-known/jwks.json");
        Assert.Equal(1, fetchCount);
    }

    [Fact]
    public async Task GetKeysAsync_ReturnsNull_WhenCoreReturnsError()
    {
        _server.Given(Request.Create().WithPath("/.well-known/jwks.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(500));

        var client = new JwksClient(_httpClient);
        var result = await client.GetKeysAsync(_server.Url!);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetKeysAsync_ReturnsCachedKeys_WhenFetchFails()
    {
        var jwksJson = ValidJwksJson();

        // First call succeeds and caches.
        _server.Given(Request.Create().WithPath("/.well-known/jwks.json").UsingGet())
            .InScenario("negative-cache").WillSetStateTo("cached")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(jwksJson));

        // Second call (after cache is stale) fails.
        _server.Given(Request.Create().WithPath("/.well-known/jwks.json").UsingGet())
            .InScenario("negative-cache").WhenStateIs("cached")
            .RespondWith(Response.Create().WithStatusCode(500));

        var client = new JwksClient(_httpClient);
        var first = await client.GetKeysAsync(_server.Url!);
        Assert.NotNull(first);

        // Force the cache to be stale so the next call re-fetches.
        SetLastFetch(client, DateTime.UtcNow.AddHours(-2));

        var second = await client.GetKeysAsync(_server.Url!);
        Assert.NotNull(second);
        Assert.Same(first, second);
    }
}
