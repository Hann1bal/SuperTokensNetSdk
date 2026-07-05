using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;

namespace SuperTokensSDK.Net.Recipes.UserMetadata;

/// <summary>
/// SuperTokens UserMetadata recipe operations.
/// </summary>
public class UserMetadataRecipe
{
    private readonly ICoreApiClient _coreApiClient;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public UserMetadataRecipe(ICoreApiClient coreApiClient)
    {
        _coreApiClient = coreApiClient ?? throw new ArgumentNullException(nameof(coreApiClient));
    }

    public async Task<Dictionary<string, object>?> GetMetadataAsync(string userId, CancellationToken cancellationToken = default)
    {
        var response = await _coreApiClient.GetUserMetadataAsync(userId, cancellationToken);
        return response.Metadata;
    }

    public async Task UpdateMetadataAsync(string userId, Dictionary<string, object> update, CancellationToken cancellationToken = default)
    {
        await _coreApiClient.UpdateUserMetadataAsync(new UserMetadataUpdateRequest { UserId = userId, MetadataUpdate = update }, cancellationToken);
    }

    public async Task<T?> GetMetadataAsAsync<T>(string userId, CancellationToken cancellationToken = default) where T : class, new()
    {
        var metadata = await GetMetadataAsync(userId, cancellationToken);
        if (metadata == null) return null;

        // Serialize directly to UTF-8 bytes and deserialize from them to avoid
        // the double allocation of an intermediate string.
        var utf8Json = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(metadata, JsonOptions);
        return System.Text.Json.JsonSerializer.Deserialize<T>(utf8Json, JsonOptions);
    }
}
