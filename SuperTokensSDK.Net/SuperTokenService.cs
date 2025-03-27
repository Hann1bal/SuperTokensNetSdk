using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SuperTokensSDK.Net.DataClasses;
using SuperTokensSDK.Net.Middleware;

namespace SuperTokensSDK.Net;

public class SuperTokenService : ISuperTokenService
{
    private readonly IHttpClientFactory _httpClient;
    private readonly SuperTokenOptions _superTokenOptions;
    public SuperTokenService(IOptions<SuperTokenOptions> options, IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory;
        _superTokenOptions = options.Value;
    }

    public async Task<string> CreateSession(string userId)
    {
        var httpClient = _httpClient.CreateClient();
        var response = await httpClient.PostAsJsonAsync($"{_superTokenOptions.AuthURI}/session", new { userId });
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var session = JsonSerializer.Deserialize<SuperTokenSession>(content);
        if (session == null || session.AccessToken == null) throw new Exception("Something went wrong with SuperTokenBackend or missing AccessToken in response");
        return session.AccessToken;
    }

    public async Task<TokenValidationResult> ValidateSession(string AccessToken)
    {
        var httpClient = _httpClient.CreateClient();
        var response = await httpClient.PostAsJsonAsync($"{_superTokenOptions.AuthURI}/session/validate", new { AccessToken });
        if (!response.IsSuccessStatusCode)
        {
            return TokenValidationResult.Failed("AccessToken is invalid or missing or expired");
        }
        var content = await response.Content.ReadFromJsonAsync<RefreshSessionResponse>();
        if (content == null || content.UserId == null) return TokenValidationResult.Failed("Broken message or missing UserId in response");
        return TokenValidationResult.Success(content.UserId, content.Claims);
    }
    public async Task<TokenValidationResult> ValidateRefreshToken(string AccessToken)
    {
        var httpClient = _httpClient.CreateClient();
        var response = await httpClient.PostAsJsonAsync($"{_superTokenOptions.AuthURI}/session/validate", new { AccessToken });
        if (!response.IsSuccessStatusCode)
        {
            return TokenValidationResult.Failed("AccessToken is invalid or missing or expired");
        }
        var content = await response.Content.ReadFromJsonAsync<RefreshSessionResponse>();
        if (content == null || content.UserId == null) return TokenValidationResult.Failed("Broken message or missing UserId in response");
        return TokenValidationResult.Success(content.UserId, content.Claims);
    }
}
public static class SuperTokenServiceExtentions
{
    public static IServiceCollection UseSupetToken(this IServiceCollection services, Action<SuperTokenOptions> options)
    {
        services.Configure<SuperTokenOptions>(options);
        services.AddHttpClient<ISuperTokenService, SuperTokenService>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<SuperTokenOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.AuthURI)) throw new Exception("Authification Provider URI is not set");
            client.BaseAddress = new Uri(options.AuthURI);
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<SuperTokensSessionMiddleware>();
        return services;
    }
}