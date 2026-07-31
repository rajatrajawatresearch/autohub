using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations;

/// <summary>
/// The handful of PostgreSQL types the registries reach for, named once.
/// </summary>
/// <remarks>
/// These are technical constants — the prime directive's explicit exemption. Their value is that
/// <c>jsonb</c> versus <c>json</c> and <c>citext</c> versus <c>text</c> are decisions with real
/// consequences, and a decision repeated as a string literal in forty files is a decision that will
/// eventually be made differently in one of them.
/// </remarks>
internal static class ColumnTypes
{
    /// <summary>
    /// Parsed binary JSON. The only form a GIN index can be built over, which is what makes a rule
    /// document queryable instead of merely storable.
    /// </summary>
    public const string Jsonb = "jsonb";

    /// <summary>
    /// Case-insensitive text. Used for natural keys that humans type — a plate, a state code, a
    /// city slug — where <c>MH</c> and <c>mh</c> must be one row and not two.
    /// </summary>
    public const string CaseInsensitiveText = "citext";

    /// <summary>Unbounded text. For bodies, descriptions and translations, which have no sane cap.</summary>
    public const string Text = "text";

    /// <summary>An array of text. For short authored lists where a join table would be ceremony.</summary>
    public const string TextArray = "text[]";

    /// <summary>ISO 4217, always three characters.</summary>
    public const string CurrencyCode = "char(3)";
}

/// <summary>Shorthand for the column types above, so a configuration reads as intent.</summary>
internal static class PropertyBuilderExtensions
{
    public static PropertyBuilder<string> AsJsonb(this PropertyBuilder<string> builder)
        => builder.HasColumnType(ColumnTypes.Jsonb);

    public static PropertyBuilder<string?> AsNullableJsonb(this PropertyBuilder<string?> builder)
        => builder.HasColumnType(ColumnTypes.Jsonb);

    public static PropertyBuilder<string> AsCaseInsensitive(this PropertyBuilder<string> builder)
        => builder.HasColumnType(ColumnTypes.CaseInsensitiveText);

    public static PropertyBuilder<string?> AsNullableCaseInsensitive(this PropertyBuilder<string?> builder)
        => builder.HasColumnType(ColumnTypes.CaseInsensitiveText);

    public static PropertyBuilder<string> AsText(this PropertyBuilder<string> builder)
        => builder.HasColumnType(ColumnTypes.Text);

    public static PropertyBuilder<string?> AsNullableText(this PropertyBuilder<string?> builder)
        => builder.HasColumnType(ColumnTypes.Text);
}
