using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Order;
using TakeoutPlatform.Api.Features.Rider;
using TakeoutPlatform.Api.Features.User;

namespace TakeoutPlatform.Api.Tests;

/// <summary>
/// RiderDeliveryService 的单元测试（SQLite in-memory）。
/// 覆盖：接单准入（责任链）、并发只一个成功、配送状态流转、库存计数、可接任务过滤。
/// 种子骑手：Id=1 Zhang(ZoneA,active,available,cap3)、Id=2 Li(ZoneA,cap2)、Id=3 Off(INACTIVE)。
/// 种子用户 Id=1。
/// </summary>
public class RiderDeliveryServiceTests : ServiceTestBase
{
    private const int SeededUserId = 1;
    private const int ActiveRiderId = 1;
    private const int InactiveRiderId = 3;

    /// <summary>造一个 ReadyForDelivery 订单（含地址 + OrderUser），返回 orderId。</summary>
    private static int SeedReadyOrder(AppDbContext db, string zoneTag = "ZoneA", int userId = SeededUserId)
    {
        var address = new UserAddress
        {
            UserId = userId,
            Address = $"{zoneTag} Main Street 1",
            ContactName = "Customer",
            PhoneNumber = "13800000000",
        };
        db.UserAddresses.Add(address);
        db.SaveChanges();

        var order = new Order
        {
            Price = 50m,
            OrderTimestamp = DateTime.UtcNow,
            Status = OrderStatus.ReadyForDelivery,
            NeedUtensils = 1,
            AddressId = address.Id,
        };
        db.Orders.Add(order);
        db.SaveChanges();

        db.OrderUsers.Add(new OrderUser { OrderId = order.Id, UserId = userId });
        if (!db.Dishes.Any(item => item.Id == 900))
            db.Dishes.Add(new TakeoutPlatform.Api.Features.Merchant.Dish { Id = 900, MerchantId = 1, Name = "Delivery dish", Price = 50m, Inventory = 10 });
        db.OrderDishes.Add(new OrderDish { OrderId = order.Id, MerchantId = 1, DishId = 900, DishNum = 1 });
        db.MerchantOrderFulfillments.Add(new MerchantOrderFulfillment
        {
            OrderId = order.Id,
            MerchantId = 1,
            Status = MerchantFulfillmentStatus.ReadyForDelivery,
            UpdatedAtUtc = DateTime.UtcNow,
        });
        db.SaveChanges();
        return order.Id;
    }

    private RiderDeliveryService NewService(AppDbContext db) => new(db, TimeProvider.System);

    // ---------- AcceptAsync ----------

    [Test]
    public async Task AcceptAsync_assigns_order_and_increments_rider_load()
    {
        int orderId;
        await using (var seed = CreateContext())
            orderId = SeedReadyOrder(seed);

        await using var db = CreateContext();
        var result = await NewService(db).AcceptAsync(orderId, ActiveRiderId, CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(AcceptOutcome.Success));

        await using var verify = CreateContext();
        Assert.That(verify.Orders.Single(o => o.Id == orderId).Status, Is.EqualTo(OrderStatus.Assigned));
        Assert.That(verify.RiderDeliveries.Single(d => d.OrderId == orderId).RiderId, Is.EqualTo(ActiveRiderId));
        Assert.That(verify.Riders.Single(r => r.Id == ActiveRiderId).ActiveDeliveryCount, Is.EqualTo(1));
    }

    [Test]
    public async Task AcceptAsync_for_missing_order_returns_OrderNotFound()
    {
        await using var db = CreateContext();
        var result = await NewService(db).AcceptAsync(99999, ActiveRiderId, CancellationToken.None);
        Assert.That(result.Outcome, Is.EqualTo(AcceptOutcome.OrderNotFound));
    }

    [Test]
    public async Task AcceptAsync_when_order_not_ready_returns_NotReadyForDelivery()
    {
        int orderId;
        await using (var seed = CreateContext())
        {
            orderId = SeedReadyOrder(seed);
            var order = seed.Orders.Single(o => o.Id == orderId);
            order.Status = OrderStatus.Preparing; // 尚未就绪
            seed.MerchantOrderFulfillments.Single(item => item.OrderId == orderId).Status = MerchantFulfillmentStatus.Confirmed;
            seed.SaveChanges();
        }

        await using var db = CreateContext();
        var result = await NewService(db).AcceptAsync(orderId, ActiveRiderId, CancellationToken.None);
        Assert.That(result.Outcome, Is.EqualTo(AcceptOutcome.NotReadyForDelivery));
    }

    [Test]
    public async Task Rider_can_claim_after_merchant_accepts_but_cannot_pick_up_before_food_is_ready()
    {
        int orderId;
        await using (var seed = CreateContext())
        {
            orderId = SeedReadyOrder(seed);
            seed.Orders.Single(o => o.Id == orderId).Status = OrderStatus.Preparing;
            seed.MerchantOrderFulfillments.Single(item => item.OrderId == orderId).Status =
                MerchantFulfillmentStatus.Preparing;
            seed.SaveChanges();
        }

        await using (var claim = CreateContext())
        {
            var accepted = await NewService(claim).AcceptAsync(orderId, ActiveRiderId, CancellationToken.None);
            Assert.That(accepted.Outcome, Is.EqualTo(AcceptOutcome.Success));
        }

        await using (var earlyPickup = CreateContext())
        {
            var result = await NewService(earlyPickup).AdvanceAsync(
                orderId, ActiveRiderId, new AdvanceDeliveryRequest { Action = "PickUp" }, CancellationToken.None);
            Assert.That(result.Outcome, Is.EqualTo(AdvanceOutcome.InvalidTransition));
        }

        await using (var kitchen = CreateContext())
        {
            kitchen.MerchantOrderFulfillments.Single(item => item.OrderId == orderId).Status =
                MerchantFulfillmentStatus.ReadyForDelivery;
            kitchen.SaveChanges();
        }

        await using var pickup = CreateContext();
        var pickedUp = await NewService(pickup).AdvanceAsync(
            orderId, ActiveRiderId, new AdvanceDeliveryRequest { Action = "PickUp" }, CancellationToken.None);
        Assert.That(pickedUp.Outcome, Is.EqualTo(AdvanceOutcome.Success));
    }

    [Test]
    public async Task AcceptAsync_by_inactive_rider_returns_RiderNotEligible()
    {
        int orderId;
        await using (var seed = CreateContext())
            orderId = SeedReadyOrder(seed);

        await using var db = CreateContext();
        var result = await NewService(db).AcceptAsync(orderId, InactiveRiderId, CancellationToken.None);
        Assert.That(result.Outcome, Is.EqualTo(AcceptOutcome.RiderNotEligible));
    }

    [Test]
    public async Task AcceptAsync_second_rider_on_taken_order_returns_AlreadyTaken()
    {
        int orderId;
        await using (var seed = CreateContext())
            orderId = SeedReadyOrder(seed);

        await using (var first = CreateContext())
        {
            var r = await NewService(first).AcceptAsync(orderId, ActiveRiderId, CancellationToken.None);
            Assert.That(r.Outcome, Is.EqualTo(AcceptOutcome.Success));
        }

        // 第二个骑手尝试接同一单
        await using var second = CreateContext();
        var result = await NewService(second).AcceptAsync(orderId, 2, CancellationToken.None);
        Assert.That(result.Outcome, Is.EqualTo(AcceptOutcome.AlreadyTaken));
    }

    [Test]
    public async Task Two_riders_accepting_same_order_only_one_succeeds()
    {
        int orderId;
        await using (var seed = CreateContext())
            orderId = SeedReadyOrder(seed);

        // 并发：两个骑手同时接同一订单。OrderId 唯一约束保证只有一个成功。
        await using var dbA = CreateContext();
        await using var dbB = CreateContext();
        var taskA = NewService(dbA).AcceptAsync(orderId, 1, CancellationToken.None);
        var taskB = NewService(dbB).AcceptAsync(orderId, 2, CancellationToken.None);
        var results = await Task.WhenAll(taskA, taskB);

        var successes = results.Count(r => r.Outcome == AcceptOutcome.Success);
        Assert.That(successes, Is.EqualTo(1), "恰好一个骑手接单成功");

        await using var verify = CreateContext();
        Assert.That(verify.RiderDeliveries.Count(d => d.OrderId == orderId), Is.EqualTo(1));
    }

    // ---------- AdvanceAsync 状态流转 ----------

    [Test]
    public async Task AdvanceAsync_full_lifecycle_Assigned_to_Completed()
    {
        int orderId;
        await using (var seed = CreateContext())
            orderId = SeedReadyOrder(seed);
        await using (var accept = CreateContext())
            await NewService(accept).AcceptAsync(orderId, ActiveRiderId, CancellationToken.None);

        await using (var db = CreateContext())
            Assert.That((await NewService(db).AdvanceAsync(orderId, ActiveRiderId, new AdvanceDeliveryRequest { Action = "PickUp" }, CancellationToken.None)).Outcome, Is.EqualTo(AdvanceOutcome.Success));
        await using (var db = CreateContext())
            Assert.That((await NewService(db).AdvanceAsync(orderId, ActiveRiderId, new AdvanceDeliveryRequest { Action = "StartDelivering" }, CancellationToken.None)).Outcome, Is.EqualTo(AdvanceOutcome.Success));
        await using (var db = CreateContext())
            Assert.That((await NewService(db).AdvanceAsync(orderId, ActiveRiderId, new AdvanceDeliveryRequest { Action = "Complete" }, CancellationToken.None)).Outcome, Is.EqualTo(AdvanceOutcome.Success));

        await using var verify = CreateContext();
        Assert.That(verify.RiderDeliveries.Single(d => d.OrderId == orderId).Status, Is.EqualTo(DeliveryStatus.Completed));
        Assert.That(verify.Orders.Single(o => o.Id == orderId).Status, Is.EqualTo(OrderStatus.Completed));
        // 完成后在途单量回落到 0
        Assert.That(verify.Riders.Single(r => r.Id == ActiveRiderId).ActiveDeliveryCount, Is.EqualTo(0));
    }

    [Test]
    public async Task AdvanceAsync_invalid_transition_is_rejected()
    {
        int orderId;
        await using (var seed = CreateContext())
            orderId = SeedReadyOrder(seed);
        await using (var accept = CreateContext())
            await NewService(accept).AcceptAsync(orderId, ActiveRiderId, CancellationToken.None);

        // 刚接单(Assigned)就 Complete 是非法跳转
        await using var db = CreateContext();
        var result = await NewService(db).AdvanceAsync(orderId, ActiveRiderId, new AdvanceDeliveryRequest { Action = "Complete" }, CancellationToken.None);
        Assert.That(result.Outcome, Is.EqualTo(AdvanceOutcome.InvalidTransition));
    }

    [Test]
    public async Task AdvanceAsync_unknown_action_returns_InvalidRequest()
    {
        int orderId;
        await using (var seed = CreateContext())
            orderId = SeedReadyOrder(seed);
        await using (var accept = CreateContext())
            await NewService(accept).AcceptAsync(orderId, ActiveRiderId, CancellationToken.None);

        await using var db = CreateContext();
        var result = await NewService(db).AdvanceAsync(orderId, ActiveRiderId, new AdvanceDeliveryRequest { Action = "Teleport" }, CancellationToken.None);
        Assert.That(result.Outcome, Is.EqualTo(AdvanceOutcome.InvalidRequest));
    }

    [Test]
    public async Task AdvanceAsync_for_other_riders_delivery_returns_NotFound()
    {
        int orderId;
        await using (var seed = CreateContext())
            orderId = SeedReadyOrder(seed);
        await using (var accept = CreateContext())
            await NewService(accept).AcceptAsync(orderId, ActiveRiderId, CancellationToken.None);

        // 骑手 2 想推进骑手 1 的配送
        await using var db = CreateContext();
        var result = await NewService(db).AdvanceAsync(orderId, 2, new AdvanceDeliveryRequest { Action = "PickUp" }, CancellationToken.None);
        Assert.That(result.Outcome, Is.EqualTo(AdvanceOutcome.NotFound));
    }

    // ---------- ListAvailableAsync ----------

    [Test]
    public async Task ListAvailableAsync_excludes_already_taken_orders()
    {
        int order1, order2;
        await using (var seed = CreateContext())
        {
            order1 = SeedReadyOrder(seed);
            order2 = SeedReadyOrder(seed);
        }
        await using (var accept = CreateContext())
            await NewService(accept).AcceptAsync(order1, ActiveRiderId, CancellationToken.None);

        await using var db = CreateContext();
        var available = await NewService(db).ListAvailableAsync(CancellationToken.None);

        Assert.That(available.Any(o => o.OrderId == order1), Is.False, "已被接的订单不在可接池");
        Assert.That(available.Any(o => o.OrderId == order2), Is.True);
    }
}
