namespace TakeoutPlatform.Api.Features.Rider.Assignment;

/// <summary>
/// 骑手派单责任链的处理器接口（Chain of Responsibility）。
/// 链条顺序：Active → Zone → Availability → Capacity → Selection。
/// 每个 Handler 收窄 context.Candidates；若收窄后为空则短路返回对应失败原因，
/// 否则交给下一环；末端 Selection 选出最终骑手。
/// </summary>
public interface IRiderAssignmentHandler
{
    /// <summary>设置链条中的下一环，返回该下一环以便链式装配。</summary>
    IRiderAssignmentHandler SetNext(IRiderAssignmentHandler next);

    /// <summary>处理当前环节；无下一环且筛选成功时产出选中骑手。</summary>
    RiderAssignmentResult Handle(RiderAssignmentContext context);
}

/// <summary>
/// Handler 公共基类，封装链条推进逻辑。
/// </summary>
public abstract class RiderAssignmentHandler : IRiderAssignmentHandler
{
    private IRiderAssignmentHandler? _next;

    public IRiderAssignmentHandler SetNext(IRiderAssignmentHandler next)
    {
        _next = next;
        return next;
    }

    public abstract RiderAssignmentResult Handle(RiderAssignmentContext context);

    /// <summary>交给下一环；若已是末端仍未选出骑手，视为无可用骑手（满载）。</summary>
    protected RiderAssignmentResult Next(RiderAssignmentContext context)
        => _next?.Handle(context)
           ?? RiderAssignmentResult.NoRider(RiderAssignmentFailureReason.AllAtCapacity);
}
