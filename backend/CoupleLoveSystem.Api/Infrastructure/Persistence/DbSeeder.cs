using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Core.Enums;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CoupleLoveSystem.Infrastructure.Persistence;

/// <summary>
/// 幂等数据种子（不再负责建表/改表——schema 由 EF Migration 管理，见 Program.cs 的 MigrateAsync）。
/// 仅做数据层的开发期初始化：温情语录、演示纪念日、预绑定 demo 双账号、把历史空 CoupleId 内容回填到 demo 空间、
/// 以及情侣级共享设置行。所有写入均按「不存在才写」的幂等原则，重复启动不会重复插入。
/// </summary>
public class DbSeeder
{
    private readonly CoupleDbContext _db;
    private readonly bool _createDemoAccounts;
    public DbSeeder(CoupleDbContext db, IConfiguration config)
    {
        _db = db;
        _createDemoAccounts = config.GetValue("Seed:CreateDemoAccounts", true);
    }

    public async Task SeedAsync()
    {
        // 种子每日一句温情语录（仅当语录表为空时写入）
        await SeedQuotesAsync();
        // 预绑定两个 demo 账号（依赖账号已存在；首次启动账号尚未创建时此处为 no-op，账号创建后再绑定）
        await SeedBindAsync();
        // 演示用纪念日（仅当表为空时写入）
        await SeedAnniversariesAsync();
        // 默契问答内置题库（仅当题库为空时写入）
        await SeedQuizQuestionsAsync();
        // 把已有内容数据回填到 demo 情侣空间（CoupleId 为空的历史/外部灌入数据统一归属该空间，避免被全局过滤器隐藏）。
        // 必须放在所有 Seed* 之后：种子写入时无 HTTP 上下文（CoupleContext.Current 为 null），
        // 新种子行 CoupleId 为空，靠这一步在「同一次启动内」补盖章。
        await BackfillCoupleIdAsync();

        // 确保情侣级共享设置行存在（整库单对情侣，Key=global）；LoveStartTime 初始为 null（未设置，不显示虚假天数）
        if (!await _db.Settings.AnyAsync(s => s.Key == "global"))
        {
            _db.Settings.Add(new CoupleSetting { Key = "global", CreateUserId = 1, CreateTime = DateTime.UtcNow });
            await _db.SaveChangesAsync();
        }

        // 已存在任意账号、或显式关闭默认账号种子，则跳过创建
        // （评审 #5：生产可在 compose .env 设 Seed__CreateDemoAccounts=false 禁用弱密码账号）
        if (!_createDemoAccounts || await _db.Users.AnyAsync()) return;

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

    /// <summary>种子每日一句温情语录（仅当语录表为空时写入；已存在则跳过，不重复插入）。
    /// 存在性判定用 IgnoreQueryFilters：绕开软删除过滤，用户删过的语录不会在下次启动被重新种回来。</summary>
    private async Task SeedQuotesAsync()
    {
        if (await _db.Quotes.IgnoreQueryFilters().AnyAsync()) return;

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

    /// <summary>种子演示用纪念日（仅当表为空时写入；已存在则跳过）。含「每年重复」示例，便于直接体验该功能。
    ///
    /// 存在性判定必须用 IgnoreQueryFilters（历史 bug 修复）：CoupleAnniversary 是 ICoupleScoped，
    /// 全局过滤器为 !IsDeleted && (CoupleId == CoupleContext.Current || CoupleId == null)。
    /// 种子阶段无 HTTP 上下文 → Current 为 null → 过滤器只能看见 CoupleId 为 null 的行；
    /// 而 BackfillCoupleIdAsync 会把这些行盖章成 demo 情侣，于是下次启动过滤器又「看不见」它们，
    /// 判定为空 → 重复种入。实测该 bug 已让 4 条演示纪念日各重复 40 份（每次重启 +4）。
    /// 加 IgnoreQueryFilters 后无论 CoupleId / IsDeleted 为何都能看见，真正幂等。</summary>
    private async Task SeedAnniversariesAsync()
    {
        if (await _db.Anniversaries.IgnoreQueryFilters().AnyAsync()) return;

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

    /// <summary>种子默契问答内置题库：幂等补齐到 500 题（已有题目则只补充缺失部分直到达到目标数量，不删除用户数据）。
    /// 存在性判定用 IgnoreQueryFilters 保证幂等。内置题标记 IsBuiltin=true，接口层禁止删除。</summary>
    private async Task SeedQuizQuestionsAsync()
    {
        const int target = 500;
        var existingTexts = new HashSet<string>(
            (await _db.QuizQuestions.IgnoreQueryFilters().Select(q => q.Text).ToListAsync())
            .Where(t => !string.IsNullOrEmpty(t)).Select(t => t!));
        if (existingTexts.Count >= target) return;

        var bank = BuildQuizBank();
        var now = DateTime.UtcNow;
        var maxSort = await _db.QuizQuestions.IgnoreQueryFilters().Select(q => (int?)q.SortOrder).MaxAsync() ?? -1;
        var sort = maxSort + 1;
        var added = false;
        foreach (var q in bank)
        {
            if (existingTexts.Contains(q.text)) continue;
            _db.QuizQuestions.Add(new CoupleQuizQuestion
            {
                Text = q.text,
                OptionsJson = System.Text.Json.JsonSerializer.Serialize(q.options),
                Category = q.cat,
                SortOrder = sort++,
                IsBuiltin = true,
                CreateUserId = 1,
                CreateTime = now,
            });
            existingTexts.Add(q.text);
            added = true;
            if (existingTexts.Count >= target) break;
        }
        if (added) await _db.SaveChangesAsync();
    }

    /// <summary>构建内置题库：16 道精选 + 模板×变体批量生成，合计 500+（去重后取前 500）。</summary>
    private static List<(string cat, string text, string[] options)> BuildQuizBank()
    {
        var bank = new List<(string cat, string text, string[] options)>();
        void Add(string cat, string text, string[] opts) => bank.Add((cat, text, opts));

        // —— 16 道精选（保留）——
        Add("口味", "如果只能选一样当宵夜，TA 会选？", new[] { "火锅", "烧烤", "螺蛳粉", "泡面加蛋" });
        Add("口味", "奶茶必点甜度是？", new[] { "全糖", "七分糖", "五分糖", "不额外加糖" });
        Add("口味", "TA 最不能接受的食物是？", new[] { "香菜", "臭豆腐", "榴莲", "肥肉" });
        Add("习惯", "周末更想怎么过？", new[] { "在家躺平", "出门逛街", "短途旅行", "约朋友聚会" });
        Add("习惯", "TA 睡觉前最后做的一件事是？", new[] { "刷手机", "看书", "聊天", "直接睡" });
        Add("习惯", "定了闹钟之后 TA 通常？", new[] { "秒起", "赖床 10 分钟", "反复贪睡", "起了又躺回去" });
        Add("习惯", "出门旅行 TA 收拾行李的方式是？", new[] { "提前几天列清单", "出发前一晚打包", "临出门随手塞", "让对方帮忙收" });
        Add("性格", "吵架之后 TA 更可能？", new[] { "主动来找我", "冷静一会儿再说", "等我先开口", "假装没事发生" });
        Add("性格", "TA 收到礼物时最看重？", new[] { "心意和惊喜", "实用性", "价格档次", "包装和仪式感" });
        Add("性格", "遇到难题 TA 的第一反应是？", new[] { "自己先扛着", "立刻找我商量", "上网搜答案", "先放一放" });
        Add("回忆", "我们第一次约会的主要活动是？", new[] { "吃饭", "看电影", "散步逛街", "在家待着" });
        Add("回忆", "对方最先记住我的哪一点？", new[] { "长相", "声音", "性格", "某句话" });
        Add("默契", "如果中了一笔小钱，TA 想先？", new[] { "存起来", "带我去旅行", "换手机/数码", "请大家吃一顿" });
        Add("默契", "TA 心里理想的同居分工是？", new[] { "一人做饭一人洗碗", "谁有空谁做", "全靠外卖", "各做各的" });
        Add("默契", "TA 更希望纪念日怎么庆祝？", new[] { "精心准备惊喜", "两个人安静吃饭", "拍一组照片", "买个大礼物" });
        Add("默契", "对方觉得我们最像的地方是？", new[] { "口味", "作息", "笑点", "花钱习惯" });

        // —— 模板 × 变体批量生成 —— 每族：类目 / 含 {0} 占位的句式模板 / 主题词 / 选项集
        var families = new (string cat, string[] tmpl, string[] subs, string[] opts)[]
        {
            ("口味", new[]{ "如果给 TA 带一杯{0}，TA 最想要？", "TA 自己点{0}时更常选？", "逛街口渴 TA 会挑一杯{0}？" },
                new[]{ "奶茶", "咖啡", "碳酸饮料", "果汁", "酸奶", "气泡水", "热可可", "柠檬茶" },
                new[]{ "经典原味", "少糖少冰", "加料加满", "无糖健康版" }),
            ("口味", new[]{ "TA 最馋的一顿{0}是？", "周末放纵餐 TA 会选{0}？", "两个人下馆子 TA 更偏向{0}？" },
                new[]{ "火锅", "烧烤", "日料", "西餐", "川菜", "家常菜", "螺蛳粉", "沙拉" },
                new[]{ "重辣过瘾", "微辣刚好", "不吃辣", "看心情" }),
            ("口味", new[]{ "TA 囤零食必买{0}？", "深夜追剧 TA 手里拿的通常是{0}？", "超市购物车里 TA 一定塞{0}？" },
                new[]{ "薯片", "巧克力", "坚果", "辣条", "饼干", "冰淇淋", "芒果干", "瓜子" },
                new[]{ "咸香脆口", "甜党专属", "酸辣上头", "越嚼越香" }),
            ("习惯", new[]{ "关于{0}，TA 是？", "如果没人叫，TA 的{0}状态是？", "聊到{0} TA 属于哪类？" },
                new[]{ "早起", "熬夜", "午睡", "赖床", "定闹钟", "周末补觉" },
                new[]{ "雷打不动", "随性自由", "看当天安排", "被拖着走" }),
            ("习惯", new[]{ "家务里 TA 更情愿{0}？", "谁先忍不住动手{0}？", "轮到{0} TA 的表现是？" },
                new[]{ "洗碗", "拖地", "倒垃圾", "叠衣服", "买菜", "收纳" },
                new[]{ "抢着做", "轮到自己才做", "能拖就拖", "等我提醒" }),
            ("习惯", new[]{ "TA 对{0}的洁癖是？", "出门前 TA 最后检查的是{0}？", "换新{0} TA 的节奏是？" },
                new[]{ "手机", "电脑", "耳机", "平板", "手表" },
                new[]{ "必须满电", "随便吧", "贴膜戴壳严实", "定期清理" }),
            ("性格", new[]{ "TA {0}时最典型的表现是？", "要哄 TA 的{0}状态，先？", "察觉 TA {0}的信号是？" },
                new[]{ "开心", "生气", "委屈", "紧张", "受挫" },
                new[]{ "直接表达", "闷着不说话", "找人吐槽", "自己消化" }),
            ("性格", new[]{ "TA 更享受{0}？", "面对{0} TA 的反应是？", "被拉去{0} TA 会？" },
                new[]{ "聚会", "独处", "被夸", "被吐槽", "团建", "视频通话" },
                new[]{ "热闹才开心", "一个人更自在", "看和谁", "看心情" }),
            ("回忆", new[]{ "我们{0}的场景 TA 记得最清的是？", "回想{0}，TA 最在意的细节是？", "问起{0} TA 先笑的是？" },
                new[]{ "第一次见面", "第一次聊天", "第一次约会", "确定关系那天", "第一次牵手", "第一份礼物" },
                new[]{ "当天的天气", "对方说的话", "穿了什么", "那顿饭" }),
            ("回忆", new[]{ "提到{0}，TA 先想到？", "{0}里 TA 最舍不得的是？", "翻到{0}的照片 TA 会说？" },
                new[]{ "第一次旅行", "最远的地方", "看过的海", "爬过的山", "住过的民宿", "夜市" },
                new[]{ "照片里的笑", "当地的味道", "同行的人", "自由的感觉" }),
            ("默契", new[]{ "{0} TA 更想要？", "送 TA 礼物，{0} TA 最吃哪套？", "准备{0}的惊喜 TA 会更感动？" },
                new[]{ "生日", "纪念日", "情人节", "普通周二", "升职", "生病时" },
                new[]{ "惊喜大过天", "实用主义", "手写卡片", "一起经历" }),
            ("默契", new[]{ "聊到{0}，TA 的蓝图是？", "{0}这件事 TA 的态度是？", "做{0}的决定 TA 更倾向？" },
                new[]{ "定居城市", "养宠物", "要孩子", "买房", "换工作", "存钱" },
                new[]{ "早有计划", "顺其自然", "听你的", "再等等看" }),
            ("生活", new[]{ "TA 花钱最不手软的是{0}？", "在{0}上 TA 是？", "逛街 TA 会为{0}冲动？" },
                new[]{ "买衣服", "买数码", "请客", "囤日用品", "买花", "点外卖" },
                new[]{ "冲动型", "比价型", "只买对的", "囤货型" }),
            ("娱乐", new[]{ "TA 刷{0}的口味是？", "窝沙发 TA 会点开{0}？", "推荐{0}给 TA 命中率是？" },
                new[]{ "电影", "剧集", "综艺", "纪录片", "动漫", "悬疑剧" },
                new[]{ "悬疑烧脑", "甜宠治愈", "爆笑喜剧", "热血动作" }),
            ("娱乐", new[]{ "TA 更想拉你一起玩{0}？", "{0} TA 的水平是？", "周末开黑 TA 选{0}？" },
                new[]{ "手游", "主机游戏", "桌游", "猜拳", "拼图", "密室逃脱" },
                new[]{ "佛系娱乐", "上分狂魔", "策略脑", "纯看心情" }),
            ("健康", new[]{ "TA 更可能坚持的是{0}？", "约 TA {0} TA 会？", "{0}计划 TA 的响应是？" },
                new[]{ "跑步", "瑜伽", "撸铁", "散步", "游泳", "骑行" },
                new[]{ "一口答应", "看天气", "需鼓励", "直接拒绝" }),
            ("健康", new[]{ "提到{0}，TA 的态度是？", "{0} TA 是？", "减肥期 TA 对{0}是？" },
                new[]{ "轻食", "碳水", "奶茶", "夜宵", "戒酒", "控糖" },
                new[]{ "严格自律", "快乐至上", "偶尔放纵", "完全拒绝" }),
            ("审美", new[]{ "TA 更偏爱的{0}是？", "买{0} TA 的选择是？", "出门前纠结{0} TA 倾向于？" },
                new[]{ "颜色", "风格", "鞋子", "配饰", "包包", "香水" },
                new[]{ "低调中性", "亮眼吸睛", "经典耐看", "舒服就行" }),
            ("审美", new[]{ "TA 布置家里最在意{0}？", "{0} TA 觉得？", "挑{0} TA 更看？" },
                new[]{ "灯光", "香薰", "绿植", "收纳", "挂画", "地毯" },
                new[]{ "氛围感", "实用性", "有生命力", "越简越好" }),
            ("工作", new[]{ "关于{0}，TA 是？", "{0}时 TA 的状态是？", "被安排{0} TA 会？" },
                new[]{ "加班", "开会", "摸鱼", "通勤", "写周报", "团建" },
                new[]{ "燃尽自己", "游刃有余", "能省则省", "准时跑路" }),
            ("节日", new[]{ "{0} TA 更想怎么过？", "{0} TA 最在意？", "计划{0} TA 倾向？" },
                new[]{ "春节", "中秋", "跨年", "生日", "五一", "七夕" },
                new[]{ "回家团圆", "两人小聚", "出去玩", "热热闹闹" }),
            ("科技", new[]{ "TA 对{0}是？", "{0} TA 更倾向？", "升级{0} TA 的态度是？" },
                new[]{ "手机换代", "耳机", "手表", "相机", "键盘", "充电宝" },
                new[]{ "追新党", "够用就好", "颜值优先", "性价比" }),
            ("宠物", new[]{ "如果养{0}，TA 是？", "提到{0} TA 会？", "刷到{0}的视频 TA 反应？" },
                new[]{ "猫", "狗", "仓鼠", "鱼", "兔子", "乌龟" },
                new[]{ "瞬间融化", "理智拒绝", "观望中", "已列入计划" }),
            ("沟通", new[]{ "闹别扭 TA 更常{0}？", "{0}这件事 TA 是？", "和好后 TA 对{0}是？" },
                new[]{ "冷战", "翻旧账", "先认错", "写小作文", "甩脸色", "阴阳怪气" },
                new[]{ "主动破冰", "等我先开口", "越说越上头", "默默记着" }),
            ("居家", new[]{ "窝在家 TA 更常{0}？", "{0}是 TA 的周末打开方式？", "两人独处 TA 偏好{0}？" },
                new[]{ "追剧", "听歌", "打游戏", "看书", "做手工", "烘焙" },
                new[]{ "沉浸式", "当背景音", "碎片时间", "看心情" }),
            ("礼物", new[]{ "收到{0} TA 的反应是？", "{0} TA 更看重？", "挑{0}送 TA 命中率？" },
                new[]{ "花", "红包", "手作", "情书", "旅行", "数码" },
                new[]{ "浪漫优先", "实用优先", "惊喜优先", "心意优先" }),
            ("社交", new[]{ "TA 发朋友圈{0}的频率是？", "{0} TA 属于哪类？", "刷到{0} TA 会？" },
                new[]{ "合照", "美食", "表情包", "转发鸡汤", "打卡", "晒宠物" },
                new[]{ "高频分享", "偶尔冒泡", "从不发", "仅私密可见" }),
            ("情绪", new[]{ "TA {0}最想要的是？", "察觉 TA {0}，先？", "{0}时 TA 更需要？" },
                new[]{ "委屈时", "压力大时", "失眠时", "想家时", "被误解时", "逢年过节" },
                new[]{ "想被抱抱", "想一个人静静", "想吃顿好的", "想听你说说话" }),
            ("旅行", new[]{ "出行 TA 更选{0}？", "{0}是 TA 的节奏？", "规划{0} TA 倾向？" },
                new[]{ "自驾", "高铁", "飞机", "骑行", "徒步", "邮轮" },
                new[]{ "说走就走", "做好攻略", "跟着你", "随缘" }),
            ("消费", new[]{ "TA 对{0}的态度是？", "{0} TA 更常？", "用{0} TA 是？" },
                new[]{ "拼单", "抢券", "二手", "会员", "临期", "平替" },
                new[]{ "精打细算", "能省则省", "无所谓", "品质优先" }),
        };

        foreach (var f in families)
            foreach (var sub in f.subs)
                foreach (var t in f.tmpl)
                    Add(f.cat, string.Format(t, sub), f.opts);

        return bank;
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
            "Conflicts", "AccountRecords", "DateRecords",
            "SystemMessages", "Footprints", "QuizQuestions"
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
