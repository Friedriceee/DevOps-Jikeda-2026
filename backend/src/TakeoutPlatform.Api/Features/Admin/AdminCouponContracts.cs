using System.ComponentModel.DataAnnotations;

namespace TakeoutPlatform.Api.Features.Admin;

public sealed record AdminCouponResponse(
    int Id,
    string Code,
    string Name,
    decimal MinimumSpend,
    decimal DiscountAmount,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset ExpiresAtUtc,
    bool IsActive,
    int IssuedQuantity,
    int UsedQuantity);

public sealed record CustomerCouponAllocationResponse(
    int Id,
    int UserId,
    string CustomerName,
    int CouponId,
    string CouponCode,
    int IssuedQuantity,
    int UsedQuantity,
    int RemainingQuantity,
    DateTime IssuedAtUtc);

public sealed class IssueCouponRequest
{
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; } = 1;
}

public sealed class CreateCouponRequest
{
    [Required(ErrorMessage = "code is required")]
    [StringLength(40, MinimumLength = 1)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "name is required")]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "minimumSpend must be >= 0")]
    public decimal MinimumSpend { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "discountAmount must be > 0")]
    public decimal DiscountAmount { get; set; }

    [Required]
    public DateTimeOffset StartsAtUtc { get; set; }

    [Required]
    public DateTimeOffset ExpiresAtUtc { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class UpdateCouponRequest
{
    [Required(ErrorMessage = "name is required")]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "minimumSpend must be >= 0")]
    public decimal MinimumSpend { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "discountAmount must be > 0")]
    public decimal DiscountAmount { get; set; }

    [Required]
    public DateTimeOffset StartsAtUtc { get; set; }

    [Required]
    public DateTimeOffset ExpiresAtUtc { get; set; }
}

public sealed class SetCouponStatusRequest
{
    [Required]
    public bool IsActive { get; set; }
}
