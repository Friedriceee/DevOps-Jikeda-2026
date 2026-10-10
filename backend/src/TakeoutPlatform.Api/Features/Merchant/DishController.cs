using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TakeoutPlatform.Api.Common;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Features.Merchant;

[ApiController]
[Route("api/merchant")]
public sealed class DishController : ControllerBase
{
    private readonly DishService _dishService;

    public DishController(DishService dishService) => _dishService = dishService;

    [HttpPost("dishes")]
    [Authorize(Roles = nameof(AccountRole.Merchant))]
    public async Task<ActionResult<ApiResult<DishResponse>>> Create(
        [FromBody] CreateDishRequest request,
        CancellationToken cancellationToken)
    {
        request.MerchantId = User.GetProfileId();
        var result = await _dishService.CreateAsync(request, cancellationToken);

        if (!result.MerchantFound)
        {
            return NotFound(ApiResult<DishResponse>.Fail("Merchant was not found."));
        }

        return CreatedAtAction(
            nameof(List),
            new { merchantId = result.Dish!.MerchantId },
            ApiResult<DishResponse>.Ok(result.Dish));
    }

    [HttpGet("{merchantId:int}/dishes")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<DishResponse>>>> List(
        int merchantId,
        CancellationToken cancellationToken)
    {
        var dishes = await _dishService.ListAsync(merchantId, cancellationToken);

        if (dishes is null)
        {
            return NotFound(ApiResult<IReadOnlyList<DishResponse>>.Fail("Merchant was not found."));
        }

        return Ok(ApiResult<IReadOnlyList<DishResponse>>.Ok(dishes));
    }

    [HttpPut("dishes/{dishId:int}")]
    [Authorize(Roles = nameof(AccountRole.Merchant))]
    public async Task<ActionResult<ApiResult<DishResponse>>> Update(
        int dishId,
        [FromBody] UpdateDishRequest request,
        CancellationToken cancellationToken)
    {
        var dish = await _dishService.UpdateAsync(
            dishId,
            User.GetProfileId(),
            request,
            cancellationToken);

        return dish is null
            ? NotFound(ApiResult<DishResponse>.Fail("Menu item was not found or belongs to another merchant."))
            : Ok(ApiResult<DishResponse>.Ok(dish));
    }

    [HttpDelete("dishes/{dishId:int}")]
    [Authorize(Roles = nameof(AccountRole.Merchant))]
    public async Task<ActionResult<ApiResult<object>>> Delete(
        int dishId,
        CancellationToken cancellationToken)
    {
        var deleted = await _dishService.DeleteAsync(
            dishId,
            User.GetProfileId(),
            cancellationToken);

        if (!deleted)
        {
            return NotFound(ApiResult<object>.Fail("Menu item was not found, belongs to another merchant, or has reservation history. Set inventory to zero to stop new orders."));
        }

        return Ok(ApiResult.Ok());
    }
}
