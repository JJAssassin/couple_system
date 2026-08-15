using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// 回归测试：全局查询过滤器合并修复。
/// 此前对同一实体类型调用两次 HasQueryFilter，EF 以「最后一次」为准，
/// 导致软删除(!IsDeleted) 被情侣过滤覆盖而失效（逻辑删除的内容仍可被查询到）。
/// 现合并为单条 !IsDeleted &amp;&amp; (CoupleId==Current || CoupleId==null)。
/// 验证：1) 软删除对情侣内容实体生效；2) 情侣隔离仍生效；3) null 逃逸仍保留（系统级/历史行）。
/// </summary>
public class CoupleFilterSoftDeleteTests
{
    [Fact]
    public void 软删除后查询不到_修复覆盖失效()
    {
        CoupleContext.Current = "CID-A";
        try
        {
            using var ctx = TestDb.CreateInMemoryContext();
            var diary = new CoupleDiary { Id = 1, CoupleId = "CID-A", IsDeleted = false };
            ctx.Diaries.Add(diary);
            ctx.SaveChanges();

            // 初始：未删除，可见
            Assert.Single(ctx.Diaries.ToList());

            // 逻辑删除
            diary.IsDeleted = true;
            ctx.SaveChanges();

            // 修复后：软删除内容不可见（此前因过滤器被覆盖而可见）
            Assert.Empty(ctx.Diaries.ToList());
        }
        finally
        {
            CoupleContext.Current = null;
        }
    }

    [Fact]
    public void 情侣隔离_仅返回当前情侣内容()
    {
        CoupleContext.Current = "CID-A";
        try
        {
            using var ctx = TestDb.CreateInMemoryContext();
            ctx.Diaries.Add(new CoupleDiary { Id = 1, CoupleId = "CID-A", IsDeleted = false });
            ctx.Diaries.Add(new CoupleDiary { Id = 2, CoupleId = "CID-B", IsDeleted = false });
            ctx.SaveChanges();

            var list = ctx.Diaries.ToList();
            Assert.Single(list);
            Assert.All(list, d => Assert.Equal("CID-A", d.CoupleId));
        }
        finally
        {
            CoupleContext.Current = null;
        }
    }

    [Fact]
    public void 软删除_跨情侣内容也不可见()
    {
        CoupleContext.Current = "CID-A";
        try
        {
            using var ctx = TestDb.CreateInMemoryContext();
            ctx.Diaries.Add(new CoupleDiary { Id = 1, CoupleId = "CID-A", IsDeleted = true });
            ctx.Diaries.Add(new CoupleDiary { Id = 2, CoupleId = "CID-B", IsDeleted = false });
            ctx.SaveChanges();

            // CID-A 软删除不可见；CID-B 因隔离也不可见 → 全空
            Assert.Empty(ctx.Diaries.ToList());
        }
        finally
        {
            CoupleContext.Current = null;
        }
    }

    [Fact]
    public void null逃逸_未盖章历史行仍可读()
    {
        // 模拟无 HTTP 上下文：Current 为 null，内容 CoupleId 留空
        CoupleContext.Current = null;
        try
        {
            using var ctx = TestDb.CreateInMemoryContext();
            ctx.Diaries.Add(new CoupleDiary { Id = 1, CoupleId = null, IsDeleted = false });
            ctx.SaveChanges();

            // null 逃逸：Current==null 且 CoupleId==null 时放行（系统级消息/历史行）
            Assert.Single(ctx.Diaries.ToList());
        }
        finally
        {
            CoupleContext.Current = null;
        }
    }
}
