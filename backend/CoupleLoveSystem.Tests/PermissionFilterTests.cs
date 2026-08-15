using CoupleLoveSystem.Application.Filters;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Enums;
using CoupleLoveSystem.Core.Result;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// 验证后端数据权限兜底逻辑（绝不信任前端 ID）：
/// 列表场景过滤不可见行；详情场景私密内容抛 ForbiddenException。
/// 权限矩阵：Public / ViewOnlyOther 双方可读；PrivateSelf 仅本人可见。
/// </summary>
public class PermissionFilterTests
{
    private static List<CoupleDiary> Seed() => new()
    {
        new CoupleDiary { Id = 1, CreateUserId = 1, PermissionType = PermissionType.Public },
        new CoupleDiary { Id = 2, CreateUserId = 2, PermissionType = PermissionType.PrivateSelf }, // user2 自己的私密
        new CoupleDiary { Id = 3, CreateUserId = 1, PermissionType = PermissionType.ViewOnlyOther },
        new CoupleDiary { Id = 4, CreateUserId = 1, PermissionType = PermissionType.PrivateSelf }, // user1 的私密，user2 不应见
    };

    [Fact]
    public void WhereVisible_本人可见自己全部_过滤掉对方私密()
    {
        // 当前用户 = 1：可见 id1(Public)、id3(ViewOnlyOther)、id4(自己的私密)；id2 是对方私密被过滤
        var q = PermissionFilter.WhereVisible(Seed().AsQueryable(), currentUserId: 1).ToList();
        Assert.Equal(3, q.Count);
        Assert.Contains(q, d => d.Id == 1);
        Assert.Contains(q, d => d.Id == 3);
        Assert.Contains(q, d => d.Id == 4);
        Assert.DoesNotContain(q, d => d.Id == 2);
    }

    [Fact]
    public void WhereVisible_对方视角看不到对方私密()
    {
        // 当前用户 = 2：可见 id1(Public)、id2(自己的私密)、id3(ViewOnlyOther)；id4(user1 私密)被过滤
        var q = PermissionFilter.WhereVisible(Seed().AsQueryable(), currentUserId: 2).ToList();
        Assert.Equal(3, q.Count);
        Assert.Contains(q, d => d.Id == 1);
        Assert.Contains(q, d => d.Id == 2);
        Assert.Contains(q, d => d.Id == 3);
        Assert.DoesNotContain(q, d => d.Id == 4);
    }

    [Fact]
    public void EnsureVisible_非本人访问私密_抛Forbidden()
    {
        var diary = Seed().First(d => d.Id == 4); // user1 的 PrivateSelf
        Assert.Throws<ForbiddenException>(() => PermissionFilter.EnsureVisible(currentUserId: 2, diary));
    }

    [Fact]
    public void EnsureVisible_本人访问私密_通过()
    {
        var diary = Seed().First(d => d.Id == 2); // user2 的 PrivateSelf
        PermissionFilter.EnsureVisible(currentUserId: 2, diary); // 不抛
    }

    [Fact]
    public void CanEdit_公开可双方编辑_私密仅本人()
    {
        var pub = Seed().First(d => d.Id == 1);  // Public, owner1
        var priv = Seed().First(d => d.Id == 2); // PrivateSelf, owner2
        Assert.True(PermissionFilter.CanEdit(1, pub));   // 本人可编辑
        Assert.False(PermissionFilter.CanEdit(1, priv)); // 非本人且私密 → 不可编辑
    }
}
