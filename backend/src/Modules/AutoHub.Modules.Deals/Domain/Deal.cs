using System.Globalization;
using System.Text.Json.Nodes;
using AutoHub.Platform.Pricing;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Deals.Domain;

/// <summary>
/// The settlement spine. One row shape serves every commercial intent the platform has.
/// </summary>
/// <remarks>
/// <b>The claim.</b> A C2C sale, an auction win, a rental booking, a lease instalment and a taxi
/// attachment contract are not five transactions. They are one transaction — money moves between
/// named parties, against a vehicle, priced by a rule set, settled once — observed at five different
/// points of a vehicle's life. Modelled as five tables they need five ledgers, five escrow
/// implementations, five commission engines and five reconciliation jobs, and the fifth one is
/// always subtly wrong because nobody re-derived the rounding.
/// <para>
/// <b>What actually differs, and why none of it needs a table.</b>
/// <code>
/// intent        period      recurrence   source                lines the pricing registry emits
/// ------------  ----------  -----------  --------------------  --------------------------------
/// sale          null        null         listings.listing      principal, platform_fee, gst
/// auction       null        null         auction.lot           principal, auction_commission, gst
/// rental        start..end  null         rental.booking        principal, security_deposit, gst
/// lease         start..end  monthly      lease.agreement       principal, processing_fee, gst
/// attachment    start..end  monthly      attachment.contract   principal, attachment_fee, gst
/// </code>
/// Three nullable columns — <see cref="PeriodStart"/>, <see cref="PeriodEnd"/>,
/// <see cref="RecurrenceCode"/> — plus a <see cref="SourceType"/>/<see cref="SourceId"/> pair and a
/// <see cref="PricingScope"/> absorb the entire difference. Everything else that looks
/// intent-specific is a <see cref="DealLine"/>, and every line is emitted by the pricing registry
/// rather than authored here: a rental's refundable deposit is a line, a lease's processing fee is a
/// line, an auction's commission and its GST are lines. Adding "spare parts" in v2 adds a scope and
/// a rule set, not a schema.
/// </para>
/// <para>
/// <b>There is no status column.</b> Where a deal sits in its lifecycle lives in the workflow
/// registry's instance table, keyed by <see cref="DealWorkflow.Key"/> and this aggregate's id. The
/// timestamps here — <see cref="AcceptedAt"/>, <see cref="SettledAt"/>, <see cref="CancelledAt"/> —
/// are facts about what happened, recorded after the engine allowed the move. They are not the
/// state, and nothing branches on them to decide whether a move is permitted.
/// </para>
/// <para>
/// <b>The deal never computes a fee.</b> <see cref="ApplyQuote"/> takes a <see cref="PriceQuote"/>
/// the pricing engine produced and records it verbatim, including which rule won and at which rule
/// set version. That is what makes "why was I charged this?" answerable eighteen months later, after
/// the rule has been superseded twice.
/// </para>
/// </remarks>
public sealed class Deal : AggregateRoot, IAuditable, ISoftDeletable
{
    private readonly List<DealParty> _parties = [];
    private readonly List<DealLine> _lines = [];

    private Deal(Guid id, string reference, DealSpecification specification)
        : base(id)
    {
        Reference = reference;
        IntentCode = specification.IntentCode;
        PricingScope = specification.PricingScope;
        SourceType = specification.SourceType;
        SourceId = specification.SourceId;
        ListingId = specification.ListingId;
        VehicleId = specification.VehicleId;
        ParentDealId = specification.ParentDealId;
        BaseAmount = specification.BaseAmount;
        FeeTotal = Money.Zero(specification.BaseAmount.Currency);
        Payable = specification.BaseAmount;
        PeriodStart = specification.PeriodStart;
        PeriodEnd = specification.PeriodEnd;
        RecurrenceCode = specification.RecurrenceCode;
        InstalmentNumber = specification.InstalmentNumber;
        CityCode = specification.CityCode;
        StateCode = specification.StateCode;
    }

    private Deal() { }

    /// <summary>
    /// Human-quotable reference printed on invoices and read out over the phone.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="Entity.Id"/> because a support agent cannot read a UUID to a dealer
    /// and a dealer cannot read one back. Derived from the id's own timestamp so it stays sortable
    /// and needs no sequence, which would have been a write hotspot and a cross-region problem.
    /// </remarks>
    public string Reference { get; private set; } = string.Empty;

    /// <summary>Taxonomy code for the commercial intent: <c>sale</c>, <c>auction</c>, <c>rental</c>…</summary>
    public string IntentCode { get; private set; } = string.Empty;

    /// <summary>
    /// The pricing registry rule set that prices this deal, e.g. <c>auction.win</c>.
    /// </summary>
    /// <remarks>
    /// Supplied by whichever module opened the deal rather than derived from
    /// <see cref="IntentCode"/> here. A mapping from intent to scope living in this class would be
    /// exactly the hardcoded lookup the prime directive bans, and it would stop operations from
    /// running two pricing experiments over the same intent.
    /// </remarks>
    public string PricingScope { get; private set; } = string.Empty;

    /// <summary>The module and aggregate this deal was opened from, e.g. <c>auction.lot</c>.</summary>
    /// <remarks>
    /// A type string and a bare id rather than a foreign key, for the reason
    /// <c>EntityAttributeValue.EntityType</c> is: the deals module must not have a table-level
    /// dependency on five other modules, and a cross-schema foreign key is the one thing that would
    /// stop any of them being extracted to its own service.
    /// </remarks>
    public string? SourceType { get; private set; }

    public Guid? SourceId { get; private set; }

    public Guid? ListingId { get; private set; }

    public Guid? VehicleId { get; private set; }

    /// <summary>
    /// The master agreement a recurring instalment belongs to.
    /// </summary>
    /// <remarks>
    /// A 36-month lease is one master deal plus 36 child deals, not one deal with 36 amounts. Each
    /// instalment is separately priced, separately settled, separately cancellable and separately
    /// reconcilable — which is what a late payment on month 14 actually requires.
    /// </remarks>
    public Guid? ParentDealId { get; private set; }

    /// <summary>The principal: sale price, winning bid, one period's rent, one lease instalment.</summary>
    public Money BaseAmount { get; private set; }

    public Money FeeTotal { get; private set; }

    /// <summary>Base plus fees. What the payer parts with, and what the ledger must balance to.</summary>
    public Money Payable { get; private set; }

    /// <summary>Null for a sale or an auction win. Set for anything that occupies time.</summary>
    public DateTimeOffset? PeriodStart { get; private set; }

    public DateTimeOffset? PeriodEnd { get; private set; }

    /// <summary>Taxonomy code — <c>monthly</c>, <c>weekly</c>. Null means this deal happens once.</summary>
    public string? RecurrenceCode { get; private set; }

    /// <summary>1-based position in a recurring series. Null on a one-off or on a master agreement.</summary>
    public int? InstalmentNumber { get; private set; }

    /// <summary>Geography codes, carried because pricing and GST place-of-supply both turn on them.</summary>
    public string? CityCode { get; private set; }

    public string? StateCode { get; private set; }

    public IReadOnlyList<DealParty> Parties => _parties;

    public IReadOnlyList<DealLine> Lines => _lines;

    /// <summary>Which pricing rule produced <see cref="Lines"/>, so support can explain the number.</summary>
    public string? AppliedRuleCode { get; private set; }

    public int? PricingRuleSetVersion { get; private set; }

    public string? PricingExplanation { get; private set; }

    public DateTimeOffset? QuotedAt { get; private set; }

    public DateTimeOffset? AcceptedAt { get; private set; }

    public DateTimeOffset? SettledAt { get; private set; }

    /// <summary>
    /// The ledger journal this deal settled through.
    /// </summary>
    /// <remarks>
    /// Deterministic rather than reported back: it is the idempotency key the payments module posts
    /// its journal under, derived from this deal's id. Both sides compute the same string from the
    /// same fact, so neither has to wait for the other and a retry on either side lands on the same
    /// journal instead of a second one.
    /// </remarks>
    public string? SettlementReference { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public string? CancellationReason { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public static Result<Deal> Open(DealSpecification specification, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(specification.IntentCode))
        {
            return Error.Validation("deal.intent_required", "A deal must state which intent it settles.");
        }

        if (string.IsNullOrWhiteSpace(specification.PricingScope))
        {
            return Error.Validation(
                "deal.pricing_scope_required",
                "A deal must name the pricing rule set that prices it.");
        }

        if (specification.BaseAmount.IsNegative)
        {
            return Error.Validation("deal.amount_negative", "A deal's principal cannot be negative.");
        }

        // A period that ends before it starts is a data entry mistake that would otherwise surface
        // as a negative rental duration and a negative late fee three weeks later.
        if (specification.PeriodEnd is { } end && specification.PeriodStart is { } start && end <= start)
        {
            return Error.Validation(
                "deal.period_inverted",
                "A deal's period must end after it starts.");
        }

        if (specification.RecurrenceCode is not null && specification.PeriodStart is null)
        {
            return Error.Validation(
                "deal.recurrence_needs_period",
                "A recurring deal must state the period its instalment covers.");
        }

        var id = UuidV7.New(now);
        var deal = new Deal(id, BuildReference(id, now), specification);

        deal.Raise(new DealOpened(
            id,
            now,
            deal.IntentCode,
            deal.PricingScope,
            deal.SourceType,
            deal.SourceId,
            deal.BaseAmount.MinorUnits,
            deal.BaseAmount.Currency.Code));

        return deal;
    }

    /// <summary>
    /// Adds a counterparty. Idempotent on <c>(roleCode, partyId)</c>.
    /// </summary>
    /// <remarks>
    /// Idempotent because the same party is attached again on every retry of a deal-opening flow
    /// that failed after the deal row committed, and a deal with the buyer listed twice pays them
    /// twice at settlement.
    /// </remarks>
    public Result<DealParty> AttachParty(DealPartySpecification specification, DateTimeOffset now)
    {
        if (SettledAt is not null || CancelledAt is not null)
        {
            return Error.Conflict(
                "deal.parties_frozen",
                "Parties cannot be changed after a deal has settled or been cancelled.");
        }

        if (string.IsNullOrWhiteSpace(specification.RoleCode))
        {
            return Error.Validation("deal.party.role_required", "A party must have a role.");
        }

        if (specification.SettlementShareRatio < 0)
        {
            return Error.Validation(
                "deal.party.share_negative",
                "A settlement share cannot be negative.");
        }

        var existing = _parties.Find(p =>
            string.Equals(p.RoleCode, specification.RoleCode, StringComparison.Ordinal)
            && p.PartyId == specification.PartyId);

        if (existing is not null)
        {
            return existing;
        }

        var party = DealParty.Create(Id, specification);
        _parties.Add(party);

        Raise(new DealPartyAttached(Id, now, party.Id, specification.RoleCode, specification.PartyId));

        return party;
    }

    /// <summary>
    /// Records a priced quote produced by the pricing engine, replacing any earlier one.
    /// </summary>
    /// <remarks>
    /// Replacement rather than accumulation: re-quoting a deal whose city changed must not leave the
    /// old city's commission line sitting underneath the new one. The principal line is rebuilt from
    /// <see cref="BaseAmount"/> here rather than expected from the quote, because the pricing
    /// registry prices <i>fees</i> — the principal is the deal's own fact and no rule set should be
    /// able to restate it.
    /// </remarks>
    public Result ApplyQuote(PriceQuote quote, DateTimeOffset now)
    {
        if (AcceptedAt is not null)
        {
            return Error.Conflict(
                "deal.quote_after_acceptance",
                "A deal cannot be re-quoted after both sides have accepted it.");
        }

        if (quote.BaseAmount != BaseAmount)
        {
            return Error.Validation(
                "deal.quote_amount_mismatch",
                "The quote was produced for a different principal than this deal carries.")
                .With("dealAmountMinor", BaseAmount.MinorUnits)
                .With("quoteAmountMinor", quote.BaseAmount.MinorUnits);
        }

        _lines.Clear();
        _lines.Add(DealLine.Principal(Id, BaseAmount));

        var sortOrder = 1;
        foreach (var line in quote.Lines)
        {
            if (line.Amount.Currency != BaseAmount.Currency)
            {
                return Error.Validation(
                    "deal.quote_currency_mismatch",
                    "A quote line is denominated in a different currency to the deal.");
            }

            _lines.Add(DealLine.FromQuote(Id, line, quote.AppliedRuleCode, sortOrder++));
        }

        FeeTotal = quote.TotalFees;
        Payable = quote.Payable;
        AppliedRuleCode = quote.AppliedRuleCode;
        PricingRuleSetVersion = quote.RuleSetVersion;
        PricingExplanation = quote.Explanation;
        QuotedAt = now;

        Raise(new DealQuoted(
            Id,
            now,
            PricingScope,
            quote.AppliedRuleCode,
            quote.RuleSetVersion,
            FeeTotal.MinorUnits,
            Payable.MinorUnits,
            Payable.Currency.Code));

        return Result.Success();
    }

    /// <summary>Records one party's acceptance. Returns whether every required party has now accepted.</summary>
    public Result<bool> RecordPartyAcceptance(Guid partyId, DateTimeOffset now)
    {
        var party = _parties.Find(p => p.PartyId == partyId);
        if (party is null)
        {
            return Error.Forbidden(
                "deal.party.not_on_deal",
                "You are not a party to this deal.");
        }

        party.Accept(now);

        return _parties
            .Where(p => p.RequiresAcceptance)
            .All(p => p.AcceptedAt is not null);
    }

    /// <summary>
    /// Marks the deal accepted. Called only after the workflow engine has allowed the transition.
    /// </summary>
    /// <remarks>
    /// The quote is required first, and the check is here rather than in the handler because it is an
    /// invariant of the aggregate rather than a rule of one code path: a deal accepted before it was
    /// priced settles for its principal with no commission, and nobody notices until the month closes
    /// short.
    /// </remarks>
    public Result MarkAccepted(DateTimeOffset now)
    {
        if (QuotedAt is null)
        {
            return Error.Rule(
                "deal.not_quoted",
                "A deal must be priced before it can be accepted.");
        }

        if (AcceptedAt is not null)
        {
            return Result.Success();
        }

        AcceptedAt = now;
        Raise(new DealAccepted(Id, now, Payable.MinorUnits, Payable.Currency.Code));

        return Result.Success();
    }

    public Result MarkSettled(DateTimeOffset now)
    {
        if (AcceptedAt is null)
        {
            return Error.Rule(
                "deal.not_accepted",
                "A deal must be accepted before it can settle.");
        }

        if (SettledAt is not null)
        {
            return Result.Success();
        }

        SettledAt = now;
        SettlementReference = SettlementKey(Id);

        Raise(new DealSettled(
            Id,
            now,
            SettlementReference,
            PricingScope,
            Payable.MinorUnits,
            Payable.Currency.Code));

        return Result.Success();
    }

    public Result MarkCancelled(string reason, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Error.Validation("deal.cancel.reason_required", "Cancelling a deal needs a reason.");
        }

        if (SettledAt is not null)
        {
            return Error.Conflict(
                "deal.already_settled",
                "A settled deal is reversed with a refund, never cancelled.");
        }

        if (CancelledAt is not null)
        {
            return Result.Success();
        }

        CancelledAt = now;
        CancellationReason = reason;

        Raise(new DealCancelled(Id, now, reason, Payable.MinorUnits, Payable.Currency.Code));

        return Result.Success();
    }

    /// <summary>
    /// The facts a pricing rule may match on, assembled in the shape §4 of the API contract publishes.
    /// </summary>
    /// <remarks>
    /// Built here so every caller that prices a deal presents the same context. Three handlers each
    /// assembling their own is three chances for a rule that matches in the quote preview and misses
    /// at settlement, which a dealer experiences as being charged a different number to the one they
    /// were shown.
    /// </remarks>
    public JsonObject ToPricingContext(JsonObject? additionalFacts = null)
    {
        var context = additionalFacts?.DeepClone() as JsonObject ?? [];

        context["deal"] = new JsonObject
        {
            ["intent"] = IntentCode,
            ["recurrence"] = RecurrenceCode,
            ["instalment"] = InstalmentNumber,
            ["sourceType"] = SourceType,
        };

        context["location"] = new JsonObject
        {
            ["city"] = CityCode,
            ["state"] = StateCode,
        };

        return context;
    }

    /// <summary>
    /// The idempotency key the settlement journal is posted under.
    /// </summary>
    /// <remarks>
    /// Deterministic and public so the payments module can compute it from a deal id alone. That is
    /// what lets a settlement be retried from either side — a workflow effect replayed by the
    /// scheduler, an operator re-running a stuck batch — and land on the journal that already exists
    /// instead of posting a second one.
    /// </remarks>
    public static string SettlementKey(Guid dealId)
        => string.Create(CultureInfo.InvariantCulture, $"deal:{dealId:D}:settle");

    private static string BuildReference(Guid id, DateTimeOffset now)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"DL{now.UtcDateTime:yyyyMMdd}{id.ToString("N", CultureInfo.InvariantCulture)[^6..].ToUpperInvariant()}");
}

/// <summary>Everything needed to open a deal, stated by the module that is opening it.</summary>
public sealed record DealSpecification
{
    public required string IntentCode { get; init; }

    public required string PricingScope { get; init; }

    public required Money BaseAmount { get; init; }

    public string? SourceType { get; init; }

    public Guid? SourceId { get; init; }

    public Guid? ListingId { get; init; }

    public Guid? VehicleId { get; init; }

    public Guid? ParentDealId { get; init; }

    public DateTimeOffset? PeriodStart { get; init; }

    public DateTimeOffset? PeriodEnd { get; init; }

    public string? RecurrenceCode { get; init; }

    public int? InstalmentNumber { get; init; }

    public string? CityCode { get; init; }

    public string? StateCode { get; init; }
}
