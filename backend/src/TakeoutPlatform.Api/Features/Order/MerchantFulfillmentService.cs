using System.Data;
using Microsoft.EntityFrameworkCore;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Inventory;

namespace TakeoutPlatform.Api.Features.Order;

public enum MerchantTransitionOutcome
{
    Success,
    NotFound,
    Forbidden,
    InvalidRequest,
    InvalidTransition,
}

public sealed record MerchantTransitionResult(
    MerchantTransitionOutcome Outcome,
    MerchantOrderResponse? Order,
    string? Message);

public sealed class MerchantFulfillmentService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly InventoryService _inventory;

    public MerchantFulfillmentService(AppDbContext db, TimeProvider timeProvider, InventoryService? inventory = null)
    {
        _db = db;
        _timeProvider = timeProvider;
        _inventory = inventory ?? new InventoryService(db, timeProvider);
    }

    public async Task<IReadOnlyList<MerchantOrderResponse>> ListAsync(
        int merchantId,
        CancellationToken cancellationToken)
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Where(order => order.Status != OrderStatus.Pending
                && order.OrderDishes.Any(item => item.MerchantId == merchantId))
            .Include(order => order.OrderUser)
            .Include(order => order.OrderDishes)
            .OrderByDescending(order => order.OrderTimestamp)
            .ThenByDescending(order => order.Id)
            .ToListAsync(cancellationToken);

        if (orders.Count == 0)
            return Array.Empty<MerchantOrderResponse>();

        var orderIds = orders.Select(order => order.Id).ToArray();
        var fulfillments = await _db.MerchantOrderFulfillments
            .AsNoTracking()
            .Where(item => item.MerchantId == merchantId && orderIds.Contains(item.OrderId))
            .ToDictionaryAsync(item => item.OrderId, cancellationToken);

        var dishIds = orders
            .SelectMany(order => order.OrderDishes)
            .Where(item => item.MerchantId == merchantId)
            .Select(item => item.DishId)
            .Distinct()
            .ToArray();
        var dishes = await _db.Dishes
            .AsNoTracking()
            .Where(dish => dish.MerchantId == merchantId && dishIds.Contains(dish.Id))
            .ToDictionaryAsync(dish => dish.Id, cancellationToken);

        var deliveryRows = await _db.RiderDeliveries.AsNoTracking()
            .Where(item => orderIds.Contains(item.OrderId))
            .Join(_db.Riders.AsNoTracking(), delivery => delivery.RiderId, rider => rider.Id,
                (delivery, rider) => new RiderInfo(delivery.OrderId, rider.Name, rider.PhoneNumber, delivery.Status))
            .ToDictionaryAsync(item => item.OrderId, cancellationToken);

        return orders
            .Select(order => BuildResponse(
                order,
                merchantId,
                fulfillments.GetValueOrDefault(order.Id),
                dishes,
                deliveryRows.GetValueOrDefault(order.Id)))
            .ToList();
    }

    public async Task<MerchantTransitionResult> TransitionAsync(
        int orderId,
        int merchantId,
        MerchantOrderTransitionRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<MerchantOrderAction>(request.Action, true, out var action))
        {
            return new MerchantTransitionResult(
                MerchantTransitionOutcome.InvalidRequest,
                null,
                "Action must be Accept, StartPreparing, MarkReady, or Reject.");
        }

        if (action == MerchantOrderAction.Reject && string.IsNullOrWhiteSpace(request.Reason))
        {
            return new MerchantTransitionResult(
                MerchantTransitionOutcome.InvalidRequest,
                null,
                "A rejection reason is required.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var order = await _db.Orders
            .Include(item => item.OrderUser)
            .Include(item => item.OrderDishes)
            .Include(item => item.MerchantFulfillments)
            .FirstOrDefaultAsync(item => item.Id == orderId, cancellationToken);

        if (order is null)
            return new MerchantTransitionResult(MerchantTransitionOutcome.NotFound, null, "Order was not found.");

        var merchantItems = order.OrderDishes
            .Where(item => item.MerchantId == merchantId)
            .ToList();
        if (merchantItems.Count == 0)
        {
            return new MerchantTransitionResult(
                MerchantTransitionOutcome.Forbidden,
                null,
                "This order does not belong to the signed-in merchant.");
        }

        if (order.Status == OrderStatus.Pending)
        {
            return new MerchantTransitionResult(
                MerchantTransitionOutcome.InvalidTransition,
                null,
                "A pending-payment order cannot enter merchant fulfilment.");
        }

        var fulfillment = order.MerchantFulfillments
            .SingleOrDefault(item => item.MerchantId == merchantId);
        var currentStatus = fulfillment?.Status ?? MerchantFulfillmentStatus.Confirmed;

        if (action == MerchantOrderAction.Reject
            && await _db.RiderDeliveries.AnyAsync(item => item.OrderId == orderId, cancellationToken))
        {
            return new MerchantTransitionResult(
                MerchantTransitionOutcome.InvalidTransition,
                null,
                "The order cannot be rejected after a courier has accepted the delivery.");
        }

        if (!TryGetNextStatus(currentStatus, action, out var nextStatus))
        {
            return new MerchantTransitionResult(
                MerchantTransitionOutcome.InvalidTransition,
                null,
                $"Action {action} is not allowed while the order is {currentStatus}.");
        }

        if (fulfillment is null)
        {
            fulfillment = new MerchantOrderFulfillment
            {
                OrderId = order.Id,
                MerchantId = merchantId,
            };
            order.MerchantFulfillments.Add(fulfillment);
        }

        fulfillment.Status = nextStatus;
        fulfillment.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        if (nextStatus == MerchantFulfillmentStatus.Rejected)
        {
            fulfillment.RejectionReason = request.Reason!.Trim();
            if (!fulfillment.InventoryRestored)
            {
                var release = await _inventory.ReleaseAsync(orderId, merchantId, cancellationToken);
                if (!release.Succeeded && release.Outcome != InventoryOutcome.ReservationNotFound)
                    return new(MerchantTransitionOutcome.InvalidRequest, null, release.Message);
                // Old orders have no reservation ledger; retain their existing restoration path.
                if (release.Outcome == InventoryOutcome.ReservationNotFound)
                {
                    var dishIds = merchantItems.Select(item => item.DishId).ToArray();
                    var dishes = await _db.Dishes
                        .Where(dish => dish.MerchantId == merchantId && dishIds.Contains(dish.Id))
                        .ToDictionaryAsync(dish => dish.Id, cancellationToken);

                    foreach (var item in merchantItems)
                    {
                        if (dishes.TryGetValue(item.DishId, out var dish))
                            dish.Inventory += item.DishNum;
                    }
                }

                fulfillment.InventoryRestored = true;
            }
        }

        var hasRider = await _db.RiderDeliveries.AnyAsync(item => item.OrderId == orderId, cancellationToken);
        if (!hasRider) RecalculateOrderStatus(order, fulfillment);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var response = (await ListAsync(merchantId, cancellationToken))
            .Single(item => item.OrderId == orderId);
        return new MerchantTransitionResult(MerchantTransitionOutcome.Success, response, null);
    }

    private static bool TryGetNextStatus(
        MerchantFulfillmentStatus current,
        MerchantOrderAction action,
        out MerchantFulfillmentStatus next)
    {
        next = (current, action) switch
        {
            (MerchantFulfillmentStatus.Confirmed, MerchantOrderAction.Accept) => MerchantFulfillmentStatus.Accepted,
            (MerchantFulfillmentStatus.Confirmed, MerchantOrderAction.Reject) => MerchantFulfillmentStatus.Rejected,
            (MerchantFulfillmentStatus.Accepted, MerchantOrderAction.StartPreparing) => MerchantFulfillmentStatus.Preparing,
            (MerchantFulfillmentStatus.Accepted, MerchantOrderAction.Reject) => MerchantFulfillmentStatus.Rejected,
            (MerchantFulfillmentStatus.Preparing, MerchantOrderAction.MarkReady) => MerchantFulfillmentStatus.ReadyForDelivery,
            _ => current,
        };

        return next != current;
    }

    private static void RecalculateOrderStatus(
        Order order,
        MerchantOrderFulfillment changedFulfillment)
    {
        var merchantIds = order.OrderDishes
            .Select(item => item.MerchantId)
            .Distinct()
            .ToArray();
        var statuses = merchantIds
            .Select(merchantId => order.MerchantFulfillments
                .SingleOrDefault(item => item.MerchantId == merchantId)?.Status
                ?? (merchantId == changedFulfillment.MerchantId
                    ? changedFulfillment.Status
                    : MerchantFulfillmentStatus.Confirmed))
            .ToArray();

        if (statuses.All(status => status == MerchantFulfillmentStatus.Rejected))
        {
            order.Status = OrderStatus.Rejected;
            return;
        }

        var active = statuses.Where(status => status != MerchantFulfillmentStatus.Rejected).ToArray();
        order.Status = active.All(status => status >= MerchantFulfillmentStatus.ReadyForDelivery)
            ? OrderStatus.ReadyForDelivery
            : active.All(status => status >= MerchantFulfillmentStatus.Preparing)
                ? OrderStatus.Preparing
                : active.All(status => status >= MerchantFulfillmentStatus.Accepted)
                    ? OrderStatus.Accepted
                    : OrderStatus.Paid;
    }

    private static MerchantOrderResponse BuildResponse(
        Order order,
        int merchantId,
        MerchantOrderFulfillment? fulfillment,
        IReadOnlyDictionary<int, Features.Merchant.Dish> dishes,
        RiderInfo? delivery)
    {
        var items = order.OrderDishes
            .Where(item => item.MerchantId == merchantId)
            .Select(item =>
            {
                dishes.TryGetValue(item.DishId, out var dish);
                var unitPrice = dish?.Price ?? 0m;
                return new MerchantOrderItemResponse(
                    item.DishId,
                    dish?.Name ?? "Unavailable dish",
                    unitPrice,
                    item.DishNum,
                    unitPrice * item.DishNum);
            })
            .ToList();

        return new MerchantOrderResponse(
            order.Id,
            merchantId,
            order.OrderUser?.UserId ?? 0,
            order.OrderTimestamp,
            (fulfillment?.Status ?? MerchantFulfillmentStatus.Confirmed).ToString(),
            order.Status.ToString(),
            fulfillment?.RejectionReason,
            items.Sum(item => item.LineTotal),
            items.Sum(item => item.Quantity),
            delivery?.Name,
            delivery?.PhoneNumber,
            delivery?.Status.ToString(),
            items);
    }

    private sealed record RiderInfo(int OrderId, string Name, string? PhoneNumber, Features.Rider.DeliveryStatus Status);

    private enum MerchantOrderAction
    {
        Accept,
        StartPreparing,
        MarkReady,
        Reject,
    }
}
