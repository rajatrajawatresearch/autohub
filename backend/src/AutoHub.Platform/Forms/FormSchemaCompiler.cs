using System.Text.Json.Nodes;
using AutoHub.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace AutoHub.Platform.Forms;

/// <summary>
/// Turns stored form rows into the document the renderers consume.
/// </summary>
/// <remarks>
/// Compilation happens in two passes, and the split is the whole design.
/// <para>
/// <see cref="Project"/> is a pure function over the rows and runs once, at publish. It produces the
/// structure — steps, fields, controls, conditions, rules — and is what gets frozen into
/// <see cref="FormVersion.SchemaJson"/>. Nothing in it can change afterwards, which is exactly the
/// guarantee an in-flight submission needs.
/// </para>
/// <para>
/// <see cref="HydrateAsync"/> runs per request and fills in everything that legitimately moves
/// underneath a published form: option lists from the taxonomy registry and labels from the
/// localisation registry. Baking those in at publish would mean adding CNG to <c>fuel_type</c>
/// silently does nothing until every form that asks for fuel is republished — which is precisely the
/// class of "we'll make it configurable later" failure the platform exists to avoid.
/// </para>
/// </remarks>
public sealed class FormSchemaCompiler
{
    private readonly IFormAttributeSource _attributes;
    private readonly IFormLocalizer _localizer;
    private readonly ILogger<FormSchemaCompiler> _logger;

    public FormSchemaCompiler(
        IFormAttributeSource attributes,
        IFormLocalizer localizer,
        ILogger<FormSchemaCompiler> logger)
    {
        _attributes = attributes;
        _localizer = localizer;
        _logger = logger;
    }

    /// <summary>
    /// The structural projection: rows in, contract document out, no I/O and no options.
    /// </summary>
    /// <remarks>
    /// Static and dependency-free so <see cref="FormVersion.Publish"/> can call it without the
    /// aggregate acquiring a service reference, and so it can be tested with nothing but a form.
    /// </remarks>
    public static CompiledForm Project(string formKey, FormVersion version)
    {
        var steps = version.Steps
            .OrderBy(s => s.SortOrder)
            .Select(step => new CompiledStep
            {
                Key = step.Key,
                Title = step.Title,
                TitleKey = step.TitleKey,
                Description = step.Description,
                DescriptionKey = step.DescriptionKey,
                Fields = step.Fields
                    .OrderBy(f => f.SortOrder)
                    .Select(ProjectField)
                    .ToList(),
            })
            .ToList();

        return new CompiledForm
        {
            Key = formKey,
            Version = version.Version,
            Variant = version.VariantKey,
            Title = version.Title,
            TitleKey = version.TitleKey,
            SubmitLabel = version.SubmitLabel,
            SubmitLabelKey = version.SubmitLabelKey,
            Steps = steps,
        };
    }

    /// <summary>
    /// Fills in what the taxonomy and localisation registries own, at serve time.
    /// </summary>
    public async Task<Result<CompiledForm>> HydrateAsync(
        CompiledForm form,
        string? locale,
        CancellationToken cancellationToken)
    {
        var attributeCodes = form.Fields()
            .Select(f => f.AttributeCode)
            .Where(code => code is not null)
            .Select(code => code!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var attributes = attributeCodes.Count == 0
            ? new Dictionary<string, FormAttribute>(0, StringComparer.Ordinal)
            : await _attributes.ResolveAsync(attributeCodes, cancellationToken);

        var missing = attributeCodes
            .Where(code => !attributes.ContainsKey(code))
            .ToList();

        if (missing.Count > 0)
        {
            // Serving the form anyway would render an empty dropdown the user cannot get past, and
            // they would have no way to know why. Failing loudly puts the problem on the operator
            // who retired the attribute rather than on the seller trying to list a car.
            _logger.LogError(
                "Form {FormKey} v{Version} references attributes no longer in the taxonomy: {Codes}",
                form.Key, form.Version, string.Join(", ", missing));

            return Error.Unexpected(
                "form.attribute_missing",
                "This form is misconfigured and cannot be shown.")
                .With("attributeCodes", missing);
        }

        var translations = await ResolveTranslationsAsync(form, locale, cancellationToken);

        var steps = form.Steps
            .Select(step => step with
            {
                Title = Localise(step.TitleKey, step.Title, translations),
                Description = step.Description is null
                    ? null
                    : Localise(step.DescriptionKey, step.Description, translations),
                Fields = step.Fields
                    .Select(field => HydrateField(field, attributes, translations))
                    .ToList(),
            })
            .ToList();

        return form with
        {
            Title = Localise(form.TitleKey, form.Title, translations),
            SubmitLabel = Localise(form.SubmitLabelKey, form.SubmitLabel, translations),
            Steps = steps,
        };
    }

    private static CompiledField ProjectField(FormField field) => new()
    {
        Key = field.Key,
        Label = field.Label,
        LabelKey = field.LabelKey,
        Control = field.Control,
        Required = field.Required,
        Unit = field.Unit,
        Placeholder = field.Placeholder,
        HelpText = field.HelpText,
        AttributeCode = field.AttributeCode,
        Config = field.Config(),
        VisibleWhen = field.VisibleWhen(),
        Validation = field.Rules
            .OrderBy(r => r.SortOrder)
            .Select(rule => new CompiledRule
            {
                Rule = rule.Rule,
                Value = rule.Value(),
                Code = rule.Code,
                Message = rule.Message,
            })
            .ToList(),
    };

    private static CompiledField HydrateField(
        CompiledField field,
        IReadOnlyDictionary<string, FormAttribute> attributes,
        IReadOnlyDictionary<string, string> translations)
    {
        var label = Localise(field.LabelKey, field.Label, translations);

        if (field.AttributeCode is null || !attributes.TryGetValue(field.AttributeCode, out var attribute))
        {
            return field with { Label = label };
        }

        var options = attribute.Options.Count == 0
            ? null
            : attribute.Options.OrderBy(o => o.SortOrder).ToList();

        return field with
        {
            Label = label,
            // The attribute's unit is inherited, never copied onto the field, so "km" changing to
            // "kilometres" is one edit in the taxonomy rather than one per form that asks.
            Unit = field.Unit ?? attribute.Unit,
            Options = options,
            Validation = WithOptionConstraint(field, options),
        };
    }

    /// <summary>
    /// Adds the <c>oneOf</c> rule implied by a taxonomy-backed option list.
    /// </summary>
    /// <remarks>
    /// Without this the server would accept <c>fuel_type: "kerosene"</c> from a client that skipped
    /// the picker, because nobody authored a rule saying otherwise — and the option list would be
    /// enforced only by the UI, which is not enforcement. Emitting it into the served document rather
    /// than checking it privately keeps the promise that matters: the client evaluates exactly the
    /// rules the server will, so a payload that passes on the phone passes here.
    /// </remarks>
    private static IReadOnlyList<CompiledRule> WithOptionConstraint(
        CompiledField field,
        List<CompiledOption>? options)
    {
        if (options is null || options.Count == 0)
        {
            return field.Validation;
        }

        if (field.Validation.Any(r => r.Rule == FormRuleKind.OneOf))
        {
            // Authored explicitly — the author narrowed the list on purpose (a form that offers only
            // the electric variants of a shared attribute). Their rule wins.
            return field.Validation;
        }

        var allowed = new JsonArray();
        foreach (var option in options)
        {
            allowed.Add(option.Value);
        }

        return
        [
            .. field.Validation,
            new CompiledRule
            {
                Rule = FormRuleKind.OneOf,
                Value = allowed,
                Code = $"{field.Key}.not_an_option",
                Message = "Choose one of the listed options.",
            },
        ];
    }

    private async Task<IReadOnlyDictionary<string, string>> ResolveTranslationsAsync(
        CompiledForm form,
        string? locale,
        CancellationToken cancellationToken)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);

        AddKey(keys, form.TitleKey);
        AddKey(keys, form.SubmitLabelKey);

        foreach (var step in form.Steps)
        {
            AddKey(keys, step.TitleKey);
            AddKey(keys, step.DescriptionKey);

            foreach (var field in step.Fields)
            {
                AddKey(keys, field.LabelKey);
            }
        }

        if (keys.Count == 0)
        {
            return new Dictionary<string, string>(0, StringComparer.Ordinal);
        }

        return await _localizer.ResolveAsync(locale, keys, cancellationToken);
    }

    private static void AddKey(HashSet<string> keys, string? key)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            keys.Add(key);
        }
    }

    /// <summary>
    /// Resolved string, or the authored default. Never an empty string — a blank label is a control
    /// nobody can identify, which is worse than an untranslated one.
    /// </summary>
    private static string Localise(string? key, string fallback, IReadOnlyDictionary<string, string> translations)
        => key is not null
           && translations.TryGetValue(key, out var translated)
           && !string.IsNullOrWhiteSpace(translated)
            ? translated
            : fallback;
}
