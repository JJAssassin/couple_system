using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Enums;

namespace CoupleLoveSystem.Core.Dtos;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

// ---------- 认证 ----------
public class LoginReq
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
public class RefreshReq
{
    public string RefreshToken { get; set; } = string.Empty;
}
public class LoginResp
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public UserProfileDto UserProfile { get; set; } = new();
}
public class UserProfileDto
{
    public long Id { get; set; }
    public string NickName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public RoleType RoleType { get; set; }
    public DateTime LoveStartTime { get; set; }
}

// ---------- 首页 ----------
public class LoveInfoDto
{
    public bool HasLoveStart { get; set; }
    public int TotalDays { get; set; }
    public int TotalHours { get; set; }
    public int TotalMinutes { get; set; }
    public DateTime? LoveStartTime { get; set; }
}
public class ChartPointDto
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
}
public class DashboardDataDto
{
    public List<ChartPointDto> MoodTrend { get; set; } = new();
    public List<ChartPointDto> ConflictTrend { get; set; } = new();
    public double WishCompleteRate { get; set; }
    public AccountSummaryDto AccountSummary { get; set; } = new();
    /// <summary>连续互动天数：以「任一方当日有内容产出（日记/愿望/矛盾/留言/足迹/相册/照片）」为活跃日，
    /// 从今天（或昨天若今天尚未记录）往前连续计数的天数。由 HomeService 实时派生并缓存到当日。</summary>
    public int ActiveStreakDays { get; set; }
}
public class AccountSummaryDto
{
    public decimal Income { get; set; }
    public decimal Expend { get; set; }
    public decimal Balance => Income - Expend;
}

// ---------- 纪念日 ----------
public class AnniversaryDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AnniversaryType AnniversaryType { get; set; }
    public DateTime TargetDate { get; set; }
    public string? CoverImage { get; set; }
    public int RemindDays { get; set; }
    public int DaysLeft { get; set; } // 前端展示用，后端按下一次发生日期推算
    public bool IsYearly { get; set; } // 是否每年重复
    public DateTime? NextOccurrence { get; set; } // 下一次实际发生日期（每年重复则滚动到下一年）
    public long CreateUserId { get; set; }
    public DateTime CreateTime { get; set; }
}
public class AnniversaryReq
{
    public string Name { get; set; } = string.Empty;
    public AnniversaryType AnniversaryType { get; set; }
    public DateTime TargetDate { get; set; }
    public string? CoverImage { get; set; }
    public int RemindDays { get; set; }
    public bool IsYearly { get; set; } // 是否每年重复
}

// ---------- 情侣共享设置 ----------
public class CoupleSettingDto
{
    public DateTime? LoveStartTime { get; set; }
    public string? CoupleName { get; set; }
    public string? CoupleAvatar { get; set; }
}
public class SetLoveStartReq
{
    public DateTime LoveStartTime { get; set; }
}
public class UpdateCoupleSettingReq
{
    // 相恋纪念日可由任一方设置 / 修改（修改后会同步双方首页的恋爱时长计算）。
    public DateTime? LoveStartTime { get; set; }
    public string? CoupleName { get; set; }
    public string? CoupleAvatar { get; set; }
}

// ---------- 绑定对方 ----------
public class PartnerInfoDto
{
    public long Id { get; set; }
    public string NickName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public RoleType RoleType { get; set; }
}
public class BindStatusDto
{
    public bool IsBound { get; set; }
    public PartnerInfoDto? Partner { get; set; }
    public string? CoupleId { get; set; }
    public bool CanInvite { get; set; }
}
public class InviteDto
{
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

// ---------- 足迹 / 自定义计数卡 ----------
public class FootprintDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Emoji { get; set; } = "✨";
    public int Count { get; set; }
    public DateTime? LastIncrementTime { get; set; }
    public int? TargetCount { get; set; }
    public string? Description { get; set; }
    public long CreateUserId { get; set; }
    public DateTime CreateTime { get; set; }
}
public class FootprintReq
{
    public string Title { get; set; } = string.Empty;
    public string Emoji { get; set; } = "✨";
    public int? TargetCount { get; set; }
    public string? Description { get; set; }
}

// ---------- 每日一句温情语录 ----------
public class DailyQuoteDto
{
    public string Content { get; set; } = string.Empty;
    public string? Author { get; set; }
}
