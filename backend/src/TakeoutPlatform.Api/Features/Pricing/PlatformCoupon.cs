namespace TakeoutPlatform.Api.Features.Pricing;

public sealed class PlatformCoupon
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal MinimumSpend { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public bool IsActive { get; set; }
}
