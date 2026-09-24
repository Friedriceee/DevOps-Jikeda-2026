namespace TakeoutPlatform.Api.Features.Order;

public sealed record OrderResponse(
    int Id,
    int UserId,
    int AddressId,
    decimal Price,
    DateTime OrderTimestamp,
    OrderStatus Status,
    int NeedUtensils,
    IReadOnlyList<OrderDishResponse> Dishes);

public sealed record OrderDishResponse(
    int MerchantId,
    int DishId,
    int DishNum);
