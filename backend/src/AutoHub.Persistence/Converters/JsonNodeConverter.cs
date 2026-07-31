using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AutoHub.Persistence.Converters;

/// <summary>
/// A <see cref="JsonNode"/> document stored in a <c>jsonb</c> column.
/// </summary>
/// <remarks>
/// <b>Why jsonb and not json or text.</b> <c>json</c> keeps the source text verbatim, whitespace and
/// key order included, and can only be searched by re-parsing every row. <c>jsonb</c> stores a
/// parsed binary form, which is what makes a GIN index possible — and a GIN index is the entire
/// reason the registries can hold rule documents and still answer "which rules mention this city?"
/// without a sequential scan. The cost is that key order is not preserved and duplicate keys are
/// collapsed, neither of which any consumer here relies on.
/// </remarks>
public sealed class JsonNodeConverter : ValueConverter<JsonNode, string>
{
    public JsonNodeConverter()
        : base(
            node => Serialize(node),
            json => Deserialize(json))
    {
    }

    public static JsonNodeConverter Instance { get; } = new();

    /// <summary>The column type every JSON document column uses. Stated once so none of them drifts.</summary>
    public const string ColumnType = "jsonb";

    // Wrapped rather than called inline because ToJsonString and Parse both take optional
    // parameters, and an expression tree may not omit one. The wrappers also fix the options for
    // every call site, so a document cannot be written with one serialiser configuration and read
    // with another.
    internal static string Serialize(JsonNode node) => node.ToJsonString();

    internal static JsonNode Deserialize(string json) => JsonNode.Parse(json)!;
}

/// <summary>The nullable half. A column that may hold no document at all.</summary>
public sealed class NullableJsonNodeConverter : ValueConverter<JsonNode?, string?>
{
    public NullableJsonNodeConverter()
        : base(
            node => SerializeOrNull(node),
            json => DeserializeOrNull(json))
    {
    }

    public static NullableJsonNodeConverter Instance { get; } = new();

    internal static string? SerializeOrNull(JsonNode? node)
        => node is null ? null : JsonNodeConverter.Serialize(node);

    internal static JsonNode? DeserializeOrNull(string? json)
        => json is null ? null : JsonNodeConverter.Deserialize(json);
}

/// <summary>
/// Structural comparison for JSON documents.
/// </summary>
/// <remarks>
/// EF's change tracker compares a property's snapshot with its current value to decide whether to
/// write it, and for a mutable reference type it defaults to reference equality. A caller who edits
/// a node in place — <c>config["retries"] = 3</c> — would therefore produce no <c>UPDATE</c> at all,
/// and the change would vanish on the next read with no error anywhere. Comparing and snapshotting
/// by serialised text costs a little on save and makes in-place edits behave the way every
/// developer already assumes they do.
/// </remarks>
public sealed class JsonNodeComparer : ValueComparer<JsonNode>
{
    public JsonNodeComparer()
        : base(
            (left, right) => AreEqual(left, right),
            node => HashOf(node),
            node => Clone(node))
    {
    }

    public static JsonNodeComparer Instance { get; } = new();

    internal static bool AreEqual(JsonNode? left, JsonNode? right)
        => left is null
            ? right is null
            : right is not null && string.Equals(
                JsonNodeConverter.Serialize(left),
                JsonNodeConverter.Serialize(right),
                StringComparison.Ordinal);

    internal static int HashOf(JsonNode? node)
        => node is null ? 0 : JsonNodeConverter.Serialize(node).GetHashCode(StringComparison.Ordinal);

    internal static JsonNode Clone(JsonNode node) => node.DeepClone();
}
