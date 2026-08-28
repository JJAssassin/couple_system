namespace CoupleLoveSystem.Domain.Entities;

/// <summary>
/// 标注在实体类上，声明「该实体发生变更后应通过 SignalR 广播给对应情侣组的模块名」。
/// 配合 <see cref="CoupleLoveSystem.Infrastructure.Persistence.BroadcastSaveChangesInterceptor"/>，
/// 在 SaveChanges 提交后自动按变更实体广播，免去在各 Service 中手写 NotifyAsync 的重复与遗漏。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class BroadcastAttribute : Attribute
{
    /// <summary>模块名（与前端 onSync(module, ...) 订阅的模块一致，如 "album" / "diary"）。</summary>
    public string Module { get; }

    public BroadcastAttribute(string module) => Module = module;
}
