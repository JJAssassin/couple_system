using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Domain.Entities;

namespace CoupleLoveSystem.Domain.Interfaces;

public interface IAnniversaryRepository : IRepository<CoupleAnniversary>
{
    Task<PagedResult<CoupleAnniversary>> PagedAsync(int page, int pageSize, long currentUserId, CancellationToken ct = default);
    Task<List<CoupleAnniversary>> NearestAsync(int take, CancellationToken ct = default);
}
