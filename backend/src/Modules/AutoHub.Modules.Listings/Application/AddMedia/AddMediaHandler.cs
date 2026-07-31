using AutoHub.Modules.Listings.Application.Abstractions;
using AutoHub.Modules.Listings.Application.GetListing;
using AutoHub.Modules.Listings.Application.Validation;
using AutoHub.Modules.Listings.Infrastructure;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using FluentValidation;
using MediatR;

namespace AutoHub.Modules.Listings.Application.AddMedia;

/// <summary>
/// Attaches media and rescores the listing.
/// </summary>
/// <remarks>
/// Rescoring here rather than on a schedule is what makes the seller's progress bar move as they
/// upload. A nightly job would be cheaper and would also mean a seller who added the last three
/// photos sees no change, concludes it did not work, and uploads them again.
/// </remarks>
public sealed class AddMediaHandler : IRequestHandler<AddMediaCommand, Result<ListingDetail>>
{
    private readonly IListingRepository _listings;
    private readonly IMediaPolicy _mediaPolicy;
    private readonly IListingQualityScorer _scorer;
    private readonly ListingDescriber _describer;
    private readonly WorkflowActorFactory _actors;
    private readonly IValidator<AddMediaCommand> _validator;
    private readonly IClock _clock;

    public AddMediaHandler(
        IListingRepository listings,
        IMediaPolicy mediaPolicy,
        IListingQualityScorer scorer,
        ListingDescriber describer,
        WorkflowActorFactory actors,
        IValidator<AddMediaCommand> validator,
        IClock clock)
    {
        _listings = listings;
        _mediaPolicy = mediaPolicy;
        _scorer = scorer;
        _describer = describer;
        _actors = actors;
        _validator = validator;
        _clock = clock;
    }

    public async Task<Result<ListingDetail>> Handle(
        AddMediaCommand command,
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

        var policy = await _mediaPolicy.ForAsync(listing, cancellationToken);

        var attached = listing.AttachMedia(
            command.ObjectKey,
            command.MediaTypeCode,
            command.PromptCode,
            command.Width,
            command.Height,
            command.ContentHash,
            policy.Maximum,
            _clock.UtcNow);

        if (attached.IsFailure)
        {
            return Result.Failure<ListingDetail>(attached.Error);
        }

        if (command.MakeCover)
        {
            var cover = listing.SetCover(attached.Value.Id);
            if (cover.IsFailure)
            {
                return Result.Failure<ListingDetail>(cover.Error);
            }
        }

        listing.Score(await _scorer.ScoreAsync(listing, cancellationToken));

        await _listings.SaveChangesAsync(cancellationToken);

        var actor = await _actors.ForCurrentUserAsync(cancellationToken);
        return await _describer.DescribeAsync(listing, actor, cancellationToken);
    }
}
