using AutoHub.Platform.Localization;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Notifications;

/// <summary>
/// The message itself: one key, one channel, one locale.
/// </summary>
/// <remarks>
/// A template is keyed by <c>(Key, Channel, Locale)</c> because the same event says different things
/// on different surfaces. "Your bid on the 2021 Swift was outbid — new high bid ₹5,15,000" is a
/// reasonable WhatsApp message and a terrible push notification, which has about forty characters
/// before Android truncates it.
/// </remarks>
public sealed class NotificationTemplate : AggregateRoot, IAuditable
{
    private readonly List<NotificationTemplateVariable> _variables = [];

    private NotificationTemplate(Guid id, string key, NotificationChannel channel, string locale, string body)
        : base(id)
    {
        Key = key;
        Channel = channel;
        Locale = locale;
        Body = body;
    }

    private NotificationTemplate() { }

    /// <summary>Stable code, e.g. <c>auction.outbid</c>, <c>deal.payment_due</c>.</summary>
    public string Key { get; private set; } = string.Empty;

    public NotificationChannel Channel { get; private set; }

    /// <summary>BCP-47. Resolution walks the same fallback chain the i18n registry uses.</summary>
    public string Locale { get; private set; } = string.Empty;

    /// <summary>Email subject or push title. Null for channels that have no such concept.</summary>
    public string? Subject { get; private set; }

    public string Body { get; private set; } = string.Empty;

    /// <summary>
    /// The DLT-registered template id, without which Indian SMS is not deliverable.
    /// </summary>
    /// <remarks>
    /// <b>Why SMS cannot be free text in India.</b> Under TRAI's TCCCPR 2018 regulations, every
    /// commercial SMS must be sent from a registered six-character header (<c>AUTOHB</c>) against a
    /// message template that was registered in advance on a DLT — Distributed Ledger Technology —
    /// platform operated by the telecom operators. At delivery time the operator matches the body
    /// against the registered template, variable slots and all. A message that does not match is not
    /// rejected loudly at the API; it is accepted, billed in some cases, and silently dropped
    /// somewhere in the operator's network.
    /// <para>
    /// The consequence for this registry is specific and severe: <b>editing an SMS body is not a
    /// content change, it is a compliance change</b>. Fixing a typo without re-registering the
    /// template on DLT and updating this id means every OTP and every payment reminder stops arriving
    /// — and the only symptom is a support queue full of "I never got the code". So the id lives on
    /// the row next to the body, <see cref="Activate"/> refuses to publish an SMS template without
    /// one, and <see cref="Body"/> is understood to be a <i>mirror</i> of what was registered rather
    /// than the source of truth.
    /// </para>
    /// <para>
    /// WhatsApp works the same way for the same commercial reasons — Meta approves message templates
    /// per business account — so the field is shared rather than SMS-specific.
    /// </para>
    /// </remarks>
    public string? ProviderTemplateId { get; private set; }

    /// <summary>
    /// The registered sender header, e.g. <c>AUTOHB</c>. Also DLT-registered, also not free text.
    /// </summary>
    public string? SenderId { get; private set; }

    /// <summary>
    /// Inactive templates are invisible to resolution. Used to withdraw a message whose DLT
    /// registration lapsed without deleting the translation work.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// The variables this template promises to consume.
    /// </summary>
    /// <remarks>
    /// Declared rather than inferred so the admin editor can validate as the author types — offering
    /// the available variables, refusing an unknown one, and warning that a required variable is
    /// unused. Inferring them from the body would make a typo (<c>{vehicel}</c>) look like a
    /// deliberate new variable, and the message would ship with a literal brace in it.
    /// </remarks>
    public IReadOnlyList<NotificationTemplateVariable> Variables => _variables;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static NotificationTemplate Draft(
        string key,
        NotificationChannel channel,
        string locale,
        string body,
        string? subject = null)
        => new(UuidV7.New(), key, channel, locale, body) { Subject = subject };

    public void Declare(NotificationTemplateVariable variable) => _variables.Add(variable);

    public void RegisterWithProvider(string providerTemplateId, string? senderId)
    {
        ProviderTemplateId = providerTemplateId;
        SenderId = senderId;
    }

    /// <summary>
    /// Makes the template eligible for resolution, after proving it can actually be delivered.
    /// </summary>
    public Result Activate()
    {
        if (string.IsNullOrWhiteSpace(Body))
        {
            return Error.Validation("notification.template.body_empty", "A template needs a body.");
        }

        if (Channel.RequiresProviderRegistration() && string.IsNullOrWhiteSpace(ProviderTemplateId))
        {
            return Error.Validation(
                "notification.template.dlt_registration_missing",
                $"{Channel} templates must carry the registered provider template id before activation.")
                .With("channel", Channel.ToString());
        }

        var undeclared = MessageTemplate.UndeclaredPlaceholders(
            $"{Subject} {Body}",
            _variables.Select(v => v.Name));

        if (undeclared.Count > 0)
        {
            // Catching this at activation rather than at send time matters: the send path runs
            // inside a workflow effect, where the only observable symptom would be a notification
            // that reads "Hi {name}".
            return Error.Validation(
                "notification.template.variable_undeclared",
                "This template uses variables it does not declare.")
                .With("placeholders", undeclared);
        }

        IsActive = true;
        return Result.Success();
    }

    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Fills the template, refusing to send a half-rendered message.
    /// </summary>
    /// <remarks>
    /// A missing required variable fails the whole dispatch rather than rendering a gap. For an SMS
    /// this is not merely cosmetic: a body that no longer matches its DLT registration is dropped by
    /// the operator anyway, so the choice is between a logged failure we can alert on and a silent
    /// one we cannot.
    /// </remarks>
    public Result<RenderedNotification> Render(IReadOnlyDictionary<string, string?> variables)
        => NotificationRendering.Render(
            Key,
            Subject,
            Body,
            ProviderTemplateId,
            SenderId,
            _variables.Select(v => new TemplateVariableSnapshot(v.Name, v.IsRequired)).ToList(),
            variables);
}

/// <summary>A variable a template promises to consume.</summary>
public sealed class NotificationTemplateVariable
{
    public NotificationTemplateVariable(
        string name,
        bool isRequired = true,
        string? sampleValue = null,
        string? description = null)
    {
        Name = name;
        IsRequired = isRequired;
        SampleValue = sampleValue;
        Description = description;
    }

    private NotificationTemplateVariable() { }

    public Guid Id { get; private set; } = UuidV7.New();

    public Guid NotificationTemplateId { get; private set; }

    /// <summary>Matches the <c>{name}</c> placeholder in the body.</summary>
    public string Name { get; private set; } = string.Empty;

    public bool IsRequired { get; private set; }

    /// <summary>
    /// What the editor renders in its live preview, and what a DLT registration submission uses as
    /// the sample. A preview showing "Your bid on {vehicle}" tells an author nothing about whether
    /// the message fits in one SMS segment; "Your bid on 2021 Maruti Swift VXi" does.
    /// </summary>
    public string? SampleValue { get; private set; }

    public string? Description { get; private set; }
}

public sealed record RenderedNotification(
    string Subject,
    string Body,
    string? ProviderTemplateId,
    string? SenderId);

public enum NotificationChannel
{
    Push = 1,
    Sms = 2,
    Email = 3,
    Whatsapp = 4,

    /// <summary>The bell icon in the app. Always delivered — it is a read model, not an interruption.</summary>
    InApp = 5,
}

public static class NotificationChannelExtensions
{
    /// <summary>
    /// Channels whose content must be pre-approved by a third party before it can be delivered.
    /// </summary>
    public static bool RequiresProviderRegistration(this NotificationChannel channel)
        => channel is NotificationChannel.Sms or NotificationChannel.Whatsapp;

    /// <summary>
    /// Channels that interrupt a person, and therefore respect quiet hours and DND.
    /// </summary>
    /// <remarks>
    /// In-app and email do not interrupt: a badge and an inbox row wait to be looked at. A push at
    /// 2am wakes someone up, and an SMS at 2am wakes them up and costs us money.
    /// </remarks>
    public static bool IsIntrusive(this NotificationChannel channel)
        => channel is NotificationChannel.Push or NotificationChannel.Sms or NotificationChannel.Whatsapp;
}
