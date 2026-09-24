namespace TakeoutPlatform.Api.Features.Order;

/// <summary>
/// 订单主表。对应旧项目 OrderDB。
/// 保留旧项目的五表结构，主键改为自增 Id（旧项目手动 AssignOrderId）。
/// State 用 OrderStatus 枚举表达，DB 中仍存 int。
/// </summary>
public class Order
{
    public int Id { get; set; }
    public decimal Price { get; set; }
    public DateTime OrderTimestamp { get; set; }
    public DateTime? ExpectedTimeOfArrival { get; set; }
    public DateTime? RealTimeOfArrival { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public int NeedUtensils { get; set; } = 1;  // 0=不需要餐具, 1=需要餐具
    public int AddressId { get; set; }
    public int? MerchantRating { get; set; }
    public int? RiderRating { get; set; }
    public string? Comment { get; set; }

    // 五表结构的导航属性
    public OrderUser? OrderUser { get; set; }
    public OrderRider? OrderRider { get; set; }
    public OrderCoupon? OrderCoupon { get; set; }
    public ICollection<OrderDish> OrderDishes { get; set; } = new List<OrderDish>();
}
