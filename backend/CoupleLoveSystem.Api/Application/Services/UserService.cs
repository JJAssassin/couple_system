using CoupleLoveSystem.Application.Filters;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text.Json;

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

        // 仅导出当前用户可见数据：日记按权限过滤（排除对方 PrivateSelf），书信取接收人为本人的，其余按 CreateUserId/UserId 归属本人
        var diaries = await PermissionFilter.WhereVisible(_db.Diaries, currentUserId).ToListAsync(ct);
        var payload = new
        {
            ExportedAt = DateTime.UtcNow,
            User = Map(user),
            Anniversaries = await _db.Anniversaries.ToListAsync(ct),
            Diaries = diaries,
            Wishes = await _db.Wishes.ToListAsync(ct),
            Conflicts = await _db.Conflicts.ToListAsync(ct),
            Letters = await _db.Letters.Where(l => l.ReceiverUserId == currentUserId).ToListAsync(ct),
            AccountRecords = await _db.AccountRecords.Where(a => a.CreateUserId == currentUserId).ToListAsync(ct),
            DateRecords = await _db.DateRecords.Where(d => d.CreateUserId == currentUserId).ToListAsync(ct),
            SystemMessages = await _db.SystemMessages.Where(m => m.ReceiverUserId == currentUserId).ToListAsync(ct)
        };

        var root = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "uploads");
        Directory.CreateDirectory(root);
        var fileName = $"export_{currentUserId}_{Guid.NewGuid():N}.json";
        var fullPath = Path.Combine(root, fileName);
        await File.WriteAllTextAsync(fullPath,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }),
            ct);

        // TODO: 图片/附件打包进 zip 未做，当前仅导出元数据 JSON。
        return new ExportResp
        {
            DownloadUrl = $"/uploads/{fileName}",
            FileName = "couple_export.json"
        };
    }

    private static UserProfileDto Map(CoupleUser u) => new()
    {
        Id = u.Id, NickName = u.NickName, Avatar = u.Avatar, RoleType = u.RoleType, LoveStartTime = u.LoveStartTime
    };
}
