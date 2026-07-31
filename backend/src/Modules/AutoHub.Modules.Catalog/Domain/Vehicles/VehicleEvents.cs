using AutoHub.Modules.Catalog.Contracts;
using AutoHub.SharedKernel.Domain;

namespace AutoHub.Modules.Catalog.Domain.Vehicles;

/// <summary>A canonical vehicle now exists that did not before.</summary>
/// <remarks>
/// Consumed by search indexing and by analytics' supply funnel. Deliberately carries the category
/// rather than the whole vehicle: a consumer that needs more asks the contract, and an event that
/// inlines an aggregate becomes a second, staler copy of it the moment the aggregate changes.
/// </remarks>
public sealed record VehicleRegistered(Guid VehicleId, Guid CategoryId, DateTimeOffset OccurredOn)
    : DomainEvent(VehicleId, OccurredOn)
{
    public override string EventType => "catalog.vehicle_registered.v1";
}

/// <summary>An identifier started applying to a vehicle.</summary>
public sealed record VehicleIdentityRecorded(
    Guid VehicleId,
    VehicleIdentifierType IdentifierType,
    string Value,
    DateTimeOffset OccurredOn)
    : DomainEvent(VehicleId, OccurredOn)
{
    public override string EventType => "catalog.vehicle_identity_recorded.v1";
}

/// <summary>
/// An identifier stopped applying — a re-registration, a surrender, or a number reissued elsewhere.
/// </summary>
/// <remarks>
/// Trust cares about this one. A plate that changes hands twice in six months is a laundering
/// pattern, and the only place that fact is visible is the stream of these events.
/// </remarks>
public sealed record VehicleIdentityRetired(
    Guid VehicleId,
    VehicleIdentifierType IdentifierType,
    string Value,
    DateTimeOffset OccurredOn)
    : DomainEvent(VehicleId, OccurredOn)
{
    public override string EventType => "catalog.vehicle_identity_retired.v1";
}

/// <summary>The vehicle's attribute values changed, so anything projecting them is now stale.</summary>
public sealed record VehicleAttributesUpdated(Guid VehicleId, DateTimeOffset OccurredOn)
    : DomainEvent(VehicleId, OccurredOn)
{
    public override string EventType => "catalog.vehicle_attributes_updated.v1";
}
