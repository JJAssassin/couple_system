using CoupleLoveSystem.Api;
using CoupleLoveSystem.Api.Hubs;
using CoupleLoveSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CoupleLoveSystem.Infrastructure.Persistence;

public class CoupleDbContext : DbContext
{
    // 实时广播器：由 DI 作用域注入（scoped）；无 DI 的测试用 new CoupleDbContext(options) 时默认为 null，广播安全跳过。
    // 不通过 GetService 在 EF 内部服务容器解析 scoped 服务（该路径在 InMemory/测试下解析不可靠）。
    private readonly SyncBroadcaster? _sync;

    public CoupleDbContext(DbContextOptions<CoupleDbContext> options, SyncBroadcaster? sync = null) : base(options)
        => _sync = sync;

    /// <summary>本次 SaveChanges 操作的情侣空间标识，在 SaveChanges/SaveChangesAsync 的「同步部分」捕获
    /// （此刻 CoupleContext.Current 仍有效）。保存拦截器与广播器在异步阶段改读此实例字段，
    /// 避免 AsyncLocal 在 EF 内部的 await 续体（ExecutionContext 切换）上丢失值——
    /// 否则广播会误推到 anon 组，前端收不到实时更新。</summary>
    internal string? OperatingCoupleId { get; private set; }

    public DbSet<CoupleUser> Users => Set<CoupleUser>();
    public DbSet<CoupleAnniversary> Anniversaries => Set<CoupleAnniversary>();
    public DbSet<CoupleDiary> Diaries => Set<CoupleDiary>();
    public DbSet<CoupleDiaryComment> DiaryComments => Set<CoupleDiaryComment>();
    public DbSet<CoupleWish> Wishes => Set<CoupleWish>();
    public DbSet<CoupleAlbum> Albums => Set<CoupleAlbum>();
    public DbSet<CoupleImage> Images => Set<CoupleImage>();
    public DbSet<CoupleConflict> Conflicts => Set<CoupleConflict>();
    public DbSet<CoupleLetter> Letters => Set<CoupleLetter>();
    public DbSet<CoupleAccountRecord> AccountRecords => Set<CoupleAccountRecord>();
    public DbSet<CoupleDateRecord> DateRecords => Set<CoupleDateRecord>();
    public DbSet<CoupleSystemMessage> SystemMessages => Set<CoupleSystemMessage>();
    public DbSet<CoupleSetting> Settings => Set<CoupleSetting>();
    public DbSet<CoupleFootprint> Footprints => Set<CoupleFootprint>();
    public DbSet<CoupleQuote> Quotes => Set<CoupleQuote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 全局字符集 + 全局查询过滤器（所有 BaseEntity）
        // 关键修复：此前对同一实体类型先后调用两次 HasQueryFilter，EF 以「最后一次」为准，
        // 导致软删除(!IsDeleted) 被情侣过滤覆盖而失效（逻辑删除的内容仍可被查询到）。
        // 现合并为单条 lambda：!IsDeleted && (CoupleId==Current || CoupleId==null)，
        // 既保留软删除，又保留情侣隔离与对 null 行的宽容逃逸。
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var entity = modelBuilder.Entity(entityType.ClrType);
            entity.HasCharSet("utf8mb4");

            var param = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
            var isDeletedProp = System.Linq.Expressions.Expression.Property(param, nameof(BaseEntity.IsDeleted));
            var notDeleted = System.Linq.Expressions.Expression.Not(isDeletedProp);

            if (typeof(ICoupleScoped).IsAssignableFrom(entityType.ClrType))
            {
                // 情侣空间隔离：!IsDeleted && (CoupleId == Current || CoupleId == null)
                // Current 为静态 CoupleContext.Current（AsyncLocal），查询执行时才取值；
                // null 逃逸保留给尚未盖章的历史行 / 系统级消息（CoupleId 真实为 null 的内容，对所有人可见）。
                var cidProp = System.Linq.Expressions.Expression.Property(param, nameof(BaseEntity.CoupleId));
                var currentProp = typeof(CoupleContext).GetProperty(nameof(CoupleContext.Current))!;
                var currentConst = System.Linq.Expressions.Expression.Property(null, currentProp);
                var eqCurrent = System.Linq.Expressions.Expression.Equal(cidProp, currentConst);
                var nullConst = System.Linq.Expressions.Expression.Constant(null, typeof(string));
                var eqNull = System.Linq.Expressions.Expression.Equal(cidProp, nullConst);
                var coupleFilter = System.Linq.Expressions.Expression.OrElse(eqCurrent, eqNull);
                var combined = System.Linq.Expressions.Expression.AndAlso(notDeleted, coupleFilter);
                entity.HasQueryFilter(System.Linq.Expressions.Expression.Lambda(combined, param));

                // 性能：几乎所有查询都带 WHERE CoupleId=，必须建索引，否则全表扫描。
                entity.HasIndex(nameof(BaseEntity.CoupleId));
            }
            else
            {
                entity.HasQueryFilter(System.Linq.Expressions.Expression.Lambda(notDeleted, param));
            }
        }

        ConfigureAnniversary(modelBuilder);
        ConfigureDiary(modelBuilder);
        ConfigureImage(modelBuilder);
        ConfigureLetter(modelBuilder);
        ConfigureSetting(modelBuilder);
        ConfigureFootprint(modelBuilder);
        ConfigureQuote(modelBuilder);
    }

    private static void ConfigureAnniversary(ModelBuilder mb)
    {
        mb.Entity<CoupleAnniversary>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.NextRemindTime);
            e.HasIndex(x => x.CreateUserId);
        });
    }
    private static void ConfigureDiary(ModelBuilder mb)
    {
        mb.Entity<CoupleDiary>(e =>
        {
            e.HasIndex(x => x.PermissionType);
            e.HasIndex(x => x.CreateUserId);
            e.HasIndex(x => x.DiaryDate);
        });
        mb.Entity<CoupleDiaryComment>(e =>
        {
            e.HasIndex(x => x.DiaryId);
            e.HasOne<CoupleDiary>().WithMany().HasForeignKey(x => x.DiaryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
    private static void ConfigureImage(ModelBuilder mb)
    {
        mb.Entity<CoupleImage>(e =>
        {
            e.HasIndex(x => x.AlbumId);
            e.HasOne<CoupleAlbum>().WithMany().HasForeignKey(x => x.AlbumId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
    private static void ConfigureLetter(ModelBuilder mb)
    {
        mb.Entity<CoupleLetter>(e =>
        {
            e.HasIndex(x => new { x.IsUnlocked, x.UnlockTime });
            e.HasIndex(x => x.ReceiverUserId);
        });
    }
    private static void ConfigureSetting(ModelBuilder mb)
    {
        mb.Entity<CoupleSetting>(e =>
        {
            e.HasIndex(x => x.Key);
        });
    }
    private static void ConfigureFootprint(ModelBuilder mb)
    {
        mb.Entity<CoupleFootprint>(e =>
        {
            e.HasIndex(x => x.CreateUserId);
        });
    }
    private static void ConfigureQuote(ModelBuilder mb)
    {
        mb.Entity<CoupleQuote>(e =>
        {
            e.HasIndex(x => x.SortOrder);
        });
    }

    // 插入时自动盖章当前情侣空间：内容实体（ICoupleScoped）新建行若未显式指定 CoupleId，
    // 则填入 CoupleContext.Current（由中间件按 JWT 声明写入），从而实现"新建内容自动归属当前情侣"。
    // 后台托管服务等无 HTTP 上下文的场景 Current 为 null，此时留空（由 SeedAsync 回填或由全局过滤器宽容放行）。
    public override int SaveChanges()
    {
        OperatingCoupleId = CoupleContext.Current;
        StampCoupleId();
        var pending = CaptureBroadcasts();
        var affected = base.SaveChanges();
        _ = BroadcastAfterAsync(pending, default); // 同步路径：尽力广播，fire-and-forget（best-effort）
        return affected;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        OperatingCoupleId = CoupleContext.Current;
        StampCoupleId();
        var pending = CaptureBroadcasts();
        var affected = await base.SaveChangesAsync(cancellationToken);
        await BroadcastAfterAsync(pending, cancellationToken);
        return affected;
    }

    /// <summary>保存前捕获本次变更中带 [Broadcast] 的实体，构造 (模块, 操作类型, EntityEntry) 列表。
    /// 必须在 base.SaveChanges 之前调用——此刻 ChangeTracker 仍保留 Added/Modified/Deleted 状态，可区分操作类型。
    /// 情侣空间取自 OperatingCoupleId（SaveChanges 同步段已捕获的实例字段，不受 AsyncLocal 在异步续体丢失影响）。
    /// 种子 / 后台托管服务无情侣上下文时 OperatingCoupleId 为 null，返回 null 表示不广播。</summary>
    private List<(string Module, string Kind, EntityEntry Entry)>? CaptureBroadcasts()
    {
        var coupleId = OperatingCoupleId;
        if (string.IsNullOrEmpty(coupleId)) return null;

        var list = new List<(string, string, EntityEntry)>();
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var attr = entry.Entity.GetType().GetCustomAttribute<BroadcastAttribute>();
            if (attr is null) continue;

            var kind = entry.State switch
            {
                EntityState.Added => "created",
                EntityState.Modified => "updated",
                EntityState.Deleted => "deleted",
                _ => "updated"
            };
            list.Add((attr.Module, kind, entry));
        }

        return list.Count > 0 ? list : null;
    }

    /// <summary>保存后广播：此时 PK 已由 EF 写回实体，从捕获的 EntityEntry.Entity 读取真实主键。
    /// 用 OperatingCoupleId 定组，避免依赖异步阶段已失效的 AsyncLocal。GetService 取不到 SyncBroadcaster（无 DI）时安全跳过。</summary>
    private async Task BroadcastAfterAsync(IReadOnlyList<(string Module, string Kind, EntityEntry Entry)>? pending, CancellationToken ct)
    {
        if (pending is null || pending.Count == 0) return;

        var sync = _sync;
        if (sync is null) return;

        var byModule = new Dictionary<string, List<SyncChange>>();
        foreach (var (module, kind, entry) in pending)
        {
            long? id = entry.Entity is BaseEntity be ? be.Id : null;
            if (!byModule.TryGetValue(module, out var changes))
                byModule[module] = changes = new List<SyncChange>();
            changes.Add(new SyncChange(kind, id));
        }

        foreach (var kv in byModule)
            await sync.NotifySignalAsync(new SyncSignal(kv.Key, kv.Value), OperatingCoupleId, ct);
    }

    private void StampCoupleId()
    {
        var cid = CoupleContext.Current;
        foreach (var entry in ChangeTracker.Entries<ICoupleScoped>())
        {
            if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.CoupleId))
                entry.Entity.CoupleId = cid;
        }
    }
}
