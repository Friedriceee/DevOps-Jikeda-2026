using System.ComponentModel.DataAnnotations;

namespace TakeoutPlatform.Api.Features.Cart;

public sealed class AddCartItemRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "dishId 必须大于 0")]
    public int DishId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "dishNum 必须大于 0")]
    public int DishNum { get; set; } = 1;
}

public sealed class UpdateCartItemRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "dishNum 必须大于 0")]
    public int DishNum { get; set; }
}
