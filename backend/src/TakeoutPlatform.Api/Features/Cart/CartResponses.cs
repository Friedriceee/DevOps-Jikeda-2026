namespace TakeoutPlatform.Api.Features.Cart;

public sealed record CartItemResponse(
    int Id,
    int MerchantId,
    int DishId,
    string DishName,
    decimal UnitPrice,
    int DishNum,
    decimal LineTotal,
    string? ImageUrl);

public sealed record CartMerchantGroupResponse(
    int MerchantId,
    string MerchantName,
    IReadOnlyList<CartItemResponse> Items,
    decimal Subtotal,
    decimal Discount,
    decimal Total);

public sealed record CartResponse(
    IReadOnlyList<CartMerchantGroupResponse> Merchants,
    int TotalCount,
    decimal Subtotal,
    decimal Discount,
    decimal Total);
