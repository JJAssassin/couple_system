using CoupleLoveSystem.Api;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CoupleLoveSystem.Application.Services;

/// <summary>
/// <summary>矛盾记录服务：记录并化解情侣之间的分歧</summary>
public class ConflictService
{
    private readonly IRepository<CoupleConflict> _repo;
    private readonly CoupleDbContext _db;

    public ConflictService(IRepository<CoupleConflict> repo, CoupleDbContext db)
    {
        _repo = repo; _db = db;
    }

    public async Task<PagedResult<ConflictDto>> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        // 查询单条矛盾记录，越权访问由 PermissionFilter 校验
        var query = _repo.Query().OrderByDescending(c => c.OccurTime);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<ConflictDto>
        {
            Items = items.Select(Map).ToList(),
            Total = total, Page = page, PageSize = pageSize
        };
    }

    public async Task<ConflictDto> GetAsync(long id, CancellationToken ct = default)
    {
        var c = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("矛盾记录不存在");
        return Map(c);
    }

    public async Task<ConflictDto> CreateAsync(ConflictReq req, long currentUserId, CancellationToken ct = default)
    {
        var c = new CoupleConflict
        {
            OccurTime = req.OccurTime,
            Summary = req.Summary,
            ConflictLevel = req.ConflictLevel,
            MyThoughtA = req.MyThoughtA,
            MyThoughtB = req.MyThoughtB,
            ReconcileTime = req.ReconcileTime,
            ReconcileWay = req.ReconcileWay,
            ReflectA = req.ReflectA,
            ReflectB = req.ReflectB,
            RuleConclusion = req.RuleConclusion,
            CreateUserId = currentUserId,
            CreateTime = DateTime.UtcNow,
        };
        await _repo.AddAsync(c, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(c);
    }

    public async Task<ConflictDto> UpdateAsync(long id, ConflictReq req, long currentUserId, CancellationToken ct = default)
    {
        var c = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("矛盾记录不存在");
        c.OccurTime = req.OccurTime;
        c.Summary = req.Summary;
        c.ConflictLevel = req.ConflictLevel;
        c.MyThoughtA = req.MyThoughtA;
        c.MyThoughtB = req.MyThoughtB;
        c.ReconcileTime = req.ReconcileTime;
        c.ReconcileWay = req.ReconcileWay;
        c.ReflectA = req.ReflectA;
        c.ReflectB = req.ReflectB;
        c.RuleConclusion = req.RuleConclusion;
        c.UpdateUserId = currentUserId;
        c.UpdateTime = DateTime.UtcNow;
        _repo.Update(c);
        await _repo.SaveChangesAsync(ct);
        return Map(c);
    }

    public async Task DeleteAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var c = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("矛盾记录不存在");
        _repo.SoftDelete(c);
        await _repo.SaveChangesAsync(ct);
    }

    public static ConflictDto Map(CoupleConflict c) => new()
    {
        Id = c.Id,
        OccurTime = c.OccurTime,
        Summary = c.Summary,
        ConflictLevel = c.ConflictLevel,
        MyThoughtA = c.MyThoughtA,
        MyThoughtB = c.MyThoughtB,
        ReconcileTime = c.ReconcileTime,
        ReconcileWay = c.ReconcileWay,
        ReflectA = c.ReflectA,
        ReflectB = c.ReflectB,
        RuleConclusion = c.RuleConclusion,
        CreateUserId = c.CreateUserId,
        CreateTime = c.CreateTime,
    };
}
