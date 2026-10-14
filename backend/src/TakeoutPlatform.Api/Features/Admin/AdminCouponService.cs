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
            .Select(c => ToResponse(c))
            .ToListAsync(cancellationToken);
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
        return new AdminCouponResult(AdminCouponOutcome.Success, ToResponse(coupon), null);
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
        return new AdminCouponResult(AdminCouponOutcome.Success, ToResponse(coupon), null);
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
        return new AdminCouponResult(AdminCouponOutcome.Success, ToResponse(coupon), null);
    }

    private Task Audit(string action, int couponId, string result, string actor, string correlationId, CancellationToken ct)
        => _audit.LogAsync(new AuditEvent(
            actor, nameof(AccountRole.Administrator), action,
            nameof(PlatformCoupon), couponId.ToString(), result,
            _timeProvider.GetUtcNow().UtcDateTime, correlationId), ct);

    private static AdminCouponResponse ToResponse(PlatformCoupon c) => new(
        c.Id, c.Code, c.Name, c.MinimumSpend, c.DiscountAmount, c.StartsAtUtc, c.ExpiresAtUtc, c.IsActive);
}
