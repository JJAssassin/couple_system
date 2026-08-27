using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CoupleLoveSystem.Infrastructure.Persistence;

/// <summary>
/// 设计期工厂：供 `dotnet ef migrations add` 在**不启动整个 Web 宿主**的情况下构建模型与生成迁移。
/// 注意：此处**刻意不**替换 IModelCacheKeyFactory（运行时 Program.cs 才替换为多租户版本）。
/// 原因：Pomelo 的迁移比对器会读取 EF 的「read-optimized model」，自定义缓存键工厂会让该路径抛
/// "The requested configuration is not stored in the read-optimized model"。设计期只需幂等生成表结构快照，
/// 全局过滤器的值（CoupleContext.Current）不属于 schema，不在迁移 SQL 内，故用默认缓存键即可，
/// 生成的 InitialCreate 快照与运行时表结构一致。
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CoupleDbContext>
{
    // 设计期不连库即可建模（migrations add 不会打开连接）；显式 ServerVersion 避免 AutoDetect 触发连接。
    // 默认指向本机开发库；真实连接串（含密码）请勿硬编码在此，改用环境变量 DESIGN_MYSQL_CONN 注入
    // （如 CI / 本地连库做 migrations script / database update 时）。默认占位串不含任何真实凭据。
    private const string DefaultConn =
        "Server=127.0.0.1;Port=3306;Database=couple_love;User=app;Password=DEV_PLACEHOLDER;CharSet=utf8mb4;";

    public CoupleDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("DESIGN_MYSQL_CONN") ?? DefaultConn;
        var options = new DbContextOptionsBuilder<CoupleDbContext>()
            .UseMySql(conn, ServerVersion.Parse("8.0.46"),
                my => my.MigrationsAssembly("CoupleLoveSystem.Api"))
            .Options;
        return new CoupleDbContext(options);
    }
}
