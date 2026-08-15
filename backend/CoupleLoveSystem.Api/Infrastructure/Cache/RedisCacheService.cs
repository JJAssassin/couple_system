using CoupleLoveSystem.Core.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;

namespace CoupleLoveSystem.Infrastructure.Cache;

/// <summary>
/// 基于 StackExchange.Redis 的缓存实现。
/// - 复用 TokenStore 的连接配置（同 Redis 实例，独立 key 前缀 `cache:`），不另起连接池负担。
/// - Redis 不可用（连接失败 / 命令异常）时**自动降级**为进程内内存缓存，保证主流程不受影响
///   （与 TokenStore 的降级策略一致，符合「健硕代码」目标）。
/// - GetOrCreateAsync 提供「未命中才回源」语义，并用 per-key 信号量防止惊群（thundering herd）。
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private readonly IDatabase? _db;
    private readonly string _prefix = "cache:";
    private readonly ConcurrentDictionary<string, (string Json, DateTimeOffset Expire)> _memory = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public RedisCacheService(IOptions<TokenStoreOptions> opt)
    {
        try
        {
            // AbortOnConnectFail=false：即便 Redis 暂不可达，也由 SE.Redis 后台自动重连，而非让应用启动即崩溃。
            var config = ConfigurationOptions.Parse(opt.Value.Configuration);
            config.AbortOnConnectFail = false;
            var mux = ConnectionMultiplexer.Connect(config);
            _db = mux.GetDatabase();
        }
        catch
        {
            // 连接失败 → 降级内存，本实例后续读写全部走 _memory。
            _db = null;
        }
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        var full = _prefix + key;
        try
        {
            if (_db != null)
            {
                var v = await _db.StringGetAsync(full);
                if (v.HasValue) return JsonSerializer.Deserialize<T>(v!);
            }
        }
        catch
        {
            // 落到内存降级分支
        }

        if (_memory.TryGetValue(full, out var m) && m.Expire > DateTimeOffset.UtcNow)
            return JsonSerializer.Deserialize<T>(m.Json);
        return null;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
    {
        var full = _prefix + key;
        var json = JsonSerializer.Serialize(value);
        try
        {
            if (_db != null)
            {
                await _db.StringSetAsync(full, json, ttl);
                return;
            }
        }
        catch
        {
            // 落到内存降级
        }

        _memory[full] = (json, DateTimeOffset.UtcNow + ttl);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        var full = _prefix + key;
        try { if (_db != null) await _db.KeyDeleteAsync(full); } catch { }
        _memory.TryRemove(full, out _);
    }

    public async Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default) where T : class
    {
        var hit = await GetAsync<T>(key, ct);
        if (hit is not null) return hit;

        // per-key 信号量：并发回源时只有一个线程真正执行 factory，其余等待后取缓存结果。
        var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct);
        try
        {
            var recheck = await GetAsync<T>(key, ct); // 双重检查，避免信号量等待期间已被填充
            if (recheck is not null) return recheck;

            var val = await factory(ct);
            await SetAsync(key, val, ttl, ct);
            return val;
        }
        finally
        {
            sem.Release();
        }
    }
}
