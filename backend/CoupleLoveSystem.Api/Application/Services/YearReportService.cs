using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Core.Enums;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Infrastructure.Cache;
using CoupleLoveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoupleLoveSystem.Application.Services;

/// <summary>
/// 年度恋爱报告：把这一年两个人共同留下的痕迹聚合成结构化数据，供前端渲染「我们这一年」。
/// 所有计数均走全局 CoupleId 过滤器自动按当前情侣隔离，且只统计未软删除记录。
/// </summary>
public class YearReportService
{
    private readonly CoupleDbContext _db;
    private readonly ICacheService _cache;

    public YearReportService(CoupleDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    /// <summary>取指定年度报告（缓存 10 分钟，报告为只读聚合、低频访问）。</summary>
    public async Task<YearReportDto> GetYearReportAsync(int year, CancellationToken ct = default)
    {
        var cid = CoupleContext.Current ?? "anon";
        return await _cache.GetOrCreateAsync($"stats:yearreport:{cid}:{year}", TimeSpan.FromMinutes(10),
            _ => ComputeAsync(year, ct), ct);
    }

    /// <summary>取指定年度心情日历（缓存 10 分钟）。</summary>
    public async Task<MoodCalendarDto> GetMoodCalendarAsync(int year, CancellationToken ct = default)
    {
        var cid = CoupleContext.Current ?? "anon";
        return await _cache.GetOrCreateAsync($"stats:moodcalendar:{cid}:{year}", TimeSpan.FromMinutes(10),
            _ => ComputeMoodCalendarAsync(year, ct), ct);
    }

    private async Task<MoodCalendarDto> ComputeMoodCalendarAsync(int year, CancellationToken ct)
    {
        var start = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddYears(1);

        // 取该年度所有日记（按 DiaryDate 归属，无 DiaryDate 的跳过）
        var diaries = await _db.Diaries
            .Where(d => d.DiaryDate >= start && d.DiaryDate < end)
            .Select(d => new { d.DiaryDate, d.MoodScore, d.MoodTag })
            .ToListAsync(ct);

        // 按日期去重（同一日期多条取最后一条）
        var daily = diaries
            .GroupBy(d => d.DiaryDate!.Value.Date)
            .Select(g => g.Last())
            .OrderBy(d => d.DiaryDate)
            .ToList();

        var dto = new MoodCalendarDto { Year = year };
        dto.Days = daily.Select(d => new MoodDayDto
        {
            Date = d.DiaryDate!.Value.ToString("yyyy-MM-dd"),
            MoodScore = d.MoodScore,
            MoodTag = d.MoodTag,
        }).ToList();

        return dto;
    }

    private async Task<YearReportDto> ComputeAsync(int year, CancellationToken ct)
    {
        // CreateTime 均以 UTC 存储；年度区间 [year-01-01, year+1-01-01)
        var start = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddYears(1);
        var dto = new YearReportDto { Year = year };

        // ---- 感情基调 ----
        var setting = await _db.Settings.FirstOrDefaultAsync(s => s.Key == "global", ct);
        if (setting?.LoveStartTime is { } loveStart)
            dto.LoveDays = Math.Max(1, (int)(DateTime.UtcNow.Date - loveStart.Date).TotalDays + 1);
        dto.AnniversaryTotal = await _db.Anniversaries.CountAsync(ct);
        var anns = await _db.Anniversaries
            .Where(a => a.TargetDate >= start && a.TargetDate < end)
            .OrderBy(a => a.TargetDate)
            .ToListAsync(ct);
        dto.Anniversaries = anns
            .Select(a => new AnniversaryPassedDto { Name = a.Name, TargetDate = a.TargetDate })
            .ToList();

        // ---- 内容产出（年度新增） ----
        dto.DiaryCount = await _db.Diaries.CountAsync(d => d.CreateTime >= start && d.CreateTime < end, ct);
        dto.AvgMood = dto.DiaryCount == 0
            ? 0
            : Math.Round(await _db.Diaries
                .Where(d => d.CreateTime >= start && d.CreateTime < end)
                .AverageAsync(d => (double?)d.MoodScore, ct) ?? 0, 1);

        dto.WishCreated = await _db.Wishes.CountAsync(w => w.CreateTime >= start && w.CreateTime < end, ct);
        dto.WishDone = await _db.Wishes.CountAsync(
            w => w.CreateTime >= start && w.CreateTime < end && w.Status == WishStatus.Completed, ct);
        dto.TodoDone = await _db.Todos.CountAsync(
            t => t.CreateTime >= start && t.CreateTime < end && t.IsDone, ct);
        dto.ConflictCount = await _db.Conflicts.CountAsync(c => c.OccurTime >= start && c.OccurTime < end, ct);
        dto.ConflictResolved = await _db.Conflicts.CountAsync(
            c => c.OccurTime >= start && c.OccurTime < end && c.ReconcileTime != null, ct);
        dto.BoardCount = await _db.BoardMessages.CountAsync(b => b.CreateTime >= start && b.CreateTime < end, ct);
        dto.ImageCount = await _db.Images.CountAsync(i => i.CreateTime >= start && i.CreateTime < end, ct);
        dto.FootprintCount = await _db.Footprints.CountAsync(f => f.CreateTime >= start && f.CreateTime < end, ct);
        dto.DateCount = await _db.DateRecords.CountAsync(dr => dr.CreateTime >= start && dr.CreateTime < end, ct);
        dto.DateCompleted = await _db.DateRecords.CountAsync(
            dr => dr.CreateTime >= start && dr.CreateTime < end && dr.IsCompleted, ct);

        // ---- 默契 ----
        dto.QuizRounds = await _db.QuizRounds.CountAsync(q => q.CreateTime >= start && q.CreateTime < end, ct);
        dto.QuizRevealed = await _db.QuizRounds.CountAsync(
            q => q.CreateTime >= start && q.CreateTime < end && q.IsRevealed, ct);
        dto.QuizMatched = await _db.QuizRounds.CountAsync(
            q => q.CreateTime >= start && q.CreateTime < end && q.IsMatched, ct);
        dto.MatchRate = dto.QuizRevealed == 0
            ? 0
            : Math.Round((double)dto.QuizMatched / dto.QuizRevealed * 100, 1);

        // ---- 记账（按记账日期 RecordTime 归属年度） ----
        var accounts = await _db.AccountRecords
            .Where(a => a.RecordTime >= start && a.RecordTime < end)
            .ToListAsync(ct);
        dto.Income = accounts.Where(a => a.RecordType == AccountRecordType.Income).Sum(a => a.Amount);
        dto.Expense = accounts.Where(a => a.RecordType == AccountRecordType.Expend).Sum(a => a.Amount);

        // 月度收支：固定 12 个月补齐（前端画图省心）
        var monthly = new List<MonthlyFinanceDto>(12);
        for (var m = 1; m <= 12; m++)
        {
            var month = accounts.Where(a => a.RecordTime.Month == m).ToList();
            monthly.Add(new MonthlyFinanceDto
            {
                Month = $"{year}-{m:D2}",
                Income = month.Where(a => a.RecordType == AccountRecordType.Income).Sum(a => a.Amount),
                Expense = month.Where(a => a.RecordType == AccountRecordType.Expend).Sum(a => a.Amount),
            });
        }
        dto.MonthlyFinance = monthly;

        // 支出分类 top5
        dto.TopSpend = accounts
            .Where(a => a.RecordType == AccountRecordType.Expend)
            .GroupBy(a => string.IsNullOrWhiteSpace(a.Category) ? "其他" : a.Category)
            .Select(g => new CategorySpendDto { Category = g.Key, Amount = g.Sum(a => a.Amount) })
            .OrderByDescending(x => x.Amount)
            .Take(5)
            .ToList();

        // ---- 月度趋势 ----
        var moods = await _db.Diaries
            .Where(d => d.DiaryDate != null && d.DiaryDate >= start && d.DiaryDate < end)
            .ToListAsync(ct);
        var conflicts = await _db.Conflicts
            .Where(c => c.OccurTime >= start && c.OccurTime < end)
            .ToListAsync(ct);
        for (var m = 1; m <= 12; m++)
        {
            var ms = moods.Where(d => d.DiaryDate!.Value.Month == m).ToList();
            dto.MoodTrend.Add(new ChartPointDto
            {
                Label = $"{m}月",
                Value = ms.Count == 0 ? 0 : Math.Round(ms.Average(d => d.MoodScore), 1),
            });
            dto.ConflictTrend.Add(new ChartPointDto
            {
                Label = $"{m}月",
                Value = conflicts.Count(c => c.OccurTime.Month == m),
            });
        }

        return dto;
    }
}
