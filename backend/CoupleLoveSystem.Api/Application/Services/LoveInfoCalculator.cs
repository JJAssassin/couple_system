using System;
using CoupleLoveSystem.Core.Dtos;

namespace CoupleLoveSystem.Application.Services;

/// <summary>
/// 恋爱时长计算（与首页展示解耦，便于单测）。
/// 整日计数以「本地日期」为准，在用户所在时区的午夜翻页，避免按 UTC 时段内算出的天数差浮动 ±1 天。
/// </summary>
public static class LoveInfoCalculator
{
    public static LoveInfoDto Compute(DateTime? start, DateTime today, DateTime now)
    {
        if (start == null)
            return new LoveInfoDto { HasLoveStart = false, LoveStartTime = null, TotalDays = 0, TotalHours = 0, TotalMinutes = 0 };

        var wholeDays = (int)(today.Date - start.Value.Date).TotalDays;
        var live = now - start.Value;
        return new LoveInfoDto
        {
            HasLoveStart = true,
            LoveStartTime = start,
            TotalDays = Math.Max(0, wholeDays),
            TotalHours = (int)live.TotalHours,
            TotalMinutes = (int)live.TotalMinutes
        };
    }
}
