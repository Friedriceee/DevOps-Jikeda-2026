using Microsoft.EntityFrameworkCore;
using TakeoutPlatform.Api.Data;

namespace TakeoutPlatform.Api.Features.Pricing;

public sealed class PricingService
{
    public const decimal DefaultDeliveryFee = 5m;

    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly IReadOnlyDictionary<DiscountStrategyKind, IDiscountStrategy> _strategies;

    public PricingService(
        AppDbContext db,
        TimeProvider timeProvider,
        IEnumerable<IDiscountStrategy> strategies)
    {
        _db = db;
        _timeProvider = timeProvider;
        _strategies = strategies.ToDictionary(strategy => strategy.Kind);
    }

    public async Task<IReadOnlyList<CouponOptionResponse>> ListCouponsAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var allocations = await _db.CustomerCoupons
            .AsNoTracking()
            .Include(allocation => allocation.Coupon)
            .Where(allocation => allocation.UserId == userId
                && allocation.UsedQuantity < allocation.IssuedQuantity
                && allocation.Coupon.IsActive)
            .ToListAsync(cancellationToken);

        // SQLite (used by tests) cannot translate DateTimeOffset ordering. The
        // active coupon set is intentionally small, so validity is filtered in
        // memory while PostgreSQL and SQLite keep identical behavior.
        return allocations
            .Where(allocation => allocation.Coupon.StartsAtUtc <= now && allocation.Coupon.ExpiresAtUtc > now)
            .OrderBy(allocation => allocation.Coupon.MinimumSpend)
            .ThenByDescending(allocation => allocation.Coupon.DiscountAmount)
            .Select(allocation => ToCouponResponse(
                allocation.Coupon,
                allocation.IssuedQuantity - allocation.UsedQuantity))
            .ToList();
    }

    public async Task<PricingQuoteOutcome> QuoteAsync(
        int userId,
        PricingQuoteRequest request,
        CancellationToken cancellationToken)
    {
        var rows = await _db.CartItems
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderBy(item => item.MerchantId)
            .ThenBy(item => item.Id)
            .Select(item => new CartPricingRow(
                item.MerchantId,
                item.Dish.Merchant.Name,
                item.Dish.Price,
                item.DishNum))
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return Failure(PricingErrorCode.EmptyCart, "Add at least one item before checking out.");
        }

        var merchantIds = rows.Select(row => row.MerchantId).Distinct().ToList();
        var offers = await _db.SpecialOffers
            .AsNoTracking()
            .Where(offer => merchantIds.Contains(offer.MerchantId))
            .ToListAsync(cancellationToken);
        var merchantStrategy = Strategy(DiscountStrategyKind.MerchantOffer);
        var now = _timeProvider.GetUtcNow();

        var merchants = rows
            .GroupBy(row => new { row.MerchantId, row.MerchantName })
            .Select(group =>
            {
                var subtotal = Money.Round(group.Sum(row => Money.Round(row.UnitPrice * row.Quantity)));
                var decision = merchantStrategy.Apply(new DiscountContext(
                    subtotal,
                    now,
                    offers.Where(offer => offer.MerchantId == group.Key.MerchantId).ToList()));
                return new MerchantPricingResponse(
                    group.Key.MerchantId,
                    group.Key.MerchantName,
                    subtotal,
                    decision.Amount,
                    Money.Round(subtotal - decision.Amount));
            })
            .ToList();

        var subtotal = Money.Round(merchants.Sum(merchant => merchant.Subtotal));
        var merchantDiscount = Money.Round(merchants.Sum(merchant => merchant.MerchantDiscount));
        var discountedFoodTotal = Money.Round(subtotal - merchantDiscount);
        PlatformCoupon? selectedCoupon = null;
        var couponDiscount = 0m;

        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var normalizedCode = request.CouponCode.Trim().ToUpperInvariant();
            selectedCoupon = await _db.PlatformCoupons
                .AsNoTracking()
                .SingleOrDefaultAsync(coupon => coupon.Code == normalizedCode, cancellationToken);
            if (selectedCoupon is null)
                return Failure(PricingErrorCode.CouponNotFound, "Coupon code was not found.");

            var allocation = await _db.CustomerCoupons.AsNoTracking()
                .SingleOrDefaultAsync(item => item.UserId == userId && item.CouponId == selectedCoupon.Id, cancellationToken);
            if (allocation is null)
                return Failure(PricingErrorCode.CouponNotOwned, "This coupon has not been issued to your account.");
            if (allocation.UsedQuantity >= allocation.IssuedQuantity)
                return Failure(PricingErrorCode.CouponAlreadyUsed, "You have no remaining uses for this coupon.");

            var decision = Strategy(DiscountStrategyKind.PlatformCoupon).Apply(
                new DiscountContext(discountedFoodTotal, now, Coupon: selectedCoupon));
            if (!decision.IsValid)
                return Failure(decision.ErrorCode!.Value, decision.Message!);

            couponDiscount = decision.Amount;
        }

        var deliveryFee = Money.Round(DefaultDeliveryFee);
        var total = Money.Round(Math.Max(0m, discountedFoodTotal - couponDiscount) + deliveryFee);
        return new PricingQuoteOutcome(new PricingResult(
            subtotal,
            merchantDiscount,
            couponDiscount,
            deliveryFee,
            total,
            selectedCoupon is null ? null : ToCouponResponse(
                selectedCoupon,
                (await _db.CustomerCoupons.AsNoTracking()
                    .Where(item => item.UserId == userId && item.CouponId == selectedCoupon.Id)
                    .Select(item => item.IssuedQuantity - item.UsedQuantity)
                    .SingleAsync(cancellationToken))),
            merchants));
    }

    private IDiscountStrategy Strategy(DiscountStrategyKind kind) =>
        _strategies.TryGetValue(kind, out var strategy)
            ? strategy
            : throw new InvalidOperationException($"Discount strategy {kind} is not registered.");

    private static PricingQuoteOutcome Failure(PricingErrorCode code, string message) =>
        new(null, code, message);

    private static CouponOptionResponse ToCouponResponse(PlatformCoupon coupon, int remainingQuantity) => new(
        coupon.Id,
        coupon.Code,
        coupon.Name,
        coupon.MinimumSpend,
        coupon.DiscountAmount,
        coupon.ExpiresAtUtc,
        remainingQuantity);

    private sealed record CartPricingRow(
        int MerchantId,
        string MerchantName,
        decimal UnitPrice,
        int Quantity);
}
