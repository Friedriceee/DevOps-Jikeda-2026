using System.ComponentModel.DataAnnotations;
using TakeoutPlatform.Api.Features.Order;
using TakeoutPlatform.Api.Features.Pricing;

namespace TakeoutPlatform.Api.Features.Checkout;

/// <summary>
/// Checkout input. It deliberately carries no user id, items, prices or totals:
/// identity comes from the JWT, items from the persisted cart and every amount
/// from <see cref="PricingService"/>. Unknown JSON members are ignored.
/// </summary>
public sealed class CheckoutRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "addressId must be greater than 0.")]
    public int AddressId { get; set; }

    [StringLength(40, ErrorMessage = "couponCode must be at most 40 characters.")]
    public string? CouponCode { get; set; }

    [Range(0, 1, ErrorMessage = "needUtensils must be 0 or 1.")]
    public int NeedUtensils { get; set; }

    [Required(ErrorMessage = "paymentMethod is required.")]
    [StringLength(40, ErrorMessage = "paymentMethod must be at most 40 characters.")]
    public string? PaymentMethod { get; set; } = SimulatedPaymentGateway.ApproveMethod;
}

public sealed record CheckoutResponse(
    int OrderId,
    OrderStatus Status,
    decimal AmountPaid,
    int PaymentId,
    string PaymentReference,
    string PaymentMethod,
    DateTime PaidAtUtc,
    bool Replayed,
    PricingResult Pricing,
    IReadOnlyList<OrderDishResponse> Dishes);

public enum CheckoutOutcome
{
    Success,
    Replayed,
    InvalidIdempotencyKey,
    IdempotencyKeyReused,
    UnsupportedPaymentMethod,
    AddressNotFound,
    PricingFailed,
    CartChanged,
    InsufficientStock,
    ItemUnavailable,
    PaymentDeclined,
}

/// <summary>Stable, machine-readable error codes returned in <c>ApiResult.Code</c>.</summary>
public static class CheckoutErrorCodes
{
    public const string InvalidIdempotencyKey = nameof(CheckoutOutcome.InvalidIdempotencyKey);
    public const string IdempotencyKeyReused = nameof(CheckoutOutcome.IdempotencyKeyReused);
    public const string UnsupportedPaymentMethod = nameof(CheckoutOutcome.UnsupportedPaymentMethod);
    public const string AddressNotFound = nameof(CheckoutOutcome.AddressNotFound);
    public const string CartChanged = nameof(CheckoutOutcome.CartChanged);
    public const string InsufficientStock = nameof(CheckoutOutcome.InsufficientStock);
    public const string ItemUnavailable = nameof(CheckoutOutcome.ItemUnavailable);
    public const string PaymentDeclined = nameof(CheckoutOutcome.PaymentDeclined);
}

public sealed record CheckoutResult(
    CheckoutOutcome Outcome,
    CheckoutResponse? Response = null,
    string? Message = null,
    string? Code = null)
{
    public bool Succeeded => Outcome is CheckoutOutcome.Success or CheckoutOutcome.Replayed;
}
