using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AutoHub.Platform.Expressions;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Forms;

/// <summary>
/// One named form in the product — the sell flow, dealer onboarding, the 200-point inspection
/// checklist, a loan application — stored as data rather than written as screens.
/// </summary>
/// <remarks>
/// Every one of those forms changes on a business cadence, not an engineering one. Marketing wants
/// one fewer question in the sell flow because the funnel drops at step two; compliance wants a
/// GSTIN on dealer onboarding; the inspection vendor adds eleven checks to the brakes section. Each
/// of those, written as React components and C# request objects, is a web release plus two app-store
/// reviews plus a migration — a fortnight for a change a business person could describe in a
/// sentence.
/// <para>
/// Written here it is rows, and the same document renders on web, Android and iOS the moment it is
/// published. The renderers have never heard of a sell form.
/// </para>
/// </remarks>
public sealed class FormDefinition : AggregateRoot, IAuditable, ISoftDeletable
{
    private readonly List<FormVersion> _versions = [];

    private FormDefinition(Guid id, string key, string name, string entityType) : base(id)
    {
        Key = key;
        Name = name;
        EntityType = entityType;
        IsActive = true;
    }

    private FormDefinition() { }

    /// <summary>Stable identifier the API is addressed by, e.g. <c>sell_vehicle</c>.</summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>Admin-facing name. Never rendered to a customer — the version's title is.</summary>
    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    /// <summary>What a submission becomes, e.g. <c>listings.listing</c>. Ties the form to its module.</summary>
    public string EntityType { get; private set; } = string.Empty;

    /// <summary>
    /// Retired forms are deactivated, never deleted: submissions made against them still have to be
    /// explainable years later when a dispute or an audit asks what the seller was actually shown.
    /// </summary>
    public bool IsActive { get; private set; }

    public IReadOnlyList<FormVersion> Versions => _versions;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public static FormDefinition Draft(string key, string name, string entityType)
        => new(UuidV7.New(), key, name, entityType);

    public void Describe(string? description) => Description = description;

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Version numbers run across the whole definition, not per variant, so the number alone
    /// identifies exactly which document a client is holding — which is what the ETag needs.
    /// </summary>
    public int NextVersionNumber => _versions.Count == 0 ? 1 : _versions.Max(v => v.Version) + 1;

    public FormVersion? FindVersion(int version)
        => _versions.Find(v => v.Version == version);

    public Result AddVersion(FormVersion version)
    {
        if (FindVersion(version.Version) is not null)
        {
            return Error.Conflict(
                "form.version.duplicate",
                $"Version {version.Version} already exists on form '{Key}'.");
        }

        _versions.Add(version);
        return Result.Success();
    }

    public Result PublishVersion(int version, FormPublishContext context, DateTimeOffset now)
    {
        var target = FindVersion(version);
        if (target is null)
        {
            return Error.NotFound("form.version.not_found", $"Form '{Key}' has no version {version}.");
        }

        return target.Publish(Key, context, now);
    }

    /// <summary>
    /// Picks the published variant that best fits the request — the SUV sell form for an SUV, the
    /// generic one otherwise.
    /// </summary>
    /// <remarks>
    /// Specificity is authored as a priority rather than inferred, for the same reason it is in the
    /// pricing registry: a system that guesses which of two overlapping variants is "more specific"
    /// is a system nobody can predict the behaviour of, and the person debugging it at launch is the
    /// person who wrote neither variant.
    /// </remarks>
    public FormVersion? SelectVariant(JsonNode context)
    {
        FormVersion? best = null;

        foreach (var version in _versions)
        {
            if (!version.IsPublished || !version.Matches(context))
            {
                continue;
            }

            if (best is null
                || version.Priority > best.Priority
                || (version.Priority == best.Priority && version.Version > best.Version))
            {
                best = version;
            }
        }

        return best;
    }
}

/// <summary>
/// One immutable, published-or-draft revision of a form.
/// </summary>
/// <remarks>
/// Published versions cannot be edited, for the same reason a published
/// <see cref="Workflow.WorkflowDefinition"/> cannot: half-finished submissions are in flight against
/// them. A seller who is on step three of the sell flow when someone deletes step two has a form
/// that cannot be submitted and an error message that describes a field they were never shown. A
/// new version is published instead, and drafts already started continue to render the version they
/// began on.
/// <para>
/// <b>Rows or JSON?</b> Both, with a clear division. Steps, fields and rules are rows because the
/// questions we need to answer are relational — "which forms collect <c>fuel_type</c>, so what
/// breaks if we retire it?", "which forms still ask for an Aadhaar number?" — and those are a query
/// over rows and a full-table JSONB scan otherwise. <see cref="SchemaJson"/> is the compiled
/// projection of those rows, written once at publish, so serving a form is one row read rather than
/// a four-table join on the hottest screen in the product.
/// </para>
/// <para>
/// What <see cref="SchemaJson"/> deliberately does <b>not</b> contain is taxonomy-backed option
/// lists. Those are resolved at serve time from the attribute definitions, because the taxonomy
/// registry versions on its own schedule: adding CNG to <c>fuel_type</c> must appear in every form
/// that asks for fuel without republishing any of them.
/// </para>
/// </remarks>
public sealed class FormVersion : IAuditable
{
    public FormVersion(
        int version,
        string title,
        string submitLabel,
        string? titleKey = null,
        string? submitLabelKey = null,
        string? variantKey = null,
        string? selectorJson = null,
        int priority = 0)
    {
        Version = version;
        Title = title;
        SubmitLabel = submitLabel;
        TitleKey = titleKey;
        SubmitLabelKey = submitLabelKey;
        VariantKey = variantKey;
        SelectorJson = selectorJson;
        Priority = priority;
    }

    private FormVersion() { }

    private readonly List<FormStep> _steps = [];

    public Guid Id { get; private set; } = UuidV7.New();

    public Guid FormDefinitionId { get; private set; }

    public int Version { get; private set; }

    /// <summary>Default English heading, e.g. "Tell us about your car".</summary>
    public string Title { get; private set; } = string.Empty;

    public string? TitleKey { get; private set; }

    /// <summary>The submit button's words. "Get my price", never "Submit".</summary>
    public string SubmitLabel { get; private set; } = string.Empty;

    public string? SubmitLabelKey { get; private set; }

    /// <summary>
    /// Admin-facing name for this variant, e.g. <c>category:suv</c>. Null is the base variant that
    /// applies when nothing more specific matches.
    /// </summary>
    public string? VariantKey { get; private set; }

    /// <summary>JsonLogic over the request context. Null means it matches every request.</summary>
    public string? SelectorJson { get; private set; }

    /// <summary>Higher wins when several variants match. Authored, never inferred.</summary>
    public int Priority { get; private set; }

    /// <summary>The compiled steps/fields document, written at publish. See the type's remarks.</summary>
    public string? SchemaJson { get; private set; }

    /// <summary>
    /// Drafts can be edited freely and are invisible to the registry. Publishing is the one-way door,
    /// and it runs <see cref="Validate"/> first.
    /// </summary>
    public bool IsPublished { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public IReadOnlyList<FormStep> Steps => _steps;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public FormStep? FindStep(string key)
        => _steps.Find(s => string.Equals(s.Key, key, StringComparison.Ordinal));

    /// <summary>Every field in step order — the flat shape a submitted payload actually has.</summary>
    public IEnumerable<FormField> Fields()
        => _steps.OrderBy(s => s.SortOrder).SelectMany(s => s.Fields.OrderBy(f => f.SortOrder));

    public FormField? FindField(string key)
        => Fields().FirstOrDefault(f => string.Equals(f.Key, key, StringComparison.Ordinal));

    public Result AddStep(FormStep step)
    {
        if (IsPublished)
        {
            return PublishedIsImmutable();
        }

        if (FindStep(step.Key) is not null)
        {
            return Error.Conflict("form.step.duplicate", $"Step '{step.Key}' already exists.");
        }

        _steps.Add(step);
        return Result.Success();
    }

    public Result AddField(string stepKey, FormField field)
    {
        if (IsPublished)
        {
            return PublishedIsImmutable();
        }

        var step = FindStep(stepKey);
        if (step is null)
        {
            return Error.NotFound("form.step.not_found", $"No step '{stepKey}' on this version.");
        }

        if (FindField(field.Key) is not null)
        {
            // Across the whole form, not just this step — the payload is flat, so two fields sharing
            // a key means one silently overwrites the other on submit.
            return Error.Conflict("form.field.duplicate", $"Field '{field.Key}' already exists on this form.");
        }

        step.Attach(field);
        return Result.Success();
    }

    public Result AddRule(string stepKey, string fieldKey, FormValidationRule rule)
    {
        if (IsPublished)
        {
            return PublishedIsImmutable();
        }

        var field = FindStep(stepKey)?.FindField(fieldKey);
        if (field is null)
        {
            return Error.NotFound("form.field.not_found", $"No field '{fieldKey}' on step '{stepKey}'.");
        }

        field.Attach(rule);
        return Result.Success();
    }

    /// <summary>
    /// Whether this variant applies to a request context.
    /// </summary>
    /// <remarks>
    /// A selector that will not parse does not match. The safe direction is falling back to the base
    /// variant; the alternative is serving a category-specific form to everyone because a condition
    /// nobody could read evaluated truthy.
    /// </remarks>
    public bool Matches(JsonNode context)
    {
        var selector = ConditionExpression.FromJson(SelectorJson);
        return selector.IsSuccess && selector.Value.Evaluate(context);
    }

    /// <summary>
    /// Proves the form can actually be filled in and submitted, before anyone is shown it.
    /// </summary>
    /// <remarks>
    /// Every check here is a failure mode that is invisible in the authoring UI and obvious to the
    /// user: a duplicate key silently drops an answer on submit, a misspelled variable hides a field
    /// forever, a required field behind an impossible condition produces a form that reports an error
    /// on a question nobody was asked. All of them are cheap to catch here and expensive to diagnose
    /// from a support ticket that says "it won't let me continue".
    /// </remarks>
    public Result<IReadOnlyList<string>> Validate(FormPublishContext context)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(Title))
        {
            problems.Add("The form has no title.");
        }

        if (string.IsNullOrWhiteSpace(SubmitLabel))
        {
            problems.Add("The form has no submit label.");
        }

        if (ConditionExpression.FromJson(SelectorJson).IsFailure)
        {
            problems.Add($"Variant '{VariantKey ?? "(base)"}' has an unparseable selector.");
        }

        if (_steps.Count == 0)
        {
            problems.Add("The form has no steps.");
        }

        var seenSteps = new HashSet<string>(StringComparer.Ordinal);
        var fieldKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var step in _steps)
        {
            if (!FormSyntax.IsValidKey(step.Key))
            {
                problems.Add($"Step key '{step.Key}' must be lower snake_case starting with a letter.");
            }

            if (!seenSteps.Add(step.Key))
            {
                problems.Add($"Step '{step.Key}' is defined more than once.");
            }

            if (step.Fields.Count == 0)
            {
                // An empty step renders as a page with a heading, no questions and a Next button.
                problems.Add($"Step '{step.Key}' has no fields.");
            }

            foreach (var field in step.Fields)
            {
                if (!FormSyntax.IsValidKey(field.Key))
                {
                    problems.Add($"Field key '{field.Key}' must be lower snake_case starting with a letter.");
                }

                if (!fieldKeys.Add(field.Key))
                {
                    problems.Add($"Field '{field.Key}' is defined more than once; the submitted payload is flat.");
                }
            }
        }

        foreach (var step in _steps)
        {
            foreach (var field in step.Fields)
            {
                ValidateField(field, fieldKeys, context, problems);
            }
        }

        return problems.Count == 0
            ? Result.Success<IReadOnlyList<string>>([])
            : Error.Validation("form.invalid", "This form cannot be published.")
                .With("problems", problems);
    }

    public Result Publish(string formKey, FormPublishContext context, DateTimeOffset now)
    {
        if (IsPublished)
        {
            return Error.Conflict("form.already_published", "This version is already published.");
        }

        var validation = Validate(context);
        if (validation.IsFailure)
        {
            return Result.Failure(validation.Error);
        }

        SchemaJson = FormJson.Serialize(FormSchemaCompiler.Project(formKey, this));
        IsPublished = true;
        PublishedAt = now;
        return Result.Success();
    }

    private static void ValidateField(
        FormField field,
        HashSet<string> fieldKeys,
        FormPublishContext context,
        List<string> problems)
    {
        if (field.AttributeCode is { } attributeCode && !context.KnownAttributeCodes.Contains(attributeCode))
        {
            problems.Add(
                $"Field '{field.Key}' references attribute '{attributeCode}', which the taxonomy registry " +
                "does not define.");
        }

        if (RequiresOptions(field.Control) && field.AttributeCode is null)
        {
            // The alternative — options authored on the field — is the same list maintained in the
            // sell form, the inspection checklist and the search facets, drifting apart quietly.
            problems.Add(
                $"Field '{field.Key}' uses the '{field.Control}' control but has no attribute code; " +
                "option lists come from the taxonomy registry, never from the form.");
        }

        JsonNode? visibleWhen = null;
        var visibilityParsed = false;

        try
        {
            visibleWhen = field.VisibleWhen();
            visibilityParsed = ConditionExpression.FromJson(visibleWhen).IsSuccess;
        }
        catch (System.Text.Json.JsonException)
        {
            visibilityParsed = false;
        }

        if (!visibilityParsed)
        {
            problems.Add($"Field '{field.Key}' has an unparseable visibleWhen condition.");
        }
        else if (visibleWhen is not null)
        {
            var unknown = FormSyntax.VariableRoots(visibleWhen)
                .Where(root => !fieldKeys.Contains(root)
                               && !string.Equals(root, FormSyntax.ContextRoot, StringComparison.Ordinal))
                .ToList();

            var neverTrue = IsLiteralFalse(visibleWhen) || unknown.Count > 0;

            if (unknown.Count > 0)
            {
                problems.Add(
                    $"Field '{field.Key}' is shown when {string.Join(", ", unknown)} — " +
                    $"no field on this form collects that. Did you mean another key?");
            }

            if (field.Required && neverTrue)
            {
                problems.Add(
                    $"Field '{field.Key}' is required but can never be shown, so the form could never " +
                    "be submitted.");
            }
        }

        if (field.ConfigJson is { } config && !IsParsableJson(config))
        {
            problems.Add($"Field '{field.Key}' has unparseable control config.");
        }

        foreach (var rule in field.Rules)
        {
            ValidateRule(field, rule, context, problems);
        }
    }

    private static void ValidateRule(
        FormField field,
        FormValidationRule rule,
        FormPublishContext context,
        List<string> problems)
    {
        if (string.IsNullOrWhiteSpace(rule.Code))
        {
            // The code is the contract clients branch on and translators key off. A blank one turns
            // into an untranslatable, unbranchable error at the worst possible moment.
            problems.Add($"Rule '{rule.Rule}' on field '{field.Key}' has no machine code.");
        }

        JsonNode? value;
        try
        {
            value = rule.Value();
        }
        catch (System.Text.Json.JsonException)
        {
            problems.Add($"Rule '{rule.Rule}' on field '{field.Key}' has an unparseable value.");
            return;
        }

        switch (rule.Rule)
        {
            case FormRuleKind.Min or FormRuleKind.Max or FormRuleKind.MinLength or FormRuleKind.MaxLength:
                if (value is not JsonValue bound || !bound.TryGetValue<decimal>(out _))
                {
                    problems.Add($"Rule '{rule.Rule}' on field '{field.Key}' needs a numeric value.");
                }

                break;

            case FormRuleKind.Pattern:
                var pattern = value is JsonValue text && text.TryGetValue<string>(out var p) ? p : null;
                if (string.IsNullOrEmpty(pattern))
                {
                    problems.Add($"Rule 'pattern' on field '{field.Key}' needs a regular expression.");
                }
                else if (!IsParsableRegex(pattern))
                {
                    // An unparseable pattern throws inside the validation engine on every submit,
                    // turning a typo in the admin panel into a 500 on the sell flow.
                    problems.Add($"Rule 'pattern' on field '{field.Key}' is not a valid regular expression.");
                }

                break;

            case FormRuleKind.OneOf:
                if (value is not JsonArray options || options.Count == 0)
                {
                    problems.Add($"Rule 'oneOf' on field '{field.Key}' needs a non-empty list of values.");
                }

                break;

            case FormRuleKind.Custom:
                var handler = rule.CustomHandlerCode();
                if (handler is null)
                {
                    problems.Add($"Rule 'custom' on field '{field.Key}' does not name a handler.");
                }
                else if (!context.KnownCustomRuleCodes.Contains(handler))
                {
                    problems.Add(
                        $"Rule 'custom' on field '{field.Key}' names handler '{handler}', " +
                        "which no code is registered for.");
                }

                break;

            case FormRuleKind.Required or FormRuleKind.Email or FormRuleKind.Phone:
            default:
                break;
        }
    }

    /// <summary>Controls whose whole purpose is choosing from a list the taxonomy registry owns.</summary>
    private static bool RequiresOptions(FormControl control)
        => control is FormControl.Select or FormControl.Multiselect or FormControl.Radio;

    private static bool IsLiteralFalse(JsonNode node)
        => node is JsonValue value && value.TryGetValue<bool>(out var literal) && !literal;

    private static bool IsParsableJson(string json)
    {
        try
        {
            return JsonNode.Parse(json) is not null;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    private static bool IsParsableRegex(string pattern)
    {
        try
        {
            var probe = new Regex(pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));
            return probe.MatchTimeout > TimeSpan.Zero;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static Error PublishedIsImmutable()
        => Error.Conflict(
            "form.published_immutable",
            "A published form cannot be changed. Create the next version instead.");
}

/// <summary>
/// The facts publish-time validation checks a form against.
/// </summary>
/// <remarks>
/// Passed in rather than fetched, so the aggregate stays free of I/O and the whole check runs as a
/// pure function over two sets — which is what makes it cheap enough to run on every draft save as
/// well as on publish.
/// </remarks>
public sealed record FormPublishContext
{
    /// <summary>Every attribute code the taxonomy registry defines.</summary>
    public required IReadOnlySet<string> KnownAttributeCodes { get; init; }

    /// <summary>Every custom-rule handler the running build registers.</summary>
    public required IReadOnlySet<string> KnownCustomRuleCodes { get; init; }
}
