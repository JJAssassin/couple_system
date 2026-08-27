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

    /// <summary>查询账户汇总（余额与收支统计）。数据库端 SUM，避免全表加载到内存。</summary>
    public async Task<AccountSummaryDto> SummaryAsync(long currentUserId, CancellationToken ct = default)
    {
        var income = await _db.AccountRecords.AsNoTracking()
            .Where(a => a.RecordType == AccountRecordType.Income)
            .SumAsync(a => a.Amount, ct);
        var expend = await _db.AccountRecords.AsNoTracking()
            .Where(a => a.RecordType == AccountRecordType.Expend)
            .SumAsync(a => a.Amount, ct);
        return new AccountSummaryDto
        {
            Income = income,
            Expend = expend
        };
    }

    /// <summary>记账统计：当月收支 + 近 6 个月（含当月）收支趋势，供月度趋势/分类可视化。SQL 端按年/月聚合一次返回。</summary>
    public async Task<AccountStatisticsDto> StatisticsAsync(int year, int month, long currentUserId, CancellationToken ct = default)
    {
        if (year < 2000 || year > 2100) throw new ConflictException("年份不合法");
        if (month < 1 || month > 12) throw new ConflictException("月份需为 1-12");

        // 一次性把「近 6 个月窗口」内的记录按 年/月 聚合到 SQL 层，避免全表加载
        var since = new DateTime(year, month, 1).AddMonths(-5);
        var until = new DateTime(year, month, 1).AddMonths(1); // 当月月末次日（不含）

        var rows = await _db.AccountRecords.AsNoTracking()
            .Where(a => a.RecordTime >= since && a.RecordTime < until)
            .GroupBy(a => new { a.RecordTime.Year, a.RecordTime.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Income = g.Sum(x => x.RecordType == AccountRecordType.Income ? x.Amount : 0m),
                Expense = g.Sum(x => x.RecordType == AccountRecordType.Expend ? x.Amount : 0m),
            })
            .ToListAsync(ct);

        // 以 (年,月) 为键，便于把无记录的月份补 0
        var byKey = rows.ToDictionary(r => (r.Year, r.Month), r => r);

        // 近 6 个月（含当月）逐月收支，无记录的月份补 0
        var trend = new List<AccountTrendDto>();
        var cursor = since;
        for (var i = 0; i < 6; i++)
        {
            byKey.TryGetValue((cursor.Year, cursor.Month), out var rec);
            trend.Add(new AccountTrendDto
            {
                Month = $"{cursor.Year:D4}-{cursor.Month:D2}",
                Income = rec?.Income ?? 0m,
                Expense = rec?.Expense ?? 0m,
            });
            cursor = cursor.AddMonths(1);
        }

        byKey.TryGetValue((year, month), out var cur);
        return new AccountStatisticsDto
        {
            Year = year,
            Month = month,
            MonthIncome = cur?.Income ?? 0m,
            MonthExpense = cur?.Expense ?? 0m,
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
