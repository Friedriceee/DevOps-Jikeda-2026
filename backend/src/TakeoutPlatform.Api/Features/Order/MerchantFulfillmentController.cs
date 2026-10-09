using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TakeoutPlatform.Api.Common;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Features.Order;

[ApiController]
[Route("api/merchant/orders")]
[Authorize(Roles = nameof(AccountRole.Merchant))]
public sealed class MerchantFulfillmentController : ControllerBase
{
    private readonly MerchantFulfillmentService _service;

    public MerchantFulfillmentController(MerchantFulfillmentService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResult<IReadOnlyList<MerchantOrderResponse>>>> List(
        CancellationToken cancellationToken)
    {
        var orders = await _service.ListAsync(User.GetProfileId(), cancellationToken);
        return Ok(ApiResult<IReadOnlyList<MerchantOrderResponse>>.Ok(orders));
    }

    [HttpPut("{orderId:int}/status")]
    public async Task<ActionResult<ApiResult<MerchantOrderResponse>>> Transition(
        int orderId,
        [FromBody] MerchantOrderTransitionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.TransitionAsync(
            orderId,
            User.GetProfileId(),
            request,
            cancellationToken);

        return result.Outcome switch
        {
            MerchantTransitionOutcome.Success => Ok(ApiResult<MerchantOrderResponse>.Ok(result.Order!)),
            MerchantTransitionOutcome.NotFound => NotFound(ApiResult<MerchantOrderResponse>.Fail(result.Message!)),
            MerchantTransitionOutcome.Forbidden => StatusCode(
                StatusCodes.Status403Forbidden,
                ApiResult<MerchantOrderResponse>.Fail(result.Message!)),
            MerchantTransitionOutcome.InvalidRequest => BadRequest(ApiResult<MerchantOrderResponse>.Fail(result.Message!)),
            _ => Conflict(ApiResult<MerchantOrderResponse>.Fail(result.Message!)),
        };
    }
}
