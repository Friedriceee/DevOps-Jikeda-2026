using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TakeoutPlatform.Api.Common;

namespace TakeoutPlatform.Api.Features.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService) => _authService = authService;

    [AllowAnonymous]
    [HttpPost("customer/register")]
    public async Task<ActionResult<ApiResult<CustomerRegistrationResponse>>> RegisterCustomer(
        [FromBody] RegisterCustomerRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.DisplayName))
            return BadRequest(ApiResult<CustomerRegistrationResponse>.Fail("必填字段不能为空"));

        var result = await _authService.RegisterCustomerAsync(request, cancellationToken);
        return result.Conflict
            ? Conflict(ApiResult<CustomerRegistrationResponse>.Fail("用户名已存在"))
            : StatusCode(StatusCodes.Status201Created,
                ApiResult<CustomerRegistrationResponse>.Ok(result.Customer!));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResult<LoginResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var login = await _authService.LoginAsync(request, cancellationToken);
        return login is null
            ? Unauthorized(ApiResult<LoginResponse>.Fail("用户名或密码错误"))
            : Ok(ApiResult<LoginResponse>.Ok(login));
    }
}
