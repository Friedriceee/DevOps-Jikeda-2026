using System.ComponentModel.DataAnnotations;

namespace TakeoutPlatform.Api.Features.Rider;

/// <summary>可接任务（待派送订单）视图。</summary>
public sealed record AvailableDeliveryResponse(
    int OrderId,
    string Zone,
    decimal Price,
    DateTime OrderTimestamp,
    string DeliveryAddress,
    string ContactName,
    string ContactPhone,
    string KitchenStatus);

/// <summary>骑手自己的配送任务视图。</summary>
public sealed record RiderDeliveryResponse(
    int OrderId,
    int RiderId,
    string Status,
    decimal Price,
    string DeliveryAddress,
    string ContactName,
    string ContactPhone,
    DateTime AssignedAtUtc,
    DateTime? PickedUpAtUtc,
    DateTime? DeliveringAtUtc,
    DateTime? CompletedAtUtc,
    bool ReadyForPickup);

/// <summary>推进配送状态的请求。Action: PickUp / StartDelivering / Complete。</summary>
public sealed class AdvanceDeliveryRequest
{
    [Required]
    [MaxLength(40)]
    public string Action { get; set; } = string.Empty;
}
