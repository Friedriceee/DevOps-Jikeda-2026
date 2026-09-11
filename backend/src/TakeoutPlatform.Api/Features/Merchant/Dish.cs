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

    public Merchant Merchant { get; set; } = null!;
}
