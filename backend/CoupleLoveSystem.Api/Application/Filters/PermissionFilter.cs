using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Core.Enums;
using CoupleLoveSystem.Core.Result;
using System.Linq.Expressions;

namespace CoupleLoveSystem.Application.Filters;

/// <summary>
/// 业务数据权限兜底。前端只做 UI 隐藏，这里强制按后端判定，绝不信任前端传入的 ID。
/// 判定矩阵：
///   Public       -> 双方可读写
///   PrivateSelf  -> 仅本人可见，对方不可见（列表过滤掉 / 详情抛 Forbidden）
///   ViewOnlyOther-> 双方可读，仅 owner 可写
/// </summary>
public static class PermissionFilter
{
    /// <summary>详情场景：无权直接抛 ForbiddenException -> 全局中间件转 403。</summary>
    public static void EnsureVisible(long currentUserId, IProtectable entity)
    {
        if (currentUserId == entity.CreateUserId) return; // 本人永远可见
        if (entity.PermissionType == PermissionType.PrivateSelf)
            throw new ForbiddenException("无权访问该私密内容");
    }

    /// <summary>列表场景：过滤掉不可见行（不抛，避免反向泄露存在性）。</summary>
    public static IQueryable<T> WhereVisible<T>(IQueryable<T> query, long currentUserId) where T : IProtectable
    {
        // SQL: ownerId == current OR PermissionType != PrivateSelf
        var param = Expression.Parameter(typeof(T), "x");
        var ownerProp = Expression.Property(param, nameof(IProtectable.CreateUserId));
        var permProp = Expression.Property(param, nameof(IProtectable.PermissionType));

        var isOwner = Expression.Equal(ownerProp, Expression.Constant(currentUserId));
        var permPropInt = Expression.Convert(permProp, typeof(int));
        var notPrivate = Expression.NotEqual(permPropInt, Expression.Constant((int)PermissionType.PrivateSelf, typeof(int)));
        var or = Expression.OrElse(isOwner, notPrivate);
        var lambda = Expression.Lambda<Func<T, bool>>(or, param);
        return query.Where(lambda);
    }

    /// <summary>判断当前用户对该实体是否可编辑。</summary>
    public static bool CanEdit(long currentUserId, IProtectable entity) =>
        currentUserId == entity.CreateUserId ||
        entity.PermissionType is PermissionType.Public;
}
