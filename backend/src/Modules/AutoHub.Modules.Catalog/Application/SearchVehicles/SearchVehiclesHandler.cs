using AutoHub.Modules.Catalog.Application.Abstractions;
using AutoHub.Modules.Catalog.Application.GetVehicle;
using AutoHub.Modules.Catalog.Application.Validation;
using AutoHub.Modules.Catalog.Contracts;
using AutoHub.SharedKernel.Results;
using FluentValidation;
using MediatR;

namespace AutoHub.Modules.Catalog.Application.SearchVehicles;

public sealed class SearchVehiclesHandler : IRequestHandler<SearchVehiclesQuery, Result<VehicleSearchResults>>
{
    private readonly IVehicleRepository _vehicles;
    private readonly IVehicleTaxonomy _taxonomy;
    private readonly GetVehicleHandler _describe;
    private readonly IValidator<SearchVehiclesQuery> _validator;

    public SearchVehiclesHandler(
        IVehicleRepository vehicles,
        IVehicleTaxonomy taxonomy,
        GetVehicleHandler describe,
        IValidator<SearchVehiclesQuery> validator)
    {
        _vehicles = vehicles;
        _taxonomy = taxonomy;
        _describe = describe;
        _validator = validator;
    }

    public async Task<Result<VehicleSearchResults>> Handle(
        SearchVehiclesQuery query,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return RequestValidation.ToError(validation);
        }

        var filters = await _taxonomy.ResolveFiltersAsync(
            query.CategoryCode, query.MakeCode, query.ModelCode, cancellationToken);

        if (filters.IsFailure)
        {
            return Result.Failure<VehicleSearchResults>(filters.Error);
        }

        var criteria = new VehicleSearchCriteria
        {
            Query = Canonicalise(query.Query),
            CategoryId = filters.Value.CategoryId,
            MakeId = filters.Value.MakeId,
            ModelId = filters.Value.ModelId,
            ManufactureYearFrom = query.ManufactureYearFrom,
            ManufactureYearTo = query.ManufactureYearTo,
            Attributes = query.Attributes,
            Cursor = query.Cursor,
            Limit = query.Limit,
        };

        var page = await _vehicles.SearchAsync(criteria, cancellationToken);

        var items = new List<VehicleDetail>(page.Items.Count);
        foreach (var vehicle in page.Items)
        {
            items.Add(await _describe.DescribeAsync(vehicle, cancellationToken));
        }

        return new VehicleSearchResults(items, page.NextCursor, page.TotalEstimate);
    }

    /// <summary>
    /// Canonicalises a free-text term the way a stored identifier is canonicalised.
    /// </summary>
    /// <remarks>
    /// A user types the plate the way it is painted — <c>MH 12 AB 1234</c> — and the column holds
    /// <c>MH12AB1234</c>. Stripping separators and upper-casing here means the store can compare
    /// against the indexed column directly instead of wrapping it in a function, which would make
    /// the index unusable and turn a plate lookup into a table scan.
    /// <para>
    /// Uses the invariant culture explicitly. An implicit <c>ToUpper()</c> under a Turkish locale
    /// maps <c>i</c> to a dotted capital that matches nothing, and the bug only reproduces on the
    /// machine with the wrong locale.
    /// </para>
    /// </remarks>
    private static string? Canonicalise(string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return null;
        }

        var stripped = new string([.. term.Where(char.IsLetterOrDigit)]);
        return stripped.Length == 0 ? null : stripped.ToUpperInvariant();
    }
}
