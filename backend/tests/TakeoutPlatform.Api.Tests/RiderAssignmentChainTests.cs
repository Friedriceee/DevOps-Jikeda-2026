using NUnit.Framework;
using TakeoutPlatform.Api.Features.Rider;
using TakeoutPlatform.Api.Features.Rider.Assignment;

namespace TakeoutPlatform.Api.Tests;

/// <summary>
/// 骑手派单责任链单元测试（纯内存，不依赖数据库）。
/// 覆盖链条每一环的短路失败，以及末端选择策略。
/// 链条：Active → Zone → Availability → Capacity → Selection。
/// </summary>
public class RiderAssignmentChainTests
{
    private const string OrderZone = "ZoneA";

    private static Rider NewRider(
        int id,
        bool isActive = true,
        string zone = OrderZone,
        bool isAvailable = true,
        int capacity = 3,
        int activeCount = 0) => new()
    {
        Id = id,
        Name = $"Rider{id}",
        IsActive = isActive,
        Zone = zone,
        IsAvailable = isAvailable,
        Capacity = capacity,
        ActiveDeliveryCount = activeCount,
    };

    private static RiderAssignmentResult Run(params Rider[] riders)
    {
        var context = new RiderAssignmentContext(OrderZone, riders);
        return RiderAssignmentChain.Build().Handle(context);
    }

    // ---------- 各环短路失败 ----------

    [Test]
    public void No_active_rider_fails_with_NoActiveRider()
    {
        var result = Run(NewRider(1, isActive: false), NewRider(2, isActive: false));

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(RiderAssignmentFailureReason.NoActiveRider));
    }

    [Test]
    public void No_rider_in_zone_fails_with_NoRiderInZone()
    {
        var result = Run(NewRider(1, zone: "ZoneB"), NewRider(2, zone: "ZoneC"));

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(RiderAssignmentFailureReason.NoRiderInZone));
    }

    [Test]
    public void No_available_rider_fails_with_NoAvailableRider()
    {
        var result = Run(NewRider(1, isAvailable: false), NewRider(2, isAvailable: false));

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(RiderAssignmentFailureReason.NoAvailableRider));
    }

    [Test]
    public void All_at_capacity_fails_with_AllAtCapacity()
    {
        var result = Run(
            NewRider(1, capacity: 2, activeCount: 2),
            NewRider(2, capacity: 3, activeCount: 3));

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(RiderAssignmentFailureReason.AllAtCapacity));
    }

    // ---------- 正常选择 ----------

    [Test]
    public void Selects_rider_with_most_free_capacity()
    {
        // r1 剩余 1 (cap3,active2)，r2 剩余 3 (cap3,active0)，r3 剩余 2 (cap4,active2)
        var result = Run(
            NewRider(1, capacity: 3, activeCount: 2),
            NewRider(2, capacity: 3, activeCount: 0),
            NewRider(3, capacity: 4, activeCount: 2));

        Assert.That(result.Success, Is.True);
        Assert.That(result.SelectedRider, Is.Not.Null);
        Assert.That(result.SelectedRider!.Id, Is.EqualTo(2));
    }

    [Test]
    public void Ignores_riders_from_other_zones_when_selecting()
    {
        // 同区只有 r2 可选，尽管 r1 在别区且更空闲
        var result = Run(
            NewRider(1, zone: "ZoneB", capacity: 5, activeCount: 0),
            NewRider(2, zone: OrderZone, capacity: 3, activeCount: 1));

        Assert.That(result.Success, Is.True);
        Assert.That(result.SelectedRider!.Id, Is.EqualTo(2));
    }

    [Test]
    public void Full_pipeline_filters_inactive_wrong_zone_unavailable_and_full()
    {
        var result = Run(
            NewRider(1, isActive: false),                      // 被 Active 淘汰
            NewRider(2, zone: "ZoneB"),                         // 被 Zone 淘汰
            NewRider(3, isAvailable: false),                   // 被 Availability 淘汰
            NewRider(4, capacity: 2, activeCount: 2),          // 被 Capacity 淘汰(满载)
            NewRider(5, capacity: 3, activeCount: 1));         // 唯一合格 → 选中

        Assert.That(result.Success, Is.True);
        Assert.That(result.SelectedRider!.Id, Is.EqualTo(5));
    }
}
