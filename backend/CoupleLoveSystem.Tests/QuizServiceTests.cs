using System.Threading.Tasks;
using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Repositories;
using CoupleLoveSystem.Tests.Infrastructure;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// QuizService 集成测试（InMemory EF + 空广播器）。
/// 覆盖题库校验、开局抽题、双方作答后揭晓比对、未揭晓不泄漏对方选项、默契率统计。
/// </summary>
public class QuizServiceTests
{
    private const long UserA = 1;
    private const long UserB = 2;

    private static QuizService Build(out CoupleDbContext db)
    {
        db = TestDb.CreateInMemoryContext();
        return new QuizService(
            new EfRepository<CoupleQuizQuestion>(db),
            new EfRepository<CoupleQuizRound>(db));
    }

    private static async Task<QuizQuestionDto> SeedQuestion(QuizService svc) =>
        await svc.CreateQuestionAsync(new QuizQuestionReq
        {
            Text = "宵夜选什么",
            Options = new() { "火锅", "烧烤", "泡面" },
            Category = "口味",
        }, UserA);

    [Fact]
    public async Task CreateQuestionAsync_选项不足两个_抛出Conflict()
    {
        var svc = Build(out _);
        await Assert.ThrowsAsync<ConflictException>(() => svc.CreateQuestionAsync(
            new QuizQuestionReq { Text = "只有一个选项", Options = new() { "A" } }, UserA));
    }

    [Fact]
    public async Task CreateQuestionAsync_题面为空_抛出Conflict()
    {
        var svc = Build(out _);
        await Assert.ThrowsAsync<ConflictException>(() => svc.CreateQuestionAsync(
            new QuizQuestionReq { Text = "   ", Options = new() { "A", "B" } }, UserA));
    }

    [Fact]
    public async Task CreateQuestionAsync_自建题非内置且可删除()
    {
        var svc = Build(out _);
        var q = await SeedQuestion(svc);

        Assert.False(q.IsBuiltin);
        Assert.Equal(3, q.Options.Count);
        Assert.Equal("火锅", q.Options[0]);

        await svc.DeleteQuestionAsync(q.Id, UserA);
        Assert.Empty(await svc.ListQuestionsAsync());
    }

    [Fact]
    public async Task DeleteQuestionAsync_内置题_禁止删除()
    {
        var svc = Build(out var db);
        db.QuizQuestions.Add(new CoupleQuizQuestion
        {
            Id = 99, Text = "内置题", OptionsJson = "[\"A\",\"B\"]", IsBuiltin = true
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() => svc.DeleteQuestionAsync(99, UserA));
    }

    [Fact]
    public async Task StartRoundAsync_题库为空_抛出Conflict()
    {
        var svc = Build(out _);
        await Assert.ThrowsAsync<ConflictException>(() => svc.StartRoundAsync(new QuizStartReq(), UserA));
    }

    [Fact]
    public async Task StartRoundAsync_快照题面与选项()
    {
        var svc = Build(out _);
        var q = await SeedQuestion(svc);

        var round = await svc.StartRoundAsync(new QuizStartReq { QuestionId = q.Id }, UserA);

        Assert.Equal(q.Id, round.QuestionId);
        Assert.Equal("宵夜选什么", round.QuestionText);
        Assert.Equal(3, round.Options.Count);
        Assert.False(round.IsRevealed);
        Assert.False(round.MyAnswered);
        Assert.False(round.MateAnswered);
    }

    [Fact]
    public async Task StartRoundAsync_已有未答完的局_禁止再开()
    {
        var svc = Build(out _);
        var q = await SeedQuestion(svc);
        await svc.StartRoundAsync(new QuizStartReq { QuestionId = q.Id }, UserA);

        await Assert.ThrowsAsync<ConflictException>(() =>
            svc.StartRoundAsync(new QuizStartReq { QuestionId = q.Id }, UserA));
    }

    [Fact]
    public async Task AnswerAsync_选项一致_揭晓且默契()
    {
        var svc = Build(out _);
        var q = await SeedQuestion(svc);
        var round = await svc.StartRoundAsync(new QuizStartReq { QuestionId = q.Id }, UserA);

        await svc.AnswerAsync(new QuizAnswerReq { RoundId = round.Id, Answer = 1 }, UserA);
        var after = await svc.AnswerAsync(new QuizAnswerReq { RoundId = round.Id, Answer = 1 }, UserB);

        Assert.True(after.IsRevealed);
        Assert.True(after.IsMatched);
        Assert.Equal(1, after.FirstAnswer);
        Assert.Equal(1, after.SecondAnswer);
        Assert.Equal(UserA, after.FirstUserId);
        Assert.Equal(UserB, after.SecondUserId);
    }

    [Fact]
    public async Task AnswerAsync_选项不同_揭晓但不默契()
    {
        var svc = Build(out _);
        var q = await SeedQuestion(svc);
        var round = await svc.StartRoundAsync(new QuizStartReq { QuestionId = q.Id }, UserA);

        await svc.AnswerAsync(new QuizAnswerReq { RoundId = round.Id, Answer = 0 }, UserA);
        var after = await svc.AnswerAsync(new QuizAnswerReq { RoundId = round.Id, Answer = 2 }, UserB);

        Assert.True(after.IsRevealed);
        Assert.False(after.IsMatched);
    }

    [Fact]
    public async Task AnswerAsync_未揭晓时不泄漏对方选项()
    {
        var svc = Build(out _);
        var q = await SeedQuestion(svc);
        var round = await svc.StartRoundAsync(new QuizStartReq { QuestionId = q.Id }, UserA);
        await svc.AnswerAsync(new QuizAnswerReq { RoundId = round.Id, Answer = 2 }, UserA);

        // B 视角：能看到「对方已作答」，但拿不到对方选的是哪个
        var fromB = await svc.GetRoundAsync(round.Id, UserB);
        Assert.True(fromB.MateAnswered);
        Assert.False(fromB.MyAnswered);
        Assert.Null(fromB.MyAnswer);
        Assert.Null(fromB.FirstAnswer);
        Assert.Null(fromB.SecondAnswer);

        // A 视角：自己的选项始终可见
        var fromA = await svc.GetRoundAsync(round.Id, UserA);
        Assert.True(fromA.MyAnswered);
        Assert.Equal(2, fromA.MyAnswer);
        Assert.Null(fromA.FirstAnswer); // 未揭晓，双方选项字段仍为空
    }

    [Fact]
    public async Task AnswerAsync_同一人重复作答_抛出Conflict()
    {
        var svc = Build(out _);
        var q = await SeedQuestion(svc);
        var round = await svc.StartRoundAsync(new QuizStartReq { QuestionId = q.Id }, UserA);
        await svc.AnswerAsync(new QuizAnswerReq { RoundId = round.Id, Answer = 0 }, UserA);

        await Assert.ThrowsAsync<ConflictException>(() =>
            svc.AnswerAsync(new QuizAnswerReq { RoundId = round.Id, Answer = 1 }, UserA));
    }

    [Fact]
    public async Task AnswerAsync_已揭晓的局_禁止再答()
    {
        var svc = Build(out _);
        var q = await SeedQuestion(svc);
        var round = await svc.StartRoundAsync(new QuizStartReq { QuestionId = q.Id }, UserA);
        await svc.AnswerAsync(new QuizAnswerReq { RoundId = round.Id, Answer = 0 }, UserA);
        await svc.AnswerAsync(new QuizAnswerReq { RoundId = round.Id, Answer = 0 }, UserB);

        await Assert.ThrowsAsync<ConflictException>(() =>
            svc.AnswerAsync(new QuizAnswerReq { RoundId = round.Id, Answer = 1 }, 3));
    }

    [Fact]
    public async Task AnswerAsync_选项越界_抛出Conflict()
    {
        var svc = Build(out _);
        var q = await SeedQuestion(svc);
        var round = await svc.StartRoundAsync(new QuizStartReq { QuestionId = q.Id }, UserA);

        await Assert.ThrowsAsync<ConflictException>(() =>
            svc.AnswerAsync(new QuizAnswerReq { RoundId = round.Id, Answer = 5 }, UserA));
        await Assert.ThrowsAsync<ConflictException>(() =>
            svc.AnswerAsync(new QuizAnswerReq { RoundId = round.Id, Answer = -1 }, UserA));
    }

    [Fact]
    public async Task GetRoundAsync_不存在_抛出NotFound()
    {
        var svc = Build(out _);
        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetRoundAsync(999, UserA));
    }

    [Fact]
    public async Task GetStatsAsync_默契率只按已揭晓局计算()
    {
        var svc = Build(out _);
        var q = await SeedQuestion(svc);

        // 第 1 局：默契
        var r1 = await svc.StartRoundAsync(new QuizStartReq { QuestionId = q.Id }, UserA);
        await svc.AnswerAsync(new QuizAnswerReq { RoundId = r1.Id, Answer = 0 }, UserA);
        await svc.AnswerAsync(new QuizAnswerReq { RoundId = r1.Id, Answer = 0 }, UserB);

        // 第 2 局：不默契
        var r2 = await svc.StartRoundAsync(new QuizStartReq { QuestionId = q.Id }, UserA);
        await svc.AnswerAsync(new QuizAnswerReq { RoundId = r2.Id, Answer = 0 }, UserA);
        await svc.AnswerAsync(new QuizAnswerReq { RoundId = r2.Id, Answer = 1 }, UserB);

        // 第 3 局：只有一人作答，不计入分母
        var r3 = await svc.StartRoundAsync(new QuizStartReq { QuestionId = q.Id }, UserA);
        await svc.AnswerAsync(new QuizAnswerReq { RoundId = r3.Id, Answer = 0 }, UserA);

        var stats = await svc.GetStatsAsync(UserA);

        Assert.Equal(3, stats.TotalRounds);
        Assert.Equal(2, stats.RevealedRounds);
        Assert.Equal(1, stats.MatchedRounds);
        Assert.Equal(1, stats.PendingRounds);
        Assert.Equal(50, stats.MatchRate);
    }

    [Fact]
    public async Task GetStatsAsync_无已揭晓局_默契率为零不除零()
    {
        var svc = Build(out _);
        var stats = await svc.GetStatsAsync(UserA);

        Assert.Equal(0, stats.TotalRounds);
        Assert.Equal(0, stats.MatchRate);
    }

    [Fact]
    public async Task ListRoundsAsync_最新局排在最前()
    {
        var svc = Build(out _);
        var q = await SeedQuestion(svc);

        var r1 = await svc.StartRoundAsync(new QuizStartReq { QuestionId = q.Id }, UserA);
        await svc.AnswerAsync(new QuizAnswerReq { RoundId = r1.Id, Answer = 0 }, UserA);
        await svc.AnswerAsync(new QuizAnswerReq { RoundId = r1.Id, Answer = 0 }, UserB);
        var r2 = await svc.StartRoundAsync(new QuizStartReq { QuestionId = q.Id }, UserA);

        var list = await svc.ListRoundsAsync(1, 50, UserA);

        Assert.Equal(2, list.Total);
        Assert.Equal(r2.Id, list.Items[0].Id);
    }
}
