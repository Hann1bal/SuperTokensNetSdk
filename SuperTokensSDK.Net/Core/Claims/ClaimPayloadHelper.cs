using System.Text.Json;

namespace SuperTokensSDK.Net.Core.Claims;

internal static class ClaimPayloadHelper
{
    public const string MetadataPrefix = "_";

    public static string MetadataKey(string claimKey) => $"{MetadataPrefix}{claimKey}";

    public static T? ConvertValue<T>(object? value)
    {
        if (value is T typed)
        {
            return typed;
        }

        if (value is JsonElement element)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(element.GetRawText());
            }
            catch (JsonException)
            {
                return default;
            }
        }

        if (value is not null && typeof(T).IsEnum)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), value.ToString()!, true);
            }
            catch
            {
                return default;
            }
        }

        try
        {
            return (T?)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    public static string[] GetStringArray(object? value)
    {
        if (value is string[] array)
        {
            return array;
        }

        if (value is IEnumerable<string> enumerable)
        {
            return enumerable.ToArray();
        }

        if (value is JsonElement element && element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray()
                .Select(x => x.GetString() ?? x.ToString())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray()!;
        }

        if (value is string single)
        {
            return new[] { single };
        }

        return Array.Empty<string>();
    }

    public static long? GetLastRefetchTime(Dictionary<string, object?> payload, string claimKey)
    {
        var key = MetadataKey(claimKey);
        if (payload.TryGetValue(key, out var value) && value is not null)
        {
            try
            {
                return Convert.ToInt64(value);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    public static Dictionary<string, object?> SetMetadata(Dictionary<string, object?> payload, string claimKey)
    {
        var updated = new Dictionary<string, object?>(payload);
        updated[MetadataKey(claimKey)] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return updated;
    }

    public static Dictionary<string, object?> RemoveMetadata(Dictionary<string, object?> payload, string claimKey)
    {
        var updated = new Dictionary<string, object?>(payload);
        updated.Remove(MetadataKey(claimKey));
        return updated;
    }
}
