using Microsoft.AspNetCore.Mvc;
using TakeoutPlatform.Api.Common;

namespace TakeoutPlatform.Api.Features.User;

[ApiController]
[Route("api/user")]
public sealed class AddressController : ControllerBase
{
    private readonly AddressService _addressService;

    public AddressController(AddressService addressService) => _addressService = addressService;

    [HttpPost("addresses")]
    public async Task<ActionResult<ApiResult<AddressResponse>>> Create(
        [FromBody] CreateAddressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _addressService.CreateAsync(request, cancellationToken);

        if (!result.UserFound)
        {
            return NotFound(ApiResult<AddressResponse>.Fail("用户不存在"));
        }

        return CreatedAtAction(
            nameof(List),
            new { userId = result.Address!.UserId },
            ApiResult<AddressResponse>.Ok(result.Address));
    }

    [HttpGet("{userId:int}/addresses")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<AddressResponse>>>> List(
        int userId,
        CancellationToken cancellationToken)
    {
        var addresses = await _addressService.ListByUserAsync(userId, cancellationToken);

        if (addresses is null)
        {
            return NotFound(ApiResult<IReadOnlyList<AddressResponse>>.Fail("用户不存在"));
        }

        return Ok(ApiResult<IReadOnlyList<AddressResponse>>.Ok(addresses));
    }
}
