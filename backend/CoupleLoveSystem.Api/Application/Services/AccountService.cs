using CoupleLoveSystem.Api;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Text;

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

    /// <summary>记账统计：当月收支 + 近 6 个月（含当月）收支趋势，供月度趋势/分类可视化。</summary>
    public async Task<AccountStatisticsDto> StatisticsAsync(int year, int month, long currentUserId, CancellationToken ct = default)
    {
        if (year < 2000 || year > 2100) throw new ConflictException("年份不合法");
        if (month < 1 || month > 12) throw new ConflictException("月份需为 1-12");

        var all = await _db.AccountRecords.AsNoTracking().ToListAsync(ct);
        var monthRecords = all.Where(a => a.RecordTime.Year == year && a.RecordTime.Month == month).ToList();

        // 近 6 个月（含当月）逐月收支
        var trend = new List<AccountTrendDto>();
        var cursor = new DateTime(year, month, 1).AddMonths(-5);
        for (var i = 0; i < 6; i++)
        {
            var y = cursor.Year; var m = cursor.Month;
            var recs = all.Where(a => a.RecordTime.Year == y && a.RecordTime.Month == m).ToList();
            trend.Add(new AccountTrendDto
            {
                Month = $"{y:D4}-{m:D2}",
                Income = recs.Where(a => a.RecordType == AccountRecordType.Income).Sum(a => a.Amount),
                Expense = recs.Where(a => a.RecordType == AccountRecordType.Expend).Sum(a => a.Amount),
            });
            cursor = cursor.AddMonths(1);
        }

        return new AccountStatisticsDto
        {
            Year = year,
            Month = month,
            MonthIncome = monthRecords.Where(a => a.RecordType == AccountRecordType.Income).Sum(a => a.Amount),
            MonthExpense = monthRecords.Where(a => a.RecordType == AccountRecordType.Expend).Sum(a => a.Amount),
            Trend = trend,
        };
    }

    /// <summary>查询某年某月全部账单（按时间倒序），供 CSV 导出。</summary>
    public async Task<List<CoupleAccountRecord>> RecordsInMonthAsync(int year, int month, CancellationToken ct = default)
        => await _db.AccountRecords.AsNoTracking()
            .Where(a => a.RecordTime.Year == year && a.RecordTime.Month == month)
            .OrderByDescending(a => a.RecordTime)
            .ToListAsync(ct);

    /// <summary>把某月账单渲染为 CSV 文本（UTF-8 带 BOM，Excel 直接打开不乱码；逗号/引号做兼容转义）。</summary>
    public static string ExportCsv(int year, int month, IEnumerable<CoupleAccountRecord> records)
    {
        var sb = new StringBuilder();
        sb.Append('\uFEFF'); // BOM，Excel 识别 UTF-8
        sb.AppendLine("日期,类型,分类,金额,备注");
        foreach (var r in records.OrderByDescending(r => r.RecordTime))
        {
            var type = r.RecordType == AccountRecordType.Income ? "收入" : "支出";
            var cat = r.Category.Replace(",", "，").Replace("\"", "“");
            var remark = (r.Remark ?? "").Replace(",", "，").Replace("\"", "“").Replace("\r", " ").Replace("\n", " ");
            sb.AppendLine($"{r.RecordTime:yyyy-MM-dd},{type},{cat},{r.Amount:F2},{remark}");
        }
        return sb.ToString();
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
