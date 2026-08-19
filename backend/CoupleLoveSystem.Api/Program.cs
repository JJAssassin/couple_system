using CoupleLoveSystem.Api.Middlewares;
using CoupleLoveSystem.Api;
using CoupleLoveSystem.Api.Hubs;
using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Options;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Redis;
using CoupleLoveSystem.Infrastructure.Cache;
using CoupleLoveSystem.Infrastructure.Realtime;
using CoupleLoveSystem.Infrastructure.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using CoupleLoveSystem.Infrastructure.Email;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// ---- Serilog ----
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       // Kestrel 传输层会记录原始请求行（含 URL 查询串）。即便握手方案已不再在 URL 带 JWT，
       // 仍关闭其 Information 级 "Request starting" 日志，确保任何意外出现在查询串的令牌都不会落入日志。
       .MinimumLevel.Override("Microsoft.AspNetCore.Server.Kestrel", Serilog.Events.LogEventLevel.Warning));

// ---- Options ----
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));

// ---- DbContext (MySQL / Pomelo) ----
var conn = builder.Configuration.GetConnectionString("MySql")!;

builder.Services.AddDbContext<CoupleDbContext>((sp, opt) =>
{
    opt.UseMySql(conn, ServerVersion.AutoDetect(conn),
        my => my.MigrationsAssembly("CoupleLoveSystem.Api"));
    // 多租户隔离：把当前情侣 id 纳入 EF 模型缓存键，避免全局过滤器被内联缓存为旧值导致跨情侣串数据。
    opt.ReplaceService<IModelCacheKeyFactory, CoupleModelCacheKeyFactory>();
});
// 实时同步广播逻辑已内聚到 CoupleDbContext.SaveChanges/SaveChangesAsync 重写（保存前捕获 [Broadcast] 实体、
// 保存后读取真实 PK 再经 SyncBroadcaster 推送给对应情侣组）。不依赖 EF 的 SavedChanges 拦截器后回调，
// 跨 MySQL/InMemory 均可靠触发，且彻底移除了各 Service 里 17 处手写 NotifyAsync。

// ---- EF Repositories（泛型 + 纪念日自定义） ----
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<IAnniversaryRepository, AnniversaryRepository>();

// ---- Application Services ----
// ---- TokenStore：按配置切换 InMemory / Redis（Redis 接本地 Redis80）----
builder.Services.Configure<TokenStoreOptions>(builder.Configuration.GetSection("TokenStore"));
builder.Services.AddSingleton<RedisTokenStore>(); // 复用单连接
builder.Services.AddSingleton<ICacheService, RedisCacheService>(); // 首页统计/Streak 缓存（Redis，不可用时降级内存）
builder.Services.AddScoped<ITokenStore>(sp =>
{
    var opt = sp.GetRequiredService<IOptions<TokenStoreOptions>>().Value;
    if (opt.Provider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
        return sp.GetRequiredService<RedisTokenStore>();
    return new InMemoryTokenStore(); // 连接失败时降级
});
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<HtmlSanitizerService>(); // 富文本白名单净化（无状态，单例）
builder.Services.AddSingleton<IConnectionIdentityStore, ConnectionIdentityStore>(); // SignalR 握手身份绑定（单实例）
builder.Services.AddScoped<HomeService>();
builder.Services.AddScoped<YearReportService>(); // 年度恋爱报告（数据统计聚合）
builder.Services.AddScoped<LoginRateLimiter>(); // 登录防爆破（IP+账号双维度限速）
builder.Services.AddScoped<AnniversaryService>();
builder.Services.AddScoped<DiaryService>();
builder.Services.AddScoped<WishService>();
builder.Services.AddScoped<TodoService>();
builder.Services.AddScoped<BoardMessageService>();
builder.Services.AddScoped<QuizService>();
builder.Services.AddScoped<AlbumService>();
builder.Services.AddScoped<ConflictService>();
builder.Services.AddScoped<LetterService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<DatePlanService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<TimelineService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<CoupleService>();
builder.Services.AddScoped<PartnerService>();
builder.Services.AddScoped<FootprintService>();
builder.Services.AddScoped<QuoteService>();
builder.Services.AddScoped<SyncBroadcaster>();

// ---- 邮件通知（SMTP，可选）：Email.Enabled=true 且配置 SmtpHost 时走真实 SMTP；否则安全降级为 NoOp（仅日志、不连网）----
builder.Services.AddScoped<SystemMessageEmailNotifier>();
builder.Services.AddScoped<IEmailSender>(sp =>
{
    var opt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailOptions>>().Value;
    if (opt.Enabled && !string.IsNullOrWhiteSpace(opt.SmtpHost))
        return new SmtpEmailSender(
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailOptions>>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SmtpEmailSender>>());
    return new NoOpEmailSender(sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<NoOpEmailSender>>());
});

// ---- 定时任务（纪念日提醒 / 书信解锁），用托管服务替代独立 Quartz 项目，零额外依赖 ----
builder.Services.AddHostedService<ScheduledHostedService>();

// ---- 定时任务分布式锁：Redis(Provider=Redis) 跨实例互斥，否则进程内互斥，避免重复发提醒 ----
builder.Services.AddSingleton<IDistributedJobLock>(sp =>
{
    var opt = sp.GetRequiredService<IOptions<TokenStoreOptions>>().Value;
    return opt.Provider.Equals("Redis", StringComparison.OrdinalIgnoreCase)
        ? new RedisJobLock(opt.Configuration)
        : new InMemoryJobLock();
});

// ---- 生产环境强制 Redis TokenStore：禁止内存降级（多实例/重启会丢失刷新令牌）----
if (builder.Environment.IsProduction())
{
    var tsOpt = builder.Configuration.GetSection("TokenStore").Get<TokenStoreOptions>()!;
    if (!tsOpt.Provider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("生产环境 TokenStore.Provider 必须为 Redis，禁止 InMemory 降级。");
}

// ---- JWT（RSA 非对称优先，密钥移出 appsettings）----
// 注册 JwtKeyResolver 单例；验签密钥通过 Configure<JwtKeyResolver> 延迟从容器解析，
// 既避免注册期过早构造，也避免 BuildServiceProvider 产生重复单例副本（ASP0000）。
builder.Services.AddSingleton<JwtKeyResolver>();

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<JwtKeyResolver>((o, key) =>
    {
        o.RequireHttpsMetadata = false; // 内网 IIS 终结点已 HTTPS，此处关以方便开发
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key.ValidationKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
        // 注意：SignalR 中枢不再通过 ?access_token= 查询串取令牌（令牌会泄露到浏览器历史 / 服务端日志 / 代理日志）。
        // 改为「握手方案」：前端匿名建 WebSocket，再调 [Authorize] 的 /api/sync/authenticate 上报 connectionId，
        // 后端登记身份并把连接加入对应情侣组。故此处不再注入 URL 令牌。
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR();
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<DbSeeder>();

var app = builder.Build();

// 生产启动 fail-fast（评审 #4 残留项 P2-7 收尾）：
// 上方已强制 Provider=Redis；这里再做一次真实连通性探测——RedisTokenStore 的
// AbortOnConnectFail=false 会让应用「带病运行」到请求期才炸（刷新令牌读写失败），
// 多实例部署下尤其隐蔽。启动时探测失败直接拒绝启动，报错信息含配置地址便于排查。
if (app.Environment.IsProduction())
{
    var redis = app.Services.GetRequiredService<RedisTokenStore>();
    if (!redis.Ping())
        throw new InvalidOperationException(
            "生产环境 Redis 不可达（TokenStore:Configuration="
            + app.Configuration["TokenStore:Configuration"]
            + "），拒绝启动：refresh token 需跨实例共享，请先恢复 Redis 再启动。");
}

// 启动期：应用待执行的 EF 迁移（建/改表，替代原先的 EnsureCreated + 手搓 ALTER），再执行幂等数据种子。
// 改用 Migrations 后，启动日志不再出现 ALTER ... ADD COLUMN [ERR] 噪音，schema 演进可版本化。
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CoupleDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
}

// ---- Pipeline（顺序：静态文件 -> Swagger -> 异常 -> 认证 -> 授权 -> 路由） ----
app.UseStaticFiles(); // 提供 wwwroot 静态资源（Swagger 等）

// 显式托管 /uploads：上传图片落盘于 WebRootPath??ContentRootPath/uploads，
// 而默认 UseStaticFiles 仅在 wwwroot 存在时托管，bin 下缺 wwwroot 会导致图片 404。
// 这里用 PhysicalFileProvider 直接映射该目录，确保头像/封面/相册图可访问。
var uploadsRoot = Path.Combine(app.Environment.WebRootPath ?? app.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadsRoot)) Directory.CreateDirectory(uploadsRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = "/uploads",
    OnPrepareResponse = ctx =>
    {
        // 禁止上传目录内的脚本被执行，降低风险
        var ext = Path.GetExtension(ctx.File.Name);
        if (ext.Equals(".js", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".exe", StringComparison.OrdinalIgnoreCase))
            ctx.Context.Response.StatusCode = 404;
    }
});
app.UseSwagger(); app.UseSwaggerUI();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<AccessTokenScrubMiddleware>(); // 防御性脱敏：摘除任何 ?access_token=，防令牌写入日志
app.UseAuthentication();
app.UseAuthorization();
app.UseCoupleScope(); // 在每个请求中写入当前情侣空间（CoupleContext.Current），供隔离过滤与盖章使用
app.MapControllers();
app.MapHub<SyncHub>("/hub/sync");

app.Run();

public partial class Program { } // 供集成测试引用
