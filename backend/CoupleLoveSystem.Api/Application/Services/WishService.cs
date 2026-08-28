using System.Collections.Generic;
using CoupleLoveSystem.Api;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Core.Enums;
using CoupleLoveSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CoupleLoveSystem.Application.Services;

public class WishService
{
    private readonly IRepository<CoupleWish> _repo;
    private readonly IRepository<CoupleUser> _userRepo;
    private readonly CoupleDbContext _db;

    public WishService(IRepository<CoupleWish> repo, IRepository<CoupleUser> userRepo, CoupleDbContext db)
    {
        _repo = repo; _userRepo = userRepo; _db = db;
    }

    /// <summary>许愿服务：愿望可由一方认领（ClaimUserName），记录认领用户 Users</summary>
    /// <summary>愿望列表：数据库端排序 + 分页；认领人昵称用子查询投影，避免全量加载用户表到内存。</summary>
    public async Task<PagedResult<WishDto>> ListAsync(int page, int pageSize, long currentUserId, CancellationToken ct = default)
    {
        var query = _db.Wishes.AsNoTracking()
            .OrderBy(w => w.Status).ThenBy(w => w.SortOrder).ThenByDescending(w => w.CreateTime);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new WishDto
            {
                Id = w.Id,
                WishType = w.WishType,
                Title = w.Title,
                Description = w.Description,
                ExpectTime = w.ExpectTime,
                Priority = w.Priority,
                Status = w.Status,
                ClaimUserId = w.ClaimUserId,
                ClaimUserName = _db.Users
                    .Where(u => w.ClaimUserId != null && u.Id == w.ClaimUserId)
                    .Select(u => u.NickName)
                    .FirstOrDefault(),
                CompleteTime = w.CompleteTime,
                CompleteRemark = w.CompleteRemark,
                CompleteImage = w.CompleteImage,
                CreateUserId = w.CreateUserId,
                CreateTime = w.CreateTime
            })
            .ToListAsync(ct);

        return new PagedResult<WishDto> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<WishDto> GetAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var w = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("愿望不存在");
        return Map(w, await ResolveNameAsync(w.ClaimUserId, ct));
    }

    public async Task<WishDto> CreateAsync(WishReq req, long currentUserId, CancellationToken ct = default)
    {
        var w = new CoupleWish
        {
            WishType = req.WishType,
            Title = req.Title,
            Description = req.Description,
            ExpectTime = req.ExpectTime,
            Priority = req.Priority,
            Status = WishStatus.NotStart,     // 新建愿望默认状态为未开始
            CreateUserId = currentUserId,     // 记录创建者，避免创作者信息丢失
            CreateTime = DateTime.UtcNow
        };
        await _repo.AddAsync(w, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(w, null);
    }

    public async Task<WishDto> UpdateAsync(long id, WishReq req, long currentUserId, CancellationToken ct = default)
    {
        var w = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("愿望不存在");
        w.WishType = req.WishType;
        w.Title = req.Title;
        w.Description = req.Description;
        w.ExpectTime = req.ExpectTime;
        w.Priority = req.Priority;
        w.Status = req.Status;
        w.UpdateUserId = currentUserId;
        _repo.Update(w);
        await _repo.SaveChangesAsync(ct);
        return Map(w, await ResolveNameAsync(w.ClaimUserId, ct));
    }

    public async Task DeleteAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var w = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("愿望不存在");
        _repo.SoftDelete(w);
        await _repo.SaveChangesAsync(ct);
    }

    /// <summary>认领愿望：设置 ClaimUserId 与 ClaimUserName，标记已由某人认领</summary>
    public async Task<WishDto> ClaimAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var w = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("愿望不存在");
        w.ClaimUserId = currentUserId;
        if (w.Status == WishStatus.NotStart) w.Status = WishStatus.Doing;
        w.UpdateUserId = currentUserId;
        _repo.Update(w);
        await _repo.SaveChangesAsync(ct);
        return Map(w, await ResolveNameAsync(w.ClaimUserId, ct));
    }

    /// <summary>更新愿望：支持修改标题 / 描述 / 状态等字段</summary>
    public async Task<WishDto> CompleteAsync(WishCompleteReq req, long currentUserId, CancellationToken ct = default)
    {
        var w = await _repo.GetByIdAsync(req.Id, ct) ?? throw new NotFoundException("愿望不存在");
        if (w.ClaimUserId == null) w.ClaimUserId = currentUserId;
        w.Status = WishStatus.Completed;
        w.CompleteTime = DateTime.UtcNow;
        w.CompleteRemark = req.CompleteRemark;
        w.CompleteImage = req.CompleteImage;
        w.UpdateUserId = currentUserId;
        _repo.Update(w);
        await _repo.SaveChangesAsync(ct);
        return Map(w, await ResolveNameAsync(w.ClaimUserId, ct));
    }

    /// <summary>拖拽排序：按传入的 id 顺序写入 SortOrder（只更新传入项；其余项顺序由列表查询的 SortOrder/CreateTime 兜底）。</summary>
    public async Task ReorderAsync(List<long> ids, long currentUserId, CancellationToken ct = default)
    {
        if (ids == null || ids.Count == 0) return;
        var items = await _repo.Query().Where(w => ids.Contains(w.Id)).ToListAsync(ct);
        var map = items.ToDictionary(w => w.Id);
        for (int i = 0; i < ids.Count; i++)
        {
            if (map.TryGetValue(ids[i], out var w))
            {
                w.SortOrder = i;
                w.UpdateUserId = currentUserId;
                _repo.Update(w);
            }
        }
        await _repo.SaveChangesAsync(ct);
    }

    private async Task<string?> ResolveNameAsync(long? userId, CancellationToken ct)
    {
        if (userId == null) return null;
        var u = await _userRepo.GetByIdAsync(userId.Value, ct);
        return u?.NickName;
    }

    private static WishDto Map(CoupleWish w, string? claimUserName) => new()
    {
        Id = w.Id,
        WishType = w.WishType,
        Title = w.Title,
        Description = w.Description,
        ExpectTime = w.ExpectTime,
        Priority = w.Priority,
        Status = w.Status,
        ClaimUserId = w.ClaimUserId,
        ClaimUserName = claimUserName,
        CompleteTime = w.CompleteTime,
        CompleteRemark = w.CompleteRemark,
        CompleteImage = w.CompleteImage,
        CreateUserId = w.CreateUserId,
        CreateTime = w.CreateTime
    };
}
