using System.Text.Json.Nodes;
using AutoHub.Modules.Listings.Application.Abstractions;
using AutoHub.Modules.Listings.Domain.Listings;
using AutoHub.Platform.Forms;

namespace AutoHub.Modules.Listings.Infrastructure;

/// <summary>
/// Reads the media policy out of the listing's form schema.
/// </summary>
/// <remarks>
/// The <c>photo_set</c> control's <c>config</c> is the contract's own shape —
/// <c>{ "min": 6, "max": 20, "prompts": [ "Front 3/4", … ] }</c> — so nothing here invents a
/// vocabulary; it reads the one the registry already publishes to the renderers.
/// <para>
/// <b>The fallbacks are technical backstops, not business rules.</b> They apply only when a form has
/// no photo-set field at all, which is a misconfiguration rather than a decision, and they are
/// deliberately permissive: refusing a seller's upload because an operator forgot to add a field is
/// a support ticket the seller did not deserve. The maximum exists so a scripted client cannot turn
/// one listing into ten thousand object-store rows.
/// </para>
/// </remarks>
internal sealed class FormMediaPolicy : IMediaPolicy
{
    private const int FallbackMinimum = 1;

    private const int FallbackMaximum = 60;

    private readonly IFormRegistry _forms;

    public FormMediaPolicy(IFormRegistry forms) => _forms = forms;

    public async Task<MediaPolicy> ForAsync(Listing listing, CancellationToken cancellationToken)
    {
        var form = await _forms.GetPublishedAsync(listing.FormKey, FormContext.Empty, cancellationToken);

        if (form.IsFailure)
        {
            return new MediaPolicy(FallbackMinimum, FallbackMaximum, []);
        }

        var field = form.Value.Fields().FirstOrDefault(f => f.Control == FormControl.PhotoSet);

        if (field?.Config is not JsonObject config)
        {
            return new MediaPolicy(FallbackMinimum, FallbackMaximum, []);
        }

        var minimum = ReadInt(config, "min") ?? FallbackMinimum;
        var maximum = ReadInt(config, "max") ?? FallbackMaximum;

        // An inverted pair is an authoring mistake that would otherwise make every upload fail. The
        // ceiling wins, because a listing that can hold no photos is useless and a listing that can
        // hold a few too many is not.
        if (maximum < minimum)
        {
            maximum = Math.Max(minimum, FallbackMaximum);
        }

        return new MediaPolicy(minimum, maximum, ReadPrompts(config));
    }

    private static int? ReadInt(JsonObject config, string key)
        => config.TryGetPropertyValue(key, out var node)
           && node is JsonValue value
           && value.TryGetValue<int>(out var number)
            ? number
            : null;

    private static List<string> ReadPrompts(JsonObject config)
    {
        if (!config.TryGetPropertyValue("prompts", out var node) || node is not JsonArray array)
        {
            return [];
        }

        var prompts = new List<string>(array.Count);

        foreach (var element in array)
        {
            if (element is JsonValue value && value.TryGetValue<string>(out var prompt))
            {
                prompts.Add(prompt);
            }
        }

        return prompts;
    }
}
