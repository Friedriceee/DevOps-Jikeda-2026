using OrderEntity = TakeoutPlatform.Api.Features.Order.Order;

namespace TakeoutPlatform.Api.Features.Checkout;

public enum PaymentStatus
{
    Succeeded = 1,
}

/// <summary>
/// One successful simulated payment. A payment row only exists for orders that
/// were actually paid: declined attempts roll back and leave no order behind.
///
/// (UserId, IdempotencyKey) is unique, so a repeated request cannot create a
/// second order or a second payment, even when two identical requests race.
/// </summary>
public sealed class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>SHA-256 of the normalised request; detects a key reused with a different payload.</summary>
    public string RequestHash { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; } = PaymentStatus.Succeeded;
    public string Reference { get; set; } = string.Empty;

    /// <summary>The server-calculated PricingResult at the moment of payment, so a replay returns the same body.</summary>
    public string PricingSnapshotJson { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public OrderEntity Order { get; set; } = null!;
}
