namespace TakeoutPlatform.Api.Features.Rider.Assignment;

/// <summary>
/// 责任链筛选的失败原因（无人可派时说明卡在哪一环，便于前端/日志诊断）。
/// </summary>
public enum RiderAssignmentFailureReason
{
    None = 0,
    NoActiveRider,       // 没有在职骑手
    NoRiderInZone,       // 所在区域没有骑手
    NoAvailableRider,    // 区域内骑手都已下线
    AllAtCapacity,       // 可用骑手都已满载
}

/// <summary>
/// 责任链最终产出：选中的骑手，或无人可派 + 原因。
/// </summary>
public sealed class RiderAssignmentResult
{
    public bool Success { get; private init; }
    public Rider? SelectedRider { get; private init; }
    public RiderAssignmentFailureReason FailureReason { get; private init; }

    public static RiderAssignmentResult Assigned(Rider rider) => new()
    {
        Success = true,
        SelectedRider = rider,
        FailureReason = RiderAssignmentFailureReason.None,
    };

    public static RiderAssignmentResult NoRider(RiderAssignmentFailureReason reason) => new()
    {
        Success = false,
        SelectedRider = null,
        FailureReason = reason,
    };
}
