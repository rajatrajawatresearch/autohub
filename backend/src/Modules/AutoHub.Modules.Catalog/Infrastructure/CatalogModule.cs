using AutoHub.Modules.Catalog.Application.GetVehicle;
using AutoHub.Modules.Catalog.Application.ResolveOrCreateVehicle;
using AutoHub.Modules.Catalog.Application.SearchVehicles;
using AutoHub.Modules.Catalog.Application.UpdateVehicleAttributes;
using AutoHub.Modules.Catalog.Contracts;
using AutoHub.Platform.Access;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoHub.Modules.Catalog.Infrastructure;

/// <summary>
/// Everything the catalog module needs from the container, in one place.
/// </summary>
/// <remarks>
/// The module registers its own handlers, validators and contract implementation; the host registers
/// the repository, because the repository is EF and EF lives in the persistence layer. That split is
/// the same one <c>ITaxonomyStore</c> makes, and it is what keeps this project free of a database
/// dependency it would otherwise carry into every unit test.
/// <para>
/// Validators are registered by hand rather than by assembly scanning. Three lines of explicit
/// registration fail at startup with a name when one is missing; a scan that silently finds nothing
/// fails at the first request with a resolution error, and only for the one command nobody tested.
/// </para>
/// </remarks>
public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssemblyContaining<ResolveOrCreateVehicleCommand>());

        services.AddScoped<IValidator<ResolveOrCreateVehicleCommand>, ResolveOrCreateVehicleValidator>();
        services.AddScoped<IValidator<UpdateVehicleAttributesCommand>, UpdateVehicleAttributesValidator>();
        services.AddScoped<IValidator<SearchVehiclesQuery>, SearchVehiclesValidator>();

        // Registered as a concrete type as well as a handler because two other handlers compose one
        // vehicle description through it. Sending a MediatR query to do that would re-run the whole
        // pipeline per row, which on a twenty-four-result page is twenty-four times the overhead for
        // a method call.
        services.AddScoped<GetVehicleHandler>();

        services.TryAddScoped<IVehicleTaxonomy, VehicleTaxonomy>();
        services.TryAddScoped<IVehicleDirectory, VehicleDirectory>();

        services.AddSingleton<IPermissionContributor, CatalogPermissionContributor>();

        return services;
    }
}
