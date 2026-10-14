using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TakeoutPlatform.Api.Common;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Features.Checkout;

[ApiController]
[Route("api/checkout")]
[Authorize(Roles = nameof(AccountRole.Customer))]
public sealed class CheckoutController : ControllerBase
{
    public const string IdempotencyHeader = "Idempotency-Key";

    private readonly CheckoutService _checkoutService;

    public CheckoutController(CheckoutService checkoutService) => _checkoutService = checkoutService;

    /// <summary>
    /// Prices the signed-in customer's cart on the server, reserves stock, takes a
    /// simulated payment and returns the paid order. Safe to retry with the same
    /// <c>Idempotency-Key</c>: a repeat returns 200 with <c>replayed: true</c>.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResult<CheckoutResponse>>> Checkout(
        [FromBody] CheckoutRequest request,
        [FromHeader(Name = IdempotencyHeader)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _checkoutService.CheckoutAsync(
            User.GetProfileId(), idempotencyKey, request, cancellationToken);

        if (result.Succeeded)
        {
            var body = ApiResult<CheckoutResponse>.Ok(result.Response!);
            return result.Outcome == CheckoutOutcome.Replayed
                ? Ok(body)
                : StatusCode(StatusCodes.Status201Created, body);
        }

        var failure = ApiResult<CheckoutResponse>.Fail(result.Message!, result.Code);
        return result.Outcome switch
        {
            CheckoutOutcome.InvalidIdempotencyKey
                or CheckoutOutcome.UnsupportedPaymentMethod
                or CheckoutOutcome.PricingFailed => BadRequest(failure),
            CheckoutOutcome.AddressNotFound => NotFound(failure),
            CheckoutOutcome.PaymentDeclined => StatusCode(StatusCodes.Status402PaymentRequired, failure),
            _ => Conflict(failure),
        };
    }
}
