using System.Text.Json.Nodes;
using AutoHub.Platform.Integrations;
using AutoHub.Platform.Integrations.PaymentCollect;
using AutoHub.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace AutoHub.Modules.Payments.Integration;

/// <summary>
/// Taking money in and sending money out, without naming a vendor.
/// </summary>
/// <remarks>
/// A thin, deliberate facade over <c>IIntegrationInvoker</c>. Handlers could call the invoker
/// directly; they do not, because then every handler would repeat the capability code, the routing
/// facts and the idempotency plumbing, and one of them would eventually get the idempotency key
/// wrong. One place gets it right.
/// <para>
/// Which provider actually answers — Razorpay, Cashfree, a bank's own gateway, the development
/// stub — is decided by the integration registry per city, per amount, per method, and neither this
/// interface nor anything above it learns that a decision was made.
/// </para>
/// </remarks>
public interface IPaymentGateway
{
    Task<Result<PaymentCollectResponse>> CollectAsync(
        PaymentCollectRequest request,
        PaymentRoutingFacts facts,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<Result<PaymentPayoutResponse>> PayoutAsync(
        PaymentPayoutRequest request,
        PaymentRoutingFacts facts,
        string idempotencyKey,
        CancellationToken cancellationToken);
}

/// <summary>
/// The facts the routing table matches on.
/// </summary>
/// <remarks>
/// Typed rather than a raw <c>JsonNode</c> so a caller cannot mistype <c>ammount</c> and silently
/// fall through to the default route. Serialised to the shape the registry's conditions are authored
/// against by <see cref="ToJson"/>, which is the one place the wire names live.
/// </remarks>
public sealed record PaymentRoutingFacts
{
    public string? CityCode { get; init; }

    public string? StateCode { get; init; }

    public string? MethodCode { get; init; }

    public long AmountMinorUnits { get; init; }

    public string? PartyTier { get; init; }

    public JsonObject ToJson() => new()
    {
        ["city"] = CityCode,
        ["state"] = StateCode,
        ["method"] = MethodCode,
        ["amountMinor"] = AmountMinorUnits,
        ["party"] = new JsonObject { ["tier"] = PartyTier },
    };
}

/// <summary>The invoker-backed implementation. The only one in the product.</summary>
public sealed class PaymentGateway : IPaymentGateway
{
    private readonly IIntegrationInvoker _invoker;
    private readonly ILogger<PaymentGateway> _logger;

    public PaymentGateway(IIntegrationInvoker invoker, ILogger<PaymentGateway> logger)
    {
        _invoker = invoker;
        _logger = logger;
    }

    public async Task<Result<PaymentCollectResponse>> CollectAsync(
        PaymentCollectRequest request,
        PaymentRoutingFacts facts,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var outcome = await _invoker.InvokeAsync<PaymentCollectRequest, PaymentCollectResponse>(
            IntegrationCapabilities.PaymentCollect,
            request,
            BuildContext(facts, idempotencyKey),
            idempotencyKey,
            cancellationToken);

        if (outcome.IsFailure && IntegrationErrors.IsIndeterminate(outcome.Error))
        {
            // The charge may exist. The chain has already stopped rather than failing over, and the
            // caller must not retry either — the payment is reconciled against the provider's
            // settlement file, which is the only source that can say what actually happened.
            _logger.LogError(
                "Collection {IdempotencyKey} is indeterminate and must be reconciled, not retried: {Code}",
                idempotencyKey, outcome.Error.Code);
        }

        return outcome;
    }

    public Task<Result<PaymentPayoutResponse>> PayoutAsync(
        PaymentPayoutRequest request,
        PaymentRoutingFacts facts,
        string idempotencyKey,
        CancellationToken cancellationToken)
        => _invoker.InvokeAsync<PaymentPayoutRequest, PaymentPayoutResponse>(
            IntegrationCapabilities.PaymentPayout,
            request,
            BuildContext(facts, idempotencyKey),
            idempotencyKey,
            cancellationToken);

    /// <summary>
    /// Builds the routing context, pinning the affinity key to the caller's idempotency key.
    /// </summary>
    /// <remarks>
    /// Affinity is what stops a retry landing on a different provider from the attempt it is
    /// retrying. Canary splitting hashes this value, so the same key deterministically re-selects the
    /// provider that already holds the charge — which is the difference between a retry and a second
    /// debit.
    /// </remarks>
    private static IntegrationContext BuildContext(PaymentRoutingFacts facts, string idempotencyKey)
        => new()
        {
            Facts = facts.ToJson(),
            AffinityKey = idempotencyKey,
        };
}
