using System.Threading.Tasks;
using CoupleLoveSystem.Application.Services;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>ApiRateLimiter 固定窗口限速测试（审计 P2-1/2/3）：同窗口内超限拒绝、分区/策略互相隔离。</summary>
public class ApiRateLimiterTests
{
    private static ApiRateLimiter Build() => new(new FakeCacheService());

    [Fact]
    public async Task 刷新按IP限速_第6次拒绝()
    {
        var lim = Build();
        for (var i = 0; i < 5; i++)
            Assert.True(await lim.TryAsync("refresh", "1.2.3.4"));
        Assert.False(await lim.TryAsync("refresh", "1.2.3.4"));
    }

    [Fact]
    public async Task 不同IP互不干扰()
    {
        var lim = Build();
        for (var i = 0; i < 5; i++)
            Assert.True(await lim.TryAsync("refresh", "1.2.3.4"));
        Assert.False(await lim.TryAsync("refresh", "1.2.3.4"));
        Assert.True(await lim.TryAsync("refresh", "9.9.9.9")); // 另一 IP 仍放行
    }

    [Fact]
    public async Task 导出按用户限速_第4次拒绝()
    {
        var lim = Build();
        for (var i = 0; i < 3; i++)
            Assert.True(await lim.TryAsync("export", "user-1"));
        Assert.False(await lim.TryAsync("export", "user-1"));
    }

    [Fact]
    public async Task 不同策略独立计数()
    {
        var lim = Build();
        // refresh 耗尽
        for (var i = 0; i < 5; i++)
            Assert.True(await lim.TryAsync("refresh", "same"));
        Assert.False(await lim.TryAsync("refresh", "same"));
        // 同一分区下 export 不受 refresh 影响
        for (var i = 0; i < 3; i++)
            Assert.True(await lim.TryAsync("export", "same"));
        Assert.False(await lim.TryAsync("export", "same"));
    }
}
