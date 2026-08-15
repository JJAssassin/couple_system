using System;
using System.Threading;
using System.Threading.Tasks;
using CoupleLoveSystem.Core.Options;
using CoupleLoveSystem.Infrastructure.Cache;
using Microsoft.Extensions.Options;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// 缓存抽象测试：验证 GetOrCreateAsync「未命中才回源、命中走缓存、Remove 失效」语义。
/// 无论 Redis 是否可达（不可达时 RedisCacheService 自动降级内存），该语义都成立，
/// 故本测试在有无 Redis 的环境均能稳定通过。
/// </summary>
public class CacheServiceTests
{
    private static RedisCacheService Build()
        => new(Options.Create(new TokenStoreOptions { Configuration = "127.0.0.1:6379" }));

    [Fact]
    public async Task GetOrCreateAsync_回源一次后命中缓存()
    {
        var cache = Build();
        var key = "test:dashboard:" + Guid.NewGuid();
        var calls = 0;
        Func<CancellationToken, Task<string>> factory = _ =>
        {
            calls++;
            return Task.FromResult("v1");
        };

        var a = await cache.GetOrCreateAsync(key, TimeSpan.FromMinutes(1), factory);
        var b = await cache.GetOrCreateAsync(key, TimeSpan.FromMinutes(1), factory);

        Assert.Equal("v1", a);
        Assert.Equal("v1", b);
        Assert.Equal(1, calls); // 第二次走缓存，不再回源
    }

    [Fact]
    public async Task RemoveAsync_使缓存失效_再次回源()
    {
        var cache = Build();
        var key = "test:remove:" + Guid.NewGuid();
        var calls = 0;
        Func<CancellationToken, Task<string>> factory = _ =>
        {
            calls++;
            return Task.FromResult("n" + calls);
        };

        var first = await cache.GetOrCreateAsync(key, TimeSpan.FromMinutes(1), factory); // calls=1 -> "n1" 实际 "n1"
        await cache.RemoveAsync(key);
        var second = await cache.GetOrCreateAsync(key, TimeSpan.FromMinutes(1), factory); // calls=2 -> "n2"

        Assert.Equal(2, calls);
        Assert.Equal("n1", first);
        Assert.Equal("n2", second);
    }
}
