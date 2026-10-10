using TakeoutPlatform.Api.Features.Menu;

namespace TakeoutPlatform.Api.Features.Merchant;

public class Dish
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Category { get; set; }
    public string? ImageUrl { get; set; }
    public int Inventory { get; set; }
    public MenuItemType ItemType { get; set; } = MenuItemType.Standard;
    public int? BundleSize { get; set; }
    public DateTimeOffset? PromotionStartsAtUtc { get; set; }
    public DateTimeOffset? PromotionEndsAtUtc { get; set; }

    public Merchant Merchant { get; set; } = null!;
}
