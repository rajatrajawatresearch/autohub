using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Valuation.Contracts;

/// <summary>The entity-type key this module owns in the platform's shared tables.</summary>
public static class ValuationEntityTypes
{
    public const string Valuation = "valuation.quote";
}

/// <summary>One of the seven prices, on the wire.</summary>
/// <remarks>
/// <c>code</c> and <c>label</c> both travel because the client branches on the code and renders the
/// label, and the label is localisable while the code is a contract. Sending only the label would
/// make a translation a breaking change.
/// </remarks>
public sealed record ValuationChannelView(string Code, string Label, long AmountMinor, bool IsPrimary);

/// <summary>
/// A valuation as other modules see it.
/// </summary>
/// <remarks>
/// Listings uses it as a guard input — "this asking price is 40% above the market band" — and
/// Finance uses it as an indicative loan-to-value base. Neither may reach into this module's domain,
/// because a valuation is immutable and a module that could load the aggregate could be tempted to
/// write one.
/// </remarks>
public sealed record ValuationSummary
{
    public required Guid QuoteId { get; init; }

    public Guid? VehicleId { get; init; }

    public required string Currency { get; init; }

    /// <summary>The anchor. The other channels are spreads from it.</summary>
    public required long PointMinorUnits { get; init; }

    public required long LowMinorUnits { get; init; }

    public required long HighMinorUnits { get; init; }

    public required decimal Confidence { get; init; }

    public required string ConfidenceBand { get; init; }

    public required string ModelVersion { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    public required IReadOnlyList<ValuationChannelView> Channels { get; init; }
}

/// <summary>
/// The read side of valuations.
/// </summary>
/// <remarks>
/// Read-only by design. Producing a valuation goes through the module's own command, because a
/// valuation that another module could write is a price with no model behind it and no way to
/// reproduce it.
/// </remarks>
public interface IValuationDirectory
{
    /// <summary>
    /// The most recent unexpired valuation for a vehicle, or a not-found error.
    /// </summary>
    /// <remarks>
    /// Unexpired specifically. A six-month-old quote is worse than no quote for a price-band guard —
    /// it would hold back a listing priced correctly for today's market against last winter's number.
    /// </remarks>
    Task<Result<ValuationSummary>> GetLatestForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken);
}
