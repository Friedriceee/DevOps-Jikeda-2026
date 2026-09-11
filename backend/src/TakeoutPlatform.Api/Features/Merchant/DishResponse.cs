namespace TakeoutPlatform.Api.Features.Merchant;

public sealed record DishResponse(
    int Id,
    int MerchantId,
    string Name,
    decimal Price,
    string? Category,
    string? ImageUrl,
    int Inventory);
