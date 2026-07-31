using System.Text.Json;
using System.Text.Json.Nodes;

namespace AutoHub.Modules.Listings.Domain.Listings;

/// <summary>One thing a complete listing has, and whether this listing has it.</summary>
/// <remarks>
/// <see cref="Code"/> is a machine key the front end localises, never a sentence — "Add three more
/// photos" is a translated string, and the number in it comes from <see cref="Detail"/>.
/// </remarks>
public sealed record QualitySignal(string Code, bool Satisfied, string? Detail);

/// <summary>
/// How complete a listing is, as a number a seller can act on.
/// </summary>
/// <remarks>
/// <b>Why every signal counts the same.</b> The obvious design is a weighted score — photos worth
/// 40, description worth 10 — and it would put five percentages into compiled code, which is exactly
/// what the prime directive forbids and exactly what an operations team would want to tune. The
/// alternative would be a weights table, which is a registry nobody asked for and one more thing to
/// seed before the site works.
/// <para>
/// So the score is the fraction of satisfied signals, and the <i>signals themselves</i> come from
/// registries: which attributes are required is the taxonomy registry's answer for this category,
/// and how many photos are enough is the form registry's <c>photo_set</c> configuration. Adding
/// <c>boom_length</c> to Construction Equipment automatically makes it a quality signal, with no
/// code change and no weight to invent — which is the same acceptance test the north star sets for
/// everything else.
/// </para>
/// <para>
/// The breakdown travels with the score because a bare number is not actionable. A seller shown "62"
/// closes the tab; a seller shown "62 — add 3 photos, add the odometer reading" finishes the
/// listing, and that difference is most of the sell-flow funnel.
/// </para>
/// </remarks>
public sealed record ListingQuality(int Score, IReadOnlyList<QualitySignal> Signals)
{
    /// <summary>A listing with no signals evaluated yet. Not zero quality — unknown quality.</summary>
    public static ListingQuality Unscored { get; } = new(0, []);

    public static ListingQuality From(IReadOnlyList<QualitySignal> signals)
    {
        if (signals.Count == 0)
        {
            return Unscored;
        }

        var satisfied = signals.Count(signal => signal.Satisfied);

        // Rounded away from zero so a listing that satisfies half its signals reads as 50 rather
        // than 49, which is the kind of off-by-one a seller notices and support cannot explain.
        return new ListingQuality(
            (int)Math.Round(100m * satisfied / signals.Count, 0, MidpointRounding.AwayFromZero),
            signals);
    }

    public string ToJson() => JsonSerializer.Serialize(Signals, QualityJson.Options);

    public static ListingQuality FromStorage(int score, string? signalsJson)
    {
        if (string.IsNullOrWhiteSpace(signalsJson))
        {
            return new ListingQuality(score, []);
        }

        var signals = JsonSerializer.Deserialize<List<QualitySignal>>(signalsJson, QualityJson.Options);
        return new ListingQuality(score, signals ?? []);
    }

    public JsonArray ToJsonArray()
        => JsonNode.Parse(ToJson()) as JsonArray ?? [];
}

/// <summary>
/// One serializer configuration for the breakdown, shared by the writer and the reader.
/// </summary>
/// <remarks>
/// Shared because the same options must produce the stored column and read it back. Two
/// near-identical configurations eventually differ in one casing flag, and the difference shows up
/// as a breakdown that deserialises to every signal unsatisfied — which reads as a listing that
/// suddenly lost its photos.
/// </remarks>
internal static class QualityJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web);
}
