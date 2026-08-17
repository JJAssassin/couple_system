using CoupleLoveSystem.Core.Enums;

namespace CoupleLoveSystem.Core.Entities;

/// <summary>所有业务表统一基础字段（逻辑删除，不物理删除情感数据）。</summary>
public abstract class BaseEntity
{
    public long Id { get; set; }
    public long CreateUserId { get; set; }
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
    public long? UpdateUserId { get; set; }
    public DateTime? UpdateTime { get; set; }
    public bool IsDeleted { get; set; }
    /// <summary>情侣空间标识：内容数据归属哪个情侣。由全局查询过滤器 + SaveChanges 拦截器自动按当前登录用户所属情侣隔离。</summary>
    public string? CoupleId { get; set; }
}

/// <summary>标记「受情侣空间隔离的内容实体」。DbContext 会为其自动追加 CoupleId 全局查询过滤器，
/// 并在插入时由 SaveChanges 拦截器盖章当前情侣空间，从而实现「同库多对情侣互不串数据」。
/// 属性 <see cref="CoupleId"/> 由 <see cref="BaseEntity"/> 统一实现，接口仅用于约束与识别。</summary>
public interface ICoupleScoped
{
    /// <summary>情侣空间标识（实际存储于 BaseEntity.CoupleId）。</summary>
    string? CoupleId { get; set; }
}

[Broadcast("partner")]
public class CoupleUser : BaseEntity
{
    public string UserName { get; set; } = string.Empty;
    public string NickName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public DateTime LoveStartTime { get; set; }
    public RoleType RoleType { get; set; }

    /// <summary>绑定关系：PartnerId 指向另一半的用户 Id；CoupleId（继承自 BaseEntity）为双方共享的情侣空间标识（同空间即一对）。</summary>
    public long? PartnerId { get; set; }
    /// <summary>邀请码（生成方持有，10 分钟有效），用于对方输入后加入情侣空间。</summary>
    public string? BindCode { get; set; }
    public DateTime? BindCodeExpire { get; set; }

    /// <summary>接收站内消息邮件通知的邮箱；为空则不发邮件。由未来「设置」页维护（演示/种子数据未填，需配置 SMTP 且填邮箱后才会真正发信）。</summary>
    public string? Email { get; set; }
}

[Broadcast("anniversary")]
public class CoupleAnniversary : BaseEntity, ICoupleScoped
{
    public string Name { get; set; } = string.Empty;
    public AnniversaryType AnniversaryType { get; set; }
    public DateTime TargetDate { get; set; }
    public string? CoverImage { get; set; }
    public int RemindDays { get; set; }   // 0/1/3/7/15
    public DateTime? NextRemindTime { get; set; }
    public bool IsYearly { get; set; }    // 是否每年重复：true=每年同一天再来一次；false=一次性

    /// <summary>计算下一次实际发生的日期（单一事实来源，HomeService / 仓库 / 定时任务共用）。
    /// 每年重复：滚动到当前/下一个相同月日；一次性：若尚未过期返回 TargetDate，否则返回 null。</summary>
    public DateTime? ComputeNextOccurrence()
    {
        var now = DateTime.UtcNow.Date;
        if (!IsYearly) return TargetDate.Date >= now ? TargetDate.Date : (DateTime?)null;
        // 处理闰年 2/29：非闰年没有 2/29，回退到 2/28，保证每年都有“下一次发生日期”而不会抛异常
        var occ = MakeOccurrenceDate(now.Year);
        if (occ < now) occ = occ.AddYears(1);
        return occ;
    }

    private DateTime MakeOccurrenceDate(int year)
    {
        var month = TargetDate.Month;
        var day = TargetDate.Day;
        if (month == 2 && day == 29 && !DateTime.IsLeapYear(year)) day = 28;
        return new DateTime(year, month, day);
    }
}

[Broadcast("diary")]
public class CoupleDiary : BaseEntity, IProtectable, ICoupleScoped
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty; // 富文本，入库前 HtmlSanitizer 净化
    public string? MoodTag { get; set; }
    public int MoodScore { get; set; } = 5; // 1-10
    public PermissionType PermissionType { get; set; } = PermissionType.Public;
    public string? Weather { get; set; }
    public DateTime? DiaryDate { get; set; }
}

[Broadcast("diary")]
public class CoupleDiaryComment : BaseEntity, ICoupleScoped
{
    public long DiaryId { get; set; }
    public string Content { get; set; } = string.Empty;
}

[Broadcast("wish")]
public class CoupleWish : BaseEntity, ICoupleScoped
{
    public WishType WishType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? ExpectTime { get; set; }
    public int Priority { get; set; } = 2;
    public WishStatus Status { get; set; } = WishStatus.NotStart;
    public long? ClaimUserId { get; set; }
    public DateTime? CompleteTime { get; set; }
    public string? CompleteRemark { get; set; }
    public string? CompleteImage { get; set; }
}

/// <summary>情侣共享待办清单：双方可添加、勾选完成、指派给对方，实时同步。整库即一对情侣，数据双方互通。</summary>
[Broadcast("todo")]
public class CoupleTodo : BaseEntity, ICoupleScoped
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDone { get; set; }
    public DateTime? DoneTime { get; set; }
    public long? DoneUserId { get; set; }
    public string? DoneUserName { get; set; }
    public int Priority { get; set; } = 2; // 1-3，越大越优先
    public DateTime? DueTime { get; set; }
    public string? Category { get; set; } // 分类标签（购物/家务/出行…），可选
    public long? AssigneeUserId { get; set; } // 责任人；null=双方共同
    public string? AssigneeName { get; set; }
}

[Broadcast("album")]
public class CoupleAlbum : BaseEntity, ICoupleScoped
{
    public string AlbumName { get; set; } = string.Empty;
    public string? Cover { get; set; }
    public string? Remark { get; set; }
}

[Broadcast("album")]
public class CoupleImage : BaseEntity, ICoupleScoped
{
    public long AlbumId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public DateTime? ShootTime { get; set; }
    public string? Location { get; set; }
}

[Broadcast("conflict")]
public class CoupleConflict : BaseEntity, ICoupleScoped
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

[Broadcast("letter")]
public class CoupleLetter : BaseEntity, ICoupleScoped
{
    public long ReceiverUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? CoverImage { get; set; }
    public DateTime UnlockTime { get; set; }   // 以服务器时间为准
    public bool IsUnlocked { get; set; }
}

[Broadcast("account")]
public class CoupleAccountRecord : BaseEntity, ICoupleScoped
{
    public AccountRecordType RecordType { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime RecordTime { get; set; }
    public string? Remark { get; set; }
}

public class CoupleDateRecord : BaseEntity, ICoupleScoped
{
    public bool IsCompleted { get; set; }
    public DateTime? PlanTime { get; set; }
    public DateTime? RealTime { get; set; }
    public string? Location { get; set; }
    public decimal? Budget { get; set; }
    public decimal? RealCost { get; set; }
    public int? ExperienceScore { get; set; } // 1-5
    public string? Remark { get; set; }
}

public class CoupleSystemMessage : BaseEntity, ICoupleScoped
{
    public long ReceiverUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public MessageType MessageType { get; set; }
    public bool IsRead { get; set; }
}

/// <summary>情侣级共享设置：整库即一对情侣，用固定 Key="global" 的单行承载共享态（相恋日期、情侣名等）。</summary>
[Broadcast("setting")]
public class CoupleSetting : BaseEntity
{
    public string Key { get; set; } = "global";
    public DateTime? LoveStartTime { get; set; } // 相恋纪念日（共享，任一方设置双方生效）
    public string? CoupleName { get; set; }
    public string? CoupleAvatar { get; set; }
}

/// <summary>足迹 / 自定义计数卡：情侣共享，记录「抱抱 / 亲亲 / 一起看过的电影」等可 +1 的小确幸。
/// 整库即一对情侣，数据双方实时互通，不做按用户隔离。</summary>
[Broadcast("footprint")]
public class CoupleFootprint : BaseEntity, ICoupleScoped
{
    public string Title { get; set; } = string.Empty;
    public string Emoji { get; set; } = "✨";
    public int Count { get; set; } = 0;
    public DateTime? LastIncrementTime { get; set; }
    public int? TargetCount { get; set; }   // 每日/阶段目标次数（可选）；达到后前端高亮
    public string? Description { get; set; } // 说明（可选）
}

/// <summary>每日一句温情语录：种子表，按日期确定性地选出当天展示的一句。</summary>
public class CoupleQuote : BaseEntity
{
    public string Content { get; set; } = string.Empty;
    public string? Author { get; set; }
    public int SortOrder { get; set; }
}
