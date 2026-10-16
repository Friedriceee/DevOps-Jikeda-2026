using NUnit.Framework;
using TakeoutPlatform.Api.Features.Cart;
using TakeoutPlatform.Api.Features.Merchant;
using TakeoutPlatform.Api.Features.Pricing;

namespace TakeoutPlatform.Api.Tests;

public class PricingServiceTests : ServiceTestBase
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task QuoteAsync_stacks_merchant_discount_then_coupon_then_delivery_fee()
    {
        await using var context = CreateContext();
        context.Dishes.Add(new Dish
        {
            Id = 10,
            MerchantId = MerchantId,
            Name = "Dinner set",
            Price = 50m,
            Inventory = 5,
        });
        context.CartItems.Add(new CartItem
        {
            UserId = 1,
            MerchantId = MerchantId,
            DishId = 10,
            DishNum = 2,
        });
        context.SpecialOffers.Add(new SpecialOffer
        {
            MerchantId = MerchantId,
            MinPrice = 80m,
            AmountRemission = 10m,
        });
        await context.SaveChangesAsync();

        var result = await Service(context).QuoteAsync(
            1,
            new PricingQuoteRequest { CouponCode = "SAVE12" },
            CancellationToken.None);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Pricing!.Subtotal, Is.EqualTo(100m));
        Assert.That(result.Pricing.MerchantDiscount, Is.EqualTo(10m));
        Assert.That(result.Pricing.CouponDiscount, Is.EqualTo(12m));
        Assert.That(result.Pricing.DeliveryFee, Is.EqualTo(5m));
        Assert.That(result.Pricing.Total, Is.EqualTo(83m));
        Assert.That(result.Pricing.SelectedCoupon!.Code, Is.EqualTo("SAVE12"));
    }

    [Test]
    public async Task QuoteAsync_rejects_coupon_when_post_merchant_discount_total_is_below_threshold()
    {
        await using var context = CreateContext();
        context.Dishes.Add(new Dish
        {
            Id = 10,
            MerchantId = MerchantId,
            Name = "Lunch",
            Price = 40m,
            Inventory = 5,
        });
        context.CartItems.Add(new CartItem
        {
            UserId = 1,
            MerchantId = MerchantId,
            DishId = 10,
            DishNum = 2,
        });
        context.SpecialOffers.Add(new SpecialOffer
        {
            MerchantId = MerchantId,
            MinPrice = 80m,
            AmountRemission = 5m,
        });
        await context.SaveChangesAsync();

        var result = await Service(context).QuoteAsync(
            1,
            new PricingQuoteRequest { CouponCode = "SAVE12" },
            CancellationToken.None);

        Assert.That(result.ErrorCode, Is.EqualTo(PricingErrorCode.MinimumSpendNotMet));
        Assert.That(result.Pricing, Is.Null);
    }

    [Test]
    public async Task QuoteAsync_rejects_empty_cart_and_unknown_coupon()
    {
        await using var context = CreateContext();
        var empty = await Service(context).QuoteAsync(
            1, new PricingQuoteRequest(), CancellationToken.None);

        context.Dishes.Add(new Dish
        {
            Id = 10,
            MerchantId = MerchantId,
            Name = "Snack",
            Price = 25m,
            Inventory = 5,
        });
        context.CartItems.Add(new CartItem
        {
            UserId = 1,
            MerchantId = MerchantId,
            DishId = 10,
            DishNum = 1,
        });
        await context.SaveChangesAsync();
        var missingCoupon = await Service(context).QuoteAsync(
            1,
            new PricingQuoteRequest { CouponCode = "NOPE" },
            CancellationToken.None);

        Assert.That(empty.ErrorCode, Is.EqualTo(PricingErrorCode.EmptyCart));
        Assert.That(missingCoupon.ErrorCode, Is.EqualTo(PricingErrorCode.CouponNotFound));
    }

    [Test]
    public async Task ListCouponsAsync_returns_only_current_active_coupons()
    {
        await using var context = CreateContext();
        context.PlatformCoupons.AddRange(
            new PlatformCoupon
            {
                Id = 20,
                Code = "EXPIRED",
                Name = "Expired",
                MinimumSpend = 1m,
                DiscountAmount = 1m,
                StartsAtUtc = Now.AddDays(-2),
                ExpiresAtUtc = Now.AddDays(-1),
                IsActive = true,
            },
            new PlatformCoupon
            {
                Id = 21,
                Code = "DISABLED",
                Name = "Disabled",
                MinimumSpend = 1m,
                DiscountAmount = 1m,
                StartsAtUtc = Now.AddDays(-1),
                ExpiresAtUtc = Now.AddDays(1),
                IsActive = false,
            });
        await context.SaveChangesAsync();

        var coupons = await Service(context).ListCouponsAsync(1, CancellationToken.None);

        Assert.That(coupons.Select(coupon => coupon.Code),
            Is.EquivalentTo(new[] { "WELCOME5", "SAVE12" }));
    }

    [Test]
    public async Task QuoteAsync_rejects_coupon_not_owned_by_the_customer()
    {
        await using var context = CreateContext();
        context.Dishes.Add(new Dish
        {
            Id = 10, MerchantId = MerchantId, Name = "Dinner", Price = 100m, Inventory = 5,
        });
        context.CartItems.Add(new CartItem
        {
            UserId = 1, MerchantId = MerchantId, DishId = 10, DishNum = 1,
        });
        context.CustomerCoupons.RemoveRange(context.CustomerCoupons.Where(item => item.UserId == 1));
        await context.SaveChangesAsync();

        var result = await Service(context).QuoteAsync(
            1, new PricingQuoteRequest { CouponCode = "SAVE12" }, CancellationToken.None);

        Assert.That(result.ErrorCode, Is.EqualTo(PricingErrorCode.CouponNotOwned));
    }

    [Test]
    public async Task ListCouponsAsync_hides_fully_used_allocations()
    {
        await using var context = CreateContext();
        var allocation = context.CustomerCoupons.Single(item => item.UserId == 1 && item.CouponId == 1);
        allocation.UsedQuantity = allocation.IssuedQuantity;
        await context.SaveChangesAsync();

        var coupons = await Service(context).ListCouponsAsync(1, CancellationToken.None);

        Assert.That(coupons.Select(coupon => coupon.Code), Does.Not.Contain("WELCOME5"));
        Assert.That(coupons.Select(coupon => coupon.Code), Does.Contain("SAVE12"));
    }

    private static PricingService Service(Data.AppDbContext context) => new(
        context,
        new FixedTimeProvider(Now),
        new IDiscountStrategy[]
        {
            new MerchantDiscountStrategy(),
            new PlatformCouponDiscountStrategy(),
        });

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
