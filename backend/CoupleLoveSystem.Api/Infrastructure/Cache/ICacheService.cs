namespace CoupleLoveSystem.Infrastructure.Cache;

/// <summary>
/// 轻量缓存抽象。实现方应保证：读未命中返回 null；写入带 TTL；
/// GetOrCreateAsync 在缓存未命中时才回源（factory），并对同一 key 做并发压制（防惊群）。
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class;
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default) where T : class;
}
