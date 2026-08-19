using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Threading;
using System.Threading.Tasks;

namespace CoupleLoveSystem.Infrastructure.Redis;

/// <summary>
/// 基于 StackExchange.Redis 的 RefreshToken 存储实现。
/// Key 规范：{KeyPrefix}{userId}；值为 refresh token 字符串，TTL 由调用方传入。
///
/// 重要：本类**刻意不实现 IDisposable**。
/// 它被注册为单例，又通过 `AddScoped&lt;ITokenStore&gt;(sp =&gt; sp.GetRequiredService&lt;RedisTokenStore&gt;())`
/// 以「同一个单例实例」作为 Scoped 返回。.NET DI 在请求结束释放 Scope 时，会对「具体类型实现了
/// IDisposable 的解析实例」调用 Dispose()——于是共享的 ConnectionMultiplexer 在第一次请求后就被释放，
/// 后续请求再使用即抛 "Cannot access a disposed object"。去掉 IDisposable 后， multiplexer 随应用生命周期
/// 存活（进程退出由 OS 回收），彻底规避该陷阱。
/// </summary>
public sealed class RedisTokenStore : ITokenStore
{
    private readonly IConnectionMultiplexer _mux;
    private readonly IDatabase _db;
    private readonly string _prefix;

    public RedisTokenStore(IOptions<TokenStoreOptions> opt)
    {
        // AbortOnConnectFail=false：Redis 暂不可用时由 SE.Redis 后台自动重连，而非让应用启动即崩溃。
        var config = ConfigurationOptions.Parse(opt.Value.Configuration);
        config.AbortOnConnectFail = false;
        _mux = ConnectionMultiplexer.Connect(config);
        _db = _mux.GetDatabase();
        _prefix = opt.Value.KeyPrefix;
    }

    public async Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken ct = default)
        => await _db.StringSetAsync(_prefix + key, value, ttl);

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        var v = await _db.StringGetAsync(_prefix + key);
        return v.HasValue ? v.ToString() : null;
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
        => await _db.KeyDeleteAsync(_prefix + key);

    /// <summary>
    /// 探测 Redis 连通性（真实往返一次，不抛异常）。
    /// 生产启动 fail-fast 用：本类 AbortOnConnectFail=false 会「带病运行」——Redis 不可达时应用照常启动、
    /// 直到刷新令牌读写请求期才炸；这里在启动阶段做一次真实探测，由调用方决定是否拒绝启动。
    /// 探测耗时上限 = SE.Redis ConnectTimeout + SyncTimeout（默认约 5s）。
    /// </summary>
    public bool Ping()
    {
        try
        {
            _db.Ping();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
