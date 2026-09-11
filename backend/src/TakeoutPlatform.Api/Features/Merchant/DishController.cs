using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TakeoutPlatform.Api.Common;

namespace TakeoutPlatform.Api.Features.Merchant;

[ApiController]
[Route("api/merchant")]
public sealed class DishController : ControllerBase
{
    private readonly DishService _dishService;

    public DishController(DishService dishService) => _dishService = dishService;

    [HttpPost("dishes")]
    public async Task<ActionResult<ApiResult<DishResponse>>> Create(
        [FromBody] CreateDishRequest request,
        CancellationToken cancellationToken)
    {
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

    /// <summary>编辑菜品（US-03）。</summary>
    [HttpPut("dishes/{dishId:int}")]
    public async Task<ActionResult<ApiResult<DishResponse>>> Update(
        int dishId,
        [FromBody] UpdateDishRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _dishService.UpdateAsync(dishId, request, cancellationToken);

        if (!result.Found)
        {
            return NotFound(ApiResult<DishResponse>.Fail("菜品不存在"));
        }

        return Ok(ApiResult<DishResponse>.Ok(result.Dish!));
    }

    /// <summary>下架/删除菜品（US-04）。</summary>
    [HttpDelete("dishes/{dishId:int}")]
    public async Task<ActionResult<ApiResult<object>>> Delete(
        int dishId,
        [FromQuery] [Range(1, int.MaxValue, ErrorMessage = "merchantId 必须大于 0")] int merchantId,
        CancellationToken cancellationToken)
    {
        var found = await _dishService.DeleteAsync(dishId, merchantId, cancellationToken);

        if (!found)
        {
            return NotFound(ApiResult<object>.Fail("菜品不存在"));
        }

        return Ok(ApiResult.Ok());
    }
}
