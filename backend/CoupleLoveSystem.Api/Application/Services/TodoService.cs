using CoupleLoveSystem.Api;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CoupleLoveSystem.Application.Services;

/// <summary>情侣共享待办清单：双方可添加、勾选完成、指派给对方，实时同步。</summary>
public class TodoService
{
    private readonly IRepository<CoupleTodo> _repo;
    private readonly IRepository<CoupleUser> _userRepo;

    public TodoService(IRepository<CoupleTodo> repo, IRepository<CoupleUser> userRepo)
    {
        _repo = repo; _userRepo = userRepo;
    }

    public async Task<PagedResult<TodoDto>> ListAsync(int page, int pageSize, long currentUserId, CancellationToken ct = default)
    {
        var all = await _repo.Query()
            .OrderBy(t => t.IsDone).ThenByDescending(t => t.Priority).ThenByDescending(t => t.CreateTime)
            .ToListAsync(ct);

        var nameOf = (await _userRepo.Query().ToListAsync(ct))
            .ToDictionary(u => u.Id, u => u.NickName);

        var total = all.Count;
        var items = all.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => Map(t,
                nameOf.TryGetValue(t.DoneUserId ?? 0, out var dn) ? dn : null,
                nameOf.TryGetValue(t.AssigneeUserId ?? 0, out var an) ? an : null))
            .ToList();

        return new PagedResult<TodoDto> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<TodoDto> GetAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var t = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("待办不存在");
        return Map(t, await ResolveNameAsync(t.DoneUserId, ct), await ResolveNameAsync(t.AssigneeUserId, ct));
    }

    public async Task<TodoDto> CreateAsync(TodoReq req, long currentUserId, CancellationToken ct = default)
    {
        var t = new CoupleTodo
        {
            Title = req.Title,
            Description = req.Description,
            Priority = req.Priority,
            DueTime = req.DueTime,
            Category = req.Category,
            AssigneeUserId = req.AssigneeUserId,
            CreateUserId = currentUserId,
            CreateTime = DateTime.UtcNow,
        };
        await _repo.AddAsync(t, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(t, null, await ResolveNameAsync(t.AssigneeUserId, ct));
    }

    public async Task<TodoDto> UpdateAsync(long id, TodoReq req, long currentUserId, CancellationToken ct = default)
    {
        var t = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("待办不存在");
        t.Title = req.Title;
        t.Description = req.Description;
        t.Priority = req.Priority;
        t.DueTime = req.DueTime;
        t.Category = req.Category;
        t.AssigneeUserId = req.AssigneeUserId;
        t.UpdateUserId = currentUserId;
        _repo.Update(t);
        await _repo.SaveChangesAsync(ct);
        return Map(t, await ResolveNameAsync(t.DoneUserId, ct), await ResolveNameAsync(t.AssigneeUserId, ct));
    }

    public async Task DeleteAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var t = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("待办不存在");
        _repo.SoftDelete(t);
        await _repo.SaveChangesAsync(ct);
    }

    /// <summary>勾选 / 取消完成：切换 IsDone，完成时记录完成人与时间，取消时清空。</summary>
    public async Task<TodoDto> ToggleAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var t = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("待办不存在");
        t.IsDone = !t.IsDone;
        t.DoneTime = t.IsDone ? DateTime.UtcNow : null;
        t.DoneUserId = t.IsDone ? currentUserId : null;
        t.UpdateUserId = currentUserId;
        _repo.Update(t);
        await _repo.SaveChangesAsync(ct);
        return Map(t, t.IsDone ? await ResolveNameAsync(currentUserId, ct) : null, await ResolveNameAsync(t.AssigneeUserId, ct));
    }

    /// <summary>指派责任人：设置 AssigneeUserId / AssigneeName（null 表示双方共同）。</summary>
    public async Task<TodoDto> AssignAsync(TodoAssignReq req, long currentUserId, CancellationToken ct = default)
    {
        var t = await _repo.GetByIdAsync(req.Id, ct) ?? throw new NotFoundException("待办不存在");
        t.AssigneeUserId = req.AssigneeUserId;
        t.UpdateUserId = currentUserId;
        _repo.Update(t);
        await _repo.SaveChangesAsync(ct);
        return Map(t, await ResolveNameAsync(t.DoneUserId, ct), await ResolveNameAsync(t.AssigneeUserId, ct));
    }

    private async Task<string?> ResolveNameAsync(long? userId, CancellationToken ct)
    {
        if (userId == null) return null;
        var u = await _userRepo.GetByIdAsync(userId.Value, ct);
        return u?.NickName;
    }

    private static TodoDto Map(CoupleTodo t, string? doneUserName, string? assigneeName) => new()
    {
        Id = t.Id,
        Title = t.Title,
        Description = t.Description,
        IsDone = t.IsDone,
        DoneTime = t.DoneTime,
        DoneUserId = t.DoneUserId,
        DoneUserName = doneUserName,
        Priority = t.Priority,
        DueTime = t.DueTime,
        Category = t.Category,
        AssigneeUserId = t.AssigneeUserId,
        AssigneeName = assigneeName,
        CreateUserId = t.CreateUserId,
        CreateTime = t.CreateTime,
    };
}
