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
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? MoodTag { get; set; }
    public int MoodScore { get; set; } = 5;
    public PermissionType PermissionType { get; set; } = PermissionType.Public;
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
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? ExpectTime { get; set; }
    public int Priority { get; set; } = 2;
    public WishStatus Status { get; set; } = WishStatus.NotStart;
}
public class WishClaimReq { public long Id { get; set; } }
public class WishCompleteReq
{
    public long Id { get; set; }
    public string? CompleteRemark { get; set; }
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
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Priority { get; set; } = 2;
    public DateTime? DueTime { get; set; }
    public string? Category { get; set; }
    public long? AssigneeUserId { get; set; }
}
public class TodoIdReq { public long Id { get; set; } }
public class TodoAssignReq { public long Id { get; set; } public long? AssigneeUserId { get; set; } }
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
    public string AlbumName { get; set; } = string.Empty;
    public string? Cover { get; set; }
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
    public string Summary { get; set; } = string.Empty;
    public ConflictLevel ConflictLevel { get; set; } = ConflictLevel.Small;
    public string? MyThoughtA { get; set; }
    public string? MyThoughtB { get; set; }
    public DateTime? ReconcileTime { get; set; }
    public string? ReconcileWay { get; set; }
    public string? ReflectA { get; set; }
    public string? ReflectB { get; set; }
    public string? RuleConclusion { get; set; }
}
#endregion

#region 书信 Letter
public class LetterDto
{
    public long Id { get; set; }
    public long ReceiverUserId { get; set; }
    public string? ReceiverUserName { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? CoverImage { get; set; }
    public DateTime UnlockTime { get; set; }
    public bool IsUnlocked { get; set; }
    public long CreateUserId { get; set; }
    public DateTime CreateTime { get; set; }
}
public class LetterReq
{
    public long ReceiverUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? CoverImage { get; set; }
    public DateTime UnlockTime { get; set; }
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
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime RecordTime { get; set; }
    public string? Remark { get; set; }
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
    public string? Location { get; set; }
    public decimal? Budget { get; set; }
    public decimal? RealCost { get; set; }
    public int? ExperienceScore { get; set; }
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
    public string Type { get; set; } = string.Empty; // anniversary / diary / wish / conflict / checkin
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
    public string? NickName { get; set; }
    public string? Avatar { get; set; }
    public string? OldPassword { get; set; }
    public string? NewPassword { get; set; }
}
public class ExportResp
{
    public string DownloadUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int MediaCount { get; set; }
}
#endregion
