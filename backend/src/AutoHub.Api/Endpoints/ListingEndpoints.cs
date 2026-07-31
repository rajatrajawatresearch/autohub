using System.Text.Json.Serialization;
using AutoHub.Api.Http;
using AutoHub.Integrations.Storage;
using AutoHub.Modules.Listings.Application.GetListing;
using AutoHub.Modules.Listings.Contracts;
using AutoHub.Platform.Workflow;
using MediatR;

namespace AutoHub.Api.Endpoints;

/// <summary>
/// <c>GET /listings/{id}</c> — the vehicle detail page.
/// </summary>
/// <remarks>
/// Anonymous for the same reason search is: a buyer reads a listing before they sign in, and this is
/// the page that has to be indexable.
/// <para>
/// <b>There is no status field on the response.</b> State lives on <c>workflow</c>, resolved for this
/// caller — the actions listed are the ones this user may actually fire, guards and permissions
/// already evaluated. That indirection is the whole point of the workflow registry, and a
/// convenience <c>status</c> string alongside it would be the first thing a client branched on and
/// the first thing to go stale.
/// </para>
/// </remarks>
internal static class ListingEndpoints
{
    public static IEndpointRouteBuilder MapListingEndpoints(this IEndpointRouteBuilder app)
    {
        var listings = app.MapGroup("/listings").WithTags("Listings").AllowAnonymous();

        listings.MapGet("/{listingId:guid}", async (
                Guid listingId,
                HttpContext context,
                ISender sender,
                IObjectStore objects,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetListingQuery(listingId), ct);

                if (result.IsFailure)
                {
                    return ApiProblem.Result(result.Error, context);
                }

                return Results.Ok(await ListingDetailResponse.FromAsync(result.Value, objects, ct));
            })
            .WithName("GetListing")
            .WithSummary("One listing, with its media, specs and workflow state.");

        return app;
    }
}

/// <summary>
/// The detail body, named for the wire rather than for the module.
/// </summary>
/// <remarks>
/// Extends the search card's shape rather than inventing a second one, because the client's
/// <c>ListingDetailSchema</c> extends <c>SearchItemSchema</c> — a detail page and a search card
/// disagreeing about what a listing is would be a bug waiting on a rename.
/// </remarks>
public sealed record ListingDetailResponse
{
    /// <summary>
    /// How long a signed media URL stays valid.
    /// </summary>
    /// <remarks>
    /// Matches the search endpoint's cover lifetime. Longer than a page view, short enough that a URL
    /// copied out of devtools stops working before it is worth sharing.
    /// </remarks>
    private static readonly TimeSpan MediaLifetime = TimeSpan.FromHours(1);

    public required Guid ListingId { get; init; }

    public required string Intent { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PlateResponse? Registration { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required long PriceMinor { get; init; }

    public required string Currency { get; init; }

    public required string City { get; init; }

    public required IReadOnlyList<SpecItemResponse> SpecStrip { get; init; }

    public required MediaResponse Media { get; init; }

    public required IReadOnlyList<MediaAssetResponse> Gallery { get; init; }

    public required IReadOnlyList<string> Badges { get; init; }

    /// <summary>Where the state lives. There is no <c>status</c> on this response by design.</summary>
    public required WorkflowSnapshotBody Workflow { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Year { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ViewCount { get; init; }

    public static async Task<ListingDetailResponse> FromAsync(
        ListingDetail detail,
        IObjectStore objects,
        CancellationToken cancellationToken)
    {
        var gallery = await ResolveGalleryAsync(detail.Media, objects, cancellationToken);
        var cover = gallery.FirstOrDefault(asset => asset.IsCover) ?? gallery.FirstOrDefault();

        return new ListingDetailResponse
        {
            ListingId = detail.Listing.ListingId,
            Intent = detail.Listing.Intent.ToWireName(),

            // The plate belongs to the vehicle, not the listing — a listing is an intent to transact
            // something the catalog owns. Null when the catalog could not answer, which the client
            // renders as a card without a plate rather than as an error.
            Registration = detail.Vehicle?.Registration is { Length: > 0 } canonical
                ? new PlateResponse(canonical, detail.Vehicle.RegistrationDisplay ?? canonical)
                : null,

            Title = detail.Title,
            Description = detail.Description,
            PriceMinor = detail.Listing.PriceMinorUnits,
            Currency = detail.Listing.PriceCurrency,
            City = detail.Listing.CityCode,
            SpecStrip = [.. detail.SpecStrip.Select(SpecItemResponse.From)],
            Media = new MediaResponse(cover?.Url, cover?.AspectRatio, gallery.Count),
            Gallery = gallery,

            // Badges are quality signals the listing actually earned, as codes. Labels resolve
            // through the i18n registry — nothing here turns `verified_rc` into a word.
            Badges = [.. detail.Quality.Signals.Where(s => s.Satisfied).Select(s => s.Code)],

            Workflow = WorkflowSnapshotBody.From(detail.Workflow),
            Year = detail.Vehicle?.ManufactureYear,
        };
    }

    /// <summary>
    /// Signs every gallery object key once.
    /// </summary>
    /// <remarks>
    /// A key that will not sign is dropped from the gallery rather than failing the page. A detail
    /// page missing one photo is still a page a buyer can act on; a 500 is not.
    /// </remarks>
    private static async Task<List<MediaAssetResponse>> ResolveGalleryAsync(
        IReadOnlyList<ListingMediaView> media,
        IObjectStore objects,
        CancellationToken cancellationToken)
    {
        var assets = new List<MediaAssetResponse>(media.Count);

        foreach (var item in media.OrderByDescending(m => m.IsCover).ThenBy(m => m.SortOrder))
        {
            var url = await objects.CreateReadUrlAsync(item.ObjectKey, MediaLifetime, cancellationToken);

            if (url.IsFailure)
            {
                continue;
            }

            assets.Add(new MediaAssetResponse
            {
                Id = item.MediaId,
                Url = url.Value.ToString(),
                AspectRatio = item.AspectRatio,
                Kind = item.MediaTypeCode,
                Caption = item.PromptCode,
                IsCover = item.IsCover,
            });
        }

        return assets;
    }
}

/// <summary>One gallery item.</summary>
public sealed record MediaAssetResponse
{
    public required Guid Id { get; init; }

    public required string Url { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? AspectRatio { get; init; }

    public required string Kind { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Caption { get; init; }

    /// <summary>Used to pick the cover; not part of the client contract.</summary>
    [JsonIgnore]
    public bool IsCover { get; init; }
}

/// <summary>
/// The workflow snapshot as §3 of the contract publishes it.
/// </summary>
/// <remarks>
/// Guards are deliberately absent. The condition that decides whether a dealer may accept a deal is
/// business logic; shipping it to a customer's phone hands it to anyone with devtools, and hands a
/// fraudster the shape of the check they need to defeat.
/// </remarks>
public sealed record WorkflowSnapshotBody
{
    public required string WorkflowKey { get; init; }

    public required WorkflowStateBody CurrentState { get; init; }

    public required DateTimeOffset EnteredAt { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? DueAt { get; init; }

    public required IReadOnlyList<AvailableActionBody> AvailableActions { get; init; }

    public required IReadOnlyList<WorkflowHistoryBody> History { get; init; }

    public static WorkflowSnapshotBody From(WorkflowSnapshot snapshot) => new()
    {
        WorkflowKey = snapshot.WorkflowKey,
        CurrentState = new WorkflowStateBody(
            snapshot.CurrentState.Code,
            snapshot.CurrentState.Name,
            ToWireCategory(snapshot.CurrentState.Category),
            snapshot.CurrentState.IsTerminal),
        EnteredAt = snapshot.EnteredAt,
        DueAt = snapshot.DueAt,
        AvailableActions =
        [
            .. snapshot.AvailableActions.Select(action =>
                new AvailableActionBody(action.ActionCode, action.Label, action.RequiresReason)),
        ],
        History =
        [
            .. snapshot.History.Select(entry => new WorkflowHistoryBody(
                entry.FromStateCode,
                entry.ToStateCode,
                entry.ActionCode,
                entry.OccurredAt,
                entry.Reason,
                entry.TimeInPreviousState)),
        ],
    };

    /// <summary>
    /// The six categories, snake_cased for the wire.
    /// </summary>
    /// <remarks>
    /// The client maps category to a <c>StatusPill</c> tone, so the server never sends a colour and a
    /// new state gets the right treatment without a hex value in the database.
    /// </remarks>
    private static string ToWireCategory(StateCategory category) => category switch
    {
        StateCategory.Draft => "draft",
        StateCategory.AwaitingAction => "awaiting_action",
        StateCategory.InProgress => "in_progress",
        StateCategory.Succeeded => "succeeded",
        StateCategory.Failed => "failed",
        StateCategory.Cancelled => "cancelled",
        _ => "in_progress",
    };
}

public sealed record WorkflowStateBody(string Code, string Name, string Category, bool IsTerminal);

public sealed record AvailableActionBody(string ActionCode, string Label, bool RequiresReason);

public sealed record WorkflowHistoryBody(
    string? From,
    string To,
    string? ActionCode,
    DateTimeOffset OccurredAt,
    string? Reason,
    TimeSpan? TimeInPreviousState);
