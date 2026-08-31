using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

    /// <summary>切换某用户对某条消息的某表情反应：已点则取消，未点则加上。返回最新消息。</summary>
    public async Task<SystemMessageDto> ToggleReactionAsync(long id, string emojiKey, long currentUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(emojiKey)) throw new ArgumentException("表情标识不能为空");
        var m = await _db.SystemMessages.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("消息不存在");
        if (m.ReceiverUserId != currentUserId)
            throw new ForbiddenException("无权操作该消息");
        var map = ParseReactions(m.Reactions);
        if (!map.TryGetValue(emojiKey, out var list) || list is null)
        {
            list = new List<long>();
            map[emojiKey] = list;
        }
        if (list.Contains(currentUserId)) list.Remove(currentUserId);
        else list.Add(currentUserId);
        if (list.Count == 0) map.Remove(emojiKey);
        m.Reactions = SerializeReactions(map);
        m.UpdateUserId = currentUserId;
        _db.SystemMessages.Update(m);
        await _db.SaveChangesAsync(ct);
        return Map(m);
    }

    private static Dictionary<string, List<long>> ParseReactions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, List<long>>();
        try { return JsonSerializer.Deserialize<Dictionary<string, List<long>>>(json) ?? new Dictionary<string, List<long>>(); }
        catch (JsonException) { return new Dictionary<string, List<long>>(); }
    }

    private static string SerializeReactions(Dictionary<string, List<long>> map) =>
        JsonSerializer.Serialize(map);

    private static SystemMessageDto Map(CoupleSystemMessage m) => new()
    {
        Id = m.Id,
        ReceiverUserId = m.ReceiverUserId,
        Title = m.Title,
        Content = m.Content,
        MessageType = m.MessageType,
        IsRead = m.IsRead,
        CreateTime = m.CreateTime,
        Reactions = ParseReactions(m.Reactions),
    };
}
