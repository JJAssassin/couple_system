namespace CoupleLoveSystem.Core.Dtos;

/// <summary>心情日历单日数据</summary>
public class MoodCalendarDto
{
    public int Year { get; set; }
    public List<MoodDayDto> Days { get; set; } = new();
}

public class MoodDayDto
{
    public string Date { get; set; } = string.Empty;      // "2026-08-22"
    public int? MoodScore { get; set; }                   // 1-10，null 表示无记录
    public string? MoodTag { get; set; }                  // 心情标签（如有）
}
