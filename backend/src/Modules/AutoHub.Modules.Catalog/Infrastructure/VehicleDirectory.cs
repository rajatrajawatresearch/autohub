using AutoHub.Modules.Catalog.Application.Abstractions;
using AutoHub.Modules.Catalog.Application.ResolveOrCreateVehicle;
using AutoHub.Modules.Catalog.Contracts;
using AutoHub.Modules.Catalog.Domain.Vehicles;
using AutoHub.SharedKernel.Results;
using MediatR;

namespace AutoHub.Modules.Catalog.Infrastructure;

/// <summary>
/// The catalog's contract, served in-process today and over HTTP after extraction.
/// </summary>
/// <remarks>
/// A thin adapter on purpose. Every method here either projects an aggregate the repository already
/// loaded or forwards to the same command the module's own API endpoint sends, so a listing created
/// by another module goes through the identical deduplication path as one created by the sell form.
/// A second implementation of resolution behind this interface would be a second way to create a
/// duplicate.
/// <para>
/// When this module becomes a service, this class is replaced by a generated HTTP client and nothing
/// on the calling side changes — which is the promise the modular monolith is making and the reason
/// no other module is allowed to hold a <c>Vehicle</c>.
/// </para>
/// </remarks>
internal sealed class VehicleDirectory : IVehicleDirectory
{
    private readonly IVehicleRepository _vehicles;
    private readonly IVehicleTaxonomy _taxonomy;
    private readonly ISender _sender;

    public VehicleDirectory(IVehicleRepository vehicles, IVehicleTaxonomy taxonomy, ISender sender)
    {
        _vehicles = vehicles;
        _taxonomy = taxonomy;
        _sender = sender;
    }

    public async Task<Result<VehicleSummary>> GetAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicles.GetAsync(vehicleId, cancellationToken);

        return vehicle is null
            ? Error.NotFound("catalog.vehicle.not_found", "No vehicle with that id.")
                .With("vehicleId", vehicleId)
            : vehicle.ToSummary();
    }

    public async Task<Result<IReadOnlyDictionary<Guid, VehicleSummary>>> GetManyAsync(
        IReadOnlyCollection<Guid> vehicleIds,
        CancellationToken cancellationToken)
    {
        if (vehicleIds.Count == 0)
        {
            return new Dictionary<Guid, VehicleSummary>();
        }

        var vehicles = await _vehicles.GetManyAsync(vehicleIds, cancellationToken);

        // Ids that no longer resolve are simply absent from the dictionary rather than failing the
        // batch. A soft-deleted vehicle behind one of twenty-four search results must not blank the
        // page; the caller renders the twenty-three it has.
        return vehicles.ToDictionary(v => v.Id, v => v.ToSummary());
    }

    public async Task<Result<VehicleSummary>> FindByIdentifierAsync(
        VehicleIdentifierInput identifier,
        CancellationToken cancellationToken)
    {
        var parsed = VehicleIdentifier.TryParse(identifier.Type, identifier.Value);
        if (parsed.IsFailure)
        {
            return Result.Failure<VehicleSummary>(parsed.Error);
        }

        var matches = await _vehicles.FindByActiveIdentifiersAsync([parsed.Value], cancellationToken);

        return matches.Count switch
        {
            0 => Error.NotFound("catalog.vehicle.not_found", "No vehicle is currently known by that identifier.")
                .With("identifierType", identifier.Type.ToWireName())
                .With("value", parsed.Value.Value),

            1 => matches[0].ToSummary(),

            // Unreachable through the active-period unique index. Reaching it means the index is
            // missing, and picking one of two vehicles arbitrarily would attach a valuation, and then
            // a listing, to whichever row the planner happened to return first.
            _ => Error.Conflict(
                "catalog.vehicle.ambiguous_identifier",
                "That identifier is active on more than one vehicle.")
                .With("identifierType", identifier.Type.ToWireName())
                .With("value", parsed.Value.Value),
        };
    }

    public async Task<Result<VehicleSummary>> ResolveOrCreateAsync(
        Guid categoryId,
        IReadOnlyList<VehicleIdentifierInput> identifiers,
        CancellationToken cancellationToken)
    {
        // The contract takes an id because that is what a caller holding a listing or an auction lot
        // has; the command takes a code because that is what a form posts. Translating here keeps
        // both callers honest instead of adding a second parameter to the command that half of them
        // would leave null.
        var described = await _taxonomy.DescribeAsync([categoryId], cancellationToken);
        if (described.IsFailure)
        {
            return Result.Failure<VehicleSummary>(described.Error);
        }

        if (!described.Value.TryGetValue(categoryId, out var category))
        {
            return Error.NotFound("catalog.taxonomy.category_not_found", "No category with that id.")
                .With("categoryId", categoryId);
        }

        var resolved = await _sender.Send(
            new ResolveOrCreateVehicleCommand
            {
                CategoryCode = category.Code,
                Identifiers = identifiers,
            },
            cancellationToken);

        return resolved.Map(response => response.Vehicle);
    }
}
