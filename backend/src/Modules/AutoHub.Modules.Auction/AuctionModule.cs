using System.Reflection;
using AutoHub.Modules.Auction.Application;
using AutoHub.Modules.Auction.Contracts;
using AutoHub.Modules.Auction.Domain;
using AutoHub.Modules.Auction.Infrastructure;
using AutoHub.Platform.Access;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoHub.Modules.Auction;

/// <summary>
/// Registers the auction module.
/// </summary>
/// <remarks>
/// <see cref="IBidGate"/> is registered as the Redis implementation and there is deliberately no
/// in-memory fallback. A fallback would be a bid gate with no atomicity guarantee that looks like one
/// that has it, and the first time it ran under load two dealers would win the same lot. A host
/// without Redis must fail to start rather than run auctions incorrectly.
/// <para>
/// The broadcaster and the event journal do have safe defaults, because the failure mode is
/// different in kind: a lost broadcast costs a client a refresh, and both defaults are registered
/// with <c>TryAdd</c> so a host with SignalR and a durable journal replaces them.
/// </para>
/// </remarks>
public static class AuctionModule
{
    public static IServiceCollection AddAuctionModule(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services.TryAddScoped<IBidGate, RedisBidGate>();
        services.TryAddScoped<IBidIncrementPolicy, BidIncrementPolicy>();

        services.TryAddSingleton<IAuctionEventJournal, InMemoryAuctionEventJournal>();
        services.TryAddSingleton<IAuctionBroadcaster, NoOpAuctionBroadcaster>();

        services.AddSingleton<IPermissionContributor, AuctionPermissionContributor>();

        return services;
    }
}

/// <summary>
/// A replay journal that keeps a bounded window of events per lot in memory.
/// </summary>
/// <remarks>
/// Enough to make <c>?sinceSeq=</c> work on a single node during development, and explicitly not
/// enough for production: it is per node, so a client that reconnects to a different pod gets
/// nothing, and it is lost on restart. The production implementation is a Redis stream per lot,
/// trimmed to the lot's lifetime, which survives both.
/// <para>
/// The window is bounded because a busy lot in its final minute produces hundreds of events and an
/// unbounded per-lot list is a memory leak with a fixed end date — the auction's — which is exactly
/// the kind of leak that is never noticed until the sale that is twice as busy as usual.
/// </para>
/// </remarks>
public sealed class InMemoryAuctionEventJournal : IAuctionEventJournal
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, List<AuctionRealtimeEvent>> _events = new();

    private const int WindowPerLot = 500;

    public Task AppendAsync(AuctionRealtimeEvent realtimeEvent, CancellationToken cancellationToken)
    {
        var window = _events.GetOrAdd(realtimeEvent.LotId, _ => []);

        lock (window)
        {
            window.Add(realtimeEvent);

            if (window.Count > WindowPerLot)
            {
                window.RemoveRange(0, window.Count - WindowPerLot);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuctionRealtimeEvent>> ReplayAsync(
        Guid lotId,
        long sinceSeq,
        CancellationToken cancellationToken)
    {
        if (!_events.TryGetValue(lotId, out var window))
        {
            return Task.FromResult<IReadOnlyList<AuctionRealtimeEvent>>([]);
        }

        lock (window)
        {
            return Task.FromResult<IReadOnlyList<AuctionRealtimeEvent>>(
                window.Where(e => e.Seq > sinceSeq).OrderBy(e => e.Seq).ToList());
        }
    }
}

/// <summary>
/// Drops broadcasts on the floor so the module runs without a socket server.
/// </summary>
/// <remarks>
/// Safe precisely because the journal is written first and the client can replay: with no
/// broadcaster, a lot page polls and still converges. A no-op <i>gate</i> would not be safe, which is
/// why one does not exist.
/// </remarks>
public sealed class NoOpAuctionBroadcaster : IAuctionBroadcaster
{
    public Task PublishAsync(AuctionRealtimeEvent realtimeEvent, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
