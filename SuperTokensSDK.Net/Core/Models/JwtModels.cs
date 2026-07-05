namespace SuperTokensSDK.Net.Core.Models;

public sealed class CreateJwtRequest
{
    public Dictionary<string, object>? Payload { get; set; }
    public int? Validity { get; set; }
    public string? UseStaticSigningKey { get; set; }
}

public sealed class CreateJwtResponse
{
    public string? Status { get; set; }
    public string? Jwt { get; set; }
}

public sealed class JwksResponse
{
    public List<JsonWebKey> Keys { get; set; } = new();
}

public sealed class JsonWebKey
{
    public string? Kty { get; set; }
    public string? Kid { get; set; }
    public string? Use { get; set; }
    public string? Alg { get; set; }
    public string? N { get; set; }
    public string? E { get; set; }
}
