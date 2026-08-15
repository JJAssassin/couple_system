using StackExchange.Redis;
using System.Runtime.Versioning;

namespace CoupleLoveSystem.Infrastructure.Redis;

/// <summary>
/// 基于 Redis 的分布式锁（SET NX + 过期）。多个 API 实例部署时，只有一个能抢到锁执行定时任务。
/// 复用 TokenStore 的 Redis 地址配置；Redis 不可用时保守跳过本轮（而非崩溃或重复执行）。
/// 单例注册，应用关闭时由 DI 释放连接。
/// </summary>
public sealed class RedisJobLock : IDistributedJobLock, IDisposable
{
    private readonly IConnectionMultiplexer _mux;
    private readonly IDatabase _db;
    private const string LockKey = "lock:scheduled-job";

    public RedisJobLock(string configuration)
    {
        // AbortOnConnectFail=false：Redis 暂不可用时后台自动重连，不使应用启动即崩
        var cfg = ConfigurationOptions.Parse(configuration);
        cfg.AbortOnConnectFail = false;
        _mux = ConnectionMultiplexer.Connect(cfg);
        _db = _mux.GetDatabase();
    }

    public async Task<bool> TryAcquireAsync(TimeSpan ttl, CancellationToken ct = default)
    {
        try
        {
            // 仅当锁不存在时设置（NX），并设置过期，返回结果即是否抢到
            return await _db.StringSetAsync(LockKey, Environment.MachineName, ttl, When.NotExists);
        }
        catch (Exception)
        {
            // Redis 不可用：保守跳过本轮，避免重复执行
            return false;
        }
    }

    public async Task ReleaseAsync(CancellationToken ct = default)
    {
        try { await _db.KeyDeleteAsync(LockKey); }
        catch (Exception) { /* 释放失败忽略，锁会随 TTL 自动过期 */ }
    }

    public void Dispose() => _mux.Dispose();
}
