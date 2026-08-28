using CoupleLoveSystem.Api;
using System.Collections.Generic;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CoupleLoveSystem.Application.Services;

public class AlbumService
{
    private readonly IRepository<CoupleAlbum> _repo;
    private readonly IRepository<CoupleImage> _imgRepo;
    private readonly CoupleDbContext _db;

    public AlbumService(IRepository<CoupleAlbum> repo, IRepository<CoupleImage> imgRepo, CoupleDbContext db)
    {
        _repo = repo; _imgRepo = imgRepo; _db = db;
    }

    public async Task<PagedResult<AlbumDto>> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _repo.Query().OrderByDescending(a => a.CreateTime);
        var total = await query.CountAsync(ct);
        var albums = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var items = new List<AlbumDto>();
        foreach (var a in albums)
        {
            var dto = Map(a);
            dto.ImageCount = await _imgRepo.Query().CountAsync(i => i.AlbumId == a.Id, ct);
            items.Add(dto);
        }
        return new PagedResult<AlbumDto> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<AlbumDto> GetAsync(long id, CancellationToken ct = default)
    {
        var a = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("相册不存在");
        var dto = Map(a);
        dto.ImageCount = await _imgRepo.Query().CountAsync(i => i.AlbumId == a.Id, ct);
        return dto;
    }

    public async Task<AlbumDto> CreateAsync(AlbumReq req, long currentUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.AlbumName))
            throw new ConflictException("相册名称不能为空");
        var a = new CoupleAlbum
        {
            AlbumName = req.AlbumName,
            Cover = req.Cover,
            Remark = req.Remark,
            CreateUserId = currentUserId,
            CreateTime = DateTime.UtcNow
        };
        await _repo.AddAsync(a, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(a);
    }

    public async Task<AlbumDto> UpdateAsync(long id, AlbumReq req, long currentUserId, CancellationToken ct = default)
    {
        var a = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("相册不存在");
        if (!string.IsNullOrWhiteSpace(req.AlbumName)) a.AlbumName = req.AlbumName;
        if (req.Cover != null) a.Cover = req.Cover;
        if (req.Remark != null) a.Remark = req.Remark;
        a.UpdateUserId = currentUserId;
        a.UpdateTime = DateTime.UtcNow;
        _repo.Update(a);
        await _repo.SaveChangesAsync(ct);
        return Map(a);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var a = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("相册不存在");
        // 注意：相册与图片未启用级联删除（DeleteBehavior.Cascade），删除相册需先清理其图片
        // 逻辑删除：仅标记删除，保留磁盘图片，避免误删对方上传的照片
        var imgs = await _imgRepo.Query().Where(i => i.AlbumId == id).ToListAsync(ct);
        foreach (var img in imgs) _imgRepo.SoftDelete(img);
        _repo.SoftDelete(a);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task<List<ImageDto>> ListImagesAsync(long albumId, CancellationToken ct = default)
    {
        var imgs = await _imgRepo.Query().Where(i => i.AlbumId == albumId)
            .OrderBy(i => i.SortOrder).ThenByDescending(i => i.CreateTime).ToListAsync(ct);
        return imgs.Select(MapImage).ToList();
    }

    /// <summary>拖拽排序：按传入的 id 顺序写入 SortOrder（只更新传入项；其余项顺序由列表查询的 SortOrder/CreateTime 兜底）。</summary>
    public async Task ReorderImagesAsync(List<long> ids, CancellationToken ct = default)
    {
        if (ids == null || ids.Count == 0) return;
        var items = await _imgRepo.Query().Where(i => ids.Contains(i.Id)).ToListAsync(ct);
        var map = items.ToDictionary(i => i.Id);
        for (int k = 0; k < ids.Count; k++)
        {
            if (map.TryGetValue(ids[k], out var img))
            {
                img.SortOrder = k;
                _imgRepo.Update(img);
            }
        }
        await _imgRepo.SaveChangesAsync(ct);
    }

    /// <summary>批量删除：逻辑删除（保留磁盘图片）。全局情侣隔离过滤器保证只影响本情侣的图片。</summary>
    public async Task BatchDeleteImagesAsync(List<long> ids, CancellationToken ct = default)
    {
        if (ids == null || ids.Count == 0) return;
        var items = await _imgRepo.Query().Where(i => ids.Contains(i.Id)).ToListAsync(ct);
        foreach (var img in items) _imgRepo.SoftDelete(img);
        await _imgRepo.SaveChangesAsync(ct);
    }

    /// <summary>批量移动到其他相册：目标相册须经全局过滤器确认属于本情侣（否则 NotFound）。</summary>
    public async Task BatchMoveImagesAsync(List<long> ids, long targetAlbumId, CancellationToken ct = default)
    {
        if (ids == null || ids.Count == 0) return;
        var target = await _repo.GetByIdAsync(targetAlbumId, ct)
            ?? throw new NotFoundException("目标相册不存在");
        var items = await _imgRepo.Query().Where(i => ids.Contains(i.Id)).ToListAsync(ct);
        foreach (var img in items)
        {
            img.AlbumId = target.Id;
            _imgRepo.Update(img);
        }
        await _imgRepo.SaveChangesAsync(ct);
    }

    public static AlbumDto Map(CoupleAlbum a) => new()
    {
        Id = a.Id,
        AlbumName = a.AlbumName,
        Cover = a.Cover,
        Remark = a.Remark,
        ImageCount = 0,
        CreateUserId = a.CreateUserId,
        CreateTime = a.CreateTime
    };

    public static ImageDto MapImage(CoupleImage i) => new()
    {
        Id = i.Id,
        AlbumId = i.AlbumId,
        ImagePath = i.ImagePath,
        Url = string.IsNullOrEmpty(i.ImagePath) ? null : i.ImagePath,
        Remark = i.Remark,
        ShootTime = i.ShootTime,
        Location = i.Location,
        CreateUserId = i.CreateUserId,
        CreateTime = i.CreateTime
    };
}
