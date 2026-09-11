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
}
