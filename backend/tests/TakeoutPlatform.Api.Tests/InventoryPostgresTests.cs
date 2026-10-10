using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using NUnit.Framework;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Inventory;
using TakeoutPlatform.Api.Features.Merchant;
using TakeoutPlatform.Api.Features.Order;

namespace TakeoutPlatform.Api.Tests;

[Category("PostgreSQL")]
public class InventoryPostgresTests
{
    private string? _admin;
    private string? _connection;
    private string? _database;
    private string? _migrationDatabase;
    private AppDbContext Context() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_connection).Options);

    [OneTimeSetUp]
    public async Task Create_isolated_database()
    {
        _admin = Environment.GetEnvironmentVariable("JIKEDA_TEST_POSTGRES");
        if (string.IsNullOrEmpty(_admin)) Assert.Ignore("Set JIKEDA_TEST_POSTGRES to run real PostgreSQL inventory tests.");
        var builder = new NpgsqlConnectionStringBuilder(_admin);
        if (builder.Host is not ("127.0.0.1" or "localhost"))
            throw new InvalidOperationException("Inventory tests only create disposable databases on localhost.");
        _database = "jikeda_inventory_test_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(_admin);
        await admin.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{_database}\"", admin);
        await command.ExecuteNonQueryAsync();
        builder.Database = _database;
        _connection = builder.ConnectionString;
        await using var db = Context();
        await db.Database.EnsureCreatedAsync();
    }

    [OneTimeTearDown]
    public async Task Remove_only_the_database_created_by_this_fixture()
    {
        if (_connection is null || _database is null) return;
        NpgsqlConnection.ClearAllPools();
        await using var admin = new NpgsqlConnection(_admin);
        await admin.OpenAsync();
        foreach (var database in new[] { _database, _migrationDatabase }.Where(name => name is not null))
        {
            await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)", admin);
            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task<(int DishId, int[] OrderIds)> Seed(int stock, int orderCount)
    {
        await using var db = Context();
        var dish = new Dish { MerchantId = 1, Name = "Concurrent stock", Price = 10, Inventory = stock };
        var orders = Enumerable.Range(0, orderCount).Select(_ => new Order
        { Price = 10, AddressId = 1, OrderTimestamp = DateTime.UtcNow }).ToArray();
        db.Dishes.Add(dish);
        db.Orders.AddRange(orders);
        await db.SaveChangesAsync();
        return (dish.Id, orders.Select(order => order.Id).ToArray());
    }

    private async Task<InventoryResult> Reserve(int order, int dish, int qty = 1)
    {
        await using var db = Context();
        return await new InventoryService(db, TimeProvider.System).ReserveAsync(order, [new(1, dish, qty)], CancellationToken.None);
    }
    private async Task<InventoryResult> Release(int order)
    {
        await using var db = Context();
        return await new InventoryService(db, TimeProvider.System).ReleaseAsync(order, null, CancellationToken.None);
    }

    [Test]
    public async Task Ten_concurrent_orders_competing_for_three_units_cannot_oversell()
    {
        var (dish, orders) = await Seed(3, 10);
        var results = await Task.WhenAll(orders.Select(order => Reserve(order, dish)));
        await using var db = Context();
        Assert.That(results.Count(result => result.Outcome == InventoryOutcome.Success), Is.EqualTo(3));
        Assert.That(results.Count(result => result.Outcome == InventoryOutcome.InsufficientStock), Is.EqualTo(7));
        Assert.That((await db.Dishes.SingleAsync(item => item.Id == dish)).Inventory, Is.Zero);
        Assert.That(await db.InventoryReservations.CountAsync(row => row.DishId == dish), Is.EqualTo(3));
    }

    [Test]
    public async Task Concurrent_identical_replays_consume_stock_only_once()
    {
        var (dish, orders) = await Seed(1, 1);
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Reserve(orders[0], dish)));
        Assert.That(results.All(result => result.Succeeded), Is.True);
        Assert.That(results.Count(result => result.Outcome == InventoryOutcome.Success), Is.EqualTo(1));
        await using var db = Context();
        Assert.That((await db.Dishes.SingleAsync(item => item.Id == dish)).Inventory, Is.Zero);
        Assert.That(await db.InventoryReservations.CountAsync(row => row.DishId == dish), Is.EqualTo(1));
    }

    [Test]
    public async Task Concurrent_releases_restore_stock_only_once()
    {
        var (dish, orders) = await Seed(5, 1);
        await Reserve(orders[0], dish, 3);
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Release(orders[0])));
        Assert.That(results.All(result => result.Succeeded), Is.True);
        Assert.That(results.Count(result => result.Outcome == InventoryOutcome.Success), Is.EqualTo(1));
        await using var db = Context();
        Assert.That((await db.Dishes.SingleAsync(item => item.Id == dish)).Inventory, Is.EqualTo(5));
    }

    [Test]
    public async Task Concurrent_changed_replays_roll_back_the_losing_subtraction()
    {
        var (dish, orders) = await Seed(10, 1);
        var results = await Task.WhenAll(Reserve(orders[0], dish, 2), Reserve(orders[0], dish, 3));
        Assert.That(results.Count(result => result.Outcome == InventoryOutcome.Success), Is.EqualTo(1));
        Assert.That(results.Count(result => result.Outcome == InventoryOutcome.ReservationConflict), Is.EqualTo(1));
        await using var db = Context();
        var reservation = await db.InventoryReservations.SingleAsync(row => row.DishId == dish);
        Assert.That((await db.Dishes.SingleAsync(item => item.Id == dish)).Inventory, Is.EqualTo(10 - reservation.Quantity));
    }

    [Test]
    public async Task Menu_migration_up_down_up_preserves_existing_standard_items()
    {
        _migrationDatabase = "jikeda_inventory_test_" + Guid.NewGuid().ToString("N");
        await using (var admin = new NpgsqlConnection(_admin))
        {
            await admin.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{_migrationDatabase}\"", admin);
            await command.ExecuteNonQueryAsync();
        }
        var connection = new NpgsqlConnectionStringBuilder(_admin) { Database = _migrationDatabase }.ConnectionString;
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options);
        var migrator = db.GetService<IMigrator>();
        // Older handwritten migrations use 12-digit ids. Resolve their name
        // through EF's id generator rather than treating them as modern ids.
        var previous = db.GetService<IMigrationsIdGenerator>().GetName("202610100001_AddMerchantOrderFulfillment");
        const string current = "20261010090352_AddMenuFactoryAndInventory";
        await migrator.MigrateAsync(previous);
        await db.Database.ExecuteSqlRawAsync("INSERT INTO \"Dishes\" (\"MerchantId\", \"Name\", \"Price\", \"Inventory\") VALUES (1, 'Legacy meal', 10, 5)");
        await migrator.MigrateAsync(current);
        Assert.That((await db.Dishes.AsNoTracking().SingleAsync()).ItemType.ToString(), Is.EqualTo("Standard"));
        await migrator.MigrateAsync(previous);
        await migrator.MigrateAsync(current);
        var dish = await db.Dishes.AsNoTracking().SingleAsync();
        Assert.That(dish.Name, Is.EqualTo("Legacy meal"));
        Assert.That(dish.Inventory, Is.EqualTo(5));
        Assert.That(await db.InventoryReservations.CountAsync(), Is.Zero);
    }
}
