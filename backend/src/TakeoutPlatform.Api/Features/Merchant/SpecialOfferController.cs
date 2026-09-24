using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TakeoutPlatform.Api.Common;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Features.Merchant;

[ApiController]
[Route("api/merchant")]
public sealed class SpecialOfferController : ControllerBase
{
    private readonly SpecialOfferService _specialOfferService;

    public SpecialOfferController(SpecialOfferService specialOfferService) =>
        _specialOfferService = specialOfferService;

    [HttpPost("special-offers")]
    [Authorize(Roles = nameof(AccountRole.Merchant))]
    public async Task<ActionResult<ApiResult<SpecialOfferResponse>>> Create(
        [FromBody] CreateSpecialOfferRequest request,
        CancellationToken cancellationToken)
    {
        request.MerchantId = User.GetProfileId();
        var result = await _specialOfferService.CreateAsync(request, cancellationToken);

        if (!result.MerchantFound)
        {
            return NotFound(ApiResult<SpecialOfferResponse>.Fail("商家不存在"));
        }

        return CreatedAtAction(
            nameof(List),
            new { merchantId = result.Offer!.MerchantId },
            ApiResult<SpecialOfferResponse>.Ok(result.Offer));
    }

    [HttpPut("special-offers/{offerId:int}")]
    [Authorize(Roles = nameof(AccountRole.Merchant))]
    public async Task<ActionResult<ApiResult<SpecialOfferResponse>>> Update(
        int offerId,
        [FromBody] UpdateSpecialOfferRequest request,
        CancellationToken cancellationToken)
    {
        var offer = await _specialOfferService.UpdateAsync(
            offerId,
            User.GetProfileId(),
            request,
            cancellationToken);

        return offer is null
            ? NotFound(ApiResult<SpecialOfferResponse>.Fail("满减活动不存在或不属于该商家"))
            : Ok(ApiResult<SpecialOfferResponse>.Ok(offer));
    }

    [HttpDelete("special-offers/{offerId:int}")]
    [Authorize(Roles = nameof(AccountRole.Merchant))]
    public async Task<ActionResult<ApiResult<object>>> Delete(
        int offerId,
        CancellationToken cancellationToken)
    {
        var deleted = await _specialOfferService.DeleteAsync(
            offerId,
            User.GetProfileId(),
            cancellationToken);

        if (!deleted)
        {
            return NotFound(ApiResult<object>.Fail("满减活动不存在或不属于该商家"));
        }

        return Ok(ApiResult.Ok());
    }

    [HttpGet("{merchantId:int}/special-offers")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<SpecialOfferResponse>>>> List(
        int merchantId,
        CancellationToken cancellationToken)
    {
        if (merchantId <= 0)
        {
            return BadRequest(ApiResult<IReadOnlyList<SpecialOfferResponse>>.Fail(
                "merchantId 必须大于 0"));
        }

        var offers = await _specialOfferService.ListAsync(merchantId, cancellationToken);

        return offers is null
            ? NotFound(ApiResult<IReadOnlyList<SpecialOfferResponse>>.Fail("商家不存在"))
            : Ok(ApiResult<IReadOnlyList<SpecialOfferResponse>>.Ok(offers));
    }
}
