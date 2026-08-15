using CoupleLoveSystem.Api;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CoupleLoveSystem.Application.Services;

public class AccountService
{
    private readonly IRepository<CoupleAccountRecord> _repo;
    private readonly CoupleDbContext _db;

    public AccountService(IRepository<CoupleAccountRecord> repo, CoupleDbContext db)
    {
        _repo = repo; _db = db;
    }

    /// <summary>记账服务：记录与管理情侣共同收支</summary>
    public async Task<PagedResult<AccountRecordDto>> ListAsync(int page, int pageSize, long currentUserId, CancellationToken ct = default)
    {
        var query = _db.AccountRecords.AsNoTracking().OrderByDescending(a => a.RecordTime);
        var total = await query.CountAsync(ct);
        var list = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<AccountRecordDto>
        {
            Items = list.Select(Map).ToList(),
            Total = total, Page = page, PageSize = pageSize
        };
    }

    public async Task<AccountRecordDto> GetAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var a = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("账号记录不存在");
        return Map(a);
    }

    public async Task<AccountRecordDto> CreateAsync(AccountRecordReq req, long currentUserId, CancellationToken ct = default)
    {
        if (req.Amount <= 0) throw new ConflictException("金额必须大于 0");
        var a = new CoupleAccountRecord
        {
            RecordType = req.RecordType,
            Category = req.Category,
            Amount = req.Amount,
            RecordTime = req.RecordTime,
            Remark = req.Remark,
            CreateUserId = currentUserId,
            CreateTime = DateTime.UtcNow
        };
        await _repo.AddAsync(a, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(a);
    }

    public async Task<AccountRecordDto> UpdateAsync(long id, AccountRecordReq req, long currentUserId, CancellationToken ct = default)
    {
        var a = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("账号记录不存在");
        a.RecordType = req.RecordType;
        a.Category = req.Category;
        a.Amount = req.Amount;
        a.RecordTime = req.RecordTime;
        a.Remark = req.Remark;
        a.UpdateUserId = currentUserId;
        a.UpdateTime = DateTime.UtcNow;
        _repo.Update(a);
        await _repo.SaveChangesAsync(ct);
        return Map(a);
    }

    public async Task DeleteAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var a = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("账号记录不存在");
        _repo.SoftDelete(a);
        await _repo.SaveChangesAsync(ct);
    }

    /// <summary>查询账户汇总（余额与收支统计）</summary>
    public async Task<AccountSummaryDto> SummaryAsync(long currentUserId, CancellationToken ct = default)
    {
        var list = await _db.AccountRecords.AsNoTracking().ToListAsync(ct);
        var income = list.Where(a => a.RecordType == AccountRecordType.Income).Sum(a => a.Amount);
        var expend = list.Where(a => a.RecordType == AccountRecordType.Expend).Sum(a => a.Amount);
        return new AccountSummaryDto
        {
            Income = income,
            Expend = expend
        };
    }

    public static AccountRecordDto Map(CoupleAccountRecord a) => new()
    {
        Id = a.Id,
        RecordType = a.RecordType,
        Category = a.Category,
        Amount = a.Amount,
        RecordTime = a.RecordTime,
        Remark = a.Remark,
        CreateUserId = a.CreateUserId,
        CreateTime = a.CreateTime
    };
}
