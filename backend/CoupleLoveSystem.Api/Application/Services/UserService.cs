using CoupleLoveSystem.Application.Filters;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CoupleLoveSystem.Application.Services;

public class UserService
{
    private readonly IRepository<CoupleUser> _userRepo;
    private readonly CoupleDbContext _db;
    private readonly IWebHostEnvironment _env;

    public UserService(IRepository<CoupleUser> userRepo, CoupleDbContext db, IWebHostEnvironment env)
    {
        _userRepo = userRepo; _db = db; _env = env;
    }

    public async Task<UserProfileDto> UpdateProfileAsync(UpdateProfileReq req, long currentUserId, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(currentUserId, ct)
            ?? throw new NotFoundException("用户不存在");

        if (req.NickName != null) user.NickName = req.NickName;
        if (req.Avatar != null) user.Avatar = req.Avatar;

        if (!string.IsNullOrEmpty(req.OldPassword) && !string.IsNullOrEmpty(req.NewPassword))
        {
            if (!AuthService.VerifyPassword(req.OldPassword, user.PasswordHash))
                throw new ConflictException("原密码错误");
            user.PasswordHash = AuthService.HashPassword(req.NewPassword);
        }

        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync(ct);
        return Map(user);
    }

    public async Task<ExportResp> ExportAsync(long currentUserId, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(currentUserId, ct)
            ?? throw new NotFoundException("用户不存在");

        // 仅导出当前用户可见数据：日记按权限过滤（排除对方 PrivateSelf），私密留言取接收人为本人的，其余按 CreateUserId/UserId 归属本人
        var diaries = await PermissionFilter.WhereVisible(_db.Diaries, currentUserId).ToListAsync(ct);
        var payload = new
        {
            ExportedAt = DateTime.UtcNow,
            User = Map(user),
            Anniversaries = await _db.Anniversaries.ToListAsync(ct),
            Diaries = diaries,
            Wishes = await _db.Wishes.ToListAsync(ct),
            Conflicts = await _db.Conflicts.ToListAsync(ct),
            AccountRecords = await _db.AccountRecords.Where(a => a.CreateUserId == currentUserId).ToListAsync(ct),
            DateRecords = await _db.DateRecords.Where(d => d.CreateUserId == currentUserId).ToListAsync(ct),
            SystemMessages = await _db.SystemMessages.Where(m => m.ReceiverUserId == currentUserId).ToListAsync(ct)
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

        // 收集 JSON 中引用的本地上传文件（头像 / 配图 / 相册图等），统一打包进 zip
        var uploadsRoot = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploadsRoot);
        var uploadsRootFull = Path.GetFullPath(uploadsRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var media = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in Regex.Matches(json, @"/uploads/[^""\s\\]+", RegexOptions.IgnoreCase))
        {
            var rel = m.Value.Length > "/uploads/".Length ? m.Value.Substring("/uploads/".Length) : string.Empty;
            if (string.IsNullOrWhiteSpace(rel)) continue;
            var physical = Path.GetFullPath(Path.Combine(uploadsRoot, rel));
            // 防穿越：物理路径必须落在 uploadsRoot 内
            if (physical.StartsWith(uploadsRootFull, StringComparison.OrdinalIgnoreCase) && File.Exists(physical))
                media.Add(physical);
        }

        var zipName = $"export_{currentUserId}_{Guid.NewGuid():N}.zip";
        var zipPath = Path.Combine(uploadsRoot, zipName);
        using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
        using (var zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false))
        {
            var dataEntry = zip.CreateEntry("data.json");
            await using (var es = dataEntry.Open())
            await using (var sw = new StreamWriter(es, leaveOpen: false))
            {
                await sw.WriteAsync(json.AsMemory(), ct);
            }

            foreach (var f in media)
                zip.CreateEntryFromFile(f, "media/" + Path.GetFileName(f));
        }

        return new ExportResp
        {
            DownloadUrl = $"/uploads/{zipName}",
            FileName = "couple_export.zip",
            MediaCount = media.Count
        };
    }

    private static UserProfileDto Map(CoupleUser u) => new()
    {
        Id = u.Id, NickName = u.NickName, Avatar = u.Avatar, RoleType = u.RoleType, LoveStartTime = u.LoveStartTime
    };
}
