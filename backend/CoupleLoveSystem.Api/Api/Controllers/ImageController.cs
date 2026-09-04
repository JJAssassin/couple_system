using CoupleLoveSystem.Api;
using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/image")]
[Authorize]
public class ImageController : BaseController
{
    private readonly IRepository<CoupleImage> _repo;
    private readonly IWebHostEnvironment _env;
    private readonly CoupleDbContext _db;
    private readonly ILogger<ImageController> _logger;

    public ImageController(IRepository<CoupleImage> repo, IWebHostEnvironment env, CoupleDbContext db, ILogger<ImageController> logger)
    {
        _repo = repo; _env = env; _db = db; _logger = logger;
    }

    // 允许的扩展名（忽略大小写）
    private static readonly HashSet<string> AllowedExt =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    // 单文件大小上限 25MB（支持手机直拍大图，配合 Program.cs 中 Kestrel MaxRequestBodySize）
    private const long MaxFileSize = 25 * 1024 * 1024;

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResult<ImageDto>>> Upload(
        IFormFile file, [FromQuery] long albumId, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            throw new ConflictException("请选择要上传的图片");
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExt.Contains(ext))
            throw new ConflictException("不支持的文件类型，仅支持 jpg/jpeg/png/gif/webp");

        // P2-15：先校验 albumId 归属当前情侣，避免写入后才拒绝造成的孤儿文件。
        // _db.Albums 受全局情侣过滤器约束，查不到即代表该相册不属于当前情侣（或不存在）。
        if (albumId > 0)
        {
            var album = await _db.Albums.FirstOrDefaultAsync(a => a.Id == albumId, ct);
            if (album == null)
                throw new ForbiddenException("相册不存在或无权访问");
        }

        // P2-5：校验内容 Magic bytes + 重编码剥离 EXIF/GPS，落盘并返回相对路径
        var relative = await SaveValidatedImageAsync(file, ct);

        var img = new CoupleImage
        {
            AlbumId = albumId,
            ImagePath = relative,
            CreateUserId = CurrentUserId,
            CreateTime = DateTime.UtcNow
        };
        await _repo.AddAsync(img, ct);
        await _repo.SaveChangesAsync(ct);
        return Ok(ApiResult<ImageDto>.Ok(AlbumService.MapImage(img)));
    }

    /// <summary>
    /// #16-c 相册照片批量导入：一次请求多文件，归到指定相册。复用单图上传的内容校验（Magic bytes +
    /// ImageSharp 重编码剥离 EXIF/GPS），逐文件容错（单张失败不影响其余），一次提交后由 [Broadcast("album")]
    /// 拦截器自动广播同步。返回成功/失败计数与逐文件错误明细。
    /// </summary>
    [HttpPost("batch-upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResult<AlbumImageBatchUploadResult>>> BatchUpload(
        IFormFileCollection files, [FromQuery] long albumId, CancellationToken ct = default)
    {
        if (files == null || files.Count == 0)
            throw new ConflictException("请选择要上传的图片");

        // 校验 albumId 归属当前情侣（P2-15：写入前校验，避免孤儿文件）
        if (albumId > 0)
        {
            var album = await _db.Albums.FirstOrDefaultAsync(a => a.Id == albumId, ct);
            if (album == null)
                throw new ForbiddenException("相册不存在或无权访问");
        }

        var result = new AlbumImageBatchUploadResult();
        foreach (var file in files)
        {
            var name = file?.FileName ?? "未知文件";
            if (file == null || file.Length == 0)
            {
                result.Errors.Add(new AlbumImageBatchUploadError { FileName = name, Reason = "空文件" });
                result.Failed++;
                continue;
            }
            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(ext) || !AllowedExt.Contains(ext))
            {
                result.Errors.Add(new AlbumImageBatchUploadError { FileName = name, Reason = "不支持的文件类型，仅支持 jpg/jpeg/png/gif/webp" });
                result.Failed++;
                continue;
            }

            string relative;
            try
            {
                relative = await SaveValidatedImageAsync(file, ct);
            }
            catch (ConflictException ex)
            {
                result.Errors.Add(new AlbumImageBatchUploadError { FileName = name, Reason = ex.Message });
                result.Failed++;
                continue;
            }

            var img = new CoupleImage
            {
                AlbumId = albumId,
                ImagePath = relative,
                CreateUserId = CurrentUserId,
                CreateTime = DateTime.UtcNow
            };
            await _repo.AddAsync(img, ct);
            result.Images.Add(AlbumService.MapImage(img));
            result.Imported++;
        }

        // 一次提交 → 自动广播 album 同步（全局情侣隔离过滤器保证只影响本情侣）
        await _repo.SaveChangesAsync(ct);
        return Ok(ApiResult<AlbumImageBatchUploadResult>.Ok(result, $"成功导入 {result.Imported} 张，失败 {result.Failed} 张"));
    }

    // 通用单图上传：不归属相册，仅落盘并返回可访问路径（头像 / 封面 / 配图等复用）
    [HttpPost("upload-standalone")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResult<object>>> UploadStandalone(
        IFormFile file, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            throw new ConflictException("请选择要上传的图片");
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExt.Contains(ext))
            throw new ConflictException("不支持的文件类型，仅支持 jpg/jpeg/png/gif/webp");

        // P2-5：校验内容 Magic bytes + 重编码剥离 EXIF/GPS
        var relative = await SaveValidatedImageAsync(file, ct);
        return Ok(ApiResult<object>.Ok(new { path = relative }, "上传成功"));
    }

    [HttpDelete("delete")]
    public async Task<ActionResult<ApiResult<object>>> Delete([FromQuery] long id, CancellationToken ct = default)
    {
        var img = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("图片不存在");
        var imagePath = img.ImagePath; // 先取出路径，软删后再物理清理
        // 逻辑删除（保留数据可追溯）
        _repo.SoftDelete(img);
        await _repo.SaveChangesAsync(ct);

        // 物理清理磁盘原文件（落地 ImageController 的 TODO）：仅删 uploads 根内文件，best-effort 不阻断请求
        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            try
            {
                var root = _env.WebRootPath ?? _env.ContentRootPath;
                var uploadsRoot = Path.GetFullPath(Path.Combine(root, "uploads")).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                var rel = imagePath.Length > "/uploads/".Length ? imagePath.Substring("/uploads/".Length) : imagePath.TrimStart('/');
                if (!string.IsNullOrWhiteSpace(rel))
                {
                    var physical = Path.GetFullPath(Path.Combine(root, "uploads", rel));
                    if (physical.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(physical))
                        System.IO.File.Delete(physical);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "删除图片物理文件失败（已忽略）：{Path}", imagePath);
            }
        }

        return Ok(ApiResults.Ok(new { }, "已删除"));
    }

    /// <summary>
    /// P2-5 上传纵深防御：读取文件内容校验 Magic bytes（杜绝扩展名伪装），再用 ImageSharp 解码 +
    /// AutoOrient + 清空 EXIF/GPS/IPTC/XMP 等隐私元数据后重编码落盘（同时完成「内容校验」与「剥离位置信息」）。
    /// GIF 仅做 Magic bytes 校验后原样落盘（重编码会丢失动画帧）。返回可访问的相对路径。
    /// </summary>
    private async Task<string> SaveValidatedImageAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length > MaxFileSize)
            throw new ConflictException("文件大小不能超过 25MB");

        byte[] bytes;
        await using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms, ct);
            bytes = ms.ToArray();
        }

        var fmt = DetectFormat(bytes);
        if (fmt == null)
            throw new ConflictException("文件内容不是有效的图片（扩展名与实际格式不符）");

        byte[] outBytes;
        string finalExt;
        if (fmt == DetectedFormat.Gif)
        {
            outBytes = bytes; // GIF 保留原字节，避免丢失动画帧
            finalExt = ".gif";
        }
        else
        {
            try
            {
                using var image = Image.Load(bytes.AsSpan());
                image.Mutate(x => x.AutoOrient());
                // 剥离 EXIF/GPS/IPTC/XMP 等隐私元数据（情侣私密照片常见含 GPS 定位）
                image.Metadata.ExifProfile = null;
                image.Metadata.IptcProfile = null;
                image.Metadata.XmpProfile = null;
                IImageEncoder encoder = fmt switch
                {
                    DetectedFormat.Jpeg => new JpegEncoder { Quality = 85 },
                    DetectedFormat.Png => new PngEncoder(),
                    DetectedFormat.Webp => new WebpEncoder { Quality = 85 },
                    _ => throw new InvalidOperationException()
                };
                await using var outMs = new MemoryStream();
                image.Save(outMs, encoder);
                outBytes = outMs.ToArray();
            }
            catch (Exception)
            {
                throw new ConflictException("文件内容无法解析为有效图片");
            }

            finalExt = fmt switch
            {
                DetectedFormat.Jpeg => ".jpg",
                DetectedFormat.Png => ".png",
                DetectedFormat.Webp => ".webp",
                _ => ".bin"
            };
        }

        var dir = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "uploads");
        Directory.CreateDirectory(dir);
        var newName = $"{DateTime.UtcNow:yyyyMMdd}_{Guid.NewGuid()}{finalExt}";
        var fullPath = Path.Combine(dir, newName);
        await System.IO.File.WriteAllBytesAsync(fullPath, outBytes, ct);
        return "/uploads/" + newName;
    }

    private enum DetectedFormat { Jpeg, Png, Gif, Webp }

    /// <summary>按文件头 Magic bytes 识别真实图片格式（与实际扩展名无关）。</summary>
    private static DetectedFormat? DetectFormat(ReadOnlySpan<byte> b)
    {
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return DetectedFormat.Jpeg;
        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A) return DetectedFormat.Png;
        if (b.Length >= 6 && b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x38) return DetectedFormat.Gif; // GIF8
        if (b.Length >= 12 && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50) return DetectedFormat.Webp; // RIFF....WEBP
        return null;
    }
}
