using System.ComponentModel.DataAnnotations;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Core.Enums;

namespace CoupleLoveSystem.Core.Dtos;

/// <summary>集中存放字段长度上限，与下方请求 DTO 的 [StringLength] 对齐，便于统一调整。</summary>
internal static class FieldLimits
{
    public const int ShortName = 100;     // 昵称 / 纪念日名 / 足迹标题 / 相册名 / 愿望标题 / 日记标题
    public const int ShortText = 200;     // 备注 / 地点 / 分类 等中等长度
    public const int LongText = 2000;     // 描述 / 留言 / 记账备注
    public const int RichText = 50000;    // 日记正文（富文本）
    public const int BoardText = 10000;   // 留言板内容
    public const int ConflictText = 10000;// 矛盾梳理各字段
    public const int Url = 500;           // 头像 / 封面 / 配图 等路径或 URL
    public const int Token = 512;         // 刷新令牌等
    public const int Color = 32;          // 装饰色 / 标签
    public const int Emoji = 16;          // 单 emoji
}

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
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(FieldLimits.ShortName, ErrorMessage = "用户名过长")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(FieldLimits.ShortText, ErrorMessage = "密码过长")]
    public string Password { get; set; } = string.Empty;
}
public class RefreshReq
{
    [Required(ErrorMessage = "刷新令牌不能为空")]
    [StringLength(FieldLimits.Token, ErrorMessage = "刷新令牌过长")]
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
    public string? LunarDate { get; set; } // 目标日的农历表示（农历X年X月X），前端展示用（审计 #14）
    public long CreateUserId { get; set; }
    public DateTime CreateTime { get; set; }
}
public class AnniversaryReq
{
    [Required(ErrorMessage = "纪念日的名称不能为空")]
    [StringLength(FieldLimits.ShortName, ErrorMessage = "名称过长")]
    public string Name { get; set; } = string.Empty;

    public AnniversaryType AnniversaryType { get; set; }
    public DateTime TargetDate { get; set; }

    [StringLength(FieldLimits.Url, ErrorMessage = "封面图路径过长")]
    public string? CoverImage { get; set; }

    [Range(0, 365, ErrorMessage = "提醒天数须在 0-365 之间")]
    public int RemindDays { get; set; }

    public bool IsYearly { get; set; } // 是否每年重复
}

// ---------- 情侣共享设置 ----------
public class CoupleSettingDto
{
    public DateTime? LoveStartTime { get; set; }
    public string? CoupleName { get; set; }
    public string? CoupleAvatar { get; set; }
    public string? LunarLoveStart { get; set; } // 相恋日的农历表示，前端展示用（审计 #14）
}
public class SetLoveStartReq
{
    public DateTime LoveStartTime { get; set; }
}
public class UpdateCoupleSettingReq
{
    // 相恋纪念日可由任一方设置 / 修改（修改后会同步双方首页的恋爱时长计算）。
    public DateTime? LoveStartTime { get; set; }

    [StringLength(FieldLimits.ShortName, ErrorMessage = "情侣名过长")]
    public string? CoupleName { get; set; }

    [StringLength(FieldLimits.Url, ErrorMessage = "情侣头像路径过长")]
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

/// <summary>加入结果：返回对方资料 + 为加入方重签的全新令牌（cid 已是最新 CoupleId）。</summary>
public class JoinResultDto
{
    public PartnerInfoDto Partner { get; set; } = new();
    public LoginResp Tokens { get; set; } = new();
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
    [Required(ErrorMessage = "足迹标题不能为空")]
    [StringLength(FieldLimits.ShortName, ErrorMessage = "标题过长")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "足迹图标不能为空")]
    [StringLength(FieldLimits.Emoji, ErrorMessage = "图标过长")]
    public string Emoji { get; set; } = "✨";

    [Range(1, 1_000_000, ErrorMessage = "目标次数不合法")]
    public int? TargetCount { get; set; }

    [StringLength(FieldLimits.LongText, ErrorMessage = "说明过长")]
    public string? Description { get; set; }
}

// ---------- 每日一句温情语录 ----------
public class DailyQuoteDto
{
    public string Content { get; set; } = string.Empty;
    public string? Author { get; set; }
}
