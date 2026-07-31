using System.Text.Json.Nodes;
using AutoHub.Api.Application.Behaviours;
using AutoHub.Persistence;
using AutoHub.Platform.Access;
using AutoHub.Platform.Caching;
using AutoHub.Platform.Features;
using AutoHub.Platform.Localization;
using AutoHub.Platform.Notifications;
using AutoHub.SharedKernel.Results;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Module = AutoHub.Platform.Features.Module;

namespace AutoHub.Api.Application.RegistryAdmin;

/// <summary>The write side of registries 6, 7 and 9 — flags, templates and translations.</summary>
/// <remarks>
/// Grouped in one file because all three are the same shape of write: upsert a row, then bump a
/// version. None of them has a draft-and-publish lifecycle, and that is a deliberate difference from
/// workflows, forms and pricing. Those three are immutable once published because in-flight entities
/// are pinned to a version; a translation, a flag rule and a message template have no in-flight
/// entities — they are evaluated fresh on every read — so versioning them would be ceremony that
/// buys nothing and makes fixing a typo in a Marathi string a four-step process.
/// </remarks>
public sealed record UpsertFlagBody(
    string Key,
    bool DefaultValue,
    string? Description,
    IReadOnlyList<FlagRuleBody>? Rules);

public sealed record FlagRuleBody(int Priority, JsonNode? Condition, bool Value, string? Description);

public sealed record UpsertModuleBody(string Code, string Name, bool Enabled, string? Description, string? GatingFlagKey);

public sealed record UpsertI18nBody(string Namespace, string Locale, IReadOnlyDictionary<string, string> Resources);

public sealed record UpsertTemplateBody(
    string Key,
    string Channel,
    string Locale,
    string Body,
    string? Subject,
    string? ProviderTemplateId,
    string? SenderId,
    bool Activate);

internal sealed record UpsertFlagCommand(UpsertFlagBody Body)
    : IRequest<Result<RegistryWriteResponse>>, ITransactionalRequest;

internal sealed record UpsertModuleCommand(UpsertModuleBody Body)
    : IRequest<Result<RegistryWriteResponse>>, ITransactionalRequest;

internal sealed record UpsertI18nCommand(UpsertI18nBody Body)
    : IRequest<Result<RegistryWriteResponse>>, ITransactionalRequest;

internal sealed record UpsertTemplateCommand(UpsertTemplateBody Body)
    : IRequest<Result<RegistryWriteResponse>>, ITransactionalRequest;

/// <summary>Forces a registry cache rebuild without changing anything. Diagnostic, and audited.</summary>
internal sealed record InvalidateRegistryCommand(string Registry)
    : IRequest<Result<RegistryWriteResponse>>, ITransactionalRequest;

internal sealed class UpsertFlagValidator : AbstractValidator<UpsertFlagCommand>
{
    public UpsertFlagValidator()
        => RuleFor(command => command.Body.Key).NotEmpty().WithErrorCode("feature.flag.key_required");
}

internal sealed class UpsertI18nValidator : AbstractValidator<UpsertI18nCommand>
{
    public UpsertI18nValidator()
    {
        RuleFor(command => command.Body.Locale).NotEmpty().WithErrorCode("i18n.locale_required");
        RuleFor(command => command.Body.Namespace).NotEmpty().WithErrorCode("i18n.namespace_required");
        RuleFor(command => command.Body.Resources).NotEmpty().WithErrorCode("i18n.resources_required");
    }
}

internal sealed class ContentAdminHandler
    : IRequestHandler<UpsertFlagCommand, Result<RegistryWriteResponse>>,
      IRequestHandler<UpsertModuleCommand, Result<RegistryWriteResponse>>,
      IRequestHandler<UpsertI18nCommand, Result<RegistryWriteResponse>>,
      IRequestHandler<UpsertTemplateCommand, Result<RegistryWriteResponse>>,
      IRequestHandler<InvalidateRegistryCommand, Result<RegistryWriteResponse>>
{
    private readonly AutoHubDbContext _context;
    private readonly RegistryPublisher _publisher;

    public ContentAdminHandler(AutoHubDbContext context, RegistryPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task<Result<RegistryWriteResponse>> Handle(
        UpsertFlagCommand command,
        CancellationToken cancellationToken)
    {
        var body = command.Body;

        var flag = await _context.FeatureFlags
            .Include(nameof(FeatureFlag.Rules))
            .FirstOrDefaultAsync(item => item.Key == body.Key, cancellationToken);

        JsonNode? before = flag is null ? null : Describe(flag);

        if (flag is null)
        {
            var created = FeatureFlag.Create(body.Key, body.DefaultValue, body.Description);
            if (created.IsFailure)
            {
                return Result.Failure<RegistryWriteResponse>(created.Error);
            }

            flag = created.Value;
            _context.FeatureFlags.Add(flag);
        }
        else
        {
            flag.SetDefault(body.DefaultValue);
            flag.Describe(body.Description);
        }

        if (body.Rules is not null)
        {
            // Rules are replaced wholesale rather than merged. Targeting is read as an ordered list
            // by whoever authored it, and a partial update that leaves an old higher-priority rule in
            // place produces a flag that ignores the change nobody can see.
            foreach (var existing in flag.Rules.ToList())
            {
                flag.RemoveRule(existing.Id);
            }

            foreach (var rule in body.Rules)
            {
                var added = flag.AddRule(new FlagRule(
                    rule.Priority, rule.Condition?.ToJsonString(), rule.Value, rule.Description));

                if (added.IsFailure)
                {
                    return Result.Failure<RegistryWriteResponse>(added.Error);
                }
            }
        }

        var version = await _publisher.PublishAsync(
            RegistryNames.Features,
            PlatformPermissions.FeaturesManage,
            "platform.feature_flag",
            flag.Key,
            flag.Id,
            $"Updated flag '{flag.Key}'.",
            before,
            Describe(flag),
            cancellationToken);

        return new RegistryWriteResponse(RegistryNames.Features, version, flag.Key, flag.Id);
    }

    public async Task<Result<RegistryWriteResponse>> Handle(
        UpsertModuleCommand command,
        CancellationToken cancellationToken)
    {
        var body = command.Body;

        var module = await _context.Modules
            .FirstOrDefaultAsync(item => item.Code == body.Code, cancellationToken);

        JsonNode? before = module is null
            ? null
            : new JsonObject { ["code"] = module.Code, ["enabled"] = module.IsEnabled };

        if (module is null)
        {
            var created = Module.Create(body.Code, body.Name, body.Enabled, body.Description);
            if (created.IsFailure)
            {
                return Result.Failure<RegistryWriteResponse>(created.Error);
            }

            module = created.Value;
            _context.Modules.Add(module);
        }
        else if (body.Enabled)
        {
            module.Enable();
        }
        else
        {
            module.Disable();
        }

        var gated = module.GateBehind(body.GatingFlagKey);
        if (gated.IsFailure)
        {
            return Result.Failure<RegistryWriteResponse>(gated.Error);
        }

        var version = await _publisher.PublishAsync(
            RegistryNames.Features,
            PlatformPermissions.FeaturesManage,
            "platform.module",
            module.Code,
            module.Id,
            $"{(body.Enabled ? "Enabled" : "Disabled")} module '{module.Code}'.",
            before,
            new JsonObject { ["code"] = module.Code, ["enabled"] = module.IsEnabled },
            cancellationToken);

        return new RegistryWriteResponse(RegistryNames.Features, version, module.Code, module.Id);
    }

    public async Task<Result<RegistryWriteResponse>> Handle(
        UpsertI18nCommand command,
        CancellationToken cancellationToken)
    {
        var body = command.Body;

        var existing = await _context.I18nResources
            .Where(resource => resource.Namespace == body.Namespace && resource.Locale == body.Locale)
            .ToDictionaryAsync(resource => resource.Key, StringComparer.Ordinal, cancellationToken);

        var created = 0;
        var updated = 0;

        foreach (var (key, value) in body.Resources)
        {
            if (existing.TryGetValue(key, out var resource))
            {
                var result = resource.Update(value);
                if (result.IsFailure)
                {
                    return Result.Failure<RegistryWriteResponse>(result.Error);
                }

                resource.Activate();
                updated++;
                continue;
            }

            var made = I18nResource.Create(body.Namespace, key, body.Locale, value);
            if (made.IsFailure)
            {
                return Result.Failure<RegistryWriteResponse>(made.Error);
            }

            _context.I18nResources.Add(made.Value);
            created++;
        }

        var version = await _publisher.PublishAsync(
            RegistryNames.I18n,
            PlatformPermissions.I18nPublish,
            "platform.i18n_resource",
            $"{body.Namespace}:{body.Locale}",
            entityId: null,
            $"Published {created} new and {updated} updated strings for {body.Locale}/{body.Namespace}.",
            before: null,
            after: new JsonObject
            {
                ["locale"] = body.Locale,
                ["namespace"] = body.Namespace,
                ["created"] = created,
                ["updated"] = updated,
            },
            cancellationToken);

        return new RegistryWriteResponse(RegistryNames.I18n, version, $"{body.Namespace}:{body.Locale}", null);
    }

    public async Task<Result<RegistryWriteResponse>> Handle(
        UpsertTemplateCommand command,
        CancellationToken cancellationToken)
    {
        var body = command.Body;

        if (!Enum.TryParse<NotificationChannel>(body.Channel, ignoreCase: true, out var channel))
        {
            return Error.Validation(
                "notification.channel_unknown",
                $"'{body.Channel}' is not a supported channel.")
                .With("channel", body.Channel);
        }

        var template = await _context.NotificationTemplates.FirstOrDefaultAsync(
            item => item.Key == body.Key && item.Channel == channel && item.Locale == body.Locale,
            cancellationToken);

        JsonNode? before = template is null
            ? null
            : new JsonObject { ["key"] = template.Key, ["isActive"] = template.IsActive };

        if (template is null)
        {
            template = NotificationTemplate.Draft(body.Key, channel, body.Locale, body.Body, body.Subject);
            _context.NotificationTemplates.Add(template);
        }

        if (!string.IsNullOrWhiteSpace(body.ProviderTemplateId))
        {
            // DLT registration is a legal precondition in India, not a nicety: an unregistered
            // sender id is silently dropped by the operator and the user simply never gets the SMS.
            template.RegisterWithProvider(body.ProviderTemplateId, body.SenderId);
        }

        if (body.Activate)
        {
            var activated = template.Activate();
            if (activated.IsFailure)
            {
                return Result.Failure<RegistryWriteResponse>(activated.Error);
            }
        }
        else
        {
            template.Deactivate();
        }

        var version = await _publisher.PublishAsync(
            RegistryNames.Notifications,
            PlatformPermissions.NotificationsPublish,
            "platform.notification_template",
            $"{body.Key}:{body.Channel}:{body.Locale}",
            template.Id,
            $"Updated template '{body.Key}' ({body.Channel}, {body.Locale}).",
            before,
            new JsonObject { ["key"] = template.Key, ["isActive"] = template.IsActive },
            cancellationToken);

        return new RegistryWriteResponse(RegistryNames.Notifications, version, template.Key, template.Id);
    }

    public async Task<Result<RegistryWriteResponse>> Handle(
        InvalidateRegistryCommand command,
        CancellationToken cancellationToken)
    {
        if (!RegistryNames.All.Contains(command.Registry, StringComparer.Ordinal))
        {
            return Error.Validation("registry.unknown", $"'{command.Registry}' is not a registry.")
                .With("registry", command.Registry)
                .With("known", RegistryNames.All);
        }

        var version = await _publisher.PublishAsync(
            command.Registry,
            PlatformPermissions.CacheInvalidate,
            "platform.registry",
            command.Registry,
            entityId: null,
            $"Manually invalidated the {command.Registry} registry cache.",
            before: null,
            after: null,
            cancellationToken);

        return new RegistryWriteResponse(command.Registry, version, command.Registry, null);
    }

    private static JsonObject Describe(FeatureFlag flag)
        => new JsonObject
        {
            ["key"] = flag.Key,
            ["defaultValue"] = flag.DefaultValue,
            ["rules"] = flag.Rules.Count,
        };
}

/// <summary>Every registry's current generation — the admin console's cache panel.</summary>
internal sealed record GetRegistryVersionsQuery : IRequest<Result<IReadOnlyList<RegistryVersionView>>>;

public sealed record RegistryVersionView(string Registry, long Version, string ETag);

internal sealed class GetRegistryVersionsHandler
    : IRequestHandler<GetRegistryVersionsQuery, Result<IReadOnlyList<RegistryVersionView>>>
{
    private readonly IRegistryCache _cache;

    public GetRegistryVersionsHandler(IRegistryCache cache) => _cache = cache;

    public async Task<Result<IReadOnlyList<RegistryVersionView>>> Handle(
        GetRegistryVersionsQuery query,
        CancellationToken cancellationToken)
    {
        var views = new List<RegistryVersionView>(RegistryNames.All.Count);

        foreach (var registry in RegistryNames.All)
        {
            var version = await _cache.GetVersionAsync(registry, cancellationToken);
            views.Add(new RegistryVersionView(registry, version.Value, version.ToETag()));
        }

        return views;
    }
}
