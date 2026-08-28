using CoupleLoveSystem.Domain.Entities;
using System.Linq.Expressions;

namespace CoupleLoveSystem.Domain.Interfaces;

/// <summary>
/// 泛型仓储接口（领域层契约）。实现位于 Infrastructure.Repositories。
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    IQueryable<T> Query();
    Task<T?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void SoftDelete(T entity);   // 逻辑删除：IsDeleted = true
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
