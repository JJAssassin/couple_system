using CoupleLoveSystem.Api;
using CoupleLoveSystem.Application.Filters;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CoupleLoveSystem.Application.Services;

/// <summary>
/// <summary>日记服务：含私密日记可见性控制，越权访问抛出 ForbiddenException；评论接口见 ListCommentsAsync / AddCommentAsync</summary>
public class DiaryService
{
    private readonly IRepository<CoupleDiary> _diaryRepo;
    private readonly IRepository<CoupleDiaryComment> _commentRepo;
    private readonly CoupleDbContext _db;
    private readonly HtmlSanitizerService _html;

    public DiaryService(IRepository<CoupleDiary> diaryRepo, IRepository<CoupleDiaryComment> commentRepo, CoupleDbContext db, HtmlSanitizerService html)
    {
        _diaryRepo = diaryRepo; _commentRepo = commentRepo; _db = db; _html = html;
    }

    public async Task<PagedResult<DiaryDto>> ListAsync(int page, int pageSize, long currentUserId, CancellationToken ct = default)
    {
        var q = PermissionFilter.WhereVisible(_diaryRepo.Query(), currentUserId);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(d => d.DiaryDate)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<DiaryDto>
        {
            Items = items.Select(Map).ToList(),
            Total = total, Page = page, PageSize = pageSize
        };
    }

    public async Task<DiaryDto> GetAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var e = await _diaryRepo.GetByIdAsync(id, ct) ?? throw new NotFoundException("日记不存在");
        PermissionFilter.EnsureVisible(currentUserId, e);
        return Map(e);
    }

    public async Task<DiaryDto> CreateAsync(DiaryReq req, long currentUserId, CancellationToken ct = default)
    {
        var e = new CoupleDiary
        {
            Title = req.Title,
            Content = _html.Sanitize(req.Content),
            MoodTag = req.MoodTag,
            MoodScore = req.MoodScore,
            PermissionType = req.PermissionType,
            Weather = req.Weather,
            DiaryDate = req.DiaryDate,
            CreateUserId = currentUserId,
            CreateTime = DateTime.UtcNow
        };
        await _diaryRepo.AddAsync(e, ct);
        await _diaryRepo.SaveChangesAsync(ct);
        return Map(e);
    }

    public async Task<DiaryDto> UpdateAsync(long id, DiaryReq req, long currentUserId, CancellationToken ct = default)
    {
        var e = await _diaryRepo.GetByIdAsync(id, ct) ?? throw new NotFoundException("日记不存在");
        PermissionFilter.EnsureVisible(currentUserId, e);
        e.Title = req.Title;
        e.Content = _html.Sanitize(req.Content);
        e.MoodTag = req.MoodTag;
        e.MoodScore = req.MoodScore;
        e.PermissionType = req.PermissionType;
        e.Weather = req.Weather;
        e.DiaryDate = req.DiaryDate;
        e.UpdateUserId = currentUserId;
        _diaryRepo.Update(e);
        await _diaryRepo.SaveChangesAsync(ct);
        return Map(e);
    }

    public async Task DeleteAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var e = await _diaryRepo.GetByIdAsync(id, ct) ?? throw new NotFoundException("日记不存在");
        PermissionFilter.EnsureVisible(currentUserId, e);
        _diaryRepo.SoftDelete(e);
        await _diaryRepo.SaveChangesAsync(ct);
    }

    /// <summary>校验日记可见性（EnsureVisible）：私密日记仅作者可见</summary>
    public async Task<List<DiaryCommentDto>> ListCommentsAsync(long diaryId, long currentUserId, CancellationToken ct = default)
    {
        var diary = await _diaryRepo.GetByIdAsync(diaryId, ct) ?? throw new NotFoundException("日记不存在");
        PermissionFilter.EnsureVisible(currentUserId, diary);
        var comments = await _commentRepo.Query()
            .Where(c => c.DiaryId == diaryId)
            .OrderBy(c => c.CreateTime).ToListAsync(ct);
        return comments.Select(MapComment).ToList();
    }

    public async Task<DiaryCommentDto> AddCommentAsync(DiaryCommentReq req, long currentUserId, CancellationToken ct = default)
    {
        var diary = await _diaryRepo.GetByIdAsync(req.DiaryId, ct) ?? throw new NotFoundException("日记不存在");
        // 校验可见性：私密日记仅作者本人可见
        var c = new CoupleDiaryComment
        {
            DiaryId = req.DiaryId,
            Content = req.Content,
            CreateUserId = currentUserId,
            CreateTime = DateTime.UtcNow
        };
        await _commentRepo.AddAsync(c, ct);
        await _commentRepo.SaveChangesAsync(ct);
        return MapComment(c);
    }

    private static DiaryDto Map(CoupleDiary d) => new()
    {
        Id = d.Id, Title = d.Title, Content = d.Content, MoodTag = d.MoodTag,
        MoodScore = d.MoodScore, PermissionType = d.PermissionType, Weather = d.Weather,
        DiaryDate = d.DiaryDate, CreateUserId = d.CreateUserId, CreateTime = d.CreateTime
    };

    private static DiaryCommentDto MapComment(CoupleDiaryComment c) => new()
    {
        Id = c.Id, DiaryId = c.DiaryId, Content = c.Content,
        CreateUserId = c.CreateUserId, CreateTime = c.CreateTime
    };

    /// <summary>
    /// <summary>创建 / 更新日记时使用 HtmlSanitizerService 净化 content 与 cover 的 HTML，防止 XSS</summary>
}
