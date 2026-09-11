using Microsoft.EntityFrameworkCore;
using TakeoutPlatform.Api.Data;

namespace TakeoutPlatform.Api.Features.Merchant;

public sealed class SpecialOfferService
{
    private readonly AppDbContext _db;

    public SpecialOfferService(AppDbContext db) => _db = db;

    public async Task<(bool MerchantFound, SpecialOfferResponse? Offer)> CreateAsync(
        CreateSpecialOfferRequest request,
        CancellationToken cancellationToken)
    {
        var merchantExists = await _db.Merchants
            .AnyAsync(merchant => merchant.Id == request.MerchantId, cancellationToken);

        if (!merchantExists)
        {
            return (false, null);
        }

        var offer = new SpecialOffer
        {
            MerchantId = request.MerchantId,
            MinPrice = request.MinPrice,
            AmountRemission = request.AmountRemission,
        };

        _db.SpecialOffers.Add(offer);
        await _db.SaveChangesAsync(cancellationToken);

        return (true, ToResponse(offer));
    }

    public async Task<SpecialOfferResponse?> UpdateAsync(
        int offerId,
        int merchantId,
        UpdateSpecialOfferRequest request,
        CancellationToken cancellationToken)
    {
        var offer = await _db.SpecialOffers
            .SingleOrDefaultAsync(
                item => item.Id == offerId && item.MerchantId == merchantId,
                cancellationToken);

        if (offer is null)
        {
            return null;
        }

        offer.MinPrice = request.MinPrice;
        offer.AmountRemission = request.AmountRemission;

        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(offer);
    }

    public async Task<bool> DeleteAsync(
        int offerId,
        int merchantId,
        CancellationToken cancellationToken)
    {
        var offer = await _db.SpecialOffers
            .SingleOrDefaultAsync(
                item => item.Id == offerId && item.MerchantId == merchantId,
                cancellationToken);

        if (offer is null)
        {
            return false;
        }

        _db.SpecialOffers.Remove(offer);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<SpecialOfferResponse>?> ListAsync(
        int merchantId,
        CancellationToken cancellationToken)
    {
        var merchantExists = await _db.Merchants
            .AnyAsync(merchant => merchant.Id == merchantId, cancellationToken);

        if (!merchantExists)
        {
            return null;
        }

        return await _db.SpecialOffers
            .AsNoTracking()
            .Where(offer => offer.MerchantId == merchantId)
            .OrderBy(offer => offer.Id)
            .Select(offer => new SpecialOfferResponse(
                offer.Id,
                offer.MerchantId,
                offer.MinPrice,
                offer.AmountRemission))
            .ToListAsync(cancellationToken);
    }

    private static SpecialOfferResponse ToResponse(SpecialOffer offer) => new(
        offer.Id,
        offer.MerchantId,
        offer.MinPrice,
        offer.AmountRemission);
}

