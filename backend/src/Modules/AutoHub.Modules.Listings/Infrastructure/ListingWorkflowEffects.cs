using System.Text.Json.Nodes;
using AutoHub.Modules.Listings.Application.Abstractions;
using AutoHub.Modules.Listings.Domain.Listings;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace AutoHub.Modules.Listings.Infrastructure;

/// <summary>
/// Puts a listing into the search index, and records that it went public.
/// </summary>
/// <remarks>
/// <b>This is why nothing in this module compares against the word "live".</b> The question "which
/// transition means the listing is now on the market?" is answered by an operator attaching this
/// effect to that transition, not by a developer writing <c>if (state == "live")</c>. Insert a
/// moderation step, split approval in two, add a "photos pending" gate — the effect follows whichever
/// transition it is wired to, and this file never changes.
/// <para>
/// It raises <see cref="ListingPublished"/> on the aggregate as well as indexing, because "the
/// listing went public" is a domain fact other modules subscribe to — notifications tells the
/// watchers, analytics starts the days-to-sale clock — and the outbox row for it must be written in
/// the same transaction as the transition that caused it.
/// </para>
/// <para>
/// <b>Idempotent, as every effect handler must be.</b> The engine can retry a transition after a
/// transient failure and the scheduler can pick up the same due instance twice; indexing the same
/// listing again is a no-op, and the duplicate event is deduplicated by the outbox consumer.
/// </para>
/// </remarks>
public sealed class ListingIndexEffect : IWorkflowEffectHandler
{
    private readonly IListingRepository _listings;
    private readonly ISearchIndex _index;
    private readonly ILogger<ListingIndexEffect> _logger;

    public ListingIndexEffect(
        IListingRepository listings,
        ISearchIndex index,
        ILogger<ListingIndexEffect> logger)
    {
        _listings = listings;
        _index = index;
        _logger = logger;
    }

    public string EffectCode => "listings.search_index.publish";

    public async Task<Result> ExecuteAsync(
        WorkflowEffectContext context,
        JsonNode? arguments,
        CancellationToken cancellationToken)
    {
        var listing = await _listings.GetAsync(context.EntityId, cancellationToken);

        if (listing is null)
        {
            // The effect is wired to a workflow whose entity is not a listing. That is a
            // configuration mistake, and refusing the transition is the safe direction — the
            // alternative is a lifecycle that appears to work and silently indexes nothing.
            _logger.LogError(
                "Effect {EffectCode} fired for {EntityId}, which is not a listing",
                EffectCode, context.EntityId);

            return Error.Unexpected(
                "listing.effect.entity_not_found",
                "This action is misconfigured and has been blocked.")
                .With("entityId", context.EntityId);
        }

        listing.MarkPublished(context.OccurredAt);

        return await _index.IndexAsync(listing.Id, cancellationToken);
    }
}

/// <summary>
/// Takes a listing out of the search index without deleting it.
/// </summary>
/// <remarks>
/// Wired to whichever transitions end a listing's public life — sold, withdrawn, rejected, expired.
/// Removal is separate from deletion for the reason <c>ISoftDeletable</c> exists: a sold listing has
/// to survive for the deal, the invoice and any dispute, and only its visibility ends.
/// <para>
/// Worth marking <c>continueOnFailure</c> in the registry. A search cluster that is down must not
/// stop a sale from completing, and the next reindex sweep corrects the stale document — whereas a
/// blocked transition leaves money in escrow.
/// </para>
/// </remarks>
public sealed class ListingUnindexEffect : IWorkflowEffectHandler
{
    private readonly ISearchIndex _index;

    public ListingUnindexEffect(ISearchIndex index) => _index = index;

    public string EffectCode => "listings.search_index.remove";

    public async Task<Result> ExecuteAsync(
        WorkflowEffectContext context,
        JsonNode? arguments,
        CancellationToken cancellationToken)
        => await _index.RemoveAsync(context.EntityId, cancellationToken);
}
