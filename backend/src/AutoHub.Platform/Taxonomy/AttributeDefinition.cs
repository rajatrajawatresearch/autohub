using System.Diagnostics.CodeAnalysis;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Taxonomy;

/// <summary>
/// One property a vehicle can have — <c>fuel_type</c>, <c>kms_driven</c>, <c>boom_length</c>.
/// </summary>
/// <remarks>
/// This is the row that makes the north star's acceptance test pass. Adding <c>boom_length</c> to
/// Construction Equipment inserts one definition and one <see cref="CategoryAttribute"/>, and from
/// that moment it is a field on the sell form, a row in the spec table, a facet on search, and a
/// validated value on bulk upload — with no deployment, because every one of those surfaces is
/// generated from this row rather than written against it.
/// <para>
/// The definition carries three separable concerns on purpose, and each is used by a different
/// consumer: <b>type and bounds</b> feed <see cref="AttributeValueValidator"/>,
/// <b><see cref="IsSearchable"/> and <see cref="FacetStyle"/></b> feed the search indexer and the
/// facet rail, and <b><see cref="DisplayGroup"/> and <see cref="SortOrder"/></b> feed the form and
/// the spec sheet. Splitting them into three tables would mean three writes and three chances to
/// forget one.
/// </para>
/// </remarks>
public sealed class AttributeDefinition : AggregateRoot, IAuditable, ISoftDeletable
{
    private readonly List<AttributeOption> _options = [];

    private AttributeDefinition(Guid id, string code, string label, AttributeDataType dataType)
        : base(id)
    {
        Code = code;
        Label = label;
        DataType = dataType;
        FacetStyle = FacetStyle.List;
        IsActive = true;
    }

    private AttributeDefinition() { }

    /// <summary>
    /// Stable machine code, e.g. <c>kms_driven</c>. It becomes a JSON key on the attribute bag, a
    /// query-string parameter (<c>attr.kms_driven=0..40000</c>) and an OpenSearch field name, so it
    /// is a contract with three clients the moment it ships and is never renamed.
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Default English label. Real user-facing text comes from the i18n registry.</summary>
    public string Label { get; private set; } = string.Empty;

    public AttributeDataType DataType { get; private set; }

    /// <summary>
    /// Unit key, e.g. <c>km</c>, <c>bhp</c>, <c>%</c>. Rendered next to the value, never inside it —
    /// storing "38,400 km" as a string is how a range facet becomes impossible.
    /// </summary>
    public string? Unit { get; private set; }

    /// <summary>
    /// The default. A category can relax or tighten it through
    /// <see cref="CategoryAttribute.IsRequiredOverride"/> — registration number is mandatory when
    /// selling and meaningless when listing a vehicle that has not been registered yet.
    /// </summary>
    public bool IsRequired { get; private set; }

    /// <summary>Whether the search indexer emits this attribute as a queryable field and a facet.</summary>
    public bool IsSearchable { get; private set; }

    public FacetStyle FacetStyle { get; private set; }

    /// <summary>
    /// Groups fields on the form and rows on the spec sheet — <c>engine</c>, <c>usage</c>,
    /// <c>ownership</c>. A key, not a heading: the heading is a localised string.
    /// </summary>
    public string? DisplayGroup { get; private set; }

    public int SortOrder { get; private set; }

    /// <summary>
    /// Inclusive lower bound for numeric types.
    /// </summary>
    /// <remarks>
    /// For <see cref="AttributeDataType.Money"/> these bounds are in <b>minor units</b>, like every
    /// other amount in the system. A bound authored in rupees against a value stored in paise is the
    /// 100× bug the <c>Money</c> type exists to prevent, so the unit is fixed here and documented
    /// rather than inferred. For <see cref="AttributeDataType.MultiEnum"/> they bound the
    /// <i>number of selections</i>, which is the only numeric quantity a multi-select has.
    /// </remarks>
    public decimal? MinNumber { get; private set; }

    /// <summary>Inclusive upper bound. See <see cref="MinNumber"/> for units.</summary>
    public decimal? MaxNumber { get; private set; }

    /// <summary>
    /// Granularity. A slider that reports 38,417 km implies a precision no odometer reading has;
    /// a step of 500 keeps the input honest and the facet buckets stable.
    /// </summary>
    public decimal? Step { get; private set; }

    public int? MinLength { get; private set; }

    public int? MaxLength { get; private set; }

    /// <summary>
    /// Regular expression for string values, authored in the admin panel.
    /// </summary>
    /// <remarks>
    /// Treated as untrusted input by <see cref="AttributeValueValidator"/> and always matched with a
    /// timeout. A pattern with catastrophic backtracking is trivially authorable by accident and
    /// would otherwise pin a request thread for the lifetime of the process.
    /// </remarks>
    public string? Pattern { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyList<AttributeOption> Options => _options;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    /// <summary>Whether this type is meaningless without a closed option list.</summary>
    public bool RequiresOptions
        => DataType is AttributeDataType.Enum or AttributeDataType.MultiEnum;

    public static Result<AttributeDefinition> Create(string code, string label, AttributeDataType dataType)
    {
        var validCode = TaxonomyCode.Validate(code, "taxonomy.attribute.code_invalid");
        if (validCode.IsFailure)
        {
            return Result.Failure<AttributeDefinition>(validCode.Error);
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            return Error.Validation("taxonomy.attribute.label_required", "An attribute needs a label.");
        }

        return new AttributeDefinition(UuidV7.New(), validCode.Value, label.Trim(), dataType);
    }

    public void SetPresentation(
        string? unit,
        string? displayGroup,
        int sortOrder,
        bool isRequired,
        bool isSearchable,
        FacetStyle facetStyle)
    {
        Unit = unit;
        DisplayGroup = displayGroup;
        SortOrder = sortOrder;
        IsRequired = isRequired;
        IsSearchable = isSearchable;
        FacetStyle = facetStyle;
    }

    public Result SetNumericBounds(decimal? min, decimal? max, decimal? step)
    {
        if (min is { } lower && max is { } upper && upper < lower)
        {
            return Error.Validation(
                "taxonomy.attribute.bounds_inverted",
                "The maximum cannot be below the minimum.")
                .With("min", lower)
                .With("max", upper);
        }

        if (step is { } granularity && granularity <= 0)
        {
            return Error.Validation("taxonomy.attribute.step_invalid", "The step must be greater than zero.");
        }

        MinNumber = min;
        MaxNumber = max;
        Step = step;
        return Result.Success();
    }

    public Result SetTextBounds(int? minLength, int? maxLength, string? pattern)
    {
        if (minLength is { } lower && lower < 0)
        {
            return Error.Validation("taxonomy.attribute.length_negative", "A minimum length cannot be negative.");
        }

        if (minLength is { } from && maxLength is { } to && to < from)
        {
            return Error.Validation(
                "taxonomy.attribute.length_inverted",
                "The maximum length cannot be below the minimum length.");
        }

        MinLength = minLength;
        MaxLength = maxLength;
        Pattern = string.IsNullOrWhiteSpace(pattern) ? null : pattern;
        return Result.Success();
    }

    public AttributeOption? FindOption(string value)
        => _options.Find(o => string.Equals(o.Value, value, StringComparison.Ordinal));

    public Result AddOption(string value, string label, int sortOrder = 0)
    {
        if (!RequiresOptions)
        {
            return Error.Validation(
                "taxonomy.attribute.options_not_supported",
                $"An attribute of type '{DataType}' does not take options.");
        }

        var validValue = TaxonomyCode.Validate(value, "taxonomy.attribute_option.value_invalid");
        if (validValue.IsFailure)
        {
            return Result.Failure(validValue.Error);
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            return Error.Validation("taxonomy.attribute_option.label_required", "An option needs a label.");
        }

        if (FindOption(validValue.Value) is not null)
        {
            return Error.Conflict(
                "taxonomy.attribute_option.duplicate",
                $"Option '{validValue.Value}' already exists on '{Code}'.");
        }

        _options.Add(new AttributeOption(Id, validValue.Value, label.Trim(), sortOrder));
        return Result.Success();
    }

    /// <summary>
    /// Proves the definition can actually be rendered and validated before anything depends on it.
    /// </summary>
    /// <remarks>
    /// Each check is a failure mode seen in the wild: an enum with no options is a dropdown a seller
    /// cannot complete and a facet with nothing in it; a range facet on a string is a filter rail
    /// that renders a slider over text; a searchable attribute with no facet style makes the search
    /// page decide presentation for itself, which is exactly the hardcoding this registry removes.
    /// </remarks>
    public Result<IReadOnlyList<string>> Validate()
    {
        var problems = new List<string>();

        if (RequiresOptions && _options.Count == 0)
        {
            problems.Add($"'{Code}' is an option type but has no options.");
        }

        if (!RequiresOptions && _options.Count > 0)
        {
            problems.Add($"'{Code}' is not an option type but carries {_options.Count} options.");
        }

        if (FacetStyle == FacetStyle.Range && DataType is not (AttributeDataType.Number
            or AttributeDataType.Integer or AttributeDataType.Money or AttributeDataType.Date))
        {
            problems.Add($"'{Code}' asks for a range facet but its type cannot be ordered.");
        }

        if (FacetStyle == FacetStyle.Toggle && DataType != AttributeDataType.Boolean)
        {
            problems.Add($"'{Code}' asks for a toggle facet but is not a boolean.");
        }

        if (FacetStyle is FacetStyle.Chips or FacetStyle.List && !RequiresOptions)
        {
            problems.Add($"'{Code}' asks for a chip or list facet but has no closed option set.");
        }

        if (MinNumber is { } min && MaxNumber is { } max && max < min)
        {
            problems.Add($"'{Code}' has a maximum below its minimum.");
        }

        return problems.Count == 0
            ? Result.Success<IReadOnlyList<string>>([])
            : Error.Validation("taxonomy.attribute.invalid", $"Attribute '{Code}' is not usable.")
                .With("problems", problems);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}

/// <summary>
/// A permitted value of an enum or multi-enum attribute.
/// </summary>
/// <remarks>
/// The option list is the reason <c>new[] { "Petrol", "Diesel" }</c> never appears in code. Adding
/// hydrogen is an insert; the sell form, the search facet, the spec strip and the validator all pick
/// it up on the next registry read.
/// </remarks>
public sealed class AttributeOption
{
    internal AttributeOption(Guid attributeDefinitionId, string value, string label, int sortOrder)
    {
        AttributeDefinitionId = attributeDefinitionId;
        Value = value;
        Label = label;
        SortOrder = sortOrder;
        IsActive = true;
    }

    private AttributeOption() { }

    public Guid Id { get; private set; } = UuidV7.New();

    public Guid AttributeDefinitionId { get; private set; }

    /// <summary>Stored and indexed, e.g. <c>cng</c>. Never shown to a user.</summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>Default English label, e.g. <c>CNG</c>. Localised at the edge.</summary>
    public string Label { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    /// <summary>
    /// Retired options stay readable. A 2014 listing with <c>bs3</c> emissions must still render its
    /// label years after the option stopped being offered on the sell form.
    /// </summary>
    public bool IsActive { get; private set; }

    public void Relabel(string label) => Label = label;

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}

/// <summary>
/// The value types an attribute can hold.
/// </summary>
/// <remarks>
/// This <i>is</i> a closed set in code, and deliberately so: each member has a validator branch, a
/// storage column and a search mapping behind it, so adding one is a code change by definition.
/// The prime directive forbids hardcoding <i>business</i> vocabulary — fuel types, statuses, cities.
/// A type system is not business vocabulary.
/// </remarks>
[SuppressMessage(
    "Naming",
    "CA1720:Identifier contains type name",
    Justification =
        "The member names are the published wire values — the API contract sends dataType: \"string\" " +
        "and \"integer\", and three clients already branch on them. Renaming these to Text and Whole " +
        "to satisfy a rule about parameter naming would put a translation layer between the enum and " +
        "the contract, which is exactly where a typo goes unnoticed. Suppressed here only, so the " +
        "rule still catches a genuine `object obj` elsewhere.")]
public enum AttributeDataType
{
    /// <summary>Free text. Bounded by length and pattern.</summary>
    String = 1,

    /// <summary>Fractional number — ground clearance, mileage, power.</summary>
    Number = 2,

    /// <summary>Whole number — kms, owners, seats.</summary>
    Integer = 3,

    Boolean = 4,

    /// <summary>Exactly one value from <see cref="AttributeDefinition.Options"/>.</summary>
    Enum = 5,

    /// <summary>Any number of values from <see cref="AttributeDefinition.Options"/>.</summary>
    MultiEnum = 6,

    /// <summary>A calendar date. No time — insurance expiry is a day, not an instant.</summary>
    Date = 7,

    /// <summary>An amount in minor units, paired with a currency. Never a bare number.</summary>
    Money = 8,
}

/// <summary>
/// How the search rail renders a facet.
/// </summary>
/// <remarks>
/// Presentation lives in the registry, not the front end, because the front end must be able to
/// render a facet for an attribute that did not exist when it was built. The server sends a style;
/// the design system maps that style to a component. It never sends a colour or a component name.
/// </remarks>
public enum FacetStyle
{
    /// <summary>Wrapped pills. For short, high-traffic option sets — fuel, transmission.</summary>
    Chips = 1,

    /// <summary>A scrolling checkbox list with counts. For long option sets — make, colour.</summary>
    List = 2,

    /// <summary>Two-handled slider. For ordered types — kms, price, year.</summary>
    Range = 3,

    /// <summary>A single switch. For booleans — "accident free", "single owner".</summary>
    Toggle = 4,
}
