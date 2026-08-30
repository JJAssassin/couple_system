using CoupleLoveSystem.Api;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CoupleLoveSystem.Application.Services;

/// <summary>私密留言板：仅两人可见的留言墙，可置顶、实时同步。</summary>
public class BoardMessageService
{
    private readonly IRepository<CoupleBoardMessage> _repo;
    private readonly IRepository<CoupleUser> _userRepo;

    public BoardMessageService(IRepository<CoupleBoardMessage> repo, IRepository<CoupleUser> userRepo)
    {
        _repo = repo; _userRepo = userRepo;
    }

    public async Task<PagedResult<BoardMessageDto>> ListAsync(int page, int pageSize, long currentUserId, CancellationToken ct = default)
    {
        var query = _repo.Query();
        // 可见性：公开消息所有人可见；私密消息发送者 + 接收者双方可见
        query = query.Where(m => !m.IsPrivate || m.ReceiverUserId == currentUserId || m.CreateUserId == currentUserId);
        
        var all = await query
            .OrderByDescending(m => m.Pinned)
            .ThenByDescending(m => m.CreateTime)
            .ToListAsync(ct);

        var total = all.Count;
        var items = all.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(Map)
            .ToList();

        return new PagedResult<BoardMessageDto> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<BoardMessageDto> GetAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var m = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("留言不存在");
        return Map(m);
    }

    public async Task<BoardMessageDto> CreateAsync(BoardMessageReq req, long currentUserId, CancellationToken ct = default)
    {
        var author = await _userRepo.GetByIdAsync(currentUserId, ct);
        var now = DateTime.UtcNow;
        var m = new CoupleBoardMessage
        {
            Content = req.Content,
            Color = req.Color,
            AuthorName = author?.NickName,
            CreateUserId = currentUserId,
            CreateTime = now,
            ImageUrl = req.ImageUrl,
            IsPrivate = req.IsPrivate,
            ReceiverUserId = req.ReceiverUserId ?? currentUserId,
            ScheduledAt = req.ScheduledAt,
            IsUnlocked = !req.ScheduledAt.HasValue || req.ScheduledAt.Value <= now,
        };
        await _repo.AddAsync(m, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(m);
    }

    public async Task<BoardMessageDto> UpdateAsync(long id, BoardMessageReq req, long currentUserId, CancellationToken ct = default)
    {
        var m = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("留言不存在");
        m.Content = req.Content;
        m.Color = req.Color;
        m.ImageUrl = req.ImageUrl;
        m.IsPrivate = req.IsPrivate;
        m.ReceiverUserId = req.ReceiverUserId;
        m.UpdateUserId = currentUserId;
        _repo.Update(m);
        await _repo.SaveChangesAsync(ct);
        return Map(m);
    }

    public async Task DeleteAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var m = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("留言不存在");
        _repo.SoftDelete(m);
        await _repo.SaveChangesAsync(ct);
    }

    /// <summary>切换置顶状态。</summary>
    public async Task<BoardMessageDto> PinAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var m = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("留言不存在");
        m.Pinned = !m.Pinned;
        m.UpdateUserId = currentUserId;
        _repo.Update(m);
        await _repo.SaveChangesAsync(ct);
        return Map(m);
    }

    /// <summary>切换某用户对某条留言的某表情反应：已点则取消，未点则加上。返回最新留言。</summary>
    public async Task<BoardMessageDto> ToggleReactionAsync(long id, string emojiKey, long currentUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(emojiKey)) throw new ArgumentException("表情标识不能为空");
        var m = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("留言不存在");
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
        _repo.Update(m);
        await _repo.SaveChangesAsync(ct);
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

    private static BoardMessageDto Map(CoupleBoardMessage m) => new()
    {
        Id = m.Id,
        Content = m.Content,
        AuthorName = m.AuthorName,
        Color = m.Color,
        Pinned = m.Pinned,
        ImageUrl = m.ImageUrl,
        ReceiverUserId = m.ReceiverUserId,
        IsPrivate = m.IsPrivate,
        ScheduledAt = m.ScheduledAt,
        IsUnlocked = m.IsUnlocked,
        CreateUserId = m.CreateUserId,
        CreateTime = m.CreateTime,
        Reactions = ParseReactions(m.Reactions),
    };
}
