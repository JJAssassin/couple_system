using System.ComponentModel.DataAnnotations;
using CoupleLoveSystem.Core.Dtos;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>审计 P2-6：请求 DTO 数据注解校验回归测试。验证 [Required]/[StringLength]/[Range]/[MinLength]/[MaxLength]
/// 能正确拦截缺失必填项、超长字符串、越界数值与非法集合，且合法输入可正常通过。</summary>
public class DtoValidationTests
{
    private static IList<ValidationResult> Validate(object obj)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(obj, new ValidationContext(obj), results, true);
        return results;
    }

    private static bool IsValid(object obj) => Validate(obj).Count == 0;

    [Fact]
    public void 日记_缺标题或内容_不通过()
    {
        Assert.False(IsValid(new DiaryReq { Title = "", Content = "正文" }));
        Assert.False(IsValid(new DiaryReq { Title = "标题", Content = "" }));
    }

    [Fact]
    public void 日记_内容超长_不通过()
    {
        Assert.False(IsValid(new DiaryReq { Title = "t", Content = new string('x', 50001) }));
        Assert.True(IsValid(new DiaryReq { Title = "t", Content = new string('x', 50000) }));
    }

    [Fact]
    public void 日记_心情评分越界_不通过()
    {
        Assert.False(IsValid(new DiaryReq { Title = "t", Content = "c", MoodScore = 0 }));
        Assert.False(IsValid(new DiaryReq { Title = "t", Content = "c", MoodScore = 11 }));
        Assert.True(IsValid(new DiaryReq { Title = "t", Content = "c", MoodScore = 10 }));
    }

    [Fact]
    public void 登录_用户名或密码为空_不通过()
    {
        Assert.False(IsValid(new LoginReq { UserName = "", Password = "p" }));
        Assert.False(IsValid(new LoginReq { UserName = "u", Password = "" }));
    }

    [Fact]
    public void 纪念日_名称必填且提醒天数有界()
    {
        Assert.False(IsValid(new AnniversaryReq { Name = "", TargetDate = DateTime.UtcNow }));
        Assert.False(IsValid(new AnniversaryReq { Name = "n", TargetDate = DateTime.UtcNow, RemindDays = 999 }));
        Assert.True(IsValid(new AnniversaryReq { Name = "n", TargetDate = DateTime.UtcNow, RemindDays = 7 }));
    }

    [Fact]
    public void 愿望_优先级越界_不通过()
    {
        Assert.False(IsValid(new WishReq { Title = "t", Priority = 0 }));
        Assert.False(IsValid(new WishReq { Title = "t", Priority = 6 }));
        Assert.True(IsValid(new WishReq { Title = "t", Priority = 3 }));
    }

    [Fact]
    public void 待办_优先级有界且标题必填()
    {
        Assert.False(IsValid(new TodoReq { Title = "", Priority = 2 }));
        Assert.False(IsValid(new TodoReq { Title = "t", Priority = 4 }));
        Assert.True(IsValid(new TodoReq { Title = "t", Priority = 1 }));
    }

    [Fact]
    public void 留言板_内容必填且超长不通过()
    {
        Assert.False(IsValid(new BoardMessageReq { Content = "" }));
        Assert.False(IsValid(new BoardMessageReq { Content = new string('x', 10001) }));
        Assert.True(IsValid(new BoardMessageReq { Content = "爱你" }));
    }

    [Fact]
    public void 默契题_选项至少两个且不超二十()
    {
        Assert.False(IsValid(new QuizQuestionReq { Text = "q", Options = new List<string> { "a" } }));
        Assert.False(IsValid(new QuizQuestionReq
        {
            Text = "q",
            Options = Enumerable.Range(0, 21).Select(i => i.ToString()).ToList()
        }));
        Assert.True(IsValid(new QuizQuestionReq { Text = "q", Options = new List<string> { "a", "b" } }));
    }

    [Fact]
    public void 相册_名称必填()
    {
        Assert.False(IsValid(new AlbumReq { AlbumName = "" }));
        Assert.True(IsValid(new AlbumReq { AlbumName = "旅行" }));
    }

    [Fact]
    public void 矛盾_摘要必填()
    {
        Assert.False(IsValid(new ConflictReq { OccurTime = DateTime.UtcNow, Summary = "" }));
        Assert.True(IsValid(new ConflictReq { OccurTime = DateTime.UtcNow, Summary = "吵架" }));
    }

    [Fact]
    public void 记账_分类必填()
    {
        Assert.False(IsValid(new AccountRecordReq { RecordType = AccountRecordType.Expend, Category = "" }));
        Assert.True(IsValid(new AccountRecordReq { RecordType = AccountRecordType.Expend, Category = "餐饮" }));
    }

    [Fact]
    public void 预算_年月有界()
    {
        Assert.False(IsValid(new BudgetSetReq { Year = 1999, Month = 6 }));
        Assert.False(IsValid(new BudgetSetReq { Year = 2026, Month = 13 }));
        Assert.True(IsValid(new BudgetSetReq { Year = 2026, Month = 8 }));
    }

    [Fact]
    public void 约会_体验评分越界_不通过()
    {
        Assert.False(IsValid(new DateRecordReq { ExperienceScore = 0 }));
        Assert.False(IsValid(new DateRecordReq { ExperienceScore = 6 }));
        Assert.True(IsValid(new DateRecordReq { ExperienceScore = 5 }));
    }

    [Fact]
    public void 足迹_标题与图标必填()
    {
        Assert.False(IsValid(new FootprintReq { Title = "", Emoji = "✨" }));
        Assert.False(IsValid(new FootprintReq { Title = "抱抱", Emoji = "" }));
        Assert.True(IsValid(new FootprintReq { Title = "抱抱", Emoji = "🤗" }));
    }

    [Fact]
    public void 刷新令牌_不能为空()
    {
        Assert.False(IsValid(new RefreshReq { RefreshToken = "" }));
        Assert.True(IsValid(new RefreshReq { RefreshToken = "eyJhbGciOi" }));
    }
}
