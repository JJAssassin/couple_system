using CoupleLoveSystem.Application.Filters;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Domain.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CoupleLoveSystem.Application.Services;

public class UserService
{
    private readonly IRepository<CoupleUser> _userRepo;
    private readonly CoupleDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ITokenStore _tokens;

    public UserService(IRepository<CoupleUser> userRepo, CoupleDbContext db, IWebHostEnvironment env, ITokenStore tokens)
    {
        _userRepo = userRepo; _db = db; _env = env; _tokens = tokens;
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
        // 写入系统临时目录（非公开 /uploads），文件本身不对外暴露为可猜 URL
        var exportTemp = Path.Combine(Path.GetTempPath(), "couple-export");
        Directory.CreateDirectory(exportTemp);
        // 兜底清理：未下载（令牌已过期）的历史 zip 可能滞留临时目录，清理超过 1 小时的旧文件，避免磁盘堆积
        foreach (var old in Directory.GetFiles(exportTemp).Where(f => (DateTime.UtcNow - File.GetLastWriteTimeUtc(f)).TotalHours > 1))
        {
            try { File.Delete(old); } catch { /* 忽略单个清理失败 */ }
        }
        var zipPath = Path.Combine(exportTemp, zipName);
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

        // 一次性下载令牌：映射临时文件全路径，带 10 分钟 TTL；下载端点消费后即删文件并作废令牌
        var token = Guid.NewGuid().ToString("N");
        var tokenKey = "export:" + token;
        await _tokens.SetAsync(tokenKey, zipPath, TimeSpan.FromMinutes(10), ct);

        return new ExportResp
        {
            Token = token,
            FileName = "couple_export.zip",
            MediaCount = media.Count
        };
    }

    // —— 全量备份导入（与 ExportAsync 配对）——
    // 两阶段：preview 仅解析统计；commit 先按导出对称范围清空再插入，保证幂等（重复导入不翻倍）。
    // 插入前清除 Id/CoupleId/IsDeleted，由 SaveChanges 拦截器重盖当前情侣 CoupleId、自增分配新 Id。

    public async Task<ImportPreviewResult> ImportPreviewAsync(IFormFile file, CancellationToken ct = default)
    {
        var (payload, _, error) = await ParseBackupAsync(file, ct);
        if (payload is null)
            return new ImportPreviewResult { Valid = false, Message = error ?? "备份解析失败" };
        return new ImportPreviewResult
        {
            Valid = true,
            Message = "解析成功，以下数据将覆盖当前账号导出的对应范围（覆盖前会先清空该类现有数据）",
            Counts = new ImportCounts
            {
                Anniversaries = payload.Anniversaries.Count,
                Diaries = payload.Diaries.Count,
                Wishes = payload.Wishes.Count,
                Conflicts = payload.Conflicts.Count,
                AccountRecords = payload.AccountRecords.Count,
                DateRecords = payload.DateRecords.Count,
                SystemMessages = payload.SystemMessages.Count,
            }
        };
    }

    public async Task<ImportCommitResult> ImportCommitAsync(IFormFile file, long currentUserId, CancellationToken ct = default)
    {
        var (payload, mediaEntries, error) = await ParseBackupAsync(file, ct);
        if (payload is null) throw new ConflictException(error ?? "备份解析失败");

        var result = new ImportCommitResult();
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // 1) 按导出对称范围清空：全情侣三类 + 本人可见日记 + 本人三类
        _db.Anniversaries.RemoveRange(_db.Anniversaries);
        _db.Wishes.RemoveRange(_db.Wishes);
        _db.Conflicts.RemoveRange(_db.Conflicts);
        _db.Diaries.RemoveRange(PermissionFilter.WhereVisible(_db.Diaries, currentUserId));
        _db.AccountRecords.RemoveRange(_db.AccountRecords.Where(a => a.CreateUserId == currentUserId));
        _db.DateRecords.RemoveRange(_db.DateRecords.Where(d => d.CreateUserId == currentUserId));
        _db.SystemMessages.RemoveRange(_db.SystemMessages.Where(m => m.ReceiverUserId == currentUserId));
        await _db.SaveChangesAsync(ct);

        // 2) 准备并插入（清除 Id/CoupleId/IsDeleted，拦截器重盖 CoupleId、自增分配 Id）
        Prep(payload.Anniversaries);
        Prep(payload.Diaries);
        Prep(payload.Wishes);
        Prep(payload.Conflicts);
        Prep(payload.AccountRecords);
        Prep(payload.DateRecords);
        Prep(payload.SystemMessages);

        _db.Anniversaries.AddRange(payload.Anniversaries);
        _db.Diaries.AddRange(payload.Diaries);
        _db.Wishes.AddRange(payload.Wishes);
        _db.Conflicts.AddRange(payload.Conflicts);
        _db.AccountRecords.AddRange(payload.AccountRecords);
        _db.DateRecords.AddRange(payload.DateRecords);
        _db.SystemMessages.AddRange(payload.SystemMessages);
        await _db.SaveChangesAsync(ct);

        // 3) 尽力还原媒体文件（best-effort，不删已有文件）
        result.MediaRestored = await RestoreMediaFromAsync(file, mediaEntries, ct);

        await tx.CommitAsync(ct);

        result.Imported = new ImportCounts
        {
            Anniversaries = payload.Anniversaries.Count,
            Diaries = payload.Diaries.Count,
            Wishes = payload.Wishes.Count,
            Conflicts = payload.Conflicts.Count,
            AccountRecords = payload.AccountRecords.Count,
            DateRecords = payload.DateRecords.Count,
            SystemMessages = payload.SystemMessages.Count,
        };
        result.ImportedTotal = result.Imported.Total;
        if (mediaEntries.Count > result.MediaRestored)
            result.Warnings.Add($"有 {mediaEntries.Count - result.MediaRestored} 个媒体文件未能还原（可能已不存在于备份中）");
        return result;
    }

    private static void Prep<T>(List<T> items) where T : BaseEntity, ICoupleScoped
    {
        foreach (var e in items)
        {
            e.Id = 0;
            e.CoupleId = null;
            e.IsDeleted = false;
        }
    }

    private async Task<(BackupPayload? payload, List<string> mediaEntries, string? error)> ParseBackupAsync(IFormFile file, CancellationToken ct)
    {
        var mediaEntries = new List<string>();
        string json;
        var fileName = file.FileName ?? string.Empty;
        if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            ms.Position = 0;
            using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
            var dataEntry = zip.GetEntry("data.json") ?? throw new ConflictException("压缩包内缺少 data.json");
            using var rs = dataEntry.Open();
            using var sr = new StreamReader(rs);
            json = await sr.ReadToEndAsync(ct);
            foreach (var e in zip.Entries)
                if (e.FullName.StartsWith("media/", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(e.Name))
                    mediaEntries.Add(e.FullName);
        }
        else
        {
            using var sr = new StreamReader(file.OpenReadStream());
            json = await sr.ReadToEndAsync(ct);
        }

        BackupPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<BackupPayload>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            return (null, mediaEntries, "备份文件格式无法解析：" + ex.Message);
        }
        if (payload is null) return (null, mediaEntries, "备份内容为空或格式不正确");
        return (payload, mediaEntries, null);
    }

    private async Task<int> RestoreMediaFromAsync(IFormFile file, List<string> mediaEntries, CancellationToken ct)
    {
        if (mediaEntries.Count == 0) return 0;
        var fileName = file.FileName ?? "";
        if (!fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) return 0;
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        ms.Position = 0;
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
        var uploadsRoot = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploadsRoot);
        var rootFull = Path.GetFullPath(uploadsRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        int restored = 0;
        foreach (var entryName in mediaEntries)
        {
            var entry = zip.GetEntry(entryName);
            if (entry is null) continue;
            var safeName = Path.GetFileName(entryName);
            if (string.IsNullOrWhiteSpace(safeName)) continue;
            var dest = Path.GetFullPath(Path.Combine(uploadsRoot, safeName));
            if (!dest.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase)) continue; // 防目录穿越
            using var es = entry.Open();
            using var fs = new FileStream(dest, FileMode.Create, FileAccess.Write);
            await es.CopyToAsync(fs, ct);
            restored++;
        }
        return restored;
    }

    private class BackupPayload
    {
        public DateTime ExportedAt { get; set; }
        public JsonElement User { get; set; }
        public List<CoupleAnniversary> Anniversaries { get; set; } = new();
        public List<CoupleDiary> Diaries { get; set; } = new();
        public List<CoupleWish> Wishes { get; set; } = new();
        public List<CoupleConflict> Conflicts { get; set; } = new();
        public List<CoupleAccountRecord> AccountRecords { get; set; } = new();
        public List<CoupleDateRecord> DateRecords { get; set; } = new();
        public List<CoupleSystemMessage> SystemMessages { get; set; } = new();
    }

    private static UserProfileDto Map(CoupleUser u) => new()
    {
        Id = u.Id, NickName = u.NickName, Avatar = u.Avatar, RoleType = u.RoleType, LoveStartTime = u.LoveStartTime
    };
}
