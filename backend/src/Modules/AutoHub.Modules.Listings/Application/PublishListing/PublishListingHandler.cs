using AutoHub.Modules.Listings.Application.Abstractions;
using AutoHub.Modules.Listings.Application.GetListing;
using AutoHub.Modules.Listings.Application.Validation;
using AutoHub.Modules.Listings.Contracts;
using AutoHub.Modules.Listings.Infrastructure;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Results;
using FluentValidation;
using MediatR;

namespace AutoHub.Modules.Listings.Application.PublishListing;

public sealed class PublishListingHandler : IRequestHandler<PublishListingCommand, Result<ListingDetail>>
{
    private readonly IListingRepository _listings;
    private readonly IListingQualityScorer _scorer;
    private readonly IWorkflowEngine _workflow;
    private readonly ListingDescriber _describer;
    private readonly WorkflowActorFactory _actors;
    private readonly IValidator<PublishListingCommand> _validator;

    public PublishListingHandler(
        IListingRepository listings,
        IListingQualityScorer scorer,
        IWorkflowEngine workflow,
        ListingDescriber describer,
        WorkflowActorFactory actors,
        IValidator<PublishListingCommand> validator)
    {
        _listings = listings;
        _scorer = scorer;
        _workflow = workflow;
        _describer = describer;
        _actors = actors;
        _validator = validator;
    }

    public async Task<Result<ListingDetail>> Handle(
        PublishListingCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return RequestValidation.ToError(validation);
        }

        var listing = await _listings.GetAsync(command.ListingId, cancellationToken);
        if (listing is null)
        {
            return Error.NotFound("listing.not_found", "No listing with that id.")
                .With("listingId", command.ListingId);
        }

        // Rescored before the guard runs, so a rule like "at least six photos and a quality score
        // above 60" is evaluated against what the listing is now rather than what it was when its
        // last photo landed. A stale score is a guard that passes a listing it should have held.
        listing.Score(await _scorer.ScoreAsync(listing, cancellationToken));
        await _listings.SaveChangesAsync(cancellationToken);

        var actor = await _actors.ForCurrentUserAsync(cancellationToken);

        var fired = await _workflow.FireAsync(
            new WorkflowFireRequest
            {
                WorkflowKey = ListingWorkflows.Listing,
                EntityId = listing.Id,
                ActionCode = command.ActionCode,
                Actor = actor,
                Context = ListingDescriber.GuardContext(listing),
                Reason = command.Reason,
                IdempotencyKey = command.IdempotencyKey,
            },
            cancellationToken);

        if (fired.IsFailure)
        {
            // The engine's own codes travel back unchanged — `workflow.transition_not_allowed`
            // carries the current state in its metadata, which is what lets a stale tab refresh
            // instead of guessing. Wrapping it in a listing-flavoured error would throw that away.
            return Result.Failure<ListingDetail>(fired.Error);
        }

        await _listings.SaveChangesAsync(cancellationToken);

        return await _describer.DescribeAsync(listing, actor, cancellationToken);
    }
}
