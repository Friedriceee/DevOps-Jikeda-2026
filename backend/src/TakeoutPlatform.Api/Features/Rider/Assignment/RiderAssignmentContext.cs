namespace TakeoutPlatform.Api.Features.Rider.Assignment;

/// <summary>
/// 责任链上下文。承载一次派单筛选的输入与中间态：
/// - OrderZone：订单所属配送区域（由 Service 根据订单地址解析后传入，责任链不碰数据库）。
/// - Candidates：候选骑手集合，随链条逐级收窄。
///
/// 责任链各 Handler 只读 OrderZone、过滤/排序 Candidates，保持纯内存、可独立单测。
/// </summary>
public sealed class RiderAssignmentContext
{
    public RiderAssignmentContext(string orderZone, IEnumerable<Rider> candidates)
    {
        OrderZone = orderZone;
        Candidates = candidates.ToList();
    }

    public string OrderZone { get; }

    /// <summary>当前候选骑手。每个 Handler 可就地收窄或重排。</summary>
    public IReadOnlyList<Rider> Candidates { get; set; }
}
