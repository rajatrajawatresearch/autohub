using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Taxonomy;

/// <summary>
/// The published spelling of the taxonomy enums.
/// </summary>
/// <remarks>
/// The API contract fixes these strings — <c>multi_enum</c>, <c>facetStyle: "chips"</c> — and three
/// clients branch on them. Deriving them from the C# member names with a naming policy would mean a
/// harmless-looking rename silently changes the wire format for every web and mobile build already
/// in the field, and nothing in the compiler would notice.
/// <para>
/// So the mapping is written down. Adding a member to either enum without adding it here is a
/// compile error, which is the only kind of reminder that works.
/// </para>
/// </remarks>
public static class TaxonomyWireNames
{
    public static string ToWireName(this AttributeDataType dataType) => dataType switch
    {
        AttributeDataType.String => "string",
        AttributeDataType.Number => "number",
        AttributeDataType.Integer => "integer",
        AttributeDataType.Boolean => "boolean",
        AttributeDataType.Enum => "enum",
        AttributeDataType.MultiEnum => "multi_enum",
        AttributeDataType.Date => "date",
        AttributeDataType.Money => "money",
        _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, "Unmapped attribute data type."),
    };

    public static string ToWireName(this FacetStyle facetStyle) => facetStyle switch
    {
        FacetStyle.Chips => "chips",
        FacetStyle.List => "list",
        FacetStyle.Range => "range",
        FacetStyle.Toggle => "toggle",
        _ => throw new ArgumentOutOfRangeException(nameof(facetStyle), facetStyle, "Unmapped facet style."),
    };

    /// <summary>Parses an inbound wire value — admin writes and seed files both arrive as strings.</summary>
    public static Result<AttributeDataType> ParseDataType(string? wireName) => wireName switch
    {
        "string" => AttributeDataType.String,
        "number" => AttributeDataType.Number,
        "integer" => AttributeDataType.Integer,
        "boolean" => AttributeDataType.Boolean,
        "enum" => AttributeDataType.Enum,
        "multi_enum" => AttributeDataType.MultiEnum,
        "date" => AttributeDataType.Date,
        "money" => AttributeDataType.Money,
        _ => Error.Validation(
            "taxonomy.attribute.data_type_unknown",
            $"'{wireName}' is not a supported attribute data type.")
            .With("dataType", wireName),
    };

    public static Result<FacetStyle> ParseFacetStyle(string? wireName) => wireName switch
    {
        "chips" => FacetStyle.Chips,
        "list" => FacetStyle.List,
        "range" => FacetStyle.Range,
        "toggle" => FacetStyle.Toggle,
        _ => Error.Validation(
            "taxonomy.attribute.facet_style_unknown",
            $"'{wireName}' is not a supported facet style.")
            .With("facetStyle", wireName),
    };
}
