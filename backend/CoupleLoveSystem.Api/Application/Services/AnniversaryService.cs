using CoupleLoveSystem.Api;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CoupleLoveSystem.Application.Services;

public class AnniversaryService
{
    private readonly IAnniversaryRepository _repo;
    private readonly CoupleDbContext _db;

    public AnniversaryService(IAnniversaryRepository repo, CoupleDbContext db)
    {
        _repo = repo; _db = db;
    }

    public async Task<PagedResult<AnniversaryDto>> ListAsync(int page, int pageSize, long currentUserId, CancellationToken ct = default)
    {
        var p = await _repo.PagedAsync(page, pageSize, currentUserId, ct);
        return new PagedResult<AnniversaryDto>
        {
            Items = p.Items.Select(HomeService.Map).ToList(),
            Total = p.Total, Page = p.Page, PageSize = p.PageSize
        };
    }

    public async Task<AnniversaryDto> GetAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var a = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("纪念日不存在");
        return HomeService.Map(a);
    }

    public async Task<AnniversaryDto> CreateAsync(AnniversaryReq req, long currentUserId, CancellationToken ct = default)
    {
        var a = new CoupleAnniversary
        {
            Name = req.Name, AnniversaryType = req.AnniversaryType, TargetDate = req.TargetDate,
            CoverImage = req.CoverImage, RemindDays = req.RemindDays, IsYearly = req.IsYearly,
            CreateUserId = currentUserId, CreateTime = DateTime.UtcNow,
            NextRemindTime = ComputeNextRemind(req.TargetDate, req.RemindDays, req.IsYearly)
        };
        await _repo.AddAsync(a, ct);
        await _repo.SaveChangesAsync(ct);
        return HomeService.Map(a);
    }

    public async Task<AnniversaryDto> UpdateAsync(long id, AnniversaryReq req, long currentUserId, CancellationToken ct = default)
    {
        var a = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("纪念日不存在");
        a.Name = req.Name; a.AnniversaryType = req.AnniversaryType; a.TargetDate = req.TargetDate;
        a.CoverImage = req.CoverImage; a.RemindDays = req.RemindDays; a.IsYearly = req.IsYearly;
        a.UpdateUserId = currentUserId; a.UpdateTime = DateTime.UtcNow;
        a.NextRemindTime = ComputeNextRemind(req.TargetDate, req.RemindDays, req.IsYearly);
        _repo.Update(a);
        await _repo.SaveChangesAsync(ct);
        return HomeService.Map(a);
    }

    public async Task DeleteAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var a = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("纪念日不存在");
        _repo.SoftDelete(a);
        await _repo.SaveChangesAsync(ct);
    }

    /// <summary>计算下次提醒时间：基于下一次实际发生日期 - 提前天数。
    /// 每年重复→下一次周年 - 提前天数；一次性且尚未过期→TargetDate - 提前天数；一次性且已过期→null（不再提醒）。</summary>
    private static DateTime? ComputeNextRemind(DateTime target, int remindDays, bool isYearly)
    {
        var temp = new CoupleAnniversary { TargetDate = target, IsYearly = isYearly };
        var next = temp.ComputeNextOccurrence();
        return next == null ? (DateTime?)null : next.Value.AddDays(-remindDays);
    }
}
