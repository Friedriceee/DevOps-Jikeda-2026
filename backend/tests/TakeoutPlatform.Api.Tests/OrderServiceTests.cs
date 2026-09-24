using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TakeoutPlatform.Api.Features.Merchant;
using TakeoutPlatform.Api.Features.Order;

namespace TakeoutPlatform.Api.Tests;

/// <summary>
/// OrderService 的单元测试，覆盖三类关注点：
///   1. 订单组装（Order Builder）：CreateAsync 组装五表、扣减库存、空车/库存不足/菜品不存在的处理。
///   2. 事务：任一步失败时整体回滚，库存不被错误扣减、五表不留脏数据。
///   3. 状态守卫：CancelAsync 仅允许取消待支付（Pending）订单，并恢复库存。
///
/// 逻辑参照旧项目 CreateOrder / deleteOrder。
/// 种子中已有 Merchant Id=1、User Id=1；菜品需在各测试里自行创建（带库存）。
/// </summary>
public class OrderServiceTests : ServiceTestBase
{
    private const int SeededUserId = 1;
    private const int SeededMerchantId = 1;

    /// <summary>在指定 context 里建一个带库存的菜品，返回其自增 Id。</summary>
    private static int SeedDish(
        Data.AppDbContext context,
        int inventory,
        decimal price = 10.00m,
        string name = "Test Dish",
        int merchantId = SeededMerchantId)
    {
        var dish = new Dish
        {
            MerchantId = merchantId,
            Name = name,
            Price = price,
            Inventory = inventory,
        };
        context.Dishes.Add(dish);
        context.SaveChanges();
        return dish.Id;
    }

    private static CreateOrderRequest NewOrderRequest(
        int userId,
        int addressId,
        params CreateOrderRequest.CartItem[] items) => new()
    {
        UserId = userId,
        AddressId = addressId,
        Price = 20.00m,
        OrderTimestamp = DateTime.UtcNow,
        NeedUtensils = 1,
        RiderPrice = 3.00m,
        ShoppingCart = items,
    };

    private static CreateOrderRequest.CartItem Item(int dishId, int dishNum, int merchantId = SeededMerchantId)
        => new() { MerchantId = merchantId, DishId = dishId, DishNum = dishNum };

    // ---------- 1. 订单组装（Order Builder）----------

    [Test]
    public async Task CreateAsync_builds_order_persists_five_tables_and_deducts_inventory()
    {
        int dishId;
        await using (var seed = CreateContext())
        {
            dishId = SeedDish(seed, inventory: 100);
        }

        await using var context = CreateContext();
        var service = new OrderService(context);

        var result = await service.CreateAsync(
            NewOrderRequest(SeededUserId, addressId: 1, Item(dishId, 3)),
            CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(CreateOrderOutcome.Success));
        Assert.That(result.Order, Is.Not.Null);
        Assert.That(result.Order!.Status, Is.EqualTo(OrderStatus.Pending));
        Assert.That(result.Order.Dishes, Has.Count.EqualTo(1));

        await using var verify = CreateContext();
        var orderId = result.Order.Id;
        // 五表：Order / OrderUser / OrderRider / OrderDish 应各有记录，OrderCoupon 无（未用券）。
        Assert.That(verify.Orders.Any(o => o.Id == orderId), Is.True);
        Assert.That(verify.OrderUsers.Any(ou => ou.OrderId == orderId && ou.UserId == SeededUserId), Is.True);
        Assert.That(verify.OrderRiders.Any(or => or.OrderId == orderId), Is.True);
        Assert.That(verify.OrderDishes.Count(od => od.OrderId == orderId), Is.EqualTo(1));
        Assert.That(verify.OrderCoupons.Any(oc => oc.OrderId == orderId), Is.False);
        // 库存被扣减 100 -> 97
        Assert.That(verify.Dishes.Single(d => d.Id == dishId).Inventory, Is.EqualTo(97));
    }

    [Test]
    public async Task CreateAsync_with_coupon_also_creates_order_coupon_row()
    {
        int dishId;
        await using (var seed = CreateContext())
        {
            dishId = SeedDish(seed, inventory: 10);
        }

        await using var context = CreateContext();
        var service = new OrderService(context);

        var request = NewOrderRequest(SeededUserId, addressId: 1, Item(dishId, 1));
        request.CouponId = 5;
        request.ExpirationDate = DateTime.UtcNow.AddDays(7);

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(CreateOrderOutcome.Success));

        await using var verify = CreateContext();
        Assert.That(verify.OrderCoupons.Any(oc => oc.OrderId == result.Order!.Id && oc.CouponId == 5), Is.True);
    }

    [Test]
    public async Task CreateAsync_with_empty_cart_returns_empty_cart_and_persists_nothing()
    {
        await using var context = CreateContext();
        var service = new OrderService(context);

        var result = await service.CreateAsync(
            NewOrderRequest(SeededUserId, addressId: 1),
            CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(CreateOrderOutcome.EmptyCart));

        await using var verify = CreateContext();
        Assert.That(verify.Orders.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task CreateAsync_with_missing_dish_returns_dish_not_found()
    {
        await using var context = CreateContext();
        var service = new OrderService(context);

        var result = await service.CreateAsync(
            NewOrderRequest(SeededUserId, addressId: 1, Item(dishId: 12345, dishNum: 1)),
            CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(CreateOrderOutcome.DishNotFound));
    }

    [Test]
    public async Task CreateAsync_with_insufficient_stock_returns_insufficient_stock()
    {
        int dishId;
        await using (var seed = CreateContext())
        {
            dishId = SeedDish(seed, inventory: 2);
        }

        await using var context = CreateContext();
        var service = new OrderService(context);

        var result = await service.CreateAsync(
            NewOrderRequest(SeededUserId, addressId: 1, Item(dishId, 5)),
            CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(CreateOrderOutcome.InsufficientStock));
    }

    // ---------- 2. 事务：中途失败整体回滚 ----------

    [Test]
    public async Task CreateAsync_rolls_back_everything_when_a_later_item_is_out_of_stock()
    {
        int okDishId;
        int lowDishId;
        await using (var seed = CreateContext())
        {
            okDishId = SeedDish(seed, inventory: 100, name: "Plenty");
            lowDishId = SeedDish(seed, inventory: 1, name: "Scarce");
        }

        await using var context = CreateContext();
        var service = new OrderService(context);

        // 第一项库存充足会先被扣减，第二项库存不足触发回滚。
        var result = await service.CreateAsync(
            NewOrderRequest(SeededUserId, addressId: 1, Item(okDishId, 2), Item(lowDishId, 5)),
            CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(CreateOrderOutcome.InsufficientStock));

        await using var verify = CreateContext();
        // 事务回滚：不留任何订单/明细，且第一项库存必须恢复原值。
        Assert.That(verify.Orders.Count(), Is.EqualTo(0));
        Assert.That(verify.OrderDishes.Count(), Is.EqualTo(0));
        Assert.That(verify.OrderUsers.Count(), Is.EqualTo(0));
        Assert.That(verify.Dishes.Single(d => d.Id == okDishId).Inventory, Is.EqualTo(100));
    }

    // ---------- 3. 状态守卫：取消待支付订单 ----------

    [Test]
    public async Task CancelAsync_cancels_pending_order_and_restores_inventory()
    {
        int dishId;
        int orderId;
        await using (var seed = CreateContext())
        {
            dishId = SeedDish(seed, inventory: 10);
            var service = new OrderService(seed);
            var result = await service.CreateAsync(
                NewOrderRequest(SeededUserId, addressId: 1, Item(dishId, 4)),
                CancellationToken.None);
            orderId = result.Order!.Id;
        }

        // 下单后库存 10 -> 6
        await using (var check = CreateContext())
        {
            Assert.That(check.Dishes.Single(d => d.Id == dishId).Inventory, Is.EqualTo(6));
        }

        await using var context = CreateContext();
        var cancelService = new OrderService(context);
        var cancel = await cancelService.CancelAsync(orderId, CancellationToken.None);

        Assert.That(cancel.Outcome, Is.EqualTo(CancelOrderOutcome.Success));

        await using var verify = CreateContext();
        // 订单被删除，库存恢复 6 -> 10
        Assert.That(verify.Orders.Any(o => o.Id == orderId), Is.False);
        Assert.That(verify.Dishes.Single(d => d.Id == dishId).Inventory, Is.EqualTo(10));
    }

    [Test]
    public async Task CancelAsync_for_missing_order_returns_not_found()
    {
        await using var context = CreateContext();
        var service = new OrderService(context);

        var cancel = await service.CancelAsync(999, CancellationToken.None);

        Assert.That(cancel.Outcome, Is.EqualTo(CancelOrderOutcome.NotFound));
    }

    [Test]
    public async Task CancelAsync_for_non_pending_order_is_rejected_and_keeps_order()
    {
        int dishId;
        int orderId;
        await using (var seed = CreateContext())
        {
            dishId = SeedDish(seed, inventory: 10);
            var service = new OrderService(seed);
            var result = await service.CreateAsync(
                NewOrderRequest(SeededUserId, addressId: 1, Item(dishId, 2)),
                CancellationToken.None);
            orderId = result.Order!.Id;
        }

        // 手动把订单状态改成已付款，模拟非待支付。
        await using (var mutate = CreateContext())
        {
            var order = mutate.Orders.Single(o => o.Id == orderId);
            order.Status = OrderStatus.Paid;
            await mutate.SaveChangesAsync();
        }

        await using var context = CreateContext();
        var cancelService = new OrderService(context);
        var cancel = await cancelService.CancelAsync(orderId, CancellationToken.None);

        Assert.That(cancel.Outcome, Is.EqualTo(CancelOrderOutcome.NotCancellable));

        await using var verify = CreateContext();
        // 非待支付订单不被删除，库存不被恢复。
        Assert.That(verify.Orders.Any(o => o.Id == orderId), Is.True);
        Assert.That(verify.Dishes.Single(d => d.Id == dishId).Inventory, Is.EqualTo(8));
    }

    // ---------- 查询 ----------

    [Test]
    public async Task ListByUserAsync_returns_orders_for_the_user()
    {
        int dishId;
        await using (var seed = CreateContext())
        {
            dishId = SeedDish(seed, inventory: 50);
            var service = new OrderService(seed);
            await service.CreateAsync(
                NewOrderRequest(SeededUserId, addressId: 1, Item(dishId, 1)),
                CancellationToken.None);
            await service.CreateAsync(
                NewOrderRequest(SeededUserId, addressId: 1, Item(dishId, 2)),
                CancellationToken.None);
        }

        await using var context = CreateContext();
        var listService = new OrderService(context);

        var orders = await listService.ListByUserAsync(SeededUserId, CancellationToken.None);

        Assert.That(orders, Has.Count.EqualTo(2));
        Assert.That(orders.All(o => o.UserId == SeededUserId), Is.True);
    }

    [Test]
    public async Task ListByUserAsync_returns_empty_when_user_has_no_orders()
    {
        await using var context = CreateContext();
        var service = new OrderService(context);

        var orders = await service.ListByUserAsync(SeededUserId, CancellationToken.None);

        Assert.That(orders, Is.Empty);
    }
}
