using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TakeoutPlatform.Api.Common;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Features.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = nameof(AccountRole.Administrator))]
public sealed class AdminController : ControllerBase
{
    private readonly AdminAccountService _accounts;
    private readonly AdminCouponService _coupons;
    private readonly AdminOrderService _orders;

    public AdminController(
        AdminAccountService accounts,
        AdminCouponService coupons,
        AdminOrderService orders)
    {
        _accounts = accounts;
        _coupons = coupons;
        _orders = orders;
    }

    private string Actor => User.Identity?.Name ?? "unknown";
    private string CorrelationId => HttpContext.TraceIdentifier;

    // ---------- 账号 ----------

    [HttpGet("accounts")]
    public async Task<ActionResult<ApiResult<PagedResult<AdminAccountResponse>>>> ListAccounts(
        [FromQuery] string? role,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _accounts.ListAsync(role, page, pageSize, cancellationToken);
        return Ok(ApiResult<PagedResult<AdminAccountResponse>>.Ok(result));
    }

    [HttpPut("accounts/{accountId:int}/status")]
    public async Task<ActionResult<ApiResult<AdminAccountResponse>>> SetAccountStatus(
        int accountId,
        [FromBody] UpdateAccountStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _accounts.SetStatusAsync(accountId, request.IsActive, Actor, CorrelationId, cancellationToken);
        return MapAccount(result);
    }

    [HttpPut("accounts/{accountId:int}/role")]
    public async Task<ActionResult<ApiResult<AdminAccountResponse>>> SetAccountRole(
        int accountId,
        [FromBody] UpdateAccountRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _accounts.SetRoleAsync(accountId, request.Role, Actor, CorrelationId, cancellationToken);
        return MapAccount(result);
    }

    // ---------- 优惠券 ----------

    [HttpGet("coupons")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<AdminCouponResponse>>>> ListCoupons(
        CancellationToken cancellationToken)
    {
        var coupons = await _coupons.ListAsync(cancellationToken);
        return Ok(ApiResult<IReadOnlyList<AdminCouponResponse>>.Ok(coupons));
    }

    [HttpPost("coupons")]
    public async Task<ActionResult<ApiResult<AdminCouponResponse>>> CreateCoupon(
        [FromBody] CreateCouponRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _coupons.CreateAsync(request, Actor, CorrelationId, cancellationToken);
        return MapCoupon(result, created: true);
    }

    [HttpPut("coupons/{couponId:int}")]
    public async Task<ActionResult<ApiResult<AdminCouponResponse>>> UpdateCoupon(
        int couponId,
        [FromBody] UpdateCouponRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _coupons.UpdateAsync(couponId, request, Actor, CorrelationId, cancellationToken);
        return MapCoupon(result, created: false);
    }

    [HttpPut("coupons/{couponId:int}/status")]
    public async Task<ActionResult<ApiResult<AdminCouponResponse>>> SetCouponStatus(
        int couponId,
        [FromBody] SetCouponStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _coupons.SetStatusAsync(couponId, request.IsActive, Actor, CorrelationId, cancellationToken);
        return MapCoupon(result, created: false);
    }

    [HttpGet("coupon-allocations")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<CustomerCouponAllocationResponse>>>> ListCouponAllocations(
        CancellationToken cancellationToken)
    {
        var allocations = await _coupons.ListAllocationsAsync(cancellationToken);
        return Ok(ApiResult<IReadOnlyList<CustomerCouponAllocationResponse>>.Ok(allocations));
    }

    [HttpPost("coupons/{couponId:int}/issue")]
    public async Task<ActionResult<ApiResult<CustomerCouponAllocationResponse>>> IssueCoupon(
        int couponId,
        [FromBody] IssueCouponRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _coupons.IssueAsync(couponId, request, Actor, CorrelationId, cancellationToken);
        return result.Outcome switch
        {
            AdminCouponOutcome.Success => StatusCode(StatusCodes.Status201Created,
                ApiResult<CustomerCouponAllocationResponse>.Ok(result.Allocation!)),
            AdminCouponOutcome.NotFound or AdminCouponOutcome.CustomerNotFound =>
                NotFound(ApiResult<CustomerCouponAllocationResponse>.Fail(result.Message!)),
            _ => BadRequest(ApiResult<CustomerCouponAllocationResponse>.Fail(result.Message!)),
        };
    }

    // ---------- 订单查询 ----------

    [HttpGet("orders")]
    public async Task<ActionResult<ApiResult<PagedResult<AdminOrderResponse>>>> QueryOrders(
        [FromQuery] int? userId,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _orders.QueryAsync(userId, status, fromUtc, toUtc, page, pageSize, cancellationToken);
        return Ok(ApiResult<PagedResult<AdminOrderResponse>>.Ok(result));
    }

    // ---------- 映射 ----------

    private ActionResult<ApiResult<AdminAccountResponse>> MapAccount(AdminAccountResult result) =>
        result.Outcome switch
        {
            AdminAccountOutcome.Success => Ok(ApiResult<AdminAccountResponse>.Ok(result.Account!)),
            AdminAccountOutcome.NotFound => NotFound(ApiResult<AdminAccountResponse>.Fail(result.Message!)),
            _ => BadRequest(ApiResult<AdminAccountResponse>.Fail(result.Message!)),
        };

    private ActionResult<ApiResult<AdminCouponResponse>> MapCoupon(AdminCouponResult result, bool created) =>
        result.Outcome switch
        {
            AdminCouponOutcome.Success => created
                ? StatusCode(StatusCodes.Status201Created, ApiResult<AdminCouponResponse>.Ok(result.Coupon!))
                : Ok(ApiResult<AdminCouponResponse>.Ok(result.Coupon!)),
            AdminCouponOutcome.NotFound => NotFound(ApiResult<AdminCouponResponse>.Fail(result.Message!)),
            AdminCouponOutcome.DuplicateCode => Conflict(ApiResult<AdminCouponResponse>.Fail(result.Message!)),
            _ => BadRequest(ApiResult<AdminCouponResponse>.Fail(result.Message!)),
        };
}
