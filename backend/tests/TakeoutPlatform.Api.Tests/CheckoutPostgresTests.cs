using Microsoft.EntityFrameworkCore;
using Npgsql;
using NUnit.Framework;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Cart;
using TakeoutPlatform.Api.Features.Checkout;
using TakeoutPlatform.Api.Features.Inventory;
using TakeoutPlatform.Api.Features.Merchant;
using TakeoutPlatform.Api.Features.Pricing;
using Microsoft.Extensions.Options;
using Customer = TakeoutPlatform.Api.Features.User.User;
using UserAddress = TakeoutPlatform.Api.Features.User.UserAddress;

namespace TakeoutPlatform.Api.Tests;

/// <summary>
/// Checkout against a real PostgreSQL server: the schema is built by running the
/// real EF migrations (so the Payments migration is exercised) and the concurrency
/// guarantees are checked with genuinely parallel transactions.
///
/// Set JIKEDA_TEST_POSTGRES to a localhost admin connection string to run them,
/// e.g. "Host=127.0.0.1;Port=5432;Username=takeout;Password=takeout;Database=postgres".
/// Each fixture creates and drops its own disposable database.
/// </summary>
[Category("PostgreSQL")]
public class CheckoutPostgresTests
{
    private string? _admin;
    private string? _connection;
    private string? _database;

    private AppDbContext Context() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_connection).Options);

    private static CheckoutService ServiceFor(AppDbContext db) => new(
        db,
        new PricingService(
            db,
            TimeProvider.System,
            [new MerchantDiscountStrategy(), new PlatformCouponDiscountStrategy()]),
        new InventoryService(db, TimeProvider.System),
        new SimulatedPaymentGateway(Options.Create(new PaymentOptions())),
        TimeProvider.System);

    [OneTimeSetUp]
    public async Task Create_isolated_database_from_migrations()
    {
        _admin = Environment.GetEnvironmentVariable("JIKEDA_TEST_POSTGRES");
        if (string.IsNullOrEmpty(_admin))
            Assert.Ignore("Set JIKEDA_TEST_POSTGRES to run the real PostgreSQL checkout tests.");

        var builder = new NpgsqlConnectionStringBuilder(_admin);
        if (builder.Host is not ("127.0.0.1" or "localhost"))
            throw new InvalidOperationException("Checkout tests only create disposable databases on localhost.");

        _database = "jikeda_checkout_test_" + Guid.NewGuid().ToString("N");
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
    }

    [OneTimeTearDown]
    public async Task Drop_only_the_database_created_by_this_fixture()
    {
        if (_connection is null || _database is null) return;
        NpgsqlConnection.ClearAllPools();
        await using var admin = new NpgsqlConnection(_admin);
        await admin.OpenAsync();
        await using var command = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)", admin);
        await command.ExecuteNonQueryAsync();
    }

    [Test]
    public async Task Migrations_create_the_payments_table_with_its_unique_indexes()
    {
        await using var db = Context();
        var indexes = await db.Database
            .SqlQueryRaw<string>("SELECT indexname AS \"Value\" FROM pg_indexes WHERE tablename = 'Payments'")
            .ToListAsync();

        Assert.That(indexes, Does.Contain("IX_Payments_OrderId"));
        Assert.That(indexes, Does.Contain("IX_Payments_UserId_IdempotencyKey"));
    }

    [Test]
    public async Task Parallel_identical_requests_create_one_order_and_one_payment()
    {
        var fixture = await Seed(customers: 1, stock: 20, quantity: 2);
        var customer = fixture.Customers[0];
        const string key = "parallel-same-key-0001";

        var results = await RunInParallel(8, _ => Checkout(customer, key, fixture.AddressOf(customer)));

        Assert.That(results.Select(r => r.Outcome).Distinct(),
            Is.SubsetOf(new[] { CheckoutOutcome.Success, CheckoutOutcome.Replayed }));
        Assert.That(results.Count(r => r.Outcome == CheckoutOutcome.Success), Is.EqualTo(1));
        Assert.That(results.Select(r => r.Response!.OrderId).Distinct().Count(), Is.EqualTo(1));

        await using var db = Context();
        Assert.That(await db.Payments.CountAsync(p => p.UserId == customer), Is.EqualTo(1));
        Assert.That(await OrdersOf(db, customer), Is.EqualTo(1));
        Assert.That(await db.InventoryReservations.CountAsync(r => r.DishId == fixture.DishId), Is.EqualTo(1));
        Assert.That(await StockOf(db, fixture.DishId), Is.EqualTo(18));
    }

    [Test]
    public async Task Parallel_requests_with_different_keys_for_one_cart_charge_only_once()
    {
        var fixture = await Seed(customers: 1, stock: 20, quantity: 2);
        var customer = fixture.Customers[0];

        var results = await RunInParallel(8, index =>
            Checkout(customer, $"parallel-diff-key-{index:D4}", fixture.AddressOf(customer)));

        Assert.That(results.Count(r => r.Outcome == CheckoutOutcome.Success), Is.EqualTo(1));
        Assert.That(
            results.Where(r => !r.Succeeded).Select(r => r.Outcome).Distinct(),
            Is.SubsetOf(new[] { CheckoutOutcome.CartChanged, CheckoutOutcome.PricingFailed }));

        await using var db = Context();
        Assert.That(await db.Payments.CountAsync(p => p.UserId == customer), Is.EqualTo(1));
        Assert.That(await OrdersOf(db, customer), Is.EqualTo(1));
        Assert.That(await StockOf(db, fixture.DishId), Is.EqualTo(18));
    }

    [Test]
    public async Task Ten_customers_competing_for_three_units_cannot_oversell()
    {
        var fixture = await Seed(customers: 10, stock: 3, quantity: 1);

        var results = await RunInParallel(10, index =>
            Checkout(fixture.Customers[index], $"compete-key-{index:D6}", fixture.AddressOf(fixture.Customers[index])));

        Assert.That(results.Count(r => r.Outcome == CheckoutOutcome.Success), Is.EqualTo(3));
        Assert.That(results.Count(r => r.Outcome == CheckoutOutcome.InsufficientStock), Is.EqualTo(7));

        await using var db = Context();
        Assert.That(await StockOf(db, fixture.DishId), Is.Zero);
        Assert.That(await db.Payments.CountAsync(p => fixture.Customers.Contains(p.UserId)), Is.EqualTo(3));
        Assert.That(await db.InventoryReservations.CountAsync(r => r.DishId == fixture.DishId), Is.EqualTo(3));
    }

    [Test]
    public async Task A_declined_payment_leaves_stock_cart_and_orders_untouched()
    {
        var fixture = await Seed(customers: 1, stock: 5, quantity: 2);
        var customer = fixture.Customers[0];

        await using (var db = Context())
        {
            var result = await ServiceFor(db).CheckoutAsync(
                customer,
                "declined-key-0001",
                new CheckoutRequest
                {
                    AddressId = fixture.AddressOf(customer),
                    PaymentMethod = SimulatedPaymentGateway.DeclineMethod,
                },
                CancellationToken.None);
            Assert.That(result.Outcome, Is.EqualTo(CheckoutOutcome.PaymentDeclined));
        }

        await using var check = Context();
        Assert.That(await StockOf(check, fixture.DishId), Is.EqualTo(5));
        Assert.That(await check.CartItems.CountAsync(i => i.UserId == customer), Is.EqualTo(1));
        Assert.That(await check.Payments.CountAsync(p => p.UserId == customer), Is.Zero);
        Assert.That(await OrdersOf(check, customer), Is.Zero);
    }

    // ---------- helpers ----------

    private sealed record Fixture(int DishId, int[] Customers, Dictionary<int, int> Addresses)
    {
        public int AddressOf(int customer) => Addresses[customer];
    }

    private async Task<Fixture> Seed(int customers, int stock, int quantity)
    {
        await using var db = Context();
        var dish = new Dish { MerchantId = 1, Name = "Parallel dish " + Guid.NewGuid().ToString("N")[..6], Price = 10m, Inventory = stock };
        var users = Enumerable.Range(0, customers)
            .Select(i => new Customer { UserName = "Concurrent customer " + i, PhoneNumber = "13800000000" })
            .ToList();
        db.Dishes.Add(dish);
        db.Users.AddRange(users);
        await db.SaveChangesAsync();

        var addresses = users
            .Select(user => new UserAddress
            {
                UserId = user.Id, Address = "1 Campus Road ZoneA", ContactName = "C", PhoneNumber = "13800000000",
            })
            .ToList();
        db.UserAddresses.AddRange(addresses);
        db.CartItems.AddRange(users.Select(user => new CartItem
        {
            UserId = user.Id, MerchantId = 1, DishId = dish.Id, DishNum = quantity,
        }));
        await db.SaveChangesAsync();

        return new Fixture(
            dish.Id,
            users.Select(user => user.Id).ToArray(),
            addresses.ToDictionary(address => address.UserId, address => address.Id));
    }

    private async Task<CheckoutResult> Checkout(int customer, string key, int addressId)
    {
        await using var db = Context();
        return await ServiceFor(db).CheckoutAsync(
            customer, key, new CheckoutRequest { AddressId = addressId }, CancellationToken.None);
    }

    /// <summary>Starts every task behind a barrier so the transactions genuinely overlap.</summary>
    private static async Task<CheckoutResult[]> RunInParallel(int count, Func<int, Task<CheckoutResult>> work)
    {
        using var gate = new ManualResetEventSlim(false);
        var tasks = Enumerable.Range(0, count)
            .Select(index => Task.Run(() =>
            {
                gate.Wait();
                return work(index);
            }))
            .ToArray();
        gate.Set();
        return await Task.WhenAll(tasks);
    }

    private static Task<int> StockOf(AppDbContext db, int dishId) =>
        db.Dishes.Where(dish => dish.Id == dishId).Select(dish => dish.Inventory).SingleAsync();

    private static Task<int> OrdersOf(AppDbContext db, int customer) =>
        db.OrderUsers.CountAsync(orderUser => orderUser.UserId == customer);
}
