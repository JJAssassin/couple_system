using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Enums;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace CoupleLoveSystem.Infrastructure.Persistence;

/// <summary>
/// 幂等数据种子（不再负责建表/改表——schema 由 EF Migration 管理，见 Program.cs 的 MigrateAsync）。
/// 仅做数据层的开发期初始化：温情语录、演示纪念日、预绑定 demo 双账号、把历史空 CoupleId 内容回填到 demo 空间、
/// 以及情侣级共享设置行。所有写入均按「不存在才写」的幂等原则，重复启动不会重复插入。
/// </summary>
public class DbSeeder
{
    private readonly CoupleDbContext _db;
    public DbSeeder(CoupleDbContext db) => _db = db;

    public async Task SeedAsync()
    {
        // 种子每日一句温情语录（仅当语录表为空时写入）
        await SeedQuotesAsync();
        // 预绑定两个 demo 账号（依赖账号已存在；首次启动账号尚未创建时此处为 no-op，账号创建后再绑定）
        await SeedBindAsync();
        // 把已有内容数据回填到 demo 情侣空间（CoupleId 为空的历史/外部灌入数据统一归属该空间，避免被全局过滤器隐藏）
        await BackfillCoupleIdAsync();
        // 演示用纪念日（仅当表为空时写入）
        await SeedAnniversariesAsync();

        // 确保情侣级共享设置行存在（整库单对情侣，Key=global）；LoveStartTime 初始为 null（未设置，不显示虚假天数）
        if (!await _db.Settings.AnyAsync(s => s.Key == "global"))
        {
            _db.Settings.Add(new CoupleSetting { Key = "global", CreateUserId = 1, CreateTime = DateTime.UtcNow });
            await _db.SaveChangesAsync();
        }

        // 已存在任意账号则跳过创建（demo 双账号已就绪）
        if (await _db.Users.AnyAsync()) return;

        var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var hash = BCrypt.Net.BCrypt.HashPassword("123456");
        _db.Users.Add(new CoupleUser
        {
            UserName = "partner_a", NickName = "TA", PasswordHash = hash,
            LoveStartTime = start, RoleType = RoleType.PartnerA,
            CreateUserId = 1, CreateTime = DateTime.UtcNow
        });
        _db.Users.Add(new CoupleUser
        {
            UserName = "partner_b", NickName = "我", PasswordHash = hash,
            LoveStartTime = start, RoleType = RoleType.PartnerB,
            CreateUserId = 2, CreateTime = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    /// <summary>种子每日一句温情语录（仅当语录表为空时写入；已存在则跳过，不重复插入）。</summary>
    private async Task SeedQuotesAsync()
    {
        if (await _db.Quotes.AnyAsync()) return;

        var quotes = new[]
        {
            "喜欢你，是我做过最对的决定。",
            "世界很大，但我只想和你过小日子。",
            "和你在一起的每一天，都值得被收藏。",
            "所谓幸福，就是早上醒来身边有你。",
            "你是我平淡生活里的那束光。",
            "无论晴天雨天，只要有你在就是好天气。",
            "我想和你一起，把平凡过成浪漫。",
            "你的笑容，是我每天想见的第一件事。",
            "慢慢来，我们有一辈子可以相爱。",
            "遇见你之后，我才懂什么叫心动。",
            "有你的地方，就是家。",
            "谢谢你，出现在我的生命里。",
            "今天也要好好吃饭，好好想我。",
            "你是我的软肋，也是我的铠甲。",
            "愿我们吵不散，骂不走，爱很久。",
            "想把所有的温柔，都留给你。",
            "和你说话，时间总是过得特别快。",
            "你不用完美，你只要是你就好。",
            "未来的每一步，都想有你参与。",
            "再忙也要记得，有人很爱你。",
            "今天也要给彼此一个拥抱呀 🤗",
            "我们的故事，未完待续。",
            "喜欢是乍见之欢，爱是久处仍怦然。",
            "你是我所有好心情的来源。",
            "哪怕什么都不做，和你待着也很开心。",
            "愿岁月静好，你我如初。",
            "被你爱着，是我最大的幸运。",
            "今天的你也很好看，记得开心。",
            "在一起的第 N 天，依旧心动。",
            "要把日子过得闪闪发光，因为我们在一起。",
        };

        for (var i = 0; i < quotes.Length; i++)
        {
            _db.Quotes.Add(new CoupleQuote
            {
                Content = quotes[i],
                SortOrder = i,
                CreateUserId = 1,
                CreateTime = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();
    }

    /// <summary>预绑定两个 demo 账号，使产品开箱即展示「已与 TA 绑定」。账号不存在时跳过（首启后由下方创建逻辑补齐并再次绑定）。已绑定则跳过。</summary>
    private async Task SeedBindAsync()
    {
        var a = await _db.Users.FirstOrDefaultAsync(u => u.UserName == "partner_a");
        var b = await _db.Users.FirstOrDefaultAsync(u => u.UserName == "partner_b");
        if (a == null || b == null) return;
        if (!string.IsNullOrEmpty(a.CoupleId) && a.PartnerId != null) return;

        var cid = Guid.NewGuid().ToString("N");
        a.CoupleId = cid; b.CoupleId = cid;
        a.PartnerId = b.Id; b.PartnerId = a.Id;
        a.BindCode = null; a.BindCodeExpire = null;
        b.BindCode = null; b.BindCodeExpire = null;
        await _db.SaveChangesAsync();
    }

    /// <summary>种子演示用纪念日（仅当表为空时写入；已存在则跳过）。含「每年重复」示例，便于直接体验该功能。</summary>
    private async Task SeedAnniversariesAsync()
    {
        if (await _db.Anniversaries.AnyAsync()) return;

        var now = DateTime.UtcNow;
        var demos = new (string name, AnniversaryType type, DateTime date, int remind, bool yearly)[]
        {
            ("相恋纪念日", AnniversaryType.LoveDay, new DateTime(2024, 1, 1), 3, true),
            ("初次相遇", AnniversaryType.MeetDay, new DateTime(2023, 8, 20), 1, true),
            ("我的生日", AnniversaryType.Birthday, new DateTime(2000, 11, 11), 7, true),
            ("第一次旅行", AnniversaryType.Custom, new DateTime(2025, 5, 1), 0, false),
        };
        foreach (var d in demos)
        {
            var a = new CoupleAnniversary
            {
                Name = d.name, AnniversaryType = d.type, TargetDate = d.date,
                RemindDays = d.remind, IsYearly = d.yearly,
                CreateUserId = 1, CreateTime = now,
                NextRemindTime = new CoupleAnniversary { TargetDate = d.date, IsYearly = d.yearly }.ComputeNextOccurrence()?.AddDays(-d.remind)
            };
            _db.Anniversaries.Add(a);
        }
        await _db.SaveChangesAsync();
    }

    /// <summary>把已有内容数据回填到 demo 情侣空间：取任一已绑定用户的 CoupleId，
    /// 将内容表（受隔离过滤的实体）中 CoupleId 为空的历史/外部灌入数据统一归属该空间，避免被全局过滤器隐藏。</summary>
    private async Task BackfillCoupleIdAsync()
    {
        var demoCid = (await _db.Users.Where(u => u.CoupleId != null && u.CoupleId != "").Select(u => u.CoupleId).FirstOrDefaultAsync());
        if (string.IsNullOrEmpty(demoCid)) return;

        var contentTables = new[]
        {
            "Anniversaries", "Diaries", "DiaryComments", "Wishes", "Albums", "Images",
            "Conflicts", "Letters", "AccountRecords", "DateRecords",
            "SystemMessages", "Footprints"
        };
        foreach (var t in contentTables)
        {
#pragma warning disable EF1002 // 表名/值为常量，非用户输入，内联安全
            await _db.Database.ExecuteSqlRawAsync(
                $"UPDATE `{t}` SET `CoupleId` = {{0}} WHERE `CoupleId` IS NULL OR `CoupleId` = '';", demoCid);
#pragma warning restore EF1002
        }
    }
}
