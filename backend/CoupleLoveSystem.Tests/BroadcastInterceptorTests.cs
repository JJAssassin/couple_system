using System.Linq;
using System.Threading.Tasks;
using CoupleLoveSystem.Api;
using CoupleLoveSystem.Api.Hubs;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// 验证 CoupleDbContext 保存重写里的自动广播（AOP，替代各 Service 手写 NotifyAsync）：
/// 1) 写入带 [Broadcast] 的实体后，自动经 SyncBroadcaster 向情侣组推送结构化增量信号；
/// 2) created 信号携带真实主键（PK 在 SaveChanges 后才落库，验证读取时机正确，id 不为 0）；
/// 3) 无情侣上下文（种子 / 后台场景）时不广播，避免噪声与越权。
/// </summary>
public class BroadcastInterceptorTests
{
    private static CoupleDbContext CreateContext(RecordingHubContext hub, string dbName)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHubContext<SyncHub>>(hub);
        services.AddScoped<SyncBroadcaster>();
        services.AddDbContext<CoupleDbContext>((_, opt) => opt.UseInMemoryDatabase(dbName));
        var sp = services.BuildServiceProvider();
        var scope = sp.CreateScope();
        return scope.ServiceProvider.GetRequiredService<CoupleDbContext>();
    }

    [Fact]
    public async Task SaveChanges_Broadcasts_Created_With_RealPrimaryKey()
    {
        var hub = new RecordingHubContext();
        var db = CreateContext(hub, "broadcast-created-" + System.Guid.NewGuid());

        CoupleContext.Current = "cid-X"; // 有情侣上下文才广播
        try
        {
            var wish = new CoupleWish { WishType = (WishType)0, Title = "integration-test" };
            db.Wishes.Add(wish);
            await db.SaveChangesAsync();

            // 广播应发生：目标组 = 当前情侣组，方法 = "Sync"，载荷 = SyncSignal
            Assert.Equal("couple-cid-X", hub.LastGroup);
            Assert.Equal("Sync", hub.LastMethod);
            var sig = Assert.IsType<SyncSignal>(hub.LastArg);
            Assert.Equal("wish", sig.Module);
            Assert.Single(sig.Changes);
            Assert.Equal("created", sig.Changes[0].Kind);

            // 关键：PK 在保存后才生成，重写必须在 base.SaveChanges 之后读取，id 应 > 0 且与实体一致
            Assert.True(sig.Changes[0].Id is > 0, "created 信号必须携带真实主键（>0），不应为 0");
            Assert.Equal(wish.Id, sig.Changes[0].Id);
        }
        finally
        {
            CoupleContext.Current = null;
        }
    }

    [Fact]
    public async Task SaveChanges_No_CoupleContext_Does_Not_Broadcast()
    {
        var hub = new RecordingHubContext();
        var db = CreateContext(hub, "broadcast-noctx-" + System.Guid.NewGuid());

        CoupleContext.Current = null; // 无情侣上下文（种子 / 后台）→ 不广播
        var wish = new CoupleWish { WishType = (WishType)0, Title = "seed" };
        db.Wishes.Add(wish);
        await db.SaveChangesAsync();

        Assert.Null(hub.LastArg); // 未广播
    }

    [Fact]
    public async Task SaveChanges_Broadcasts_Created_With_EntityPayload()
    {
        var hub = new RecordingHubContext();
        var db = CreateContext(hub, "broadcast-payload-" + System.Guid.NewGuid());

        CoupleContext.Current = "cid-P"; // 有情侣上下文才广播
        try
        {
            var wish = new CoupleWish { WishType = (WishType)0, Title = "payload-test" };
            db.Wishes.Add(wish);
            await db.SaveChangesAsync();

            var sig = Assert.IsType<SyncSignal>(hub.LastArg);
            var change = Assert.Single(sig.Changes);
            Assert.Equal("created", change.Kind);

            // 增量信号须携带实体标量投影，便于前端就地 upsert（不再整表重载）
            Assert.NotNull(change.Payload);
            var dict = Assert.IsType<System.Collections.Generic.Dictionary<string, object?>>(change.Payload);
            Assert.Equal("payload-test", dict["Title"]);
            Assert.Equal(wish.Id, dict["Id"]);
        }
        finally
        {
            CoupleContext.Current = null;
        }
    }

    [Fact]
    public async Task SaveChanges_Broadcasts_Updated_With_EntityPayload()
    {
        var hub = new RecordingHubContext();
        var db = CreateContext(hub, "broadcast-updated-" + System.Guid.NewGuid());

        CoupleContext.Current = "cid-U";
        try
        {
            var wish = new CoupleWish { WishType = (WishType)0, Title = "before" };
            db.Wishes.Add(wish);
            await db.SaveChangesAsync();

            wish.Title = "after";
            await db.SaveChangesAsync();

            var sig = Assert.IsType<SyncSignal>(hub.LastArg);
            var change = Assert.Single(sig.Changes);
            Assert.Equal("updated", change.Kind);

            // 更新信号同样携带最新标量投影（含改动后的值）
            Assert.NotNull(change.Payload);
            var dict = Assert.IsType<System.Collections.Generic.Dictionary<string, object?>>(change.Payload);
            Assert.Equal("after", dict["Title"]);
            Assert.Equal(wish.Id, dict["Id"]);
        }
        finally
        {
            CoupleContext.Current = null;
        }
    }
}
