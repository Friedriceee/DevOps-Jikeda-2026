namespace TakeoutPlatform.Api.Features.Pricing;

/// <summary>
/// A coupon allocation owned by one customer. Quantities are issued by an
/// administrator and redeemed atomically during checkout.
/// </summary>
public sealed class CustomerCoupon
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CouponId { get; set; }
    public int IssuedQuantity { get; set; }
    public int UsedQuantity { get; set; }
    public DateTime IssuedAtUtc { get; set; }

    public PlatformCoupon Coupon { get; set; } = null!;
}
