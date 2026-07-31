using System.Reflection;
using AutoHub.Modules.Bookings.Domain;
using AutoHub.Platform.Access;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoHub.Modules.Bookings;

/// <summary>
/// Registers the bookings module.
/// </summary>
/// <remarks>
/// <see cref="IResourceDayLock"/> is not registered here and has no default. The lock is a PostgreSQL
/// advisory lock that must run on the ambient transaction, so only the persistence layer can supply
/// it — and a no-op default would be a double-booking guard that silently guards nothing. A host that
/// forgets it fails to construct the handler, loudly, at startup.
/// </remarks>
public static class BookingsModule
{
    public static IServiceCollection AddBookingsModule(
        this IServiceCollection services,
        string handoverSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handoverSecret);

        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // Singleton because the hasher holds a key and no per-request state. The secret comes from
        // configuration; there is deliberately no overload that defaults it, because a default
        // handover secret is a handover secret every deployment shares.
        services.TryAddSingleton<IHandoverOtp>(_ => new HandoverOtp(handoverSecret));

        services.AddSingleton<IPermissionContributor, BookingPermissionContributor>();

        return services;
    }
}
