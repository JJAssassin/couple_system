using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CoupleLoveSystem.Application.Services;

/// <summary>每日一句温情语录：按 UTC 日期确定性地选出当天展示的一句（同一天所有人看到同一句，翻页稳定）。</summary>
public class QuoteService
{
    private static readonly DateTime Epoch = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly IRepository<CoupleQuote> _repo;

    public QuoteService(IRepository<CoupleQuote> repo)
    {
        _repo = repo;
    }

    public async Task<DailyQuoteDto> GetDailyAsync(CancellationToken ct = default)
    {
        var all = await _repo.Query().OrderBy(q => q.SortOrder).ThenBy(q => q.Id).ToListAsync(ct);
        if (all.Count == 0)
            return new DailyQuoteDto { Content = "今天也要好好相爱呀 💞" };

        var days = (int)(DateTime.UtcNow.Date - Epoch.Date).TotalDays;
        var idx = ((days % all.Count) + all.Count) % all.Count; // 防负，确定性的「每日一句」
        var q = all[idx];
        return new DailyQuoteDto { Content = q.Content, Author = q.Author };
    }
}
