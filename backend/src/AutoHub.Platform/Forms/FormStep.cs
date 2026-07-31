using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AutoHub.SharedKernel.Ids;

namespace AutoHub.Platform.Forms;

/// <summary>One page of a form.</summary>
/// <remarks>
/// Steps exist because a sell flow with twenty-eight fields on one screen is abandoned, and because
/// the mobile apps need a natural place to save progress. Everything about a step is authored data;
/// the renderer decides nothing except how a step looks.
/// </remarks>
public sealed class FormStep
{
    public FormStep(
        string key,
        string title,
        string? titleKey = null,
        string? description = null,
        string? descriptionKey = null,
        int sortOrder = 0)
    {
        Key = key;
        Title = title;
        TitleKey = titleKey;
        Description = description;
        DescriptionKey = descriptionKey;
        SortOrder = sortOrder;
    }

    private FormStep() { }

    private readonly List<FormField> _fields = [];

    public Guid Id { get; private set; } = UuidV7.New();

    public Guid FormVersionId { get; private set; }

    /// <summary>Machine key, e.g. <c>identity</c>. Used for resume-where-you-left-off and analytics.</summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>Default English heading. The shown heading is resolved from the i18n registry.</summary>
    public string Title { get; private set; } = string.Empty;

    public string? TitleKey { get; private set; }

    public string? Description { get; private set; }

    public string? DescriptionKey { get; private set; }

    public int SortOrder { get; private set; }

    public IReadOnlyList<FormField> Fields => _fields;

    public FormField? FindField(string key)
        => _fields.Find(f => string.Equals(f.Key, key, StringComparison.Ordinal));

    /// <summary>
    /// Structural edits go through <see cref="FormVersion"/> so the published-is-immutable check
    /// exists in exactly one place rather than on every child.
    /// </summary>
    internal void Attach(FormField field) => _fields.Add(field);
}

/// <summary>One input.</summary>
/// <remarks>
/// A field is deliberately not a C# type. The moment "kms driven" is a property on a
/// <c>SellVehicleRequest</c>, adding "battery health" for EVs is a backend release, a web release,
/// two app-store reviews, and a fortnight. Here it is a row.
/// </remarks>
public sealed class FormField
{
    public FormField(
        string key,
        string label,
        FormControl control,
        bool required = false,
        string? labelKey = null,
        string? unit = null,
        string? placeholder = null,
        string? helpText = null,
        string? attributeCode = null,
        string? configJson = null,
        string? visibleWhenJson = null,
        int sortOrder = 0)
    {
        Key = key;
        Label = label;
        Control = control;
        Required = required;
        LabelKey = labelKey;
        Unit = unit;
        Placeholder = placeholder;
        HelpText = helpText;
        AttributeCode = attributeCode;
        ConfigJson = configJson;
        VisibleWhenJson = visibleWhenJson;
        SortOrder = sortOrder;
    }

    private FormField() { }

    private readonly List<FormValidationRule> _rules = [];

    public Guid Id { get; private set; } = UuidV7.New();

    public Guid FormStepId { get; private set; }

    /// <summary>
    /// The payload property name, and the name conditions refer to. Unique across the whole form,
    /// not merely within its step, because the submitted payload is flat.
    /// </summary>
    public string Key { get; private set; } = string.Empty;

    public string Label { get; private set; } = string.Empty;

    public string? LabelKey { get; private set; }

    public FormControl Control { get; private set; }

    public bool Required { get; private set; }

    /// <summary>Rendered next to the input — <c>km</c>, <c>%</c>. Inherited from the attribute if unset.</summary>
    public string? Unit { get; private set; }

    public string? Placeholder { get; private set; }

    public string? HelpText { get; private set; }

    /// <summary>
    /// The taxonomy attribute this field collects, if any.
    /// </summary>
    /// <remarks>
    /// This is the link that stops option lists being duplicated. A fuel picker stores
    /// <c>fuel_type</c> here and no options at all; the compiler fills them from the attribute
    /// definition at serve time, so adding CNG to the taxonomy adds it to every form that asks for
    /// fuel — the sell flow, the inspection checklist and the dealer bulk-upload template — at once.
    /// <para>
    /// It is also what makes "which forms break if we retire this attribute?" a query rather than a
    /// grep through JSON blobs, which is the reason fields are rows and not just <c>schemaJson</c>.
    /// </para>
    /// </remarks>
    public string? AttributeCode { get; private set; }

    /// <summary>
    /// Control-specific settings the renderer understands — <c>{"min":6,"max":20,"prompts":[…]}</c>
    /// for a photo set, step size for a slider. Opaque to the server on purpose: a new renderer
    /// capability must not require a backend change.
    /// </summary>
    public string? ConfigJson { get; private set; }

    /// <summary>JsonLogic over the form's own values. Null means always visible.</summary>
    public string? VisibleWhenJson { get; private set; }

    public int SortOrder { get; private set; }

    /// <summary>Ordered; evaluated identically on web, Flutter and here.</summary>
    public IReadOnlyList<FormValidationRule> Rules => _rules;

    public JsonNode? Config()
        => string.IsNullOrWhiteSpace(ConfigJson) ? null : JsonNode.Parse(ConfigJson);

    public JsonNode? VisibleWhen()
        => string.IsNullOrWhiteSpace(VisibleWhenJson) ? null : JsonNode.Parse(VisibleWhenJson);

    internal void Attach(FormValidationRule rule) => _rules.Add(rule);
}

/// <summary>
/// One validation constraint, carrying its own machine code and default message.
/// </summary>
/// <remarks>
/// The code travels with the rule rather than being derived from the rule kind because the same
/// kind means different things on different fields: a <c>max</c> on the odometer is
/// <c>kms.too_high</c> and a <c>max</c> on the asking price is <c>price.above_cap</c>, and a
/// translator needs to distinguish them. Once shipped, a code never changes — clients branch on it.
/// </remarks>
public sealed class FormValidationRule
{
    public FormValidationRule(FormRuleKind rule, string code, string message, JsonNode? value = null, int sortOrder = 0)
    {
        Rule = rule;
        Code = code;
        Message = message;
        ValueJson = value?.ToJsonString();
        SortOrder = sortOrder;
    }

    private FormValidationRule() { }

    public Guid Id { get; private set; } = UuidV7.New();

    public Guid FormFieldId { get; private set; }

    public FormRuleKind Rule { get; private set; }

    /// <summary>The bound, pattern, or option list. Null for rules that take no argument.</summary>
    public string? ValueJson { get; private set; }

    /// <summary>Stable machine code reported per field, e.g. <c>kms.too_high</c>.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Developer-facing default. Users see the i18n registry's string for <see cref="Code"/>.</summary>
    public string Message { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public JsonNode? Value()
        => string.IsNullOrWhiteSpace(ValueJson) ? null : JsonNode.Parse(ValueJson);

    /// <summary>For <see cref="FormRuleKind.Custom"/>, the registered handler this rule invokes.</summary>
    public string? CustomHandlerCode() => FormSyntax.CustomHandlerCode(Value());
}

/// <summary>
/// The renderer's vocabulary.
/// </summary>
/// <remarks>
/// This is a closed enum and stays one, for the same reason workflow effects are named handlers:
/// a control is a capability someone wrote, reviewed and tested, not a string an operator can
/// invent. An operator choosing which control a field uses is configuration; an operator naming a
/// control that no renderer implements is a blank screen on a customer's phone.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<FormControl>))]
public enum FormControl
{
    [JsonStringEnumMemberName("text")]
    Text = 1,

    /// <summary>Registration-number input with plate grouping and RTO-aware masking.</summary>
    [JsonStringEnumMemberName("plate")]
    Plate = 2,

    [JsonStringEnumMemberName("number")]
    Number = 3,

    [JsonStringEnumMemberName("select")]
    Select = 4,

    [JsonStringEnumMemberName("multiselect")]
    Multiselect = 5,

    [JsonStringEnumMemberName("radio")]
    Radio = 6,

    [JsonStringEnumMemberName("checkbox")]
    Checkbox = 7,

    [JsonStringEnumMemberName("date")]
    Date = 8,

    /// <summary>Amount input. The payload carries minor units; the renderer formats.</summary>
    [JsonStringEnumMemberName("money")]
    Money = 9,

    [JsonStringEnumMemberName("phone")]
    Phone = 10,

    [JsonStringEnumMemberName("otp")]
    Otp = 11,

    [JsonStringEnumMemberName("file")]
    File = 12,

    /// <summary>Guided multi-photo capture with per-slot prompts. The inspection checklist runs on this.</summary>
    [JsonStringEnumMemberName("photo_set")]
    PhotoSet = 13,

    [JsonStringEnumMemberName("slider")]
    Slider = 14,

    [JsonStringEnumMemberName("toggle")]
    Toggle = 15,

    [JsonStringEnumMemberName("address")]
    Address = 16,

    /// <summary>Make/model/variant cascade fed by the taxonomy registry.</summary>
    [JsonStringEnumMemberName("vehicle_picker")]
    VehiclePicker = 17,

    [JsonStringEnumMemberName("rich_text")]
    RichText = 18,
}

/// <summary>The rule union from the API contract. Wire names are the contract's, not C#'s.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FormRuleKind>))]
public enum FormRuleKind
{
    [JsonStringEnumMemberName("required")]
    Required = 1,

    /// <summary>Numeric lower bound; element count for a multi-value field.</summary>
    [JsonStringEnumMemberName("min")]
    Min = 2,

    /// <summary>Numeric upper bound; element count for a multi-value field.</summary>
    [JsonStringEnumMemberName("max")]
    Max = 3,

    [JsonStringEnumMemberName("minLength")]
    MinLength = 4,

    [JsonStringEnumMemberName("maxLength")]
    MaxLength = 5,

    [JsonStringEnumMemberName("pattern")]
    Pattern = 6,

    [JsonStringEnumMemberName("email")]
    Email = 7,

    [JsonStringEnumMemberName("phone")]
    Phone = 8,

    [JsonStringEnumMemberName("oneOf")]
    OneOf = 9,

    /// <summary>
    /// A named server-side check — "this registration is already listed", "this PAN is blacklisted".
    /// The client cannot evaluate it and does not try; it simply renders whatever the server returns.
    /// </summary>
    [JsonStringEnumMemberName("custom")]
    Custom = 10,
}
