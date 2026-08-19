namespace CoupleLoveSystem.Core.Dtos;

// ---------- 年度恋爱报告（/api/stats/yearreport） ----------

/// <summary>年度报告：把这一年两个人共同留下的痕迹聚合成一份可读的数字回忆。</summary>
public class YearReportDto
{
    public int Year { get; set; }

    // ---- 感情基调 ----
    /// <summary>相恋天数（自 LoveStartTime 至今天）</summary>
    public int LoveDays { get; set; }
    /// <summary>纪念日总数（历史全部）</summary>
    public int AnniversaryTotal { get; set; }
    /// <summary>本年度内落日的纪念日</summary>
    public List<AnniversaryPassedDto> Anniversaries { get; set; } = new();

    // ---- 内容产出（本年度新增） ----
    public int DiaryCount { get; set; }
    public double AvgMood { get; set; }            // 平均心情分 1-10
    public int WishCreated { get; set; }
    public int WishDone { get; set; }
    public int TodoDone { get; set; }
    public int ConflictCount { get; set; }
    public int ConflictResolved { get; set; }      // ReconcileTime != null
    public int LetterCount { get; set; }
    public int BoardCount { get; set; }
    public int ImageCount { get; set; }
    public int FootprintCount { get; set; }
    public int DateCount { get; set; }
    public int DateCompleted { get; set; }

    // ---- 默契（默契问答） ----
    public int QuizRounds { get; set; }
    public int QuizRevealed { get; set; }
    public int QuizMatched { get; set; }
    public double MatchRate { get; set; }          // 0-100

    // ---- 记账（按记账日期 RecordTime） ----
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public List<MonthlyFinanceDto> MonthlyFinance { get; set; } = new();  // 固定 12 个月
    public List<CategorySpendDto> TopSpend { get; set; } = new();         // 支出分类 top5

    // ---- 趋势（月度） ----
    public List<ChartPointDto> MoodTrend { get; set; } = new();           // 1-12 月平均心情
    public List<ChartPointDto> ConflictTrend { get; set; } = new();       // 1-12 月矛盾数
}

public class AnniversaryPassedDto
{
    public string Name { get; set; } = string.Empty;
    public DateTime TargetDate { get; set; }
}

public class MonthlyFinanceDto
{
    public string Month { get; set; } = string.Empty;  // "2026-01"
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
}

public class CategorySpendDto
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
