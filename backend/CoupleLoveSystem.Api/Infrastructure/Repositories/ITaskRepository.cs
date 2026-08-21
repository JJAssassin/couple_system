using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CoupleLoveSystem.Infrastructure.Repositories;

public interface ITaskRepository : IRepository<CoupleTaskTemplate>
{
    Task<PagedResult<CoupleTaskTemplate>> PagedTemplatesAsync(int page, int pageSize, bool? isActive = null, CancellationToken ct = default);
    Task<CoupleTaskTemplate?> GetSystemTemplateAsync(string title, CancellationToken ct = default);
}

public class TaskRepository : EfRepository<CoupleTaskTemplate>, ITaskRepository
{
    private readonly CoupleDbContext _db;
    public TaskRepository(CoupleDbContext db) : base(db) => _db = db;

    public async Task<PagedResult<CoupleTaskTemplate>> PagedTemplatesAsync(int page, int pageSize, bool? isActive = null, CancellationToken ct = default)
    {
        var query = _db.TaskTemplates.AsNoTracking();
        if (isActive.HasValue) query = query.Where(t => t.IsActive == isActive.Value);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(t => t.CreateTime)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<CoupleTaskTemplate> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public Task<CoupleTaskTemplate?> GetSystemTemplateAsync(string title, CancellationToken ct = default)
        => _db.TaskTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.TaskType == Core.Enums.TaskType.System && t.Title == title, ct);
}
