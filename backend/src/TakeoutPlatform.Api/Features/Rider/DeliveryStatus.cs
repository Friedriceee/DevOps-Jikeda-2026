namespace TakeoutPlatform.Api.Features.Rider;

/// <summary>
/// 骑手配送阶段。独立于全局 OrderStatus，使骑手模块内部状态流转自洽、
/// 不依赖 OrderStatus 枚举数值顺序。接单后依次 Assigned → PickedUp → Delivering → Completed。
/// 每一步会同步更新对应的 Order.Status（见 RiderDeliveryService）。
/// </summary>
public enum DeliveryStatus
{
    Assigned = 0,
    PickedUp = 1,
    Delivering = 2,
    Completed = 3,
}
