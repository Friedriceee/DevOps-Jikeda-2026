using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;
using NUnit.Framework;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Auth;
using TakeoutPlatform.Api.Features.Seed;

namespace TakeoutPlatform.Api.Tests;

/// <summary>
/// What a brand-new staging environment does on its first start: apply every migration to an
/// empty PostgreSQL database, then run the demo seeder. SQLite tests build the schema from
/// the model and therefore cannot catch a migration that is wrong, missing or never discovered.
/// Set JIKEDA_TEST_POSTGRES to a localhost admin connection string to run these.
/// </summary>
[Category("PostgreSQL")]
public class FreshDatabasePostgresTests
{
    private const string Password = "Fresh-Db-Password-1";
    private string? _admin;
    private string? _connection;
    private string? _database;
    private string? _adminHashAfterMigrating;

    private AppDbContext Context() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_connection).Options);

    [OneTimeSetUp]
    public async Task Create_and_migrate_an_empty_database()
    {
        _admin = Environment.GetEnvironmentVariable("JIKEDA_TEST_POSTGRES");
        if (string.IsNullOrEmpty(_admin))
            Assert.Ignore("Set JIKEDA_TEST_POSTGRES to run the fresh-database tests.");

        var builder = new NpgsqlConnectionStringBuilder(_admin);
        if (builder.Host is not ("127.0.0.1" or "localhost"))
            throw new InvalidOperationException("Only disposable databases on localhost may be created.");

        _database = "jikeda_fresh_test_" + Guid.NewGuid().ToString("N");
        await using (var admin = new NpgsqlConnection(_admin))
        {
            await admin.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{_database}\"", admin);
            await command.ExecuteNonQueryAsync();
        }

        builder.Database = _database;
        _connection = builder.ConnectionString;
        await using var db = Context();
        await db.Database.MigrateAsync();
        // Captured before any test (such as the seeder tests) can give the admin a password.
        _adminHashAfterMigrating = await db.Accounts.AsNoTracking()
            .Where(account => account.Id == 1000).Select(account => account.PasswordHash).SingleAsync();
    }

    [OneTimeTearDown]
    public async Task Drop_the_database()
    {
        if (_connection is null || _database is null) return;
        NpgsqlConnection.ClearAllPools();
        await using var admin = new NpgsqlConnection(_admin);
        await admin.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)", admin);
        await command.ExecuteNonQueryAsync();
    }

    [Test]
    public async Task Every_migration_in_the_assembly_has_been_applied()
    {
        await using var db = Context();

        var discovered = db.Database.GetMigrations().ToList();
        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();

        Assert.That(applied, Is.EquivalentTo(discovered));
        Assert.That(await db.Database.GetPendingMigrationsAsync(), Is.Empty);
    }

    [Test]
    public async Task Both_demo_merchants_exist_after_migrating_from_scratch()
    {
        await using var db = Context();

        var ids = await db.Merchants.Select(merchant => merchant.Id).ToListAsync();

        Assert.That(ids, Is.SupersetOf(new[] { 1, 2 }));
    }

    [Test]
    public async Task The_staging_seeder_succeeds_on_a_freshly_migrated_database_and_is_repeatable()
    {
        await using var provider = SeedServices();

        await StagingSeeder.RunAsync(provider);
        await StagingSeeder.RunAsync(provider); // a restart must not fail or duplicate anything

        await using var db = Context();
        var usernames = await db.Accounts.Select(account => account.Username).ToListAsync();
        Assert.That(usernames, Is.SupersetOf(new[]
        {
            StagingSeeder.CustomerUsername, StagingSeeder.MerchantUsername, StagingSeeder.SecondMerchantUsername,
            StagingSeeder.RiderUsername, StagingSeeder.AdministratorUsername,
        }));
        Assert.That(await db.Dishes.CountAsync(dish => dish.MerchantId == StagingSeeder.DemoMerchantId), Is.GreaterThan(0));
        Assert.That(await db.Dishes.CountAsync(dish => dish.MerchantId == StagingSeeder.SecondMerchantId), Is.GreaterThan(0));
        Assert.That(await db.Accounts.CountAsync(account => account.NormalizedUsername == "ADMIN"), Is.EqualTo(1));
    }

    [Test]
    public async Task Every_seeded_demo_account_can_sign_in_with_the_seed_password()
    {
        await using var provider = SeedServices();
        await StagingSeeder.RunAsync(provider);

        await using var scope = provider.CreateAsyncScope();
        var auth = scope.ServiceProvider.GetRequiredService<AuthService>();
        foreach (var username in new[]
        {
            StagingSeeder.CustomerUsername, StagingSeeder.MerchantUsername, StagingSeeder.SecondMerchantUsername,
            StagingSeeder.RiderUsername, StagingSeeder.AdministratorUsername,
        })
        {
            var login = await auth.LoginAsync(new LoginRequest { Username = username, Password = Password }, default);
            Assert.That(login, Is.Not.Null, $"{username} could not sign in");
        }
    }

    [Test]
    public async Task The_built_in_admin_has_no_usable_password_on_a_fresh_database()
    {
        Assert.That(_adminHashAfterMigrating, Is.EqualTo(PasswordHashes.Unusable));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Migrating_an_existing_database_replaces_the_old_committed_admin_hash_only()
    {
        const string oldCommittedHash =
            "AQAAAAIAAYagAAAAEP8/kHbqZOLjY1r3lOeyHus9BtbVjlwlQHFjJ4AvAWRSQwPvAllNkaLJ9XNK+nk6kA==";
        await using var db = Context();
        var migrator = db.GetService<IMigrator>();
        var previous = "20261016105812_AddCustomerCouponAllocations";

        // 1. an old database: roll back before the fix and put the committed credential back
        await migrator.MigrateAsync(previous);
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE \"Accounts\" SET \"PasswordHash\" = {0} WHERE \"Id\" = 1000", oldCommittedHash);
        await migrator.MigrateAsync();
        Assert.That(
            (await db.Accounts.AsNoTracking().SingleAsync(a => a.Id == 1000)).PasswordHash,
            Is.EqualTo(PasswordHashes.Unusable), "the committed credential must be disabled");

        // 2. a database whose administrator already chose a real password keeps it
        await migrator.MigrateAsync(previous);
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE \"Accounts\" SET \"PasswordHash\" = 'chosen-by-an-operator' WHERE \"Id\" = 1000");
        await migrator.MigrateAsync();
        Assert.That(
            (await db.Accounts.AsNoTracking().SingleAsync(a => a.Id == 1000)).PasswordHash,
            Is.EqualTo("chosen-by-an-operator"));
    }

    private ServiceProvider SeedServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(_connection));
        services.AddScoped<IPasswordHasher<Account>, PasswordHasher<Account>>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IHostEnvironment>(new StagingEnvironment());
        services.AddSingleton(Options.Create(new SeedOptions { Enabled = true, Password = Password }));
        // test-only signing key for an in-memory host; it protects nothing
        services.AddSingleton(Options.Create(new JwtOptions { Key = new string('k', 48) }));
        services.AddScoped<AuthService>();
        return services.BuildServiceProvider();
    }

    private sealed class StagingEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Staging";
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
