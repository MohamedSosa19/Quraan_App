namespace Quraan.Application.Caching;

public interface ICachedReader
{
    Task<T> GetOrAddAsync<T>(string key, TimeSpan ttl, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default)
        where T : class;
}
