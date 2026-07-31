using System.Globalization;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Payments.Domain;

/// <summary>
/// A set of payouts instructed to the bank as one file.
/// </summary>
/// <remarks>
/// Batched rather than per payout, because that is how Indian bank payout APIs and NEFT files
/// actually work, and because a batch gives one thing to approve, one thing to retry and one thing
/// to reconcile against the bank's response file. Eight thousand individual instructions give eight
/// thousand of each.
/// <para>
/// The batch's lifecycle — drafted, approved, instructed, settled, partially failed — lives in the
/// workflow registry under <see cref="PayoutWorkflow.Key"/>, because "payouts above ₹5 lakh need a
/// second approver" is precisely the kind of rule that arrives after launch.
/// </para>
/// </remarks>
public sealed class PayoutBatch : AggregateRoot, IAuditable
{
    private readonly List<PayoutInstruction> _instructions = [];

    private PayoutBatch(Guid id, string reference, Currency currency, DateOnly businessDate)
        : base(id)
    {
        Reference = reference;
        Currency = currency;
        BusinessDate = businessDate;
        Total = Money.Zero(currency);
    }

    private PayoutBatch() { }

    public string Reference { get; private set; } = string.Empty;

    public Currency Currency { get; private set; }

    /// <summary>The banking day this batch belongs to, in IST. Cut-off times are a real constraint.</summary>
    public DateOnly BusinessDate { get; private set; }

    public Money Total { get; private set; }

    public IReadOnlyList<PayoutInstruction> Instructions => _instructions;

    public DateTimeOffset? InstructedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static PayoutBatch Draft(Currency currency, DateOnly businessDate, DateTimeOffset now)
    {
        var id = UuidV7.New(now);
        return new PayoutBatch(id, BuildReference(id, businessDate), currency, businessDate);
    }

    public Result<PayoutInstruction> Add(
        string payeeType,
        Guid payeeId,
        Money amount,
        string payoutAccountRef)
    {
        if (InstructedAt is not null)
        {
            return Error.Conflict(
                "payout.batch_already_instructed",
                "An instructed batch cannot be added to. Open the next batch.");
        }

        if (amount.Currency != Currency)
        {
            return Error.Validation(
                "payout.currency_mismatch",
                "A payout cannot be added to a batch in another currency.");
        }

        if (!amount.IsPositive)
        {
            return Error.Validation("payout.amount_not_positive", "A payout must be for a positive amount.");
        }

        // One instruction per payee per batch. Two rows for one dealer is two bank transfers, and
        // the second one is the kind of duplicate that is noticed by the recipient and nobody else.
        var existing = _instructions.Find(i => i.PayeeId == payeeId);
        if (existing is not null)
        {
            return Error.Conflict(
                "payout.payee_already_in_batch",
                "This payee already has an instruction in this batch.")
                .With("payeeId", payeeId);
        }

        var instruction = PayoutInstruction.Create(Id, payeeType, payeeId, amount, payoutAccountRef);
        _instructions.Add(instruction);
        Total += amount;

        return instruction;
    }

    /// <summary>
    /// Closes the batch once every instruction has been posted and sent.
    /// </summary>
    /// <remarks>
    /// There is no batch-level journal id, deliberately: each instruction posts its own journal, so
    /// one refused rail leaves the rest correctly accounted for. A single journal for the batch would
    /// mean one bad beneficiary rolls back nine hundred good payouts.
    /// </remarks>
    public Result MarkInstructed(DateTimeOffset now)
    {
        if (_instructions.Count == 0)
        {
            return Error.Rule("payout.batch_empty", "An empty batch has nothing to instruct.");
        }

        if (InstructedAt is not null)
        {
            return Result.Success();
        }

        InstructedAt = now;

        Raise(new PayoutBatchInstructed(
            Id, now, Reference, _instructions.Count, Total.MinorUnits, Currency.Code));

        return Result.Success();
    }

    public PayoutInstruction? FindInstruction(Guid instructionId)
        => _instructions.Find(i => i.Id == instructionId);

    private static string BuildReference(Guid id, DateOnly businessDate)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"PB{businessDate:yyyyMMdd}{id.ToString("N", CultureInfo.InvariantCulture)[^4..].ToUpperInvariant()}");
}

/// <summary>One payee's share of a batch.</summary>
public sealed class PayoutInstruction : Entity, IAuditable
{
    private PayoutInstruction(Guid id, Guid batchId, string payeeType, Guid payeeId, Money amount, string accountRef)
        : base(id)
    {
        PayoutBatchId = batchId;
        PayeeType = payeeType;
        PayeeId = payeeId;
        Amount = amount;
        PayoutAccountRef = accountRef;
        Outcome = PayoutOutcome.Pending;
    }

    private PayoutInstruction() { }

    public Guid PayoutBatchId { get; private set; }

    public string PayeeType { get; private set; } = string.Empty;

    public Guid PayeeId { get; private set; }

    public Money Amount { get; private set; }

    /// <summary>Opaque handle to the beneficiary record at the provider. Never an account number.</summary>
    public string PayoutAccountRef { get; private set; } = string.Empty;

    public PayoutOutcome Outcome { get; private set; }

    public string? ProviderReference { get; private set; }

    public string? FailureCode { get; private set; }

    public DateTimeOffset? SettledAt { get; private set; }

    /// <summary>The journal that recorded the money actually leaving. Null until the bank confirms.</summary>
    public Guid? SettlementJournalId { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    internal static PayoutInstruction Create(
        Guid batchId, string payeeType, Guid payeeId, Money amount, string accountRef)
        => new(UuidV7.New(), batchId, payeeType, payeeId, amount, accountRef);

    public void RecordSent(string providerReference)
    {
        ProviderReference = providerReference;
        Outcome = PayoutOutcome.Sent;
    }

    public void RecordSettled(Guid journalId, DateTimeOffset now)
    {
        Outcome = PayoutOutcome.Succeeded;
        SettlementJournalId = journalId;
        SettledAt = now;
    }

    public void RecordFailed(string? failureCode)
    {
        Outcome = PayoutOutcome.Failed;
        FailureCode = failureCode;
    }
}

/// <summary>
/// The normalised outcome of one payout at the bank.
/// </summary>
/// <remarks>
/// The sibling of <c>PaymentCollectStatus</c>, and for the same reason: it is the translation target
/// for incompatible vendor vocabularies, not a lifecycle. What the platform <i>does</i> about a
/// failed payout — retry it, freeze the wallet, escalate to operations — is workflow registry data.
/// <para>
/// <see cref="Sent"/> and <see cref="Succeeded"/> are separate because they are genuinely different
/// facts. "Accepted by the bank" is not "the beneficiary has it": UPI and IMPS reverse hours later,
/// and NEFT settles in batches. Collapsing the two would let a dealer's statement claim they were
/// paid before the money moved.
/// </para>
/// </remarks>
public enum PayoutOutcome
{
    Pending = 1,

    /// <summary>Accepted by the bank. Not yet confirmed as debited.</summary>
    Sent = 2,

    Succeeded = 3,

    Failed = 4,
}

/// <summary>The workflow registry key governing a payout batch's approvals.</summary>
public static class PayoutWorkflow
{
    public const string Key = "payout_batch";

    public const string EntityType = "payments.payout_batch";

    public const string Approve = "approve";

    public const string Instruct = "instruct";

    public const string Settle = "settle";

    public const string Cancel = "cancel";
}
