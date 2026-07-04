using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperTokensSDK.Net.Configuration;
using SuperTokensSDK.Net.Core.Models;

namespace SuperTokensSDK.Net.Core;

/// <summary>
/// HTTP client implementation for talking to SuperTokens Core via CDI.
/// </summary>
public class CoreApiClient : ICoreApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CoreApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public CoreApiClient(HttpClient httpClient, IOptions<SuperTokensOptions> options, ILogger<CoreApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var opts = options.Value;
        if (!string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("api-key", opts.ApiKey);
        }
    }

    public async Task<CreateSessionResponse> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        return await PostJsonAsync<CreateSessionRequest, CreateSessionResponse>("/recipe/session", request, cancellationToken);
    }

    public async Task<VerifySessionResponse> VerifySessionAsync(VerifySessionRequest request, CancellationToken cancellationToken = default)
    {
        return await PostJsonAsync<VerifySessionRequest, VerifySessionResponse>("/recipe/session/verify", request, cancellationToken);
    }

    public async Task<RefreshSessionCoreResponse> RefreshSessionAsync(RefreshSessionRequest request, CancellationToken cancellationToken = default)
    {
        return await PostJsonAsync<RefreshSessionRequest, RefreshSessionCoreResponse>("/recipe/session/refresh", request, cancellationToken);
    }

    public async Task<RevokeSessionResponse> RevokeSessionAsync(RevokeSessionRequest request, CancellationToken cancellationToken = default)
    {
        return await PostJsonAsync<RevokeSessionRequest, RevokeSessionResponse>("/recipe/session/revoke", request, cancellationToken);
    }

    public async Task<SignUpResponse> SignUpAsync(SignUpRequest request, CancellationToken cancellationToken = default)
    {
        return await PostJsonAsync<SignUpRequest, SignUpResponse>("/recipe/signup", request, cancellationToken);
    }

    public async Task<SignUpResponse> SignInAsync(SignUpRequest request, CancellationToken cancellationToken = default)
    {
        return await PostJsonAsync<SignUpRequest, SignUpResponse>("/recipe/signin", request, cancellationToken);
    }

    public async Task<StatusResponse> ResetPasswordAsync(PasswordResetRequest request, CancellationToken cancellationToken = default)
    {
        return await PostJsonAsync<PasswordResetRequest, StatusResponse>("/recipe/user/password/reset", request, cancellationToken);
    }

    public async Task<StatusResponse> AddUserRolesAsync(UserRolesRequest request, CancellationToken cancellationToken = default)
    {
        return await PutJsonAsync<UserRolesRequest, StatusResponse>("/recipe/user/roles", request, cancellationToken);
    }

    public async Task<UserRolesResponse> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["userId"] = userId;
        return await GetJsonAsync<UserRolesResponse>($"/recipe/user/roles?{query}", cancellationToken);
    }

    public async Task<StatusResponse> RemoveUserRolesAsync(UserRolesRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<UserRolesRequest, StatusResponse>(HttpMethod.Delete, "/recipe/user/roles", request, cancellationToken);
    }

    public async Task<RoleExistsResponse> DoesRoleExistAsync(string userId, string role, CancellationToken cancellationToken = default)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["userId"] = userId;
        query["role"] = role;
        return await GetJsonAsync<RoleExistsResponse>($"/recipe/user/role?{query}", cancellationToken);
    }

    public async Task<UserMetadataResponse> GetUserMetadataAsync(string userId, CancellationToken cancellationToken = default)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["userId"] = userId;
        return await GetJsonAsync<UserMetadataResponse>($"/recipe/user/metadata?{query}", cancellationToken);
    }

    public async Task<StatusResponse> UpdateUserMetadataAsync(UserMetadataUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return await PutJsonAsync<UserMetadataUpdateRequest, StatusResponse>("/recipe/user/metadata", request, cancellationToken);
    }

    private async Task<TResponse> GetJsonAsync<TResponse>(string path, CancellationToken cancellationToken) where TResponse : new()
    {
        _logger.LogDebug("CDI GET {Path}", path);
        var response = await _httpClient.GetAsync(path, cancellationToken);
        return await DeserializeResponseAsync<TResponse>(response, cancellationToken);
    }

    private async Task<TResponse> PostJsonAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken cancellationToken) where TResponse : new()
    {
        _logger.LogDebug("CDI POST {Path}", path);
        var response = await _httpClient.PostAsJsonAsync(path, request, JsonOptions, cancellationToken);
        return await DeserializeResponseAsync<TResponse>(response, cancellationToken);
    }

    private async Task<TResponse> PutJsonAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken cancellationToken) where TResponse : new()
    {
        _logger.LogDebug("CDI PUT {Path}", path);
        var response = await _httpClient.PutAsJsonAsync(path, request, JsonOptions, cancellationToken);
        return await DeserializeResponseAsync<TResponse>(response, cancellationToken);
    }

    private async Task<TResponse> SendJsonAsync<TRequest, TResponse>(HttpMethod method, string path, TRequest request, CancellationToken cancellationToken) where TResponse : new()
    {
        _logger.LogDebug("CDI {Method} {Path}", method, path);
        var content = JsonContent.Create(request, null, JsonOptions);
        var requestMessage = new HttpRequestMessage(method, path) { Content = content };
        var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        return await DeserializeResponseAsync<TResponse>(response, cancellationToken);
    }

    private static async Task<TResponse> DeserializeResponseAsync<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken) where TResponse : new()
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"SuperTokens Core returned {(int)response.StatusCode}: {error}", null, response.StatusCode);
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            return new TResponse();
        }

        var result = JsonSerializer.Deserialize<TResponse>(content, JsonOptions);
        return result ?? new TResponse();
    }
}
