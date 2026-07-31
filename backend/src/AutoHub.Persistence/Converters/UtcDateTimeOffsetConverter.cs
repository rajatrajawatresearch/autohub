using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AutoHub.Persistence.Converters;

/// <summary>
/// Normalises every instant to UTC on the way into a <c>timestamptz</c> column.
/// </summary>
/// <remarks>
/// PostgreSQL's <c>timestamptz</c> does not store an offset — it stores an instant, normalising to
/// UTC on write. Npgsql enforces that by refusing a <see cref="DateTimeOffset"/> whose offset is not
/// zero, and it is right to: silently discarding <c>+05:30</c> would mean the value read back is not
/// the value written, and the first place that surfaces is an auction that closes five and a half
/// hours late.
/// <para>
/// Refusing is correct but hostile at the boundary, because an IST instant is a perfectly valid
/// thing for a caller to hold — <c>IClock.TodayInIst</c> exists precisely because the business runs
/// on IST. So the conversion happens here, once, for every <see cref="DateTimeOffset"/> in the
/// model, rather than as a <c>.ToUniversalTime()</c> that forty call sites have to remember.
/// </para>
/// <para>
/// Nothing is lost. The offset a caller supplied is not information the domain ever needs back —
/// India has one time zone, display is an edge concern, and an instant is an instant.
/// </para>
/// </remarks>
public sealed class UtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTimeOffset>
{
    public UtcDateTimeOffsetConverter()
        : base(
            value => value.ToUniversalTime(),
            value => value.ToUniversalTime())
    {
    }

    /// <summary>The column type every instant uses. Stated once so none of them drifts.</summary>
    public const string ColumnType = "timestamptz";
}
