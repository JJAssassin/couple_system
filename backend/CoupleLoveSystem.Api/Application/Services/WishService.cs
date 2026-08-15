using CoupleLoveSystem.Api;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Enums;
using CoupleLoveSystem.Infrastructure.Repositories;
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
    public async Task<PagedResult<WishDto>> ListAsync(int page, int pageSize, long currentUserId, CancellationToken ct = default)
    {
        var all = await _repo.Query()
            .OrderBy(w => w.Status).ThenBy(w => w.Priority).ThenByDescending(w => w.CreateTime)
            .ToListAsync(ct);

        var nameOf = (await _userRepo.Query().ToListAsync(ct))
            .ToDictionary(u => u.Id, u => u.NickName);

        var total = all.Count;
        var items = all.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(w => Map(w, nameOf.TryGetValue(w.ClaimUserId ?? 0, out var n) ? n : null))
            .ToList();

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
