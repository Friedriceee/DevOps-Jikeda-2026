using System.ComponentModel.DataAnnotations;

namespace TakeoutPlatform.Api.Features.Order;

/// <summary>
/// 创建订单请求。对应旧项目 OrderCreate。
/// 购物车不能为空的规则由 Service 层显式返回业务错误（与旧项目一致），
/// 这里用 [MinLength] 兜底，避免空数组进入组装逻辑。
/// </summary>
public sealed class CreateOrderRequest
{
    [Required(ErrorMessage = "userId 不能为空")]
    [Range(1, int.MaxValue, ErrorMessage = "userId 必须大于 0")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "addressId 不能为空")]
    [Range(1, int.MaxValue, ErrorMessage = "addressId 必须大于 0")]
    public int AddressId { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "price 必须大于 0")]
    public decimal Price { get; set; }

    public DateTime OrderTimestamp { get; set; }

    [Range(0, 1, ErrorMessage = "needUtensils 只能为 0 或 1")]
    public int NeedUtensils { get; set; } = 1;

    [Range(0, int.MaxValue, ErrorMessage = "riderPrice 不能小于 0")]
    public decimal RiderPrice { get; set; }

    // 优惠券：可选，CouponId > 0 时才记录
    public int CouponId { get; set; }
    public DateTime ExpirationDate { get; set; }

    [MinLength(1, ErrorMessage = "购物车不能为空")]
    public CartItem[] ShoppingCart { get; set; } = System.Array.Empty<CartItem>();

    public sealed class CartItem
    {
        [Range(1, int.MaxValue, ErrorMessage = "merchantId 必须大于 0")]
        public int MerchantId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "dishId 必须大于 0")]
        public int DishId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "dishNum 必须大于 0")]
        public int DishNum { get; set; }
    }
}
