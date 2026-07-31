using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;

namespace AutoHub.Modules.Deals.Domain;

/// <summary>
/// Somebody with a stake in a deal: the buyer, the seller, a dealer, a broker, the platform itself.
/// </summary>
/// <remarks>
/// <b>Why a role code and not a set of columns.</b> The tempting shape is <c>buyer_id</c> and
/// <c>seller_id</c> on the deal, and it survives exactly until the second intent. A rental has an
/// owner and a renter; an auction has a consignor, a winning dealer and the auction house; an
/// attachment contract has a fleet operator, a driver and an aggregator; a financed sale adds a
/// lender who is paid directly and never meets either other party. Every one of those is a column
/// nobody would add, so the alternative is a deal that pretends a fleet operator is a "seller".
/// <para>
/// <see cref="RoleCode"/> is a taxonomy registry value, so opening a new kind of counterparty is
/// data. What is <b>not</b> data is <see cref="IsPayer"/>, <see cref="IsPayee"/> and
/// <see cref="SettlementShareRatio"/> — those are structural, because the ledger has to know which
/// direction money moves without consulting a label it cannot interpret.
/// </para>
/// <para>
/// <see cref="PartyId"/> is nullable because the platform is a party to nearly every deal and is not
/// a user. Modelling it as a role with a null id, rather than as an implicit fourth thing the
/// settlement code knows about, is what makes the commission leg of a journal look like every other
/// leg.
/// </para>
/// </remarks>
public sealed class DealParty : Entity, IAuditable
{
    private DealParty(Guid id, Guid dealId, DealPartySpecification specification)
        : base(id)
    {
        DealId = dealId;
        RoleCode = specification.RoleCode;
        PartyId = specification.PartyId;
        PartyType = specification.PartyType;
        IsPayer = specification.IsPayer;
        IsPayee = specification.IsPayee;
        RequiresAcceptance = specification.RequiresAcceptance;
        SettlementShareRatio = specification.SettlementShareRatio;
        PayoutAccountRef = specification.PayoutAccountRef;
    }

    private DealParty() { }

    public Guid DealId { get; private set; }

    /// <summary>Taxonomy code: <c>buyer</c>, <c>seller</c>, <c>dealer</c>, <c>broker</c>, <c>platform</c>.</summary>
    public string RoleCode { get; private set; } = string.Empty;

    /// <summary>The user or dealer. Null when the party is the platform itself.</summary>
    public Guid? PartyId { get; private set; }

    /// <summary>Which module owns <see cref="PartyId"/>, e.g. <c>identity.user</c>.</summary>
    public string? PartyType { get; private set; }

    public bool IsPayer { get; private set; }

    public bool IsPayee { get; private set; }

    /// <summary>
    /// Whether the deal waits for this party before it can move forward.
    /// </summary>
    /// <remarks>
    /// A C2C sale needs both sides; an auction win needs neither, because the bid was the acceptance
    /// and the hammer fell. Marking it per party rather than per intent means a single flag change
    /// turns a manual-confirmation flow into an instant one, in data.
    /// </remarks>
    public bool RequiresAcceptance { get; private set; }

    /// <summary>
    /// Relative share of the settlement, fed to <c>Money.Allocate</c>.
    /// </summary>
    /// <remarks>
    /// A ratio rather than an amount or a percentage, because <c>Money.Allocate</c> distributes the
    /// leftover paise so the parts sum back to the whole. Percentages that each round independently
    /// are how ₹1,00,000 splits into ₹99,999.99 and a reconciliation ticket.
    /// </remarks>
    public int SettlementShareRatio { get; private set; }

    /// <summary>Opaque handle to the payout instrument. Never the bank account number itself.</summary>
    public string? PayoutAccountRef { get; private set; }

    public DateTimeOffset? AcceptedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    internal static DealParty Create(Guid dealId, DealPartySpecification specification)
        => new(UuidV7.New(), dealId, specification);

    /// <summary>Idempotent: a party who taps "Accept" twice on a flaky connection accepts once.</summary>
    internal void Accept(DateTimeOffset now) => AcceptedAt ??= now;
}

public sealed record DealPartySpecification
{
    public required string RoleCode { get; init; }

    public Guid? PartyId { get; init; }

    public string? PartyType { get; init; }

    public bool IsPayer { get; init; }

    public bool IsPayee { get; init; }

    public bool RequiresAcceptance { get; init; }

    public int SettlementShareRatio { get; init; }

    public string? PayoutAccountRef { get; init; }
}
