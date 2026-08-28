using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Domain.Entities;
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

    [Fact]
    public void ParseCsv_应用导出格式可原样解析()
    {
        var records = new[]
        {
            new CoupleAccountRecord { RecordType = AccountRecordType.Income, Category = "工资", Amount = 8000m, RecordTime = new DateTime(2026, 8, 10), Remark = "月薪" },
            new CoupleAccountRecord { RecordType = AccountRecordType.Expend, Category = "餐饮", Amount = 12.5m, RecordTime = new DateTime(2026, 8, 1) },
        };
        var csv = AccountService.ExportCsv(2026, 8, records);
        var rows = AccountService.ParseCsv(csv);

        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.True(r.Valid));
        // 导出按时间倒序：首行应为 8-10 的工资
        Assert.Equal(new DateTime(2026, 8, 10), rows[0].RecordTime);
        Assert.Equal(AccountRecordType.Income, rows[0].RecordType);
        Assert.Equal("工资", rows[0].Category);
        Assert.Equal(8000m, rows[0].Amount);
        Assert.Equal("月薪", rows[0].Remark);
        Assert.Equal(AccountRecordType.Expend, rows[1].RecordType);
        Assert.Equal("餐饮", rows[1].Category);
        Assert.Equal(12.5m, rows[1].Amount);
    }

    [Fact]
    public void ParseCsv_非法行被标记无效并附原因()
    {
        var csv = "日期,类型,分类,金额,备注\n" +
                  "2026-08-01,收入,工资,8000,正常\n" +
                  "不是日期,收入,工资,8000,日期坏\n" +
                  "2026-08-02,未知,餐饮,30,类型坏\n" +
                  "2026-08-03,支出,餐饮,0,金额非正\n" +
                  "2026-08-04,支出,餐饮,-5,金额负";
        var rows = AccountService.ParseCsv(csv);

        Assert.Equal(5, rows.Count);
        Assert.True(rows[0].Valid);
        Assert.False(rows[1].Valid); Assert.Contains("日期", rows[1].Error ?? "");
        Assert.False(rows[2].Valid); Assert.Contains("类型", rows[2].Error ?? "");
        Assert.False(rows[3].Valid); Assert.Contains("金额", rows[3].Error ?? "");
        Assert.False(rows[4].Valid);
    }

    [Fact]
    public async Task ImportAsync_导入有效行且重复导入被跳过()
    {
        var svc = Build(out var db);
        var csv = "日期,类型,分类,金额,备注\n" +
                  "2026-08-01,收入,工资,8000,月薪\n" +
                  "2026-08-02,支出,餐饮,50,午饭\n" +
                  "2026-08-03,支出,交通,20,地铁";
        var first = await svc.ImportAsync(csv, 1, CancellationToken.None);
        Assert.Equal(3, first.Total);
        Assert.Equal(3, first.Imported);
        Assert.Equal(0, first.Skipped);
        Assert.Equal(0, first.Failed);
        Assert.Equal(3, db.AccountRecords.Count());

        var second = await svc.ImportAsync(csv, 1, CancellationToken.None);
        Assert.Equal(3, second.Skipped);
        Assert.Equal(0, second.Imported);
        Assert.Equal(3, db.AccountRecords.Count()); // 不翻倍
    }

    [Fact]
    public async Task ImportAsync_仅插入有效行并报告失败行()
    {
        var svc = Build(out var db);
        var csv = "日期,类型,分类,金额,备注\n" +
                  "2026-08-01,收入,工资,8000,正常\n" +
                  "坏日期,收入,工资,100,日期错";
        var res = await svc.ImportAsync(csv, 1, CancellationToken.None);
        Assert.Equal(2, res.Total);
        Assert.Equal(1, res.Imported);
        Assert.Equal(1, res.Failed);
        Assert.Single(res.Errors);
        Assert.Equal(1, db.AccountRecords.Count());
    }

    [Fact]
    public void ParseCsv_兼容银行流水表头与千位逗号()
    {
        var csv = "交易日期,收支类型,分类,金额,摘要\n" +
                  "2026/08/01,收入,工资,8000.00,月薪\n" +
                  "2026/08/02,支出,餐饮,\"1,250.50\",午饭";
        var rows = AccountService.ParseCsv(csv);
        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.True(r.Valid));
        Assert.Equal(new DateTime(2026, 8, 1), rows[0].RecordTime);
        Assert.Equal(AccountRecordType.Income, rows[0].RecordType);
        Assert.Equal(8000m, rows[0].Amount);
        Assert.Equal(new DateTime(2026, 8, 2), rows[1].RecordTime);
        Assert.Equal(1250.5m, rows[1].Amount);
        Assert.Equal("午饭", rows[1].Remark);
    }

    [Fact]
    public async Task ListAsync_按收支类型过滤()
    {
        var svc = Build(out var db);
        SeedRecord(db, AccountRecordType.Income, "工资", 8000, 2026, 8);
        SeedRecord(db, AccountRecordType.Expend, "餐饮", 300, 2026, 8);
        SeedRecord(db, AccountRecordType.Expend, "交通", 200, 2026, 8);

        var all = await svc.ListAsync(1, 20, 1, null, CancellationToken.None);
        Assert.Equal(3, all.Total);
        Assert.Equal(3, all.Items.Count);

        var income = await svc.ListAsync(1, 20, 1, (int)AccountRecordType.Income, CancellationToken.None);
        Assert.Equal(1, income.Total);
        Assert.All(income.Items, i => Assert.Equal(AccountRecordType.Income, i.RecordType));

        var expend = await svc.ListAsync(1, 20, 1, (int)AccountRecordType.Expend, CancellationToken.None);
        Assert.Equal(2, expend.Total);
        Assert.All(expend.Items, i => Assert.Equal(AccountRecordType.Expend, i.RecordType));
    }
}
