using Microsoft.EntityFrameworkCore;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Audit;
using TakeoutPlatform.Api.Features.Auth;
using TakeoutPlatform.Api.Features.Pricing;

namespace TakeoutPlatform.Api.Features.Admin;

public enum AdminCouponOutcome
{
    Success,
    NotFound,
    DuplicateCode,
    InvalidDateRange,
    CustomerNotFound,
}

public sealed record AdminCouponResult(
    AdminCouponOutcome Outcome,
    AdminCouponResponse? Coupon,
    string? Message);

/// <summary>
/// 平台优惠券管理：新建、修改、启用/停用、查询。
/// 复用现有 PlatformCoupon 表（PricingService 读取同一张表计算优惠）。
/// 校验有效期、门槛、金额；Code 唯一；变更写审计。
/// </summary>
public sealed class AdminCouponService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly TimeProvider _timeProvider;

    public AdminCouponService(AppDbContext db, IAuditLogger audit, TimeProvider timeProvider)
    {
        _db = db;
        _audit = audit;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<AdminCouponResponse>> ListAsync(CancellationToken cancellationToken)
    {
        return await _db.PlatformCoupons
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => new AdminCouponResponse(
                c.Id, c.Code, c.Name, c.MinimumSpend, c.DiscountAmount,
                c.StartsAtUtc, c.ExpiresAtUtc, c.IsActive,
                _db.CustomerCoupons.Where(item => item.CouponId == c.Id).Sum(item => (int?)item.IssuedQuantity) ?? 0,
                _db.CustomerCoupons.Where(item => item.CouponId == c.Id).Sum(item => (int?)item.UsedQuantity) ?? 0))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerCouponAllocationResponse>> ListAllocationsAsync(
        CancellationToken cancellationToken) =>
        await _db.CustomerCoupons.AsNoTracking()
            .OrderByDescending(item => item.IssuedAtUtc)
            .Select(item => new CustomerCouponAllocationResponse(
                item.Id,
                item.UserId,
                _db.Users.Where(user => user.Id == item.UserId).Select(user => user.UserName).FirstOrDefault() ?? "Unknown customer",
                item.CouponId,
                item.Coupon.Code,
                item.IssuedQuantity,
                item.UsedQuantity,
                item.IssuedQuantity - item.UsedQuantity,
                item.IssuedAtUtc))
            .ToListAsync(cancellationToken);

    public async Task<(AdminCouponOutcome Outcome, CustomerCouponAllocationResponse? Allocation, string? Message)> IssueAsync(
        int couponId,
        IssueCouponRequest request,
        string actor,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var coupon = await _db.PlatformCoupons.FirstOrDefaultAsync(item => item.Id == couponId, cancellationToken);
        if (coupon is null)
            return (AdminCouponOutcome.NotFound, null, "Coupon was not found.");
        var customer = await _db.Users.FirstOrDefaultAsync(item => item.Id == request.UserId, cancellationToken);
        if (customer is null)
            return (AdminCouponOutcome.CustomerNotFound, null, "Customer was not found.");

        var allocation = await _db.CustomerCoupons
            .SingleOrDefaultAsync(item => item.UserId == request.UserId && item.CouponId == couponId, cancellationToken);
        if (allocation is null)
        {
            allocation = new CustomerCoupon
            {
                UserId = request.UserId,
                CouponId = couponId,
                IssuedQuantity = request.Quantity,
                IssuedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            };
            _db.CustomerCoupons.Add(allocation);
        }
        else
        {
            allocation.IssuedQuantity += request.Quantity;
            allocation.IssuedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        }
        await _db.SaveChangesAsync(cancellationToken);
        await Audit("CouponIssued", couponId, $"Success:customer={customer.Id},quantity={request.Quantity}", actor, correlationId, cancellationToken);
        return (AdminCouponOutcome.Success, new CustomerCouponAllocationResponse(
            allocation.Id, customer.Id, customer.UserName, coupon.Id, coupon.Code,
            allocation.IssuedQuantity, allocation.UsedQuantity,
            allocation.IssuedQuantity - allocation.UsedQuantity, allocation.IssuedAtUtc), null);
    }

    public async Task<AdminCouponResult> CreateAsync(
        CreateCouponRequest request,
        string actor,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (request.ExpiresAtUtc <= request.StartsAtUtc)
            return new AdminCouponResult(AdminCouponOutcome.InvalidDateRange, null, "过期时间必须晚于开始时间");

        var code = request.Code.Trim();
        if (await _db.PlatformCoupons.AnyAsync(c => c.Code == code, cancellationToken))
            return new AdminCouponResult(AdminCouponOutcome.DuplicateCode, null, "优惠券码已存在");

        var coupon = new PlatformCoupon
        {
            Code = code,
            Name = request.Name.Trim(),
            MinimumSpend = request.MinimumSpend,
            DiscountAmount = request.DiscountAmount,
            StartsAtUtc = request.StartsAtUtc,
            ExpiresAtUtc = request.ExpiresAtUtc,
            IsActive = request.IsActive,
        };
        _db.PlatformCoupons.Add(coupon);
        await _db.SaveChangesAsync(cancellationToken);

        await Audit("CouponCreated", coupon.Id, "Success", actor, correlationId, cancellationToken);
        return new AdminCouponResult(AdminCouponOutcome.Success, ToResponse(coupon, 0, 0), null);
    }

    public async Task<AdminCouponResult> UpdateAsync(
        int couponId,
        UpdateCouponRequest request,
        string actor,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (request.ExpiresAtUtc <= request.StartsAtUtc)
            return new AdminCouponResult(AdminCouponOutcome.InvalidDateRange, null, "过期时间必须晚于开始时间");

        var coupon = await _db.PlatformCoupons.FirstOrDefaultAsync(c => c.Id == couponId, cancellationToken);
        if (coupon is null)
            return new AdminCouponResult(AdminCouponOutcome.NotFound, null, "优惠券不存在");

        coupon.Name = request.Name.Trim();
        coupon.MinimumSpend = request.MinimumSpend;
        coupon.DiscountAmount = request.DiscountAmount;
        coupon.StartsAtUtc = request.StartsAtUtc;
        coupon.ExpiresAtUtc = request.ExpiresAtUtc;
        await _db.SaveChangesAsync(cancellationToken);

        await Audit("CouponUpdated", coupon.Id, "Success", actor, correlationId, cancellationToken);
        return new AdminCouponResult(AdminCouponOutcome.Success, await ToResponseAsync(coupon, cancellationToken), null);
    }

    public async Task<AdminCouponResult> SetStatusAsync(
        int couponId,
        bool isActive,
        string actor,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var coupon = await _db.PlatformCoupons.FirstOrDefaultAsync(c => c.Id == couponId, cancellationToken);
        if (coupon is null)
            return new AdminCouponResult(AdminCouponOutcome.NotFound, null, "优惠券不存在");

        coupon.IsActive = isActive;
        await _db.SaveChangesAsync(cancellationToken);

        await Audit(isActive ? "CouponEnabled" : "CouponDisabled", coupon.Id, "Success", actor, correlationId, cancellationToken);
        return new AdminCouponResult(AdminCouponOutcome.Success, await ToResponseAsync(coupon, cancellationToken), null);
    }

    private Task Audit(string action, int couponId, string result, string actor, string correlationId, CancellationToken ct)
        => _audit.LogAsync(new AuditEvent(
            actor, nameof(AccountRole.Administrator), action,
            nameof(PlatformCoupon), couponId.ToString(), result,
            _timeProvider.GetUtcNow().UtcDateTime, correlationId), ct);

    private async Task<AdminCouponResponse> ToResponseAsync(PlatformCoupon c, CancellationToken cancellationToken)
    {
        var issued = await _db.CustomerCoupons.Where(item => item.CouponId == c.Id)
            .SumAsync(item => (int?)item.IssuedQuantity, cancellationToken) ?? 0;
        var used = await _db.CustomerCoupons.Where(item => item.CouponId == c.Id)
            .SumAsync(item => (int?)item.UsedQuantity, cancellationToken) ?? 0;
        return ToResponse(c, issued, used);
    }

    private static AdminCouponResponse ToResponse(PlatformCoupon c, int issued, int used) => new(
        c.Id, c.Code, c.Name, c.MinimumSpend, c.DiscountAmount, c.StartsAtUtc, c.ExpiresAtUtc, c.IsActive, issued, used);
}
