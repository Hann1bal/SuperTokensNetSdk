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
    private DateTime _lastFetchAttempt = DateTime.MinValue;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Negative cache window applied after a failed fetch. Prevents request
    /// storms against an unreachable Core by short-circuiting repeated fetches
    /// within this interval.
    /// </summary>
    private static readonly TimeSpan NegativeCacheInterval = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Parameterless constructor retained for backward compatibility and DI
    /// singleton registration. Marked internal so external callers must use
    /// the <see cref="JwksClient(HttpClient)"/> constructor (or DI) which
    /// allows the host to manage the <see cref="HttpClient"/> lifetime.
    /// </summary>
    internal JwksClient()
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
    /// When a previous fetch failed within the negative cache window, the
    /// cached keys (or null) are returned without making a new HTTP call to
    /// avoid request storms during Core outages.
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

        // Negative cache: if the last fetch attempt failed very recently,
        // return whatever we have (possibly null) without hitting Core again.
        if (_cachedKeys == null
            && _lastFetchAttempt != DateTime.MinValue
            && DateTime.UtcNow - _lastFetchAttempt < NegativeCacheInterval)
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

            // Re-check the negative cache inside the semaphore to avoid
            // duplicate fetches from concurrent callers that were queued.
            if (_cachedKeys == null
                && _lastFetchAttempt != DateTime.MinValue
                && DateTime.UtcNow - _lastFetchAttempt < NegativeCacheInterval)
            {
                return _cachedKeys;
            }

            var baseUri = coreUri.TrimEnd('/');
            _lastFetchAttempt = DateTime.UtcNow;
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
