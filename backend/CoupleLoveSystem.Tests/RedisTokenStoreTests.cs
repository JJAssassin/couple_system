using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Options;
using CoupleLoveSystem.Infrastructure.Redis;
using Microsoft.Extensions.Options;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// 验证 ITokenStore 契约（用既有的 InMemory 实现做基线），
/// 并确认 RedisTokenStore 实现了该契约。真实 Redis 集成在本地 Redis80 上手动验证。
/// </summary>
public class RedisTokenStoreTests
{
    private readonly ITokenStore _store = new InMemoryTokenStore();
    private static readonly CancellationToken Ct = CancellationToken.None;

    [Fact]
    public async Task Set_Get_ReturnsValue_WithinTtl()
    {
        await _store.SetAsync("rt:1", "tokA", TimeSpan.FromMinutes(5), Ct);
        Assert.Equal("tokA", await _store.GetAsync("rt:1", Ct));
    }

    [Fact]
    public async Task Get_Expired_ReturnsNull_AndRemoves()
    {
        await _store.SetAsync("rt:2", "tokB", TimeSpan.FromTicks(1), Ct);
        await Task.Delay(2);
        Assert.Null(await _store.GetAsync("rt:2", Ct));
    }

    [Fact]
    public async Task Remove_DeletesValue()
    {
        await _store.SetAsync("rt:3", "tokC", TimeSpan.FromMinutes(5), Ct);
        await _store.RemoveAsync("rt:3", Ct);
        Assert.Null(await _store.GetAsync("rt:3", Ct));
    }

    [Fact]
    public void RedisTokenStore_Implements_ITokenStore()
    {
        Assert.True(typeof(ITokenStore).IsAssignableFrom(typeof(RedisTokenStore)));
    }

    [Fact]
    public void RedisTokenStore_Connects_With_Options()
    {
        // 仅验证构造可用（本地 Redis80 127.0.0.1:6379 无密码）
        var opt = Options.Create(new TokenStoreOptions
        {
            Provider = "Redis",
            Configuration = "127.0.0.1:6379",
            KeyPrefix = "auth:rt:"
        });
        // 注：RedisTokenStore 刻意不实现 IDisposable（避免被 Scoped 释放共享 multiplexer），故此处不包 using
        var store = new RedisTokenStore(opt);
        Assert.True(typeof(ITokenStore).IsAssignableFrom(store.GetType()));
    }
}
