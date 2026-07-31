using AutoHub.SharedKernel.Vehicles;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AutoHub.Persistence.Converters;

/// <summary>
/// A registration number stored as its canonical form in a <c>citext</c> column.
/// </summary>
/// <remarks>
/// <b>Why citext and not text.</b> The value object already canonicalises to uppercase without
/// separators, so every row this converter writes is uppercase and a plain <c>text</c> unique index
/// would be correct. <c>citext</c> is for the rows nobody writes through the domain — a bulk import,
/// a data-repair script, a partner feed loaded with <c>COPY</c>. Those bypass the value object, and
/// the failure they cause is not a crash but a duplicate: <c>mh12ab1234</c> and <c>MH12AB1234</c>
/// coexisting as two vehicles, which surfaces months later as two listings for one car and a
/// valuation that averages them.
/// <para>
/// Case-insensitive comparison in the column itself makes the constraint true regardless of who
/// wrote the row, which is the only version of a uniqueness rule worth having.
/// </para>
/// <para>
/// Reading throws on a value the parser refuses, and that is deliberate. A malformed registration in
/// the database is not a user error to be reported — it is corruption, and the loud failure names
/// the row. Modules that must tolerate it read the raw column instead.
/// </para>
/// </remarks>
public sealed class RegistrationNumberConverter : ValueConverter<RegistrationNumber, string>
{
    public RegistrationNumberConverter()
        : base(
            registration => registration.Canonical,
            canonical => Parse(canonical))
    {
    }

    public static RegistrationNumberConverter Instance { get; } = new();

    /// <summary>The column type every registration column uses. Stated once so none of them drifts.</summary>
    public const string ColumnType = "citext";

    private static RegistrationNumber Parse(string canonical)
    {
        var parsed = RegistrationNumber.TryParse(canonical);

        return parsed.IsSuccess
            ? parsed.Value
            : throw new InvalidOperationException(
                $"Stored registration number '{canonical}' cannot be parsed. The row is corrupt.");
    }
}
