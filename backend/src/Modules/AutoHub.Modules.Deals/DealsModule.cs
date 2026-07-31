using System.Reflection;
using AutoHub.Modules.Deals.Application;
using AutoHub.Modules.Deals.Contracts;
using AutoHub.Platform.Access;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoHub.Modules.Deals;

/// <summary>
/// Registers the deals module. The repository is supplied by the persistence layer.
/// </summary>
/// <remarks>
/// <c>IDealRepository</c> is deliberately not registered here: this project never references EF Core
/// and must not, or the module stops being extractable. The host registers the persistence
/// implementation, and a missing registration is a startup failure with a type name rather than a
/// null at first request.
/// </remarks>
public static class DealsModule
{
    public static IServiceCollection AddDealsModule(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services.TryAddScoped<IDealGateway, DealGateway>();

        // Enumerable rather than TryAdd: every module contributes its permissions, and the catalog
        // throws at startup if two modules claim the same code.
        services.AddSingleton<IPermissionContributor, DealPermissionContributor>();

        return services;
    }
}
