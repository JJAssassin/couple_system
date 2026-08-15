using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CoupleLoveSystem.Infrastructure.Persistence;

/// <summary>
/// 多租户（情侣空间）模型缓存键工厂。
///
/// 背景：全局查询过滤器 `!IsDeleted &amp;&amp; (CoupleId == CoupleContext.Current || CoupleId == null)`
/// 在 EF 编译查询计划时会把 `CoupleContext.Current` 当时的值**内联为常数并缓存**。
/// 若仅依赖默认的模型缓存键（不含情侣 id），同一进程内不同情侣/匿名的查询会复用上一次编译的
/// 过滤值，导致跨情侣数据串号（隔离失效，且只在多情侣并发时暴露，单情侣冒烟测不出）。
///
/// 修复：把「当前情侣空间标识」纳入模型缓存键，使每个不同的情侣 id 拥有独立编译的模型，
/// 其全局过滤器被正确内联为该情侣的 CoupleId。单次请求内 Current 稳定，跨请求各自正确。
/// 代价：每个不同情侣 id 首次访问时编译一次模型（之后按 id 缓存复用），对本项目规模完全可接受。
///
/// 基础键用 context 类型（本项目仅 CoupleDbContext 一种），不依赖默认 IModelCacheKeyFactory，
/// 从而避免 ReplaceService 引发的循环依赖。
/// </summary>
public sealed class CoupleModelCacheKey : IEquatable<CoupleModelCacheKey>
{
    private readonly Type _contextType;
    private readonly string? _coupleId;

    public CoupleModelCacheKey(Type contextType, string? coupleId)
    {
        _contextType = contextType;
        _coupleId = coupleId;
    }

    public bool Equals(CoupleModelCacheKey? other)
        => other is not null && _contextType == other._contextType && _coupleId == other._coupleId;

    public override bool Equals(object? obj) => Equals(obj as CoupleModelCacheKey);

    public override int GetHashCode() => HashCode.Combine(_contextType, _coupleId);
}

public class CoupleModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
        => new CoupleModelCacheKey(context.GetType(), CoupleContext.Current);
}
