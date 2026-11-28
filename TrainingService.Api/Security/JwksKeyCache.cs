using Microsoft.IdentityModel.Tokens;

namespace TrainingService.Api.Security;

public sealed class JwksKeyCache(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<JwksKeyCache> logger)
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan MinRefreshOnUnknownKid = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private IReadOnlyList<SecurityKey> _keys = [];
    private DateTime _lastFetchUtc = DateTime.MinValue;

    public IEnumerable<SecurityKey> GetKeys(string? kid)
    {
        var keys = Snapshot();

        var stale = DateTime.UtcNow - _lastFetchUtc > RefreshInterval;
        var unknownKid = kid is not null && keys.All(k => k.KeyId != kid)
                                         && DateTime.UtcNow - _lastFetchUtc > MinRefreshOnUnknownKid;

        if (keys.Count == 0 || stale || unknownKid)
        {
            Refresh();
            keys = Snapshot();
        }

        return keys;
    }

    private IReadOnlyList<SecurityKey> Snapshot() => _keys;

    private void Refresh()
    {
        _lock.Wait();
        try
        {
            var url = configuration["Jwt:JwksUrl"]
                      ?? throw new InvalidOperationException("Jwt:JwksUrl is not configured.");
            using var client = httpClientFactory.CreateClient("jwks");
            var json = client.GetStringAsync(url).GetAwaiter().GetResult();

            _keys = new JsonWebKeySet(json).GetSigningKeys().ToList();
            _lastFetchUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch JWKS from Identity service.");
            // نكمل بآخر keys معروفة لو موجودة
            _lastFetchUtc = DateTime.UtcNow;
        }
        finally
        {
            _lock.Release();
        }
    }
}