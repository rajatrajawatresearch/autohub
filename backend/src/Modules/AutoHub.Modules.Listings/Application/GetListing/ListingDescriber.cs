using System.Globalization;
using System.Text.Json.Nodes;
using AutoHub.Modules.Catalog.Contracts;
using AutoHub.Modules.Listings.Contracts;
using AutoHub.Modules.Listings.Domain.Listings;
using AutoHub.Platform.Taxonomy;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Listings.Application.GetListing;

/// <summary>
/// Assembles the one shape every listing write and read returns.
/// </summary>
/// <remarks>
/// Every use case in this module — create, add media, publish, get — ends by returning a listing,
/// and they must all return the <i>same</i> listing. A second assembler is how a detail page and the
/// response to a publish end up disagreeing about the quality score, which a seller reads as the
/// site losing their photos.
/// <para>
/// A plain class rather than a MediatR query, because the search handler calls it once per result
/// row. Sending twenty-four queries through the mediator to render one page is twenty-four times the
/// pipeline for what is a method call.
/// </para>
/// </remarks>
public sealed class ListingDescriber
{
    private readonly IWorkflowEngine _workflow;
    private readonly IVehicleDirectory _vehicles;
    private readonly IVehicleTaxonomy _taxonomy;
    private readonly ITaxonomyRegistry _registry;

    public ListingDescriber(
        IWorkflowEngine workflow,
        IVehicleDirectory vehicles,
        IVehicleTaxonomy taxonomy,
        ITaxonomyRegistry registry)
    {
        _workflow = workflow;
        _vehicles = vehicles;
        _taxonomy = taxonomy;
        _registry = registry;
    }

    public async Task<Result<ListingDetail>> DescribeAsync(
        Listing listing,
        WorkflowActor actor,
        CancellationToken cancellationToken)
    {
        var snapshot = await _workflow.GetSnapshotAsync(
            ListingWorkflows.Listing,
            listing.Id,
            GuardContext(listing),
            actor,
            cancellationToken);

        if (snapshot.IsFailure)
        {
            // A listing with no workflow instance is a listing that was written without going
            // through the create use case. There is no honest state to render for it, and inventing
            // one would be the status column re-entering through the back door.
            return Result.Failure<ListingDetail>(snapshot.Error);
        }

        var vehicle = await _vehicles.GetAsync(listing.VehicleId, cancellationToken);

        var values = await _registry.ResolveAttributeValuesAsync(
            ListingEntityTypes.Listing, [listing.Id], cancellationToken);

        var attributes = values.IsSuccess && values.Value.ByEntity.TryGetValue(listing.Id, out var resolved)
            ? resolved
            : [];

        var title = listing.Title
            ?? (vehicle.IsSuccess
                ? await ComposeTitleAsync(vehicle.Value, cancellationToken)
                : string.Empty);

        return new ListingDetail
        {
            Listing = listing.ToSummary(),
            Title = title,
            Description = listing.Description,
            Workflow = snapshot.Value,
            Quality = listing.Quality,
            Media = [.. listing.Media
                .OrderByDescending(m => m.IsCover)
                .ThenBy(m => m.SortOrder)
                .Select(m => new ListingMediaView(
                    m.Id, m.ObjectKey, m.MediaTypeCode, m.PromptCode, m.IsCover, m.SortOrder, m.AspectRatio))],
            Attributes = attributes,
            SpecStrip = BuildSpecStrip(attributes),
            Vehicle = vehicle.IsSuccess ? vehicle.Value : null,
        };
    }

    /// <summary>
    /// The facts a workflow guard is allowed to evaluate against.
    /// </summary>
    /// <remarks>
    /// Assembled here so that "a listing may not be published without six photos" is a JsonLogic
    /// guard an operator writes rather than an <c>if</c> a developer ships. Adding a fact to this
    /// object is the only code change a whole family of new rules needs — which is the trade the
    /// workflow registry is making.
    /// </remarks>
    public static JsonObject GuardContext(Listing listing) => new()
    {
        ["intent"] = listing.Intent.ToWireName(),
        ["cityCode"] = listing.CityCode,
        ["sellerPartyId"] = listing.SellerPartyId.ToString(),
        ["priceMinorUnits"] = listing.PriceMinorUnits,
        ["currency"] = listing.PriceCurrencyCode,
        ["mediaCount"] = listing.Media.Count,
        ["hasCover"] = listing.Cover is not null,
        ["qualityScore"] = listing.QualityScore,
        ["hasDescription"] = !string.IsNullOrWhiteSpace(listing.Description),
    };

    /// <summary>
    /// The strip the card renders: the category's attributes in the registry's own order.
    /// </summary>
    /// <remarks>
    /// Values are data, not display strings — see <see cref="SpecStripItem"/>. Attributes with no
    /// renderable value are dropped rather than shown blank, because an empty row on a spec strip
    /// reads as missing information about the car rather than missing information in the database.
    /// </remarks>
    private static List<SpecStripItem> BuildSpecStrip(IReadOnlyList<ResolvedAttribute> attributes)
    {
        var items = new List<SpecStripItem>(attributes.Count);

        foreach (var attribute in attributes)
        {
            if (IsMoney(attribute))
            {
                continue;
            }

            var value = attribute.DisplayValue ?? Stringify(attribute.Value);

            if (!string.IsNullOrWhiteSpace(value))
            {
                items.Add(new SpecStripItem(attribute.Code, attribute.Label, value, attribute.Unit));
            }
        }

        return items;
    }

    /// <summary>
    /// Money never belongs on the spec strip.
    /// </summary>
    /// <remarks>
    /// Two reasons, and the first one shipped before anyone noticed. A spec item carries a datum and
    /// a unit and nothing else — no currency, no data type — so a client has no way to know it is
    /// looking at money, and rendered it verbatim: every card on the marketplace showed its price
    /// twice, once as <c>₹13.9L</c> and once as <c>139000000</c>. Raw paise in front of a buyer is
    /// the "format at the edge" rule producing exactly what it exists to prevent.
    /// <para>
    /// The second reason is that it was duplication even when it looked right. Price is a
    /// first-class field on every listing response — <c>priceMinor</c> plus <c>currency</c> — and
    /// every surface already renders it through the money component. A money attribute that is not
    /// the asking price needs a home of its own with a currency beside it, not a row on a strip
    /// designed for "38,400 km".
    /// </para>
    /// </remarks>
    private static bool IsMoney(ResolvedAttribute attribute)
        => string.Equals(attribute.DataType, "money", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The invariant string form of a typed value.
    /// </summary>
    /// <remarks>
    /// Invariant on purpose. A number rendered under the server's culture is a bug that only
    /// reproduces on the machine with the wrong culture, and "5,49,000" versus "549000" is the
    /// difference between a client that can parse the value and one that cannot.
    /// </remarks>
    private static string Stringify(TypedAttributeValue value) => value.DataType switch
    {
        AttributeDataType.String or AttributeDataType.Enum => value.Text ?? string.Empty,
        AttributeDataType.Number or AttributeDataType.Integer =>
            value.Number?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        AttributeDataType.Boolean => value.Flag?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        AttributeDataType.Date => value.Date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
        AttributeDataType.Money =>
            value.Amount?.MinorUnits.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        AttributeDataType.MultiEnum => string.Join(", ", value.Choices),
        _ => string.Empty,
    };

    /// <summary>
    /// "2021 Maruti Suzuki Swift VXi", falling back to the plate when the taxonomy is unknown.
    /// </summary>
    /// <remarks>
    /// A vehicle created from a plate alone has no make and no model, and its honest title is the
    /// plate. "Unknown Unknown" looks like a defect to the seller who just typed a valid number.
    /// </remarks>
    private async Task<string> ComposeTitleAsync(VehicleSummary vehicle, CancellationToken cancellationToken)
    {
        var ids = new List<Guid>(3);
        AddIfPresent(ids, vehicle.MakeId);
        AddIfPresent(ids, vehicle.ModelId);
        AddIfPresent(ids, vehicle.VariantId);

        var described = ids.Count == 0
            ? new Dictionary<Guid, TaxonomyRef>()
            : (await _taxonomy.DescribeAsync(ids, cancellationToken)) is { IsSuccess: true } result
                ? result.Value
                : new Dictionary<Guid, TaxonomyRef>();

        var parts = new List<string>(4);

        if (vehicle.ManufactureYear is { } year)
        {
            parts.Add(year.ToString(CultureInfo.InvariantCulture));
        }

        foreach (var id in ids)
        {
            if (described.TryGetValue(id, out var reference) && !string.IsNullOrWhiteSpace(reference.Name))
            {
                parts.Add(reference.Name);
            }
        }

        return parts.Count > 0
            ? string.Join(' ', parts)
            : vehicle.RegistrationDisplay ?? vehicle.Registration ?? string.Empty;
    }

    private static void AddIfPresent(List<Guid> ids, Guid? id)
    {
        if (id is { } value)
        {
            ids.Add(value);
        }
    }
}
