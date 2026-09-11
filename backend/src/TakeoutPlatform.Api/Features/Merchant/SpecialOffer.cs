namespace TakeoutPlatform.Api.Features.Merchant;

public class SpecialOffer
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public decimal MinPrice { get; set; }
    public decimal AmountRemission { get; set; }

    public Merchant Merchant { get; set; } = null!;
}
