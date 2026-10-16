using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Auth;
using TakeoutPlatform.Api.Features.Seed;

namespace TakeoutPlatform.Api.Tests;

/// <summary>The demo data created for staging: idempotent, opt-in and never in Production.</summary>
public class StagingSeederTests
{
    private const string Password = "Seed-Password-1";
    private SqliteConnection _connection = null!;

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        db.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown() => _connection.Dispose();

    private ServiceProvider Services(bool enabled, string? password, string environment = "Staging")
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        services.AddScoped<IPasswordHasher<Account>, PasswordHasher<Account>>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IHostEnvironment>(new TestEnvironment(environment));
        services.AddSingleton(Options.Create(new SeedOptions { Enabled = enabled, Password = password }));
        return services.BuildServiceProvider();
    }

    [Test]
    public async Task Creates_a_login_for_each_role_that_uses_the_configured_password()
    {
        await using var provider = Services(enabled: true, Password);
        await StagingSeeder.RunAsync(provider);

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Account>>();
        var accounts = await db.Accounts.ToListAsync();

        Assert.That(accounts.Select(a => a.Role), Does.Contain(AccountRole.Customer));
        Assert.That(accounts.Select(a => a.Role), Does.Contain(AccountRole.Merchant));
        Assert.That(accounts.Select(a => a.Role), Does.Contain(AccountRole.Rider));
        Assert.That(accounts.Single(a => a.Username == StagingSeeder.MerchantUsername).ProfileId, Is.EqualTo(StagingSeeder.DemoMerchantId));
        Assert.That(accounts.Single(a => a.Username == StagingSeeder.SecondMerchantUsername).ProfileId, Is.EqualTo(StagingSeeder.SecondMerchantId));
        Assert.That(accounts.Single(a => a.Role == AccountRole.Rider).ProfileId, Is.EqualTo(StagingSeeder.DemoRiderId));
        Assert.That(accounts.All(a => hasher.VerifyHashedPassword(a, a.PasswordHash, Password) != PasswordVerificationResult.Failed));
        Assert.That(accounts.All(a => a.PasswordHash != Password), "passwords must be stored hashed");
    }

    [Test]
    public async Task Renames_a_legacy_second_merchant_account_to_the_documented_demo_login()
    {
        await using var provider = Services(enabled: true, Password);
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Account>>();
            var legacy = new Account
            {
                Username = "cartdemo-salad",
                NormalizedUsername = "CARTDEMO-SALAD",
                Role = AccountRole.Merchant,
                ProfileId = StagingSeeder.SecondMerchantId,
                CreatedAtUtc = DateTime.UtcNow,
                IsActive = true,
            };
            legacy.PasswordHash = hasher.HashPassword(legacy, "legacy-password");
            db.Accounts.Add(legacy);
            await db.SaveChangesAsync();
        }

        await StagingSeeder.RunAsync(provider);

        using var verifyScope = provider.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var secondMerchant = await verifyDb.Accounts.SingleAsync(
            account => account.Role == AccountRole.Merchant
                && account.ProfileId == StagingSeeder.SecondMerchantId);
        Assert.That(secondMerchant.Username, Is.EqualTo(StagingSeeder.SecondMerchantUsername));
        Assert.That(secondMerchant.NormalizedUsername, Is.EqualTo("SPRINT3MERCHANT"));
    }

    [Test]
    public async Task Gives_the_demo_merchant_a_menu_and_an_offer()
    {
        await using var provider = Services(enabled: true, Password);
        await StagingSeeder.RunAsync(provider);

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.That(await db.Dishes.CountAsync(d => d.MerchantId == StagingSeeder.DemoMerchantId), Is.GreaterThan(0));
        Assert.That(await db.Dishes.CountAsync(d => d.MerchantId == StagingSeeder.SecondMerchantId), Is.GreaterThan(0));
        Assert.That(await db.Dishes.AllAsync(d => d.Inventory > 100));
        Assert.That(await db.SpecialOffers.CountAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task Running_twice_does_not_duplicate_anything()
    {
        await using var provider = Services(enabled: true, Password);
        await StagingSeeder.RunAsync(provider);
        await StagingSeeder.RunAsync(provider);

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.That(await db.Accounts.CountAsync(), Is.EqualTo(5));
        Assert.That(await db.Users.CountAsync(u => u.UserName == "Demo Customer"), Is.EqualTo(1));
        Assert.That(await db.SpecialOffers.CountAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task Does_nothing_unless_it_is_enabled()
    {
        await using var provider = Services(enabled: false, password: null);
        await StagingSeeder.RunAsync(provider);

        using var scope = provider.CreateScope();
        Assert.That(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Accounts.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public void Refuses_to_run_in_production()
    {
        using var provider = Services(enabled: true, Password, environment: "Production");

        Assert.ThrowsAsync<InvalidOperationException>((Func<Task>)(() => StagingSeeder.RunAsync(provider)));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("short")]
    public void Requires_a_real_password(string? password)
    {
        using var provider = Services(enabled: true, password);

        Assert.ThrowsAsync<InvalidOperationException>((Func<Task>)(() => StagingSeeder.RunAsync(provider)));
    }

    private sealed class TestEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
