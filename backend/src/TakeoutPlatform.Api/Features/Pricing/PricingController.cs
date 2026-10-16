using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TakeoutPlatform.Api.Common;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Features.Pricing;

[ApiController]
[Route("api/pricing")]
[Authorize(Roles = nameof(AccountRole.Customer))]
public sealed class PricingController : ControllerBase
{
    private readonly PricingService _pricingService;

    public PricingController(PricingService pricingService) => _pricingService = pricingService;

    [HttpGet("coupons")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<CouponOptionResponse>>>> ListCoupons(
        CancellationToken cancellationToken)
    {
        var coupons = await _pricingService.ListCouponsAsync(User.GetProfileId(), cancellationToken);
        return Ok(ApiResult<IReadOnlyList<CouponOptionResponse>>.Ok(coupons));
    }

    [HttpPost("quote")]
    public async Task<ActionResult<ApiResult<PricingResult>>> Quote(
        [FromBody] PricingQuoteRequest request,
        CancellationToken cancellationToken)
    {
        var outcome = await _pricingService.QuoteAsync(
            User.GetProfileId(), request, cancellationToken);
        if (!outcome.Succeeded)
        {
            return BadRequest(ApiResult<PricingResult>.Fail(
                outcome.Message!, outcome.ErrorCode!.Value.ToString()));
        }

        return Ok(ApiResult<PricingResult>.Ok(outcome.Pricing!));
    }
}
