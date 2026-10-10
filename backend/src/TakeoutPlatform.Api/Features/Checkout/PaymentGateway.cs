using Microsoft.Extensions.Options;

namespace TakeoutPlatform.Api.Features.Checkout;

public sealed class PaymentOptions
{
    public const string SectionName = "Payment";

    /// <summary>
    /// Allows the <see cref="SimulatedPaymentGateway.DeclineMethod"/> test card.
    /// It exists so the failure path can be demonstrated and smoke-tested; a real
    /// gateway would never expose it.
    /// </summary>
    public bool AllowSimulatedDecline { get; set; } = true;
}

public sealed record PaymentGatewayRequest(int UserId, decimal Amount, string Method, string IdempotencyKey);

public sealed record PaymentGatewayResult(bool Approved, string Reference, string? DeclineReason = null);

/// <summary>Boundary to the payment provider. Only a simulator exists in Sprint 3.</summary>
public interface IPaymentGateway
{
    bool SupportsMethod(string method);
    Task<PaymentGatewayResult> ChargeAsync(PaymentGatewayRequest request, CancellationToken cancellationToken);
}

public sealed class SimulatedPaymentGateway : IPaymentGateway
{
    public const string ApproveMethod = "SimulatedCard";
    public const string DeclineMethod = "SimulatedCardDeclined";

    private readonly PaymentOptions _options;

    public SimulatedPaymentGateway(IOptions<PaymentOptions> options) => _options = options.Value;

    public bool SupportsMethod(string method) =>
        method == ApproveMethod || (method == DeclineMethod && _options.AllowSimulatedDecline);

    public Task<PaymentGatewayResult> ChargeAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Method == DeclineMethod)
        {
            return Task.FromResult(new PaymentGatewayResult(
                false, string.Empty, "The simulated card was declined."));
        }

        return Task.FromResult(new PaymentGatewayResult(
            true, "SIM-" + Guid.NewGuid().ToString("N")[..16].ToUpperInvariant()));
    }
}
