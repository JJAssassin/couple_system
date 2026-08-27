using System;
using System.Linq;
using System.Threading.Tasks;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Enums;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Repositories;
using CoupleLoveSystem.Tests.Infrastructure;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// HomeService 的集成测试（InMemory EF）。
/// 覆盖：仪表盘愿望完成率、共同余额汇总、恋爱时长计算（含未设置场景）。
/// </summary>
public class HomeServiceTests
{
    private static HomeService Build(out CoupleDbContext db)
    {
        db = TestDb.CreateInMemoryContext();
        return new HomeService(
            db,
            new AnniversaryRepository(db),
            new FakeCacheService());
    }

    [Fact]
    public async Task GetDashboardAsync_愿望完成率正确()
    {
        var svc = Build(out var db);
        db.Wishes.Add(new CoupleWish { WishType = WishType.Common, Title = "A", Status = WishStatus.Completed });
        db.Wishes.Add(new CoupleWish { WishType = WishType.Common, Title = "B", Status = WishStatus.NotStart });
        await db.SaveChangesAsync();

        var dash = await svc.GetDashboardAsync(1);
        Assert.Equal(50.0, dash.WishCompleteRate); // 1/2 = 50%
    }

    [Fact]
    public async Task GetDashboardAsync_无愿望时完成率为0()
    {
        var svc = Build(out _);
        var dash = await svc.GetDashboardAsync(1);
        Assert.Equal(0, dash.WishCompleteRate);
    }

    [Fact]
    public async Task GetDashboardAsync_共同余额汇总收入与支出()
    {
        var svc = Build(out var db);
        db.AccountRecords.Add(new CoupleAccountRecord { RecordType = AccountRecordType.Income, Category = "工资", Amount = 100m });
        db.AccountRecords.Add(new CoupleAccountRecord { RecordType = AccountRecordType.Expend, Category = "餐饮", Amount = 40m });
        await db.SaveChangesAsync();

        var dash = await svc.GetDashboardAsync(1);
        Assert.Equal(100m, dash.AccountSummary.Income);
        Assert.Equal(40m, dash.AccountSummary.Expend);
        Assert.Equal(60m, dash.AccountSummary.Balance);
    }

    [Fact]
    public async Task GetLoveInfoAsync_未设置相恋日期_未开始()
    {
        var svc = Build(out _);
        var info = await svc.GetLoveInfoAsync(1);
        Assert.False(info.HasLoveStart);
        Assert.Equal(0, info.TotalDays);
    }

    [Fact]
    public async Task GetLoveInfoAsync_已设置_整日正确()
    {
        var svc = Build(out var db);
        db.Settings.Add(new CoupleSetting { Key = "global", LoveStartTime = DateTime.Today.AddDays(-10) });
        await db.SaveChangesAsync();

        var info = await svc.GetLoveInfoAsync(1);
        Assert.True(info.HasLoveStart);
        Assert.Equal(10, info.TotalDays);
    }
}
