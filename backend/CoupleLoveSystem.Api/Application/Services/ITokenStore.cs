namespace CoupleLoveSystem.Application.Services;

/// <summary>
/// RefreshToken 存储抽象。生产环境用 Redis 实现（见文档 Redis Key 规范 auth:rt:{userId}:{deviceId}）；
/// 此处提供内存实现，保证无 Redis 依赖也能本地运行，上线前替换为 Redis 版即可。
/// </summary>
public interface ITokenStore
{
    Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken ct = default);
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}

public class InMemoryTokenStore : ITokenStore
{
    private static readonly ConcurrentDictionary<string, (string Value, DateTime Expire)> _store = new();
    public Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken ct = default)
    {
        _store[key] = (value, DateTime.UtcNow + ttl);
        return Task.CompletedTask;
    }
    public Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        if (_store.TryGetValue(key, out var v) && v.Expire > DateTime.UtcNow) return Task.FromResult<string?>(v.Value);
        _store.TryRemove(key, out _);
        return Task.FromResult<string?>(null);
    }
    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
