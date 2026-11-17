using IdentityService.Security.Keys;
namespace IdentityService.Common;

public static class JwksEndpoint
{
    public const string Path = "/.well-known/jwks.json";

    public static IEndpointRouteBuilder MapJwks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(Path, (RsaKeyProvider keys, HttpResponse response) =>
        {
            var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(keys.ValidationKey);
            response.Headers.CacheControl = "public, max-age=300";
            return Microsoft.AspNetCore.Http.Results.Json(new
            {
                keys = new[]
                {
                    new { kty = "RSA", use = "sig", alg = SecurityAlgorithms.RsaSha256, kid = keys.ValidationKey.KeyId, n = jwk.N, e = jwk.E }
                }
            });
        })
        .RequireRateLimiting(RateLimitPolicies.Auth)
        .ExcludeFromDescription();
        return endpoints;
    }
}
