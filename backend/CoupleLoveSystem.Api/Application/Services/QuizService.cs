using System.Text.Json;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CoupleLoveSystem.Application.Services;

/// <summary>
/// 默契问答：一方发起一局（抽题），两人各自独立选一个选项；都答完后揭晓，选项一致即「默契」。
/// 关键设计：未揭晓前接口不下发任何一方的选项（只回自己的），避免抓包偷看对方答案。
/// 题面/选项在开局时做快照，题库后续改动不影响历史战绩。
/// </summary>
public class QuizService
{
    private readonly IRepository<CoupleQuizQuestion> _qRepo;
    private readonly IRepository<CoupleQuizRound> _rRepo;

    public QuizService(IRepository<CoupleQuizQuestion> qRepo, IRepository<CoupleQuizRound> rRepo)
    {
        _qRepo = qRepo; _rRepo = rRepo;
    }

    #region 题库

    public async Task<List<QuizQuestionDto>> ListQuestionsAsync(CancellationToken ct = default)
    {
        var list = await _qRepo.Query()
            .OrderBy(q => q.SortOrder).ThenBy(q => q.Id)
            .ToListAsync(ct);
        return list.Select(MapQuestion).ToList();
    }

    public async Task<QuizQuestionDto> CreateQuestionAsync(QuizQuestionReq req, long currentUserId, CancellationToken ct = default)
    {
        var text = (req.Text ?? string.Empty).Trim();
        if (text.Length == 0) throw new ConflictException("请填写题目");

        var options = (req.Options ?? new List<string>())
            .Select(o => (o ?? string.Empty).Trim())
            .Where(o => o.Length > 0)
            .ToList();
        if (options.Count < 2) throw new ConflictException("至少需要 2 个选项");
        if (options.Count > 6) throw new ConflictException("最多 6 个选项");

        var maxSort = await _qRepo.Query().Select(q => (int?)q.SortOrder).MaxAsync(ct) ?? 0;
        var q = new CoupleQuizQuestion
        {
            Text = text,
            OptionsJson = JsonSerializer.Serialize(options),
            Category = string.IsNullOrWhiteSpace(req.Category) ? null : req.Category!.Trim(),
            SortOrder = maxSort + 1,
            IsBuiltin = false,
            CreateUserId = currentUserId,
            CreateTime = DateTime.UtcNow,
        };
        await _qRepo.AddAsync(q, ct);
        await _qRepo.SaveChangesAsync(ct);
        return MapQuestion(q);
    }

    public async Task DeleteQuestionAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var q = await _qRepo.GetByIdAsync(id, ct) ?? throw new NotFoundException("题目不存在");
        if (q.IsBuiltin) throw new ConflictException("内置题目不可删除");
        _qRepo.SoftDelete(q);
        await _qRepo.SaveChangesAsync(ct);
    }

    #endregion

    #region 对局

    /// <summary>发起一局。指定 QuestionId 则用该题；否则从题库随机抽一题，优先抽还没玩过的。</summary>
    public async Task<QuizRoundDto> StartRoundAsync(QuizStartReq req, long currentUserId, CancellationToken ct = default)
    {
        // 已有未答完的一局时不允许再开，避免堆积一堆半场
        var pending = await _rRepo.Query().AnyAsync(r => !r.IsRevealed, ct);
        if (pending) throw new ConflictException("还有一局没答完，先把它答完吧");

        CoupleQuizQuestion? question;
        if (req.QuestionId is > 0)
        {
            question = await _qRepo.GetByIdAsync(req.QuestionId!.Value, ct)
                ?? throw new NotFoundException("题目不存在");
        }
        else
        {
            var all = await _qRepo.Query().ToListAsync(ct);
            if (all.Count == 0) throw new ConflictException("题库还是空的，先添加一道题吧");

            var playedIds = await _rRepo.Query().Select(r => r.QuestionId).Distinct().ToListAsync(ct);
            var pool = all.Where(q => !playedIds.Contains(q.Id)).ToList();
            if (pool.Count == 0) pool = all; // 全玩过了就允许重玩
            question = pool[Random.Shared.Next(pool.Count)];
        }

        var round = new CoupleQuizRound
        {
            QuestionId = question.Id,
            QuestionText = question.Text,
            OptionsJson = question.OptionsJson,
            Category = question.Category,
            CreateUserId = currentUserId,
            CreateTime = DateTime.UtcNow,
        };
        await _rRepo.AddAsync(round, ct);
        await _rRepo.SaveChangesAsync(ct);
        return MapRound(round, currentUserId);
    }

    /// <summary>作答。第一个作答的人占 First 位，第二个占 Second 位并立即揭晓比对。</summary>
    public async Task<QuizRoundDto> AnswerAsync(QuizAnswerReq req, long currentUserId, CancellationToken ct = default)
    {
        var round = await _rRepo.GetByIdAsync(req.RoundId, ct) ?? throw new NotFoundException("对局不存在");
        if (round.IsRevealed) throw new ConflictException("本局已揭晓");
        if (round.FirstUserId == currentUserId || round.SecondUserId == currentUserId)
            throw new ConflictException("你已经作答了，等对方就好");

        var options = ParseOptions(round.OptionsJson);
        if (req.Answer < 0 || req.Answer >= options.Count) throw new ConflictException("选项不存在");

        var now = DateTime.UtcNow;
        if (round.FirstUserId == null)
        {
            round.FirstUserId = currentUserId;
            round.FirstAnswer = req.Answer;
            round.FirstAnsweredTime = now;
        }
        else
        {
            round.SecondUserId = currentUserId;
            round.SecondAnswer = req.Answer;
            round.SecondAnsweredTime = now;
            round.IsRevealed = true;
            round.IsMatched = round.FirstAnswer == req.Answer;
        }
        round.UpdateUserId = currentUserId;
        _rRepo.Update(round);
        await _rRepo.SaveChangesAsync(ct);
        return MapRound(round, currentUserId);
    }

    public async Task<PagedResult<QuizRoundDto>> ListRoundsAsync(int page, int pageSize, long currentUserId, CancellationToken ct = default)
    {
        var all = await _rRepo.Query()
            .OrderByDescending(r => r.Id)
            .ToListAsync(ct);

        var total = all.Count;
        var items = all.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => MapRound(r, currentUserId))
            .ToList();
        return new PagedResult<QuizRoundDto> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<QuizRoundDto> GetRoundAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var r = await _rRepo.GetByIdAsync(id, ct) ?? throw new NotFoundException("对局不存在");
        return MapRound(r, currentUserId);
    }

    public async Task DeleteRoundAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var r = await _rRepo.GetByIdAsync(id, ct) ?? throw new NotFoundException("对局不存在");
        _rRepo.SoftDelete(r);
        await _rRepo.SaveChangesAsync(ct);
    }

    /// <summary>默契统计：默契率按已揭晓局计算，没揭晓的不算分母。</summary>
    public async Task<QuizStatsDto> GetStatsAsync(long currentUserId, CancellationToken ct = default)
    {
        var all = await _rRepo.Query().Select(r => new { r.IsRevealed, r.IsMatched }).ToListAsync(ct);
        var revealed = all.Count(r => r.IsRevealed);
        var matched = all.Count(r => r.IsRevealed && r.IsMatched);
        return new QuizStatsDto
        {
            TotalRounds = all.Count,
            RevealedRounds = revealed,
            MatchedRounds = matched,
            PendingRounds = all.Count - revealed,
            MatchRate = revealed == 0 ? 0 : (int)Math.Round(matched * 100.0 / revealed),
        };
    }

    #endregion

    private static List<string> ParseOptions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>(); }
        catch (JsonException) { return new List<string>(); }
    }

    private static QuizQuestionDto MapQuestion(CoupleQuizQuestion q) => new()
    {
        Id = q.Id,
        Text = q.Text,
        Options = ParseOptions(q.OptionsJson),
        Category = q.Category,
        IsBuiltin = q.IsBuiltin,
    };

    private static QuizRoundDto MapRound(CoupleQuizRound r, long currentUserId)
    {
        var iAmFirst = r.FirstUserId == currentUserId;
        var iAmSecond = r.SecondUserId == currentUserId;

        var dto = new QuizRoundDto
        {
            Id = r.Id,
            QuestionId = r.QuestionId,
            QuestionText = r.QuestionText,
            Options = ParseOptions(r.OptionsJson),
            Category = r.Category,
            FirstUserId = r.FirstUserId,
            FirstAnsweredTime = r.FirstAnsweredTime,
            SecondUserId = r.SecondUserId,
            SecondAnsweredTime = r.SecondAnsweredTime,
            IsRevealed = r.IsRevealed,
            IsMatched = r.IsMatched,
            MyAnswered = iAmFirst || iAmSecond,
            MyAnswer = iAmFirst ? r.FirstAnswer : iAmSecond ? r.SecondAnswer : null,
            MateAnswered = (r.FirstUserId != null && !iAmFirst) || (r.SecondUserId != null && !iAmSecond),
            CreateUserId = r.CreateUserId,
            CreateTime = r.CreateTime,
        };

        // 揭晓后才下发双方选项；未揭晓时置空，防止提前偷看对方答案
        if (r.IsRevealed)
        {
            dto.FirstAnswer = r.FirstAnswer;
            dto.SecondAnswer = r.SecondAnswer;
        }
        return dto;
    }
}
