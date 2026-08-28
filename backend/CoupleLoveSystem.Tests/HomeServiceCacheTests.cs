using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Infrastructure.Cache;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Repositories;
using CoupleLoveSystem.Tests.Infrastructure;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// 内存版 ICacheService 桩：记录回源（factory）次数，用于断言 HomeService 是否按情侣维度正确缓存。
/// </summary>
public class FakeCacheService : ICacheService
{
    private readonly Dictionary<string, (string Json, DateTimeOffset Exp)> _store = new();
    public int FactoryCalls { get; private set; }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        if (_store.TryGetValue(key, out var v) && v.Exp > DateTimeOffset.UtcNow)
            return Task.FromResult(JsonSerializer.Deserialize<T>(v.Json));
        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
    {
        _store[key] = (JsonSerializer.Serialize(value), DateTimeOffset.UtcNow + ttl);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _store.Remove(key);
        return Task.CompletedTask;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default) where T : class
    {
        var hit = await GetAsync<T>(key, ct);
        if (hit is not null) return hit;
        FactoryCalls++;
        var val = await factory(ct);
        await SetAsync(key, val, ttl, ct);
        return val;
    }
}

/// <summary>
/// HomeService 缓存行为测试：验证仪表盘/连续互动天数按情侣维度缓存，且重复调用不重复回源。
/// 一次 GetDashboardAsync 内部会触发两次回源（dashboard 聚合 + streak 派生），
/// 因此「同情侣二次调用」总回源次数应为 2（而非 4）。不同情侣因 key 不同各自回源。
/// </summary>
public class HomeServiceCacheTests
{
    private static HomeService Build(out CoupleDbContext db, out FakeCacheService cache)
    {
        db = TestDb.CreateInMemoryContext();
        cache = new FakeCacheService();
        return new HomeService(db,
            new AnniversaryRepository(db),
            cache);
    }

    [Fact]
    public async Task GetDashboardAsync_同情侣二次调用仅回源一次()
    {
        CoupleContext.Current = "cid-A";
        try
        {
            var svc = Build(out _, out var cache);
            var d1 = await svc.GetDashboardAsync(1);
            var d2 = await svc.GetDashboardAsync(1);

            Assert.NotNull(d1);
            Assert.Equal(d1.ActiveStreakDays, d2.ActiveStreakDays);
            // dashboard 回源 + streak 回源 各 1 次；第二次调用全部命中缓存
            Assert.Equal(2, cache.FactoryCalls);
        }
        finally
        {
            CoupleContext.Current = null;
        }
    }

    [Fact]
    public async Task GetDashboardAsync_不同情侣各自回源_缓存隔离()
    {
        var svc = Build(out _, out var cache);
        CoupleContext.Current = "cid-A";
        await svc.GetDashboardAsync(1); // 2 次回源
        CoupleContext.Current = "cid-B";
        await svc.GetDashboardAsync(2); // 不同 key → 再 2 次回源
        CoupleContext.Current = null;

        Assert.Equal(4, cache.FactoryCalls);
    }
}
