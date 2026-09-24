namespace TakeoutPlatform.Api.Features.Merchant;

public class Merchant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }
    public string? Address { get; set; }
    public string? Contact { get; set; }
    public string? DishType { get; set; }
    public int? TimeForOpenBusiness { get; set; }
    public int? TimeForCloseBusiness { get; set; }
    public int CouponType { get; set; }
    public decimal Wallet { get; set; }
    public string? WalletPasswordHash { get; set; }

    public ICollection<Dish> Dishes { get; set; } = new List<Dish>();
    public ICollection<SpecialOffer> SpecialOffers { get; set; } = new List<SpecialOffer>();
}
