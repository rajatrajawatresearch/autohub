using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Deals.Contracts;

/// <summary>
/// The only surface another module may use to open a deal.
/// </summary>
/// <remarks>
/// The module isolation rule says a module may reference <c>SharedKernel</c>, <c>Platform</c>, and
/// another module's <c>Contracts</c> folder — nothing else. This interface is what the auction,
/// rental, lease and attachment modules bind to, and it is deliberately narrow: it exposes opening
/// and reading, and no way to reach a <c>Deal</c> aggregate, its repository, or its workflow key.
/// <para>
/// That narrowness is what makes extraction cheap later. When deals becomes its own service, this
/// interface gains an HTTP implementation and every consumer is unchanged; if the auction module had
/// been calling <c>Deal.Open</c> directly, the extraction would have been a rewrite of five modules
/// instead of one adapter.
/// </para>
/// <para>
/// Opening is idempotent on <see cref="OpenDealRequest.SourceType"/> plus
/// <see cref="OpenDealRequest.SourceId"/>. Callers are expected to rely on that rather than to track
/// whether they have already opened a deal, because the caller's own retry is exactly the case where
/// its memory of having called is what was lost.
/// </para>
/// </remarks>
public interface IDealGateway
{
    Task<Result<DealHandle>> OpenAsync(OpenDealRequest request, CancellationToken cancellationToken);

    Task<Result<DealHandle>> FindBySourceAsync(
        string sourceType,
        Guid sourceId,
        CancellationToken cancellationToken);
}

/// <summary>
/// What a calling module gets back: enough to link to the deal and to show what is owed.
/// </summary>
/// <remarks>
/// Not the aggregate and not the full view. An auction lot needs the deal's id to store, its
/// reference to display, and the payable amount to start a payment window against — anything more
/// would be the auction module reading the deals module's internals through a return type.
/// </remarks>
public sealed record DealHandle(
    Guid DealId,
    string Reference,
    long PayableMinorUnits,
    string Currency,
    bool IsQuoted);

public sealed record OpenDealRequest
{
    /// <summary>Taxonomy code for the intent, e.g. <c>auction</c>.</summary>
    public required string IntentCode { get; init; }

    /// <summary>Pricing registry scope that prices this deal, e.g. <c>auction.win</c>.</summary>
    public required string PricingScope { get; init; }

    public required long BaseAmountMinorUnits { get; init; }

    public string CurrencyCode { get; init; } = "INR";

    /// <summary>The calling module's aggregate type, e.g. <c>auction.lot</c>. Half of the idempotency key.</summary>
    public required string SourceType { get; init; }

    public required Guid SourceId { get; init; }

    public Guid? ListingId { get; init; }

    public Guid? VehicleId { get; init; }

    public DateTimeOffset? PeriodStart { get; init; }

    public DateTimeOffset? PeriodEnd { get; init; }

    public string? RecurrenceCode { get; init; }

    public int? InstalmentNumber { get; init; }

    public string? CityCode { get; init; }

    public string? StateCode { get; init; }

    public IReadOnlyList<DealPartyRequest> Parties { get; init; } = [];

    /// <summary>
    /// Whether to price the deal immediately.
    /// </summary>
    /// <remarks>
    /// An auction win prices at once — the commission is known the moment the hammer falls. A rental
    /// booking does not, because the final bill depends on the odometer at return. Making it a flag
    /// rather than a rule in the deals module keeps the decision with the module that knows.
    /// </remarks>
    public bool QuoteImmediately { get; init; } = true;
}

public sealed record DealPartyRequest
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
