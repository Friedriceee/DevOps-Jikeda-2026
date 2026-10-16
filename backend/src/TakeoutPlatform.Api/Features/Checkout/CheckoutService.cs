using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Inventory;
using TakeoutPlatform.Api.Features.Order;
using TakeoutPlatform.Api.Features.Pricing;
using OrderEntity = TakeoutPlatform.Api.Features.Order.Order;

namespace TakeoutPlatform.Api.Features.Checkout;

/// <summary>
/// Turns the signed-in customer's persisted cart into a paid order.
///
/// Trust boundaries
///  - identity comes from the JWT (the caller passes <c>userId</c>), never from the body;
///  - items come from the cart table, amounts from <see cref="PricingService"/>;
///  - nothing the client sends can change a price.
///
/// Consistency: everything below happens in one database transaction —
/// claim the cart, create the order rows, reserve stock, charge, mark paid, store
/// the payment. A decline or any failure rolls all of it back (stock and cart
/// included), so there is no half-paid state to clean up.
///
/// Idempotency: each request carries an <c>Idempotency-Key</c>. (UserId, key) is
/// unique in <c>Payments</c>. A repeat returns the stored result without touching
/// stock or state; the same key with a different payload is rejected. Racing
/// duplicates are serialised by the cart claim (a second DELETE of the same rows
/// affects nothing) and by the unique index, and the loser returns the winner's result.
/// </summary>
public sealed class CheckoutService
{
    private static readonly Regex KeyPattern = new("^[A-Za-z0-9_-]{8,64}$", RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly PricingService _pricing;
    private readonly InventoryService _inventory;
    private readonly IPaymentGateway _gateway;
    private readonly TimeProvider _timeProvider;

    public CheckoutService(
        AppDbContext db,
        PricingService pricing,
        InventoryService inventory,
        IPaymentGateway gateway,
        TimeProvider timeProvider)
    {
        _db = db;
        _pricing = pricing;
        _inventory = inventory;
        _gateway = gateway;
        _timeProvider = timeProvider;
    }

    public async Task<CheckoutResult> CheckoutAsync(
        int userId,
        string? idempotencyKey,
        CheckoutRequest request,
        CancellationToken cancellationToken)
    {
        if (idempotencyKey is null || !KeyPattern.IsMatch(idempotencyKey))
        {
            return Fail(
                CheckoutOutcome.InvalidIdempotencyKey,
                CheckoutErrorCodes.InvalidIdempotencyKey,
                "Idempotency-Key must be 8-64 characters using letters, digits, '-' or '_'.");
        }

        var method = request.PaymentMethod?.Trim() ?? string.Empty;
        if (!_gateway.SupportsMethod(method))
        {
            return Fail(
                CheckoutOutcome.UnsupportedPaymentMethod,
                CheckoutErrorCodes.UnsupportedPaymentMethod,
                "This payment method is not supported.");
        }

        var couponCode = string.IsNullOrWhiteSpace(request.CouponCode)
            ? null
            : request.CouponCode.Trim().ToUpperInvariant();
        var requestHash = HashRequest(request.AddressId, couponCode, request.NeedUtensils, method);

        var replay = await FindReplayAsync(userId, idempotencyKey, requestHash, cancellationToken);
        if (replay is not null)
            return replay;

        CheckoutResult result;
        try
        {
            result = await ExecuteAsync(
                userId, idempotencyKey, request, couponCode, method, requestHash, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A concurrent request with the same key committed first (unique index).
            _db.ChangeTracker.Clear();
            replay = await FindReplayAsync(userId, idempotencyKey, requestHash, cancellationToken);
            if (replay is not null)
                return replay;
            throw;
        }

        if (!result.Succeeded)
        {
            // This attempt failed. If a request with the same key committed in the
            // meantime (the cart is already consumed, so this one saw an empty or
            // changed cart), the right answer is that request's result, not an error.
            replay = await FindReplayAsync(userId, idempotencyKey, requestHash, cancellationToken);
            if (replay is not null)
                return replay;
        }

        return result;
    }

    private async Task<CheckoutResult> ExecuteAsync(
        int userId,
        string idempotencyKey,
        CheckoutRequest request,
        string? couponCode,
        string method,
        string requestHash,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var ownsAddress = await _db.UserAddresses.AsNoTracking().AnyAsync(
            address => address.Id == request.AddressId && address.UserId == userId,
            cancellationToken);
        if (!ownsAddress)
        {
            return await AbortAsync(transaction, Fail(
                CheckoutOutcome.AddressNotFound,
                CheckoutErrorCodes.AddressNotFound,
                "Delivery address was not found."));
        }

        var cart = await _db.CartItems.AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => new CartLine(item.Id, item.MerchantId, item.DishId, item.DishNum, item.Dish.Price))
            .ToListAsync(cancellationToken);

        var quote = await _pricing.QuoteAsync(
            userId, new PricingQuoteRequest { CouponCode = couponCode }, cancellationToken);
        if (!quote.Succeeded)
        {
            return await AbortAsync(transaction, Fail(
                CheckoutOutcome.PricingFailed,
                quote.ErrorCode!.Value.ToString(),
                quote.Message!));
        }

        var pricing = quote.Pricing!;

        // The quote and the cart rows were read separately. If the cart moved in
        // between (another tab, another request), refuse rather than charge for
        // something different from what was priced.
        var cartSubtotal = Money.Round(cart.Sum(line => Money.Round(line.UnitPrice * line.Quantity)));
        if (cart.Count == 0 || cartSubtotal != pricing.Subtotal)
        {
            return await AbortAsync(transaction, CartChanged());
        }

        // Claim the cart. A concurrent checkout that already consumed these rows
        // makes this delete affect fewer rows, which we treat as a conflict.
        var cartIds = cart.Select(line => line.Id).ToArray();
        var claimed = await _db.CartItems
            .Where(item => item.UserId == userId && cartIds.Contains(item.Id))
            .ExecuteDeleteAsync(cancellationToken);
        if (claimed != cartIds.Length)
        {
            return await AbortAsync(transaction, CartChanged());
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var order = new OrderEntity
        {
            Price = pricing.Total,
            OrderTimestamp = now,
            Status = OrderStatus.Pending,
            NeedUtensils = request.NeedUtensils,
            AddressId = request.AddressId,
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        var lines = cart
            .GroupBy(line => new { line.MerchantId, line.DishId })
            .Select(group => new OrderDish
            {
                OrderId = order.Id,
                MerchantId = group.Key.MerchantId,
                DishId = group.Key.DishId,
                DishNum = group.Sum(line => line.Quantity),
            })
            .ToList();

        _db.OrderUsers.Add(new OrderUser { OrderId = order.Id, UserId = userId });
        _db.OrderRiders.Add(new OrderRider { OrderId = order.Id, RiderPrice = pricing.DeliveryFee });
        if (pricing.SelectedCoupon is { } coupon)
        {
            var allocation = await _db.CustomerCoupons
                .SingleOrDefaultAsync(item => item.UserId == userId && item.CouponId == coupon.Id, cancellationToken);
            if (allocation is null || allocation.UsedQuantity >= allocation.IssuedQuantity)
            {
                return await AbortAsync(transaction, Fail(
                    CheckoutOutcome.PricingFailed,
                    PricingErrorCode.CouponAlreadyUsed.ToString(),
                    "You have no remaining uses for this coupon."));
            }
            allocation.UsedQuantity += 1;
            _db.OrderCoupons.Add(new OrderCoupon
            {
                OrderId = order.Id,
                UserId = userId,
                CouponId = coupon.Id,
                ExpirationDate = coupon.ExpiresAtUtc.UtcDateTime,
            });
        }
        _db.OrderDishes.AddRange(lines);
        await _db.SaveChangesAsync(cancellationToken);

        var reservation = await _inventory.ReserveAsync(
            order.Id,
            lines.Select(line => new InventoryReservationItem(line.MerchantId, line.DishId, line.DishNum)).ToArray(),
            cancellationToken);
        if (!reservation.Succeeded)
        {
            return await AbortAsync(transaction, reservation.Outcome switch
            {
                InventoryOutcome.InsufficientStock => Fail(
                    CheckoutOutcome.InsufficientStock,
                    CheckoutErrorCodes.InsufficientStock,
                    reservation.Message ?? "There is not enough stock for one of the items."),
                _ => Fail(
                    CheckoutOutcome.ItemUnavailable,
                    CheckoutErrorCodes.ItemUnavailable,
                    reservation.Message ?? "One of the items is no longer available."),
            });
        }

        var charge = await _gateway.ChargeAsync(
            new PaymentGatewayRequest(userId, pricing.Total, method, idempotencyKey),
            cancellationToken);
        if (!charge.Approved)
        {
            return await AbortAsync(transaction, Fail(
                CheckoutOutcome.PaymentDeclined,
                CheckoutErrorCodes.PaymentDeclined,
                charge.DeclineReason ?? "The payment was declined."));
        }

        order.Status = OrderStatus.Paid;
        var payment = new Payment
        {
            OrderId = order.Id,
            UserId = userId,
            IdempotencyKey = idempotencyKey,
            RequestHash = requestHash,
            Amount = pricing.Total,
            Method = method,
            Status = PaymentStatus.Succeeded,
            Reference = charge.Reference,
            PricingSnapshotJson = JsonSerializer.Serialize(pricing),
            CreatedAtUtc = now,
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CheckoutResult(
            CheckoutOutcome.Success,
            BuildResponse(payment, order.Status, lines, pricing, replayed: false));
    }

    private async Task<CheckoutResult?> FindReplayAsync(
        int userId,
        string idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken)
    {
        var payment = await _db.Payments.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.UserId == userId && item.IdempotencyKey == idempotencyKey,
                cancellationToken);
        if (payment is null)
            return null;

        if (payment.RequestHash != requestHash)
        {
            return Fail(
                CheckoutOutcome.IdempotencyKeyReused,
                CheckoutErrorCodes.IdempotencyKeyReused,
                "This Idempotency-Key was already used for a different checkout request.");
        }

        var order = await _db.Orders.AsNoTracking()
            .Include(item => item.OrderDishes)
            .SingleAsync(item => item.Id == payment.OrderId, cancellationToken);
        var pricing = JsonSerializer.Deserialize<PricingResult>(payment.PricingSnapshotJson)!;

        return new CheckoutResult(
            CheckoutOutcome.Replayed,
            BuildResponse(payment, order.Status, order.OrderDishes, pricing, replayed: true));
    }

    private static CheckoutResponse BuildResponse(
        Payment payment,
        OrderStatus status,
        IEnumerable<OrderDish> lines,
        PricingResult pricing,
        bool replayed) => new(
            payment.OrderId,
            status,
            payment.Amount,
            payment.Id,
            payment.Reference,
            payment.Method,
            DateTime.SpecifyKind(payment.CreatedAtUtc, DateTimeKind.Utc),
            replayed,
            pricing,
            lines.OrderBy(line => line.MerchantId).ThenBy(line => line.DishId)
                .Select(line => new OrderDishResponse(line.MerchantId, line.DishId, line.DishNum))
                .ToList());

    private async Task<CheckoutResult> AbortAsync(IDbContextTransaction transaction, CheckoutResult result)
    {
        await transaction.RollbackAsync(CancellationToken.None);
        // The rolled-back entities must not leak into later queries on this context.
        _db.ChangeTracker.Clear();
        return result;
    }

    private static CheckoutResult CartChanged() => Fail(
        CheckoutOutcome.CartChanged,
        CheckoutErrorCodes.CartChanged,
        "Your cart changed while checking out. Review it and try again.");

    private static CheckoutResult Fail(CheckoutOutcome outcome, string code, string message) =>
        new(outcome, null, message, code);

    private static string HashRequest(int addressId, string? couponCode, int needUtensils, string method) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{addressId}|{couponCode}|{needUtensils}|{method}")));

    private sealed record CartLine(int Id, int MerchantId, int DishId, int Quantity, decimal UnitPrice);
}
