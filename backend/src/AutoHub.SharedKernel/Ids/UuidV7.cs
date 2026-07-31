using System.Buffers.Binary;
using System.Security.Cryptography;

namespace AutoHub.SharedKernel.Ids;

/// <summary>
/// Time-sortable UUID v7 (RFC 9562) generation.
/// </summary>
/// <remarks>
/// Random v4 keys scatter inserts across the whole B-tree; at a few hundred million bids and ledger
/// entries that turns every write into a random page fetch and every index into bloat. v7 puts a
/// millisecond timestamp in the high 48 bits, so inserts land at the right edge of the index the way
/// a sequence would — while staying globally unique, so we can mint ids on the API node without a
/// database round trip and without leaking row counts to the public.
/// <para>
/// The database's <c>uuid_generate_v7()</c> produces the identical layout, so ids minted in SQL and
/// ids minted here interleave correctly.
/// </para>
/// </remarks>
public static class UuidV7
{
    public static Guid New() => New(DateTimeOffset.UtcNow);

    public static Guid New(DateTimeOffset timestamp)
    {
        Span<byte> bytes = stackalloc byte[16];

        // 48-bit big-endian Unix milliseconds
        var unixMs = timestamp.ToUnixTimeMilliseconds();
        Span<byte> ms = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(ms, unixMs);
        ms[2..8].CopyTo(bytes);

        RandomNumberGenerator.Fill(bytes[6..]);

        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x70); // version 7
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80); // RFC 4122 variant

        return CreateGuidFromBigEndian(bytes);
    }

    /// <summary>Recovers the creation instant. Useful for partition routing and for debugging.</summary>
    public static DateTimeOffset? GetTimestamp(Guid id)
    {
        Span<byte> bytes = stackalloc byte[16];
        WriteBigEndian(id, bytes);

        if ((bytes[6] & 0xF0) != 0x70)
        {
            return null;
        }

        Span<byte> ms = stackalloc byte[8];
        bytes[..6].CopyTo(ms[2..]);
        return DateTimeOffset.FromUnixTimeMilliseconds(BinaryPrimitives.ReadInt64BigEndian(ms));
    }

    /// <summary>
    /// The lowest UUID v7 for an instant. Lets a time-range scan use the primary key index instead
    /// of a separate index on <c>created_at</c>.
    /// </summary>
    public static Guid LowerBound(DateTimeOffset timestamp)
    {
        Span<byte> bytes = stackalloc byte[16];
        Span<byte> ms = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(ms, timestamp.ToUnixTimeMilliseconds());
        ms[2..8].CopyTo(bytes);
        bytes[6] = 0x70;
        bytes[8] = 0x80;
        return CreateGuidFromBigEndian(bytes);
    }

    // Guid's in-memory layout is little-endian for its first three groups. Npgsql writes the
    // big-endian RFC byte order to PostgreSQL, so we build from big-endian bytes to keep the
    // database's textual sort order identical to chronological order.
    private static Guid CreateGuidFromBigEndian(ReadOnlySpan<byte> bigEndian)
        => new(bigEndian, bigEndian: true);

    private static void WriteBigEndian(Guid id, Span<byte> destination)
        => id.TryWriteBytes(destination, bigEndian: true, out _);
}
