using System.Text.Json.Nodes;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Taxonomy;

/// <summary>
/// A specific trim of a model — "Swift VXi AMT 2021", "Nexon EV Max XZ+ Lux".
/// </summary>
/// <remarks>
/// The variant is where a valuation stops guessing. Two 2021 Swifts differ by ₹1.2 lakh depending on
/// trim, and a marketplace that only knows "Swift" prices both wrong and loses trust on the first
/// quote. It is also the row a seller picks once and never sees again — everything downstream (the
/// title, the spec strip, the insurance quote, the finance eligibility) is derived from it.
/// <para>
/// <b>Fuel, transmission and body type are codes, not enums.</b> They point at
/// <see cref="AttributeOption"/> values in this same registry. Hydrogen, CNG-retrofit and
/// strong-hybrid all arrived in the Indian market after any enum a developer would have written, and
/// each would have been an <c>ALTER TYPE</c>, a migration and a deploy. Here each is an insert.
/// </para>
/// <para>
/// <see cref="SpecJson"/> is the escape hatch for the long tail: kerb weight, boot litres, ground
/// clearance, ARAI mileage, charging time, payload, axle configuration. Those are per-category and
/// endless, and a column per spec would mean a migration per category. The handful that people
/// actually filter and sort on are promoted out of the bag into
/// <see cref="AttributeDefinition"/>s and stored as <see cref="EntityAttributeValue"/> rows, where
/// they are indexable — the bag is for display, never for querying.
/// </para>
/// </remarks>
public sealed class VehicleVariant : AggregateRoot, IAuditable, ISoftDeletable
{
    private VehicleVariant(Guid id, Guid modelId, string code, string name, int? yearFrom, int? yearTo)
        : base(id)
    {
        ModelId = modelId;
        Code = code;
        Name = name;
        YearFrom = yearFrom;
        YearTo = yearTo;
        IsActive = true;
    }

    private VehicleVariant() { }

    public Guid ModelId { get; private set; }

    /// <summary>Unique within the model, e.g. <c>vxi-amt</c>.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>First model year this trim was offered. Null when unknown.</summary>
    public int? YearFrom { get; private set; }

    /// <summary>Last model year. Null means current.</summary>
    public int? YearTo { get; private set; }

    /// <summary>Attribute option value, e.g. <c>petrol</c>. Resolved against <c>fuel_type</c>.</summary>
    public string? FuelCode { get; private set; }

    /// <summary>Attribute option value, e.g. <c>amt</c>. Resolved against <c>transmission</c>.</summary>
    public string? TransmissionCode { get; private set; }

    /// <summary>Attribute option value, e.g. <c>hatchback</c>. Resolved against <c>body_type</c>.</summary>
    public string? BodyTypeCode { get; private set; }

    public int? Seats { get; private set; }

    /// <summary>Engine displacement in cc. Null on an EV, which is a fact, not missing data.</summary>
    public int? EngineCc { get; private set; }

    /// <summary>Maximum power in bhp. Fractional — 88.5 bhp is a real published figure.</summary>
    public decimal? PowerBhp { get; private set; }

    /// <summary>The long tail of specifications, as a JSON object. Display only — never queried.</summary>
    public string? SpecJson { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public static Result<VehicleVariant> Create(
        Guid modelId,
        string code,
        string name,
        int? yearFrom = null,
        int? yearTo = null)
    {
        var validCode = TaxonomyCode.Validate(code, "taxonomy.variant.code_invalid");
        if (validCode.IsFailure)
        {
            return Result.Failure<VehicleVariant>(validCode.Error);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("taxonomy.variant.name_required", "A variant needs a display name.");
        }

        if (yearFrom is { } from && yearTo is { } to && to < from)
        {
            return Error.Validation(
                "taxonomy.variant.year_range_inverted",
                "The last model year cannot be before the first.")
                .With("yearFrom", from)
                .With("yearTo", to);
        }

        return new VehicleVariant(UuidV7.New(), modelId, validCode.Value, name.Trim(), yearFrom, yearTo);
    }

    public void SetPowertrain(
        string? fuelCode,
        string? transmissionCode,
        string? bodyTypeCode,
        int? seats,
        int? engineCc,
        decimal? powerBhp)
    {
        FuelCode = fuelCode;
        TransmissionCode = transmissionCode;
        BodyTypeCode = bodyTypeCode;
        Seats = seats;
        EngineCc = engineCc;
        PowerBhp = powerBhp;
    }

    /// <summary>
    /// Replaces the specification bag.
    /// </summary>
    /// <remarks>
    /// Rejects anything that is not a JSON object, because a bare array or scalar here would be
    /// silently unreadable by every consumer that expects to look a key up in it.
    /// </remarks>
    public Result SetSpec(JsonNode? spec)
    {
        if (spec is null)
        {
            SpecJson = null;
            return Result.Success();
        }

        if (spec is not JsonObject)
        {
            return Error.Validation(
                "taxonomy.variant.spec_not_object",
                "The specification bag must be a JSON object.");
        }

        SpecJson = spec.ToJsonString();
        return Result.Success();
    }

    public JsonNode? Spec()
        => string.IsNullOrWhiteSpace(SpecJson) ? null : JsonNode.Parse(SpecJson);

    public bool CoversYear(int year)
        => (YearFrom is null || year >= YearFrom)
           && (YearTo is null || year <= YearTo);

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
