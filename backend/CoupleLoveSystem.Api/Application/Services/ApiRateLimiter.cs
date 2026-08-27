using CoupleLoveSystem.Infrastructure.Cache;

namespace CoupleLoveSystem.Application.Services;

/// <summary>
/// 通用 API 速率限制（审计 P2-1/2/3）。基于 ICacheService（生产 Redis / 开发内存）做固定窗口计数，
/// 不依赖 ASP.NET Core 内置 RateLimiter 中间件（本机共享框架缺 Microsoft.AspNetCore.RateLimiter 程序集）。
/// 三档策略：
///  - refresh：匿名刷新端点，按客户端 IP，1 分钟 5 次（刷新令牌为 128-bit GUID 爆破不可行，但匿名端点无节流仍是加固缺口）。
///  - join：绑定/加入，按客户端 IP，1 分钟 10 次（邀请码 6 位 10 分钟有效，现实爆破风险低，但 /join 无节流）。
///  - export：导出（完整 zip / CSV），资源消耗大，按用户（JWT sub），10 分钟 3 次。
/// 调用方（RateLimitMiddleware）在 TryAsync 返回 false 时返回 429。
/// </summary>
public class ApiRateLimiter
{
    private readonly ICacheService _cache;

    public ApiRateLimiter(ICacheService cache) => _cache = cache;

    private sealed class CounterBox
    {
        public int Value { get; set; }
        public CounterBox() { }
        public CounterBox(int v) => Value = v;
    }

    private static readonly (int Limit, TimeSpan Window) Refresh = (5, TimeSpan.FromMinutes(1));
    private static readonly (int Limit, TimeSpan Window) Join = (10, TimeSpan.FromMinutes(1));
    private static readonly (int Limit, TimeSpan Window) Export = (3, TimeSpan.FromMinutes(10));

    /// <summary>尝试放行一次请求；超限返回 false。partition 为限速分区键（IP 或用户 Id）。</summary>
    public async Task<bool> TryAsync(string policy, string partition, CancellationToken ct = default)
    {
        var (limit, window) = policy switch
        {
            "refresh" => Refresh,
            "join" => Join,
            "export" => Export,
            _ => (int.MaxValue, TimeSpan.FromMinutes(1))
        };
        if (limit == int.MaxValue) return true;

        var windowSec = (int)window.TotalSeconds;
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var bucket = now / windowSec;                       // 固定窗口桶（每 windowSec 一个）
        var key = $"rl:{policy}:{partition}:{bucket}";

        var box = await _cache.GetAsync<CounterBox>(key, ct);
        var count = box?.Value ?? 0;
        if (count >= limit) return false;

        count++;
        // 桶的 TTL 取到本窗口结束的剩余秒数，避免每次写入都重置过期导致窗口被拉长。
        var ttlSec = (int)((bucket + 1) * windowSec - now);
        if (ttlSec < 1) ttlSec = 1;
        await _cache.SetAsync(key, new CounterBox(count), TimeSpan.FromSeconds(ttlSec), ct);
        return true;
    }
}
