using System.Text.Json;
using System.Text.Json.Nodes;
using AutoHub.Api.Application.Behaviours;
using AutoHub.Platform.Access;
using AutoHub.Platform.Caching;
using AutoHub.Platform.Forms;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using FluentValidation;
using MediatR;

namespace AutoHub.Api.Application.RegistryAdmin;

/// <summary>A whole form version, authored as one document.</summary>
/// <remarks>
/// Mirrors the served schema closely enough that an operator can copy a published form, edit it, and
/// post it back as the next version — which is how a form is actually changed, and is far less
/// error-prone than reconstructing it field by field through a granular API.
/// </remarks>
public sealed record FormVersionBody(
    string Title,
    string SubmitLabel,
    string? TitleKey,
    string? SubmitLabelKey,
    string? VariantKey,
    JsonNode? Selector,
    int Priority,
    IReadOnlyList<FormStepBody> Steps);

public sealed record FormStepBody(
    string Key,
    string Title,
    string? TitleKey,
    string? Description,
    int SortOrder,
    IReadOnlyList<FormFieldBody> Fields);

public sealed record FormFieldBody(
    string Key,
    string Label,
    string Control,
    bool Required,
    string? Unit,
    string? Placeholder,
    string? HelpText,
    string? AttributeCode,
    JsonNode? Config,
    JsonNode? VisibleWhen,
    int SortOrder,
    IReadOnlyList<FormRuleBody>? Validation);

public sealed record FormRuleBody(string Rule, JsonNode? Value, string Code, string Message, int SortOrder);

public sealed record CreateFormBody(string Key, string Name, string EntityType, string? Description);

internal sealed record CreateFormCommand(CreateFormBody Body)
    : IRequest<Result<RegistryWriteResponse>>, ITransactionalRequest;

internal sealed record AddFormVersionCommand(string Key, FormVersionBody Body)
    : IRequest<Result<RegistryWriteResponse>>, ITransactionalRequest;

internal sealed record PublishFormVersionCommand(string Key, int Version)
    : IRequest<Result<RegistryWriteResponse>>, ITransactionalRequest;

internal sealed record RollbackFormCommand(string Key, int ToVersion)
    : IRequest<Result<RegistryWriteResponse>>, ITransactionalRequest;

internal sealed class CreateFormValidator : AbstractValidator<CreateFormCommand>
{
    public CreateFormValidator()
    {
        RuleFor(command => command.Body.Key).NotEmpty().WithErrorCode("form.key_required");
        RuleFor(command => command.Body.Name).NotEmpty().WithErrorCode("form.name_required");
        RuleFor(command => command.Body.EntityType).NotEmpty().WithErrorCode("form.entity_type_required");
    }
}

internal sealed class AddFormVersionValidator : AbstractValidator<AddFormVersionCommand>
{
    public AddFormVersionValidator()
    {
        RuleFor(command => command.Key).NotEmpty().WithErrorCode("form.key_required");
        RuleFor(command => command.Body.Steps).NotEmpty().WithErrorCode("form.steps_required");
    }
}

internal sealed class FormAdminHandler
    : IRequestHandler<CreateFormCommand, Result<RegistryWriteResponse>>,
      IRequestHandler<AddFormVersionCommand, Result<RegistryWriteResponse>>,
      IRequestHandler<PublishFormVersionCommand, Result<RegistryWriteResponse>>,
      IRequestHandler<RollbackFormCommand, Result<RegistryWriteResponse>>
{
    private const string Entity = "platform.form_definition";

    private readonly IFormStore _store;
    private readonly IFormAttributeSource _attributes;
    private readonly FormValidationEngine _validation;
    private readonly RegistryPublisher _publisher;
    private readonly IClock _clock;

    public FormAdminHandler(
        IFormStore store,
        IFormAttributeSource attributes,
        FormValidationEngine validation,
        RegistryPublisher publisher,
        IClock clock)
    {
        _store = store;
        _attributes = attributes;
        _validation = validation;
        _publisher = publisher;
        _clock = clock;
    }

    public async Task<Result<RegistryWriteResponse>> Handle(
        CreateFormCommand command,
        CancellationToken cancellationToken)
    {
        var existing = await _store.GetAsync(command.Body.Key, cancellationToken);

        if (existing is not null)
        {
            return Error.Conflict("form.duplicate", $"A form named '{command.Body.Key}' already exists.");
        }

        var definition = FormDefinition.Draft(command.Body.Key, command.Body.Name, command.Body.EntityType);
        definition.Describe(command.Body.Description);

        await _store.AddAsync(definition, cancellationToken);

        _publisher.Record(
            PlatformPermissions.FormsPublish,
            Entity,
            definition.Key,
            definition.Id,
            $"Created form '{definition.Key}'.",
            after: new JsonObject { ["key"] = definition.Key, ["entityType"] = definition.EntityType });

        return new RegistryWriteResponse(RegistryNames.Forms, 0, definition.Key, definition.Id);
    }

    public async Task<Result<RegistryWriteResponse>> Handle(
        AddFormVersionCommand command,
        CancellationToken cancellationToken)
    {
        var definition = await _store.GetAsync(command.Key, cancellationToken);

        if (definition is null)
        {
            return Error.NotFound("form.not_found", $"No form named '{command.Key}'.");
        }

        var version = definition.NextVersionNumber;

        var built = Build(version, command.Body);
        if (built.IsFailure)
        {
            return Result.Failure<RegistryWriteResponse>(built.Error);
        }

        var added = definition.AddVersion(built.Value);
        if (added.IsFailure)
        {
            return Result.Failure<RegistryWriteResponse>(added.Error);
        }

        await _store.SaveChangesAsync(cancellationToken);

        _publisher.Record(
            PlatformPermissions.FormsPublish,
            Entity,
            definition.Key,
            definition.Id,
            $"Drafted form '{definition.Key}' v{version}.",
            after: new JsonObject { ["key"] = definition.Key, ["version"] = version });

        return new RegistryWriteResponse(RegistryNames.Forms, version, definition.Key, definition.Id);
    }

    public async Task<Result<RegistryWriteResponse>> Handle(
        PublishFormVersionCommand command,
        CancellationToken cancellationToken)
    {
        var definition = await _store.GetAsync(command.Key, cancellationToken);

        if (definition is null)
        {
            return Error.NotFound("form.not_found", $"No form named '{command.Key}'.");
        }

        var context = await PublishContextAsync(cancellationToken);

        var published = definition.PublishVersion(command.Version, context, _clock.UtcNow);
        if (published.IsFailure)
        {
            return Result.Failure<RegistryWriteResponse>(published.Error);
        }

        await _store.SaveChangesAsync(cancellationToken);

        // The form registry memoises per key, so its own generation has to move as well as the
        // registry version. Skipping this means an operator publishes, reloads, sees the old form,
        // and publishes again.
        FormRegistry.Invalidate(definition.Key);

        var registryVersion = await _publisher.PublishAsync(
            RegistryNames.Forms,
            PlatformPermissions.FormsPublish,
            Entity,
            definition.Key,
            definition.Id,
            $"Published form '{definition.Key}' v{command.Version}.",
            before: null,
            after: new JsonObject { ["key"] = definition.Key, ["version"] = command.Version },
            cancellationToken);

        return new RegistryWriteResponse(RegistryNames.Forms, registryVersion, definition.Key, definition.Id);
    }

    /// <summary>
    /// Republishes an earlier version's document as the next version.
    /// </summary>
    /// <remarks>
    /// Forward, like every rollback here. A published version's <c>SchemaJson</c> is frozen, so the
    /// restore is a re-parse of the exact document that was served rather than a reconstruction from
    /// rows that may since have been reinterpreted — which matters because in-flight submissions were
    /// validated against those exact bytes.
    /// </remarks>
    public async Task<Result<RegistryWriteResponse>> Handle(
        RollbackFormCommand command,
        CancellationToken cancellationToken)
    {
        var definition = await _store.GetAsync(command.Key, cancellationToken);

        if (definition is null)
        {
            return Error.NotFound("form.not_found", $"No form named '{command.Key}'.");
        }

        var source = definition.FindVersion(command.ToVersion);

        if (source is null || !source.IsPublished)
        {
            return Error.NotFound(
                "form.version_not_found",
                $"Form '{command.Key}' has no published version {command.ToVersion}.");
        }

        var nextVersion = definition.NextVersionNumber;
        var clone = CloneVersion(nextVersion, source);

        var added = definition.AddVersion(clone);
        if (added.IsFailure)
        {
            return Result.Failure<RegistryWriteResponse>(added.Error);
        }

        var context = await PublishContextAsync(cancellationToken);

        var published = definition.PublishVersion(nextVersion, context, _clock.UtcNow);
        if (published.IsFailure)
        {
            return Result.Failure<RegistryWriteResponse>(published.Error);
        }

        await _store.SaveChangesAsync(cancellationToken);
        FormRegistry.Invalidate(definition.Key);

        var registryVersion = await _publisher.PublishAsync(
            RegistryNames.Forms,
            PlatformPermissions.FormsPublish,
            Entity,
            definition.Key,
            definition.Id,
            $"Rolled form '{command.Key}' back to v{command.ToVersion}, republished as v{nextVersion}.",
            before: new JsonObject { ["version"] = command.ToVersion },
            after: new JsonObject { ["version"] = nextVersion },
            cancellationToken);

        return new RegistryWriteResponse(RegistryNames.Forms, registryVersion, definition.Key, definition.Id);
    }

    private async Task<FormPublishContext> PublishContextAsync(CancellationToken cancellationToken)
        => new()
        {
            KnownAttributeCodes = await _attributes.KnownCodesAsync(cancellationToken),
            KnownCustomRuleCodes = _validation.KnownCustomRuleCodes,
        };

    private static Result<FormVersion> Build(int version, FormVersionBody body)
    {
        var formVersion = new FormVersion(
            version,
            body.Title,
            body.SubmitLabel,
            body.TitleKey,
            body.SubmitLabelKey,
            body.VariantKey,
            body.Selector?.ToJsonString(),
            body.Priority);

        foreach (var step in body.Steps)
        {
            var added = formVersion.AddStep(new FormStep(
                step.Key, step.Title, step.TitleKey, step.Description, descriptionKey: null, step.SortOrder));

            if (added.IsFailure)
            {
                return Result.Failure<FormVersion>(added.Error);
            }

            foreach (var field in step.Fields)
            {
                var control = ParseControl(field.Control);
                if (control.IsFailure)
                {
                    return Result.Failure<FormVersion>(control.Error);
                }

                var addedField = formVersion.AddField(step.Key, new FormField(
                    field.Key,
                    field.Label,
                    control.Value,
                    field.Required,
                    labelKey: null,
                    field.Unit,
                    field.Placeholder,
                    field.HelpText,
                    field.AttributeCode,
                    field.Config?.ToJsonString(),
                    field.VisibleWhen?.ToJsonString(),
                    field.SortOrder));

                if (addedField.IsFailure)
                {
                    return Result.Failure<FormVersion>(addedField.Error);
                }

                foreach (var rule in field.Validation ?? [])
                {
                    var kind = ParseRuleKind(rule.Rule);
                    if (kind.IsFailure)
                    {
                        return Result.Failure<FormVersion>(kind.Error);
                    }

                    var addedRule = formVersion.AddRule(step.Key, field.Key, new FormValidationRule(
                        kind.Value, rule.Code, rule.Message, rule.Value, rule.SortOrder));

                    if (addedRule.IsFailure)
                    {
                        return Result.Failure<FormVersion>(addedRule.Error);
                    }
                }
            }
        }

        return formVersion;
    }

    private static FormVersion CloneVersion(int version, FormVersion source)
    {
        var clone = new FormVersion(
            version,
            source.Title,
            source.SubmitLabel,
            source.TitleKey,
            source.SubmitLabelKey,
            source.VariantKey,
            source.SelectorJson,
            source.Priority);

        foreach (var step in source.Steps.OrderBy(item => item.SortOrder))
        {
            clone.AddStep(new FormStep(
                step.Key, step.Title, step.TitleKey, step.Description, step.DescriptionKey, step.SortOrder));

            foreach (var field in step.Fields.OrderBy(item => item.SortOrder))
            {
                clone.AddField(step.Key, new FormField(
                    field.Key,
                    field.Label,
                    field.Control,
                    field.Required,
                    field.LabelKey,
                    field.Unit,
                    field.Placeholder,
                    field.HelpText,
                    field.AttributeCode,
                    field.ConfigJson,
                    field.VisibleWhenJson,
                    field.SortOrder));

                foreach (var rule in field.Rules.OrderBy(item => item.SortOrder))
                {
                    clone.AddRule(step.Key, field.Key, new FormValidationRule(
                        rule.Rule, rule.Code, rule.Message, rule.Value(), rule.SortOrder));
                }
            }
        }

        return clone;
    }

    /// <summary>
    /// Parses the contract's control name.
    /// </summary>
    /// <remarks>
    /// Through the enum's own <c>JsonStringEnumMemberName</c> attributes rather than a second
    /// mapping table, so the name accepted here is by construction the name the served document
    /// emits. A hand-written switch would be a second source of truth that drifts on the first
    /// control anyone adds.
    /// </remarks>
    private static Result<FormControl> ParseControl(string? wireName)
        => ParseWireEnum<FormControl>(wireName, "form.control_unknown", "control");

    private static Result<FormRuleKind> ParseRuleKind(string? wireName)
        => ParseWireEnum<FormRuleKind>(wireName, "form.rule_unknown", "rule");

    private static Result<T> ParseWireEnum<T>(string? wireName, string code, string field)
        where T : struct, Enum
    {
        if (!string.IsNullOrWhiteSpace(wireName))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<T>($"\"{wireName}\"", FormJson.Options);
                return parsed;
            }
            catch (JsonException)
            {
                // Falls through to the error below. The exception carries the serializer's internal
                // message, which names a C# type an operator has never heard of.
            }
        }

        return Error.Validation(code, $"'{wireName}' is not a supported {field}.").With(field, wireName);
    }
}
