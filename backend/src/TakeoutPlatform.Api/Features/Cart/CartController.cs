using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TakeoutPlatform.Api.Common;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Features.Cart;

[ApiController]
[Route("api/cart")]
[Authorize(Roles = nameof(AccountRole.Customer))]
public sealed class CartController : ControllerBase
{
    private readonly CartService _cartService;

    public CartController(CartService cartService) => _cartService = cartService;

    [HttpGet]
    public async Task<ActionResult<ApiResult<CartResponse>>> Get(CancellationToken cancellationToken)
    {
        var cart = await _cartService.GetAsync(User.GetProfileId(), cancellationToken);
        return cart is null
            ? NotFound(ApiResult<CartResponse>.Fail("用户不存在"))
            : Ok(ApiResult<CartResponse>.Ok(cart));
    }

    [HttpPost("items")]
    public async Task<ActionResult<ApiResult<CartResponse>>> Add(
        [FromBody] AddCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _cartService.AddAsync(User.GetProfileId(), request, cancellationToken);
        return ToWriteActionResult(result, created: true);
    }

    [HttpPut("items/{cartItemId:int}")]
    public async Task<ActionResult<ApiResult<CartResponse>>> Update(
        int cartItemId,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _cartService.UpdateAsync(
            User.GetProfileId(), cartItemId, request, cancellationToken);
        return ToWriteActionResult(result);
    }

    [HttpDelete("items/{cartItemId:int}")]
    public async Task<ActionResult<ApiResult<CartResponse>>> DeleteItem(
        int cartItemId,
        CancellationToken cancellationToken)
    {
        var result = await _cartService.DeleteItemAsync(
            User.GetProfileId(), cartItemId, cancellationToken);
        return ToWriteActionResult(result);
    }

    [HttpDelete("merchants/{merchantId:int}")]
    public async Task<ActionResult<ApiResult<CartResponse>>> DeleteMerchantItems(
        int merchantId,
        CancellationToken cancellationToken)
    {
        var result = await _cartService.DeleteMerchantItemsAsync(
            User.GetProfileId(), merchantId, cancellationToken);
        return ToWriteActionResult(result);
    }

    [HttpDelete]
    public async Task<ActionResult<ApiResult<CartResponse>>> Clear(CancellationToken cancellationToken)
    {
        var cart = await _cartService.ClearAsync(User.GetProfileId(), cancellationToken);
        return Ok(ApiResult<CartResponse>.Ok(cart));
    }

    private ActionResult<ApiResult<CartResponse>> ToWriteActionResult(
        CartWriteResult result,
        bool created = false) => result.Outcome switch
    {
        CartWriteOutcome.Success when created => StatusCode(
            StatusCodes.Status201Created,
            ApiResult<CartResponse>.Ok(result.Cart!)),
        CartWriteOutcome.Success => Ok(ApiResult<CartResponse>.Ok(result.Cart!)),
        CartWriteOutcome.UserNotFound or CartWriteOutcome.DishNotFound or CartWriteOutcome.CartItemNotFound
            => NotFound(ApiResult<CartResponse>.Fail(result.Message!)),
        _ => BadRequest(ApiResult<CartResponse>.Fail(result.Message!)),
    };
}
