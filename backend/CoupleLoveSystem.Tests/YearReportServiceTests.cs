using System;
using System.Linq;
using System.Threading.Tasks;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Enums;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Tests.Infrastructure;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>YearReportService 集成测试（InMemory EF）：年度聚合计数、月度趋势、默契率、记账分类。</summary>
public class YearReportServiceTests
{
    private static YearReportService Build(out CoupleDbContext db)
    {
        db = TestDb.CreateInMemoryContext();
        return new YearReportService(db, new FakeCacheService());
    }

    [Fact]
    public async Task GetYearReportAsync_聚合各模块年度计数()
    {
        var svc = Build(out var db);
        var y2026 = new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);
        var y2025 = new DateTime(2025, 12, 31, 12, 0, 0, DateTimeKind.Utc);

        db.Settings.Add(new CoupleSetting { Key = "global", LoveStartTime = new DateTime(2020, 1, 1) });
        db.Diaries.Add(new CoupleDiary { Title = "d", Content = "x", MoodScore = 8, DiaryDate = y2026, CreateTime = y2026 });
        db.Diaries.Add(new CoupleDiary { Title = "d2", Content = "y", MoodScore = 6, CreateTime = y2025 }); // 去年，不应计入
        db.Wishes.Add(new CoupleWish { WishType = WishType.Common, Title = "w", Status = WishStatus.Completed, CreateTime = y2026 });
        db.Wishes.Add(new CoupleWish { WishType = WishType.Common, Title = "w2", Status = WishStatus.NotStart, CreateTime = y2025 });
        db.Todos.Add(new CoupleTodo { Title = "t", IsDone = true, CreateTime = y2026 });
        db.Conflicts.Add(new CoupleConflict { Summary = "c", OccurTime = y2026, ReconcileTime = y2026 });
        db.Conflicts.Add(new CoupleConflict { Summary = "c2", OccurTime = y2025 }); // 去年未和解
        db.BoardMessages.Add(new CoupleBoardMessage { Content = "b", CreateTime = y2026 });
        db.Images.Add(new CoupleImage { AlbumId = 1, ImagePath = "/x.png", CreateTime = y2026 });
        db.Footprints.Add(new CoupleFootprint { Title = "f", Count = 1, CreateTime = y2026 });
        db.DateRecords.Add(new CoupleDateRecord { IsCompleted = true, CreateTime = y2026 });
        db.QuizRounds.Add(new CoupleQuizRound { QuestionText = "q", IsRevealed = true, IsMatched = true, CreateTime = y2026 });
        db.QuizRounds.Add(new CoupleQuizRound { QuestionText = "q2", IsRevealed = true, IsMatched = false, CreateTime = y2026 });
        db.QuizRounds.Add(new CoupleQuizRound { QuestionText = "q3", IsRevealed = false, IsMatched = false, CreateTime = y2025 });
        db.AccountRecords.Add(new CoupleAccountRecord { Category = "餐饮", Amount = 100, RecordType = AccountRecordType.Expend, RecordTime = y2026 });
        db.AccountRecords.Add(new CoupleAccountRecord { Category = "餐饮", Amount = 50, RecordType = AccountRecordType.Expend, RecordTime = y2026 });
        db.AccountRecords.Add(new CoupleAccountRecord { Category = "工资", Amount = 5000, RecordType = AccountRecordType.Income, RecordTime = y2026 });
        db.AccountRecords.Add(new CoupleAccountRecord { Category = "购物", Amount = 200, RecordType = AccountRecordType.Expend, RecordTime = y2025 }); // 去年
        db.Anniversaries.Add(new CoupleAnniversary { Name = "在一起", TargetDate = new DateTime(2026, 5, 20), IsYearly = false });
        await db.SaveChangesAsync();

        var r = await svc.GetYearReportAsync(2026);

        // 内容产出
        Assert.Equal(1, r.DiaryCount);
        Assert.Equal(8.0, r.AvgMood);
        Assert.Equal(1, r.WishCreated);
        Assert.Equal(1, r.WishDone);
        Assert.Equal(1, r.TodoDone);
        Assert.Equal(1, r.ConflictCount);      // 去年那条不计入
        Assert.Equal(1, r.ConflictResolved);   // 已和解 1 条
        Assert.Equal(1, r.BoardCount);
        Assert.Equal(1, r.ImageCount);
        Assert.Equal(1, r.FootprintCount);
        Assert.Equal(1, r.DateCount);
        Assert.Equal(1, r.DateCompleted);

        // 默契
        Assert.Equal(2, r.QuizRounds);         // 去年那条不计入
        Assert.Equal(2, r.QuizRevealed);
        Assert.Equal(1, r.QuizMatched);
        Assert.Equal(50.0, r.MatchRate);       // 1/2

        // 记账
        Assert.Equal(5000m, r.Income);
        Assert.Equal(150m, r.Expense);
        Assert.Equal(1, r.TopSpend.Count); // 两条「餐饮」按分类合并为一组
        Assert.Equal("餐饮", r.TopSpend[0].Category);
        Assert.Equal(150m, r.TopSpend[0].Amount);

        // 结构：12 个月固定补齐
        Assert.Equal(12, r.MonthlyFinance.Count);
        Assert.Equal(5000m, r.MonthlyFinance[4].Income);   // 5 月
        Assert.Equal(150m, r.MonthlyFinance[4].Expense);
        Assert.Equal(12, r.MoodTrend.Count);
        Assert.Equal(8.0, r.MoodTrend[4].Value);           // 5 月平均心情

        // 纪念日 + 恋爱天数
        Assert.Equal(1, r.Anniversaries.Count);
        Assert.Equal("在一起", r.Anniversaries[0].Name);
        Assert.True(r.AnniversaryTotal >= 1);
        Assert.True(r.LoveDays >= 2300); // 2020-01-01 至今
    }

    [Fact]
    public async Task GetYearReportAsync_无数据年份返回空结构()
    {
        var svc = Build(out var db);
        var r = await svc.GetYearReportAsync(1999);
        Assert.Equal(0, r.DiaryCount);
        Assert.Equal(0, r.AvgMood);
        Assert.Equal(0, r.QuizRounds);
        Assert.Equal(0m, r.Income);
        Assert.Equal(0, r.TopSpend.Count);
        Assert.Equal(12, r.MonthlyFinance.Count); // 结构仍补齐 12 个月
        Assert.Equal(12, r.MoodTrend.Count);      // 趋势同样固定 12 个月
        Assert.Equal(0, r.Anniversaries.Count);
    }

    [Fact]
    public async Task GetMoodCalendarAsync_返回指定年度每日心情()
    {
        var svc = Build(out var db);
        var d1 = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);
        var d2 = new DateTime(2026, 8, 11, 0, 0, 0, DateTimeKind.Utc);
        db.Diaries.Add(new CoupleDiary { Title = "a", Content = "x", MoodScore = 9, MoodTag = "开心", DiaryDate = d1, CreateTime = d1 });
        db.Diaries.Add(new CoupleDiary { Title = "b", Content = "y", MoodScore = 3, MoodTag = "低落", DiaryDate = d2, CreateTime = d2 });
        // 无 DiaryDate 的不计入
        db.Diaries.Add(new CoupleDiary { Title = "c", Content = "z", MoodScore = 7, CreateTime = d1 });
        await db.SaveChangesAsync();

        var r = await svc.GetMoodCalendarAsync(2026);

        Assert.Equal(2026, r.Year);
        Assert.Equal(2, r.Days.Count);
        Assert.Contains(r.Days, x => x.Date == "2026-08-10" && x.MoodScore == 9 && x.MoodTag == "开心");
        Assert.Contains(r.Days, x => x.Date == "2026-08-11" && x.MoodScore == 3 && x.MoodTag == "低落");
    }

    [Fact]
    public async Task GetMoodCalendarAsync_无日记返回空()
    {
        var svc = Build(out _);
        var r = await svc.GetMoodCalendarAsync(1999);
        Assert.Equal(1999, r.Year);
        Assert.Empty(r.Days);
    }
}
