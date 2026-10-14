using NUnit.Framework;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Admin;
using TakeoutPlatform.Api.Features.Audit;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Tests;

/// <summary>
/// 管理员后端 Service 单元测试（SQLite in-memory）。
/// 覆盖账号管理、优惠券管理（边界/过期/无效金额/重复）、订单查询。
/// 审计用 NullAuditLogger（member3 真实实现落地后不影响这些断言）。
/// 种子中已有管理员账号 Id=1000（来自迁移/HasData）。
/// </summary>
public class AdminServiceTests : ServiceTestBase
{
    private const string Actor = "admin";
    private const string Corr = "test-correlation";

    private AdminAccountService NewAccountService(AppDbContext db)
        => new(db, new NullAuditLogger(), TimeProvider.System);
    private AdminCouponService NewCouponService(AppDbContext db)
        => new(db, new NullAuditLogger(), TimeProvider.System);
    private AdminOrderService NewOrderService(AppDbContext db) => new(db);

    private static int SeedAccount(AppDbContext db, AccountRole role, int profileId, bool isActive = true, string? username = null)
    {
        var account = new Account
        {
            Username = username ?? $"user{profileId}_{role}",
            NormalizedUsername = (username ?? $"user{profileId}_{role}").ToUpperInvariant(),
            PasswordHash = "x",
            Role = role,
            ProfileId = profileId,
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = isActive,
        };
        db.Accounts.Add(account);
        db.SaveChanges();
        return account.Id;
    }

    // ---------- 账号管理 ----------

    [Test]
    public async Task ListAccounts_filters_by_role_and_paginates()
    {
        await using (var seed = CreateContext())
        {
            SeedAccount(seed, AccountRole.Customer, 10);
            SeedAccount(seed, AccountRole.Customer, 11);
            SeedAccount(seed, AccountRole.Merchant, 12);
        }

        await using var db = CreateContext();
        var customers = await NewAccountService(db).ListAsync("Customer", 1, 20, CancellationToken.None);

        Assert.That(customers.Items.All(a => a.Role == "Customer"), Is.True);
        Assert.That(customers.Items, Has.Count.EqualTo(2));
        Assert.That(customers.TotalCount, Is.EqualTo(2));
    }

    [Test]
    public async Task SetStatus_disables_account()
    {
        int id;
        await using (var seed = CreateContext())
            id = SeedAccount(seed, AccountRole.Customer, 20);

        await using var db = CreateContext();
        var result = await NewAccountService(db).SetStatusAsync(id, false, Actor, Corr, CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(AdminAccountOutcome.Success));
        Assert.That(result.Account!.IsActive, Is.False);

        await using var verify = CreateContext();
        Assert.That(verify.Accounts.Single(a => a.Id == id).IsActive, Is.False);
    }

    [Test]
    public async Task SetStatus_for_missing_account_returns_NotFound()
    {
        await using var db = CreateContext();
        var result = await NewAccountService(db).SetStatusAsync(999999, false, Actor, Corr, CancellationToken.None);
        Assert.That(result.Outcome, Is.EqualTo(AdminAccountOutcome.NotFound));
    }

    [Test]
    public async Task SetRole_changes_role()
    {
        int id;
        await using (var seed = CreateContext())
            id = SeedAccount(seed, AccountRole.Customer, 30);

        await using var db = CreateContext();
        var result = await NewAccountService(db).SetRoleAsync(id, "Rider", Actor, Corr, CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(AdminAccountOutcome.Success));
        Assert.That(result.Account!.Role, Is.EqualTo("Rider"));
    }

    [Test]
    public async Task SetRole_with_invalid_role_returns_InvalidRole()
    {
        int id;
        await using (var seed = CreateContext())
            id = SeedAccount(seed, AccountRole.Customer, 31);

        await using var db = CreateContext();
        var result = await NewAccountService(db).SetRoleAsync(id, "Overlord", Actor, Corr, CancellationToken.None);
        Assert.That(result.Outcome, Is.EqualTo(AdminAccountOutcome.InvalidRole));
    }

    // ---------- 优惠券管理 ----------

    private static CreateCouponRequest NewCouponReq(
        string code = "SAVE10",
        decimal min = 20m,
        decimal discount = 10m,
        int startOffsetDays = -1,
        int expireOffsetDays = 30) => new()
    {
        Code = code,
        Name = $"Coupon {code}",
        MinimumSpend = min,
        DiscountAmount = discount,
        StartsAtUtc = DateTimeOffset.UtcNow.AddDays(startOffsetDays),
        ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(expireOffsetDays),
        IsActive = true,
    };

    [Test]
    public async Task CreateCoupon_persists_and_returns_coupon()
    {
        await using var db = CreateContext();
        var result = await NewCouponService(db).CreateAsync(NewCouponReq(), Actor, Corr, CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(AdminCouponOutcome.Success));
        Assert.That(result.Coupon!.Code, Is.EqualTo("SAVE10"));

        await using var verify = CreateContext();
        Assert.That(verify.PlatformCoupons.Any(c => c.Code == "SAVE10"), Is.True);
    }

    [Test]
    public async Task CreateCoupon_rejects_duplicate_code()
    {
        await using (var seed = CreateContext())
            await NewCouponService(seed).CreateAsync(NewCouponReq("DUP"), Actor, Corr, CancellationToken.None);

        await using var db = CreateContext();
        var result = await NewCouponService(db).CreateAsync(NewCouponReq("DUP"), Actor, Corr, CancellationToken.None);
        Assert.That(result.Outcome, Is.EqualTo(AdminCouponOutcome.DuplicateCode));
    }

    [Test]
    public async Task CreateCoupon_rejects_invalid_date_range()
    {
        await using var db = CreateContext();
        var req = NewCouponReq("BADDATE", startOffsetDays: 10, expireOffsetDays: 5); // expire <= start
        var result = await NewCouponService(db).CreateAsync(req, Actor, Corr, CancellationToken.None);
        Assert.That(result.Outcome, Is.EqualTo(AdminCouponOutcome.InvalidDateRange));
    }

    [Test]
    public async Task UpdateCoupon_changes_fields()
    {
        int id;
        await using (var seed = CreateContext())
        {
            var r = await NewCouponService(seed).CreateAsync(NewCouponReq("UPD"), Actor, Corr, CancellationToken.None);
            id = r.Coupon!.Id;
        }

        await using var db = CreateContext();
        var result = await NewCouponService(db).UpdateAsync(id, new UpdateCouponRequest
        {
            Name = "Updated",
            MinimumSpend = 50m,
            DiscountAmount = 15m,
            StartsAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(10),
        }, Actor, Corr, CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(AdminCouponOutcome.Success));
        Assert.That(result.Coupon!.Name, Is.EqualTo("Updated"));
        Assert.That(result.Coupon.DiscountAmount, Is.EqualTo(15m));
    }

    [Test]
    public async Task SetCouponStatus_disables_coupon()
    {
        int id;
        await using (var seed = CreateContext())
        {
            var r = await NewCouponService(seed).CreateAsync(NewCouponReq("TOGGLE"), Actor, Corr, CancellationToken.None);
            id = r.Coupon!.Id;
        }

        await using var db = CreateContext();
        var result = await NewCouponService(db).SetStatusAsync(id, false, Actor, Corr, CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(AdminCouponOutcome.Success));
        Assert.That(result.Coupon!.IsActive, Is.False);
    }

    [Test]
    public async Task UpdateCoupon_for_missing_returns_NotFound()
    {
        await using var db = CreateContext();
        var result = await NewCouponService(db).SetStatusAsync(999999, false, Actor, Corr, CancellationToken.None);
        Assert.That(result.Outcome, Is.EqualTo(AdminCouponOutcome.NotFound));
    }

    // ---------- 订单查询 ----------

    [Test]
    public async Task QueryOrders_filters_by_user_and_status()
    {
        await using (var seed = CreateContext())
        {
            SeedOrder(seed, userId: 1, Features.Order.OrderStatus.Pending);
            SeedOrder(seed, userId: 1, Features.Order.OrderStatus.Paid);
            SeedOrder(seed, userId: 2, Features.Order.OrderStatus.Pending);
        }

        await using var db = CreateContext();
        var byUser = await NewOrderService(db).QueryAsync(1, null, null, null, 1, 20, CancellationToken.None);
        Assert.That(byUser.Items.All(o => o.UserId == 1), Is.True);
        Assert.That(byUser.TotalCount, Is.EqualTo(2));

        var byStatus = await NewOrderService(db).QueryAsync(null, "Pending", null, null, 1, 20, CancellationToken.None);
        Assert.That(byStatus.Items.All(o => o.Status == "Pending"), Is.True);
        Assert.That(byStatus.TotalCount, Is.EqualTo(2));
    }

    private static void SeedOrder(AppDbContext db, int userId, Features.Order.OrderStatus status)
    {
        var order = new Features.Order.Order
        {
            Price = 30m,
            OrderTimestamp = DateTime.UtcNow,
            Status = status,
            NeedUtensils = 1,
            AddressId = 1,
        };
        db.Orders.Add(order);
        db.SaveChanges();
        db.OrderUsers.Add(new Features.Order.OrderUser { OrderId = order.Id, UserId = userId });
        db.SaveChanges();
    }
}
