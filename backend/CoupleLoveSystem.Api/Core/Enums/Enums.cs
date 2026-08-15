namespace CoupleLoveSystem.Core.Enums;

/// <summary>业务数据权限：后端强制兜底，前端只做 UI 隐藏。</summary>
public enum PermissionType
{
    Public = 1,        // 双方可读写
    PrivateSelf = 2,   // 仅本人可见，对方拿不到
    ViewOnlyOther = 3  // 对方可读不可写
}

public enum RoleType { PartnerA = 1, PartnerB = 2 }

public enum AnniversaryType { LoveDay = 1, Birthday = 2, MeetDay = 3, Custom = 4 }

public enum MoodScore : byte { } // 1-10，用 byte 即可

public enum WishType { Common = 1, Gift = 2, Target = 3 }

public enum WishStatus { NotStart = 1, Doing = 2, Completed = 3, Archive = 4 }

public enum ConflictLevel { Small = 1, Middle = 2, Serious = 3 }

public enum LetterUnlock : byte { } // 0/1

public enum AccountRecordType { Income = 1, Expend = 2 }

public enum MessageType { Anniversary = 1, LetterUnlock = 2, DiaryInteract = 3, WishComplete = 4, Other = 5 }
