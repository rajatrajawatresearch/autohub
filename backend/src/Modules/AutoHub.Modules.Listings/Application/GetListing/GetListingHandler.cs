using AutoHub.Modules.Listings.Application.Abstractions;
using AutoHub.Modules.Listings.Infrastructure;
using AutoHub.SharedKernel.Results;
using MediatR;

namespace AutoHub.Modules.Listings.Application.GetListing;

public sealed class GetListingHandler : IRequestHandler<GetListingQuery, Result<ListingDetail>>
{
    private readonly IListingRepository _listings;
    private readonly ListingDescriber _describer;
    private readonly WorkflowActorFactory _actors;

    public GetListingHandler(
        IListingRepository listings,
        ListingDescriber describer,
        WorkflowActorFactory actors)
    {
        _listings = listings;
        _describer = describer;
        _actors = actors;
    }

    public async Task<Result<ListingDetail>> Handle(
        GetListingQuery query,
        CancellationToken cancellationToken)
    {
        var listing = await _listings.GetAsync(query.ListingId, cancellationToken);

        if (listing is null)
        {
            return Error.NotFound("listing.not_found", "No listing with that id.")
                .With("listingId", query.ListingId);
        }

        // The actor is resolved per request, so the actions in the response are the ones this caller
        // may actually fire. Two users opening the same listing get two different button sets from
        // the same handler, which is the point of resolving authorisation server-side.
        var actor = await _actors.ForCurrentUserAsync(cancellationToken);

        return await _describer.DescribeAsync(listing, actor, cancellationToken);
    }
}
