using System.ComponentModel.DataAnnotations;

namespace TakeoutPlatform.Api.Features.Pricing;

public sealed class PricingQuoteRequest
{
    [StringLength(40)]
    public string? CouponCode { get; set; }
}

public sealed record CouponOptionResponse(
    int Id,
    string Code,
    string Name,
    decimal MinimumSpend,
    decimal DiscountAmount,
    DateTimeOffset ExpiresAtUtc,
    int RemainingQuantity);

public sealed record MerchantPricingResponse(
    int MerchantId,
    string MerchantName,
    decimal Subtotal,
    decimal MerchantDiscount,
    decimal TotalAfterMerchantDiscount);

public sealed record PricingResult(
    decimal Subtotal,
    decimal MerchantDiscount,
    decimal CouponDiscount,
    decimal DeliveryFee,
    decimal Total,
    CouponOptionResponse? SelectedCoupon,
    IReadOnlyList<MerchantPricingResponse> Merchants);

public sealed record PricingQuoteOutcome(
    PricingResult? Pricing,
    PricingErrorCode? ErrorCode = null,
    string? Message = null)
{
    public bool Succeeded => Pricing is not null;
}
