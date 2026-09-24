namespace TakeoutPlatform.Api.Features.Order;

/// <summary>
/// 订单-用户关联表。对应旧项目 OrderUserDB。
/// 以 OrderId 为主键，与 Order 一对一。
/// </summary>
public class OrderUser
{
    public int OrderId { get; set; }
    public int UserId { get; set; }

    public Order Order { get; set; } = null!;
}
