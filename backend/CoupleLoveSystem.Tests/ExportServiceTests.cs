using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Xunit;
using CoupleLoveSystem.Tests.Infrastructure;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// 验证数据导出：除 JSON 元数据外，图片/附件需打包进 zip（落地 UserService 的 TODO）。
/// 用 EF InMemory 提供 CoupleDbContext，手写 IRepository / IWebHostEnvironment 桩，
/// 真实落盘临时 uploads 目录后断言 zip 内含 data.json 与 media/* 图片。
/// </summary>
public class ExportServiceTests
{
    private class FakeUserRepo : IRepository<CoupleUser>
    {
        private readonly CoupleUser _user;
        public FakeUserRepo(CoupleUser user) => _user = user;
        public IQueryable<CoupleUser> Query() => new[] { _user }.AsQueryable();
        public Task<CoupleUser?> GetByIdAsync(long id, CancellationToken ct = default) =>
            Task.FromResult(id == _user.Id ? _user : null);
        public Task<List<CoupleUser>> ListAsync(Expression<Func<CoupleUser, bool>>? p = null, CancellationToken ct = default) =>
            Task.FromResult(new List<CoupleUser> { _user });
        public Task AddAsync(CoupleUser e, CancellationToken ct = default) => Task.CompletedTask;
        public void Update(CoupleUser e) { }
        public void SoftDelete(CoupleUser e) { }
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
    }

    private class FakeEnv : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string EnvironmentName { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    [Fact]
    public async Task ExportAsync_Bundles_Referenced_Images_Into_Zip()
    {
        // 1) InMemory 数据：用户含头像图 + 日记正文内嵌配图
        var db = TestDb.CreateInMemoryContext();
        var user = new CoupleUser
        {
            Id = 1,
            UserName = "partner_a",
            NickName = "A",
            PasswordHash = AuthService.HashPassword("123456"),
            RoleType = RoleType.PartnerA,
            Avatar = "/uploads/avatar.jpg",
            IsDeleted = false
        };
        db.Users.Add(user);
        db.Diaries.Add(new CoupleDiary
        {
            Id = 10,
            Title = "today",
            Content = "今天拍了张照 /uploads/diary.jpg 留念",
            CreateUserId = 1,
            CoupleId = null
        });
        db.SaveChanges();

        // 2) 真实落盘临时 uploads：写出被引用的两张图
        var webRoot = Path.Combine(Path.GetTempPath(), "export_test_" + Guid.NewGuid().ToString("N"));
        var uploadsDir = Path.Combine(webRoot, "uploads");
        Directory.CreateDirectory(uploadsDir);
        await File.WriteAllBytesAsync(Path.Combine(uploadsDir, "avatar.jpg"), new byte[] { 1, 2, 3 });
        await File.WriteAllBytesAsync(Path.Combine(uploadsDir, "diary.jpg"), new byte[] { 4, 5, 6 });

        var svc = new UserService(new FakeUserRepo(user), db, new FakeEnv { WebRootPath = webRoot });

        // 3) 执行导出
        var resp = await svc.ExportAsync(1);

        // 4) 断言响应元数据
        Assert.Equal("couple_export.zip", resp.FileName);
        Assert.StartsWith("/uploads/", resp.DownloadUrl);
        Assert.Equal(2, resp.MediaCount);

        // 5) 断言 zip 内容：data.json + media/avatar.jpg + media/diary.jpg
        var zipName = resp.DownloadUrl["/uploads/".Length..];
        var zipPath = Path.Combine(uploadsDir, zipName);
        Assert.True(File.Exists(zipPath), "zip 文件应已生成");
        using (var zip = ZipFile.OpenRead(zipPath))
        {
            var names = zip.Entries.Select(e => e.FullName).ToHashSet();
            Assert.Contains("data.json", names);
            Assert.Contains("media/avatar.jpg", names);
            Assert.Contains("media/diary.jpg", names);
            // data.json 应含被引用的图片路径，便于离线还原
            using var sr = new StreamReader(zip.GetEntry("data.json")!.Open());
            var json = await sr.ReadToEndAsync();
            Assert.Contains("/uploads/avatar.jpg", json);
            Assert.Contains("/uploads/diary.jpg", json);
        }

        // 清理临时目录
        try { Directory.Delete(webRoot, true); } catch { /* 忽略清理失败 */ }
    }
}
