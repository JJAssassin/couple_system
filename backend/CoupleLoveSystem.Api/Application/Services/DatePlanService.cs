using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CoupleLoveSystem.Application.Services;

public class DatePlanService
{
    private readonly IRepository<CoupleDateRecord> _repo;
    private readonly CoupleDbContext _db;

    public DatePlanService(IRepository<CoupleDateRecord> repo, CoupleDbContext db)
    {
        _repo = repo; _db = db;
    }

    public async Task<PagedResult<DateRecordDto>> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.DateRecords.AsNoTracking().OrderByDescending(d => d.PlanTime);
        var total = await query.CountAsync(ct);
        var list = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<DateRecordDto>
        {
            Items = list.Select(Map).ToList(),
            Total = total, Page = page, PageSize = pageSize
        };
    }

    public async Task<DateRecordDto> GetAsync(long id, CancellationToken ct = default)
    {
        var d = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("约会记录不存在");
        return Map(d);
    }

    public async Task<DateRecordDto> CreateAsync(DateRecordReq req, long currentUserId, CancellationToken ct = default)
    {
        var d = new CoupleDateRecord
        {
            IsCompleted = req.IsCompleted,
            PlanTime = req.PlanTime,
            RealTime = req.IsCompleted ? (req.RealTime ?? DateTime.UtcNow) : req.RealTime,
            Location = req.Location,
            Budget = req.Budget,
            RealCost = req.RealCost,
            ExperienceScore = req.ExperienceScore,
            Remark = req.Remark,
            CreateUserId = currentUserId,
            CreateTime = DateTime.UtcNow
        };
        await _repo.AddAsync(d, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(d);
    }

    public async Task<DateRecordDto> UpdateAsync(long id, DateRecordReq req, long currentUserId, CancellationToken ct = default)
    {
        var d = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("约会记录不存在");
        d.IsCompleted = req.IsCompleted;
        d.PlanTime = req.PlanTime;
        // 标记完成时写实际时间：未给则补 UtcNow；显式传了则采用
        if (req.IsCompleted && d.RealTime == null) d.RealTime = req.RealTime ?? DateTime.UtcNow;
        else if (req.RealTime != null) d.RealTime = req.RealTime;
        d.Location = req.Location;
        d.Budget = req.Budget;
        d.RealCost = req.RealCost;
        d.ExperienceScore = req.ExperienceScore;
        d.Remark = req.Remark;
        d.UpdateUserId = currentUserId;
        d.UpdateTime = DateTime.UtcNow;
        _repo.Update(d);
        await _repo.SaveChangesAsync(ct);
        return Map(d);
    }

    public async Task DeleteAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var d = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("约会记录不存在");
        _repo.SoftDelete(d);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task<DateStatsDto> StatsAsync(CancellationToken ct = default)
    {
        var list = await _db.DateRecords.AsNoTracking().ToListAsync(ct);
        var completed = list.Where(d => d.IsCompleted).ToList();
        var total = completed.Count;
        var scored = completed.Where(d => d.ExperienceScore != null).ToList();
        var avg = scored.Count == 0 ? 0 : scored.Average(d => (double)d.ExperienceScore!.Value);
        return new DateStatsDto { TotalDates = total, AvgScore = Math.Round(avg, 1) };
    }

    public static DateRecordDto Map(CoupleDateRecord d) => new()
    {
        Id = d.Id,
        IsCompleted = d.IsCompleted,
        PlanTime = d.PlanTime,
        RealTime = d.RealTime,
        Location = d.Location,
        Budget = d.Budget,
        RealCost = d.RealCost,
        ExperienceScore = d.ExperienceScore,
        Remark = d.Remark,
        CreateUserId = d.CreateUserId,
        CreateTime = d.CreateTime
    };
}
