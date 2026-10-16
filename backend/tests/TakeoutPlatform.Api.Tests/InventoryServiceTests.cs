using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Inventory;
using TakeoutPlatform.Api.Features.Menu;
using TakeoutPlatform.Api.Features.Merchant;
using TakeoutPlatform.Api.Features.Order;
using TakeoutPlatform.Api.Features.Cart;

namespace TakeoutPlatform.Api.Tests;

public class InventoryServiceTests : ServiceTestBase
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-05T12:00:00Z");
    private static InventoryService Service(AppDbContext db) => new(db, new MenuTestClock(Now));

    private async Task<(int OrderId, int DishId)> Seed(int stock = 5, int merchant = 1)
    {
        await using var db = CreateContext();
        var order = new Order { Price = 10, OrderTimestamp = Now.UtcDateTime, AddressId = 1 };
        var dish = new Dish { Name = "Meal", Price = 10, Inventory = stock, MerchantId = merchant };
        db.AddRange(order, dish);
        await db.SaveChangesAsync();
        return (order.Id, dish.Id);
    }

    [Test]
    public async Task Reservation_aggregates_duplicates_and_identical_replay_does_not_subtract_again()
    {
        var (order, dish) = await Seed();
        await using var db = CreateContext();
        var first = await Service(db).ReserveAsync(order, [new(1, dish, 1), new(1, dish, 2)], Ct);
        var replay = await Service(db).ReserveAsync(order, [new(1, dish, 3)], Ct);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(first.Outcome, Is.EqualTo(InventoryOutcome.Success));
            Assert.That(replay.Outcome, Is.EqualTo(InventoryOutcome.AlreadyReserved));
            Assert.That(db.Dishes.AsNoTracking().Single().Inventory, Is.EqualTo(2));
            Assert.That(db.InventoryReservations.AsNoTracking().Single().Quantity, Is.EqualTo(3));
        }));
    }

    [Test]
    public async Task Changed_replay_is_rejected_without_changing_stock()
    {
        var (order, dish) = await Seed();
        await using var db = CreateContext();
        await Service(db).ReserveAsync(order, [new(1, dish, 2)], Ct);
        Assert.That((await Service(db).ReserveAsync(order, [new(1, dish, 3)], Ct)).Outcome,
            Is.EqualTo(InventoryOutcome.ReservationConflict));
        Assert.That(db.Dishes.AsNoTracking().Single().Inventory, Is.EqualTo(3));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public async Task Non_positive_quantities_are_rejected(int quantity)
    {
        var (order, dish) = await Seed();
        await using var db = CreateContext();
        Assert.That((await Service(db).ReserveAsync(order, [new(1, dish, quantity)], Ct)).Outcome,
            Is.EqualTo(InventoryOutcome.InvalidRequest));
        Assert.That(db.Dishes.Single().Inventory, Is.EqualTo(5));
    }

    [Test]
    public async Task Quantity_overflow_is_rejected()
    {
        var (order, dish) = await Seed();
        await using var db = CreateContext();
        Assert.That((await Service(db).ReserveAsync(order, [new(1, dish, int.MaxValue), new(1, dish, 1)], Ct)).Outcome,
            Is.EqualTo(InventoryOutcome.InvalidRequest));
    }

    [Test]
    public async Task Missing_order_dish_and_wrong_merchant_cannot_reserve()
    {
        var (order, dish) = await Seed();
        await using var db = CreateContext();
        Assert.That((await Service(db).ReserveAsync(999, [new(1, dish, 1)], Ct)).Outcome, Is.EqualTo(InventoryOutcome.OrderNotFound));
        Assert.That((await Service(db).ReserveAsync(order, [new(1, 999, 1)], Ct)).Outcome, Is.EqualTo(InventoryOutcome.DishNotFound));
        Assert.That((await Service(db).ReserveAsync(order, [new(2, dish, 1)], Ct)).Outcome, Is.EqualTo(InventoryOutcome.DishNotFound));
    }

    [Test]
    public async Task Insufficient_stock_rolls_back_earlier_items_and_the_ledger()
    {
        var (order, first) = await Seed();
        var (_, second) = await Seed(1);
        await using var db = CreateContext();
        var result = await Service(db).ReserveAsync(order, [new(1, first, 2), new(1, second, 2)], Ct);
        Assert.That(result.Outcome, Is.EqualTo(InventoryOutcome.InsufficientStock));
        Assert.That(db.Dishes.AsNoTracking().Single(d => d.Id == first).Inventory, Is.EqualTo(5));
        Assert.That(db.InventoryReservations.Count(), Is.Zero);
    }

    [Test]
    public async Task Release_restores_exactly_once_and_released_order_cannot_reserve_again()
    {
        var (order, dish) = await Seed();
        await using var db = CreateContext();
        await Service(db).ReserveAsync(order, [new(1, dish, 2)], Ct);
        Assert.That((await Service(db).ReleaseAsync(order, null, Ct)).Outcome, Is.EqualTo(InventoryOutcome.Success));
        Assert.That((await Service(db).ReleaseAsync(order, null, Ct)).Outcome, Is.EqualTo(InventoryOutcome.AlreadyReleased));
        Assert.That((await Service(db).ReserveAsync(order, [new(1, dish, 2)], Ct)).Outcome, Is.EqualTo(InventoryOutcome.ReservationConflict));
        Assert.That(db.Dishes.AsNoTracking().Single().Inventory, Is.EqualTo(5));
        Assert.That(db.InventoryReservations.AsNoTracking().Single().ReleasedAtUtc, Is.Not.Null);
    }

    [Test]
    public async Task Merchant_release_only_restores_its_own_items()
    {
        var (order, first) = await Seed();
        var (_, second) = await Seed(5, 2);
        await using var db = CreateContext();
        await Service(db).ReserveAsync(order, [new(1, first, 2), new(2, second, 3)], Ct);
        await Service(db).ReleaseAsync(order, 1, Ct);
        Assert.That(db.Dishes.AsNoTracking().Single(d => d.Id == first).Inventory, Is.EqualTo(5));
        Assert.That(db.Dishes.AsNoTracking().Single(d => d.Id == second).Inventory, Is.EqualTo(2));
        await Service(db).ReleaseAsync(order, null, Ct);
        Assert.That(db.Dishes.AsNoTracking().Single(d => d.Id == first).Inventory, Is.EqualTo(5));
        Assert.That(db.Dishes.AsNoTracking().Single(d => d.Id == second).Inventory, Is.EqualTo(5));
    }

    [Test]
    public async Task Release_without_a_reservation_is_a_no_op()
    {
        var (order, _) = await Seed();
        await using var db = CreateContext();
        Assert.That((await Service(db).ReleaseAsync(order, null, Ct)).Outcome, Is.EqualTo(InventoryOutcome.ReservationNotFound));
        Assert.That(db.Dishes.Single().Inventory, Is.EqualTo(5));
    }

    [Test]
    public async Task Expired_promotion_is_unavailable_to_menu_cart_and_reservation()
    {
        var (order, dishId) = await Seed();
        await using var db = CreateContext();
        var dish = await db.Dishes.SingleAsync();
        dish.ItemType = MenuItemType.Promotional;
        dish.PromotionStartsAtUtc = Now.AddDays(-2);
        dish.PromotionEndsAtUtc = Now.AddDays(-1);
        await db.SaveChangesAsync();
        var clock = new MenuTestClock(Now);
        Assert.That((await new DishService(db, clock).ListAsync(1, Ct))!.Single().IsAvailable, Is.False);
        Assert.That((await new CartService(db, timeProvider: clock).AddAsync(1, new() { DishId = dishId, DishNum = 1 }, Ct)).Outcome,
            Is.EqualTo(CartWriteOutcome.Unavailable));
        Assert.That((await Service(db).ReserveAsync(order, [new(1, dishId, 1)], Ct)).Outcome, Is.EqualTo(InventoryOutcome.Unavailable));
        Assert.That(db.Dishes.AsNoTracking().Single().Inventory, Is.EqualTo(5));
    }

    [Test]
    public async Task Order_create_and_cancel_use_the_ledger_without_double_restoration()
    {
        var (_, dish) = await Seed();
        await using var db = CreateContext();
        var orders = new OrderService(db, Service(db));
        var result = await orders.CreateAsync(new()
        {
            UserId = 1, AddressId = 1, Price = 20,
            ShoppingCart = [new() { MerchantId = 1, DishId = dish, DishNum = 2 }],
        }, Ct);
        Assert.That(result.Outcome, Is.EqualTo(CreateOrderOutcome.Success));
        Assert.That(db.InventoryReservations.AsNoTracking().Single().Quantity, Is.EqualTo(2));
        Assert.That((await orders.CancelAsync(result.Order!.Id, 1, Ct)).Outcome, Is.EqualTo(CancelOrderOutcome.Success));
        Assert.That((await orders.CancelAsync(result.Order.Id, 1, Ct)).Outcome, Is.EqualTo(CancelOrderOutcome.NotFound));
        Assert.That(db.Dishes.AsNoTracking().Single().Inventory, Is.EqualTo(5));
    }

    [Test]
    public async Task Merchant_rejection_uses_ledger_and_does_not_restore_a_previous_release_twice()
    {
        var (orderId, dishId) = await Seed();
        await using var db = CreateContext();
        db.OrderUsers.Add(new() { OrderId = orderId, UserId = 1 });
        db.OrderDishes.Add(new() { OrderId = orderId, MerchantId = 1, DishId = dishId, DishNum = 2 });
        (await db.Orders.SingleAsync()).Status = OrderStatus.Paid;
        await db.SaveChangesAsync();
        await Service(db).ReserveAsync(orderId, [new(1, dishId, 2)], Ct);
        await Service(db).ReleaseAsync(orderId, 1, Ct);
        var merchant = new MerchantFulfillmentService(db, new MenuTestClock(Now), Service(db));
        var result = await merchant.TransitionAsync(orderId, 1, new() { Action = "Reject", Reason = "Unavailable" }, Ct);
        Assert.That(result.Outcome, Is.EqualTo(MerchantTransitionOutcome.Success));
        Assert.That(db.Dishes.AsNoTracking().Single().Inventory, Is.EqualTo(5));
        Assert.That(db.MerchantOrderFulfillments.Single().InventoryRestored, Is.True);
    }

    [Test]
    public async Task Database_constraint_prevents_negative_inventory()
    {
        await Seed();
        await using var db = CreateContext();
        Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>((Func<Task>)(() => db.Dishes.ExecuteUpdateAsync(
            setter => setter.SetProperty(dish => dish.Inventory, -1))));
    }

    [Test]
    public async Task Menu_item_with_reservation_history_cannot_be_deleted()
    {
        var (order, dish) = await Seed();
        await using var db = CreateContext();
        await Service(db).ReserveAsync(order, [new(1, dish, 1)], Ct);
        Assert.That(await new DishService(db).DeleteAsync(dish, 1, Ct), Is.False);
        await Service(db).ReleaseAsync(order, null, Ct);
        Assert.That(await new DishService(db).DeleteAsync(dish, 1, Ct), Is.False);
    }

    [Test]
    public async Task Restoration_overflow_rolls_back_the_release_flag()
    {
        var (order, dish) = await Seed();
        await using var db = CreateContext();
        await Service(db).ReserveAsync(order, [new(1, dish, 2)], Ct);
        await db.Dishes.ExecuteUpdateAsync(setter => setter.SetProperty(item => item.Inventory, int.MaxValue));
        Assert.That((await Service(db).ReleaseAsync(order, null, Ct)).Succeeded, Is.False);
        Assert.That(db.InventoryReservations.AsNoTracking().Single().IsReleased, Is.False);
        Assert.That(db.Dishes.AsNoTracking().Single().Inventory, Is.EqualTo(int.MaxValue));
    }
}
