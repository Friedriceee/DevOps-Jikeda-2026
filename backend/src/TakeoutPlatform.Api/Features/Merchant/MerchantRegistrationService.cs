using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Features.Merchant;

public sealed class MerchantRegistrationService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<Account> _hasher;
    private readonly TimeProvider _timeProvider;

    public MerchantRegistrationService(
        AppDbContext db,
        IPasswordHasher<Account> hasher,
        TimeProvider timeProvider)
    {
        _db = db;
        _hasher = hasher;
        _timeProvider = timeProvider;
    }

    public async Task<(bool Conflict, MerchantRegistrationResponse? Merchant)> RegisterAsync(
        RegisterMerchantRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var normalizedUsername = AuthService.NormalizeUsername(username);
        if (await _db.Accounts.AnyAsync(a => a.NormalizedUsername == normalizedUsername, cancellationToken))
            return (true, null);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var merchant = new Merchant
        {
            Name = request.MerchantName.Trim(),
            Address = request.MerchantAddress.Trim(),
            Contact = request.Contact.Trim(),
            DishType = request.DishType?.Trim(),
            TimeForOpenBusiness = request.TimeForOpenBusiness,
            TimeForCloseBusiness = request.TimeForCloseBusiness,
            CouponType = 0,
            Wallet = 0,
        };
        // 钱包密码暂保留在商家资料中；登录密码只存放在统一 Account 模型。
        var walletHasher = new PasswordHasher<Merchant>();
        merchant.WalletPasswordHash = walletHasher.HashPassword(merchant, request.WalletPassword);
        _db.Merchants.Add(merchant);
        await _db.SaveChangesAsync(cancellationToken);

        var account = new Account
        {
            Username = username,
            NormalizedUsername = normalizedUsername,
            Role = AccountRole.Merchant,
            ProfileId = merchant.Id,
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
        };
        account.PasswordHash = _hasher.HashPassword(account, request.Password);
        _db.Accounts.Add(account);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (true, null);
        }

        return (false, new MerchantRegistrationResponse(merchant.Id, account.Username, merchant.Name));
    }
}

public sealed record MerchantRegistrationResponse(int Id, string Username, string MerchantName);
