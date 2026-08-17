using CoupleLoveSystem.Api;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

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
        var all = await _repo.Query()
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
        var m = new CoupleBoardMessage
        {
            Content = req.Content,
            Color = req.Color,
            AuthorName = author?.NickName,
            CreateUserId = currentUserId,
            CreateTime = DateTime.UtcNow,
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

    private static BoardMessageDto Map(CoupleBoardMessage m) => new()
    {
        Id = m.Id,
        Content = m.Content,
        AuthorName = m.AuthorName,
        Color = m.Color,
        Pinned = m.Pinned,
        CreateUserId = m.CreateUserId,
        CreateTime = m.CreateTime,
    };
}
