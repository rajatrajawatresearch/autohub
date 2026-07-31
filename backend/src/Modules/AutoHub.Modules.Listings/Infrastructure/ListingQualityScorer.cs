using System.Globalization;
using AutoHub.Modules.Catalog.Contracts;
using AutoHub.Modules.Listings.Application.Abstractions;
using AutoHub.Modules.Listings.Domain.Listings;
using AutoHub.Platform.Taxonomy;

namespace AutoHub.Modules.Listings.Infrastructure;

/// <summary>
/// Scores a listing against what the registries say a complete one looks like.
/// </summary>
/// <remarks>
/// <b>Nothing in here is a number a developer chose.</b> Which attributes count is the taxonomy
/// registry's answer for the listing's category; how many photos count is the form registry's
/// <c>photo_set</c> configuration. Adding <c>payload_kg</c> to Commercial makes it a quality signal
/// on every truck listing that afternoon, with no code change and no weight to invent — which is the
/// same acceptance test the north star sets for a new category.
/// <para>
/// Every signal is worth the same, and that is a stated choice rather than a hidden one. Weighted
/// signals would put five percentages into a compiled assembly, which the prime directive forbids,
/// and a weights registry would be a table nobody asked for that has to be seeded before the site
/// works. Equal weighting is defensible, explainable to a seller, and self-adjusting: a category
/// with more attributes automatically makes each one worth less.
/// </para>
/// </remarks>
internal sealed class ListingQualityScorer : IListingQualityScorer
{
    private readonly IVehicleTaxonomy _taxonomy;
    private readonly ITaxonomyRegistry _registry;
    private readonly IMediaPolicy _mediaPolicy;

    public ListingQualityScorer(
        IVehicleTaxonomy taxonomy,
        ITaxonomyRegistry registry,
        IMediaPolicy mediaPolicy)
    {
        _taxonomy = taxonomy;
        _registry = registry;
        _mediaPolicy = mediaPolicy;
    }

    public async Task<ListingQuality> ScoreAsync(Listing listing, CancellationToken cancellationToken)
    {
        var signals = new List<QualitySignal>();

        await AddAttributeSignalsAsync(listing, signals, cancellationToken);
        await AddMediaSignalsAsync(listing, signals, cancellationToken);

        signals.Add(new QualitySignal("listing.description", !string.IsNullOrWhiteSpace(listing.Description), null));
        signals.Add(new QualitySignal("listing.title", !string.IsNullOrWhiteSpace(listing.Title), null));

        // A zero price is a legitimate listing — "price on application" — but it is not a complete
        // one, and a buyer scanning results reads a missing price as a listing that is not serious.
        signals.Add(new QualitySignal("listing.price", listing.PriceMinorUnits > 0, null));

        return ListingQuality.From(signals);
    }

    /// <summary>
    /// One signal per attribute the category exposes, required or not.
    /// </summary>
    /// <remarks>
    /// Optional attributes count too, because a quality score exists to pull a listing towards
    /// complete rather than merely legal — a car with its service history and its insurance expiry
    /// filled in sells faster, and neither is ever going to be mandatory.
    /// </remarks>
    private async Task AddAttributeSignalsAsync(
        Listing listing,
        List<QualitySignal> signals,
        CancellationToken cancellationToken)
    {
        var described = await _taxonomy.DescribeAsync([listing.CategoryId], cancellationToken);

        if (described.IsFailure || !described.Value.TryGetValue(listing.CategoryId, out var category))
        {
            return;
        }

        var attributes = await _registry.GetAttributesForCategoryAsync(category.Code, cancellationToken);

        if (attributes.IsFailure)
        {
            return;
        }

        var filled = listing.Attributes();

        foreach (var attribute in attributes.Value.Attributes)
        {
            var present = filled.TryGetPropertyValue(attribute.Code, out var value) && value is not null;
            signals.Add(new QualitySignal($"attribute.{attribute.Code}", present, null));
        }
    }

    private async Task AddMediaSignalsAsync(
        Listing listing,
        List<QualitySignal> signals,
        CancellationToken cancellationToken)
    {
        var policy = await _mediaPolicy.ForAsync(listing, cancellationToken);
        var count = listing.Media.Count;

        // The detail is a count pair rather than a sentence — "3/6" — so the front end can render
        // "add 3 more photos" in the seller's own language from a localised template.
        signals.Add(new QualitySignal(
            "media.minimum",
            count >= policy.Minimum,
            string.Create(CultureInfo.InvariantCulture, $"{count}/{policy.Minimum}")));

        signals.Add(new QualitySignal("media.cover", listing.Cover is not null, null));

        // Every prompt the form asked for is its own signal, so a listing missing its odometer shot
        // scores below one missing a wheel shot only if the form says both matter equally — which is
        // the operator's call, made in the registry, not here.
        foreach (var prompt in policy.Prompts)
        {
            var captured = listing.Media.Any(m =>
                string.Equals(m.PromptCode, prompt, StringComparison.OrdinalIgnoreCase));

            signals.Add(new QualitySignal($"media.prompt.{prompt}", captured, null));
        }
    }
}
