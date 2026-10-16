using TakeoutPlatform.Api.Features.Merchant;

namespace TakeoutPlatform.Api.Features.Pricing;

public enum DiscountStrategyKind
{
    MerchantOffer,
    PlatformCoupon,
}

public enum PricingErrorCode
{
    EmptyCart,
    CouponNotFound,
    CouponInactive,
    CouponNotStarted,
    CouponExpired,
    CouponNotOwned,
    CouponAlreadyUsed,
    MinimumSpendNotMet,
}

public sealed record DiscountContext(
    decimal Subtotal,
    DateTimeOffset Now,
    IReadOnlyList<SpecialOffer>? MerchantOffers = null,
    PlatformCoupon? Coupon = null);

public sealed record DiscountDecision(
    decimal Amount,
    PricingErrorCode? ErrorCode = null,
    string? Message = null)
{
    public bool IsValid => ErrorCode is null;
}

public interface IDiscountStrategy
{
    DiscountStrategyKind Kind { get; }
    DiscountDecision Apply(DiscountContext context);
}

public sealed class MerchantDiscountStrategy : IDiscountStrategy
{
    public DiscountStrategyKind Kind => DiscountStrategyKind.MerchantOffer;

    public DiscountDecision Apply(DiscountContext context)
    {
        var subtotal = Money.Round(Math.Max(0m, context.Subtotal));
        var discount = context.MerchantOffers?
            .Where(offer => offer.MinPrice <= subtotal)
            .Select(offer => offer.AmountRemission)
            .DefaultIfEmpty(0m)
            .Max() ?? 0m;

        return new DiscountDecision(Money.Round(Math.Min(Math.Max(0m, discount), subtotal)));
    }
}

public sealed class PlatformCouponDiscountStrategy : IDiscountStrategy
{
    public DiscountStrategyKind Kind => DiscountStrategyKind.PlatformCoupon;

    public DiscountDecision Apply(DiscountContext context)
    {
        var coupon = context.Coupon
            ?? throw new ArgumentException("A platform coupon is required.", nameof(context));
        var subtotal = Money.Round(Math.Max(0m, context.Subtotal));

        if (!coupon.IsActive)
            return Invalid(PricingErrorCode.CouponInactive, "This coupon is not active.");
        if (context.Now < coupon.StartsAtUtc)
            return Invalid(PricingErrorCode.CouponNotStarted, "This coupon is not available yet.");
        if (context.Now >= coupon.ExpiresAtUtc)
            return Invalid(PricingErrorCode.CouponExpired, "This coupon has expired.");
        if (subtotal < coupon.MinimumSpend)
        {
            return Invalid(
                PricingErrorCode.MinimumSpendNotMet,
                $"Spend at least ¥{coupon.MinimumSpend:0.00} after restaurant discounts to use this coupon.");
        }

        return new DiscountDecision(Money.Round(Math.Min(coupon.DiscountAmount, subtotal)));
    }

    private static DiscountDecision Invalid(PricingErrorCode code, string message) =>
        new(0m, code, message);
}

public static class Money
{
    public static decimal Round(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
}
