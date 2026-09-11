using System.ComponentModel.DataAnnotations;

namespace TakeoutPlatform.Api.Features.Merchant;

/// <summary>
/// 创建 / 编辑菜品共用的字段与校验（US-01、US-03）。
/// </summary>
public abstract class DishRequestBase : IValidatableObject
{
    [Required(ErrorMessage = "name 不能为空")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "name 长度必须为 1-50 个字符")]
    public string? Name { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "price 必须大于 0")]
    public decimal Price { get; set; }

    [StringLength(20, ErrorMessage = "category 长度不能超过 20 个字符")]
    public string? Category { get; set; }

    [StringLength(500, ErrorMessage = "imageUrl 长度不能超过 500 个字符")]
    public string? ImageUrl { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "inventory 不能小于 0")]
    public int Inventory { get; set; }

    [Required(ErrorMessage = "merchantId 不能为空")]
    [Range(1, int.MaxValue, ErrorMessage = "merchantId 必须大于 0")]
    public int MerchantId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Name is not null && string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult("name 不能为空", new[] { nameof(Name) });
        }

        if (Price != decimal.Round(Price, 2))
        {
            yield return new ValidationResult("price 最多保留两位小数", new[] { nameof(Price) });
        }

        if (!string.IsNullOrWhiteSpace(ImageUrl) &&
            (!Uri.TryCreate(ImageUrl, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        {
            yield return new ValidationResult("imageUrl 必须是合法的 http/https URL", new[] { nameof(ImageUrl) });
        }
    }
}
