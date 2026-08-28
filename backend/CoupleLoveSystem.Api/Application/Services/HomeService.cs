using CoupleLoveSystem.Core;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Infrastructure.Cache;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CoupleLoveSystem.Application.Services;

public class HomeService
{
    private readonly CoupleDbContext _db;
    private readonly IAnniversaryRepository _annRepo;
    private readonly ICacheService _cache;

    public HomeService(CoupleDbContext db, IAnniversaryRepository annRepo, ICacheService cache)
    {
        _db = db; _annRepo = annRepo; _cache = cache;
    }

    public async Task<LoveInfoDto> GetLoveInfoAsync(long currentUserId, CancellationToken ct = default)
    {
        var cid = CoupleContext.Current ?? "anon";
        return await _cache.GetOrCreateAsync("home:loveinfo:" + cid, TimeSpan.FromMinutes(5),
            _ => ComputeLoveInfoAsync(currentUserId, ct), ct);
    }

    public async Task<DashboardDataDto> GetDashboardAsync(long currentUserId, CancellationToken ct = default)
    {
        var cid = CoupleContext.Current ?? "anon";
        return await _cache.GetOrCreateAsync("home:dashboard:" + cid, TimeSpan.FromSeconds(90),
            _ => ComputeDashboardAsync(currentUserId, ct), ct);
    }

    public async Task<List<AnniversaryDto>> GetNearestAsync(int take, CancellationToken ct = default)
    {
        var cid = CoupleContext.Current ?? "anon";
        return await _cache.GetOrCreateAsync("home:nearest:" + cid + ":" + take, TimeSpan.FromMinutes(1),
            _ => ComputeNearestAsync(take), ct);
    }

    /// <summary>连续互动天数（缓存到当日午夜，跨天自动失效重算）。</summary>
    public async Task<int> GetActiveStreakAsync(long currentUserId, CancellationToken ct = default)
    {
        var cid = CoupleContext.Current ?? "anon";
        var todayKey = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var box = await _cache.GetOrCreateAsync("home:streak:" + cid + ":" + todayKey, UntilEndOfUtcDay(),
            async _ => new StreakBox(await ComputeStreakAsync(ct)), ct);
        return box.Days;
    }

    // ---------------- 缓存工厂（真正的计算） ----------------

    private async Task<LoveInfoDto> ComputeLoveInfoAsync(long currentUserId, CancellationToken ct)
    {
        // 相恋日期是情侣级共享设置，任一方设置双方生效；未设置时不显示虚假天数
        var setting = await _db.Settings.FirstOrDefaultAsync(s => s.Key == "global", ct);
        return LoveInfoCalculator.Compute(setting?.LoveStartTime, DateTime.Today, DateTime.Now);
    }

    private async Task<DashboardDataDto> ComputeDashboardAsync(long currentUserId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // 心情趋势：近 30 天按天聚合（SQL 端），避免全表加载日记
        var sinceMood = now.AddDays(-30);
        var moodRows = await _db.Diaries.AsNoTracking()
            .Where(d => d.DiaryDate != null && d.DiaryDate.Value >= sinceMood)
            .GroupBy(d => d.DiaryDate!.Value.Date)
            .Select(g => new { Day = g.Key, Avg = g.Average(x => x.MoodScore) })
            .ToListAsync(ct);
        var mood = moodRows
            .OrderBy(r => r.Day)
            .Select(r => new ChartPointDto { Label = r.Day.ToString("MM-dd"), Value = r.Avg })
            .ToList();

        // 矛盾趋势：近 6 月每月数量（SQL 端按 年/月 聚合）
        var sinceConflict = now.AddMonths(-6);
        var conflictRows = await _db.Conflicts.AsNoTracking()
            .Where(c => c.OccurTime >= sinceConflict)
            .GroupBy(c => new { c.OccurTime.Year, c.OccurTime.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Cnt = g.Count() })
            .ToListAsync(ct);
        var conflict = conflictRows
            .OrderBy(r => r.Year).ThenBy(r => r.Month)
            .Select(r => new ChartPointDto { Label = $"{r.Year:D4}-{r.Month:D2}", Value = r.Cnt })
            .ToList();

        // 愿望完成率：两次 COUNT（共享清单，数据量情侣级很小）
        var wishTotal = await _db.Wishes.AsNoTracking().CountAsync(ct);
        var wishDone = await _db.Wishes.AsNoTracking()
            .CountAsync(w => w.Status == WishStatus.Completed, ct);
        var rate = wishTotal == 0 ? 0 : (double)wishDone / wishTotal;

        // 共同余额：共享账本，SQL 端 SUM
        var acctAgg = await _db.AccountRecords.AsNoTracking()
            .GroupBy(_ => 0)
            .Select(g => new
            {
                Income = g.Sum(a => a.RecordType == AccountRecordType.Income ? a.Amount : 0m),
                Expend = g.Sum(a => a.RecordType == AccountRecordType.Expend ? a.Amount : 0m),
            })
            .FirstOrDefaultAsync(ct);
        var summary = new AccountSummaryDto
        {
            Income = acctAgg?.Income ?? 0m,
            Expend = acctAgg?.Expend ?? 0m
        };

        // 连续互动天数（独立缓存，按日刷新）
        var streak = await GetActiveStreakAsync(currentUserId, ct);

        return new DashboardDataDto
        {
            MoodTrend = mood, ConflictTrend = conflict,
            WishCompleteRate = Math.Round(rate * 100, 1), AccountSummary = summary,
            ActiveStreakDays = streak
        };
    }

    private async Task<List<AnniversaryDto>> ComputeNearestAsync(int take)
        => (await _annRepo.NearestAsync(take, default)).Select(Map).ToList();

    /// <summary>
    /// 连续互动天数：取近 400 天内各内容模块的产出日（日记用 DiaryDate，其余用 CreateTime/OccurTime），
    /// 合并为「活跃日」集合，从今天（若今天无记录则从昨天）起往前连续计数。
    /// </summary>
    private async Task<int> ComputeStreakAsync(CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddDays(-400);
        var days = new HashSet<DateOnly>();

        AddDates(await _db.Set<CoupleDiary>().Where(x => x.DiaryDate != null && x.DiaryDate >= since)
            .Select(x => x.DiaryDate!.Value).ToListAsync(ct), days);
        AddDates(await _db.Set<CoupleWish>().Where(x => x.CreateTime >= since)
            .Select(x => x.CreateTime).ToListAsync(ct), days);
        AddDates(await _db.Set<CoupleConflict>().Where(x => x.OccurTime >= since)
            .Select(x => x.OccurTime).ToListAsync(ct), days);
        AddDates(await _db.Set<CoupleFootprint>().Where(x => x.CreateTime >= since)
            .Select(x => x.CreateTime).ToListAsync(ct), days);
        AddDates(await _db.Set<CoupleAlbum>().Where(x => x.CreateTime >= since)
            .Select(x => x.CreateTime).ToListAsync(ct), days);
        AddDates(await _db.Set<CoupleImage>().Where(x => x.CreateTime >= since)
            .Select(x => x.CreateTime).ToListAsync(ct), days);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var d = days.Contains(today) ? today : today.AddDays(-1); // 今天还没记录不算断签
        var streak = 0;
        while (days.Contains(d))
        {
            streak++;
            d = d.AddDays(-1);
        }
        return streak;
    }

    private static void AddDates(IEnumerable<DateTime> source, HashSet<DateOnly> target)
    {
        foreach (var dt in source) target.Add(DateOnly.FromDateTime(dt));
    }

    public static AnniversaryDto Map(CoupleAnniversary a)
    {
        var next = a.ComputeNextOccurrence();
        int days = next == null ? 0 : (int)(next.Value.Date - DateTime.UtcNow.Date).TotalDays;
        if (days < 0) days = 0;
        return new AnniversaryDto
        {
            Id = a.Id, Name = a.Name, AnniversaryType = a.AnniversaryType,
            TargetDate = a.TargetDate, CoverImage = a.CoverImage, RemindDays = a.RemindDays,
            IsYearly = a.IsYearly, NextOccurrence = next,
            DaysLeft = days, CreateUserId = a.CreateUserId, CreateTime = a.CreateTime,
            LunarDate = LunarHelper.ToLunarString(a.TargetDate)
        };
    }

    /// <summary>返回「到今天 UTC 结束」的剩余时长，用于把按日缓存的 key 自动在跨天失效。</summary>
    private static TimeSpan UntilEndOfUtcDay()
    {
        var now = DateTime.UtcNow;
        var end = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59, DateTimeKind.Utc)
            .AddSeconds(1);
        return end - now;
    }

    /// <summary>缓存 API 要求引用类型，故将 int 天数装箱为类。</summary>
    private sealed record StreakBox(int Days);
}
