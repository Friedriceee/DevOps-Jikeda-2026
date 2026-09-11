using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Merchant;

namespace TakeoutPlatform.Api.Tests;

/// <summary>
/// Service 层单元测试的公共基类。
///
/// 与 ApiTestFactory 不同：这里不启动整个 Web 应用，只针对单个 Service 类做单元测试。
/// 每个测试用一个全新的 SQLite in-memory 数据库（借助独立连接实现隔离），
/// 保证测试之间互不影响，且不依赖真实的 PostgreSQL。
///
/// 预置的种子数据：
///   - 商家 Id=1（"Demo Merchant"，来自 AppDbContext.OnModelCreating 的 HasData）
///   - 商家 Id=2（"Second Merchant"，用于验证"跨商家隔离"的场景）
/// </summary>
public abstract class ServiceTestBase
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<AppDbContext> _options = null!;

    protected const int MerchantId = 1;
    protected const int OtherMerchantId = 2;
    protected const int MissingMerchantId = 999;

    [SetUp]
    public void SetUpDatabase()
    {
        // ":memory:" 数据库的生命周期与连接绑定，只要连接保持打开，数据库就存在。
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateContext();
        // 依据实体模型创建表结构（不走 EF 迁移，直接建表，适合测试）。
        context.Database.EnsureCreated();

        // 追加一个第二商家，用于测试"数据只属于对应商家"的边界。
        context.Merchants.Add(new Merchant { Id = OtherMerchantId, Name = "Second Merchant" });
        context.SaveChanges();
    }

    [TearDown]
    public void TearDownDatabase()
    {
        // 关闭连接即销毁内存数据库，确保下一个测试从干净状态开始。
        _connection.Dispose();
    }

    /// <summary>
    /// 每次调用都返回一个新的 AppDbContext，但共享同一个底层 SQLite 连接（即同一份数据）。
    /// 用"写入用一个 context、读取用另一个 context"的方式，能更真实地验证数据确实落库，
    /// 而不是只停留在 EF 的内存跟踪缓存里。
    /// </summary>
    protected AppDbContext CreateContext() => new(_options);
}
