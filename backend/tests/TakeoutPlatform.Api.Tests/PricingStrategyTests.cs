using NUnit.Framework;
using TakeoutPlatform.Api.Features.Merchant;
using TakeoutPlatform.Api.Features.Pricing;

namespace TakeoutPlatform.Api.Tests;

public class PricingStrategyTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void Merchant_strategy_uses_the_largest_eligible_discount()
    {
        var strategy = new MerchantDiscountStrategy();
        var offers = new[]
        {
            new SpecialOffer { MinPrice = 20m, AmountRemission = 3m },
            new SpecialOffer { MinPrice = 50m, AmountRemission = 8m },
            new SpecialOffer { MinPrice = 100m, AmountRemission = 30m },
        };

        var decision = strategy.Apply(new DiscountContext(60m, Now, offers));

        Assert.That(decision.Amount, Is.EqualTo(8m));
        Assert.That(decision.IsValid, Is.True);
    }

    [Test]
    public void Merchant_strategy_caps_discount_at_subtotal_and_rounds_to_cents()
    {
        var strategy = new MerchantDiscountStrategy();
        var offers = new[] { new SpecialOffer { MinPrice = 0m, AmountRemission = 99m } };

        var decision = strategy.Apply(new DiscountContext(10.005m, Now, offers));

        Assert.That(decision.Amount, Is.EqualTo(10.01m));
    }

    [Test]
    public void Coupon_strategy_applies_after_threshold_and_caps_at_current_food_total()
    {
        var strategy = new PlatformCouponDiscountStrategy();
        var coupon = Coupon(minimumSpend: 20m, amount: 50m);

        var decision = strategy.Apply(new DiscountContext(25m, Now, Coupon: coupon));

        Assert.That(decision.Amount, Is.EqualTo(25m));
        Assert.That(decision.IsValid, Is.True);
    }

    [TestCase(19.99, PricingErrorCode.MinimumSpendNotMet)]
    [TestCase(20.00, null)]
    public void Coupon_strategy_enforces_minimum_spend_boundary(
        decimal subtotal,
        PricingErrorCode? expectedError)
    {
        var decision = new PlatformCouponDiscountStrategy().Apply(
            new DiscountContext(subtotal, Now, Coupon: Coupon(20m, 5m)));

        Assert.That(decision.ErrorCode, Is.EqualTo(expectedError));
    }

    [Test]
    public void Coupon_strategy_rejects_expired_coupon()
    {
        var coupon = Coupon(20m, 5m);
        coupon.ExpiresAtUtc = Now;

        var decision = new PlatformCouponDiscountStrategy().Apply(
            new DiscountContext(30m, Now, Coupon: coupon));

        Assert.That(decision.ErrorCode, Is.EqualTo(PricingErrorCode.CouponExpired));
        Assert.That(decision.Amount, Is.Zero);
    }

    [TestCase(1.004, 1.00)]
    [TestCase(1.005, 1.01)]
    [TestCase(1.006, 1.01)]
    public void Money_rounds_away_from_zero_at_half_cent(decimal input, decimal expected)
    {
        Assert.That(Money.Round(input), Is.EqualTo(expected));
    }

    private static PlatformCoupon Coupon(decimal minimumSpend, decimal amount) => new()
    {
        Code = "TEST",
        Name = "Test coupon",
        MinimumSpend = minimumSpend,
        DiscountAmount = amount,
        StartsAtUtc = Now.AddDays(-1),
        ExpiresAtUtc = Now.AddDays(1),
        IsActive = true,
    };

}
