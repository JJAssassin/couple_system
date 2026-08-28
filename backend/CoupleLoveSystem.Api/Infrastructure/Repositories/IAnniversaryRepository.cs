using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Domain.Interfaces;

namespace CoupleLoveSystem.Infrastructure.Repositories;

public class AnniversaryRepository : EfRepository<CoupleAnniversary>, IAnniversaryRepository
{
    private readonly CoupleDbContext _db;
    public AnniversaryRepository(CoupleDbContext db) : base(db) => _db = db;

    public async Task<PagedResult<CoupleAnniversary>> PagedAsync(int page, int pageSize, long currentUserId, CancellationToken ct = default)
    {
        var query = _db.Anniversaries.AsNoTracking().Where(a => a.CreateUserId == currentUserId || true); // 纪念日默认双方可见
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(a => a.TargetDate)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<CoupleAnniversary> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public Task<List<CoupleAnniversary>> NearestAsync(int take, CancellationToken ct = default)
    {
        // 按「下一次实际发生日期」排序；每年重复的 TargetDate 是过往日期，必须用 ComputeNextOccurrence 滚动。
        var result = _db.Anniversaries.AsNoTracking()
            .Where(a => !a.IsDeleted)
            .AsEnumerable()
            .Select(a => new { A = a, Next = a.ComputeNextOccurrence() })
            .Where(x => x.Next != null)
            .OrderBy(x => x.Next!.Value)
            .Take(take)
            .Select(x => x.A)
            .ToList();
        return Task.FromResult(result);
    }
}
