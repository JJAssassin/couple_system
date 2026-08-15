using CoupleLoveSystem.Api;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoupleLoveSystem.Application.Services;

/// <summary>情侣级共享设置：相恋日期、情侣名等。任一方修改，双方生效。</summary>
public class CoupleService
{
    private readonly CoupleDbContext _db;
    public CoupleService(CoupleDbContext db) => _db = db;

    public async Task<CoupleSettingDto> GetSettingAsync(CancellationToken ct = default)
    {
        var s = await _db.Settings.FirstOrDefaultAsync(x => x.Key == "global", ct);
        return s == null ? new CoupleSettingDto() : Map(s);
    }

    /// <summary>设置 / 修改相恋纪念日（共享）。任一方设置或修改，双方首页同步生效，并据此计算「在一起多少天」。</summary>
    public async Task<CoupleSettingDto> SetLoveStartAsync(DateTime loveStart, long userId, CancellationToken ct = default)
    {
        var s = await _db.Settings.FirstOrDefaultAsync(x => x.Key == "global", ct)
                ?? new CoupleSetting { Key = "global", CreateUserId = userId, CreateTime = DateTime.UtcNow };
        s.LoveStartTime = loveStart.Date;
        s.UpdateUserId = userId; s.UpdateTime = DateTime.UtcNow;
        if (s.Id == 0) _db.Settings.Add(s);
        await _db.SaveChangesAsync(ct);
        return Map(s);
    }

    public async Task<CoupleSettingDto> UpdateSettingAsync(UpdateCoupleSettingReq req, long userId, CancellationToken ct = default)
    {
        var s = await _db.Settings.FirstOrDefaultAsync(x => x.Key == "global", ct)
                ?? new CoupleSetting { Key = "global", CreateUserId = userId, CreateTime = DateTime.UtcNow };
        if (req.LoveStartTime != null) s.LoveStartTime = req.LoveStartTime.Value.Date;
        if (req.CoupleName != null) s.CoupleName = req.CoupleName;
        if (req.CoupleAvatar != null) s.CoupleAvatar = req.CoupleAvatar;
        s.UpdateUserId = userId; s.UpdateTime = DateTime.UtcNow;
        if (s.Id == 0) _db.Settings.Add(s);
        await _db.SaveChangesAsync(ct);
        return Map(s);
    }

    private static CoupleSettingDto Map(CoupleSetting s) => new()
    {
        LoveStartTime = s.LoveStartTime,
        CoupleName = s.CoupleName,
        CoupleAvatar = s.CoupleAvatar
    };
}
