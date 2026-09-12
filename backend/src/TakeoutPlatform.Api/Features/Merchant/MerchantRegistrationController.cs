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
            return BadRequest(ApiResult<MerchantRegistrationResponse>.Fail("必填字段不能为空"));

        var result = await _service.RegisterAsync(request, cancellationToken);
        return result.Conflict
            ? Conflict(ApiResult<MerchantRegistrationResponse>.Fail("用户名已存在"))
            : StatusCode(StatusCodes.Status201Created,
                ApiResult<MerchantRegistrationResponse>.Ok(result.Merchant!));
    }
}
