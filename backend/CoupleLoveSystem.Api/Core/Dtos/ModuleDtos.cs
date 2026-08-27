using System.ComponentModel.DataAnnotations;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Enums;

namespace CoupleLoveSystem.Core.Dtos;

#region 日记 Diary
public class DiaryDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? MoodTag { get; set; }
    public int MoodScore { get; set; } = 5;
    public PermissionType PermissionType { get; set; } = PermissionType.Public;
    public string? Weather { get; set; }
    public DateTime? DiaryDate { get; set; }
    public long CreateUserId { get; set; }
    public DateTime CreateTime { get; set; }
}
public class DiaryReq
{
    [Required(ErrorMessage = "日记标题不能为空")]
    [StringLength(FieldLimits.ShortName, ErrorMessage = "标题过长")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "日记内容不能为空")]
    [StringLength(FieldLimits.RichText, ErrorMessage = "内容过长")]
    public string Content { get; set; } = string.Empty;

    [StringLength(FieldLimits.Color, ErrorMessage = "心情标签过长")]
    public string? MoodTag { get; set; }

    [Range(1, 10, ErrorMessage = "心情评分须在 1-10 之间")]
    public int MoodScore { get; set; } = 5;

    public PermissionType PermissionType { get; set; } = PermissionType.Public;

    [StringLength(FieldLimits.Color, ErrorMessage = "天气信息过长")]
    public string? Weather { get; set; }

    public DateTime? DiaryDate { get; set; }
}
public class DiaryCommentDto
{
    public long Id { get; set; }
    public long DiaryId { get; set; }
    public string Content { get; set; } = string.Empty;
    public long CreateUserId { get; set; }
    public DateTime CreateTime { get; set; }
}
public class DiaryCommentReq
{
    public long DiaryId { get; set; }

    [Required(ErrorMessage = "评论内容不能为空")]
    [StringLength(FieldLimits.LongText, ErrorMessage = "评论过长")]
    public string Content { get; set; } = string.Empty;
}
#endregion

#region 愿望 Wish
public class WishDto
{
    public long Id { get; set; }
    public WishType WishType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? ExpectTime { get; set; }
    public int Priority { get; set; } = 2;
    public WishStatus Status { get; set; } = WishStatus.NotStart;
    public long? ClaimUserId { get; set; }
    public string? ClaimUserName { get; set; }
    public DateTime? CompleteTime { get; set; }
    public string? CompleteRemark { get; set; }
    public string? CompleteImage { get; set; }
    public long CreateUserId { get; set; }
    public DateTime CreateTime { get; set; }
}
public class WishReq
{
    public WishType WishType { get; set; }

    [Required(ErrorMessage = "愿望标题不能为空")]
    [StringLength(FieldLimits.ShortName, ErrorMessage = "标题过长")]
    public string Title { get; set; } = string.Empty;

    [StringLength(FieldLimits.LongText, ErrorMessage = "描述过长")]
    public string? Description { get; set; }

    public DateTime? ExpectTime { get; set; }

    [Range(1, 5, ErrorMessage = "优先级须在 1-5 之间")]
    public int Priority { get; set; } = 2;

    public WishStatus Status { get; set; } = WishStatus.NotStart;
}
public class WishClaimReq { public long Id { get; set; } }
public class WishCompleteReq
{
    public long Id { get; set; }

    [StringLength(FieldLimits.LongText, ErrorMessage = "完成备注过长")]
    public string? CompleteRemark { get; set; }

    [StringLength(FieldLimits.Url, ErrorMessage = "完成图片路径过长")]
    public string? CompleteImage { get; set; }
}
#endregion

#region 待办 Todo
public class TodoDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDone { get; set; }
    public DateTime? DoneTime { get; set; }
    public long? DoneUserId { get; set; }
    public string? DoneUserName { get; set; }
    public int Priority { get; set; } = 2;
    public DateTime? DueTime { get; set; }
    public string? Category { get; set; }
    public long? AssigneeUserId { get; set; }
    public string? AssigneeName { get; set; }
    public long CreateUserId { get; set; }
    public DateTime CreateTime { get; set; }
}
public class TodoReq
{
    [Required(ErrorMessage = "待办标题不能为空")]
    [StringLength(FieldLimits.ShortName, ErrorMessage = "标题过长")]
    public string Title { get; set; } = string.Empty;

    [StringLength(FieldLimits.LongText, ErrorMessage = "描述过长")]
    public string? Description { get; set; }

    [Range(1, 3, ErrorMessage = "优先级须在 1-3 之间")]
    public int Priority { get; set; } = 2;

    public DateTime? DueTime { get; set; }

    [StringLength(FieldLimits.Color, ErrorMessage = "分类标签过长")]
    public string? Category { get; set; }

    public long? AssigneeUserId { get; set; }
}
public class TodoIdReq { public long Id { get; set; } }
public class TodoAssignReq { public long Id { get; set; } public long? AssigneeUserId { get; set; } }
#endregion

#region 留言板 Board
public class BoardMessageDto
{
    public long Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AuthorName { get; set; }
    public string? Color { get; set; }
    public bool Pinned { get; set; }
    public string? ImageUrl { get; set; }
    public long? ReceiverUserId { get; set; }
    public bool IsPrivate { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public bool IsUnlocked { get; set; }
    public long CreateUserId { get; set; }
    public DateTime CreateTime { get; set; }
}
public class BoardMessageReq
{
    [Required(ErrorMessage = "留言内容不能为空")]
    [StringLength(FieldLimits.BoardText, ErrorMessage = "留言过长")]
    public string Content { get; set; } = string.Empty;

    [StringLength(FieldLimits.Color, ErrorMessage = "装饰色过长")]
    public string? Color { get; set; }

    [StringLength(FieldLimits.Url, ErrorMessage = "配图路径过长")]
    public string? ImageUrl { get; set; }

    public bool IsPrivate { get; set; }
    public long? ReceiverUserId { get; set; }
    public DateTime? ScheduledAt { get; set; }
}
public class BoardMessageIdReq { public long Id { get; set; } }
#endregion

#region 默契问答 Quiz
public class QuizQuestionDto
{
    public long Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public string? Category { get; set; }
    public bool IsBuiltin { get; set; }
}
public class QuizQuestionReq
{
    [Required(ErrorMessage = "题面不能为空")]
    [StringLength(FieldLimits.ShortText, ErrorMessage = "题面过长")]
    public string Text { get; set; } = string.Empty;

    [MinLength(2, ErrorMessage = "至少需要 2 个选项")]
    [MaxLength(20, ErrorMessage = "选项过多")]
    public List<string> Options { get; set; } = new();

    [StringLength(FieldLimits.Color, ErrorMessage = "分类过长")]
    public string? Category { get; set; }
}
public class QuizRoundDto
{
    public long Id { get; set; }
    public long QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public string? Category { get; set; }

    public long? FirstUserId { get; set; }
    public int? FirstAnswer { get; set; }
    public DateTime? FirstAnsweredTime { get; set; }
    public long? SecondUserId { get; set; }
    public int? SecondAnswer { get; set; }
    public DateTime? SecondAnsweredTime { get; set; }

    public bool IsRevealed { get; set; }
    public bool IsMatched { get; set; }

    /// <summary>当前请求者是否已作答（前端据此决定显示选项还是等待态）。</summary>
    public bool MyAnswered { get; set; }
    /// <summary>当前请求者选的选项索引；未揭晓前不暴露对方选项。</summary>
    public int? MyAnswer { get; set; }
    /// <summary>对方是否已作答。</summary>
    public bool MateAnswered { get; set; }

    public long CreateUserId { get; set; }
    public DateTime CreateTime { get; set; }
}
/// <summary>发起一局：QuestionId 为空则从题库随机抽一题（优先抽没玩过的）。</summary>
public class QuizStartReq { public long? QuestionId { get; set; } }
public class QuizAnswerReq { public long RoundId { get; set; } [Range(0, 100, ErrorMessage = "选项索引不合法")] public int Answer { get; set; } }
public class QuizIdReq { public long Id { get; set; } }
public class QuizStatsDto
{
    public int TotalRounds { get; set; }
    public int RevealedRounds { get; set; }
    public int MatchedRounds { get; set; }
    /// <summary>默契率 0-100（按已揭晓局计算）。</summary>
    public int MatchRate { get; set; }
    public int PendingRounds { get; set; }
}
#endregion

#region 相册 Album + Image
public class AlbumDto
{
    public long Id { get; set; }
    public string AlbumName { get; set; } = string.Empty;
    public string? Cover { get; set; }
    public string? Remark { get; set; }
    public int ImageCount { get; set; }
    public long CreateUserId { get; set; }
    public DateTime CreateTime { get; set; }
}
public class AlbumReq
{
    [Required(ErrorMessage = "相册名称不能为空")]
    [StringLength(FieldLimits.ShortName, ErrorMessage = "相册名称过长")]
    public string AlbumName { get; set; } = string.Empty;

    [StringLength(FieldLimits.Url, ErrorMessage = "封面路径过长")]
    public string? Cover { get; set; }

    [StringLength(FieldLimits.ShortText, ErrorMessage = "相册备注过长")]
    public string? Remark { get; set; }
}
public class ImageDto
{
    public long Id { get; set; }
    public long AlbumId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Remark { get; set; }
    public DateTime? ShootTime { get; set; }
    public string? Location { get; set; }
    public long CreateUserId { get; set; }
    public DateTime CreateTime { get; set; }
}
#endregion

#region 矛盾 Conflict
public class ConflictDto
{
    public long Id { get; set; }
    public DateTime OccurTime { get; set; }
    public string Summary { get; set; } = string.Empty;
    public ConflictLevel ConflictLevel { get; set; } = ConflictLevel.Small;
    public string? MyThoughtA { get; set; }
    public string? MyThoughtB { get; set; }
    public DateTime? ReconcileTime { get; set; }
    public string? ReconcileWay { get; set; }
    public string? ReflectA { get; set; }
    public string? ReflectB { get; set; }
    public string? RuleConclusion { get; set; }
    public long CreateUserId { get; set; }
    public DateTime CreateTime { get; set; }
}
public class ConflictReq
{
    public DateTime OccurTime { get; set; }

    [Required(ErrorMessage = "矛盾摘要不能为空")]
    [StringLength(FieldLimits.ShortText, ErrorMessage = "摘要过长")]
    public string Summary { get; set; } = string.Empty;

    public ConflictLevel ConflictLevel { get; set; } = ConflictLevel.Small;

    [StringLength(FieldLimits.ConflictText, ErrorMessage = "想法记录过长")]
    public string? MyThoughtA { get; set; }

    [StringLength(FieldLimits.ConflictText, ErrorMessage = "想法记录过长")]
    public string? MyThoughtB { get; set; }

    public DateTime? ReconcileTime { get; set; }

    [StringLength(FieldLimits.ConflictText, ErrorMessage = "和解方式过长")]
    public string? ReconcileWay { get; set; }

    [StringLength(FieldLimits.ConflictText, ErrorMessage = "反思过长")]
    public string? ReflectA { get; set; }

    [StringLength(FieldLimits.ConflictText, ErrorMessage = "反思过长")]
    public string? ReflectB { get; set; }

    [StringLength(FieldLimits.ConflictText, ErrorMessage = "结论过长")]
    public string? RuleConclusion { get; set; }
}
#endregion

#region 记账 Account
public class AccountRecordDto
{
    public long Id { get; set; }
    public AccountRecordType RecordType { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime RecordTime { get; set; }
    public string? Remark { get; set; }
    public long CreateUserId { get; set; }
    public DateTime CreateTime { get; set; }
}
public class AccountRecordReq
{
    public AccountRecordType RecordType { get; set; }

    [Required(ErrorMessage = "记账分类不能为空")]
    [StringLength(FieldLimits.Color, ErrorMessage = "分类过长")]
    public string Category { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public DateTime RecordTime { get; set; }

    [StringLength(FieldLimits.ShortText, ErrorMessage = "备注过长")]
    public string? Remark { get; set; }
}

/// <summary>预算设置项：按 年/月/分类 唯一（分类为 null 表示当月总预算）。</summary>
public class BudgetDto
{
    public long Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string? Category { get; set; }
    public decimal LimitAmount { get; set; }
}

public class BudgetSetReq
{
    [Range(2000, 2999, ErrorMessage = "年份不合法")]
    public int Year { get; set; }

    [Range(1, 12, ErrorMessage = "月份须在 1-12 之间")]
    public int Month { get; set; }

    [StringLength(FieldLimits.Color, ErrorMessage = "分类过长")]
    public string? Category { get; set; }

    public decimal LimitAmount { get; set; }
}

/// <summary>分类支出统计：金额、对应分类预算、是否超支。</summary>
public class MonthlyCategoryStat
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal? Budget { get; set; }
    public bool IsOverspent { get; set; }
}

/// <summary>某月预算总览：收支、当月总预算、剩余、是否超支、分类明细。</summary>
public class MonthlyBudgetDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal? TotalBudget { get; set; }
    public decimal Remaining { get; set; }
    public bool IsOverspent { get; set; }
    public List<MonthlyCategoryStat> Categories { get; set; } = new();
}

/// <summary>月度趋势点：某月收支汇总（共近 6 个月，含当月）。</summary>
public class AccountTrendDto
{
    public string Month { get; set; } = string.Empty; // "yyyy-MM"
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
}

/// <summary>记账统计：当月收支 + 近 6 个月收支趋势，供趋势/分类可视化。</summary>
public class AccountStatisticsDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal MonthIncome { get; set; }
    public decimal MonthExpense { get; set; }
    public List<AccountTrendDto> Trend { get; set; } = new();
}
#endregion

#region 约会 DatePlan
public class DateRecordDto
{
    public long Id { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? PlanTime { get; set; }
    public DateTime? RealTime { get; set; }
    public string? Location { get; set; }
    public decimal? Budget { get; set; }
    public decimal? RealCost { get; set; }
    public int? ExperienceScore { get; set; }
    public string? Remark { get; set; }
    public long CreateUserId { get; set; }
    public DateTime CreateTime { get; set; }
}
public class DateRecordReq
{
    public bool IsCompleted { get; set; }
    public DateTime? PlanTime { get; set; }
    public DateTime? RealTime { get; set; }

    [StringLength(FieldLimits.ShortText, ErrorMessage = "地点过长")]
    public string? Location { get; set; }

    public decimal? Budget { get; set; }
    public decimal? RealCost { get; set; }

    [Range(1, 5, ErrorMessage = "体验评分须在 1-5 之间")]
    public int? ExperienceScore { get; set; }

    [StringLength(FieldLimits.LongText, ErrorMessage = "备注过长")]
    public string? Remark { get; set; }
}
public class DateStatsDto
{
    public int TotalDates { get; set; }   // 已完成约会次数
    public double AvgScore { get; set; }  // 平均评分（1-5）
}
#endregion

#region 系统消息 Message
public class SystemMessageDto
{
    public long Id { get; set; }
    public long ReceiverUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public MessageType MessageType { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreateTime { get; set; }
}
public class MessageReadReq { public long Id { get; set; } }
#endregion

#region 时间轴 Timeline（聚合）
public class TimelineItemDto
{
    public long Id { get; set; }
    public string Type { get; set; } = string.Empty; // anniversary / diary / wish / conflict
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Summary { get; set; }
    public long RelatedId { get; set; }
    // 纪念日专用：是否每年重复、下一次实际发生日期（时间轴展示「每年」徽标与下次日期）
    public bool IsYearly { get; set; }
    public DateTime? NextOccurrence { get; set; }
}
#endregion

#region 用户 / 导出
public class UpdateProfileReq
{
    [StringLength(FieldLimits.ShortName, ErrorMessage = "昵称过长")]
    public string? NickName { get; set; }

    [StringLength(FieldLimits.Url, ErrorMessage = "头像路径过长")]
    public string? Avatar { get; set; }

    [StringLength(FieldLimits.ShortText, ErrorMessage = "原密码过长")]
    public string? OldPassword { get; set; }

    [StringLength(FieldLimits.ShortText, ErrorMessage = "新密码过长")]
    public string? NewPassword { get; set; }
}
public class ExportResp
{
    /// <summary>一次性下载令牌：映射到临时目录中的 zip 文件，带短 TTL，下载后即作废。绝不返回公开可猜的 URL。</summary>
    public string Token { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int MediaCount { get; set; }
}
#endregion

