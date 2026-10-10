using System.ComponentModel.DataAnnotations;
using TakeoutPlatform.Api.Features.Menu;

namespace TakeoutPlatform.Api.Features.Merchant;

public sealed class CreateDishRequest : IValidatableObject, IMenuItemRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Name must be 1–50 characters.")]
    public string? Name { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    [StringLength(20, ErrorMessage = "Category must not exceed 20 characters.")]
    public string? Category { get; set; }

    [StringLength(500, ErrorMessage = "Image URL must not exceed 500 characters.")]
    public string? ImageUrl { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Inventory cannot be negative.")]
    public int Inventory { get; set; }
    public string ItemType { get; set; } = "Standard";
    public int? BundleSize { get; set; }
    public DateTimeOffset? PromotionStartsAtUtc { get; set; }
    public DateTimeOffset? PromotionEndsAtUtc { get; set; }

    // 兼容旧客户端字段；Controller 始终以 JWT profile_id 覆盖它。
    public int MerchantId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var error in MenuItemValidation.Validate(this)) yield return error;
        if (Name is not null && string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult("Name is required.", new[] { nameof(Name) });
        }

        if (Price != decimal.Round(Price, 2))
        {
            yield return new ValidationResult("Price can have at most two decimal places.", new[] { nameof(Price) });
        }

        if (!string.IsNullOrWhiteSpace(ImageUrl) &&
            (!Uri.TryCreate(ImageUrl, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        {
            yield return new ValidationResult("Image URL must be a valid http/https URL.", new[] { nameof(ImageUrl) });
        }
    }
}
