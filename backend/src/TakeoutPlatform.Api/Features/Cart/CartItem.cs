using TakeoutPlatform.Api.Features.Merchant;
using Customer = TakeoutPlatform.Api.Features.User.User;

namespace TakeoutPlatform.Api.Features.Cart;

/// <summary>
/// A persistent cart line. MerchantId is copied from the selected dish so a
/// customer's cart can be queried and cleared by merchant efficiently.
/// </summary>
public class CartItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int MerchantId { get; set; }
    public int DishId { get; set; }
    public int DishNum { get; set; }

    public Customer User { get; set; } = null!;
    public Dish Dish { get; set; } = null!;
}
