using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Domain.Interfaces;
using System.Linq.Expressions;

namespace CoupleLoveSystem.Infrastructure.Repositories;

public class EfRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly CoupleDbContext _db;
    public EfRepository(CoupleDbContext db) => _db = db;

    public IQueryable<T> Query() => _db.Set<T>().AsNoTracking();
    public Task<T?> GetByIdAsync(long id, CancellationToken ct = default) =>
        _db.Set<T>().FirstOrDefaultAsync(e => e.Id == id, ct);
    public Task<List<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default) =>
        predicate == null ? _db.Set<T>().ToListAsync(ct) : _db.Set<T>().Where(predicate).ToListAsync(ct);

    public Task AddAsync(T entity, CancellationToken ct = default)
    {
        _db.Set<T>().Add(entity);
        return Task.CompletedTask;
    }
    public void Update(T entity) { entity.UpdateTime = DateTime.UtcNow; _db.Set<T>().Update(entity); }
    public void SoftDelete(T entity) { entity.IsDeleted = true; entity.UpdateTime = DateTime.UtcNow; _db.Set<T>().Update(entity); }
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
