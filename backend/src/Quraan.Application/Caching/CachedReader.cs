using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Quraan.Application.Caching;

public sealed class CachedReader : ICachedReader
{
    public static readonly TimeSpan ContentTtl = TimeSpan.FromHours(24);
    public static readonly TimeSpan SearchTtl = TimeSpan.FromMinutes(15);

    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;

    public CachedReader(IDistributedCache cache) => _cache = cache;

    public async Task<T> GetOrAddAsync<T>(string key, TimeSpan ttl, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default)
        where T : class
    {
        var bytes = await _cache.GetAsync(key, ct).ConfigureAwait(false);
        if (bytes is { Length: > 0 })
        {
            var hit = JsonSerializer.Deserialize<T>(bytes, s_json);
            if (hit is not null) return hit;
        }

        var value = await factory(ct).ConfigureAwait(false);
        var encoded = JsonSerializer.SerializeToUtf8Bytes(value, s_json);
        await _cache.SetAsync(key, encoded, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct).ConfigureAwait(false);
        return value;
    }
}
