using System.ComponentModel.DataAnnotations;

namespace TakeoutPlatform.Api.Features.Order;

public sealed class MerchantOrderTransitionRequest
{
    [Required]
    [MaxLength(40)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Reason { get; set; }
}

public sealed record MerchantOrderResponse(
    int OrderId,
    int MerchantId,
    int CustomerId,
    DateTime OrderTimestamp,
    string Status,
    string? RejectionReason,
    decimal MerchantSubtotal,
    int ItemCount,
    IReadOnlyList<MerchantOrderItemResponse> Items);

public sealed record MerchantOrderItemResponse(
    int DishId,
    string DishName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);
