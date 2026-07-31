using AutoHub.Modules.Catalog.Contracts;
using AutoHub.Platform.Taxonomy;
using AutoHub.SharedKernel.Results;
using MediatR;

namespace AutoHub.Modules.Catalog.Application.GetVehicle;

public sealed record GetVehicleQuery(Guid VehicleId) : IRequest<Result<VehicleDetail>>;

/// <summary>
/// A vehicle with everything needed to render it, and nothing that needs a second call.
/// </summary>
/// <remarks>
/// <see cref="Title"/> is composed here rather than by the client because §10 of the API contract
/// publishes it — <c>"2021 Maruti Suzuki Swift VXi"</c> — and three clients composing it themselves
/// is three implementations of the same string join, one of which will put the year last.
/// <para>
/// <see cref="Attributes"/> arrives labelled and typed from the taxonomy registry, so a spec sheet
/// renders "Petrol" from a stored <c>petrol</c> without the caller holding the option list.
/// </para>
/// </remarks>
public sealed record VehicleDetail
{
    public required VehicleSummary Vehicle { get; init; }

    public required string Title { get; init; }

    public required TaxonomyRef Category { get; init; }

    public TaxonomyRef? Make { get; init; }

    public TaxonomyRef? Model { get; init; }

    public TaxonomyRef? Variant { get; init; }

    public required IReadOnlyList<ResolvedAttribute> Attributes { get; init; }
}
