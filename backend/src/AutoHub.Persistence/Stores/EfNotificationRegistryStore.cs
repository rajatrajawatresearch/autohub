using AutoHub.Platform.Notifications;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Stores;

/// <summary>
/// Loads notification rules, templates and subscriptions.
/// </summary>
/// <remarks>
/// Everything here is read on the dispatch path, which runs inside a workflow effect behind a domain
/// event. That is the worst place in the system for an N+1: an auction closing notifies a hundred
/// watchers, and a query per watcher turns one event into a hundred round trips while the lot's
/// close transaction is still open.
/// </remarks>
public sealed class EfNotificationRegistryStore : INotificationRegistryStore
{
    private readonly AutoHubDbContext _context;

    public EfNotificationRegistryStore(AutoHubDbContext context) => _context = context;

    /// <summary>
    /// Every active rule bound to one event type.
    /// </summary>
    /// <remarks>
    /// One domain event maps to many rules — <c>auction.bid_placed.v1</c> produces "you were outbid"
    /// to the previous high bidder and "new bid received" to the seller — so this returns a list and
    /// the registry evaluates each rule's condition and audience separately.
    /// </remarks>
    public async Task<IReadOnlyList<NotificationEvent>> GetActiveEventsAsync(
        string eventType,
        CancellationToken cancellationToken)
        => await _context.NotificationEvents
            .AsNoTracking()
            .Where(rule => rule.EventType == eventType && rule.IsActive)
            .OrderBy(rule => rule.Code)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// One template, exactly.
    /// </summary>
    /// <remarks>
    /// Variables travel with it. A template rendered without its declared variables cannot tell a
    /// missing required value from an optional one, and the difference decides whether the dispatch
    /// fails loudly or sends a message that reads "Hi {name}".
    /// <para>
    /// No fallback chain here: this returns the exact locale asked for, and the registry walks the
    /// chain by calling again. Resolving the fallback in SQL would need the locale table joined into
    /// every template read for a case that is a cache miss on a cache miss.
    /// </para>
    /// </remarks>
    public async Task<NotificationTemplate?> GetTemplateAsync(
        string key,
        NotificationChannel channel,
        string locale,
        CancellationToken cancellationToken)
        => await _context.NotificationTemplates
            .Include(nameof(NotificationTemplate.Variables))
            .AsSplitQuery()
            .AsNoTrackingWithIdentityResolution()
            .FirstOrDefaultAsync(
                template => template.Key == key
                    && template.Channel == channel
                    && template.Locale == locale
                    && template.IsActive,
                cancellationToken);

    /// <summary>
    /// Preferences for a batch of users, plus their user-level defaults.
    /// </summary>
    /// <remarks>
    /// The null-category rows are the defaults every category inherits, and they are fetched in the
    /// same statement rather than in a second pass — quiet hours and DND live on those rows, so a
    /// query that returned only the category rows would silently wake up every user who has never
    /// opened the settings screen for that category.
    /// </remarks>
    public async Task<IReadOnlyList<NotificationSubscription>> GetSubscriptionsAsync(
        IReadOnlyCollection<Guid> userIds,
        string categoryCode,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return [];
        }

        var ids = userIds.ToArray();

        return await _context.NotificationSubscriptions
            .Include(nameof(NotificationSubscription.Channels))
            .AsSplitQuery()
            .AsNoTrackingWithIdentityResolution()
            .Where(subscription =>
                ids.Contains(subscription.UserId)
                && (subscription.CategoryCode == categoryCode || subscription.CategoryCode == null))
            .ToListAsync(cancellationToken);
    }
}
