using CoupleLoveSystem.Core.Enums;

namespace CoupleLoveSystem.Core.Entities;

/// <summary>带业务权限的实体实现此接口，供 PermissionFilter 统一兜底过滤。</summary>
public interface IProtectable
{
    long CreateUserId { get; }
    PermissionType PermissionType { get; }
}
