using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AutoHub.Platform.Integrations;
using AutoHub.Platform.Integrations.PaymentCollect;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace AutoHub.Modules.Payments.Integration;

/// <summary>
/// A payment provider that works on a laptop with no credentials and no internet.
/// </summary>
/// <remarks>
/// <b>What this is for.</b> Every flow in the platform that matters — a C2C sale settling, an
/// auction win being collected, a deposit held and released, a payout batch instructed — needs a
/// gateway to answer before any of it can be run end to end. Waiting for Razorpay credentials to
/// exercise the ledger is how a team ships a settlement path that has never once executed.
/// <para>
/// <b>What stops it reaching production.</b> Three things, layered, because a fake payment adapter
/// is the most dangerous class in the codebase: it can make a deal settle, a commission post and a
/// vehicle change hands with no money anywhere.
/// </para>
/// <list type="number">
/// <item>It is a normal <c>IIntegrationAdapter</c> with <see cref="ProviderCode"/>
/// <c>development</c>, so it only ever runs if a route in the integration registry points at it.
/// Production's routing table does not.</item>
/// <item>It refuses unless its provider config carries <c>"mode": "development"</c> — opt-in, so a
/// missing setting is a loud failure rather than a silent success.</item>
/// <item>It never claims money was captured. Collection returns
/// <see cref="PaymentCollectStatus.Created"/> with a callback URL, and capture happens through the
/// normal webhook path, so the code exercised locally is the same code a real gateway drives.</item>
/// </list>
/// <para>
/// The outcome is deterministic in the idempotency key rather than random. A test that fails one run
/// in twenty is a test that gets deleted, and a developer who cannot reproduce yesterday's failure
/// stops trusting the harness.
/// </para>
/// </remarks>
public sealed class DevelopmentPaymentCollectAdapter
    : IIntegrationAdapter<PaymentCollectRequest, PaymentCollectResponse>
{
    private readonly ILogger<DevelopmentPaymentCollectAdapter> _logger;

    public DevelopmentPaymentCollectAdapter(ILogger<DevelopmentPaymentCollectAdapter> logger)
        => _logger = logger;

    /// <summary>Matches the provider row an environment's routing table points at.</summary>
    public const string Code = "development";

    /// <summary>The config value that must be present for this adapter to do anything at all.</summary>
    public const string RequiredMode = "development";

    public string ProviderCode => Code;

    public string CapabilityCode => IntegrationCapabilities.PaymentCollect;

    public Task<Result<PaymentCollectResponse>> ExecuteAsync(
        PaymentCollectRequest request,
        IntegrationInvocation invocation,
        CancellationToken cancellationToken)
    {
        var guard = DevelopmentGatewayMode.Require(invocation.Provider, Code);
        if (guard.IsFailure)
        {
            return Task.FromResult(Result.Failure<PaymentCollectResponse>(guard.Error));
        }

        if (!request.Amount.IsPositive)
        {
            return Task.FromResult(Result.Failure<PaymentCollectResponse>(
                Error.Validation("payment.amount_not_positive", "A collection must be for a positive amount.")));
        }

        _logger.LogInformation(
            "Development gateway accepted collection {Reference} for {AmountMinor} {Currency}",
            request.OrderReference, request.Amount.MinorUnits, request.Amount.Currency.Code);

        var reference = DevelopmentGatewayMode.DeriveReference("pay", invocation.IdempotencyKey);

        return Task.FromResult(Result.Success(new PaymentCollectResponse
        {
            ProviderCode = Code,
            ProviderReference = reference,
            // Never Succeeded. Money is confirmed through the webhook, exactly as it is in production.
            Status = PaymentCollectStatus.Created,
            Amount = request.Amount,
            RedirectUrl = $"{guard.Value.CallbackBaseUrl}/collect/{reference}",
            UpiIntentUrl = string.Equals(request.Method, "upi", StringComparison.Ordinal)
                ? BuildUpiIntent(guard.Value.VirtualPaymentAddress, request)
                : null,
        }));
    }

    /// <summary>The <c>am</c> parameter is rupees to two decimals — the UPI spec's format, not ours.</summary>
    private static string BuildUpiIntent(string payeeAddress, PaymentCollectRequest request)
    {
        var rupees = (decimal)request.Amount.MinorUnits / request.Amount.Currency.MinorUnitsPerMajor;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"upi://pay?pa={payeeAddress}&am={rupees:0.00}&cu={request.Amount.Currency.Code}&tr={request.OrderReference}");
    }
}

/// <summary>The payout half of the development provider.</summary>
/// <remarks>
/// Returns <see cref="PaymentPayoutStatus.Accepted"/> and never <see cref="PaymentPayoutStatus.Settled"/>,
/// for the same reason its sibling never returns captured: settlement arrives on the bank's response
/// file, and a local flow that skipped that step would never exercise the code that reads it.
/// </remarks>
public sealed class DevelopmentPaymentPayoutAdapter
    : IIntegrationAdapter<PaymentPayoutRequest, PaymentPayoutResponse>
{
    private readonly ILogger<DevelopmentPaymentPayoutAdapter> _logger;

    public DevelopmentPaymentPayoutAdapter(ILogger<DevelopmentPaymentPayoutAdapter> logger)
        => _logger = logger;

    public string ProviderCode => DevelopmentPaymentCollectAdapter.Code;

    public string CapabilityCode => IntegrationCapabilities.PaymentPayout;

    public Task<Result<PaymentPayoutResponse>> ExecuteAsync(
        PaymentPayoutRequest request,
        IntegrationInvocation invocation,
        CancellationToken cancellationToken)
    {
        var guard = DevelopmentGatewayMode.Require(invocation.Provider, ProviderCode);
        if (guard.IsFailure)
        {
            return Task.FromResult(Result.Failure<PaymentPayoutResponse>(guard.Error));
        }

        _logger.LogInformation(
            "Development gateway accepted payout {Reference} for {AmountMinor} {Currency}",
            request.InstructionReference, request.Amount.MinorUnits, request.Amount.Currency.Code);

        return Task.FromResult(Result.Success(new PaymentPayoutResponse
        {
            ProviderCode = ProviderCode,
            ProviderReference = DevelopmentGatewayMode.DeriveReference("pout", invocation.IdempotencyKey),
            Status = PaymentPayoutStatus.Accepted,
            Amount = request.Amount,
        }));
    }
}

/// <summary>
/// The opt-in guard both development adapters share.
/// </summary>
/// <remarks>
/// Modelled on the platform's <c>PaymentSimulation</c> and refusing in the same direction: an
/// <c>Unexpected</c> error rather than an upstream one, so the invoker does not treat the refusal as
/// a transient vendor problem and fail over into another stub.
/// </remarks>
public sealed record DevelopmentGatewayMode
{
    private DevelopmentGatewayMode(string callbackBaseUrl, string virtualPaymentAddress)
    {
        CallbackBaseUrl = callbackBaseUrl;
        VirtualPaymentAddress = virtualPaymentAddress;
    }

    private const string ModeKey = "mode";

    public string CallbackBaseUrl { get; }

    public string VirtualPaymentAddress { get; }

    public static Result<DevelopmentGatewayMode> Require(ResolvedProvider provider, string providerCode)
    {
        var config = provider.Config as System.Text.Json.Nodes.JsonObject;
        var mode = config?[ModeKey]?.GetValue<string>();

        if (!string.Equals(mode, DevelopmentPaymentCollectAdapter.RequiredMode, StringComparison.Ordinal))
        {
            return Error.Unexpected(
                "integration.development_gateway_not_enabled",
                "The development payment provider only runs when its config sets mode to 'development'.")
                .With("provider", providerCode);
        }

        return new DevelopmentGatewayMode(
            config?["callbackBaseUrl"]?.GetValue<string>() ?? "https://localhost.invalid/dev-gateway",
            config?["vpa"]?.GetValue<string>() ?? "autohub@dev");
    }

    /// <summary>
    /// A stable pseudo-reference derived from the idempotency key.
    /// </summary>
    /// <remarks>
    /// SHA-256 rather than <c>string.GetHashCode</c>, which is randomised per process in .NET Core:
    /// a reference that changes when the API restarts would break exactly the replay behaviour this
    /// adapter exists to demonstrate.
    /// </remarks>
    public static string DeriveReference(string prefix, string idempotencyKey)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(idempotencyKey));
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{prefix}_{Convert.ToHexString(digest.AsSpan(0, 8)).ToUpperInvariant()}");
    }
}
