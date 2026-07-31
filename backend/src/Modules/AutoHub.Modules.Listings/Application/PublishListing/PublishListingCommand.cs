using AutoHub.Modules.Listings.Application.GetListing;
using AutoHub.SharedKernel.Results;
using MediatR;

namespace AutoHub.Modules.Listings.Application.PublishListing;

/// <summary>
/// Moves a listing along its lifecycle by firing a workflow action.
/// </summary>
/// <remarks>
/// <b>The action code is a parameter, not a constant.</b> This handler does not know that "submit"
/// exists, or "approve", or "reject" — those are transitions in the published <c>listing</c>
/// workflow, and the client sends whichever one the snapshot offered it. Hardcoding an action here
/// would put half the lifecycle in the registry and the other half in a deployment, which is the
/// worst of both.
/// <para>
/// So the flow is: read the listing, receive the actions this caller may fire, send one back. The
/// engine re-checks the permission and the guard server-side, because a client that decides for
/// itself which actions are available is a client an attacker can edit.
/// </para>
/// <para>
/// What actually happens on publication — indexing for search, notifying watchers, posting a listing
/// fee — is wired as workflow <i>effects</i> against whichever transition means "live", so the
/// operator who inserts a moderation step gets the effects on the right transition without anyone
/// touching this file.
/// </para>
/// </remarks>
public sealed record PublishListingCommand : IRequest<Result<ListingDetail>>
{
    public required Guid ListingId { get; init; }

    /// <summary>One of the codes the workflow snapshot offered this caller.</summary>
    public required string ActionCode { get; init; }

    /// <summary>Required by transitions the registry marks as needing one — rejections, cancellations.</summary>
    public string? Reason { get; init; }

    /// <summary>
    /// The caller's <c>Idempotency-Key</c>, so a double-tap on a flaky connection is one transition.
    /// </summary>
    public string? IdempotencyKey { get; init; }
}
