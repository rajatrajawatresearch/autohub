using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace AutoHub.Platform.Integrations.PaymentCollect;

/// <summary>
/// Cashfree's implementation of the same <c>payment.collect</c> capability.
/// </summary>
/// <remarks>
/// The same interface, the same request type, the same response type — and a completely different
/// wire contract underneath. Cashfree's Orders API takes <c>order_amount</c> as a <b>decimal number
/// of rupees</b> and returns a <c>payment_session_id</c> rather than an order id to redirect with.
/// <para>
/// That difference is the argument for this whole registry in one line of code. Every conversion,
/// every field rename, every "this vendor calls it a session and that one calls it an order" is
/// absorbed here. The deals module asks for <c>payment.collect</c> with a
/// <see cref="Money"/> and gets a <see cref="PaymentCollectResponse"/>; swapping which of these two
/// classes serves a given payment is one row in <see cref="ProviderRoute"/> and no code at all.
/// </para>
/// <para>
/// Simulating, and guarded by <see cref="PaymentSimulation"/>, for the reasons argued there.
/// </para>
/// </remarks>
public sealed class CashfreePaymentCollectAdapter
    : IIntegrationAdapter<PaymentCollectRequest, PaymentCollectResponse>
{
    private readonly ISecretResolver _secrets;
    private readonly ILogger<CashfreePaymentCollectAdapter> _logger;

    public CashfreePaymentCollectAdapter(
        ISecretResolver secrets,
        ILogger<CashfreePaymentCollectAdapter> logger)
    {
        _secrets = secrets;
        _logger = logger;
    }

    public string ProviderCode => "cashfree";

    public string CapabilityCode => IntegrationCapabilities.PaymentCollect;

    public async Task<Result<PaymentCollectResponse>> ExecuteAsync(
        PaymentCollectRequest request,
        IntegrationInvocation invocation,
        CancellationToken cancellationToken)
    {
        if (request.Amount.Currency != Currency.Inr)
        {
            return Error.Validation(
                "payment.currency_unsupported",
                "Cashfree domestic collection settles in INR only.")
                .With("currency", request.Amount.Currency.Code);
        }

        var simulation = PaymentSimulation.Require(invocation.Provider, ProviderCode);
        if (simulation.IsFailure)
        {
            return Result.Failure<PaymentCollectResponse>(simulation.Error);
        }

        var clientId = await _secrets.ResolveAsync(
            invocation.Provider.CredentialRef, "client_id", cancellationToken);

        if (clientId.IsFailure)
        {
            return Result.Failure<PaymentCollectResponse>(clientId.Error);
        }

        // The one conversion this vendor forces on us, done once, here, from an exact integer.
        // MinorUnitsPerMajor rather than a literal 100 so a future non-INR corridor does not
        // silently divide by the wrong scale.
        var orderAmountInRupees =
            (decimal)request.Amount.MinorUnits / request.Amount.Currency.MinorUnitsPerMajor;

        _logger.LogInformation(
            "Cashfree order for {Reference}: ₹{Amount}, idempotency {Key}",
            request.OrderReference, orderAmountInRupees, invocation.IdempotencyKey);

        var reference = PaymentSimulation.Reference(invocation.IdempotencyKey);

        return new PaymentCollectResponse
        {
            ProviderCode = ProviderCode,
            ProviderReference = $"session_{reference}",

            // Deliberately not Created. Cashfree returns an ACTIVE order that the customer is
            // already able to act on, and flattening both vendors onto one status is precisely the
            // normalisation this adapter exists to perform.
            Status = PaymentCollectStatus.Pending,

            // Converted back from the vendor's decimal to exact minor units. Round-tripping proves
            // the amount survived the vendor's representation, which is the assertion that catches a
            // 99.995 → 99.99 loss before it reaches the ledger.
            Amount = Money.FromMajorUnits(orderAmountInRupees, Currency.Inr),
            RedirectUrl = $"{simulation.Value.CheckoutBaseUrl}/checkout/{reference}",
            UpiIntentUrl = null,
        };
    }
}
