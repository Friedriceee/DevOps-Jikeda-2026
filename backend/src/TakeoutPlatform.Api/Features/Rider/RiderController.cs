using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TakeoutPlatform.Api.Common;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Features.Rider;

[ApiController]
[Route("api/rider")]
[Authorize(Roles = nameof(AccountRole.Rider))]
public sealed class RiderController : ControllerBase
{
    private readonly RiderDeliveryService _service;

    public RiderController(RiderDeliveryService service) => _service = service;

    /// <summary>可接任务池（待派送订单）。</summary>
    [HttpGet("available")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<AvailableDeliveryResponse>>>> Available(
        CancellationToken cancellationToken)
    {
        var orders = await _service.ListAvailableAsync(cancellationToken);
        return Ok(ApiResult<IReadOnlyList<AvailableDeliveryResponse>>.Ok(orders));
    }

    /// <summary>当前骑手自己的配送任务。</summary>
    [HttpGet("deliveries")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<RiderDeliveryResponse>>>> MyDeliveries(
        CancellationToken cancellationToken)
    {
        var deliveries = await _service.ListMyDeliveriesAsync(User.GetProfileId(), cancellationToken);
        return Ok(ApiResult<IReadOnlyList<RiderDeliveryResponse>>.Ok(deliveries));
    }

    /// <summary>接单。身份取自 JWT，不信任请求参数。</summary>
    [HttpPost("deliveries/{orderId:int}/accept")]
    public async Task<ActionResult<ApiResult<RiderDeliveryResponse>>> Accept(
        int orderId,
        CancellationToken cancellationToken)
    {
        var result = await _service.AcceptAsync(orderId, User.GetProfileId(), cancellationToken);
        return result.Outcome switch
        {
            AcceptOutcome.Success => Ok(ApiResult<RiderDeliveryResponse>.Ok(result.Delivery!)),
            AcceptOutcome.OrderNotFound => NotFound(ApiResult<RiderDeliveryResponse>.Fail(result.Message!)),
            AcceptOutcome.RiderNotFound => NotFound(ApiResult<RiderDeliveryResponse>.Fail(result.Message!)),
            AcceptOutcome.AlreadyTaken => Conflict(ApiResult<RiderDeliveryResponse>.Fail(result.Message!)),
            AcceptOutcome.RiderNotEligible => StatusCode(
                StatusCodes.Status403Forbidden,
                ApiResult<RiderDeliveryResponse>.Fail(result.Message!)),
            _ => BadRequest(ApiResult<RiderDeliveryResponse>.Fail(result.Message!)),
        };
    }

    /// <summary>推进配送状态：PickUp / StartDelivering / Complete。</summary>
    [HttpPut("deliveries/{orderId:int}/status")]
    public async Task<ActionResult<ApiResult<RiderDeliveryResponse>>> Advance(
        int orderId,
        [FromBody] AdvanceDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.AdvanceAsync(orderId, User.GetProfileId(), request, cancellationToken);
        return result.Outcome switch
        {
            AdvanceOutcome.Success => Ok(ApiResult<RiderDeliveryResponse>.Ok(result.Delivery!)),
            AdvanceOutcome.NotFound => NotFound(ApiResult<RiderDeliveryResponse>.Fail(result.Message!)),
            AdvanceOutcome.InvalidRequest => BadRequest(ApiResult<RiderDeliveryResponse>.Fail(result.Message!)),
            _ => Conflict(ApiResult<RiderDeliveryResponse>.Fail(result.Message!)),
        };
    }
}
