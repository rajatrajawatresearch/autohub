using System.Text.Json.Nodes;
using AutoHub.Modules.Catalog.Application.Abstractions;
using AutoHub.Modules.Catalog.Application.GetVehicle;
using AutoHub.Modules.Catalog.Application.Validation;
using AutoHub.Modules.Catalog.Contracts;
using AutoHub.Platform.Taxonomy;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using FluentValidation;
using MediatR;

namespace AutoHub.Modules.Catalog.Application.UpdateVehicleAttributes;

public sealed class UpdateVehicleAttributesHandler
    : IRequestHandler<UpdateVehicleAttributesCommand, Result<VehicleDetail>>
{
    private readonly IVehicleRepository _vehicles;
    private readonly IVehicleTaxonomy _taxonomy;
    private readonly ITaxonomyRegistry _registry;
    private readonly IEntityAttributeStore _attributes;
    private readonly GetVehicleHandler _describe;
    private readonly IValidator<UpdateVehicleAttributesCommand> _validator;
    private readonly IClock _clock;

    public UpdateVehicleAttributesHandler(
        IVehicleRepository vehicles,
        IVehicleTaxonomy taxonomy,
        ITaxonomyRegistry registry,
        IEntityAttributeStore attributes,
        GetVehicleHandler describe,
        IValidator<UpdateVehicleAttributesCommand> validator,
        IClock clock)
    {
        _vehicles = vehicles;
        _taxonomy = taxonomy;
        _registry = registry;
        _attributes = attributes;
        _describe = describe;
        _validator = validator;
        _clock = clock;
    }

    public async Task<Result<VehicleDetail>> Handle(
        UpdateVehicleAttributesCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return RequestValidation.ToError(validation);
        }

        var vehicle = await _vehicles.GetAsync(command.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            return Error.NotFound("catalog.vehicle.not_found", "No vehicle with that id.")
                .With("vehicleId", command.VehicleId);
        }

        // The category's code, not the caller's — see the command's remarks on why the caller does
        // not get to name the category its payload is validated against.
        var described = await _taxonomy.DescribeAsync([vehicle.CategoryId], cancellationToken);
        if (described.IsFailure)
        {
            return Result.Failure<VehicleDetail>(described.Error);
        }

        if (!described.Value.TryGetValue(vehicle.CategoryId, out var category))
        {
            return Error.Unexpected(
                "catalog.vehicle.category_missing",
                "This vehicle points at a taxonomy category that no longer exists.")
                .With("vehicleId", vehicle.Id)
                .With("categoryId", vehicle.CategoryId);
        }

        var validated = await _registry.ValidateAttributesAsync(
            category.Code, command.Attributes, cancellationToken);

        if (validated.IsFailure)
        {
            return Result.Failure<VehicleDetail>(validated.Error);
        }

        var stored = await _attributes.ReplaceAsync(
            CatalogEntityTypes.Vehicle, vehicle.Id, validated.Value, cancellationToken);

        if (stored.IsFailure)
        {
            return Result.Failure<VehicleDetail>(stored.Error);
        }

        var projection = new JsonObject();
        foreach (var (code, value) in validated.Value)
        {
            projection[code] = value.ToJson();
        }

        vehicle.ProjectAttributes(projection, _clock.UtcNow);

        await _vehicles.SaveChangesAsync(cancellationToken);

        // Re-described after the save so the caller sees labelled, typed values rather than the raw
        // bag it submitted — which is what the next screen needs to render and what it would
        // otherwise have to make a second request for.
        return await _describe.DescribeAsync(vehicle, cancellationToken);
    }
}
