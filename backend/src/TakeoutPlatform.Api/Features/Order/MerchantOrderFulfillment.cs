namespace TakeoutPlatform.Api.Features.Order;

/// <summary>
/// Tracks one merchant's fulfilment state for an order. An order can contain
/// dishes from multiple merchants, so fulfilment state cannot safely live only
/// on the order aggregate.
/// </summary>
public sealed class MerchantOrderFulfillment
{
    public int OrderId { get; set; }
    public int MerchantId { get; set; }
    public MerchantFulfillmentStatus Status { get; set; } = MerchantFulfillmentStatus.Confirmed;
    public string? RejectionReason { get; set; }
    public bool InventoryRestored { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Order Order { get; set; } = null!;
}

public enum MerchantFulfillmentStatus
{
    Confirmed = 0,
    Accepted = 1,
    Preparing = 2,
    ReadyForDelivery = 3,
    Rejected = 4,
}
