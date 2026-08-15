using CoupleLoveSystem.Application.Filters;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Enums;
using CoupleLoveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoupleLoveSystem.Application.Services;

public class TimelineService
{
    private readonly CoupleDbContext _db;

    public TimelineService(CoupleDbContext db) => _db = db;

    public async Task<List<TimelineItemDto>> ListAsync(int? year, int? month, long currentUserId, CancellationToken ct = default)
    {
        // 各表独立拉取（数据量小，内存聚合可接受）；日记按权限过滤（排除对方 PrivateSelf）
        var anniversaries = await _db.Anniversaries.ToListAsync(ct);
        var diaries = await PermissionFilter.WhereVisible(_db.Diaries, currentUserId).ToListAsync(ct);
        var wishes = await _db.Wishes.Where(w => w.Status == WishStatus.Completed).ToListAsync(ct);
        var conflicts = await _db.Conflicts.ToListAsync(ct);

        var items = new List<TimelineItemDto>();

        items.AddRange(anniversaries.Select(a => new TimelineItemDto
        {
            Id = a.Id,
            Type = "anniversary",
            Title = a.Name,
            Date = a.TargetDate,
            Summary = null,
            RelatedId = a.Id,
            IsYearly = a.IsYearly,
            NextOccurrence = a.ComputeNextOccurrence()
        }));

        items.AddRange(diaries.Select(d => new TimelineItemDto
        {
            Id = d.Id,
            Type = "diary",
            Title = d.Title,
            Date = d.DiaryDate ?? d.CreateTime,
            Summary = TrimSummary(d.Content, 50),
            RelatedId = d.Id
        }));

        items.AddRange(wishes.Select(w => new TimelineItemDto
        {
            Id = w.Id,
            Type = "wish",
            Title = w.Title,
            Date = w.CompleteTime ?? w.CreateTime,
            Summary = w.Description,
            RelatedId = w.Id
        }));

        items.AddRange(conflicts.Select(c => new TimelineItemDto
        {
            Id = c.Id,
            Type = "conflict",
            Title = c.Summary,
            Date = c.OccurTime,
            Summary = c.RuleConclusion,
            RelatedId = c.Id
        }));

        if (year.HasValue) items = items.Where(x => x.Date.Year == year.Value).ToList();
        if (month.HasValue) items = items.Where(x => x.Date.Month == month.Value).ToList();

        items.Sort((a, b) => b.Date.CompareTo(a.Date));
        return items;
    }

    private static string? TrimSummary(string? text, int max)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var clean = text.Replace("\r", " ").Replace("\n", " ").Trim();
        return clean.Length <= max ? clean : clean[..max] + "…";
    }
}
