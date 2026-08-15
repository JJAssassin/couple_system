using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// 纪念日「下一次发生日期」计算（CoupleAnniversary.ComputeNextOccurrence）的边界测试。
/// 该方法是 Home / 纪念日列表 / 时间轴的单一事实来源，闰年 2/29、跨年、一次性过期等边界必须正确。
/// </summary>
public class AnniversaryDateTests
{
    private static CoupleAnniversary A(DateTime target, bool yearly) => new() { TargetDate = target, IsYearly = yearly };

    [Fact]
    public void 一次性_未来目标_返回目标日()
    {
        var a = A(new DateTime(2030, 5, 20), false);
        Assert.Equal(new DateTime(2030, 5, 20), a.ComputeNextOccurrence());
    }

    [Fact]
    public void 一次性_过去目标_返回null_不再提醒()
    {
        var a = A(new DateTime(2000, 1, 1), false);
        Assert.Null(a.ComputeNextOccurrence());
    }

    [Fact]
    public void 每年_目标在今天_返回今天()
    {
        var now = DateTime.UtcNow.Date;
        var a = A(new DateTime(2010, now.Month, now.Day), true);
        Assert.Equal(now, a.ComputeNextOccurrence());
    }

    [Fact]
    public void 每年_已过的日期_滚动到下一年且不小于今天()
    {
        var a = A(new DateTime(2010, 8, 14), true); // 固定月日，跨年滚动
        var occ = a.ComputeNextOccurrence();
        Assert.NotNull(occ);
        Assert.Equal(8, occ.Value.Month);
        Assert.Equal(14, occ.Value.Day);
        Assert.True(occ.Value >= DateTime.UtcNow.Date);
    }

    [Fact]
    public void 每年_闰年2月29_非闰年不抛异常且回退到2月28()
    {
        // 目标 2/29，当前若非闰年，原实现 new DateTime(年,2,29) 会抛 ArgumentOutOfRangeException
        var a = A(new DateTime(2024, 2, 29), true);
        var occ = a.ComputeNextOccurrence();
        Assert.NotNull(occ);
        Assert.True(occ.Value >= DateTime.UtcNow.Date);
        if (!DateTime.IsLeapYear(occ.Value.Year))
            Assert.Equal(28, occ.Value.Day);   // 非闰年回退到 2/28
        else
            Assert.Equal(29, occ.Value.Day);   // 闰年则为 2/29
    }
}
