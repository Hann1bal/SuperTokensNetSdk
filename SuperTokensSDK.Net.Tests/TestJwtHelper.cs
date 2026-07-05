using System.Text;
using System.Text.Json;

namespace SuperTokensSDK.Net.Tests;

public static class TestJwtHelper
{
    public static string CreateJwt(Dictionary<string, object> payload, bool expired = false)
    {
        var header = "{\"alg\":\"RS256\",\"typ\":\"JWT\",\"version\":\"5\"}";

        payload["exp"] = expired
            ? DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds()
            : DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();

        var payloadJson = JsonSerializer.Serialize(payload);
        return $"{Base64UrlEncode(header)}.{Base64UrlEncode(payloadJson)}.signature";
    }

    public static long FutureEpoch()
    {
        return DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
    }

    private static string Base64UrlEncode(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
