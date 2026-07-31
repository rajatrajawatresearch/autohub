using System.Text.Json.Nodes;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Taxonomy;

/// <summary>
/// One attribute value on one entity. The EAV row.
/// </summary>
/// <remarks>
/// <b>Why typed columns and not a single JSONB blob.</b> The obvious design is one
/// <c>attributes jsonb</c> column per vehicle, and it is genuinely good for <i>reading</i> —
/// containment (<c>attributes @&gt; '{"fuel_type":"cng"}'</c>) is fast under a GIN index and a new
/// attribute needs no DDL. What it is bad at is the query the marketplace actually runs on every
/// single search: <c>kms_driven BETWEEN 0 AND 40000</c> and <c>price &lt;= 600000</c>.
/// <para>
/// A GIN index over JSONB indexes <i>keys and values as tokens</i>. It answers "which rows contain
/// this exact key/value pair" by intersecting posting lists, which is why containment is fast. It
/// cannot answer "which rows have a number in this range", because the index has no ordering — the
/// planner degrades to fetching every row whose key matches and re-checking the value, and on a
/// corpus of a few hundred thousand listings that is a sequential scan wearing a hat. Casting inside
/// the predicate (<c>(attributes-&gt;&gt;'kms')::int &lt; 40000</c>) makes it worse: the expression is
/// not indexable at all without a matching expression index per attribute, which is the DDL-per-
/// attribute cost we were avoiding.
/// </para>
/// <para>
/// A <c>numeric</c> column with a plain B-tree index answers a range in log time, and
/// <c>(attribute_definition_id, value_number)</c> as a composite makes it one index seek per facet.
/// So: typed columns are the <b>truth</b> and are what range queries run against; a denormalised
/// JSONB projection on the owning row (built from these rows in the same transaction) is what
/// containment queries and API responses read; OpenSearch is what faceted search reads. Three
/// representations, one writer, and each shaped for the question it is asked.
/// </para>
/// <para>
/// <see cref="ValueJson"/> is the fallback for the one type that has no scalar column —
/// <see cref="AttributeDataType.MultiEnum"/> — and for anything a future data type needs. It is
/// deliberately the exception, not the rule.
/// </para>
/// <para>
/// This is an <see cref="Entity"/> and not an aggregate root: the rows belong to whatever aggregate
/// owns <see cref="EntityId"/>. A vehicle and its twenty attribute values are saved in one
/// transaction because they are one thing, and the platform only owns their <i>shape</i>.
/// </para>
/// </remarks>
public sealed class EntityAttributeValue : Entity, IAuditable
{
    private EntityAttributeValue(Guid id, string entityType, Guid entityId, Guid attributeDefinitionId)
        : base(id)
    {
        EntityType = entityType;
        EntityId = entityId;
        AttributeDefinitionId = attributeDefinitionId;
    }

    private EntityAttributeValue() { }

    /// <summary>
    /// The owning aggregate's type, e.g. <c>catalog.vehicle</c>, <c>listings.listing</c>.
    /// </summary>
    /// <remarks>
    /// A string rather than a foreign key because the whole point is that this table serves entities
    /// in modules the platform must not reference. It is half of the composite index every read uses.
    /// </remarks>
    public string EntityType { get; private set; } = string.Empty;

    public Guid EntityId { get; private set; }

    public Guid AttributeDefinitionId { get; private set; }

    /// <summary>String and enum values. Indexed for equality, not for prefix search.</summary>
    public string? ValueText { get; private set; }

    /// <summary>Number and integer values. The column range facets are actually indexed on.</summary>
    public decimal? ValueNumber { get; private set; }

    public bool? ValueBool { get; private set; }

    public DateOnly? ValueDate { get; private set; }

    /// <summary>
    /// Money in minor units, kept apart from <see cref="ValueNumber"/> on purpose.
    /// </summary>
    /// <remarks>
    /// Price is the single hottest range facet in the product, so it wants a <c>bigint</c> column and
    /// a B-tree index, not a <c>numeric</c> shared with mileage. And storing paise in a column that
    /// also holds "5.2 metres" is precisely how somebody eventually reads a price as rupees.
    /// </remarks>
    public long? ValueMoneyMinorUnits { get; private set; }

    /// <summary>ISO 4217 code paired with <see cref="ValueMoneyMinorUnits"/>. Never inferred.</summary>
    public string? ValueCurrency { get; private set; }

    /// <summary>Multi-enum selections, and the escape hatch for types with no scalar column.</summary>
    public string? ValueJson { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static EntityAttributeValue Create(
        string entityType,
        Guid entityId,
        Guid attributeDefinitionId,
        TypedAttributeValue value)
    {
        var row = new EntityAttributeValue(UuidV7.New(), entityType, entityId, attributeDefinitionId);
        row.Replace(value);
        return row;
    }

    /// <summary>
    /// Writes the value into the column its type belongs in, clearing every other column first.
    /// </summary>
    /// <remarks>
    /// Clearing is not tidiness. A row that changed from <c>Integer</c> to <c>Enum</c> after an
    /// operator retyped an attribute would otherwise keep a stale <c>value_number</c>, and the next
    /// range facet built over that column would quietly count it.
    /// </remarks>
    public void Replace(TypedAttributeValue value)
    {
        ValueText = null;
        ValueNumber = null;
        ValueBool = null;
        ValueDate = null;
        ValueMoneyMinorUnits = null;
        ValueCurrency = null;
        ValueJson = null;

        switch (value.DataType)
        {
            case AttributeDataType.String:
            case AttributeDataType.Enum:
                ValueText = value.Text;
                break;

            case AttributeDataType.Number:
            case AttributeDataType.Integer:
                ValueNumber = value.Number;
                break;

            case AttributeDataType.Boolean:
                ValueBool = value.Flag;
                break;

            case AttributeDataType.Date:
                ValueDate = value.Date;
                break;

            case AttributeDataType.Money:
                ValueMoneyMinorUnits = value.Amount?.MinorUnits;
                ValueCurrency = value.Amount?.Currency.Code;
                break;

            case AttributeDataType.MultiEnum:
                ValueJson = value.ToJson()?.ToJsonString();
                break;

            default:
                ValueJson = value.ToJson()?.ToJsonString();
                break;
        }
    }

    /// <summary>
    /// Reads the row back as the type its definition currently says it is.
    /// </summary>
    /// <remarks>
    /// Returns a <c>Result</c> rather than throwing because the mismatch it detects is real and not
    /// rare: an operator retypes <c>owners</c> from string to integer, and every row written before
    /// that moment now disagrees with its definition. Surfacing it as a failure lets the caller skip
    /// the row and lets an alert count them, instead of a cast exception taking down a listing page.
    /// </remarks>
    public Result<TypedAttributeValue> Read(AttributeDefinition definition)
    {
        switch (definition.DataType)
        {
            case AttributeDataType.String when ValueText is not null:
                return TypedAttributeValue.OfText(ValueText);

            case AttributeDataType.Enum when ValueText is not null:
                return TypedAttributeValue.OfEnum(ValueText);

            case AttributeDataType.Number when ValueNumber is not null:
                return TypedAttributeValue.OfNumber(ValueNumber.Value);

            case AttributeDataType.Integer when ValueNumber is not null:
                return TypedAttributeValue.OfInteger((long)ValueNumber.Value);

            case AttributeDataType.Boolean when ValueBool is not null:
                return TypedAttributeValue.OfBoolean(ValueBool.Value);

            case AttributeDataType.Date when ValueDate is not null:
                return TypedAttributeValue.OfDate(ValueDate.Value);

            case AttributeDataType.Money when ValueMoneyMinorUnits is not null && ValueCurrency is not null:
                return ReadMoney(ValueMoneyMinorUnits.Value, ValueCurrency);

            case AttributeDataType.MultiEnum when ValueJson is not null:
                return ReadChoices(ValueJson);

            default:
                return Error.Unexpected(
                    "taxonomy.attribute_value.type_mismatch",
                    $"Stored value for '{definition.Code}' does not match its declared type.")
                    .With("attributeCode", definition.Code)
                    .With("dataType", definition.DataType.ToWireName());
        }
    }

    /// <summary>
    /// Rebuilds a stored amount.
    /// </summary>
    /// <remarks>
    /// <c>Currency.FromCode</c> throws for a code it does not know, which is right at a compile-time
    /// call site and wrong here — the code came out of a row that was written when the platform
    /// supported it. Converting the throw into the failure it actually is keeps one retired currency
    /// from taking down a listing page.
    /// </remarks>
    private static Result<TypedAttributeValue> ReadMoney(long minorUnits, string currencyCode)
    {
        Currency currency;
        try
        {
            currency = Currency.FromCode(currencyCode);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Error.Unexpected(
                "taxonomy.attribute_value.currency_unsupported",
                $"A stored amount uses currency '{currencyCode}', which is no longer supported.")
                .With("currency", currencyCode);
        }

        return TypedAttributeValue.OfMoney(Money.FromMinorUnits(minorUnits, currency));
    }

    private static Result<TypedAttributeValue> ReadChoices(string json)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (System.Text.Json.JsonException)
        {
            return Error.Unexpected(
                "taxonomy.attribute_value.json_unreadable",
                "A stored multi-select value is not valid JSON.");
        }

        if (node is not JsonArray array)
        {
            return Error.Unexpected(
                "taxonomy.attribute_value.json_unreadable",
                "A stored multi-select value is not a JSON array.");
        }

        var choices = new List<string>(array.Count);
        foreach (var element in array)
        {
            if (element is JsonValue value && value.TryGetValue<string>(out var text))
            {
                choices.Add(text);
            }
        }

        return TypedAttributeValue.OfChoices(choices);
    }
}
