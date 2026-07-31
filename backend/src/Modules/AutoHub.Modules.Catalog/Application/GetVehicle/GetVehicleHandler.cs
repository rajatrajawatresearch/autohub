using System.Globalization;
using AutoHub.Modules.Catalog.Application.Abstractions;
using AutoHub.Modules.Catalog.Contracts;
using AutoHub.Modules.Catalog.Domain.Vehicles;
using AutoHub.Platform.Taxonomy;
using AutoHub.SharedKernel.Results;
using MediatR;

namespace AutoHub.Modules.Catalog.Application.GetVehicle;

public sealed class GetVehicleHandler : IRequestHandler<GetVehicleQuery, Result<VehicleDetail>>
{
    private readonly IVehicleRepository _vehicles;
    private readonly IVehicleTaxonomy _taxonomy;
    private readonly ITaxonomyRegistry _registry;

    public GetVehicleHandler(
        IVehicleRepository vehicles,
        IVehicleTaxonomy taxonomy,
        ITaxonomyRegistry registry)
    {
        _vehicles = vehicles;
        _taxonomy = taxonomy;
        _registry = registry;
    }

    public async Task<Result<VehicleDetail>> Handle(
        GetVehicleQuery query,
        CancellationToken cancellationToken)
    {
        var vehicle = await _vehicles.GetAsync(query.VehicleId, cancellationToken);

        if (vehicle is null)
        {
            return Error.NotFound("catalog.vehicle.not_found", "No vehicle with that id.")
                .With("vehicleId", query.VehicleId);
        }

        var detail = await DescribeAsync(vehicle, cancellationToken);
        return detail;
    }

    /// <summary>
    /// Joins one vehicle to the taxonomy and its stored attribute values.
    /// </summary>
    /// <remarks>
    /// Internal rather than private so the search handler renders a result row through exactly the
    /// same code. Two describers is how a list page and a detail page end up disagreeing about a
    /// car's title, which users read as two different cars.
    /// </remarks>
    internal async Task<VehicleDetail> DescribeAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        var ids = new List<Guid> { vehicle.CategoryId };
        AddIfPresent(ids, vehicle.MakeId);
        AddIfPresent(ids, vehicle.ModelId);
        AddIfPresent(ids, vehicle.VariantId);

        var described = await _taxonomy.DescribeAsync(ids, cancellationToken);
        var refs = described.IsSuccess
            ? described.Value
            : new Dictionary<Guid, TaxonomyRef>();

        var values = await _registry.ResolveAttributeValuesAsync(
            CatalogEntityTypes.Vehicle, [vehicle.Id], cancellationToken);

        var attributes = values.IsSuccess && values.Value.ByEntity.TryGetValue(vehicle.Id, out var resolved)
            ? resolved
            : [];

        var category = refs.GetValueOrDefault(vehicle.CategoryId)
            ?? new TaxonomyRef(vehicle.CategoryId, string.Empty, string.Empty);

        var make = Lookup(refs, vehicle.MakeId);
        var model = Lookup(refs, vehicle.ModelId);
        var variant = Lookup(refs, vehicle.VariantId);

        return new VehicleDetail
        {
            Vehicle = vehicle.ToSummary(),
            Title = ComposeTitle(vehicle.ManufactureYear, make, model, variant),
            Category = category,
            Make = make,
            Model = model,
            Variant = variant,
            Attributes = attributes,
        };
    }

    /// <summary>
    /// "2021 Maruti Suzuki Swift VXi", with every missing part simply absent.
    /// </summary>
    /// <remarks>
    /// A vehicle created from a plate alone has no make and no model, and the honest title for it is
    /// the plate — not "Unknown Unknown", which looks like a bug to the seller who just typed a
    /// perfectly good registration number.
    /// </remarks>
    private static string ComposeTitle(int? year, TaxonomyRef? make, TaxonomyRef? model, TaxonomyRef? variant)
    {
        var parts = new List<string>(4);

        if (year is { } value)
        {
            parts.Add(value.ToString(CultureInfo.InvariantCulture));
        }

        AddNonEmpty(parts, make?.Name);
        AddNonEmpty(parts, model?.Name);
        AddNonEmpty(parts, variant?.Name);

        return string.Join(' ', parts);
    }

    private static void AddNonEmpty(List<string> parts, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add(value);
        }
    }

    private static void AddIfPresent(List<Guid> ids, Guid? id)
    {
        if (id is { } value)
        {
            ids.Add(value);
        }
    }

    private static TaxonomyRef? Lookup(IReadOnlyDictionary<Guid, TaxonomyRef> refs, Guid? id)
        => id is { } value ? refs.GetValueOrDefault(value) : null;
}
