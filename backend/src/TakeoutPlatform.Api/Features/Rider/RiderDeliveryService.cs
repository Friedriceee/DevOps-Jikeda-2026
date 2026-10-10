using System.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Order;
using TakeoutPlatform.Api.Features.Rider.Assignment;

namespace TakeoutPlatform.Api.Features.Rider;

public enum AcceptOutcome
{
    Success,
    OrderNotFound,
    NotReadyForDelivery,
    AlreadyTaken,        // 订单已被接走（并发或重复）
    RiderNotFound,
    RiderNotEligible,    // 责任链准入未通过（离职/错区/下线/满载）
}

public enum AdvanceOutcome
{
    Success,
    NotFound,            // 该骑手没有这单的配送记录
    InvalidRequest,      // 动作无法识别
    InvalidTransition,   // 当前状态不允许该动作
}

public sealed record AcceptResult(AcceptOutcome Outcome, RiderDeliveryResponse? Delivery, string? Message);
public sealed record AdvanceResult(AdvanceOutcome Outcome, RiderDeliveryResponse? Delivery, string? Message);

/// <summary>
/// 骑手配送服务。消费 member4 产出的 ReadyForDelivery 订单：
/// 列出可接任务、接单（并发安全、责任链准入）、推进配送状态直至 Completed。
/// </summary>
public sealed class RiderDeliveryService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public RiderDeliveryService(AppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    /// <summary>可接任务池：状态为 ReadyForDelivery 且尚未被任何骑手接走的订单。</summary>
    public async Task<IReadOnlyList<AvailableDeliveryResponse>> ListAvailableAsync(
        CancellationToken cancellationToken)
    {
        var takenOrderIds = await _db.RiderDeliveries
            .AsNoTracking()
            .Select(delivery => delivery.OrderId)
            .ToListAsync(cancellationToken);

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(order => order.Status == OrderStatus.ReadyForDelivery
                && !takenOrderIds.Contains(order.Id))
            .OrderBy(order => order.OrderTimestamp)
            .ToListAsync(cancellationToken);

        var result = new List<AvailableDeliveryResponse>();
        foreach (var order in orders)
        {
            var address = await _db.UserAddresses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == order.AddressId, cancellationToken);
            result.Add(new AvailableDeliveryResponse(
                order.Id,
                ResolveZone(address?.Address),
                order.Price,
                order.OrderTimestamp,
                address?.Address ?? string.Empty,
                address?.ContactName ?? string.Empty,
                address?.PhoneNumber ?? string.Empty));
        }

        return result;
    }

    /// <summary>骑手自己的配送任务列表。</summary>
    public async Task<IReadOnlyList<RiderDeliveryResponse>> ListMyDeliveriesAsync(
        int riderId,
        CancellationToken cancellationToken)
    {
        var deliveries = await _db.RiderDeliveries
            .AsNoTracking()
            .Where(delivery => delivery.RiderId == riderId)
            .OrderByDescending(delivery => delivery.AssignedAtUtc)
            .ToListAsync(cancellationToken);

        var result = new List<RiderDeliveryResponse>();
        foreach (var delivery in deliveries)
        {
            var order = await _db.Orders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == delivery.OrderId, cancellationToken);
            var address = order is null
                ? null
                : await _db.UserAddresses.AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == order.AddressId, cancellationToken);
            result.Add(ToResponse(delivery, order?.Price ?? 0m, address));
        }

        return result;
    }

    /// <summary>
    /// 骑手接单。并发安全：Serializable 事务 + RiderDelivery.OrderId 唯一约束兜底。
    /// 责任链对"该骑手单人候选"做准入校验（必须 Active/同区/Available/未满载）。
    /// </summary>
    public async Task<AcceptResult> AcceptAsync(
        int orderId,
        int riderId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
            if (order is null)
                return new AcceptResult(AcceptOutcome.OrderNotFound, null, "订单不存在");

            // 先判是否已被接走（已被接的订单其状态已非 ReadyForDelivery，
            // 优先返回更贴切的 AlreadyTaken，而非 NotReadyForDelivery）。
            var existing = await _db.RiderDeliveries
                .AnyAsync(d => d.OrderId == orderId, cancellationToken);
            if (existing)
                return new AcceptResult(AcceptOutcome.AlreadyTaken, null, "订单已被接走");

            if (order.Status != OrderStatus.ReadyForDelivery)
                return new AcceptResult(AcceptOutcome.NotReadyForDelivery, null, "订单尚未可配送");

            var rider = await _db.Riders.FirstOrDefaultAsync(r => r.Id == riderId, cancellationToken);
            if (rider is null)
                return new AcceptResult(AcceptOutcome.RiderNotFound, null, "骑手不存在");

            // 责任链准入校验：候选集只含该骑手，看能否通过全部筛选环。
            var address = await _db.UserAddresses.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == order.AddressId, cancellationToken);
            var zone = ResolveZone(address?.Address);
            var context = new RiderAssignmentContext(zone, new[] { rider });
            var eligibility = RiderAssignmentChain.Build().Handle(context);
            if (!eligibility.Success || eligibility.SelectedRider?.Id != rider.Id)
                return new AcceptResult(AcceptOutcome.RiderNotEligible, null, "骑手不满足接单条件");

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var delivery = new RiderDelivery
            {
                OrderId = orderId,
                RiderId = riderId,
                Status = DeliveryStatus.Assigned,
                AssignedAtUtc = now,
            };
            _db.RiderDeliveries.Add(delivery);

            order.Status = OrderStatus.Assigned;
            rider.ActiveDeliveryCount += 1;

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new AcceptResult(AcceptOutcome.Success, ToResponse(delivery, order.Price, address), null);
        }
        catch (DbUpdateException)
        {
            // 并发下另一骑手先插入，唯一约束触发：本次视为已被接走。
            await transaction.RollbackAsync(cancellationToken);
            return new AcceptResult(AcceptOutcome.AlreadyTaken, null, "订单已被接走");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>推进配送状态：PickUp → StartDelivering → Complete。</summary>
    public async Task<AdvanceResult> AdvanceAsync(
        int orderId,
        int riderId,
        AdvanceDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseAction(request.Action, out var action))
            return new AdvanceResult(AdvanceOutcome.InvalidRequest, null, "动作必须为 PickUp、StartDelivering 或 Complete");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var delivery = await _db.RiderDeliveries
                .FirstOrDefaultAsync(d => d.OrderId == orderId && d.RiderId == riderId, cancellationToken);
            if (delivery is null)
                return new AdvanceResult(AdvanceOutcome.NotFound, null, "未找到该配送任务");

            if (!TryGetNextStatus(delivery.Status, action, out var next))
                return new AdvanceResult(AdvanceOutcome.InvalidTransition, null,
                    $"配送状态 {delivery.Status} 不允许动作 {action}");

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            delivery.Status = next;
            switch (next)
            {
                case DeliveryStatus.PickedUp:
                    delivery.PickedUpAtUtc = now;
                    if (order is not null) order.Status = OrderStatus.PickedUp;
                    break;
                case DeliveryStatus.Delivering:
                    delivery.DeliveringAtUtc = now;
                    if (order is not null) order.Status = OrderStatus.Delivering;
                    break;
                case DeliveryStatus.Completed:
                    delivery.CompletedAtUtc = now;
                    if (order is not null) order.Status = OrderStatus.Completed;
                    var rider = await _db.Riders.FirstOrDefaultAsync(r => r.Id == riderId, cancellationToken);
                    if (rider is not null && rider.ActiveDeliveryCount > 0)
                        rider.ActiveDeliveryCount -= 1;
                    break;
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var address = order is null
                ? null
                : await _db.UserAddresses.AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == order.AddressId, cancellationToken);
            return new AdvanceResult(AdvanceOutcome.Success, ToResponse(delivery, order?.Price ?? 0m, address), null);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static bool TryParseAction(string action, out DeliveryAction parsed)
    {
        parsed = action?.Trim().ToLowerInvariant() switch
        {
            "pickup" => DeliveryAction.PickUp,
            "startdelivering" => DeliveryAction.StartDelivering,
            "complete" => DeliveryAction.Complete,
            _ => DeliveryAction.Unknown,
        };
        return parsed != DeliveryAction.Unknown;
    }

    private static bool TryGetNextStatus(DeliveryStatus current, DeliveryAction action, out DeliveryStatus next)
    {
        next = (current, action) switch
        {
            (DeliveryStatus.Assigned, DeliveryAction.PickUp) => DeliveryStatus.PickedUp,
            (DeliveryStatus.PickedUp, DeliveryAction.StartDelivering) => DeliveryStatus.Delivering,
            (DeliveryStatus.Delivering, DeliveryAction.Complete) => DeliveryStatus.Completed,
            _ => current,
        };
        return next != current;
    }

    /// <summary>从地址串解析配送区域（最小可演示：含 "ZoneX" 则取之，否则默认 ZoneA）。</summary>
    internal static string ResolveZone(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return "ZoneA";
        var match = Regex.Match(address, "Zone[A-Za-z0-9]+", RegexOptions.IgnoreCase);
        return match.Success ? match.Value : "ZoneA";
    }

    private static RiderDeliveryResponse ToResponse(
        RiderDelivery delivery,
        decimal price,
        Features.User.UserAddress? address) => new(
        delivery.OrderId,
        delivery.RiderId,
        delivery.Status.ToString(),
        price,
        address?.Address ?? string.Empty,
        address?.ContactName ?? string.Empty,
        address?.PhoneNumber ?? string.Empty,
        delivery.AssignedAtUtc,
        delivery.PickedUpAtUtc,
        delivery.DeliveringAtUtc,
        delivery.CompletedAtUtc);

    private enum DeliveryAction
    {
        Unknown,
        PickUp,
        StartDelivering,
        Complete,
    }
}
