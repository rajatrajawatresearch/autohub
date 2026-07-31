using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AutoHub.Platform.Expressions;
using AutoHub.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace AutoHub.Platform.Forms;

/// <summary>
/// Evaluates a form's rules against a submitted payload.
/// </summary>
/// <remarks>
/// This engine exists to be the server half of a promise: <b>a payload that passes on the phone
/// passes here</b>. It evaluates the same document the renderer was given, rule for rule, in the
/// same order, and reports the rule's own machine code — never a code it invented — so the two sides
/// cannot disagree about what went wrong.
/// <para>
/// The single most important behaviour is that hidden fields are skipped. A required field behind a
/// <c>visibleWhen</c> the user never satisfied is a field they were never shown; validating it
/// produces a form that refuses to submit and highlights nothing, which is unfixable from the user's
/// side and near-undebuggable from ours.
/// </para>
/// </remarks>
public sealed partial class FormValidationEngine
{
    /// <summary>
    /// Reported when a field marked <c>required</c> is empty and no explicit <c>required</c> rule
    /// carries a more specific code. Shared with the clients, which must report the same code.
    /// </summary>
    public const string RequiredErrorCode = "field.required";

    /// <summary>
    /// Reported when the form itself is broken — an unparseable condition, a pattern that will not
    /// compile, a custom rule naming a handler that no longer exists. Distinct from a user error on
    /// purpose: alerting keys off it, and the user is told to try again rather than to fix a field
    /// they cannot see.
    /// </summary>
    public const string MisconfiguredErrorCode = "field.misconfigured";

    // An operator authored this pattern in an admin panel; to a backtracking regex engine that is
    // untrusted input. Without a timeout a single catastrophic pattern pins a request thread on the
    // busiest POST in the product, and it takes one bad nested quantifier to get there.
    private static readonly TimeSpan PatternTimeout = TimeSpan.FromMilliseconds(200);

    private readonly Dictionary<string, IFormCustomRuleHandler> _customRules;
    private readonly ILogger<FormValidationEngine> _logger;

    public FormValidationEngine(
        IEnumerable<IFormCustomRuleHandler> customRules,
        ILogger<FormValidationEngine> logger)
    {
        _customRules = customRules.ToDictionary(h => h.RuleCode, StringComparer.Ordinal);
        KnownCustomRuleCodes = _customRules.Keys.ToHashSet(StringComparer.Ordinal);
        _logger = logger;
    }

    /// <summary>Every registered handler code. Used to validate a form before it can be published.</summary>
    public IReadOnlySet<string> KnownCustomRuleCodes { get; }

    // Deliberately permissive: the RFC's grammar allows addresses that no mail server accepts, and
    // rejecting a real address is a lost customer while accepting a fake one costs a bounced email.
    [GeneratedRegex(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$")]
    private static partial Regex EmailPattern { get; }

    // E.164 shape only. Which prefixes are real, and which of them we can actually deliver an OTP
    // to, is operator-maintained data in the geography and integration registries — not a constant
    // compiled into this assembly.
    [GeneratedRegex(@"^\+?[1-9]\d{7,14}$")]
    private static partial Regex PhonePattern { get; }

    [GeneratedRegex(@"[\s\-()\.]")]
    private static partial Regex PhoneNoisePattern { get; }

    public async Task<FormValidationReport> ValidateAsync(
        CompiledForm form,
        JsonObject payload,
        FormContext context,
        FormValidationMode mode,
        CancellationToken cancellationToken)
    {
        var document = BuildEvaluationDocument(payload, context);
        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var field in form.Fields())
        {
            var visibility = ConditionExpression.FromJson(field.VisibleWhen);
            if (visibility.IsFailure)
            {
                _logger.LogError(
                    "Form {FormKey} v{Version} field {FieldKey} has an unparseable visibleWhen",
                    form.Key, form.Version, field.Key);

                Record(errors, field.Key, MisconfiguredErrorCode);
                continue;
            }

            if (!visibility.Value.Evaluate(document))
            {
                continue;
            }

            var value = document[field.Key];

            if (IsEmpty(value))
            {
                if (field.Required && mode == FormValidationMode.Complete)
                {
                    Record(errors, field.Key, RequiredCode(field));
                }

                // Every other rule describes a constraint on a value. An absent optional answer has
                // no value to constrain, and reporting "must be at least 6 characters" for a question
                // the user chose not to answer is noise they cannot act on.
                continue;
            }

            foreach (var rule in field.Validation)
            {
                if (rule.Rule == FormRuleKind.Required)
                {
                    continue; // handled above, against the field's own required flag
                }

                var code = rule.Rule == FormRuleKind.Custom
                    ? await EvaluateCustomAsync(form, field, rule, value, document, cancellationToken)
                    : Evaluate(rule, value, form, field);

                if (code is not null)
                {
                    Record(errors, field.Key, code);
                }
            }
        }

        return FormValidationReport.From(errors);
    }

    /// <summary>
    /// The document conditions are evaluated against: the user's answers, plus the request context
    /// under a reserved root.
    /// </summary>
    /// <remarks>
    /// Namespaced under <see cref="FormSyntax.ContextRoot"/> so a payload cannot shadow a server
    /// fact. Without that, a caller who posts <c>{"category":"scooter"}</c> could talk a condition
    /// into hiding the field that would have rejected their submission.
    /// </remarks>
    private static JsonObject BuildEvaluationDocument(JsonObject payload, FormContext context)
    {
        var document = payload.DeepClone() as JsonObject ?? [];
        document[FormSyntax.ContextRoot] = context.ToJson();
        return document;
    }

    private string? Evaluate(CompiledRule rule, JsonNode value, CompiledForm form, CompiledField field)
        => rule.Rule switch
        {
            FormRuleKind.Min => Magnitude(value) is { } actual && Magnitude(rule.Value) is { } bound
                ? actual >= bound ? null : rule.Code
                : rule.Code,

            FormRuleKind.Max => Magnitude(value) is { } actual && Magnitude(rule.Value) is { } bound
                ? actual <= bound ? null : rule.Code
                : rule.Code,

            FormRuleKind.MinLength => Length(value) is { } actual && Magnitude(rule.Value) is { } bound
                ? actual >= bound ? null : rule.Code
                : rule.Code,

            FormRuleKind.MaxLength => Length(value) is { } actual && Magnitude(rule.Value) is { } bound
                ? actual <= bound ? null : rule.Code
                : rule.Code,

            FormRuleKind.Pattern => EvaluatePattern(rule, value, form, field),

            FormRuleKind.Email => Text(value) is { } email && EmailPattern.IsMatch(email) ? null : rule.Code,

            FormRuleKind.Phone => EvaluatePhone(rule, value),

            FormRuleKind.OneOf => EvaluateOneOf(rule, value),

            _ => null,
        };

    private string? EvaluatePattern(CompiledRule rule, JsonNode value, CompiledForm form, CompiledField field)
    {
        var text = Text(value);
        var pattern = Text(rule.Value);

        if (string.IsNullOrEmpty(pattern))
        {
            _logger.LogError(
                "Form {FormKey} v{Version} field {FieldKey} has a pattern rule with no pattern",
                form.Key, form.Version, field.Key);

            return MisconfiguredErrorCode;
        }

        if (text is null)
        {
            return rule.Code;
        }

        try
        {
            return Regex.IsMatch(text, pattern, RegexOptions.None, PatternTimeout) ? null : rule.Code;
        }
        catch (RegexMatchTimeoutException)
        {
            // Publish-time validation proves the pattern compiles; it cannot prove it terminates
            // quickly on every input. Failing the field rather than the request keeps one pathological
            // answer from taking the endpoint down with it.
            _logger.LogError(
                "Form {FormKey} field {FieldKey} pattern timed out; the expression is pathological",
                form.Key, field.Key);

            return MisconfiguredErrorCode;
        }
        catch (ArgumentException)
        {
            _logger.LogError(
                "Form {FormKey} field {FieldKey} has an invalid pattern that survived publish validation",
                form.Key, field.Key);

            return MisconfiguredErrorCode;
        }
    }

    private static string? EvaluatePhone(CompiledRule rule, JsonNode value)
    {
        var text = Text(value);
        if (text is null)
        {
            return rule.Code;
        }

        // People type "+91 98123-45678". Stripping the punctuation they naturally use is not
        // leniency, it is refusing to fail someone for formatting a number the way a human does.
        var digits = PhoneNoisePattern.Replace(text, string.Empty);
        return PhonePattern.IsMatch(digits) ? null : rule.Code;
    }

    private static string? EvaluateOneOf(CompiledRule rule, JsonNode value)
    {
        if (rule.Value is not JsonArray allowedNodes || allowedNodes.Count == 0)
        {
            return MisconfiguredErrorCode;
        }

        var allowed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in allowedNodes)
        {
            if (Text(node) is { } option)
            {
                allowed.Add(option);
            }
        }

        // A multi-value answer is valid only if every one of its selections is. Checking "any" would
        // let one legitimate choice smuggle in an arbitrary second value.
        if (value is JsonArray selections)
        {
            foreach (var selection in selections)
            {
                if (Text(selection) is not { } chosen || !allowed.Contains(chosen))
                {
                    return rule.Code;
                }
            }

            return null;
        }

        return Text(value) is { } single && allowed.Contains(single) ? null : rule.Code;
    }

    private async Task<string?> EvaluateCustomAsync(
        CompiledForm form,
        CompiledField field,
        CompiledRule rule,
        JsonNode value,
        JsonObject document,
        CancellationToken cancellationToken)
    {
        var handlerCode = FormSyntax.CustomHandlerCode(rule.Value);

        if (handlerCode is null || !_customRules.TryGetValue(handlerCode, out var handler))
        {
            // Publish-time validation should have caught this. Reaching it means a handler was
            // deleted from the code while a published form still referenced it — a deployment
            // mistake, and the submission must not be waved through as if the check had passed.
            _logger.LogError(
                "Form {FormKey} field {FieldKey} references unregistered custom rule {Handler}",
                form.Key, field.Key, handlerCode);

            return MisconfiguredErrorCode;
        }

        Result outcome;
        try
        {
            outcome = await handler.EvaluateAsync(
                new FormCustomRuleContext(
                    form.Key,
                    field.Key,
                    value,
                    document,
                    FormSyntax.CustomArguments(rule.Value)),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Custom form rule {Handler} threw", handlerCode);
            return MisconfiguredErrorCode;
        }

        // The handler's code, not the rule's. A custom rule is precisely the check the client could
        // not perform, so its outcome is the one thing the client has to be told specifically —
        // "already listed by another seller" and "blacklisted PAN" need different screens.
        return outcome.IsSuccess ? null : outcome.Error.Code;
    }

    private static string RequiredCode(CompiledField field)
    {
        foreach (var rule in field.Validation)
        {
            if (rule.Rule == FormRuleKind.Required && !string.IsNullOrWhiteSpace(rule.Code))
            {
                return rule.Code;
            }
        }

        return RequiredErrorCode;
    }

    private static void Record(Dictionary<string, List<string>> errors, string fieldKey, string code)
    {
        if (!errors.TryGetValue(fieldKey, out var codes))
        {
            codes = [];
            errors[fieldKey] = codes;
        }

        if (!codes.Contains(code, StringComparer.Ordinal))
        {
            codes.Add(code);
        }
    }

    /// <summary>
    /// Whitespace counts as empty. "   " in a required text box is a user who has not answered, and
    /// treating it as an answer is how a listing ends up with a blank title.
    /// </summary>
    private static bool IsEmpty([NotNullWhen(false)] JsonNode? node) => node switch
    {
        null => true,
        JsonArray array => array.Count == 0,
        JsonValue value => value.TryGetValue<string>(out var text) && string.IsNullOrWhiteSpace(text),
        _ => false,
    };

    /// <summary>Numeric magnitude for a bound comparison; the item count for a multi-value answer.</summary>
    private static decimal? Magnitude(JsonNode? node) => node switch
    {
        JsonArray array => array.Count,
        JsonValue value when value.TryGetValue<decimal>(out var number) => number,
        JsonValue value when value.TryGetValue<string>(out var text)
                             && decimal.TryParse(
                                 text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) => parsed,
        _ => null,
    };

    /// <summary>Character count for text, item count for a list — the same thing JavaScript's <c>.length</c> is.</summary>
    private static int? Length(JsonNode? node) => node switch
    {
        JsonArray array => array.Count,
        JsonValue value when value.TryGetValue<string>(out var text) => text.Length,
        _ => null,
    };

    /// <summary>
    /// The text form of an answer. Numbers and booleans get their JSON literal so a pattern authored
    /// for a numeric field still works; arrays and objects have no text form and fail their rule.
    /// </summary>
    private static string? Text(JsonNode? node) => node switch
    {
        JsonValue value when value.TryGetValue<string>(out var text) => text,
        JsonValue value => value.ToJsonString(),
        _ => null,
    };
}

/// <summary>Whether missing answers count against the payload.</summary>
public enum FormValidationMode
{
    /// <summary>
    /// Checks only what is present. The sell flow validates step by step, and a seller on step one
    /// must not be shown errors about step four.
    /// </summary>
    Partial = 1,

    /// <summary>Everything a visible required field asks for must be there. The submit-time check.</summary>
    Complete = 2,
}

/// <summary>Per-field machine codes, in the shape the contract's 422 response carries.</summary>
public sealed class FormValidationReport
{
    private FormValidationReport(IReadOnlyDictionary<string, IReadOnlyList<string>> fieldErrors)
        => FieldErrors = fieldErrors;

    public static FormValidationReport Valid { get; } =
        new(new Dictionary<string, IReadOnlyList<string>>(0, StringComparer.Ordinal));

    /// <summary>Field key to the codes it failed. Codes only — the sentence comes from the i18n registry.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> FieldErrors { get; }

    public bool IsValid => FieldErrors.Count == 0;

    internal static FormValidationReport From(Dictionary<string, List<string>> errors)
    {
        if (errors.Count == 0)
        {
            return Valid;
        }

        var frozen = new Dictionary<string, IReadOnlyList<string>>(errors.Count, StringComparer.Ordinal);
        foreach (var (field, codes) in errors)
        {
            frozen[field] = codes;
        }

        return new FormValidationReport(frozen);
    }

    public Error ToError()
        => Error.Validation("form.validation_failed", "Some answers need attention.")
            .With("fieldErrors", FieldErrors);

    public Result ToResult() => IsValid ? Result.Success() : Result.Failure(ToError());
}

/// <summary>Everything a custom rule is allowed to know about the answer it is checking.</summary>
public sealed record FormCustomRuleContext(
    string FormKey,
    string FieldKey,
    JsonNode? Value,
    JsonObject Payload,
    JsonNode? Arguments);

/// <summary>
/// A server-side check a form can invoke by name.
/// </summary>
/// <remarks>
/// The same boundary the workflow registry draws between data and code. <b>Which</b> checks run on
/// <b>which</b> fields is data an operator edits; <b>what a check can do</b> is code a developer
/// writes, reviews and tests. A scripting language in the database would instead hand production
/// query access to whoever can reach the admin panel.
/// <para>
/// Handlers must be cheap and side-effect free: this runs on every keystroke-debounced client
/// validation call as well as on submit.
/// </para>
/// </remarks>
public interface IFormCustomRuleHandler
{
    /// <summary>Stable key a <see cref="FormRuleKind.Custom"/> rule names, e.g. <c>registration_not_listed</c>.</summary>
    string RuleCode { get; }

    /// <summary>Success passes the field; the failure's <see cref="Error.Code"/> is reported to the client.</summary>
    Task<Result> EvaluateAsync(FormCustomRuleContext context, CancellationToken cancellationToken);
}
