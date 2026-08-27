using CoupleLoveSystem.Api;
using CoupleLoveSystem.Core;
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
    private bool IsRelational => _db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";

    public async Task<CoupleSettingDto> GetSettingAsync(CancellationToken ct = default)
    {
        var s = await _db.Settings.FirstOrDefaultAsync(x => x.Key == "global", ct);
        return s == null ? new CoupleSettingDto() : Map(s);
    }

    /// <summary>设置 / 修改相恋纪念日（共享）。任一方设置或修改，双方首页同步生效，并据此计算「在一起多少天」。
    /// 关系型库用 ExecuteUpdate 原子更新，避免并发「读-改-写」互相覆盖（审计 P2-14）；InMemory 回退读-改-写。</summary>
    public async Task<CoupleSettingDto> SetLoveStartAsync(DateTime loveStart, long userId, CancellationToken ct = default)
    {
        var date = loveStart.Date;
        if (IsRelational)
        {
            var affected = await _db.Settings
                .Where(x => x.Key == "global")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.LoveStartTime, date)
                    .SetProperty(x => x.UpdateUserId, userId)
                    .SetProperty(x => x.UpdateTime, DateTime.UtcNow), ct);
            if (affected == 0)
            {
                _db.Settings.Add(new CoupleSetting { Key = "global", LoveStartTime = date, CreateUserId = userId, CreateTime = DateTime.UtcNow });
                await _db.SaveChangesAsync(ct);
            }
        }
        else
        {
            var s = await _db.Settings.FirstOrDefaultAsync(x => x.Key == "global", ct)
                    ?? new CoupleSetting { Key = "global", CreateUserId = userId, CreateTime = DateTime.UtcNow };
            s.LoveStartTime = date; s.UpdateUserId = userId; s.UpdateTime = DateTime.UtcNow;
            if (s.Id == 0) _db.Settings.Add(s);
            await _db.SaveChangesAsync(ct);
        }
        return await GetSettingAsync(ct);
    }

    /// <summary>更新情侣共享设置。关系型库用 ExecuteUpdate 原子更新各字段，未提供的字段保持原值，避免并发覆盖；InMemory 回退读-改-写。</summary>
    public async Task<CoupleSettingDto> UpdateSettingAsync(UpdateCoupleSettingReq req, long userId, CancellationToken ct = default)
    {
        if (IsRelational)
        {
            var now = DateTime.UtcNow;
            var affected = await _db.Settings
                .Where(x => x.Key == "global")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.LoveStartTime, x => req.LoveStartTime != null ? req.LoveStartTime.Value.Date : x.LoveStartTime)
                    .SetProperty(x => x.CoupleName, x => req.CoupleName != null ? req.CoupleName : x.CoupleName)
                    .SetProperty(x => x.CoupleAvatar, x => req.CoupleAvatar != null ? req.CoupleAvatar : x.CoupleAvatar)
                    .SetProperty(x => x.UpdateUserId, userId)
                    .SetProperty(x => x.UpdateTime, now), ct);
            if (affected == 0)
            {
                _db.Settings.Add(new CoupleSetting
                {
                    Key = "global",
                    LoveStartTime = req.LoveStartTime?.Date,
                    CoupleName = req.CoupleName,
                    CoupleAvatar = req.CoupleAvatar,
                    CreateUserId = userId,
                    CreateTime = now
                });
                await _db.SaveChangesAsync(ct);
            }
        }
        else
        {
            var s = await _db.Settings.FirstOrDefaultAsync(x => x.Key == "global", ct)
                    ?? new CoupleSetting { Key = "global", CreateUserId = userId, CreateTime = DateTime.UtcNow };
            if (req.LoveStartTime != null) s.LoveStartTime = req.LoveStartTime.Value.Date;
            if (req.CoupleName != null) s.CoupleName = req.CoupleName;
            if (req.CoupleAvatar != null) s.CoupleAvatar = req.CoupleAvatar;
            s.UpdateUserId = userId; s.UpdateTime = DateTime.UtcNow;
            if (s.Id == 0) _db.Settings.Add(s);
            await _db.SaveChangesAsync(ct);
        }
        return await GetSettingAsync(ct);
    }

    private static CoupleSettingDto Map(CoupleSetting s) => new()
    {
        LoveStartTime = s.LoveStartTime,
        CoupleName = s.CoupleName,
        CoupleAvatar = s.CoupleAvatar,
        LunarLoveStart = s.LoveStartTime.HasValue ? LunarHelper.ToLunarString(s.LoveStartTime.Value) : null
    };
}
