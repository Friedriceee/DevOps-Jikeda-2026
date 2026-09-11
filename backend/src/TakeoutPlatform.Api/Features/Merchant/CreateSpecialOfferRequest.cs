using System.ComponentModel.DataAnnotations;

namespace TakeoutPlatform.Api.Features.Merchant;

public sealed class CreateSpecialOfferRequest : IValidatableObject
{
    [Required(ErrorMessage = "merchantId 不能为空")]
    [Range(1, int.MaxValue, ErrorMessage = "merchantId 必须大于 0")]
    public int MerchantId { get; set; }

    public decimal MinPrice { get; set; }

    public decimal AmountRemission { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        SpecialOfferRequestValidation.ValidateAmounts(MinPrice, AmountRemission);
}

internal static class SpecialOfferRequestValidation
{
    public static IEnumerable<ValidationResult> ValidateAmounts(
        decimal minPrice,
        decimal amountRemission)
    {
        if (minPrice <= 0)
        {
            yield return new ValidationResult(
                "minPrice 必须大于 0",
                new[] { nameof(CreateSpecialOfferRequest.MinPrice) });
        }
        else if (minPrice != decimal.Round(minPrice, 2))
        {
            yield return new ValidationResult(
                "minPrice 最多保留两位小数",
                new[] { nameof(CreateSpecialOfferRequest.MinPrice) });
        }

        if (amountRemission <= 0)
        {
            yield return new ValidationResult(
                "amountRemission 必须大于 0",
                new[] { nameof(CreateSpecialOfferRequest.AmountRemission) });
        }
        else if (amountRemission != decimal.Round(amountRemission, 2))
        {
            yield return new ValidationResult(
                "amountRemission 最多保留两位小数",
                new[] { nameof(CreateSpecialOfferRequest.AmountRemission) });
        }

        if (amountRemission >= minPrice)
        {
            yield return new ValidationResult(
                "amountRemission 必须小于 minPrice",
                new[]
                {
                    nameof(CreateSpecialOfferRequest.MinPrice),
                    nameof(CreateSpecialOfferRequest.AmountRemission),
                });
        }
    }
}

