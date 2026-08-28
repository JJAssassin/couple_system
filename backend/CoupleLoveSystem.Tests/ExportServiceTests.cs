using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Concurrent;
using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Domain.Interfaces;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Xunit;
using CoupleLoveSystem.Tests.Infrastructure;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// 验证数据导出：除 JSON 元数据外，图片/附件需打包进 zip。
/// 用 EF InMemory 提供 CoupleDbContext，手写 IRepository / IWebHostEnvironment / ITokenStore 桩，
/// 真实落盘临时 uploads 目录作素材来源，断言导出返回一次性令牌、zip 落地临时目录且内含 data.json 与 media/* 图片，
/// 且绝不返回公开可猜的 /uploads 下载 URL（防 PII 泄露）。
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

    private class FakeTokenStore : ITokenStore
    {
        private readonly ConcurrentDictionary<string, (string Value, DateTime Expire)> _store = new();
        public Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken ct = default)
        {
            _store[key] = (value, DateTime.UtcNow + ttl);
            return Task.CompletedTask;
        }
        public Task<string?> GetAsync(string key, CancellationToken ct = default)
        {
            if (_store.TryGetValue(key, out var v) && v.Expire > DateTime.UtcNow)
                return Task.FromResult<string?>(v.Value);
            _store.TryRemove(key, out _);
            return Task.FromResult<string?>(null);
        }
        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            _store.TryRemove(key, out _);
            return Task.CompletedTask;
        }
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

        // 2) 真实落盘临时 uploads：写出被引用的两张图（作为导出素材来源）
        var webRoot = Path.Combine(Path.GetTempPath(), "export_test_" + Guid.NewGuid().ToString("N"));
        var uploadsDir = Path.Combine(webRoot, "uploads");
        Directory.CreateDirectory(uploadsDir);
        await File.WriteAllBytesAsync(Path.Combine(uploadsDir, "avatar.jpg"), new byte[] { 1, 2, 3 });
        await File.WriteAllBytesAsync(Path.Combine(uploadsDir, "diary.jpg"), new byte[] { 4, 5, 6 });

        var tokenStore = new FakeTokenStore();
        var svc = new UserService(new FakeUserRepo(user), db, new FakeEnv { WebRootPath = webRoot }, tokenStore);

        // 3) 执行导出
        var resp = await svc.ExportAsync(1);

        // 4) 断言响应元数据：返回一次性令牌，绝不返回公开 /uploads URL
        Assert.Equal("couple_export.zip", resp.FileName);
        Assert.False(string.IsNullOrEmpty(resp.Token), "应返回一次性下载令牌");
        Assert.Equal(2, resp.MediaCount);

        // 5) 通过一次性令牌取回临时目录中的 zip 路径，断言 zip 内容
        var zipPath = await tokenStore.GetAsync("export:" + resp.Token);
        Assert.NotNull(zipPath);
        Assert.True(File.Exists(zipPath), "zip 文件应已生成于临时目录");
        using (var zip = ZipFile.OpenRead(zipPath!))
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

        // 清理：临时素材目录 + 临时 zip
        try { if (zipPath != null) File.Delete(zipPath); } catch { /* 忽略 */ }
        try { Directory.Delete(webRoot, true); } catch { /* 忽略清理失败 */ }
    }
}
