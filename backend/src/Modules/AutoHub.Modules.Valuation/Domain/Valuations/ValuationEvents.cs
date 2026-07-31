using AutoHub.SharedKernel.Domain;

namespace AutoHub.Modules.Valuation.Domain.Valuations;

/// <summary>
/// A price was published for a vehicle.
/// </summary>
/// <remarks>
/// Consumed by listings, which uses it as the guard input for "this asking price is outside the
/// market band"; by notifications, which follows up on a quote that never became a listing; and by
/// analytics, which tracks quote-to-listing conversion — the single most important funnel number
/// this module produces.
/// <para>
/// Shadow runs raise nothing. A model that is being evaluated must not send a seller a notification
/// or move a conversion metric, or the evaluation changes the thing it is measuring.
/// </para>
/// </remarks>
public sealed record ValuationProduced(
    Guid ValuationId,
    Guid? VehicleId,
    long PointMinorUnits,
    string Currency,
    string ModelVersion,
    DateTimeOffset ExpiresAt,
    DateTimeOffset OccurredOn)
    : DomainEvent(ValuationId, OccurredOn)
{
    public override string EventType => "valuation.quote_produced.v1";
}
