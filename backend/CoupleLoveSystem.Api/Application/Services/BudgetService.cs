using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Enums;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CoupleLoveSystem.Application.Services;

/// <summary>预算服务：情侣按月设定总预算 / 分类预算，并生成当月收支与超支总览。</summary>
public class BudgetService
{
    private readonly IRepository<CoupleBudget> _repo;
    private readonly CoupleDbContext _db;

    public BudgetService(IRepository<CoupleBudget> repo, CoupleDbContext db)
    {
        _repo = repo; _db = db;
    }

    /// <summary>设置/更新某年某月某分类的预算（分类为空表示当月总预算）。已存在则覆盖金额。</summary>
    public async Task<BudgetDto> SetAsync(BudgetSetReq req, long currentUserId, CancellationToken ct = default)
    {
        if (req.Year < 2000 || req.Year > 2100) throw new ConflictException("年份不合法");
        if (req.Month < 1 || req.Month > 12) throw new ConflictException("月份需为 1-12");
        if (req.LimitAmount <= 0) throw new ConflictException("预算金额必须大于 0");
        var cat = string.IsNullOrWhiteSpace(req.Category) ? null : req.Category.Trim();

        var existing = await _db.Budgets
            .FirstOrDefaultAsync(b => b.Year == req.Year && b.Month == req.Month
                && ((b.Category == null && cat == null) || (b.Category != null && b.Category == cat)), ct);

        if (existing is null)
        {
            existing = new CoupleBudget
            {
                Year = req.Year,
                Month = req.Month,
                Category = cat,
                LimitAmount = req.LimitAmount,
                CreateUserId = currentUserId,
                CreateTime = DateTime.UtcNow,
            };
            await _repo.AddAsync(existing, ct);
        }
        else
        {
            existing.LimitAmount = req.LimitAmount;
            existing.UpdateUserId = currentUserId;
            existing.UpdateTime = DateTime.UtcNow;
            _repo.Update(existing);
        }

        await _repo.SaveChangesAsync(ct);
        return Map(existing);
    }

    public async Task DeleteAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var b = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("预算不存在");
        _repo.SoftDelete(b);
        await _repo.SaveChangesAsync(ct);
    }

    /// <summary>列出某年某月已设置的预算（总预算排在最前）。</summary>
    public async Task<List<BudgetDto>> ListAsync(int year, int month, CancellationToken ct = default)
    {
        var list = await _db.Budgets.AsNoTracking()
            .Where(b => b.Year == year && b.Month == month)
            .OrderBy(b => b.Category == null ? 0 : 1).ThenBy(b => b.Category)
            .ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    /// <summary>生成某月预算总览：收支汇总、当月总预算、剩余、是否超支，以及分类明细。</summary>
    public async Task<MonthlyBudgetDto> GetMonthlyAsync(int year, int month, CancellationToken ct = default)
    {
        var records = await _db.AccountRecords.AsNoTracking()
            .Where(a => a.RecordTime.Year == year && a.RecordTime.Month == month)
            .ToListAsync(ct);

        var income = records.Where(a => a.RecordType == AccountRecordType.Income).Sum(a => a.Amount);
        var expense = records.Where(a => a.RecordType == AccountRecordType.Expend).Sum(a => a.Amount);

        var budgets = await _db.Budgets.AsNoTracking()
            .Where(b => b.Year == year && b.Month == month)
            .ToListAsync(ct);

        var totalBudget = budgets.FirstOrDefault(b => b.Category == null)?.LimitAmount;

        var byCategory = records
            .Where(a => a.RecordType == AccountRecordType.Expend)
            .GroupBy(a => a.Category)
            .Select(g =>
            {
                var catBudget = budgets.FirstOrDefault(b => b.Category != null && b.Category == g.Key)?.LimitAmount;
                var amount = g.Sum(a => a.Amount);
                return new MonthlyCategoryStat
                {
                    Category = g.Key,
                    Amount = amount,
                    Budget = catBudget,
                    IsOverspent = catBudget.HasValue && amount > catBudget.Value,
                };
            })
            .OrderBy(x => x.Category)
            .ToList();

        return new MonthlyBudgetDto
        {
            Year = year,
            Month = month,
            Income = income,
            Expense = expense,
            TotalBudget = totalBudget,
            Remaining = totalBudget.HasValue ? totalBudget.Value - expense : 0,
            IsOverspent = totalBudget.HasValue && expense > totalBudget.Value,
            Categories = byCategory,
        };
    }

    public Task<MonthlyBudgetDto> GetCurrentAsync(CancellationToken ct = default)
    {
        var now = DateTime.Now;
        return GetMonthlyAsync(now.Year, now.Month, ct);
    }

    public static BudgetDto Map(CoupleBudget b) => new()
    {
        Id = b.Id,
        Year = b.Year,
        Month = b.Month,
        Category = b.Category,
        LimitAmount = b.LimitAmount,
    };
}
