using System.Text.Json.Nodes;
using AutoHub.Modules.Catalog.Contracts;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Vehicles;

namespace AutoHub.Modules.Catalog.Domain.Vehicles;

/// <summary>
/// The canonical vehicle. One physical car, however many times the platform meets it.
/// </summary>
/// <remarks>
/// The north star's diagram has exactly one box above listings, valuations and inspections, and this
/// is it. The same Swift can arrive as a C2C sell-flow draft on Monday, in a dealer's bulk stock
/// upload on Tuesday and as an auction lot on Friday; if those produce three rows, then the price
/// history is split three ways, the inspection report is attached to the wrong one, and a buyer sees
/// the same car three times in one search page and stops trusting the site.
/// <para>
/// So the vehicle carries <b>identity and provenance only</b>. Everything that varies by transaction
/// — asking price, city, seller, media, condition at this moment — belongs to a listing, because a
/// vehicle sold twice has two prices and one identity.
/// </para>
/// <para>
/// The taxonomy pointers are ids rather than codes, and nullable rather than required. Ids because a
/// make can be renamed and a code cannot be, and nullable because a plate typed into the homepage
/// hero resolves to a vehicle long before anybody has told us which trim it is — refusing to record
/// the car until the variant is known would lose the valuation request that motivated it.
/// </para>
/// </remarks>
public sealed class Vehicle : AggregateRoot, IAuditable, ISoftDeletable
{
    private readonly List<VehicleIdentity> _identities = [];

    private Vehicle(Guid id, Guid categoryId) : base(id) => CategoryId = categoryId;

    private Vehicle() { }

    /// <summary>Taxonomy category. Decides which attributes this vehicle is allowed to carry.</summary>
    public Guid CategoryId { get; private set; }

    public Guid? MakeId { get; private set; }

    public Guid? ModelId { get; private set; }

    public Guid? VariantId { get; private set; }

    /// <summary>
    /// Year of manufacture, which is not the year of registration.
    /// </summary>
    /// <remarks>
    /// A 2019-built car registered in January 2020 is priced by the market as a 2019 car, and the gap
    /// between the two dates is itself a signal — a long lag means year-end stock, which depresses
    /// resale permanently. Storing only the registration date would throw that away.
    /// </remarks>
    public int? ManufactureYear { get; private set; }

    public int? ManufactureMonth { get; private set; }

    /// <summary>First registration date, from the RC. Drives age, insurance and permit arithmetic.</summary>
    public DateOnly? FirstRegisteredOn { get; private set; }

    /// <summary>Taxonomy option value naming where this record came from — <c>vahan</c>, <c>seller</c>.</summary>
    public string? ProvenanceCode { get; private set; }

    /// <summary>
    /// The denormalised attribute bag, rebuilt from the EAV rows in the same transaction that writes
    /// them.
    /// </summary>
    /// <remarks>
    /// Not a second source of truth — the <c>entity_attribute_values</c> rows are, and they are what
    /// range queries index. This column exists because the two hottest reads are shaped differently
    /// from a range scan: rendering a spec sheet wants every attribute of one vehicle in one row, and
    /// containment (<c>attributes @&gt; '{"fuel_type":"cng"}'</c>) wants a GIN index. Both are cheap
    /// here and expensive over the EAV table.
    /// </remarks>
    public string? AttributesJson { get; private set; }

    public IReadOnlyList<VehicleIdentity> Identities => _identities;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public static Vehicle Register(Guid categoryId, DateTimeOffset now)
    {
        var vehicle = new Vehicle(UuidV7.New(), categoryId);
        vehicle.Raise(new VehicleRegistered(vehicle.Id, categoryId, now));
        return vehicle;
    }

    /// <summary>The identifier of a given type that is in force right now, if any.</summary>
    public VehicleIdentity? ActiveIdentity(VehicleIdentifierType type)
        => _identities.Find(i => i.IsActive && i.IdentifierType == type);

    /// <summary>Every identifier in force, strongest first — the order deduplication trusts.</summary>
    public IEnumerable<VehicleIdentity> ActiveIdentities()
        => _identities.Where(i => i.IsActive).OrderByDescending(i => i.IdentifierType);

    public bool Holds(VehicleIdentifier identifier)
        => _identities.Exists(i =>
            i.IsActive
            && i.IdentifierType == identifier.Type
            && string.Equals(i.Value, identifier.Value, StringComparison.Ordinal));

    /// <summary>
    /// Records an identifier, superseding the one it replaces.
    /// </summary>
    /// <remarks>
    /// <b>Idempotent by design.</b> Re-submitting an identifier the vehicle already holds is not an
    /// error and must not open a second period — a dealer's nightly stock feed sends the same plate
    /// every night, and a version of this method that appended would produce a thousand rows a year
    /// per vehicle and break the partial unique index on the first duplicate.
    /// <para>
    /// A <i>different</i> value of the same type supersedes the old one, which is exactly the
    /// re-registration case: the previous plate is closed at <paramref name="now"/> and the new one
    /// opens. Both rows survive, so a document created last year still resolves the plate it was
    /// created with.
    /// </para>
    /// </remarks>
    public Result<VehicleIdentity> RecordIdentifier(
        VehicleIdentifier identifier,
        DateTimeOffset now,
        string? sourceCode = null,
        DateTimeOffset? validFrom = null)
    {
        var existing = ActiveIdentity(identifier.Type);

        if (existing is not null)
        {
            if (string.Equals(existing.Value, identifier.Value, StringComparison.Ordinal))
            {
                return existing;
            }

            // A VIN or chassis number that changes is not a re-registration — it is either a typo or
            // two different cars being merged, and both are worse than refusing. A plate that changes
            // is Tuesday.
            if (identifier.Type is VehicleIdentifierType.Vin or VehicleIdentifierType.Chassis)
            {
                return Error.Conflict(
                    "catalog.vehicle.permanent_identifier_conflict",
                    "This vehicle already holds a different chassis or VIN. A permanent identifier cannot be replaced.")
                    .With("identifierType", identifier.Type.ToWireName())
                    .With("existing", existing.Value)
                    .With("submitted", identifier.Value);
            }

            existing.Retire(now);
            Raise(new VehicleIdentityRetired(Id, existing.IdentifierType, existing.Value, now));
        }

        var opened = VehicleIdentity.Open(Id, identifier, validFrom ?? now, sourceCode);
        _identities.Add(opened);
        Raise(new VehicleIdentityRecorded(Id, identifier.Type, identifier.Value, now));

        return opened;
    }

    /// <summary>
    /// Retires an identifier this vehicle holds because it has provably moved to another vehicle.
    /// </summary>
    /// <remarks>
    /// Used only by the deduplication path, when a submitted VIN resolves here while the submitted
    /// plate is still live on a different vehicle. The RTO reissued the number; the other vehicle's
    /// claim on it ended whether or not anybody told us.
    /// </remarks>
    public void ReleaseIdentifier(VehicleIdentifier identifier, DateTimeOffset now)
    {
        var held = _identities.Find(i =>
            i.IsActive
            && i.IdentifierType == identifier.Type
            && string.Equals(i.Value, identifier.Value, StringComparison.Ordinal));

        if (held is null)
        {
            return;
        }

        held.Retire(now);
        Raise(new VehicleIdentityRetired(Id, identifier.Type, identifier.Value, now));
    }

    /// <summary>
    /// Fills in the taxonomy pointers as they become known.
    /// </summary>
    /// <remarks>
    /// Additive rather than replacing: a null argument means "still unknown", not "clear this".
    /// A VAHAN lookup that returns a make but no variant must not erase a variant the seller already
    /// picked, and that is the ordering these two sources actually arrive in.
    /// </remarks>
    public void Classify(Guid? makeId, Guid? modelId, Guid? variantId)
    {
        MakeId = makeId ?? MakeId;
        ModelId = modelId ?? ModelId;
        VariantId = variantId ?? VariantId;
    }

    /// <summary>Moves the vehicle to a different taxonomy category, which changes its attribute set.</summary>
    public void Recategorise(Guid categoryId) => CategoryId = categoryId;

    public Result SetProvenance(int? manufactureYear, int? manufactureMonth, DateOnly? firstRegisteredOn, string? provenanceCode)
    {
        if (manufactureMonth is { } month and (< 1 or > 12))
        {
            return Error.Validation(
                "catalog.vehicle.manufacture_month_invalid",
                "The month of manufacture must be between 1 and 12.")
                .With("manufactureMonth", month);
        }

        if (manufactureYear is { } year && firstRegisteredOn is { } registered && registered.Year < year)
        {
            // Registration before manufacture is always a data fault, and it silently makes a car
            // look a year older than it is — which is a real amount of money on a valuation.
            return Error.Validation(
                "catalog.vehicle.registered_before_manufacture",
                "A vehicle cannot be registered before it was manufactured.")
                .With("manufactureYear", year)
                .With("firstRegisteredOn", registered.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        }

        ManufactureYear = manufactureYear ?? ManufactureYear;
        ManufactureMonth = manufactureMonth ?? ManufactureMonth;
        FirstRegisteredOn = firstRegisteredOn ?? FirstRegisteredOn;
        ProvenanceCode = provenanceCode ?? ProvenanceCode;

        return Result.Success();
    }

    /// <summary>
    /// Replaces the denormalised attribute bag after the EAV rows have been written.
    /// </summary>
    /// <remarks>
    /// Takes the already-validated projection rather than raw input, so this method cannot be the
    /// place an unvalidated attribute enters the system — validation is
    /// <c>ITaxonomyRegistry.ValidateAttributesAsync</c>, and there is exactly one of it.
    /// </remarks>
    public void ProjectAttributes(JsonObject attributes, DateTimeOffset now)
    {
        AttributesJson = attributes.ToJsonString();
        Raise(new VehicleAttributesUpdated(Id, now));
    }

    public JsonObject Attributes()
        => string.IsNullOrWhiteSpace(AttributesJson)
            ? []
            : JsonNode.Parse(AttributesJson) as JsonObject ?? [];

    public VehicleSummary ToSummary()
    {
        var registration = ActiveIdentity(VehicleIdentifierType.Registration);

        // The plate is regrouped for display here rather than at the edge because the grouping is a
        // property of the number's format, not of the viewer — and a caller that regrouped it itself
        // would have to reimplement the Bharat-series rule.
        var parsed = registration is null
            ? null
            : RegistrationNumber.TryParse(registration.Value) is { IsSuccess: true } result
                ? result.Value.ToPlateDisplay()
                : registration.Value;

        return new VehicleSummary
        {
            VehicleId = Id,
            CategoryId = CategoryId,
            MakeId = MakeId,
            ModelId = ModelId,
            VariantId = VariantId,
            ManufactureYear = ManufactureYear,
            FirstRegisteredOn = FirstRegisteredOn,
            Registration = registration?.Value,
            RegistrationDisplay = parsed,
            ProvenanceCode = ProvenanceCode,
            Identifiers = [.. _identities.Select(i => i.ToView())],
        };
    }
}
