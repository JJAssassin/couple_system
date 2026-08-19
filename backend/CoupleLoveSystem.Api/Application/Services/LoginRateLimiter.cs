using CoupleLoveSystem.Infrastructure.Cache;

namespace CoupleLoveSystem.Application.Services;

/// <summary>
/// 登录防爆破限速器（Redis-backed，经 ICacheService 计数；测试环境 InMemory 亦可）。
/// 双维度固定窗口：IP 15 分钟最多 10 次失败；账号 15 分钟最多 5 次失败。
/// 任一维度超限 → RateLimitedException（429）；登录成功清空账号维度计数。
/// </summary>
public class LoginRateLimiter
{
    private readonly ICacheService _cache;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    private const int MaxFailPerIp = 10;
    private const int MaxFailPerUser = 5;

    public LoginRateLimiter(ICacheService cache) => _cache = cache;

    private static string IpKey(string ip) => "ratelimit:login:ip:" + ip;
    private static string UserKey(string user) => "ratelimit:login:u:" + user.ToLowerInvariant();

    /// <summary>尝试登录前检查：超限直接拒绝（429）。</summary>
    public async Task CheckAsync(string ip, string userName, CancellationToken ct = default)
    {
        var uCount = (await _cache.GetAsync<CounterBox>(UserKey(userName), ct))?.Value ?? 0;
        if (uCount >= MaxFailPerUser)
            throw new RateLimitedException("该账号尝试次数过多，请 15 分钟后再试");

        if (!string.IsNullOrWhiteSpace(ip))
        {
            var iCount = (await _cache.GetAsync<CounterBox>(IpKey(ip), ct))?.Value ?? 0;
            if (iCount >= MaxFailPerIp)
                throw new RateLimitedException("尝试次数过多，请 15 分钟后再试");
        }
    }

    /// <summary>登录失败：IP 与账号双维度各 +1（非原子计数，个人应用可接受）。</summary>
    public async Task RecordFailAsync(string ip, string userName, CancellationToken ct = default)
    {
        var uKey = UserKey(userName);
        var uCount = (await _cache.GetAsync<CounterBox>(uKey, ct))?.Value ?? 0;
        await _cache.SetAsync(uKey, new CounterBox(uCount + 1), Window, ct);

        if (!string.IsNullOrWhiteSpace(ip))
        {
            var iKey = IpKey(ip);
            var iCount = (await _cache.GetAsync<CounterBox>(iKey, ct))?.Value ?? 0;
            await _cache.SetAsync(iKey, new CounterBox(iCount + 1), Window, ct);
        }
    }

    /// <summary>登录成功：清空账号维度失败计数（IP 维度保留，防换号重试同一 IP）。</summary>
    public async Task ResetAsync(string userName, CancellationToken ct = default)
        => await _cache.RemoveAsync(UserKey(userName), ct);

    /// <summary>计数包装（ICacheService 泛型仅支持引用类型）。</summary>
    public sealed class CounterBox
    {
        public int Value { get; set; }
        public CounterBox() { }
        public CounterBox(int v) => Value = v;
    }
}
