namespace TakeoutPlatform.Api.Features.Inventory;

public sealed class InventoryReservation
{
    public int OrderId { get; set; }
    public int MerchantId { get; set; }
    public int DishId { get; set; }
    public int Quantity { get; set; }
    public bool IsReleased { get; set; }
    public DateTimeOffset ReservedAtUtc { get; set; }
    public DateTimeOffset? ReleasedAtUtc { get; set; }
}
