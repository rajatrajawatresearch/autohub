using System.Text.Json.Nodes;
using AutoHub.Platform.Caching;
using AutoHub.Platform.Expressions;
using AutoHub.Platform.Localization;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoHub.Platform.Notifications;

/// <summary>
/// Resolves domain events into dispatches. Registry 7's read side.
/// </summary>
public sealed class NotificationRegistry : INotificationRegistry
{
    private readonly INotificationRegistryStore _store;
    private readonly IReadOnlyList<INotificationAudienceResolver> _audienceResolvers;
    private readonly INotificationSuppressionStore _suppression;
    private readonly ILocaleChainResolver _locales;
    private readonly IRegistryCache _cache;
    private readonly IClock _clock;
    private readonly NotificationOptions _options;
    private readonly ILogger<NotificationRegistry> _logger;

    public NotificationRegistry(
        INotificationRegistryStore store,
        IEnumerable<INotificationAudienceResolver> audienceResolvers,
        INotificationSuppressionStore suppression,
        ILocaleChainResolver locales,
        IRegistryCache cache,
        IClock clock,
        IOptions<NotificationOptions> options,
        ILogger<NotificationRegistry> logger)
    {
        _store = store;

        // Longest prefix first, so a resolver registered for "auction" outranks the catch-all
        // without the registration order in Program.cs deciding behaviour.
        _audienceResolvers = audienceResolvers
            .OrderByDescending(r => r.EventTypePrefix?.Length ?? 0)
            .ToList();

        _suppression = suppression;
        _locales = locales;
        _cache = cache;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    public Task<Result<IReadOnlyList<NotificationDispatch>>> ResolveAsync(
        IDomainEvent domainEvent,
        JsonNode? payload,
        CancellationToken cancellationToken)
        => ResolveAsync(
            new NotificationResolutionRequest
            {
                EventType = domainEvent.EventType,
                AggregateId = domainEvent.AggregateId,
                OccurredAt = domainEvent.OccurredAt,
                Payload = payload,
            },
            cancellationToken);

    public async Task<Result<IReadOnlyList<NotificationDispatch>>> ResolveAsync(
        NotificationResolutionRequest request,
        CancellationToken cancellationToken)
    {
        var rules = await GetRulesAsync(request.EventType, cancellationToken);

        if (rules.Count == 0)
        {
            // Not an error. Most domain events notify nobody, and treating "no rule" as a failure
            // would make the outbox dispatcher log an error for every ledger posting on the platform.
            return Result.Success<IReadOnlyList<NotificationDispatch>>([]);
        }

        var variables = FlattenPayload(request.Payload);
        var dispatches = new List<NotificationDispatch>();

        foreach (var rule in rules)
        {
            var matched = await ResolveRuleAsync(rule, request, variables, cancellationToken);
            dispatches.AddRange(matched);
        }

        return dispatches;
    }

    private async Task<IReadOnlyList<NotificationDispatch>> ResolveRuleAsync(
        NotificationRuleSnapshot rule,
        NotificationResolutionRequest request,
        IReadOnlyDictionary<string, string?> variables,
        CancellationToken cancellationToken)
    {
        var condition = ConditionExpression.FromJson(rule.ConditionJson);
        if (condition.IsFailure)
        {
            // An unparseable condition must not notify. The safe direction for a message is "do not
            // send" — the alternative is telling every dealer on the platform something under a rule
            // nobody authored.
            _logger.LogError(
                "Notification rule {RuleCode} has an unparseable condition and was skipped",
                rule.Code);

            return [];
        }

        if (!condition.Value.Evaluate(request.Payload?.DeepClone() ?? new JsonObject()))
        {
            return [];
        }

        var recipients = await ResolveAudienceAsync(rule, request, cancellationToken);
        if (recipients.Count == 0)
        {
            return [];
        }

        var subscriptions = await _store.GetSubscriptionsAsync(
            recipients.Select(r => r.UserId).Distinct().ToList(),
            rule.CategoryCode,
            cancellationToken);

        var byUser = subscriptions.ToLookup(s => s.UserId);
        var dispatches = new List<NotificationDispatch>();

        foreach (var recipient in recipients)
        {
            foreach (var channel in rule.Channels)
            {
                var dispatch = await ResolveChannelAsync(
                    rule, request, recipient, channel, byUser[recipient.UserId].ToList(), variables, cancellationToken);

                if (dispatch is not null)
                {
                    dispatches.Add(dispatch);
                }
            }
        }

        return dispatches;
    }

    private async Task<NotificationDispatch?> ResolveChannelAsync(
        NotificationRuleSnapshot rule,
        NotificationResolutionRequest request,
        NotificationRecipient recipient,
        NotificationChannel channel,
        IReadOnlyList<NotificationSubscription> subscriptions,
        IReadOnlyDictionary<string, string?> variables,
        CancellationToken cancellationToken)
    {
        if (recipient.UnreachableChannels.Contains(channel))
        {
            return null;
        }

        var consent = EvaluateConsent(rule, channel, subscriptions);
        if (consent.IsSuppressed)
        {
            return null;
        }

        var template = await ResolveTemplateAsync(rule.TemplateKey, channel, recipient.Locale, cancellationToken);
        if (template is null)
        {
            // A rule naming a template that does not exist on this channel is a configuration gap,
            // not a reason to abandon the other channels this recipient can still be reached on.
            _logger.LogWarning(
                "No active {Channel} template '{TemplateKey}' for locale {Locale}; rule {RuleCode} skipped this channel",
                channel, rule.TemplateKey, recipient.Locale ?? _options.DefaultLocale, rule.Code);

            return null;
        }

        var rendered = template.Render(variables);
        if (rendered.IsFailure)
        {
            _logger.LogError(
                "Notification rule {RuleCode} could not render {TemplateKey} on {Channel}: {Code}",
                rule.Code, rule.TemplateKey, channel, rendered.Error.Code);

            return null;
        }

        var dedupeKey = BuildDedupeKey(rule, recipient, channel, variables);

        if (!request.IsPreview && !await PassesSuppressionAsync(rule, recipient, dedupeKey, cancellationToken))
        {
            return null;
        }

        return new NotificationDispatch
        {
            EventCode = rule.Code,
            Recipient = recipient,
            Channel = channel,
            TemplateKey = rule.TemplateKey,
            Locale = template.Locale,
            Subject = string.IsNullOrEmpty(rendered.Value.Subject) ? null : rendered.Value.Subject,
            Body = rendered.Value.Body,
            ProviderTemplateId = rendered.Value.ProviderTemplateId,
            SenderId = rendered.Value.SenderId,
            Variables = variables,
            DedupeKey = dedupeKey,
            Urgency = rule.Urgency,
            DeferUntil = consent.DeferUntil,
        };
    }

    /// <summary>
    /// Applies the user's preferences, their DND setting, and their quiet hours.
    /// </summary>
    /// <remarks>
    /// The urgency ladder is where regulation meets product. TRAI's DND framework exempts
    /// transactional traffic, and it is right to: a user who set DND last month and is right now
    /// staring at an OTP screen has not consented to being locked out of their own account. So
    /// <see cref="NotificationUrgency.Transactional"/> and <see cref="NotificationUrgency.Critical"/>
    /// bypass both DND and quiet hours, and everything else respects them.
    /// <para>
    /// Quiet hours <b>defer</b> rather than suppress, while DND <b>suppresses</b>. That asymmetry is
    /// deliberate: quiet hours say "not now", which has an obvious answer — send at 07:00 — whereas
    /// DND says "not at all", and a queue that empties itself the moment DND is switched off would be
    /// the opposite of what was asked for.
    /// </para>
    /// </remarks>
    private ConsentDecision EvaluateConsent(
        NotificationRuleSnapshot rule,
        NotificationChannel channel,
        IReadOnlyList<NotificationSubscription> subscriptions)
    {
        // Category-specific rows win over the user-level default; that is the whole point of having
        // both, and picking the wrong one silently ignores a preference the user explicitly set.
        var specific = subscriptions.FirstOrDefault(s => s.CategoryCode is not null);
        var fallback = subscriptions.FirstOrDefault(s => s.CategoryCode is null);

        var allowed = specific?.Allows(channel)
                      ?? fallback?.Allows(channel)
                      ?? _options.DefaultEnabledChannels.Contains(channel);

        if (!allowed)
        {
            return ConsentDecision.Suppress;
        }

        if (!channel.IsIntrusive() || rule.Urgency >= NotificationUrgency.Transactional)
        {
            return ConsentDecision.SendNow;
        }

        if (specific?.DoNotDisturb == true || fallback?.DoNotDisturb == true)
        {
            return ConsentDecision.Suppress;
        }

        var quietHours = specific?.QuietHours ?? fallback?.QuietHours;
        if (quietHours is null)
        {
            return ConsentDecision.SendNow;
        }

        var now = _clock.UtcNow;
        return quietHours.Contains(now)
            ? ConsentDecision.Defer(quietHours.NextOpening(now))
            : ConsentDecision.SendNow;
    }

    /// <summary>
    /// Dedupe first, then throttle. Both reserve atomically so two nodes cannot both decide to send.
    /// </summary>
    private async Task<bool> PassesSuppressionAsync(
        NotificationRuleSnapshot rule,
        NotificationRecipient recipient,
        string dedupeKey,
        CancellationToken cancellationToken)
    {
        if (rule.DedupeWindowSeconds is { } dedupeSeconds and > 0)
        {
            var reserved = await _suppression.TryReserveAsync(
                dedupeKey, TimeSpan.FromSeconds(dedupeSeconds), cancellationToken);

            if (!reserved)
            {
                _logger.LogDebug("Suppressed duplicate {RuleCode} for {UserId}", rule.Code, recipient.UserId);
                return false;
            }
        }

        if (rule.ThrottleMaxPerWindow is { } max and > 0 &&
            rule.ThrottleWindowSeconds is { } windowSeconds and > 0)
        {
            var sent = await _suppression.CountAndIncrementAsync(
                $"throttle:{rule.Code}:{recipient.UserId}",
                TimeSpan.FromSeconds(windowSeconds),
                cancellationToken);

            if (sent > max)
            {
                _logger.LogDebug("Throttled {RuleCode} for {UserId} at {Sent}", rule.Code, recipient.UserId, sent);
                return false;
            }
        }

        return true;
    }

    private async Task<IReadOnlyList<NotificationRecipient>> ResolveAudienceAsync(
        NotificationRuleSnapshot rule,
        NotificationResolutionRequest request,
        CancellationToken cancellationToken)
    {
        var resolver = _audienceResolvers.FirstOrDefault(r =>
            r.EventTypePrefix is null ||
            request.EventType.StartsWith(r.EventTypePrefix, StringComparison.Ordinal));

        if (resolver is null)
        {
            _logger.LogWarning(
                "No audience resolver serves {EventType}; rule {RuleCode} reached nobody",
                request.EventType, rule.Code);

            return [];
        }

        var recipients = await resolver.ResolveAsync(rule.Audience(), request, cancellationToken);

        // Distinct by user: an actor who also owns the aggregate would otherwise be told twice by
        // one rule, which reads as a bug to the person receiving it.
        return recipients
            .GroupBy(r => r.UserId)
            .Select(g => g.First())
            .ToList();
    }

    /// <summary>
    /// Finds the best template for a recipient's language, walking the i18n fallback chain.
    /// </summary>
    /// <remarks>
    /// Uses the localisation registry's chain rather than its own, so a Marathi speaker gets the
    /// Hindi template when Marathi is missing — the same answer the UI would give them — instead of
    /// dropping straight to English because two registries disagreed about what "close enough" means.
    /// </remarks>
    private async Task<NotificationTemplateSnapshot?> ResolveTemplateAsync(
        string templateKey,
        NotificationChannel channel,
        string? locale,
        CancellationToken cancellationToken)
    {
        var chain = await _locales.ResolveChainAsync(locale ?? _options.DefaultLocale, cancellationToken);

        foreach (var candidate in chain)
        {
            var template = await _cache.GetOrLoadAsync(
                RegistryNames.Notifications,
                $"template:{templateKey}:{channel}:{candidate}",
                async token =>
                {
                    var loaded = await _store.GetTemplateAsync(templateKey, channel, candidate, token);
                    return loaded is { IsActive: true } ? NotificationTemplateSnapshot.From(loaded) : null;
                },
                cancellationToken);

            if (template is not null)
            {
                return template;
            }
        }

        return null;
    }

    private async Task<IReadOnlyList<NotificationRuleSnapshot>> GetRulesAsync(
        string eventType,
        CancellationToken cancellationToken)
        => await _cache.GetOrLoadRequiredAsync<IReadOnlyList<NotificationRuleSnapshot>>(
            RegistryNames.Notifications,
            $"rules:{eventType}",
            async token =>
            {
                var rules = await _store.GetActiveEventsAsync(eventType, token);
                return rules.Select(NotificationRuleSnapshot.From).ToList();
            },
            cancellationToken);

    private static string BuildDedupeKey(
        NotificationRuleSnapshot rule,
        NotificationRecipient recipient,
        NotificationChannel channel,
        IReadOnlyDictionary<string, string?> variables)
    {
        var discriminator = MessageTemplate.Render(rule.DedupeKeyTemplate, variables);
        return $"dedupe:{rule.Code}:{recipient.UserId}:{channel}:{discriminator}";
    }

    /// <summary>
    /// Flattens the event payload into the flat <c>{name}</c> namespace templates author against.
    /// </summary>
    /// <remarks>
    /// Nested objects become dotted paths — <c>{vehicle.title}</c> — because that is what an author
    /// sees in the payload sample the admin editor shows them, and a second naming scheme would be a
    /// second thing to get wrong.
    /// <para>
    /// Numbers are stringified verbatim under the invariant culture and are <b>not</b> formatted.
    /// Money in particular arrives as minor units, and "515000" is not a message anyone should send;
    /// the raising module is expected to include a display-ready value (<c>amountDisplay</c>)
    /// alongside the machine one. Formatting depends on the reader's locale and numbering
    /// convention — ₹5,15,000 groups differently from $515,000 — and the north star puts that at the
    /// edge, not in the domain.
    /// </para>
    /// </remarks>
    private static Dictionary<string, string?> FlattenPayload(JsonNode? payload)
    {
        var flat = new Dictionary<string, string?>(StringComparer.Ordinal);

        if (payload is not null)
        {
            Flatten(payload, prefix: string.Empty, flat);
        }

        return flat;
    }

    private static void Flatten(JsonNode node, string prefix, Dictionary<string, string?> into)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var (key, child) in obj)
                {
                    var path = prefix.Length == 0 ? key : $"{prefix}.{key}";

                    if (child is null)
                    {
                        into[path] = null;
                        continue;
                    }

                    Flatten(child, path, into);
                }

                break;

            case JsonArray array:
                // Arrays are joined rather than indexed. A template that wants item three of a list
                // is a template that will break the day the list has two items, and there is no
                // sensible message to send in that case.
                into[prefix] = string.Join(
                    ", ",
                    array.Where(item => item is not null).Select(item => item!.ToString()));
                break;

            case JsonValue value:
                into[prefix] = value.TryGetValue<string>(out var text)
                    ? text
                    : value.ToJsonString().Trim('"');
                break;

            default:
                into[prefix] = node.ToString();
                break;
        }
    }

    /// <summary>Send now, hold until the quiet window ends, or do not send at all.</summary>
    private readonly record struct ConsentDecision(bool IsSuppressed, DateTimeOffset? DeferUntil)
    {
        public static ConsentDecision SendNow => new(false, null);

        public static ConsentDecision Suppress => new(true, null);

        public static ConsentDecision Defer(DateTimeOffset until) => new(false, until);
    }
}

public sealed class NotificationOptions
{
    public string DefaultLocale { get; set; } = "en-IN";

    /// <summary>
    /// What a user who has never opened notification settings receives.
    /// </summary>
    /// <remarks>
    /// In-app and push are on because a marketplace with silent notifications is a marketplace whose
    /// users miss their own auctions. SMS and WhatsApp are off by default because each one costs
    /// money per message and, under TCCCPR, needs consent we can point to — a default that opts
    /// everyone into paid channels is both a bill and a compliance finding.
    /// </remarks>
    public IReadOnlySet<NotificationChannel> DefaultEnabledChannels { get; set; } =
        new HashSet<NotificationChannel> { NotificationChannel.InApp, NotificationChannel.Push };
}

/// <summary>
/// Suppression counters held in this process.
/// </summary>
/// <remarks>
/// Registered so the API boots without Redis. On a fleet it under-suppresses — each node keeps its
/// own counters, so a rule throttled to three per hour can send three per hour <i>per node</i>.
/// That is the right failure for a fallback (users get more messages, never fewer than intended and
/// never a wrong one), but it is a fallback: production binds the Redis implementation.
/// </remarks>
public sealed class InMemoryNotificationSuppressionStore : INotificationSuppressionStore
{
    private readonly Dictionary<string, Reservation> _entries = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();
    private readonly IClock _clock;

    public InMemoryNotificationSuppressionStore(IClock clock) => _clock = clock;

    public Task<bool> TryReserveAsync(string dedupeKey, TimeSpan window, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        // A plain lock rather than a ConcurrentDictionary: "read the count, decide, then write" has
        // to be one indivisible step, and AddOrUpdate's factory may run more than once under
        // contention — which would hand two callers the same reservation and send the message twice.
        lock (_gate)
        {
            Evict(now);

            if (_entries.TryGetValue(dedupeKey, out var existing) && existing.ExpiresAt > now)
            {
                return Task.FromResult(false);
            }

            _entries[dedupeKey] = new Reservation(now + window, 1);
            return Task.FromResult(true);
        }
    }

    public Task<int> CountAndIncrementAsync(string throttleKey, TimeSpan window, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        lock (_gate)
        {
            Evict(now);

            var count = _entries.TryGetValue(throttleKey, out var existing) && existing.ExpiresAt > now
                ? existing.Count + 1
                : 1;

            _entries[throttleKey] = new Reservation(
                count == 1 ? now + window : existing.ExpiresAt,
                count);

            return Task.FromResult(count);
        }
    }

    /// <summary>
    /// Drops expired rows on the way past. Without it this dictionary is a slow memory leak keyed by
    /// user id — unbounded, and worst on the busiest days.
    /// </summary>
    private void Evict(DateTimeOffset now)
    {
        if (_entries.Count < EvictionThreshold)
        {
            return;
        }

        foreach (var key in _entries.Where(e => e.Value.ExpiresAt <= now).Select(e => e.Key).ToList())
        {
            _entries.Remove(key);
        }
    }

    private const int EvictionThreshold = 4096;

    private readonly record struct Reservation(DateTimeOffset ExpiresAt, int Count);
}

/// <summary>
/// Resolves nothing. Registered last so the API boots before any module supplies a real resolver.
/// </summary>
/// <remarks>
/// Returning an empty list rather than throwing is deliberate: a half-wired development environment
/// should log "reached nobody" and carry on, not fail the transaction that raised the event.
/// </remarks>
public sealed class EmptyNotificationAudienceResolver : INotificationAudienceResolver
{
    public string? EventTypePrefix => null;

    public Task<IReadOnlyList<NotificationRecipient>> ResolveAsync(
        NotificationAudience audience,
        NotificationResolutionRequest request,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<NotificationRecipient>>([]);
}
