using Microsoft.AspNetCore.Mvc;
using TakeoutPlatform.Api.Common;

namespace TakeoutPlatform.Api.Features.Order;

[ApiController]
[Route("api/orders")]
public sealed class OrderController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrderController(OrderService orderService) => _orderService = orderService;

    /// <summary>创建待支付订单。</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResult<OrderResponse>>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.CreateAsync(request, cancellationToken);

        switch (result.Outcome)
        {
            case CreateOrderOutcome.Success:
                return CreatedAtAction(
                    nameof(List),
                    new { userId = result.Order!.UserId },
                    ApiResult<OrderResponse>.Ok(result.Order));

            case CreateOrderOutcome.DishNotFound:
                return NotFound(ApiResult<OrderResponse>.Fail(result.Message!));

            // 购物车为空、库存不足都属于业务请求错误。
            case CreateOrderOutcome.EmptyCart:
            case CreateOrderOutcome.InsufficientStock:
            default:
                return BadRequest(ApiResult<OrderResponse>.Fail(result.Message!));
        }
    }

    /// <summary>按用户查询订单列表。</summary>
    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<OrderResponse>>>> List(
        int userId,
        CancellationToken cancellationToken)
    {
        var orders = await _orderService.ListByUserAsync(userId, cancellationToken);
        return Ok(ApiResult<IReadOnlyList<OrderResponse>>.Ok(orders));
    }

    /// <summary>取消待支付订单。</summary>
    [HttpDelete("{orderId:int}")]
    public async Task<ActionResult<ApiResult<object>>> Cancel(
        int orderId,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.CancelAsync(orderId, cancellationToken);

        return result.Outcome switch
        {
            CancelOrderOutcome.Success => Ok(ApiResult.Ok()),
            CancelOrderOutcome.NotFound => NotFound(ApiResult<object>.Fail(result.Message!)),
            _ => BadRequest(ApiResult<object>.Fail(result.Message!)),
        };
    }
}
