using CoupleLoveSystem.Api;
using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/image")]
[Authorize]
public class ImageController : BaseController
{
    private readonly IRepository<CoupleImage> _repo;
    private readonly IWebHostEnvironment _env;
    private readonly CoupleDbContext _db;

    public ImageController(IRepository<CoupleImage> repo, IWebHostEnvironment env, CoupleDbContext db)
    {
        _repo = repo; _env = env; _db = db;
    }

    // 允许的扩展名（忽略大小写）
    private static readonly HashSet<string> AllowedExt =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    // 单文件大小上限 5MB
    private const long MaxFileSize = 5 * 1024 * 1024;

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResult<ImageDto>>> Upload(
        IFormFile file, [FromQuery] long albumId, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            throw new ConflictException("请选择要上传的图片");
        if (file.Length > MaxFileSize)
            throw new ConflictException("文件大小不能超过 5MB");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExt.Contains(ext))
            throw new ConflictException("不支持的文件类型，仅支持 jpg/jpeg/png/gif/webp");

        // 生成新文件名，防止路径穿越或覆盖已有文件
        var newName = $"{DateTime.UtcNow:yyyyMMdd}_{Guid.NewGuid()}{ext.ToLowerInvariant()}";
        var root = _env.WebRootPath ?? _env.ContentRootPath;
        var dir = Path.Combine(root, "uploads");
        Directory.CreateDirectory(dir); // 目录不存在时自动创建
        var fullPath = Path.Combine(dir, newName);

        await using (var fs = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(fs, ct);
        }

        var img = new CoupleImage
        {
            AlbumId = albumId,
            ImagePath = "/uploads/" + newName,
            CreateUserId = CurrentUserId,
            CreateTime = DateTime.UtcNow
        };
        await _repo.AddAsync(img, ct);
        await _repo.SaveChangesAsync(ct);
        return Ok(ApiResult<ImageDto>.Ok(AlbumService.MapImage(img)));
    }

    // 通用单图上传：不归属相册，仅落盘并返回可访问路径（头像 / 封面 / 配图等复用）
    [HttpPost("upload-standalone")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResult<object>>> UploadStandalone(
        IFormFile file, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            throw new ConflictException("请选择要上传的图片");
        if (file.Length > MaxFileSize)
            throw new ConflictException("文件大小不能超过 5MB");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExt.Contains(ext))
            throw new ConflictException("不支持的文件类型，仅支持 jpg/jpeg/png/gif/webp");

        var newName = $"{DateTime.UtcNow:yyyyMMdd}_{Guid.NewGuid()}{ext.ToLowerInvariant()}";
        var root = _env.WebRootPath ?? _env.ContentRootPath;
        var dir = Path.Combine(root, "uploads");
        Directory.CreateDirectory(dir);
        var fullPath = Path.Combine(dir, newName);

        await using (var fs = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(fs, ct);
        }

        var relative = "/uploads/" + newName;
        return Ok(ApiResult<object>.Ok(new { path = relative }, "上传成功"));
    }

    [HttpDelete("delete")]
    public async Task<ActionResult<ApiResult<object>>> Delete([FromQuery] long id, CancellationToken ct = default)
    {
        var img = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("图片不存在");
        // 逻辑删除：保留磁盘原文件以便追溯（TODO：可改为定时物理清理 / 由上传者清理）
        _repo.SoftDelete(img);
        await _repo.SaveChangesAsync(ct);
        return Ok(ApiResults.Ok(new { }, "已删除"));
    }
}
