using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TakeoutPlatform.Api.Data;

namespace TakeoutPlatform.Api.Features.Merchant;

public sealed class MerchantRegistrationService
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<Merchant> _hasher = new();

    public MerchantRegistrationService(AppDbContext db) => _db = db;

    public async Task<(bool Conflict, MerchantRegistrationResponse? Merchant)> RegisterAsync(
        RegisterMerchantRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        if (await _db.Merchants.AnyAsync(m => m.Username == username, cancellationToken))
            return (true, null);

        var merchant = new Merchant
        {
            Username = username,
            Name = request.MerchantName.Trim(),
            Address = request.MerchantAddress.Trim(),
            Contact = request.Contact.Trim(),
            DishType = request.DishType?.Trim(),
            TimeForOpenBusiness = request.TimeForOpenBusiness,
            TimeForCloseBusiness = request.TimeForCloseBusiness,
            CouponType = 0,
            Wallet = 0,
        };
        merchant.PasswordHash = _hasher.HashPassword(merchant, request.Password);
        merchant.WalletPasswordHash = _hasher.HashPassword(merchant, request.WalletPassword);
        _db.Merchants.Add(merchant);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (_db.Merchants.AsNoTracking().Any(m => m.Username == username))
        {
            return (true, null);
        }

        return (false, new MerchantRegistrationResponse(merchant.Id, merchant.Username, merchant.Name));
    }
}

public sealed record MerchantRegistrationResponse(int Id, string Username, string MerchantName);
