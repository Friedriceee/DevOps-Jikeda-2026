namespace TakeoutPlatform.Api.Features.Merchant;

public sealed record SpecialOfferResponse(
    int Id,
    int MerchantId,
    decimal MinPrice,
    decimal AmountRemission);

