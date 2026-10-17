using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Auth;
using TakeoutPlatform.Api.Features.Merchant;
using TakeoutPlatform.Api.Features.Pricing;
using Customer = TakeoutPlatform.Api.Features.User.User;

namespace TakeoutPlatform.Api.Features.Seed;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>Creates the demo accounts and menu below. For local/staging only.</summary>
    public bool Enabled { get; set; }

    /// <summary>Password shared by the demo accounts. Supplied by a secret, never committed.</summary>
    public string? Password { get; set; }
}

/// <summary>
/// Demo data so a freshly migrated staging database can be logged into and smoke-tested:
/// one customer, two demo merchants (ids 1 and 2), the seeded demo rider (id 1) and
/// small menus. It is idempotent (safe on every start) and refuses to run in Production.
/// </summary>
public static class StagingSeeder
{
    public const string CustomerUsername = "demo_customer";
    public const string MerchantUsername = "demo_merchant";
    public const string SecondMerchantUsername = "sprint3merchant";
    public const string RiderUsername = "demo_rider";
    public const string AdministratorUsername = "admin";
    public const int DemoMerchantId = 1;
    public const int SecondMerchantId = 2;
    public const int DemoRiderId = 1;

    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var options = provider.GetRequiredService<IOptions<SeedOptions>>().Value;
        var environment = provider.GetRequiredService<IHostEnvironment>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(StagingSeeder));

        if (!options.Enabled)
            return;

        if (environment.IsProduction())
            throw new InvalidOperationException("Seed:Enabled must not be set in the Production environment.");
        if (string.IsNullOrWhiteSpace(options.Password) || options.Password.Length < 8)
            throw new InvalidOperationException("Seed:Password must be provided (at least 8 characters) when seeding is enabled.");

        var db = provider.GetRequiredService<AppDbContext>();
        var hasher = provider.GetRequiredService<IPasswordHasher<Account>>();
        var time = provider.GetRequiredService<TimeProvider>();

        var customer = await db.Users.SingleOrDefaultAsync(
            user => user.UserName == "Demo Customer", cancellationToken);
        if (customer is null)
        {
            customer = new Customer { UserName = "Demo Customer", PhoneNumber = "13800000001", Wallet = 0 };
            db.Users.Add(customer);
            await db.SaveChangesAsync(cancellationToken);
        }

        await EnsureAccountAsync(db, hasher, time, options.Password, CustomerUsername, AccountRole.Customer, customer.Id, cancellationToken);
        await EnsureAccountAsync(db, hasher, time, options.Password, MerchantUsername, AccountRole.Merchant, DemoMerchantId, cancellationToken);
        await EnsureAccountAsync(db, hasher, time, options.Password, SecondMerchantUsername, AccountRole.Merchant, SecondMerchantId, cancellationToken);
        await EnsureAccountAsync(db, hasher, time, options.Password, RiderUsername, AccountRole.Rider, DemoRiderId, cancellationToken);
        await EnsureAccountAsync(db, hasher, time, options.Password, AdministratorUsername, AccountRole.Administrator, 0, cancellationToken);

        foreach (var couponId in await db.PlatformCoupons.Select(item => item.Id).ToListAsync(cancellationToken))
        {
            if (!await db.CustomerCoupons.AnyAsync(
                    item => item.UserId == customer.Id && item.CouponId == couponId, cancellationToken))
            {
                db.CustomerCoupons.Add(new CustomerCoupon
                {
                    UserId = customer.Id,
                    CouponId = couponId,
                    IssuedQuantity = 5,
                    IssuedAtUtc = time.GetUtcNow().UtcDateTime,
                });
            }
        }

        if (!await db.Dishes.AnyAsync(dish => dish.MerchantId == DemoMerchantId, cancellationToken))
        {
            db.Dishes.AddRange(
                new Dish { MerchantId = DemoMerchantId, Name = "Braised Noodles", Category = "Noodles", Price = 12.00m, Inventory = 500 },
                new Dish { MerchantId = DemoMerchantId, Name = "Chicken Rice Bowl", Category = "Rice", Price = 15.00m, Inventory = 500 },
                new Dish { MerchantId = DemoMerchantId, Name = "Iced Lemon Tea", Category = "Drinks", Price = 4.50m, Inventory = 500 });
        }

        if (!await db.Dishes.AnyAsync(dish => dish.MerchantId == SecondMerchantId, cancellationToken))
        {
            db.Dishes.AddRange(
                new Dish { MerchantId = SecondMerchantId, Name = "Sichuan Beef Noodles", Category = "Noodles", Price = 16.00m, Inventory = 500 },
                new Dish { MerchantId = SecondMerchantId, Name = "Tofu Vegetable Bowl", Category = "Rice", Price = 13.00m, Inventory = 500 },
                new Dish { MerchantId = SecondMerchantId, Name = "Jasmine Tea", Category = "Drinks", Price = 3.50m, Inventory = 500 });
        }

        if (!await db.SpecialOffers.AnyAsync(offer => offer.MerchantId == DemoMerchantId, cancellationToken))
            db.SpecialOffers.Add(new SpecialOffer { MerchantId = DemoMerchantId, MinPrice = 50m, AmountRemission = 8m });

        if (!await db.SpecialOffers.AnyAsync(offer => offer.MerchantId == SecondMerchantId, cancellationToken))
            db.SpecialOffers.Add(new SpecialOffer { MerchantId = SecondMerchantId, MinPrice = 40m, AmountRemission = 5m });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Staging demo data is in place (accounts: {Customer}, {Merchant}, {SecondMerchant}, {Rider}, {Administrator}).",
            CustomerUsername, MerchantUsername, SecondMerchantUsername, RiderUsername, AdministratorUsername);
    }

    private static async Task EnsureAccountAsync(
        AppDbContext db,
        IPasswordHasher<Account> hasher,
        TimeProvider time,
        string password,
        string username,
        AccountRole role,
        int profileId,
        CancellationToken cancellationToken)
    {
        var normalized = AuthService.NormalizeUsername(username);
        var account = await db.Accounts.SingleOrDefaultAsync(
            item => item.NormalizedUsername == normalized, cancellationToken);
        // A previous staging run may have created the profile account under a
        // different demo username. Reuse that role/profile row instead of
        // violating the unique (Role, ProfileId) constraint when introducing
        // the deterministic second merchant login.
        if (account is null)
        {
            account = await db.Accounts.SingleOrDefaultAsync(
                item => item.Role == role && item.ProfileId == profileId,
                cancellationToken);
        }
        if (account is null)
        {
            account = new Account
            {
                Username = username,
                NormalizedUsername = normalized,
                CreatedAtUtc = time.GetUtcNow().UtcDateTime,
            };
            db.Accounts.Add(account);
        }
        else
        {
            // Keep the deterministic demo login usable even when the profile
            // was created under a legacy staging username.
            account.Username = username;
            account.NormalizedUsername = normalized;
        }
        account.Role = role;
        account.ProfileId = profileId;
        // Only a newly created account starts active. A demo account an administrator has since
        // disabled must stay disabled across restarts.
        if (account.Id == 0)
            account.IsActive = true;
        account.PasswordHash = hasher.HashPassword(account, password);
        await db.SaveChangesAsync(cancellationToken);
    }
}
