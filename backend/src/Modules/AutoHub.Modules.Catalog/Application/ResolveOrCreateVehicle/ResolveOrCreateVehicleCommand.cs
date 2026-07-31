using System.Text.Json.Nodes;
using AutoHub.Modules.Catalog.Contracts;
using AutoHub.SharedKernel.Results;
using MediatR;

namespace AutoHub.Modules.Catalog.Application.ResolveOrCreateVehicle;

/// <summary>
/// "Here is what I know about a car — give me the canonical one."
/// </summary>
/// <remarks>
/// Every path that can introduce a vehicle funnels through this command: the homepage plate box, the
/// sell form, a dealer's nightly stock feed, an auction lot import, an inspection booked against a
/// car we have never seen. That is the point. A second creation path is a second chance to create the
/// duplicate this module exists to prevent, and duplicates are not discovered until a buyer sees the
/// same car three times and stops trusting the site.
/// </remarks>
public sealed record ResolveOrCreateVehicleCommand : IRequest<Result<ResolveOrCreateVehicleResponse>>
{
    /// <summary>Taxonomy category code, e.g. <c>suv</c>. Decides which attributes are legal.</summary>
    public required string CategoryCode { get; init; }

    /// <summary>
    /// Everything the caller knows the vehicle by. At least one is required.
    /// </summary>
    /// <remarks>
    /// A list rather than three named properties because the set is open: a Bharat-series plate, a
    /// temporary registration and an import's bill-of-entry number are all identifiers, and each
    /// would have been another nullable parameter on a signature that already had too many.
    /// </remarks>
    public required IReadOnlyList<VehicleIdentifierInput> Identifiers { get; init; }

    public string? MakeCode { get; init; }

    public string? ModelCode { get; init; }

    public string? VariantCode { get; init; }

    public int? ManufactureYear { get; init; }

    public int? ManufactureMonth { get; init; }

    public DateOnly? FirstRegisteredOn { get; init; }

    /// <summary>Taxonomy option value naming the source — <c>vahan</c>, <c>rc_ocr</c>, <c>seller</c>.</summary>
    public string? ProvenanceCode { get; init; }

    /// <summary>
    /// Attribute values keyed by attribute code, validated against the category before storage.
    /// </summary>
    /// <remarks>
    /// Absent and empty are different. Absent leaves the vehicle's existing attributes alone, which
    /// is what a plate lookup that learned nothing new should do; empty clears them, which is what an
    /// operator correcting a bad import means.
    /// </remarks>
    public JsonObject? Attributes { get; init; }
}

/// <summary>The canonical vehicle, and whether it was found, reconciled or created.</summary>
/// <remarks>
/// The caller needs the distinction. A sell flow that created a vehicle shows the full form; one
/// that matched an existing vehicle prefills it and can say "we already know this car", which is
/// worth several points of completion.
/// </remarks>
public sealed record ResolveOrCreateVehicleResponse(VehicleSummary Vehicle, VehicleResolution Resolution);
