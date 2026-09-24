using System.ComponentModel.DataAnnotations;

namespace TakeoutPlatform.Api.Features.Merchant;

public sealed class RegisterMerchantRequest
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string MerchantName { get; set; } = string.Empty;

    [Required, StringLength(255)]
    public string MerchantAddress { get; set; } = string.Empty;

    [Required, StringLength(255)]
    public string Contact { get; set; } = string.Empty;

    [StringLength(20)]
    public string? DishType { get; set; }

    [Range(0, 86399)]
    public int TimeForOpenBusiness { get; set; }

    [Range(0, 86399)]
    public int TimeForCloseBusiness { get; set; }

    [Required, StringLength(100, MinimumLength = 6)]
    public string WalletPassword { get; set; } = string.Empty;
}
