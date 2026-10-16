using Microsoft.AspNetCore.Mvc;
using TakeoutPlatform.Api.Common;

namespace TakeoutPlatform.Api.Features.Merchant;

[ApiController]
[Route("api/merchant")]
public sealed class MerchantRegistrationController : ControllerBase
{
    private readonly MerchantRegistrationService _service;

    public MerchantRegistrationController(MerchantRegistrationService service) => _service = service;

    [HttpPost("register")]
    public async Task<ActionResult<ApiResult<MerchantRegistrationResponse>>> Register(
        [FromBody] RegisterMerchantRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.MerchantName) ||
            string.IsNullOrWhiteSpace(request.MerchantAddress) ||
            string.IsNullOrWhiteSpace(request.Contact))
            return BadRequest(ApiResult<MerchantRegistrationResponse>.Fail("Required fields cannot be empty."));

        var result = await _service.RegisterAsync(request, cancellationToken);
        return result.Conflict
            ? Conflict(ApiResult<MerchantRegistrationResponse>.Fail("Username already exists."))
            : StatusCode(StatusCodes.Status201Created,
                ApiResult<MerchantRegistrationResponse>.Ok(result.Merchant!));
    }
}
