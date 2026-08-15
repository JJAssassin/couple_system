using CoupleLoveSystem.Api;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Enums;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoupleLoveSystem.Application.Services;

/// <summary>绑定对方：生成邀请码 / 凭码加入 / 查询状态 / 解除绑定。整库即一对情侣，绑定后双方共享同一 CoupleId 与全部恋爱数据。</summary>
public class PartnerService
{
    private readonly CoupleDbContext _db;
    private readonly SystemMessageEmailNotifier _notifier;
    public PartnerService(CoupleDbContext db, SystemMessageEmailNotifier notifier) => (_db, _notifier) = (db, notifier);

    public async Task<BindStatusDto> GetStatusAsync(long userId, CancellationToken ct = default)
    {
        var me = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct)
            ?? throw new NotFoundException("用户不存在");
        if (me.PartnerId == null)
            return new BindStatusDto { IsBound = false, Partner = null, CoupleId = me.CoupleId, CanInvite = true };

        var partner = await _db.Users.FirstOrDefaultAsync(u => u.Id == me.PartnerId && !u.IsDeleted, ct);
        return new BindStatusDto
        {
            IsBound = true,
            Partner = partner == null ? null : Map(partner),
            CoupleId = me.CoupleId,
            CanInvite = false
        };
    }

    /// <summary>生成 6 位邀请码（10 分钟有效），绑定在邀请方账号上，等待对方输入加入。</summary>
    public async Task<InviteDto> CreateInviteAsync(long userId, CancellationToken ct = default)
    {
        var me = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct)
            ?? throw new NotFoundException("用户不存在");
        if (me.PartnerId != null)
            throw new ConflictException("你已经和 TA 绑定了，无需重复邀请");

        if (string.IsNullOrEmpty(me.CoupleId))
            me.CoupleId = Guid.NewGuid().ToString("N");

        var code = GenCode();
        me.BindCode = code;
        me.BindCodeExpire = DateTime.UtcNow.AddMinutes(10);
        await _db.SaveChangesAsync(ct);
        return new InviteDto { Code = code, ExpiresAt = me.BindCodeExpire.Value };
    }

    /// <summary>凭邀请码加入：把双方并入同一 CoupleId 并互指 PartnerId。同一时刻仅允许两人成双。</summary>
    public async Task<PartnerInfoDto> JoinAsync(string code, long userId, CancellationToken ct = default)
    {
        code = (code ?? "").Trim().ToUpperInvariant();
        var me = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct)
            ?? throw new NotFoundException("用户不存在");
        if (me.PartnerId != null)
            throw new ConflictException("你已经和 TA 绑定了，无法加入其他情侣空间");

        var inviter = await _db.Users.FirstOrDefaultAsync(u =>
            u.BindCode == code && u.BindCodeExpire > DateTime.UtcNow && u.Id != userId && !u.IsDeleted, ct);
        if (inviter == null)
            throw new ConflictException("邀请码无效或已过期，请向 TA 重新索取");
        if (inviter.PartnerId != null)
            throw new ConflictException("对方已经和其他人绑定了，换一个邀请码吧");

        var cid = inviter.CoupleId ?? Guid.NewGuid().ToString("N");
        inviter.CoupleId = cid;
        me.CoupleId = cid;
        inviter.PartnerId = me.Id;
        me.PartnerId = inviter.Id;
        inviter.BindCode = null; inviter.BindCodeExpire = null;
        me.BindCode = null; me.BindCodeExpire = null;
        await _db.SaveChangesAsync(ct);

        // 通知对方：TA 已与你绑定（对方打开 App 可见，且 SMTP 启用时还会收到邮件提醒）
        var bindMsg = new CoupleSystemMessage
        {
            CreateUserId = me.Id,
            ReceiverUserId = inviter.Id,
            Title = "TA 已与你绑定 💞",
            Content = $"{me.NickName} 已通过邀请码加入，你们现在可以双向同步所有恋爱数据啦",
            MessageType = MessageType.Other
        };
        _db.SystemMessages.Add(bindMsg);
        await _notifier.NotifyAsync(bindMsg, ct);
        await _db.SaveChangesAsync(ct);

        return Map(inviter);
    }

    /// <summary>解除绑定：双方互解，回到未绑定状态（相恋纪念日等情侣共享数据不受影响）。</summary>
    public async Task UnbindAsync(long userId, CancellationToken ct = default)
    {
        var me = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct)
            ?? throw new NotFoundException("用户不存在");
        if (me.PartnerId == null) return;

        var partner = await _db.Users.FirstOrDefaultAsync(u => u.Id == me.PartnerId && !u.IsDeleted, ct);
        me.PartnerId = null; me.CoupleId = null; me.BindCode = null; me.BindCodeExpire = null;
        if (partner != null)
        {
            partner.PartnerId = null; partner.CoupleId = null; partner.BindCode = null; partner.BindCodeExpire = null;
        }
        await _db.SaveChangesAsync(ct);
    }

    private static string GenCode()
    {
        // 去除易混字符（0/O、1/I/L）
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var sb = new System.Text.StringBuilder(6);
        for (int i = 0; i < 6; i++)
            sb.Append(chars[System.Security.Cryptography.RandomNumberGenerator.GetInt32(chars.Length)]);
        return sb.ToString();
    }

    private static PartnerInfoDto Map(CoupleUser u) => new()
    {
        Id = u.Id, NickName = u.NickName, Avatar = u.Avatar, RoleType = u.RoleType
    };
}
