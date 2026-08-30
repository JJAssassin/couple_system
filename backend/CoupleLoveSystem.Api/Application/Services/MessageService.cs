using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CoupleLoveSystem.Application.Services;

public class MessageService
{
    private readonly IRepository<CoupleSystemMessage> _repo;
    private readonly CoupleDbContext _db;

    public MessageService(IRepository<CoupleSystemMessage> repo, CoupleDbContext db)
    {
        _repo = repo; _db = db;
    }

    public async Task<PagedResult<SystemMessageDto>> ListAsync(long currentUserId, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.SystemMessages.Where(m => m.ReceiverUserId == currentUserId);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(m => m.CreateTime)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<SystemMessageDto>
        {
            Items = items.Select(Map).ToList(),
            Total = total, Page = page, PageSize = pageSize
        };
    }

    public Task<int> UnreadCountAsync(long currentUserId, CancellationToken ct = default) =>
        _db.SystemMessages.CountAsync(m => m.ReceiverUserId == currentUserId && !m.IsRead, ct);

    public async Task<SystemMessageDto> ReadAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var m = await _db.SystemMessages.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("消息不存在");
        if (m.ReceiverUserId != currentUserId)
            throw new ForbiddenException("无权操作该消息");
        if (!m.IsRead)
        {
            m.IsRead = true;
            _db.SystemMessages.Update(m);
            await _db.SaveChangesAsync(ct);
        }
        return Map(m);
    }

    public async Task<int> ReadAllAsync(long currentUserId, CancellationToken ct = default)
    {
        // 一次性把当前用户全部未读消息标记为已读（解决分页仅标记已加载部分导致角标卡在 99+ 的问题）
        return await _db.SystemMessages
            .Where(m => m.ReceiverUserId == currentUserId && !m.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true), ct);
    }

    public async Task<int> DeleteReadAsync(long currentUserId, CancellationToken ct = default) =>
        await _db.SystemMessages
            .Where(m => m.ReceiverUserId == currentUserId && m.IsRead)
            .ExecuteDeleteAsync(ct);

    public async Task<int> BatchDeleteAsync(List<long> ids, long currentUserId, CancellationToken ct = default)
    {
        if (ids is null || ids.Count == 0) return 0;
        return await _db.SystemMessages
            .Where(m => ids.Contains(m.Id) && m.ReceiverUserId == currentUserId)
            .ExecuteDeleteAsync(ct);
    }

    private static SystemMessageDto Map(CoupleSystemMessage m) => new()
    {
        Id = m.Id,
        ReceiverUserId = m.ReceiverUserId,
        Title = m.Title,
        Content = m.Content,
        MessageType = m.MessageType,
        IsRead = m.IsRead,
        CreateTime = m.CreateTime
    };
}
