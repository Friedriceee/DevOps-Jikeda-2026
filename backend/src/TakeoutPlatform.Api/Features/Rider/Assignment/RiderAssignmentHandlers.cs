namespace TakeoutPlatform.Api.Features.Rider.Assignment;

/// <summary>第 1 环：只保留在职骑手。全部离职 → NoActiveRider。</summary>
public sealed class ActiveRiderHandler : RiderAssignmentHandler
{
    public override RiderAssignmentResult Handle(RiderAssignmentContext context)
    {
        context.Candidates = context.Candidates.Where(rider => rider.IsActive).ToList();
        if (context.Candidates.Count == 0)
            return RiderAssignmentResult.NoRider(RiderAssignmentFailureReason.NoActiveRider);
        return Next(context);
    }
}

/// <summary>第 2 环：只保留与订单同区的骑手。同区无人 → NoRiderInZone。</summary>
public sealed class ZoneRiderHandler : RiderAssignmentHandler
{
    public override RiderAssignmentResult Handle(RiderAssignmentContext context)
    {
        context.Candidates = context.Candidates
            .Where(rider => string.Equals(rider.Zone, context.OrderZone, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (context.Candidates.Count == 0)
            return RiderAssignmentResult.NoRider(RiderAssignmentFailureReason.NoRiderInZone);
        return Next(context);
    }
}

/// <summary>第 3 环：只保留上线（愿意接单）的骑手。都下线 → NoAvailableRider。</summary>
public sealed class AvailabilityRiderHandler : RiderAssignmentHandler
{
    public override RiderAssignmentResult Handle(RiderAssignmentContext context)
    {
        context.Candidates = context.Candidates.Where(rider => rider.IsAvailable).ToList();
        if (context.Candidates.Count == 0)
            return RiderAssignmentResult.NoRider(RiderAssignmentFailureReason.NoAvailableRider);
        return Next(context);
    }
}

/// <summary>第 4 环：只保留未满载的骑手（在途单量 &lt; 容量）。都满载 → AllAtCapacity。</summary>
public sealed class CapacityRiderHandler : RiderAssignmentHandler
{
    public override RiderAssignmentResult Handle(RiderAssignmentContext context)
    {
        context.Candidates = context.Candidates
            .Where(rider => rider.ActiveDeliveryCount < rider.Capacity)
            .ToList();
        if (context.Candidates.Count == 0)
            return RiderAssignmentResult.NoRider(RiderAssignmentFailureReason.AllAtCapacity);
        return Next(context);
    }
}

/// <summary>
/// 第 5 环（末端）：从剩余候选中选剩余容量最大者（负载均衡），平手取 Id 最小（结果确定）。
/// </summary>
public sealed class SelectionRiderHandler : RiderAssignmentHandler
{
    public override RiderAssignmentResult Handle(RiderAssignmentContext context)
    {
        var selected = context.Candidates
            .OrderByDescending(rider => rider.Capacity - rider.ActiveDeliveryCount)
            .ThenBy(rider => rider.Id)
            .FirstOrDefault();

        return selected is null
            ? RiderAssignmentResult.NoRider(RiderAssignmentFailureReason.AllAtCapacity)
            : RiderAssignmentResult.Assigned(selected);
    }
}

/// <summary>
/// 责任链装配器。固定顺序 Active → Zone → Availability → Capacity → Selection。
/// </summary>
public static class RiderAssignmentChain
{
    public static IRiderAssignmentHandler Build()
    {
        var active = new ActiveRiderHandler();
        var zone = new ZoneRiderHandler();
        var availability = new AvailabilityRiderHandler();
        var capacity = new CapacityRiderHandler();
        var selection = new SelectionRiderHandler();

        active.SetNext(zone).SetNext(availability).SetNext(capacity).SetNext(selection);
        return active;
    }
}
