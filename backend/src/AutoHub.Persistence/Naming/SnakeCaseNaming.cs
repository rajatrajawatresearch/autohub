using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AutoHub.Persistence.Naming;

/// <summary>
/// Rewrites every identifier EF generates into <c>snake_case</c>.
/// </summary>
/// <remarks>
/// The north star fixes the database convention: <c>snake_case</c> columns, plural tables. EF's
/// default is <c>PascalCase</c>, which in PostgreSQL means every hand-written query, every psql
/// session and every BI tool has to double-quote every identifier — and the first person who forgets
/// gets <c>column "createdat" does not exist</c>, because unquoted identifiers fold to lowercase.
/// <para>
/// Doing it as a pass over the built model rather than as four hundred <c>HasColumnName</c> calls is
/// not laziness: an explicit name per column is four hundred chances to typo one, and the typo
/// surfaces as a migration that quietly adds a second column beside the one you meant.
/// </para>
/// <para>
/// The rewrite is idempotent — <c>snake_case</c> of a <c>snake_case</c> string is itself — so
/// configurations are free to name a column explicitly where the derived name would be wrong, and
/// this pass leaves those alone.
/// </para>
/// </remarks>
internal static class SnakeCaseNaming
{
    public static void ApplyTo(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            ApplyToTable(entity);
            ApplyToProperties(entity);
            ApplyToKeys(entity);
            ApplyToForeignKeys(entity);
            ApplyToIndexes(entity);
        }
    }

    private static void ApplyToTable(IMutableEntityType entity)
    {
        // Owned types share their owner's table; renaming here would split them out into a table
        // nobody configured. GetTableName() already reports the owner's name for those.
        if (entity.GetTableName() is { } table)
        {
            entity.SetTableName(ToSnakeCase(table));
        }
    }

    private static void ApplyToProperties(IMutableEntityType entity)
    {
        var storeObject = StoreObjectIdentifier.Create(entity, StoreObjectType.Table);

        foreach (var property in entity.GetProperties())
        {
            // xmin is a PostgreSQL system column. It is already lowercase and must stay verbatim —
            // rewriting it would point the concurrency token at a column that does not exist.
            var name = storeObject is { } target
                ? property.GetColumnName(target)
                : property.GetColumnName();

            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            property.SetColumnName(ToSnakeCase(name));
        }
    }

    private static void ApplyToKeys(IMutableEntityType entity)
    {
        foreach (var key in entity.GetKeys())
        {
            if (key.GetName() is { } name)
            {
                key.SetName(ToSnakeCase(name));
            }
        }
    }

    private static void ApplyToForeignKeys(IMutableEntityType entity)
    {
        foreach (var foreignKey in entity.GetForeignKeys())
        {
            if (foreignKey.GetConstraintName() is { } name)
            {
                foreignKey.SetConstraintName(ToSnakeCase(name));
            }
        }
    }

    private static void ApplyToIndexes(IMutableEntityType entity)
    {
        foreach (var index in entity.GetIndexes())
        {
            if (index.GetDatabaseName() is { } name)
            {
                index.SetDatabaseName(ToSnakeCase(name));
            }
        }
    }

    /// <summary>
    /// <c>VehicleCategoryId</c> becomes <c>vehicle_category_id</c>; <c>I18nResource</c> becomes
    /// <c>i18n_resource</c>; an already-snake string is returned unchanged.
    /// </summary>
    /// <remarks>
    /// The acronym rule matters here because the model is full of them — <c>RtoOffice</c>,
    /// <c>GstStateCode</c>, <c>I18nLocale</c>. A naive "underscore before every capital" turns
    /// <c>RTOOffice</c> into <c>r_t_o_office</c>; the boundary that reads correctly is the last
    /// capital of a run, which is where the next word actually starts.
    /// </remarks>
    internal static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var builder = new StringBuilder(name.Length + 8);

        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];

            if (current == '_')
            {
                builder.Append('_');
                continue;
            }

            if (char.IsUpper(current) && builder.Length > 0 && builder[^1] != '_' && NeedsSeparator(name, i))
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(current));
        }

        return builder.ToString();
    }

    private static bool NeedsSeparator(string name, int index)
    {
        var previous = name[index - 1];

        // lower|digit -> Upper is always a word boundary: "categoryId" -> "category_id".
        if (!char.IsUpper(previous))
        {
            return true;
        }

        // Upper -> Upper is only a boundary when the *next* character is lower, which makes this
        // capital the start of a word rather than another letter of an acronym: "RTOOffice".
        return index + 1 < name.Length && char.IsLower(name[index + 1]);
    }
}
