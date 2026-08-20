using System;
using System.Linq;
using System.Threading.Tasks;
using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Enums;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Repositories;
using CoupleLoveSystem.Tests.Infrastructure;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// AccountService 统计与导出测试（InMemory EF）。
/// 覆盖：近 6 月趋势正确性、当月收支、日期参数校验、CSV 导出格式（BOM/表头/转义）。
/// </summary>
public class AccountServiceTests
{
    private static AccountService Build(out CoupleDbContext db)
    {
        db = TestDb.CreateInMemoryContext();
        return new AccountService(new EfRepository<CoupleAccountRecord>(db), db);
    }

    private static void SeedRecord(CoupleDbContext db, AccountRecordType type, string category, decimal amount, int year, int month, int day = 15)
    {
        db.AccountRecords.Add(new CoupleAccountRecord
        {
            RecordType = type,
            Category = category,
            Amount = amount,
            RecordTime = new DateTime(year, month, day, 12, 0, 0),
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task StatisticsAsync_返回当月收支与近6月趋势()
    {
        var svc = Build(out var db);
        SeedRecord(db, AccountRecordType.Income, "工资", 8000, 2026, 3);
        SeedRecord(db, AccountRecordType.Expend, "餐饮", 300, 2026, 3);
        SeedRecord(db, AccountRecordType.Expend, "交通", 200, 2026, 4);
        SeedRecord(db, AccountRecordType.Income, "红包", 100, 2026, 8);
        SeedRecord(db, AccountRecordType.Expend, "餐饮", 520, 2026, 8);
        SeedRecord(db, AccountRecordType.Expend, "娱乐", 130, 2026, 8);

        var st = await svc.StatisticsAsync(2026, 8, 1);

        Assert.Equal(2026, st.Year);
        Assert.Equal(8, st.Month);
        Assert.Equal(100m, st.MonthIncome);
        Assert.Equal(650m, st.MonthExpense);
        // 趋势：2026-03 ~ 2026-08 共 6 个点
        Assert.Equal(6, st.Trend.Count);
        Assert.Equal("2026-03", st.Trend[0].Month);
        Assert.Equal("2026-08", st.Trend[5].Month);
        Assert.Equal(8000m, st.Trend[0].Income);
        Assert.Equal(300m, st.Trend[0].Expense);
        Assert.Equal(0m, st.Trend[1].Income);   // 2026-04 无收入
        Assert.Equal(200m, st.Trend[1].Expense);
        Assert.Equal(100m, st.Trend[5].Income);
        Assert.Equal(650m, st.Trend[5].Expense);
    }

    [Fact]
    public async Task StatisticsAsync_无数据月份_收支为0趋势仍6点()
    {
        var svc = Build(out _);
        var st = await svc.StatisticsAsync(2026, 8, 1);
        Assert.Equal(0m, st.MonthIncome);
        Assert.Equal(0m, st.MonthExpense);
        Assert.Equal(6, st.Trend.Count);
        Assert.All(st.Trend, t => Assert.Equal(0m, t.Income + t.Expense));
    }

    [Fact]
    public async Task StatisticsAsync_非法年月_抛出Conflict()
    {
        var svc = Build(out _);
        await Assert.ThrowsAsync<ConflictException>(() => svc.StatisticsAsync(1999, 8, 1));
        await Assert.ThrowsAsync<ConflictException>(() => svc.StatisticsAsync(2026, 13, 1));
    }

    [Fact]
    public void ExportCsv_带BOM与表头并按时间倒序()
    {
        var records = new[]
        {
            new CoupleAccountRecord { RecordType = AccountRecordType.Expend, Category = "餐饮", Amount = 12.5m, RecordTime = new DateTime(2026, 8, 1) },
            new CoupleAccountRecord { RecordType = AccountRecordType.Income, Category = "工资", Amount = 8000m, RecordTime = new DateTime(2026, 8, 10) },
        };
        var csv = AccountService.ExportCsv(2026, 8, records);

        Assert.StartsWith("\uFEFF", csv);                      // BOM
        Assert.StartsWith("日期,类型,分类,金额,备注", csv.TrimStart('\uFEFF'));
        // 倒序：8-10 的工资在前
        var lines = csv.TrimStart('\uFEFF').TrimEnd().Split('\n');
        Assert.Equal(3, lines.Length);                          // 表头 + 2 行
        Assert.Contains("2026-08-10,收入,工资,8000.00,", lines[1]);
        Assert.Contains("2026-08-01,支出,餐饮,12.50,", lines[2]);
    }

    [Fact]
    public void ExportCsv_备注与分类含逗号时转义()
    {
        var records = new[]
        {
            new CoupleAccountRecord { RecordType = AccountRecordType.Expend, Category = "餐饮,美食", Amount = 30m, Remark = "吃了顿好的,很开心\n下次再来", RecordTime = new DateTime(2026, 8, 5) },
        };
        var csv = AccountService.ExportCsv(2026, 8, records);

        // 逗号 → 中文逗号，换行 → 空格，保证单行 CSV 结构不被破坏
        Assert.Contains("餐饮，美食", csv);
        Assert.Contains("吃了顿好的，很开心 下次再来", csv);
        var lines = csv.TrimStart('\uFEFF').TrimEnd().Split('\n');
        Assert.Equal(2, lines.Length);                          // 表头 + 1 行（无被换行截断的行）
    }
}
