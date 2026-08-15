using System;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// 恋爱时长计算（LoveInfoCalculator.Compute）的测试。
/// 整日计数以本地日期差为准，小时/分钟为实时差值；未设置相恋日期时返回“未开始”。
/// </summary>
public class LoveInfoCalculatorTests
{
    [Fact]
    public void 无相恋日期_返回未开始且天数归零()
    {
        var r = LoveInfoCalculator.Compute(null, new DateTime(2026, 1, 1), new DateTime(2026, 1, 1, 12, 0, 0));
        Assert.False(r.HasLoveStart);
        Assert.Equal(0, r.TotalDays);
        Assert.Equal(0, r.TotalHours);
        Assert.Equal(0, r.TotalMinutes);
    }

    [Fact]
    public void 相恋10天前_整日计10_小时与分钟联动()
    {
        var start = new DateTime(2026, 1, 1);
        var today = new DateTime(2026, 1, 11);
        var now = new DateTime(2026, 1, 11, 3, 0, 0); // 比 start 晚 10天3小时
        var r = LoveInfoCalculator.Compute(start, today, now);
        Assert.True(r.HasLoveStart);
        Assert.Equal(10, r.TotalDays);
        Assert.Equal(10 * 24 + 3, r.TotalHours);          // 243
        Assert.Equal((10 * 24 + 3) * 60, r.TotalMinutes); // 14580
    }

    [Fact]
    public void 相恋日期在未来_整日不为负()
    {
        var start = new DateTime(2030, 1, 1);
        var today = new DateTime(2026, 1, 1);
        var r = LoveInfoCalculator.Compute(start, today, today);
        Assert.True(r.HasLoveStart);
        Assert.Equal(0, r.TotalDays);
    }
}
