using CoupleLoveSystem.Api;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Core.Enums;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CoupleLoveSystem.Application.Services;

/// <summary>绑定对方：生成邀请码 / 凭码加入 / 查询状态 / 解除绑定。绑定后双方共享同一 CoupleId 与全部恋爱数据。</summary>
public class PartnerService
{
    private readonly CoupleDbContext _db;
    private readonly SystemMessageEmailNotifier _notifier;
    private readonly AuthService _auth;
    private readonly SyncBroadcaster _broadcaster;
    public PartnerService(CoupleDbContext db, SystemMessageEmailNotifier notifier, AuthService auth, SyncBroadcaster broadcaster)
        => (_db, _notifier, _auth, _broadcaster) = (db, notifier, auth, broadcaster);

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

    /// <summary>凭邀请码加入：把双方并入同一 CoupleId 并互指 PartnerId。同一时刻仅允许两人成双。
    /// 加入后双方 CoupleId 已更新，但旧 JWT 的 cid 声明仍是旧值（多为空），全局过滤器会据此挡掉真实数据
    /// （表现为「绑定成功却空库」）。故此处为「加入方」重签全新令牌一并返回；并为「邀请方」广播 partner 信号，
    /// 前端收到后即刷新自身令牌，双方都能立刻看到共享数据。</summary>
    public async Task<JoinResultDto> JoinAsync(string code, long userId, CancellationToken ct = default)
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

        // 原子占用邀请方：仅当其 PartnerId 仍为空（未被并发的另一个 join 抢先）才写入，
        // 从根本上杜绝「同一邀请码被两个人并发 join 导致邀请方被绑定给两人」的竞态（审计 P2-14）。
        // 关系型库用 ExecuteUpdate 条件更新 + 事务；InMemory 测试库不支持，降级为读-改变更（仅测试用）。
        IDbContextTransaction? tx = _db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory"
            ? null
            : await _db.Database.BeginTransactionAsync(ct);
        var cid = inviter.CoupleId ?? Guid.NewGuid().ToString("N");
        bool claimed;
        if (_db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
        {
            var rows = await _db.Users
                .Where(u => u.Id == inviter.Id && u.PartnerId == null && u.BindCode == code && u.BindCodeExpire > DateTime.UtcNow)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.PartnerId, me.Id)
                    .SetProperty(u => u.CoupleId, cid)
                    .SetProperty(u => u.BindCode, (string?)null)
                    .SetProperty(u => u.BindCodeExpire, (DateTime?)null), ct);
            claimed = rows > 0;
        }
        else
        {
            inviter.PartnerId = me.Id;
            inviter.CoupleId = cid;
            inviter.BindCode = null;
            inviter.BindCodeExpire = null;
            claimed = true;
        }
        if (!claimed)
            throw new ConflictException("对方刚刚被绑定了，请向 TA 重新索取邀请码");

        me.CoupleId = cid;
        me.PartnerId = inviter.Id;
        me.BindCode = null; me.BindCodeExpire = null;
        _db.Users.Update(me);

        var bindMsg = new CoupleSystemMessage
        {
            CreateUserId = me.Id,
            ReceiverUserId = inviter.Id,
            Title = "TA 已与你绑定 💞",
            Content = $"{me.NickName} 已通过邀请码加入，你们现在可以双向同步所有恋爱数据啦",
            MessageType = MessageType.Other
        };
        _db.SystemMessages.Add(bindMsg);
        await _db.SaveChangesAsync(ct);
        if (tx != null) await tx.CommitAsync(ct);

        // 以下为 DB 事务之外的副作用（邮件 / 签令牌 / 广播），不占用事务，避免慢 SMTP 拉长锁。
        await _notifier.NotifyAsync(bindMsg, ct);
        // 为加入方重签令牌（cid 已是最新 CoupleId）；邀请方将通过下方 partner 广播触发自行刷新
        var tokens = await _auth.IssueTokensForUserAsync(me.Id, ct);
        // 广播 partner 信号：此刻双方 token 的 cid 仍为旧值，均落在 anon 组，故能互相送达；
        // 邀请方前端据此刷新令牌，立刻获得正确 cid。
        await _broadcaster.NotifyAsync("partner", ct);

        return new JoinResultDto { Partner = Map(inviter), Tokens = tokens };
    }

    /// <summary>解除绑定：双方互解，回到未绑定状态（相恋纪念日等情侣共享数据不受影响）。
    /// 解绑后 CoupleId 置空，旧令牌的 cid 仍指向原空间，可被过滤器放行读取已解绑方的数据——故为重绑方重签
    /// cid="" 的全新令牌并返回，同时向对方广播 partner 信号促其刷新，杜绝解绑后仍可读旧数据的越权窗口。</summary>
    public async Task<LoginResp> UnbindAsync(long userId, CancellationToken ct = default)
    {
        var me = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct)
            ?? throw new NotFoundException("用户不存在");
        if (me.PartnerId == null) return await _auth.IssueTokensForUserAsync(me.Id, ct);

        var partner = await _db.Users.FirstOrDefaultAsync(u => u.Id == me.PartnerId && !u.IsDeleted, ct);
        me.PartnerId = null; me.CoupleId = null; me.BindCode = null; me.BindCodeExpire = null;
        if (partner != null)
        {
            partner.PartnerId = null; partner.CoupleId = null; partner.BindCode = null; partner.BindCodeExpire = null;
        }
        await _db.SaveChangesAsync(ct);

        var tokens = await _auth.IssueTokensForUserAsync(me.Id, ct);
        if (partner != null) await _broadcaster.NotifyAsync("partner", ct);
        return tokens;
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
