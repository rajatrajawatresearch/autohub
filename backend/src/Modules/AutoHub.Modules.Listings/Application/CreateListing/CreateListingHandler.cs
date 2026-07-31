using System.Text.Json.Nodes;
using AutoHub.Modules.Catalog.Contracts;
using AutoHub.Modules.Listings.Application.Abstractions;
using AutoHub.Modules.Listings.Application.GetListing;
using AutoHub.Modules.Listings.Application.Validation;
using AutoHub.Modules.Listings.Contracts;
using AutoHub.Modules.Listings.Domain.Listings;
using AutoHub.Modules.Listings.Infrastructure;
using AutoHub.Platform.Forms;
using AutoHub.Platform.Taxonomy;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Security;
using AutoHub.SharedKernel.Time;
using FluentValidation;
using MediatR;

namespace AutoHub.Modules.Listings.Application.CreateListing;

/// <summary>
/// Turns a sell-flow submission into a listing with a running lifecycle.
/// </summary>
/// <remarks>
/// <b>The listing's state is never set here.</b> The handler calls
/// <c>IWorkflowEngine.StartAsync</c>, and the engine places the listing in whatever the published
/// <c>listing</c> workflow says its initial state is. Nothing in this file knows the word "draft",
/// and that is what lets operations insert a "Photos pending" state before it without a deployment.
/// <para>
/// <b>Ordering matters and is deliberate.</b> The listing is added to the unit of work, then the
/// workflow is started, and the engine's save commits both. A listing saved before its workflow
/// exists is a listing that could be read in the window between the two — with no state — and a
/// workflow started before the listing exists is an instance pointing at nothing if the insert then
/// fails.
/// </para>
/// <para>
/// <b>The vehicle is resolved, never created.</b> Going through <c>IVehicleDirectory</c> rather than
/// writing a vehicle here is what stops the same car existing once as a C2C listing and again as a
/// dealer's stock row.
/// </para>
/// </remarks>
public sealed class CreateListingHandler : IRequestHandler<CreateListingCommand, Result<ListingDetail>>
{
    private readonly IListingRepository _listings;
    private readonly IFormRegistry _forms;
    private readonly IVehicleDirectory _vehicles;
    private readonly IVehicleTaxonomy _taxonomy;
    private readonly ITaxonomyRegistry _registry;
    private readonly IEntityAttributeStore _attributes;
    private readonly IListingQualityScorer _scorer;
    private readonly IWorkflowEngine _workflow;
    private readonly ListingDescriber _describer;
    private readonly WorkflowActorFactory _actors;
    private readonly IValidator<CreateListingCommand> _validator;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public CreateListingHandler(
        IListingRepository listings,
        IFormRegistry forms,
        IVehicleDirectory vehicles,
        IVehicleTaxonomy taxonomy,
        ITaxonomyRegistry registry,
        IEntityAttributeStore attributes,
        IListingQualityScorer scorer,
        IWorkflowEngine workflow,
        ListingDescriber describer,
        WorkflowActorFactory actors,
        IValidator<CreateListingCommand> validator,
        ICurrentUser currentUser,
        IClock clock)
    {
        _listings = listings;
        _forms = forms;
        _vehicles = vehicles;
        _taxonomy = taxonomy;
        _registry = registry;
        _attributes = attributes;
        _scorer = scorer;
        _workflow = workflow;
        _describer = describer;
        _actors = actors;
        _validator = validator;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<ListingDetail>> Handle(
        CreateListingCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return RequestValidation.ToError(validation);
        }

        var intent = ListingIntentWireNames.Parse(command.Intent);
        if (intent.IsFailure)
        {
            return Result.Failure<ListingDetail>(intent.Error);
        }

        var price = ReadPrice(command);
        if (price.IsFailure)
        {
            return Result.Failure<ListingDetail>(price.Error);
        }

        var context = FormContext.Parse(command.FormContext, command.Locale);

        // The published form is fetched before validation so the version can be pinned to the
        // listing. Validating first and fetching after would leave a window in which an operator
        // publishes v8 between the two calls, and the listing would then claim to have been filled
        // against a form it never saw.
        var form = await _forms.GetPublishedAsync(command.FormKey, context, cancellationToken);
        if (form.IsFailure)
        {
            return Result.Failure<ListingDetail>(form.Error);
        }

        var formCheck = await _forms.ValidateAsync(
            command.FormKey, context, command.Payload, FormValidationMode.Complete, cancellationToken);

        if (formCheck.IsFailure)
        {
            return Result.Failure<ListingDetail>(formCheck.Error);
        }

        var category = await _taxonomy.ResolveCategoryAsync(command.CategoryCode, cancellationToken);
        if (category.IsFailure)
        {
            return Result.Failure<ListingDetail>(category.Error);
        }

        var vehicle = await _vehicles.ResolveOrCreateAsync(
            category.Value.Id, command.VehicleIdentifiers, cancellationToken);

        if (vehicle.IsFailure)
        {
            return Result.Failure<ListingDetail>(vehicle.Error);
        }

        var now = _clock.UtcNow;

        var drafted = Listing.Draft(
            vehicle.Value.VehicleId,
            category.Value.Id,
            intent.Value,
            command.SellerPartyId,
            command.CityCode,
            price.Value,
            command.FormKey,
            form.Value.Version,
            now);

        if (drafted.IsFailure)
        {
            return Result.Failure<ListingDetail>(drafted.Error);
        }

        var listing = drafted.Value;
        listing.Describe(command.Title, command.Description);

        var repriced = listing.Reprice(price.Value, command.PricePeriodCode, now);
        if (repriced.IsFailure)
        {
            return Result.Failure<ListingDetail>(repriced.Error);
        }

        await _listings.AddAsync(listing, cancellationToken);

        var attributes = await ApplyAttributesAsync(
            listing, category.Value.Code, command.Payload, cancellationToken);

        if (attributes.IsFailure)
        {
            return Result.Failure<ListingDetail>(attributes.Error);
        }

        listing.Score(await _scorer.ScoreAsync(listing, cancellationToken));

        var started = await _workflow.StartAsync(
            ListingWorkflows.Listing,
            listing.Id,
            listing.Attributes(),
            _currentUser.UserId,
            cancellationToken);

        if (started.IsFailure)
        {
            // No published listing workflow means no lifecycle, and a listing without one is a row
            // nobody can move, approve or delist. Failing here is the honest answer; the alternative
            // is a listing that exists and can never do anything.
            return Result.Failure<ListingDetail>(started.Error);
        }

        // The engine's store shares this unit of work and has already committed both the listing and
        // the instance. Called anyway so the handler does not depend on that being true — it is a
        // no-op when nothing is pending, and a correctness fix if the engine ever stops saving.
        await _listings.SaveChangesAsync(cancellationToken);

        var actor = await _actors.ForCurrentUserAsync(cancellationToken);
        return await _describer.DescribeAsync(listing, actor, cancellationToken);
    }

    /// <summary>
    /// Reads the amount, refusing a currency this build cannot represent.
    /// </summary>
    /// <remarks>
    /// <c>Currency.FromCode</c> throws for an unknown code, which is right at a compile-time call
    /// site and wrong at a request boundary — a client sending "RS" should get a 422 naming the
    /// field, not a 500 naming a stack frame.
    /// </remarks>
    private static Result<Money> ReadPrice(CreateListingCommand command)
    {
        try
        {
            return Money.FromMinorUnits(command.PriceMinorUnits, Currency.FromCode(command.PriceCurrencyCode));
        }
        catch (ArgumentOutOfRangeException)
        {
            return Error.Validation(
                "listing.currency_unsupported",
                $"'{command.PriceCurrencyCode}' is not a currency this platform accepts.")
                .With("currency", command.PriceCurrencyCode);
        }
    }

    /// <summary>
    /// Validates the submission's attribute keys against the category and stores them.
    /// </summary>
    /// <remarks>
    /// The whole payload is handed to the taxonomy registry rather than a filtered subset. Keys the
    /// category does not define are ignored by the validator, which is what lets one sell-flow
    /// submission carry both form-only answers (a preferred callback time) and attribute answers
    /// without the handler having to know which is which — a distinction that would go stale the
    /// first time an operator promoted a form field into an attribute.
    /// </remarks>
    private async Task<Result> ApplyAttributesAsync(
        Listing listing,
        string categoryCode,
        JsonObject payload,
        CancellationToken cancellationToken)
    {
        var validated = await _registry.ValidateAttributesAsync(categoryCode, payload, cancellationToken);
        if (validated.IsFailure)
        {
            return Result.Failure(validated.Error);
        }

        var stored = await _attributes.ReplaceAsync(
            ListingEntityTypes.Listing, listing.Id, validated.Value, cancellationToken);

        if (stored.IsFailure)
        {
            return stored;
        }

        var projection = new JsonObject();
        foreach (var (code, value) in validated.Value)
        {
            projection[code] = value.ToJson();
        }

        listing.ProjectAttributes(projection);
        return Result.Success();
    }
}
