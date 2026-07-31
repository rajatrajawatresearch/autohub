using System.Text.Json.Nodes;
using AutoHub.Modules.Catalog.Application.GetVehicle;
using AutoHub.SharedKernel.Results;
using MediatR;

namespace AutoHub.Modules.Catalog.Application.UpdateVehicleAttributes;

/// <summary>
/// Replaces a vehicle's attribute values.
/// </summary>
/// <remarks>
/// A full replace, not a patch. The payload is the complete set of values for the category, so an
/// attribute the caller omits is an attribute the caller is clearing — which is the only way a
/// seller who ticked "sunroof" by mistake can untick it. A merge semantics would make every field
/// permanently settable and never unsettable.
/// </remarks>
public sealed record UpdateVehicleAttributesCommand : IRequest<Result<VehicleDetail>>
{
    public required Guid VehicleId { get; init; }

    /// <summary>
    /// Values keyed by attribute code. Validated against the vehicle's <b>current</b> category.
    /// </summary>
    /// <remarks>
    /// Against the vehicle's category rather than a category the caller names, so a client cannot
    /// widen what it is allowed to write by claiming a category with looser attributes. Moving a
    /// vehicle between categories is a separate, permissioned operation for exactly that reason.
    /// </remarks>
    public required JsonObject Attributes { get; init; }
}
