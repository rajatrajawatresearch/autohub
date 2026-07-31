using AutoHub.Platform.Localization;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Notifications;

/// <summary>
/// The cacheable form of a notification rule.
/// </summary>
/// <remarks>
/// Entities are not put in the registry cache, and the reason is concrete rather than stylistic:
/// <see cref="NotificationEvent"/> has private setters so that only its own methods can change it,
/// and <c>System.Text.Json</c> will not write through a private setter. Round-tripping the entity
/// through Redis would therefore return an object with every property at its default — an empty
/// template key, no channels, <c>Promotional</c> urgency — and the failure would look like "the rule
/// stopped matching" rather than "the cache is broken".
/// <para>
/// Projecting to an immutable record makes that impossible, and doubles as the boundary that keeps a
/// caller from mutating shared cached state.
/// </para>
/// </remarks>
public sealed record NotificationRuleSnapshot
{
    public required string Code { get; init; }

    public required string EventType { get; init; }

    public required string CategoryCode { get; init; }

    public required string TemplateKey { get; init; }

    public required AudienceKind AudienceKind { get; init; }

    public string? AudienceSelector { get; init; }

    public required IReadOnlyList<NotificationChannel> Channels { get; init; }

    public string? ConditionJson { get; init; }

    public required NotificationUrgency Urgency { get; init; }

    public int? DedupeWindowSeconds { get; init; }

    public string? DedupeKeyTemplate { get; init; }

    public int? ThrottleMaxPerWindow { get; init; }

    public int? ThrottleWindowSeconds { get; init; }

    public static NotificationRuleSnapshot From(NotificationEvent rule) => new()
    {
        Code = rule.Code,
        EventType = rule.EventType,
        CategoryCode = rule.CategoryCode,
        TemplateKey = rule.TemplateKey,
        AudienceKind = rule.Audience.Kind,
        AudienceSelector = rule.Audience.Selector,
        Channels = rule.Channels(),
        ConditionJson = rule.ConditionJson,
        Urgency = rule.Urgency,
        DedupeWindowSeconds = rule.DedupeWindowSeconds,
        DedupeKeyTemplate = rule.DedupeKeyTemplate,
        ThrottleMaxPerWindow = rule.ThrottleMaxPerWindow,
        ThrottleWindowSeconds = rule.ThrottleWindowSeconds,
    };

    public NotificationAudience Audience() => AudienceKind switch
    {
        AudienceKind.Actor => NotificationAudience.Actor(),
        AudienceKind.AggregateOwner => NotificationAudience.AggregateOwner(),
        AudienceKind.Participants => NotificationAudience.Participants(),
        AudienceKind.PermissionHolders =>
            NotificationAudience.PermissionHolders(AudienceSelector ?? string.Empty),
        AudienceKind.Segment => NotificationAudience.Segment(AudienceSelector ?? string.Empty),
        AudienceKind.ExplicitUsers => NotificationAudience.ExplicitUsers(ParseUserIds(AudienceSelector)),
        _ => NotificationAudience.Actor(),
    };

    private static IEnumerable<Guid> ParseUserIds(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            yield break;
        }

        foreach (var token in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Guid.TryParse(token, out var id))
            {
                yield return id;
            }
        }
    }
}

/// <summary>The cacheable form of a template, for the same reason as <see cref="NotificationRuleSnapshot"/>.</summary>
public sealed record NotificationTemplateSnapshot
{
    public required string Key { get; init; }

    public required NotificationChannel Channel { get; init; }

    public required string Locale { get; init; }

    public string? Subject { get; init; }

    public required string Body { get; init; }

    public string? ProviderTemplateId { get; init; }

    public string? SenderId { get; init; }

    public required IReadOnlyList<TemplateVariableSnapshot> Variables { get; init; }

    public static NotificationTemplateSnapshot From(NotificationTemplate template) => new()
    {
        Key = template.Key,
        Channel = template.Channel,
        Locale = template.Locale,
        Subject = template.Subject,
        Body = template.Body,
        ProviderTemplateId = template.ProviderTemplateId,
        SenderId = template.SenderId,
        Variables = template.Variables
            .Select(v => new TemplateVariableSnapshot(v.Name, v.IsRequired))
            .ToList(),
    };

    public Result<RenderedNotification> Render(IReadOnlyDictionary<string, string?> values)
        => NotificationRendering.Render(Key, Subject, Body, ProviderTemplateId, SenderId, Variables, values);
}

public sealed record TemplateVariableSnapshot(string Name, bool IsRequired);

/// <summary>
/// The single implementation of "fill a template", shared by the entity and its cached projection.
/// </summary>
/// <remarks>
/// Two copies of this would eventually disagree about what counts as a missing variable, and the
/// disagreement would only surface in production — where the cached projection runs — and never in
/// the unit tests, which exercise the entity.
/// </remarks>
public static class NotificationRendering
{
    public static Result<RenderedNotification> Render(
        string templateKey,
        string? subject,
        string body,
        string? providerTemplateId,
        string? senderId,
        IReadOnlyList<TemplateVariableSnapshot> declared,
        IReadOnlyDictionary<string, string?> values)
    {
        var missing = declared
            .Where(v => v.IsRequired)
            .Where(v => !values.TryGetValue(v.Name, out var value) || string.IsNullOrEmpty(value))
            .Select(v => v.Name)
            .ToList();

        if (missing.Count > 0)
        {
            return Error.Validation(
                "notification.template.variable_missing",
                "This notification is missing variables it requires.")
                .With("templateKey", templateKey)
                .With("missing", missing);
        }

        return new RenderedNotification(
            MessageTemplate.Render(subject, values),
            MessageTemplate.Render(body, values),
            providerTemplateId,
            senderId);
    }
}
