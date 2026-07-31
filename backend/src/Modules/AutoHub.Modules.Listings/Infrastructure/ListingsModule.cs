using AutoHub.Modules.Listings.Application.Abstractions;
using AutoHub.Modules.Listings.Application.AddMedia;
using AutoHub.Modules.Listings.Application.CreateListing;
using AutoHub.Modules.Listings.Application.GetListing;
using AutoHub.Modules.Listings.Application.MakeOffer;
using AutoHub.Modules.Listings.Application.PublishListing;
using AutoHub.Modules.Listings.Application.RespondToOffer;
using AutoHub.Modules.Listings.Application.SearchListings;
using AutoHub.Modules.Listings.Contracts;
using AutoHub.Platform.Access;
using AutoHub.Platform.Workflow;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoHub.Modules.Listings.Infrastructure;

/// <summary>
/// Everything the listings module needs from the container.
/// </summary>
/// <remarks>
/// The repositories, the entity-attribute store and the PostgreSQL search index are all registered
/// by the persistence layer, because all three are EF and this project does not carry a database.
/// What is registered here is what the module itself owns: its handlers, its validators, its
/// contract implementation, its workflow effects and its permission declarations.
/// <para>
/// The workflow effects are registered as <see cref="IWorkflowEffectHandler"/> so the engine's
/// dispatcher can find them by code — and, just as importantly, so publish-time validation can
/// refuse a workflow that references an effect this build does not implement.
/// </para>
/// </remarks>
public static class ListingsModule
{
    public static IServiceCollection AddListingsModule(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssemblyContaining<CreateListingCommand>());

        services.AddScoped<IValidator<CreateListingCommand>, CreateListingValidator>();
        services.AddScoped<IValidator<AddMediaCommand>, AddMediaValidator>();
        services.AddScoped<IValidator<PublishListingCommand>, PublishListingValidator>();
        services.AddScoped<IValidator<SearchListingsQuery>, SearchListingsValidator>();
        services.AddScoped<IValidator<MakeOfferCommand>, MakeOfferValidator>();
        services.AddScoped<IValidator<RespondToOfferCommand>, RespondToOfferValidator>();

        // Composed by six handlers apiece rather than sent through the mediator, because the search
        // handler calls the listing describer once per result row and a pipeline per row is
        // twenty-four times the overhead for what is a method call.
        services.AddScoped<ListingDescriber>();
        services.AddScoped<OfferDescriber>();

        services.AddScoped<WorkflowActorFactory>();
        services.TryAddScoped<IMediaPolicy, FormMediaPolicy>();
        services.TryAddScoped<IListingQualityScorer, ListingQualityScorer>();
        services.TryAddScoped<IListingDirectory, ListingDirectory>();

        services.AddScoped<IWorkflowEffectHandler, ListingIndexEffect>();
        services.AddScoped<IWorkflowEffectHandler, ListingUnindexEffect>();

        services.AddSingleton<IPermissionContributor, ListingsPermissionContributor>();

        return services;
    }

    /// <summary>
    /// Routes search at the OpenSearch adapter instead of the PostgreSQL one.
    /// </summary>
    /// <remarks>
    /// A separate, explicit call rather than a configuration flag inside
    /// <see cref="AddListingsModule"/>. Turning it on is a decision about where every buyer's search
    /// goes, and it should read like one in the host's startup rather than hide in a settings file
    /// somebody copied between environments.
    /// </remarks>
    public static IServiceCollection UseOpenSearchListingIndex(this IServiceCollection services)
    {
        services.AddScoped<ISearchIndex, OpenSearchListingIndex>();
        return services;
    }
}
