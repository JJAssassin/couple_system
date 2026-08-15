using CoupleLoveSystem.Api;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Enums;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Infrastructure.Repositories;

namespace CoupleLoveSystem.Application.Services;

/// <summary>
/// <summary>来信服务：含 content / cover 字段与 ReceiverUserId 可见性、UnlockTime 解锁；未到解锁时间时 IsUnlocked=false，信件对接收方不可见</summary>
public class LetterService
{
    private readonly IRepository<CoupleLetter> _letterRepo;
    private readonly IRepository<CoupleUser> _userRepo;
    private readonly IRepository<CoupleSystemMessage> _msgRepo;
    private readonly CoupleDbContext _db;
    private readonly SystemMessageEmailNotifier _notifier;

    public LetterService(IRepository<CoupleLetter> letterRepo, IRepository<CoupleUser> userRepo,
        IRepository<CoupleSystemMessage> msgRepo, CoupleDbContext db, SystemMessageEmailNotifier notifier)
    {
        _letterRepo = letterRepo; _userRepo = userRepo; _msgRepo = msgRepo; _db = db; _notifier = notifier;
    }

    public async Task<List<LetterDto>> ListAsync(long currentUserId, CancellationToken ct = default)
    {
        var list = await _letterRepo.ListAsync(
            l => l.ReceiverUserId == currentUserId || l.CreateUserId == currentUserId, ct);
        return list.OrderByDescending(l => l.CreateTime)
                   .Select(l => Map(l, currentUserId)).ToList();
    }

    public async Task<LetterDto> GetAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var l = await _letterRepo.GetByIdAsync(id, ct) ?? throw new NotFoundException("信件不存在");
        if (l.ReceiverUserId != currentUserId && l.CreateUserId != currentUserId)
            throw new NotFoundException("信件不存在");

        // 若发送方设置了解锁时间，则信件在解锁前对接收方不可见（仅自己可见草稿）
        if (!l.IsUnlocked && l.UnlockTime <= DateTime.UtcNow)
        {
            l.IsUnlocked = true;
            _letterRepo.Update(l);
            await _letterRepo.SaveChangesAsync(ct);
            await WriteUnlockMessageAsync(l, ct);
        }
        return Map(l, currentUserId);
    }

    public async Task<LetterDto> CreateAsync(LetterReq req, long currentUserId, CancellationToken ct = default)
    {
        var l = new CoupleLetter
        {
            ReceiverUserId = req.ReceiverUserId,
            Content = req.Content,
            CoverImage = req.CoverImage,
            UnlockTime = req.UnlockTime,
            IsUnlocked = false,
            CreateUserId = currentUserId,
            CreateTime = DateTime.UtcNow,
        };
        await _letterRepo.AddAsync(l, ct);

        // 若设置了解锁时间且未到，则信件处于锁定状态，接收方暂不可见
        if (req.UnlockTime <= DateTime.UtcNow)
        {
            l.IsUnlocked = true;
            _letterRepo.Update(l);
        }
        await _letterRepo.SaveChangesAsync(ct);
        if (l.IsUnlocked) await WriteUnlockMessageAsync(l, ct);

        return Map(l, currentUserId);
    }

    public async Task DeleteAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var l = await _letterRepo.GetByIdAsync(id, ct) ?? throw new NotFoundException("信件不存在");
        if (l.CreateUserId != currentUserId)
            throw new ForbiddenException("只能删除自己创建的来信");
        _letterRepo.SoftDelete(l);
        await _letterRepo.SaveChangesAsync(ct);
    }

    private async Task WriteUnlockMessageAsync(CoupleLetter l, CancellationToken ct)
    {
        var sender = await _userRepo.GetByIdAsync(l.CreateUserId, ct);
        var msg = new CoupleSystemMessage
        {
            ReceiverUserId = l.ReceiverUserId,
            MessageType = MessageType.LetterUnlock,
            Title = "新的来信",
            Content = $"{(sender?.NickName ?? "TA")} 给你写了一封定时来信，快去查看吧",
            IsRead = false,
            CreateUserId = l.CreateUserId,
            CreateTime = DateTime.UtcNow,
        };
        await _msgRepo.AddAsync(msg, ct);
        await _notifier.NotifyAsync(msg, ct);
        await _msgRepo.SaveChangesAsync(ct);
    }

    /// <summary>映射为 LetterDto：当前用户可见 content / cover；未解锁时接收方不展示具体内容</summary>
    private static LetterDto Map(CoupleLetter l, long currentUserId)
    {
        bool isReceiver = l.ReceiverUserId == currentUserId;
        return new LetterDto
        {
            Id = l.Id,
            ReceiverUserId = l.ReceiverUserId,
            ReceiverUserName = null,
            // 私密信件保护：未到解锁时间，接收方也无法查看内容
            Content = isReceiver ? l.Content : string.Empty,
            CoverImage = isReceiver ? l.CoverImage : null,
            UnlockTime = l.UnlockTime,
            IsUnlocked = l.IsUnlocked,
            CreateUserId = l.CreateUserId,
            CreateTime = l.CreateTime,
        };
    }
}
