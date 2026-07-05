using Microsoft.IdentityModel.Tokens;

namespace SuperTokensSDK.Net.Core;

/// <summary>
/// Caches the JSON Web Key Set published by SuperTokens Core and exposes it
/// for JWT signature verification.
/// </summary>
public sealed class JwksClient
{
    private readonly HttpClient _httpClient;
    private readonly TimeSpan _refreshInterval;

    private JsonWebKeySet? _cachedKeys;
    private DateTime _lastFetch = DateTime.MinValue;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public JwksClient()
        : this(new HttpClient())
    {
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public JwksClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _refreshInterval = TimeSpan.FromHours(1);
    }

    /// <summary>
    /// Returns the cached JWKS if it is fresh, otherwise fetches it from Core.
    /// </summary>
    public async Task<JsonWebKeySet?> GetKeysAsync(string coreUri, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(coreUri))
        {
            return _cachedKeys;
        }

        if (_cachedKeys != null && DateTime.UtcNow - _lastFetch < _refreshInterval)
        {
            return _cachedKeys;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_cachedKeys != null && DateTime.UtcNow - _lastFetch < _refreshInterval)
            {
                return _cachedKeys;
            }

            var baseUri = coreUri.TrimEnd('/');
            var response = await _httpClient.GetAsync(
                $"{baseUri}/.well-known/jwks.json",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return _cachedKeys;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            _cachedKeys = JsonWebKeySet.Create(json);
            _lastFetch = DateTime.UtcNow;
            return _cachedKeys;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
