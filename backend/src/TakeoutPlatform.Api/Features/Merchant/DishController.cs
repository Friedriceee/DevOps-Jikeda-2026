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
            return NotFound(ApiResult<DishResponse>.Fail("商家不存在"));
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
            return NotFound(ApiResult<IReadOnlyList<DishResponse>>.Fail("商家不存在"));
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
            ? NotFound(ApiResult<DishResponse>.Fail("菜品不存在或不属于该商家"))
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
            return NotFound(ApiResult<object>.Fail("菜品不存在或不属于该商家"));
        }

        return Ok(ApiResult.Ok());
    }
}
