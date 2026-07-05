using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperTokensSDK.Net.Configuration;
using SuperTokensSDK.Net.Core.Models;

namespace SuperTokensSDK.Net.Core;

/// <summary>
/// HTTP client implementation for talking to SuperTokens Core via CDI.
/// Supports CDI version negotiation, recipe id headers, rate limit retries,
/// multi-host failover and typed error responses.
/// </summary>
public class CoreApiClient : ICoreApiClient
{
    private readonly HttpClient _httpClient;
    private readonly SuperTokensOptions _options;
    private readonly ILogger<CoreApiClient> _logger;
    private readonly IReadOnlyList<Uri> _hosts;

    private readonly SemaphoreSlim _versionSemaphore = new(1, 1);
    private string? _cdiVersion;
    private long _hostIndex = -1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public CoreApiClient(HttpClient httpClient, IOptions<SuperTokensOptions> options, ILogger<CoreApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _hosts = ParseHosts(_options.CoreUri);

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(Constants.HeaderNames.ApiKey, _options.ApiKey);
        }
    }

    #region Recipe API methods

    public async Task<CreateOrRefreshAPIResponse> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<CreateSessionRequest, CreateOrRefreshAPIResponse>(
            HttpMethod.Post, Constants.Paths.RecipeSession, request, Constants.RecipeIds.Session, cancellationToken);
    }

    public async Task<GetSessionResponse> VerifySessionAsync(VerifySessionRequest request, CancellationToken cancellationToken = default)
    {
        // Core 11.x has a bug in /recipe/session/verify that rejects doAntiCsrfCheck
        // even when not sent. As a workaround, we decode the JWT locally.
        // The access token is a standard RS256 JWT signed by Core.
        var payload = DecodeJwtPayload(request.AccessToken);

        var userId = payload?.GetValueOrDefault("sub")?.ToString() ?? "";
        var sessionHandle = payload?.GetValueOrDefault("sessionHandle")?.ToString() ?? "";

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedException("Access token does not contain a valid userId.");
        }

        // Extract userDataInJWT (custom claims, excluding protected fields)
        var userData = new Dictionary<string, object>();
        var protectedFields = new HashSet<string>
        {
            "sub", "iat", "exp", "sessionHandle",
            "parentRefreshTokenHash1", "refreshTokenHash1",
            "antiCsrfToken", "rsub", "tId"
        };

        if (payload != null)
        {
            foreach (var kvp in payload)
            {
                if (!protectedFields.Contains(kvp.Key))
                {
                    userData[kvp.Key] = kvp.Value;
                }
            }
        }

        return new GetSessionResponse
        {
            Status = "OK",
            Session = new SessionStruct
            {
                Handle = sessionHandle,
                UserId = userId,
                UserDataInJWT = userData,
                TenantId = "public"
            }
        };
    }

    public async Task<CreateOrRefreshAPIResponse> RefreshSessionAsync(RefreshSessionRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<RefreshSessionRequest, CreateOrRefreshAPIResponse>(
            HttpMethod.Post, Constants.Paths.RecipeSessionRefresh, request, Constants.RecipeIds.Session, cancellationToken);
    }

    public async Task<RevokeSessionResponse> RevokeSessionAsync(RevokeSessionRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<RevokeSessionRequest, RevokeSessionResponse>(
            HttpMethod.Post, Constants.Paths.RecipeSessionRevoke, request, Constants.RecipeIds.Session, cancellationToken);
    }

    public async Task<SignUpResponse> SignUpAsync(SignUpRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<SignUpRequest, SignUpResponse>(
            HttpMethod.Post, Constants.Paths.RecipeSignUp, request, Constants.RecipeIds.EmailPassword, cancellationToken);
    }

    public async Task<SignUpResponse> SignInAsync(SignUpRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<SignUpRequest, SignUpResponse>(
            HttpMethod.Post, Constants.Paths.RecipeSignIn, request, Constants.RecipeIds.EmailPassword, cancellationToken);
    }

    public async Task<StatusResponse> ResetPasswordAsync(PasswordResetRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<PasswordResetRequest, StatusResponse>(
            HttpMethod.Post, Constants.Paths.RecipeUserPasswordReset, request, Constants.RecipeIds.EmailPassword, cancellationToken);
    }

    public async Task<StatusResponse> AddUserRolesAsync(UserRolesRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<UserRolesRequest, StatusResponse>(
            HttpMethod.Put, Constants.Paths.RecipeUserRoles, request, Constants.RecipeIds.UserRoles, cancellationToken);
    }

    public async Task<UserRolesResponse> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["userId"] = userId;
        return await GetJsonAsync<UserRolesResponse>(
            $"{Constants.Paths.RecipeUserRoles}?{query}", Constants.RecipeIds.UserRoles, cancellationToken);
    }

    public async Task<StatusResponse> RemoveUserRolesAsync(UserRolesRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<UserRolesRequest, StatusResponse>(
            HttpMethod.Delete, Constants.Paths.RecipeUserRoles, request, Constants.RecipeIds.UserRoles, cancellationToken);
    }

    public async Task<RoleExistsResponse> DoesRoleExistAsync(string userId, string role, CancellationToken cancellationToken = default)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["userId"] = userId;
        query["role"] = role;
        return await GetJsonAsync<RoleExistsResponse>(
            $"{Constants.Paths.RecipeUserRole}?{query}", Constants.RecipeIds.UserRoles, cancellationToken);
    }

    public async Task<UserMetadataResponse> GetUserMetadataAsync(string userId, CancellationToken cancellationToken = default)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["userId"] = userId;
        return await GetJsonAsync<UserMetadataResponse>(
            $"{Constants.Paths.RecipeUserMetadata}?{query}", Constants.RecipeIds.UserMetadata, cancellationToken);
    }

    public async Task<StatusResponse> UpdateUserMetadataAsync(UserMetadataUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<UserMetadataUpdateRequest, StatusResponse>(
            HttpMethod.Put, Constants.Paths.RecipeUserMetadata, request, Constants.RecipeIds.UserMetadata, cancellationToken);
    }

    public async Task<CreateTotpDeviceResponse> CreateTotpDeviceAsync(CreateTotpDeviceRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<CreateTotpDeviceRequest, CreateTotpDeviceResponse>(
            HttpMethod.Post, Constants.TotpPaths.RecipeTotpDevice, request, Constants.RecipeIds.Totp, cancellationToken);
    }

    public async Task<VerifyTotpDeviceResponse> VerifyTotpDeviceAsync(VerifyTotpDeviceRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<VerifyTotpDeviceRequest, VerifyTotpDeviceResponse>(
            HttpMethod.Post, Constants.TotpPaths.RecipeTotpDeviceVerify, request, Constants.RecipeIds.Totp, cancellationToken);
    }

    public async Task<VerifyTotpCodeResponse> VerifyTotpCodeAsync(VerifyTotpCodeRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<VerifyTotpCodeRequest, VerifyTotpCodeResponse>(
            HttpMethod.Post, Constants.TotpPaths.RecipeTotpVerify, request, Constants.RecipeIds.Totp, cancellationToken);
    }

    public async Task<ListTotpDevicesResponse> ListTotpDevicesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["userId"] = userId;
        return await GetJsonAsync<ListTotpDevicesResponse>(
            $"{Constants.TotpPaths.RecipeTotpDeviceList}?{query}", Constants.RecipeIds.Totp, cancellationToken);
    }

    public async Task<RemoveTotpDeviceResponse> RemoveTotpDeviceAsync(RemoveTotpDeviceRequest request, CancellationToken cancellationToken = default)
    {
        return await SendJsonAsync<RemoveTotpDeviceRequest, RemoveTotpDeviceResponse>(
            HttpMethod.Post, Constants.TotpPaths.RecipeTotpDeviceRemove, request, Constants.RecipeIds.Totp, cancellationToken);
    }

    #endregion

    #region HTTP helpers

    private async Task<TResponse> GetJsonAsync<TResponse>(string pathAndQuery, string? rid, CancellationToken cancellationToken) where TResponse : new()
    {
        _logger.LogDebug("CDI GET {Path}", pathAndQuery);
        using var response = await SendWithRetryAsync(HttpMethod.Get, pathAndQuery, null, rid, cancellationToken);
        return await DeserializeResponseAsync<TResponse>(response, cancellationToken);
    }

    private async Task<TResponse> SendJsonAsync<TRequest, TResponse>(HttpMethod method, string path, TRequest requestBody, string? rid, CancellationToken cancellationToken) where TResponse : new()
    {
        _logger.LogDebug("CDI {Method} {Path}", method, path);
        var json = JsonSerializer.Serialize(requestBody, JsonOptions);
        using var response = await SendWithRetryAsync(method, path, json, rid, cancellationToken);
        return await DeserializeResponseAsync<TResponse>(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(HttpMethod method, string pathAndQuery, string? jsonBody, string? rid, CancellationToken cancellationToken)
    {
        var cdiVersion = await GetOrNegotiateCdiVersionAsync(cancellationToken);
        var isRecipePath = IsRecipePath(pathAndQuery);

        Exception? lastException = null;
        var hostCount = _hosts.Count;

        for (var hostAttempt = 0; hostAttempt < hostCount; hostAttempt++)
        {
            var host = GetNextHost();
            var url = new Uri(host, pathAndQuery);
            _logger.LogDebug("CDI request to {Url}", url);

            for (var retry = 0; retry <= Constants.RateLimitRetries; retry++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var request = new HttpRequestMessage(method, url);
                request.Headers.TryAddWithoutValidation(Constants.HeaderNames.CdiVersion, cdiVersion);

                if (!string.IsNullOrEmpty(rid) && isRecipePath)
                {
                    request.Headers.TryAddWithoutValidation(Constants.HeaderNames.Rid, rid);
                }

                if (!string.IsNullOrEmpty(jsonBody))
                {
                    request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                }

                try
                {
                    var response = await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                    if ((int)response.StatusCode == Constants.RateLimitStatusCode && retry < Constants.RateLimitRetries)
                    {
                        var delayMs = 10 + (250 * retry);
                        _logger.LogDebug("CDI rate limited; retrying in {DelayMs}ms", delayMs);
                        await Task.Delay(delayMs, cancellationToken);
                        continue;
                    }

                    return response;
                }
                catch (HttpRequestException ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "CDI request failed for host {Host}; failing over", host);
                    break; // try next host
                }
                catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "CDI request timed out for host {Host}; failing over", host);
                    break; // timeout, try next host
                }
            }
        }

        throw new SuperTokensException(
            "All SuperTokens Core hosts failed or the request was repeatedly rate limited.",
            lastException ?? new Exception("No hosts available."));
    }

    #endregion

    #region Response parsing and error handling

    private static async Task<TResponse> DeserializeResponseAsync<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken) where TResponse : new()
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            ThrowFromErrorBody(content, (int)response.StatusCode);
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return new TResponse();
        }

        using var document = JsonDocument.Parse(content);
        CheckStatusAndThrow(document);

        var result = JsonSerializer.Deserialize<TResponse>(content, JsonOptions);
        return result ?? new TResponse();
    }

    private static void CheckStatusAndThrow(JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("status", out var statusElement))
        {
            return;
        }

        var status = statusElement.GetString();
        if (string.IsNullOrEmpty(status) ||
            status.Equals(Constants.Status.Ok, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        ThrowFromStatus(document, status);
    }

    private static void ThrowFromErrorBody(string content, int httpStatusCode)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("status", out var statusElement))
            {
                var status = statusElement.GetString();
                if (!string.IsNullOrEmpty(status))
                {
                    ThrowFromStatus(document, status);
                    return;
                }
            }
        }
        catch (JsonException)
        {
            // body is not JSON, fall back to HTTP exception
        }

        throw new HttpRequestException(
            $"SuperTokens Core returned {httpStatusCode}: {content}",
            null,
            (HttpStatusCode)httpStatusCode);
    }

    private static void ThrowFromStatus(JsonDocument document, string status)
    {
        var message = document.RootElement.TryGetProperty("message", out var messageElement)
            ? messageElement.GetString() ?? status
            : status;

        if (status.Equals(Constants.Status.Unauthorized, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedException(message);
        }

        if (status.Equals(Constants.Status.TryRefreshToken, StringComparison.OrdinalIgnoreCase))
        {
            throw new TryRefreshTokenException(message);
        }

        if (status.Equals("NEEDS_REFRESH", StringComparison.OrdinalIgnoreCase))
        {
            throw new TryRefreshTokenException(message);
        }

        if (status.Equals(Constants.Status.TokenTheftDetected, StringComparison.OrdinalIgnoreCase))
        {
            var payload = document.RootElement.GetProperty("payload");
            var sessionHandle = payload.GetProperty("sessionHandle").GetString() ?? "";
            var userId = payload.GetProperty("userId").GetString() ?? "";
            throw new TokenTheftDetectedException(sessionHandle, userId);
        }

        if (status.Equals(Constants.Status.InvalidClaims, StringComparison.OrdinalIgnoreCase))
        {
            var invalidClaims = document.RootElement.GetProperty("invalidClaims").Deserialize<List<InvalidClaim>>(JsonOptions) ?? [];
            throw new InvalidClaimException(invalidClaims);
        }

        // Unknown status that is not OK: treat as generic SDK exception.
        throw new SuperTokensException($"SuperTokens Core returned status {status}: {message}");
    }

    #endregion

    #region CDI version negotiation

    private async Task<string> GetOrNegotiateCdiVersionAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_cdiVersion))
        {
            return _cdiVersion;
        }

        await _versionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(_cdiVersion))
            {
                return _cdiVersion;
            }

            _cdiVersion = await NegotiateCdiVersionAsync(cancellationToken);
            return _cdiVersion;
        }
        finally
        {
            _versionSemaphore.Release();
        }
    }

    private async Task<string> NegotiateCdiVersionAsync(CancellationToken cancellationToken)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(_options.ApiDomain))
        {
            query["apiDomain"] = _options.ApiDomain;
        }

        if (!string.IsNullOrWhiteSpace(_options.WebsiteDomain))
        {
            query["websiteDomain"] = _options.WebsiteDomain;
        }

        var pathAndQuery = $"{Constants.Paths.ApiVersion}?{query}";
        Exception? lastException = null;

        for (var hostAttempt = 0; hostAttempt < _hosts.Count; hostAttempt++)
        {
            var host = GetNextHost();
            var url = new Uri(host, pathAndQuery);

            try
            {
                using var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var apiVersion = JsonSerializer.Deserialize<ApiVersionResponse>(content, JsonOptions);

                var matchedVersion = SelectHighestMatchingVersion(apiVersion?.Versions);
                if (!string.IsNullOrEmpty(matchedVersion))
                {
                    _logger.LogInformation("Negotiated CDI version {Version} with {Host}", matchedVersion, host);
                    return matchedVersion;
                }

                throw new SuperTokensException(
                    $"SuperTokens Core does not support any of the SDK CDI versions: {string.Join(", ", Constants.SupportedCdiVersions)}");
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "CDI version negotiation failed for host {Host}; failing over", host);
            }
        }

        throw new SuperTokensException(
            "Could not negotiate CDI version with any SuperTokens Core host.",
            lastException ?? new Exception("No hosts available."));
    }

    private static string? SelectHighestMatchingVersion(IEnumerable<string>? serverVersions)
    {
        if (serverVersions == null)
        {
            return null;
        }

        var supported = new HashSet<string>(Constants.SupportedCdiVersions, StringComparer.OrdinalIgnoreCase);
        return serverVersions
            .Where(v => supported.Contains(v))
            .OrderByDescending(ParseVersion)
            .FirstOrDefault();
    }

    private static (int Major, int Minor) ParseVersion(string version)
    {
        var parts = version.Split('.');
        var major = int.TryParse(parts.ElementAtOrDefault(0), out var m) ? m : 0;
        var minor = int.TryParse(parts.ElementAtOrDefault(1), out var n) ? n : 0;
        return (major, minor);
    }

    #endregion

    #region Host utilities

    private static IReadOnlyList<Uri> ParseHosts(string? coreUri)
    {
        if (string.IsNullOrWhiteSpace(coreUri))
        {
            throw new ArgumentException(
                "SuperTokens CoreUri is not configured. " +
                "Set SuperTokensOptions.CoreUri in your application configuration.");
        }

        var parts = coreUri.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var hosts = new List<Uri>();
        foreach (var part in parts)
        {
            if (Uri.TryCreate(part, UriKind.Absolute, out var uri))
            {
                hosts.Add(uri);
            }
            else
            {
                throw new ArgumentException($"Invalid SuperTokens Core URI: '{part}'");
            }
        }

        if (hosts.Count == 0)
        {
            throw new ArgumentException("At least one valid SuperTokens Core URI is required.");
        }

        return hosts.AsReadOnly();
    }

    private Uri GetNextHost()
    {
        if (_hosts.Count == 1)
        {
            return _hosts[0];
        }

        var index = (int)(Interlocked.Increment(ref _hostIndex) % _hosts.Count);
        return _hosts[index];
    }

    private static bool IsRecipePath(string path)
    {
        return path.StartsWith("/recipe/", StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    private class ApiVersionResponse
    {
        [JsonPropertyName("versions")]
        public List<string> Versions { get; set; } = [];
    }

    /// <summary>
    /// Decodes a JWT payload without signature verification.
    /// Workaround for Core 11.x verify endpoint bug.
    /// </summary>
    private static Dictionary<string, object>? DecodeJwtPayload(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return null;

            var payloadBase64 = parts[1];
            payloadBase64 = payloadBase64.Replace('-', '+').Replace('_', '/');
            switch (payloadBase64.Length % 4)
            {
                case 2: payloadBase64 += "=="; break;
                case 3: payloadBase64 += "="; break;
            }

            var jsonBytes = Convert.FromBase64String(payloadBase64);
            var json = System.Text.Encoding.UTF8.GetString(jsonBytes);

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var result = new Dictionary<string, object>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                result[prop.Name] = prop.Value.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => prop.Value.GetString() ?? "",
                    System.Text.Json.JsonValueKind.Number => prop.Value.GetRawText(),
                    System.Text.Json.JsonValueKind.True => true,
                    System.Text.Json.JsonValueKind.False => false,
                    System.Text.Json.JsonValueKind.Null => "",
                    _ => prop.Value.GetRawText()
                };
            }

            // Check expiry
            if (result.TryGetValue("exp", out var expStr) && long.TryParse(expStr.ToString(), out var exp))
            {
                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp)
                {
                    throw new UnauthorizedException("Access token has expired.");
                }
            }

            return result;
        }
        catch (UnauthorizedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new UnauthorizedException($"Failed to decode access token: {ex.Message}");
        }
    }
}
