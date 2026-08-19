using System;
using System.Threading.Tasks;
using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Tests.Infrastructure;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>登录防爆破限速器测试：账号维度 5 次、IP 维度 10 次窗口限速，成功后账号计数重置。</summary>
public class LoginRateLimiterTests
{
    private static LoginRateLimiter Build() => new(new FakeCacheService());

    [Fact]
    public async Task 账号失败5次后第6次拒绝()
    {
        var lim = Build();
        for (var i = 0; i < 5; i++)
            await lim.RecordFailAsync("1.2.3.4", "partner_a");

        var ex = await Assert.ThrowsAsync<RateLimitedException>(() => lim.CheckAsync("1.2.3.4", "partner_a"));
        Assert.Contains("尝试次数过多", ex.Message);
    }

    [Fact]
    public async Task 不同账号互不影响()
    {
        var lim = Build();
        for (var i = 0; i < 5; i++)
            await lim.RecordFailAsync("1.2.3.4", "partner_a");

        // partner_b 未失败过，同一 IP 下仍可正常尝试
        await lim.CheckAsync("1.2.3.4", "partner_b");
    }

    [Fact]
    public async Task IP失败10次后第11次拒绝()
    {
        var lim = Build();
        for (var i = 0; i < 10; i++)
            await lim.RecordFailAsync("9.9.9.9", "u" + i); // 每次不同账号，只累计 IP 维度

        await Assert.ThrowsAsync<RateLimitedException>(() => lim.CheckAsync("9.9.9.9", "anyone"));
    }

    [Fact]
    public async Task 成功后账号计数重置()
    {
        var lim = Build();
        for (var i = 0; i < 5; i++)
            await lim.RecordFailAsync("1.2.3.4", "partner_a");

        await lim.ResetAsync("partner_a");
        await lim.CheckAsync("1.2.3.4", "partner_a"); // 不再抛异常
    }

    [Fact]
    public async Task 未超限时正常放行()
    {
        var lim = Build();
        await lim.CheckAsync("1.2.3.4", "partner_a"); // 无失败记录，直接通过
        await lim.RecordFailAsync("1.2.3.4", "partner_a");
        await lim.CheckAsync("1.2.3.4", "partner_a"); // 1 次失败仍在窗口内
    }
}
