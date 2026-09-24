namespace TakeoutPlatform.Api.Features.Order;

/// <summary>
/// 订单-菜品明细表。对应旧项目 OrderDishDB。
/// 复合主键 (OrderId, MerchantId, DishId)。
/// </summary>
public class OrderDish
{
    public int OrderId { get; set; }
    public int MerchantId { get; set; }
    public int DishId { get; set; }
    public int DishNum { get; set; }

    public Order Order { get; set; } = null!;
}
