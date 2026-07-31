using AutoHub.Modules.Payments.Domain;
using AutoHub.Modules.Payments.Integration;
using AutoHub.Platform.Integrations.PaymentCollect;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AutoHub.Modules.Payments.Application;

/// <summary>
/// Starts a collection at whichever gateway the integration registry routes to.
/// </summary>
/// <remarks>
/// Creates a <see cref="PaymentIntent"/> <b>before</b> calling the provider, and that order is not
/// negotiable. If the provider is called first and the process dies before the row is written, a
/// charge exists at the gateway that this platform has no record of, and it is found weeks later in
/// a settlement file with nothing to match it to. Writing the intent first means the worst case is a
/// row with no charge, which reconciliation resolves quietly.
/// </remarks>
public sealed record CollectPaymentCommand : IRequest<Result<CollectPaymentResult>>
{
    /// <summary>The caller's <c>Idempotency-Key</c>. Carried through to the gateway unchanged.</summary>
    public required string IdempotencyKey { get; init; }

    public required long AmountMinorUnits { get; init; }

    public string CurrencyCode { get; init; } = "INR";

    /// <summary>Posting scope the eventual capture uses, e.g. <c>deal.collect</c>, <c>deposit.hold</c>.</summary>
    public required string PostingScope { get; init; }

    /// <summary>Our own reference shown on the vendor's settlement report — usually the deal reference.</summary>
    public required string OrderReference { get; init; }

    /// <summary>E.164. India is phone-first, so this is the one contact detail always present.</summary>
    public required string PayerContact { get; init; }

    public Guid? DealId { get; init; }

    public Guid? PayerId { get; init; }

    public string? PayerType { get; init; }

    public string? MethodCode { get; init; }

    public string? ReturnUrl { get; init; }

    public string? CityCode { get; init; }

    public string? StateCode { get; init; }

    public string? PayerTier { get; init; }
}

public sealed record CollectPaymentResult(
    Guid PaymentIntentId,
    string ProviderCode,
    string ProviderReference,
    PaymentCollectStatus Status,
    string? RedirectUrl,
    string? UpiIntentUrl);

public sealed class CollectPaymentHandler
    : IRequestHandler<CollectPaymentCommand, Result<CollectPaymentResult>>
{
    private readonly IPaymentIntentRepository _intents;
    private readonly IPaymentGateway _gateway;
    private readonly ILogger<CollectPaymentHandler> _logger;

    public CollectPaymentHandler(
        IPaymentIntentRepository intents,
        IPaymentGateway gateway,
        ILogger<CollectPaymentHandler> logger)
    {
        _intents = intents;
        _gateway = gateway;
        _logger = logger;
    }

    public async Task<Result<CollectPaymentResult>> Handle(
        CollectPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await _intents.FindByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            // A replay. Returning the original attempt is what the Idempotency-Key contract promises,
            // and it is what stops a double-tapped pay button becoming two charges.
            return new CollectPaymentResult(
                existing.Id,
                existing.ProviderCode ?? string.Empty,
                existing.ProviderReference ?? string.Empty,
                existing.Status,
                null,
                null);
        }

        Currency currency;
        try
        {
            currency = Currency.FromCode(request.CurrencyCode);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Error.Validation("payment.currency_unsupported", "That currency is not supported.")
                .With("currency", request.CurrencyCode);
        }

        var amount = Money.FromMinorUnits(request.AmountMinorUnits, currency);

        var created = PaymentIntent.Create(
            request.IdempotencyKey,
            amount,
            request.PostingScope,
            request.DealId,
            request.PayerId,
            request.PayerType,
            request.MethodCode);

        if (created.IsFailure)
        {
            return Result.Failure<CollectPaymentResult>(created.Error);
        }

        var intent = created.Value;

        await _intents.AddAsync(intent, cancellationToken);
        await _intents.SaveChangesAsync(cancellationToken);

        var response = await _gateway.CollectAsync(
            new PaymentCollectRequest
            {
                Amount = amount,
                OrderReference = request.OrderReference,
                CustomerContact = request.PayerContact,
                Method = request.MethodCode,
                ReturnUrl = request.ReturnUrl,
            },
            new PaymentRoutingFacts
            {
                CityCode = request.CityCode,
                StateCode = request.StateCode,
                MethodCode = request.MethodCode,
                AmountMinorUnits = amount.MinorUnits,
                PartyTier = request.PayerTier,
            },
            request.IdempotencyKey,
            cancellationToken);

        if (response.IsFailure)
        {
            _logger.LogWarning(
                "Collection {IdempotencyKey} was not started: {Code}",
                request.IdempotencyKey, response.Error.Code);

            // The intent stays. It is the record that lets reconciliation ask the provider whether a
            // charge exists, which is the only safe question after an indeterminate failure.
            return Result.Failure<CollectPaymentResult>(response.Error);
        }

        intent.RecordProviderResponse(response.Value);
        await _intents.SaveChangesAsync(cancellationToken);

        return new CollectPaymentResult(
            intent.Id,
            response.Value.ProviderCode,
            response.Value.ProviderReference,
            response.Value.Status,
            response.Value.RedirectUrl,
            response.Value.UpiIntentUrl);
    }
}

/// <summary>
/// Confirms money was captured, and posts the journal that records it.
/// </summary>
/// <remarks>
/// Driven by a gateway webhook, and therefore written for a webhook's realities: it arrives more than
/// once, it arrives out of order, and it occasionally arrives for something we have never heard of.
/// Each of those has an explicit answer here, and none of them is an exception.
/// <para>
/// Signature verification happens at the endpoint, before this command is constructed. A handler
/// that accepted an unverified capture would let anybody who can reach the webhook URL settle a deal.
/// </para>
/// </remarks>
public sealed record CapturePaymentCommand : IRequest<Result<Guid>>
{
    public required string ProviderCode { get; init; }

    public required string ProviderReference { get; init; }

    public required long AmountMinorUnits { get; init; }

    public string CurrencyCode { get; init; } = "INR";

    /// <summary>The lines to post, in the pricing registry's codes. Empty means post the whole amount as principal.</summary>
    public IReadOnlyList<CaptureLine> Lines { get; init; } = [];

    public IReadOnlyDictionary<string, PostingParty> PartiesByRole { get; init; } =
        new Dictionary<string, PostingParty>(StringComparer.Ordinal);
}

public sealed record CaptureLine(string Code, long AmountMinorUnits);

public sealed class CapturePaymentHandler : IRequestHandler<CapturePaymentCommand, Result<Guid>>
{
    private readonly IPaymentIntentRepository _intents;
    private readonly ILedgerPostingService _postings;
    private readonly IClock _clock;
    private readonly ILogger<CapturePaymentHandler> _logger;

    public CapturePaymentHandler(
        IPaymentIntentRepository intents,
        ILedgerPostingService postings,
        IClock clock,
        ILogger<CapturePaymentHandler> logger)
    {
        _intents = intents;
        _postings = postings;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CapturePaymentCommand request, CancellationToken cancellationToken)
    {
        var intent = await _intents.FindByProviderReferenceAsync(
            request.ProviderCode, request.ProviderReference, cancellationToken);

        if (intent is null)
        {
            // A capture for something we never started. Almost always a webhook for another
            // environment sharing a merchant account. Rejected loudly rather than ignored, because
            // the other possibility is a charge this platform genuinely lost track of.
            _logger.LogError(
                "Capture webhook for unknown reference {ProviderReference} from {ProviderCode}",
                request.ProviderReference, request.ProviderCode);

            return Error.NotFound(
                "payment.intent_not_found",
                "No payment attempt matches that provider reference.")
                .With("providerReference", request.ProviderReference);
        }

        if (intent.JournalId is { } alreadyPosted)
        {
            return alreadyPosted;
        }

        if (intent.Amount.MinorUnits != request.AmountMinorUnits)
        {
            // A partial capture, or a tampered webhook. Either way the ledger must not be told a
            // number the intent does not agree with; operations reconciles it by hand.
            return Error.Conflict(
                "payment.amount_mismatch",
                "The captured amount does not match the amount that was requested.")
                .With("expectedMinorUnits", intent.Amount.MinorUnits)
                .With("capturedMinorUnits", request.AmountMinorUnits);
        }

        var lines = request.Lines.Count > 0
            ? request.Lines
                .Select(l => new PostingLine(l.Code, Money.FromMinorUnits(l.AmountMinorUnits, intent.Amount.Currency)))
                .ToList()
            : [new PostingLine(CapturePrincipalCode, intent.Amount)];

        var posted = await _postings.PostAsync(
            new PostingRequest
            {
                ScopeCode = intent.PostingScope,
                IdempotencyKey = $"payment:{intent.Id:D}:capture",
                Currency = intent.Amount.Currency,
                Lines = lines,
                PartiesByRole = request.PartiesByRole,
                SourceType = "payments.payment_intent",
                SourceId = intent.Id,
                Description = $"Capture {request.ProviderReference} at {request.ProviderCode}",
            },
            cancellationToken);

        if (posted.IsFailure)
        {
            return Result.Failure<Guid>(posted.Error);
        }

        var captured = intent.Capture(posted.Value.Id, _clock.UtcNow);
        if (captured.IsFailure)
        {
            return Result.Failure<Guid>(captured.Error);
        }

        await _intents.SaveChangesAsync(cancellationToken);

        return posted.Value.Id;
    }

    /// <summary>The line code a capture posts under when the caller does not break it down.</summary>
    private const string CapturePrincipalCode = "principal";
}
