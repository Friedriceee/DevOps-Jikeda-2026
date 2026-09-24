namespace TakeoutPlatform.Api.Features.Order;

/// <summary>
/// 订单-优惠券关联表。对应旧项目 OrderCouponDB。
/// 仅当下单时使用了优惠券才会创建该记录。
/// 本次不实现优惠券体系，故不建 UserCoupon 实体导航，仅保留字段。
/// </summary>
public class OrderCoupon
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public int CouponId { get; set; }
    public DateTime ExpirationDate { get; set; }

    public Order Order { get; set; } = null!;
}
