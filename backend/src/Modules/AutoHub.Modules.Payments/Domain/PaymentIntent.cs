using AutoHub.Platform.Integrations.PaymentCollect;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Payments.Domain;

/// <summary>
/// One attempt to take money from one payer, and everything the gateway told us about it.
/// </summary>
/// <remarks>
/// An intent is not a deal and is not a ledger posting. It is the record of a conversation with a
/// payment provider — which provider, under what reference, for how much, and how it ended. Keeping
/// it separate is what lets a deal have three failed card attempts and one successful UPI payment
/// without any of that appearing in the accounts, because only the capture posts.
/// <para>
/// <see cref="Status"/> is <c>PaymentCollectStatus</c>, the platform's normalisation of a dozen
/// incompatible vendor vocabularies. It is deliberately <b>not</b> a workflow state: what the
/// <i>deal</i> does when a payment succeeds is registry data, and an operator inserting a
/// "manual review above ₹10 lakh" step between capture and settlement must not need this enum
/// changed.
/// </para>
/// <para>
/// <see cref="IdempotencyKey"/> is the caller's, carried through to the provider. A retry must reuse
/// it, because a gateway that receives a new key treats a retry as a second charge — which is the
/// exact failure idempotency keys exist to prevent, reintroduced by the code meant to prevent it.
/// </para>
/// </remarks>
public sealed class PaymentIntent : AggregateRoot, IAuditable
{
    private PaymentIntent(Guid id, string idempotencyKey, Money amount)
        : base(id)
    {
        IdempotencyKey = idempotencyKey;
        Amount = amount;
        Status = PaymentCollectStatus.Created;
    }

    private PaymentIntent() { }

    public string IdempotencyKey { get; private set; } = string.Empty;

    public Money Amount { get; private set; }

    /// <summary>The deal this pays for. Null for a wallet top-up or a standalone subscription fee.</summary>
    public Guid? DealId { get; private set; }

    /// <summary>The scope the capture will post under, e.g. <c>deal.collect</c>, <c>deposit.hold</c>.</summary>
    public string PostingScope { get; private set; } = string.Empty;

    public Guid? PayerId { get; private set; }

    public string? PayerType { get; private set; }

    /// <summary>Taxonomy code for the instrument: <c>upi</c>, <c>card</c>, <c>netbanking</c>.</summary>
    public string? MethodCode { get; private set; }

    public string? ProviderCode { get; private set; }

    /// <summary>The vendor's own id for the attempt. What a refund and a reconciliation both key on.</summary>
    public string? ProviderReference { get; private set; }

    public PaymentCollectStatus Status { get; private set; }

    public DateTimeOffset? CapturedAt { get; private set; }

    public DateTimeOffset? FailedAt { get; private set; }

    /// <summary>The vendor's failure code, kept verbatim. Support needs the vendor's word, not ours.</summary>
    public string? FailureCode { get; private set; }

    /// <summary>The journal the capture posted. Null until money is confirmed captured.</summary>
    public Guid? JournalId { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static Result<PaymentIntent> Create(
        string idempotencyKey,
        Money amount,
        string postingScope,
        Guid? dealId,
        Guid? payerId,
        string? payerType,
        string? methodCode)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Error.Validation(
                "payment.idempotency_key_required",
                "Taking money requires an Idempotency-Key.");
        }

        if (!amount.IsPositive)
        {
            return Error.Validation("payment.amount_not_positive", "A payment must be for a positive amount.");
        }

        return new PaymentIntent(UuidV7.New(), idempotencyKey, amount)
        {
            PostingScope = postingScope,
            DealId = dealId,
            PayerId = payerId,
            PayerType = payerType,
            MethodCode = methodCode,
        };
    }

    /// <summary>Records what the gateway said when the request was made.</summary>
    public void RecordProviderResponse(PaymentCollectResponse response)
    {
        ProviderCode = response.ProviderCode;
        ProviderReference = response.ProviderReference;
        Status = response.Status;
    }

    /// <summary>
    /// Confirms money was captured.
    /// </summary>
    /// <remarks>
    /// Idempotent, and it has to be: a gateway sends the same webhook several times by design, and
    /// the second delivery must not post a second journal. The caller checks
    /// <see cref="JournalId"/> before posting, and this method refuses to move a failed intent to
    /// captured — a webhook arriving out of order must not resurrect a payment the vendor already
    /// told us had failed.
    /// </remarks>
    public Result Capture(Guid journalId, DateTimeOffset now)
    {
        if (Status == PaymentCollectStatus.Failed)
        {
            return Error.Conflict(
                "payment.already_failed",
                "This payment was reported failed and cannot be captured.")
                .With("providerReference", ProviderReference);
        }

        if (Status == PaymentCollectStatus.Succeeded)
        {
            return Result.Success();
        }

        Status = PaymentCollectStatus.Succeeded;
        CapturedAt = now;
        JournalId = journalId;

        Raise(new PaymentCaptured(
            Id,
            now,
            DealId,
            ProviderCode ?? string.Empty,
            ProviderReference ?? string.Empty,
            Amount.MinorUnits,
            Amount.Currency.Code));

        return Result.Success();
    }

    public Result Fail(string? failureCode, DateTimeOffset now)
    {
        if (Status == PaymentCollectStatus.Succeeded)
        {
            // Captured then reported failed is a chargeback or a vendor bug, and either way it is a
            // reversal to be posted deliberately — not a status to be overwritten, which would leave
            // a journal behind with nothing pointing at it.
            return Error.Conflict(
                "payment.already_captured",
                "This payment was captured. Post a refund or a chargeback reversal instead.");
        }

        Status = PaymentCollectStatus.Failed;
        FailedAt = now;
        FailureCode = failureCode;

        Raise(new PaymentFailed(Id, now, DealId, ProviderCode ?? string.Empty, failureCode));

        return Result.Success();
    }
}
