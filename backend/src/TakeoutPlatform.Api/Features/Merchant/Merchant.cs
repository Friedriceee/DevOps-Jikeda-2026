namespace TakeoutPlatform.Api.Features.Merchant;

public class Merchant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Dish> Dishes { get; set; } = new List<Dish>();
    public ICollection<SpecialOffer> SpecialOffers { get; set; } = new List<SpecialOffer>();
}
