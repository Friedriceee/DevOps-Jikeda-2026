using System.ComponentModel.DataAnnotations;

namespace TakeoutPlatform.Api.Features.Merchant;

public sealed class UpdateSpecialOfferRequest : IValidatableObject
{
    public decimal MinPrice { get; set; }

    public decimal AmountRemission { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        SpecialOfferRequestValidation.ValidateAmounts(MinPrice, AmountRemission);
}
