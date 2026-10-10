using OrderEntity = TakeoutPlatform.Api.Features.Order.Order;

namespace TakeoutPlatform.Api.Features.Rider;

/// <summary>
/// 一次订单配送指派记录。OrderId 唯一，保证一个订单只能被一名骑手接走
/// （数据库唯一约束 + Serializable 事务共同防止并发重复接单）。
/// </summary>
public class RiderDelivery
{
    public int Id { get; set; }

    /// <summary>被配送的订单。唯一，防重复接单。</summary>
    public int OrderId { get; set; }

    /// <summary>接单骑手。</summary>
    public int RiderId { get; set; }

    public DeliveryStatus Status { get; set; } = DeliveryStatus.Assigned;

    public DateTime AssignedAtUtc { get; set; }
    public DateTime? PickedUpAtUtc { get; set; }
    public DateTime? DeliveringAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public Rider Rider { get; set; } = null!;
    public OrderEntity Order { get; set; } = null!;
}
