using CoupleLoveSystem.Api;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

    /// <summary>把 CSV 文本解析为预览行（纯函数，不落库）。兼容本系统导出格式（表头 日期,类型,分类,金额,备注）与常见银行流水表头；自动跳过 BOM/空行，首行若像表头则跳过。</summary>
    public static List<AccountImportRow> ParseCsv(string csv)
    {
        var rows = new List<AccountImportRow>();
        if (string.IsNullOrWhiteSpace(csv)) return rows;
        csv = csv.TrimStart('\uFEFF');
        var lines = csv.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        List<string>? header = null;
        bool headerDecided = false;
        int idx = 0;
        foreach (var raw in lines)
        {
            idx++;
            var line = raw.Trim();
            if (line.Length == 0) continue;
            if (!headerDecided)
            {
                if (LooksLikeHeader(line))
                {
                    header = SplitCsvLine(line).Select(h => h.Trim().ToLowerInvariant()).ToList();
                    headerDecided = true;
                    continue;
                }
                headerDecided = true; // 首行非空即视为数据
            }
            var fields = SplitCsvLine(line);
            rows.Add(ParseLine(fields, header, idx));
        }
        return rows;
    }

    /// <summary>解析并落库（带去重）。与现有 CreateAsync 一致：金额必须 > 0、Category 非空（缺省「未分类」）。重复判定按 类型|分类|金额|日期|备注 签名，已存在的自动跳过，可安全重复导入同一份 CSV。</summary>
    public async Task<AccountImportResult> ImportAsync(string csv, long currentUserId, CancellationToken ct = default)
    {
        var rows = ParseCsv(csv);
        var result = new AccountImportResult { Total = rows.Count };
        var existing = await _db.AccountRecords.AsNoTracking().ToListAsync(ct);
        var seen = new HashSet<string>(existing.Select(Sig));
        var toAdd = new List<CoupleAccountRecord>();
        foreach (var r in rows)
        {
            if (!r.Valid)
            {
                result.Failed++;
                result.Errors.Add(new AccountImportError { LineNo = r.LineNo, Reason = r.Error ?? "格式错误" });
                continue;
            }
            var rec = new CoupleAccountRecord
            {
                RecordType = r.RecordType,
                Category = r.Category,
                Amount = r.Amount,
                RecordTime = r.RecordTime,
                Remark = r.Remark,
                CreateUserId = currentUserId,
                CreateTime = DateTime.UtcNow
            };
            var sig = Sig(rec);
            if (seen.Contains(sig)) { result.Skipped++; continue; }
            seen.Add(sig);
            toAdd.Add(rec);
        }
        foreach (var rec in toAdd)
        {
            await _repo.AddAsync(rec, ct);
            result.Imported++;
        }
        if (toAdd.Count > 0) await _repo.SaveChangesAsync(ct);
        return result;
    }

    private static string Sig(CoupleAccountRecord r) =>
        $"{(int)r.RecordType}|{r.Category.Trim()}|{r.Amount:F2}|{r.RecordTime:yyyy-MM-dd}|{(r.Remark ?? "").Trim()}";

    private static readonly string[] HeaderKeywords = new[]
    {
        "日期", "date", "time", "时间", "类型", "type", "收支",
        "分类", "category", "类别", "金额", "amount", "money", "交易金额",
        "备注", "remark", "note", "摘要", "desc"
    };
    private static bool LooksLikeHeader(string line)
    {
        var lower = line.ToLowerInvariant();
        int hits = 0;
        foreach (var k in HeaderKeywords) if (lower.Contains(k)) hits++;
        return hits >= 2;
    }

    private static int FindCol(List<string>? header, params string[] keys)
    {
        if (header == null) return -1;
        for (int i = 0; i < header.Count; i++)
            foreach (var k in keys)
                if (header[i].Contains(k)) return i;
        return -1;
    }

    private static AccountImportRow ParseLine(List<string> fields, List<string>? header, int lineNo)
    {
        int iDate = header == null ? 0 : FindCol(header, "日期", "date", "time", "时间");
        int iType = header == null ? 1 : FindCol(header, "类型", "type", "收支");
        int iCat = header == null ? 2 : FindCol(header, "分类", "category", "类别");
        int iAmt = header == null ? 3 : FindCol(header, "金额", "amount", "money", "交易金额");
        int iRemark = header == null ? 4 : FindCol(header, "备注", "remark", "note", "摘要", "desc");
        string Get(int i) => (i >= 0 && i < fields.Count) ? fields[i].Trim() : "";

        var row = new AccountImportRow { LineNo = lineNo };

        if (!TryParseDate(Get(iDate), out var dt))
        {
            row.Valid = false; row.Error = $"无法解析日期：{Get(iDate)}";
            return row;
        }
        row.RecordTime = dt;

        if (!TryParseType(Get(iType), out var rt))
        {
            row.Valid = false; row.Error = $"无法解析收支类型：{Get(iType)}";
            return row;
        }
        row.RecordType = rt;

        var cat = Get(iCat);
        row.Category = string.IsNullOrEmpty(cat) ? "未分类" : cat;

        if (!TryParseAmount(Get(iAmt), out var amt) || amt <= 0)
        {
            row.Valid = false; row.Error = $"金额无效或必须 > 0：{Get(iAmt)}";
            return row;
        }
        row.Amount = amt;

        var remark = Get(iRemark);
        row.Remark = string.IsNullOrEmpty(remark) ? null : remark;

        row.Valid = true;
        return row;
    }

    /// <summary>CSV 感知字段切分：支持双引号包裹与字段内逗号（"" 转义为 "）。</summary>
    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(line)) return result;
        var sb = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else sb.Append(c);
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == ',') { result.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
        }
        result.Add(sb.ToString());
        return result;
    }

    private static bool TryParseDate(string s, out DateTime dt)
    {
        dt = default;
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();
        string[] formats = { "yyyy-MM-dd", "yyyy/MM/dd", "yyyy-MM-dd HH:mm:ss", "yyyy/MM/dd HH:mm:ss", "yyyy年M月d日", "MM/dd/yyyy", "M/d/yyyy", "yyyyMMdd" };
        if (DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)) return true;
        return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
    }

    private static bool TryParseType(string s, out AccountRecordType rt)
    {
        rt = AccountRecordType.Income;
        if (string.IsNullOrWhiteSpace(s)) return false;
        var t = s.Trim().ToLowerInvariant();
        if (t.Contains("支出") || t.Contains("expend") || t.Contains("out") || t == "-" || t.Contains("支") || t.Contains("出"))
        { rt = AccountRecordType.Expend; return true; }
        if (t.Contains("收入") || t.Contains("income") || t.Contains("in") || t == "+" || t.Contains("收"))
        { rt = AccountRecordType.Income; return true; }
        if (int.TryParse(t, out var n))
        {
            if (n == (int)AccountRecordType.Income) { rt = AccountRecordType.Income; return true; }
            if (n == (int)AccountRecordType.Expend) { rt = AccountRecordType.Expend; return true; }
        }
        return false;
    }

    private static bool TryParseAmount(string s, out decimal amt)
    {
        amt = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;
        var cleaned = new string(s.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());
        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out amt);
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
