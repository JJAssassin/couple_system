using System;
using System.Threading.Tasks;
using CoupleLoveSystem.Api;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Realtime;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// SignalR 广播跨情侣隔离集成测试：情侣 A 的内容变更只广播到 couple-A 组，
/// 绝不泄漏到 couple-B 组；增量信号携带正确的 (模块, 操作类型, 真实主键)。
/// 走真实 CoupleDbContext.SaveChanges → SyncBroadcaster → IHubContext 链路。
/// </summary>
public class SyncBroadcastIsolationTests
{
    private const string CidA = "bc-cid-A";
    private const string CidB = "bc-cid-B";

    [Fact]
    public async Task 情侣A变更_只广播到A组_带增量主键_不泄漏B组()
    {
        var hub = new RecordingHubContext();
        var broadcaster = new SyncBroadcaster(hub);
        var options = new DbContextOptionsBuilder<CoupleDbContext>()
            .UseInMemoryDatabase("bc-iso-" + Guid.NewGuid()).Options;

        await using (var db = new CoupleDbContext(options, broadcaster))
        {
            CoupleContext.Current = CidA;
            db.Diaries.Add(new CoupleDiary { Title = "A-diary", Content = "c", CoupleId = CidA, CreateUserId = 1 });
            await db.SaveChangesAsync();
        }

        Assert.Equal("couple-" + CidA, hub.LastGroup);
        Assert.Equal("Sync", hub.LastMethod);
        var sig = Assert.IsType<SyncSignal>(hub.LastArg);
        Assert.Equal("diary", sig.Module);
        Assert.Equal("created", sig.Changes[0].Kind);
        Assert.True(sig.Changes[0].Id > 0);

        // 情侣 B 的变更：组必须切换到 B
        await using (var db = new CoupleDbContext(options, broadcaster))
        {
            CoupleContext.Current = CidB;
            db.Diaries.Add(new CoupleDiary { Title = "B-diary", Content = "c", CoupleId = CidB, CreateUserId = 2 });
            await db.SaveChangesAsync();
        }

        // 强隔离断言：完整广播序列恰好为 [A 组, B 组]，每个情侣变更只命中各自组
        Assert.Equal(new[] { "couple-" + CidA, "couple-" + CidB }, hub.AllGroups);
        Assert.Equal("couple-" + CidB, hub.LastGroup);
        var sigB = Assert.IsType<SyncSignal>(hub.LastArg);
        Assert.Equal("diary", sigB.Module);
        Assert.True(sigB.Changes[0].Id > 0);
        // 关键：B 的变更绝没有路由到 A 组
        Assert.NotEqual("couple-" + CidA, hub.LastGroup);
    }

    [Fact]
    public async Task 匿名变更_不向任何情侣组广播_无泄漏()
    {
        var hub = new RecordingHubContext();
        var broadcaster = new SyncBroadcaster(hub);
        var options = new DbContextOptionsBuilder<CoupleDbContext>()
            .UseInMemoryDatabase("bc-anon-" + Guid.NewGuid()).Options;

        await using var db = new CoupleDbContext(options, broadcaster);
        CoupleContext.Current = null; // 匿名（后台作业 / 种子场景）
        db.Diaries.Add(new CoupleDiary { Title = "anon", Content = "c", CoupleId = null });
        await db.SaveChangesAsync();

        // 设计意图：匿名（无情侣上下文）的保存不触发任何实时广播，
        // 因此绝不向 couple-A / couple-B 等真实情侣组推送（无泄漏）。
        Assert.Null(hub.LastGroup);
        Assert.Empty(hub.AllGroups);
    }
}
