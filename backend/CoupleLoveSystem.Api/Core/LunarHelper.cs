using System.Globalization;

namespace CoupleLoveSystem.Core;

/// <summary>公历 → 农历 转换工具。基于 .NET 内置 <see cref="ChineseLunisolarCalendar"/>（权威农历数据，无需手写对照表）。
/// 用于纪念日 / 相恋日等场景展示「农历 X年X月X」。</summary>
public static class LunarHelper
{
    private static readonly ChineseLunisolarCalendar Cal = new();

    private static readonly string[] Stems = { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };
    private static readonly string[] Branches = { "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥" };
    private static readonly string[] MonthNames =
        { "正月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月", "十月", "冬月", "腊月" };
    private static readonly string[] DayNames =
        { "初一", "初二", "初三", "初四", "初五", "初六", "初七", "初八", "初九", "初十",
          "十一", "十二", "十三", "十四", "十五", "十六", "十七", "十八", "十九", "二十",
          "廿一", "廿二", "廿三", "廿四", "廿五", "廿六", "廿七", "廿八", "廿九", "三十" };

    /// <summary>将公历日期转换为形如「农历甲辰年五月初五」的字符串；转换失败（超范围等）返回空串。</summary>
    public static string ToLunarString(DateTime solar)
    {
        try
        {
            var year = Cal.GetYear(solar);
            var month = Cal.GetMonth(solar);          // 含闰月时范围 1..13，闰月会多占一个序号
            var day = Cal.GetDayOfMonth(solar);
            var leapMonth = Cal.GetLeapMonth(year);    // 0 表示当年无闰月；否则为闰月的序号
            var isLeap = leapMonth != 0 && month == leapMonth;
            var realMonth = isLeap ? month - 1 : month;

            var sexagenary = Cal.GetSexagenaryYear(solar);
            var yearName = Stems[Cal.GetCelestialStem(sexagenary) - 1] + Branches[Cal.GetTerrestrialBranch(sexagenary) - 1] + "年";
            var monthName = (isLeap ? "闰" : string.Empty) + MonthNames[realMonth - 1];
            var dayName = DayNames[day - 1];

            return $"农历{yearName}{monthName}{dayName}";
        }
        catch
        {
            return string.Empty;
        }
    }
}
