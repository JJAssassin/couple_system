using System;
using System.Linq;
using System.Threading.Tasks;
using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Core.Enums;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Repositories;
using CoupleLoveSystem.Tests.Infrastructure;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// BudgetService 集成测试（InMemory EF）。
/// 覆盖预算合法性校验、总/分类预算 upsert、当月收支汇总、总预算与分类超支判定。
/// </summary>
public class BudgetServiceTests
{
    private static BudgetService Build(out CoupleDbContext db)
    {
        db = TestDb.CreateInMemoryContext();
        return new BudgetService(new EfRepository<CoupleBudget>(db), db);
    }

    private static void SeedRecord(CoupleDbContext db, AccountRecordType type, string category, decimal amount, int year, int month)
    {
        db.AccountRecords.Add(new CoupleAccountRecord
        {
            RecordType = type,
            Category = category,
            Amount = amount,
            RecordTime = new DateTime(year, month, 15, 12, 0, 0),
        });
        db.SaveChangesAsync().Wait();
    }

    [Fact]
    public async Task SetAsync_年份不合法_抛出Conflict()
    {
        var svc = Build(out _);
        await Assert.ThrowsAsync<ConflictException>(() => svc.SetAsync(
            new BudgetSetReq { Year = 1990, Month = 8, LimitAmount = 100 }, 1));
    }

    [Fact]
    public async Task SetAsync_月份不合法_抛出Conflict()
    {
        var svc = Build(out _);
        await Assert.ThrowsAsync<ConflictException>(() => svc.SetAsync(
            new BudgetSetReq { Year = 2026, Month = 13, LimitAmount = 100 }, 1));
    }

    [Fact]
    public async Task SetAsync_金额为零或负_抛出Conflict()
    {
        var svc = Build(out _);
        await Assert.ThrowsAsync<ConflictException>(() => svc.SetAsync(
            new BudgetSetReq { Year = 2026, Month = 8, LimitAmount = 0 }, 1));
        await Assert.ThrowsAsync<ConflictException>(() => svc.SetAsync(
            new BudgetSetReq { Year = 2026, Month = 8, LimitAmount = -5 }, 1));
    }

    [Fact]
    public async Task SetAsync_新建总预算_分类为空()
    {
        var svc = Build(out _);
        var b = await svc.SetAsync(new BudgetSetReq { Year = 2026, Month = 8, LimitAmount = 5000 }, 1);
        Assert.Null(b.Category);
        Assert.Equal(5000, b.LimitAmount);
    }

    [Fact]
    public async Task SetAsync_同一月同分类_覆盖金额而非新增()
    {
        var svc = Build(out _);
        var first = await svc.SetAsync(new BudgetSetReq { Year = 2026, Month = 8, LimitAmount = 1000 }, 1);
        var again = await svc.SetAsync(new BudgetSetReq { Year = 2026, Month = 8, LimitAmount = 2000 }, 1);

        Assert.Equal(first.Id, again.Id);
        Assert.Equal(2000, again.LimitAmount);
        var list = await svc.ListAsync(2026, 8);
        Assert.Single(list);
    }

    [Fact]
    public async Task SetAsync_分类预算可独立设置()
    {
        var svc = Build(out _);
        var total = await svc.SetAsync(new BudgetSetReq { Year = 2026, Month = 8, LimitAmount = 5000 }, 1);
        var cat = await svc.SetAsync(new BudgetSetReq { Year = 2026, Month = 8, Category = "餐饮", LimitAmount = 800 }, 1);

        Assert.Null(total.Category);
        Assert.Equal("餐饮", cat.Category);
        var list = await svc.ListAsync(2026, 8);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task DeleteAsync_不存在_抛出NotFound()
    {
        var svc = Build(out _);
        await Assert.ThrowsAsync<NotFoundException>(() => svc.DeleteAsync(999, 1));
    }

    [Fact]
    public async Task GetMonthlyAsync_收支汇总与总预算超支()
    {
        var svc = Build(out var db);
        SeedRecord(db, AccountRecordType.Income, "工资", 1000, 2026, 8);
        SeedRecord(db, AccountRecordType.Expend, "餐饮", 400, 2026, 8);
        SeedRecord(db, AccountRecordType.Expend, "购物", 300, 2026, 8);
        SeedRecord(db, AccountRecordType.Expend, "餐饮", 200, 2026, 7); // 其他月，应排除

        await svc.SetAsync(new BudgetSetReq { Year = 2026, Month = 8, LimitAmount = 500 }, 1);

        var m = await svc.GetMonthlyAsync(2026, 8);

        Assert.Equal(1000, m.Income);
        Assert.Equal(700, m.Expense); // 仅 8 月：400+300
        Assert.Equal(500, m.TotalBudget);
        Assert.Equal(-200, m.Remaining);
        Assert.True(m.IsOverspent);
    }

    [Fact]
    public async Task GetMonthlyAsync_分类明细与分类超支判定()
    {
        var svc = Build(out var db);
        SeedRecord(db, AccountRecordType.Expend, "餐饮", 400, 2026, 8);
        SeedRecord(db, AccountRecordType.Expend, "购物", 300, 2026, 8);

        await svc.SetAsync(new BudgetSetReq { Year = 2026, Month = 8, Category = "餐饮", LimitAmount = 350 }, 1);
        await svc.SetAsync(new BudgetSetReq { Year = 2026, Month = 8, Category = "购物", LimitAmount = 500 }, 1);

        var m = await svc.GetMonthlyAsync(2026, 8);

        // 分类按文化相关字符串排序，不依赖拼音顺序，故按名查找断言
        Assert.Equal(2, m.Categories.Count);
        var canyin = m.Categories.First(c => c.Category == "餐饮");
        var gouwu = m.Categories.First(c => c.Category == "购物");
        Assert.Equal("餐饮", canyin.Category);
        Assert.Equal(400, canyin.Amount);
        Assert.Equal(350, canyin.Budget);
        Assert.True(canyin.IsOverspent);
        Assert.Equal("购物", gouwu.Category);
        Assert.Equal(300, gouwu.Amount);
        Assert.Equal(500, gouwu.Budget);
        Assert.False(gouwu.IsOverspent);
    }

    [Fact]
    public async Task GetMonthlyAsync_无预算_总预算为空且不超支()
    {
        var svc = Build(out var db);
        SeedRecord(db, AccountRecordType.Expend, "餐饮", 999, 2026, 8);

        var m = await svc.GetMonthlyAsync(2026, 8);

        Assert.Null(m.TotalBudget);
        Assert.False(m.IsOverspent);
        Assert.Equal(0, m.Remaining);
        Assert.Equal(999, m.Expense);
    }

    [Fact]
    public async Task ListAsync_总预算排在分类预算之前()
    {
        var svc = Build(out _);
        await svc.SetAsync(new BudgetSetReq { Year = 2026, Month = 8, Category = "餐饮", LimitAmount = 800 }, 1);
        await svc.SetAsync(new BudgetSetReq { Year = 2026, Month = 8, LimitAmount = 5000 }, 1);

        var list = await svc.ListAsync(2026, 8);
        Assert.Equal(2, list.Count);
        Assert.Null(list[0].Category); // 总预算在前
        Assert.Equal("餐饮", list[1].Category);
    }
}
