using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TakeoutPlatform.Api.Features.Merchant;
using TakeoutPlatform.Api.Features.Order;

namespace TakeoutPlatform.Api.Tests;

public class MerchantFulfillmentServiceTests : ServiceTestBase
{
    private async Task<(int OrderId, int DishId)> SeedConfirmedOrderAsync(
        int inventory = 10,
        int quantity = 2)
    {
        await using var context = CreateContext();
        var dish = new Dish
        {
            MerchantId = MerchantId,
            Name = "Merchant fulfilment dish",
            Price = 12m,
            Inventory = inventory,
        };
        context.Dishes.Add(dish);
        await context.SaveChangesAsync();

        var orderService = new OrderService(context);
        var created = await orderService.CreateAsync(
            new CreateOrderRequest
            {
                UserId = 1,
                AddressId = 1,
                Price = dish.Price * quantity,
                ShoppingCart =
                [
                    new CreateOrderRequest.CartItem
                    {
                        MerchantId = MerchantId,
                        DishId = dish.Id,
                        DishNum = quantity,
                    },
                ],
            },
            CancellationToken.None);

        var order = await context.Orders.SingleAsync(item => item.Id == created.Order!.Id);
        order.Status = OrderStatus.Paid;
        await context.SaveChangesAsync();
        return (order.Id, dish.Id);
    }

    [Test]
    public async Task ListAsync_returns_only_the_signed_in_merchants_confirmed_orders()
    {
        var seeded = await SeedConfirmedOrderAsync();
        await using var context = CreateContext();
        var service = new MerchantFulfillmentService(context, TimeProvider.System);

        var ownOrders = await service.ListAsync(MerchantId, CancellationToken.None);
        var otherOrders = await service.ListAsync(OtherMerchantId, CancellationToken.None);

        Assert.That(ownOrders, Has.Count.EqualTo(1));
        Assert.That(ownOrders[0].OrderId, Is.EqualTo(seeded.OrderId));
        Assert.That(ownOrders[0].Status, Is.EqualTo("Confirmed"));
        Assert.That(ownOrders[0].OverallStatus, Is.EqualTo("Paid"));
        Assert.That(ownOrders[0].Items.Single().DishName, Is.EqualTo("Merchant fulfilment dish"));
        Assert.That(otherOrders, Is.Empty);
    }

    [Test]
    public async Task TransitionAsync_enforces_the_confirmed_to_ready_state_sequence()
    {
        var seeded = await SeedConfirmedOrderAsync();
        await using var context = CreateContext();
        var service = new MerchantFulfillmentService(context, TimeProvider.System);

        var accepted = await Transition(service, seeded.OrderId, "Accept");
        var preparing = await Transition(service, seeded.OrderId, "StartPreparing");
        var ready = await Transition(service, seeded.OrderId, "MarkReady");

        Assert.That(accepted.Order?.Status, Is.EqualTo("Accepted"));
        Assert.That(preparing.Order?.Status, Is.EqualTo("Preparing"));
        Assert.That(ready.Order?.Status, Is.EqualTo("ReadyForDelivery"));
        Assert.That(ready.Order?.OverallStatus, Is.EqualTo("ReadyForDelivery"));

        await using var verify = CreateContext();
        Assert.That(
            verify.Orders.Single(item => item.Id == seeded.OrderId).Status,
            Is.EqualTo(OrderStatus.ReadyForDelivery));
    }

    [Test]
    public async Task TransitionAsync_rejects_an_invalid_state_change()
    {
        var seeded = await SeedConfirmedOrderAsync();
        await using var context = CreateContext();
        var service = new MerchantFulfillmentService(context, TimeProvider.System);

        var result = await Transition(service, seeded.OrderId, "MarkReady");

        Assert.That(result.Outcome, Is.EqualTo(MerchantTransitionOutcome.InvalidTransition));
    }

    [Test]
    public async Task TransitionAsync_forbids_a_merchant_that_does_not_own_order_items()
    {
        var seeded = await SeedConfirmedOrderAsync();
        await using var context = CreateContext();
        var service = new MerchantFulfillmentService(context, TimeProvider.System);

        var result = await service.TransitionAsync(
            seeded.OrderId,
            OtherMerchantId,
            new MerchantOrderTransitionRequest { Action = "Accept" },
            CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(MerchantTransitionOutcome.Forbidden));
    }

    [Test]
    public async Task Reject_restores_inventory_exactly_once_and_records_the_reason()
    {
        var seeded = await SeedConfirmedOrderAsync(inventory: 10, quantity: 4);
        await using var context = CreateContext();
        var service = new MerchantFulfillmentService(context, TimeProvider.System);

        var rejected = await service.TransitionAsync(
            seeded.OrderId,
            MerchantId,
            new MerchantOrderTransitionRequest
            {
                Action = "Reject",
                Reason = "Kitchen equipment is unavailable.",
            },
            CancellationToken.None);
        var repeated = await service.TransitionAsync(
            seeded.OrderId,
            MerchantId,
            new MerchantOrderTransitionRequest
            {
                Action = "Reject",
                Reason = "Repeated request",
            },
            CancellationToken.None);

        Assert.That(rejected.Outcome, Is.EqualTo(MerchantTransitionOutcome.Success));
        Assert.That(rejected.Order?.RejectionReason, Is.EqualTo("Kitchen equipment is unavailable."));
        Assert.That(repeated.Outcome, Is.EqualTo(MerchantTransitionOutcome.InvalidTransition));

        await using var verify = CreateContext();
        Assert.That(verify.Dishes.Single(item => item.Id == seeded.DishId).Inventory, Is.EqualTo(10));
        var fulfillment = verify.MerchantOrderFulfillments.Single();
        Assert.That(fulfillment.InventoryRestored, Is.True);
        Assert.That(fulfillment.Status, Is.EqualTo(MerchantFulfillmentStatus.Rejected));
    }

    private static Task<MerchantTransitionResult> Transition(
        MerchantFulfillmentService service,
        int orderId,
        string action) =>
        service.TransitionAsync(
            orderId,
            MerchantId,
            new MerchantOrderTransitionRequest { Action = action },
            CancellationToken.None);
}
