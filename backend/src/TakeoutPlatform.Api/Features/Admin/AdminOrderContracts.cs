namespace TakeoutPlatform.Api.Features.Admin;

/// <summary>管理员视角的订单摘要。</summary>
public sealed record AdminOrderResponse(
    int OrderId,
    int UserId,
    string Status,
    decimal Price,
    DateTime OrderTimestamp,
    int ItemCount);
