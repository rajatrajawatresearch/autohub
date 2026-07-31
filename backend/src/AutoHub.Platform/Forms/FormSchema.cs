using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace AutoHub.Platform.Forms;

/// <summary>
/// The document the renderers consume, exactly as §2 of the registry API contract specifies it.
/// </summary>
/// <remarks>
/// The web app and both Flutter apps share one renderer that has never heard of a sell form, a KYC
/// form or an inspection checklist. This type is the entire seam between them and the platform, so
/// it is shaped by the contract rather than by what is convenient to store — the storage shape is
/// <see cref="FormVersion"/> and its rows, and <see cref="FormSchemaCompiler"/> is the translation.
/// <para>
/// Properties absent from the contract (<c>variant</c>, the <c>*Key</c> localisation keys) serialise
/// only when populated, so a form with no variant and no translation keys produces byte-for-byte the
/// document in the contract. They exist because the mobile apps cache an i18n bundle locally and
/// re-resolve labels offline, which needs the key and not just the resolved string.
/// </para>
/// </remarks>
public sealed record CompiledForm
{
    public required string Key { get; init; }

    /// <summary>
    /// Version numbers are unique per form key across all variants, so this value alone identifies
    /// exactly which document a client holds — which is what the <c>ETag</c> is built from.
    /// </summary>
    public required int Version { get; init; }

    /// <summary>The variant that matched the request context, e.g. <c>category:suv</c>.</summary>
    public string? Variant { get; init; }

    public required string Title { get; init; }

    public string? TitleKey { get; init; }

    public required string SubmitLabel { get; init; }

    public string? SubmitLabelKey { get; init; }

    public required IReadOnlyList<CompiledStep> Steps { get; init; }

    /// <summary>Every field in step order. The submitted payload is flat, so this is the real shape.</summary>
    public IEnumerable<CompiledField> Fields() => Steps.SelectMany(s => s.Fields);
}

public sealed record CompiledStep
{
    public required string Key { get; init; }

    public required string Title { get; init; }

    public string? TitleKey { get; init; }

    public string? Description { get; init; }

    public string? DescriptionKey { get; init; }

    public required IReadOnlyList<CompiledField> Fields { get; init; }
}

public sealed record CompiledField
{
    public required string Key { get; init; }

    public required string Label { get; init; }

    public string? LabelKey { get; init; }

    public required FormControl Control { get; init; }

    public bool Required { get; init; }

    public string? Unit { get; init; }

    public string? Placeholder { get; init; }

    public string? HelpText { get; init; }

    /// <summary>
    /// Present when the field is taxonomy-backed. Clients ignore it; it is here so an operator
    /// reading the served document can see where the options came from without opening the database.
    /// </summary>
    public string? AttributeCode { get; init; }

    /// <summary>Resolved from the attribute definition at serve time. Never stored on the field.</summary>
    public IReadOnlyList<CompiledOption>? Options { get; init; }

    public JsonNode? Config { get; init; }

    /// <summary>JsonLogic over the form's own values.</summary>
    public JsonNode? VisibleWhen { get; init; }

    public required IReadOnlyList<CompiledRule> Validation { get; init; }
}

public sealed record CompiledRule
{
    /// <summary>Named <c>rule</c> on the wire — the contract's key, not a C# habit.</summary>
    [JsonPropertyName("rule")]
    public required FormRuleKind Rule { get; init; }

    public JsonNode? Value { get; init; }

    public required string Code { get; init; }

    public required string Message { get; init; }
}

public sealed record CompiledOption(string Value, string Label, int SortOrder);

/// <summary>
/// One serializer configuration for every form document, shared and reused.
/// </summary>
/// <remarks>
/// Shared because the same options object must produce the stored <c>schema_json</c>, the HTTP
/// response, and the round-trip back out of storage. Three near-identical configurations would
/// eventually differ in one null-handling flag, and the difference would show up as a renderer
/// crash on a field that used to serialise.
/// </remarks>
public static class FormJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        // Omitting nulls is not cosmetic: the renderer treats a present-but-null `options` as
        // "an empty option list" and shows an unusable dropdown, where absent means "not that kind
        // of field". It also keeps a 200-point inspection checklist a fraction of its verbose size
        // on a mobile connection.
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(CompiledForm form) => JsonSerializer.Serialize(form, Options);

    public static CompiledForm? Deserialize(string json) => JsonSerializer.Deserialize<CompiledForm>(json, Options);
}
