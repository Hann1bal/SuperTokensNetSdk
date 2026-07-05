using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

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

    /// <summary>
    /// Creates an RS256 signed JWT and the corresponding JWKS JSON.
    /// </summary>
    public static (string Jwt, string JwksJson) CreateSignedJwt(Dictionary<string, object> payload)
    {
        using var rsa = RSA.Create(2048);
        var securityKey = new RsaSecurityKey(rsa.ExportParameters(true))
        {
            KeyId = "test-key-1"
        };
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var claims = new List<Claim>();
        foreach (var kvp in payload)
        {
            switch (kvp.Value)
            {
                case bool b:
                    claims.Add(new Claim(kvp.Key, b ? "true" : "false", ClaimValueTypes.Boolean));
                    break;
                case long l:
                    claims.Add(new Claim(kvp.Key, l.ToString(), ClaimValueTypes.Integer64));
                    break;
                case int i:
                    claims.Add(new Claim(kvp.Key, i.ToString(), ClaimValueTypes.Integer));
                    break;
                default:
                    claims.Add(new Claim(kvp.Key, kvp.Value?.ToString() ?? string.Empty, ClaimValueTypes.String));
                    break;
            }
        }

        claims.Add(new Claim(
            "exp",
            DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString(),
            ClaimValueTypes.Integer64));

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            new JwtHeader(credentials),
            new JwtPayload(claims));

        var jwt = handler.WriteToken(token);

        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(securityKey);
        var jwks = new JsonWebKeySet();
        jwks.Keys.Add(jwk);

        var jwksJson = JsonSerializer.Serialize(new
        {
            keys = jwks.Keys.Select(k => new
            {
                kty = k.Kty,
                kid = k.KeyId,
                n = k.N,
                e = k.E,
                alg = k.Alg ?? "RS256"
            }).ToArray()
        });

        return (jwt, jwksJson);
    }

    private static string Base64UrlEncode(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
