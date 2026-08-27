using CoupleLoveSystem.Core;
using Xunit;

namespace CoupleLoveSystem.Tests;

public class LunarHelperTests
{
    [Theory]
    [InlineData(2024, 2, 10, "农历甲辰年正月初一")]   // 春节（龙年）
    [InlineData(2025, 1, 29, "农历乙巳年正月初一")]   // 春节（蛇年）
    [InlineData(2026, 2, 17, "农历丙午年正月初一")]   // 春节（马年）
    public void ToLunarString_KnownAnchors(int y, int m, int d, string expected)
    {
        var solar = new DateTime(y, m, d, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(expected, LunarHelper.ToLunarString(solar));
    }

    [Fact]
    public void ToLunarString_LeapMonth_Reported()
    {
        // 2023-03-22 为农历癸卯年闰二月初一（2023 有闰二月）
        var solar = new DateTime(2023, 3, 22, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal("农历癸卯年闰二月初一", LunarHelper.ToLunarString(solar));
    }

    [Fact]
    public void ToLunarString_RoundTripMonotonicForKnownFestival()
    {
        // 连续两年春节应分别为不同干支年，且不抛异常
        var f1 = LunarHelper.ToLunarString(new DateTime(2024, 2, 10, 0, 0, 0, DateTimeKind.Utc));
        var f2 = LunarHelper.ToLunarString(new DateTime(2025, 1, 29, 0, 0, 0, DateTimeKind.Utc));
        Assert.NotEqual(f1, f2);
        Assert.Contains("正月初一", f1);
        Assert.Contains("正月初一", f2);
    }
}
