using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Taxonomy;

/// <summary>
/// A nameplate — Swift, Nexon, Bolero, Dost.
/// </summary>
/// <remarks>
/// The model is where the tree and the manufacturer meet: it belongs to exactly one
/// <see cref="VehicleMake"/> and sits in exactly one <see cref="VehicleCategory"/>, which is what
/// makes "which makes sell SUVs?" a join rather than a maintained list.
/// <para>
/// <see cref="Code"/> is globally unique, not unique-per-make, because the published contract looks
/// up variants by model alone (<c>?model=swift</c>). Scoping the code to the make would make that
/// query ambiguous the first time two manufacturers ship a nameplate with the same slug, and the
/// ambiguity would surface as a silently wrong variant list rather than an error.
/// </para>
/// <para>
/// Production years are years, not dates. A model is "2005 to 2020" in every conversation a buyer,
/// a dealer and a valuation model ever have; storing a launch timestamp would invent a precision the
/// source data does not have and that nobody would trust.
/// </para>
/// </remarks>
public sealed class VehicleModel : AggregateRoot, IAuditable, ISoftDeletable
{
    private VehicleModel(
        Guid id,
        Guid makeId,
        Guid categoryId,
        string code,
        string name,
        int? productionFrom,
        int? productionTo)
        : base(id)
    {
        MakeId = makeId;
        CategoryId = categoryId;
        Code = code;
        Name = name;
        ProductionFrom = productionFrom;
        ProductionTo = productionTo;
        IsActive = true;
    }

    private VehicleModel() { }

    public Guid MakeId { get; private set; }

    public Guid CategoryId { get; private set; }

    /// <summary>Globally unique URL slug, e.g. <c>swift</c> or <c>honda-city</c>.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>First model year sold in India. Null when the source data does not say.</summary>
    public int? ProductionFrom { get; private set; }

    /// <summary>Last model year. Null means still in production.</summary>
    public int? ProductionTo { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public static Result<VehicleModel> Create(
        Guid makeId,
        Guid categoryId,
        string code,
        string name,
        int? productionFrom = null,
        int? productionTo = null)
    {
        var validCode = TaxonomyCode.Validate(code, "taxonomy.model.code_invalid");
        if (validCode.IsFailure)
        {
            return Result.Failure<VehicleModel>(validCode.Error);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("taxonomy.model.name_required", "A model needs a display name.");
        }

        if (productionFrom is { } from && productionTo is { } to && to < from)
        {
            return Error.Validation(
                "taxonomy.model.production_range_inverted",
                "The last production year cannot be before the first.")
                .With("productionFrom", from)
                .With("productionTo", to);
        }

        return new VehicleModel(UuidV7.New(), makeId, categoryId, validCode.Value, name.Trim(), productionFrom, productionTo);
    }

    /// <summary>
    /// Whether a given model year plausibly exists. Used to reject "2029 Swift" on the sell form
    /// before it becomes a listing nobody can value.
    /// </summary>
    public bool CoversYear(int year)
        => (ProductionFrom is null || year >= ProductionFrom)
           && (ProductionTo is null || year <= ProductionTo);

    public void Recategorise(Guid categoryId) => CategoryId = categoryId;

    public void SetProductionRange(int? from, int? to)
    {
        ProductionFrom = from;
        ProductionTo = to;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
