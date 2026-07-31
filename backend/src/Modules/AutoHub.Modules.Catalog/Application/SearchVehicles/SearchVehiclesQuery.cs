using AutoHub.Modules.Catalog.Application.GetVehicle;
using AutoHub.SharedKernel.Results;
using MediatR;

namespace AutoHub.Modules.Catalog.Application.SearchVehicles;

/// <summary>
/// Operational search over the canonical fleet.
/// </summary>
/// <remarks>
/// Not the buyer-facing search. That one runs over listings, through the search index, and answers
/// "what can I buy in Pune?"; this one answers "which vehicle is this plate?" and "did we already
/// import this VIN?", which are questions an admin, a support agent and a dealer reconciling a feed
/// ask. Keeping them separate is what lets the listing index carry facets and this one carry
/// identifiers.
/// </remarks>
public sealed record SearchVehiclesQuery : IRequest<Result<VehicleSearchResults>>
{
    /// <summary>
    /// Free text, matched against identifiers.
    /// </summary>
    /// <remarks>
    /// Canonicalised the same way a stored identifier is, so "mh 12 ab 1234" finds
    /// <c>MH12AB1234</c>. A search box that only matched the stored spelling would fail for every
    /// user who types a plate the way it is painted.
    /// </remarks>
    public string? Query { get; init; }

    public string? CategoryCode { get; init; }

    public string? MakeCode { get; init; }

    public string? ModelCode { get; init; }

    public int? ManufactureYearFrom { get; init; }

    public int? ManufactureYearTo { get; init; }

    /// <summary>Attribute equality filters keyed by attribute code — the contract's <c>attr.*</c>.</summary>
    public IReadOnlyDictionary<string, string> Attributes { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    public Guid? Cursor { get; init; }

    public int Limit { get; init; } = 24;
}

/// <summary>
/// One page, in the cursor-paginated envelope §0 of the contract specifies.
/// </summary>
/// <remarks>
/// <see cref="TotalEstimate"/> is an estimate on purpose. An exact <c>COUNT(*)</c> over a
/// multi-million-row table with a filter is a sequential scan on the hottest query in the product,
/// and no user has ever needed to know there are 1,284 rather than 1,300 results.
/// </remarks>
public sealed record VehicleSearchResults(
    IReadOnlyList<VehicleDetail> Items,
    Guid? NextCursor,
    long TotalEstimate);
