using System.Globalization;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace AutoHub.Platform.Integrations.PaymentCollect;

/// <summary>
/// Razorpay's implementation of <c>payment.collect</c>.
/// </summary>
/// <remarks>
/// Razorpay's Orders API takes the amount in <b>paise, as an integer</b>, and returns an
/// <c>order_id</c> that the client-side checkout then consumes. Compare
/// <see cref="CashfreePaymentCollectAdapter"/>, which takes the amount in <b>rupees, as a decimal</b>
/// — the same money, two incompatible representations, and exactly the seam where a platform that
/// passed <c>decimal</c> rupees around internally would eventually lose a paisa. Because
/// <see cref="Money"/> is minor units end to end, this adapter passes
/// <see cref="Money.MinorUnits"/> straight through and the other one does the conversion once, in
/// the one place that is allowed to know about it.
/// <para>
/// <b>This build simulates.</b> The HTTP client, request signing and webhook verification live in
/// <c>AutoHub.Integrations</c>, which is where a project that talks to the outside world belongs.
/// This class exists so that routing, failover and the circuit breaker are demonstrable and testable
/// today, and it refuses to run unless its provider config explicitly says <c>"mode": "simulate"</c>
/// — a fake payment adapter that would silently return success in production is a worse bug than no
/// adapter at all.
/// </para>
/// </remarks>
public sealed class RazorpayPaymentCollectAdapter
    : IIntegrationAdapter<PaymentCollectRequest, PaymentCollectResponse>
{
    private readonly ISecretResolver _secrets;
    private readonly ILogger<RazorpayPaymentCollectAdapter> _logger;

    public RazorpayPaymentCollectAdapter(
        ISecretResolver secrets,
        ILogger<RazorpayPaymentCollectAdapter> logger)
    {
        _secrets = secrets;
        _logger = logger;
    }

    public string ProviderCode => "razorpay";

    public string CapabilityCode => IntegrationCapabilities.PaymentCollect;

    public async Task<Result<PaymentCollectResponse>> ExecuteAsync(
        PaymentCollectRequest request,
        IntegrationInvocation invocation,
        CancellationToken cancellationToken)
    {
        if (request.Amount.Currency != Currency.Inr)
        {
            // A validation failure, not an upstream one, so the invoker does not waste the fallback
            // provider's latency discovering the same thing.
            return Error.Validation(
                "payment.currency_unsupported",
                "Razorpay domestic collection settles in INR only.")
                .With("currency", request.Amount.Currency.Code);
        }

        var simulation = PaymentSimulation.Require(invocation.Provider, ProviderCode);
        if (simulation.IsFailure)
        {
            return Result.Failure<PaymentCollectResponse>(simulation.Error);
        }

        // Resolved per call and never held. The real adapter signs the request with this; here it
        // proves the credential path works end to end, which is the part that is usually discovered
        // to be broken at go-live.
        var keyId = await _secrets.ResolveAsync(invocation.Provider.CredentialRef, "key_id", cancellationToken);
        if (keyId.IsFailure)
        {
            return Result.Failure<PaymentCollectResponse>(keyId.Error);
        }

        _logger.LogInformation(
            "Razorpay order for {Reference}: {AmountMinor} paise, idempotency {Key}",
            request.OrderReference, request.Amount.MinorUnits, invocation.IdempotencyKey);

        // Razorpay speaks paise. Money already is paise, so nothing is converted and nothing rounds.
        var amountInPaise = request.Amount.MinorUnits;

        return new PaymentCollectResponse
        {
            ProviderCode = ProviderCode,
            ProviderReference = $"order_{PaymentSimulation.Reference(invocation.IdempotencyKey)}",
            Status = PaymentCollectStatus.Created,
            Amount = Money.FromMinorUnits(amountInPaise, Currency.Inr),
            RedirectUrl = $"{simulation.Value.CheckoutBaseUrl}/{invocation.IdempotencyKey}",
            UpiIntentUrl = string.Equals(request.Method, "upi", StringComparison.Ordinal)
                ? BuildUpiIntent(simulation.Value.VirtualPaymentAddress, request)
                : null,
        };
    }

    /// <summary>
    /// A UPI deep link, built here rather than by the caller.
    /// </summary>
    /// <remarks>
    /// The <c>am</c> parameter is rupees with two decimals — the UPI specification's format, not
    /// ours — so the conversion from minor units happens at the boundary where it is required and
    /// nowhere else.
    /// </remarks>
    private static string BuildUpiIntent(string payeeAddress, PaymentCollectRequest request)
    {
        var rupees = (decimal)request.Amount.MinorUnits / request.Amount.Currency.MinorUnitsPerMajor;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"upi://pay?pa={payeeAddress}&am={rupees:0.00}&cu={request.Amount.Currency.Code}&tr={request.OrderReference}");
    }
}
