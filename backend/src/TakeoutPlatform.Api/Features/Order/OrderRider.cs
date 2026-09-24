namespace TakeoutPlatform.Api.Features.Order;

/// <summary>
/// 订单-骑手关联表。对应旧项目 OrderRiderDB。
/// 创建订单时先记录派送费，RiderId 可空（尚未接单）。
/// 本次不实现骑手体系，故不建 Rider 实体导航。
/// </summary>
public class OrderRider
{
    public int OrderId { get; set; }
    public int? RiderId { get; set; }
    public decimal RiderPrice { get; set; }

    public Order Order { get; set; } = null!;
}
