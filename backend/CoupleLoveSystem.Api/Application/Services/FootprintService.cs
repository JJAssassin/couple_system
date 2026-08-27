using CoupleLoveSystem.Api;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CoupleLoveSystem.Application.Services;

/// <summary>足迹 / 自定义计数卡：情侣共享，记录可 +1 的小确幸（抱抱 / 亲亲 / 一起看的电影…）。</summary>
public class FootprintService
{
    private readonly IRepository<CoupleFootprint> _repo;
    private readonly CoupleDbContext _db;

    public FootprintService(IRepository<CoupleFootprint> repo, CoupleDbContext db)
    {
        _repo = repo;
        _db = db;
    }

    private bool IsRelational => _db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";

    public async Task<List<FootprintDto>> ListAsync(CancellationToken ct = default)
    {
        var list = await _repo.Query().OrderByDescending(f => f.CreateTime).ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<FootprintDto> CreateAsync(FootprintReq req, long currentUserId, CancellationToken ct = default)
    {
        var title = (req.Title ?? string.Empty).Trim();
        if (title.Length == 0) throw new ConflictException("请填写足迹名称");
        var f = new CoupleFootprint
        {
            Title = title[..Math.Min(title.Length, 30)],
            Emoji = string.IsNullOrWhiteSpace(req.Emoji) ? "✨" : req.Emoji.Trim()[..Math.Min(req.Emoji.Trim().Length, 4)],
            Count = 0,
            TargetCount = req.TargetCount,
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim()[..Math.Min(req.Description.Trim().Length, 200)],
            CreateUserId = currentUserId,
            CreateTime = DateTime.UtcNow
        };
        await _repo.AddAsync(f, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(f);
    }

    public async Task DeleteAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var f = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("足迹不存在");
        _repo.SoftDelete(f);
        await _repo.SaveChangesAsync(ct);
    }

    /// <summary>修改已有足迹的属性（名称 / 图标 / 目标次数 / 说明），不动计数与最后记录时间。</summary>
    public async Task<FootprintDto> UpdateAsync(long id, FootprintReq req, long currentUserId, CancellationToken ct = default)
    {
        var f = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("足迹不存在");
        var title = (req.Title ?? string.Empty).Trim();
        if (title.Length == 0) throw new ConflictException("请填写足迹名称");
        f.Title = title[..Math.Min(title.Length, 30)];
        f.Emoji = string.IsNullOrWhiteSpace(req.Emoji) ? "✨" : req.Emoji.Trim()[..Math.Min(req.Emoji.Trim().Length, 4)];
        f.TargetCount = req.TargetCount;
        f.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim()[..Math.Min(req.Description.Trim().Length, 200)];
        f.UpdateUserId = currentUserId;
        f.UpdateTime = DateTime.UtcNow;
        _repo.Update(f);
        await _repo.SaveChangesAsync(ct);
        return Map(f);
    }

    /// <summary>记录一次：计数 +1，并刷新最后记录时间，驱动另一端实时跳动。
    /// 关系型库用 ExecuteUpdate 原子自增，避免「读-改-写」在并发 +1 时丢更新（审计 P2-14）；
    /// InMemory 测试库不支持批量更新，回退为读-改-写（测试无并发，仅保功能正确）。</summary>
    public async Task<FootprintDto> IncrementAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        if (IsRelational)
        {
            var affected = await _repo.Query()
                .Where(f => f.Id == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(f => f.Count, f => f.Count + 1)
                    .SetProperty(f => f.LastIncrementTime, DateTime.UtcNow)
                    .SetProperty(f => f.UpdateUserId, currentUserId), ct);
            if (affected == 0) throw new NotFoundException("足迹不存在");
        }
        else
        {
            var f = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("足迹不存在");
            f.Count += 1;
            f.LastIncrementTime = DateTime.UtcNow;
            f.UpdateUserId = currentUserId;
            _repo.Update(f);
            await _repo.SaveChangesAsync(ct);
        }
        var f2 = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("足迹不存在");
        return Map(f2);
    }

    private static FootprintDto Map(CoupleFootprint f) => new()
    {
        Id = f.Id,
        Title = f.Title,
        Emoji = f.Emoji,
        Count = f.Count,
        LastIncrementTime = f.LastIncrementTime,
        TargetCount = f.TargetCount,
        Description = f.Description,
        CreateUserId = f.CreateUserId,
        CreateTime = f.CreateTime
    };
}
