using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Localization;

/// <summary>
/// One translated string, addressed by namespace + key + locale.
/// </summary>
/// <remarks>
/// The quality floor forbids literals in components: "every user-facing string comes from the
/// localisation registry". That is not only about Hindi and Tamil — it is about being able to change
/// "Get my price" to "Check my car's value" on a Thursday afternoon because conversion dropped,
/// across web, Android and iOS, without three app releases and a store review.
/// <para>
/// India makes the multilingual case unavoidable rather than aspirational. A seller in Nashik and a
/// dealer in Coimbatore do not share a language, and asking either to transact in English is asking
/// them to transact somewhere else.
/// </para>
/// </remarks>
public sealed class I18nResource : AggregateRoot, IAuditable
{
    private I18nResource(Guid id, string @namespace, string key, string locale, string value)
        : base(id)
    {
        Namespace = @namespace;
        Key = key;
        Locale = locale;
        Value = value;
    }

    private I18nResource() { }

    /// <summary>
    /// The bundle this string ships in, e.g. <c>web</c>, <c>dealer</c>, <c>errors</c>, <c>sms</c>.
    /// </summary>
    /// <remarks>
    /// Namespaces exist so a customer opening the home page downloads the strings that page needs
    /// and not the four thousand belonging to the admin console. On a 3G connection in a tier-3 city
    /// that difference is the difference between a page that loads and one that does not.
    /// </remarks>
    public string Namespace { get; private set; } = string.Empty;

    /// <summary>
    /// Dotted, structural, and never the English text: <c>listing.cta.get_valuation</c>.
    /// </summary>
    /// <remarks>
    /// Keying by the English sentence — the approach several i18n libraries encourage — means
    /// fixing a typo in English orphans every translation of it. Structural keys survive copy edits.
    /// Error codes are keys too, which is why <c>Error.Code</c> is a stable contract: the message a
    /// user reads for <c>registration.malformed</c> is a row here, not a string in a validator.
    /// </remarks>
    public string Key { get; private set; } = string.Empty;

    /// <summary>BCP-47 with region, e.g. <c>hi-IN</c>. Region matters: <c>hi-IN</c> is not <c>hi</c>.</summary>
    public string Locale { get; private set; } = string.Empty;

    public string Value { get; private set; } = string.Empty;

    /// <summary>
    /// Context for the translator. "Button on the valuation card; keep under 18 characters."
    /// </summary>
    /// <remarks>
    /// Without it a translator sees the word "Book" and has to guess between the verb and the noun.
    /// Devanagari and Tamil also set roughly 20-30% wider than Latin at the same point size, so the
    /// length budget has to travel with the string or the button wraps.
    /// </remarks>
    public string? Description { get; private set; }

    /// <summary>
    /// Whether this row participates in bundles. Deactivating is how a bad translation is pulled
    /// without deleting it — the string falls back to the parent locale instead of disappearing.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static Result<I18nResource> Create(string @namespace, string key, string locale, string value)
    {
        var validation = ValidateValue(value);
        if (validation.IsFailure)
        {
            return Result.Failure<I18nResource>(validation.Error);
        }

        return new I18nResource(UuidV7.New(), @namespace, key, locale, value);
    }

    /// <summary>
    /// Replaces the text, refusing translations that reference variables the source string does not
    /// carry.
    /// </summary>
    public Result Update(string value, IReadOnlyList<string>? declaredPlaceholders = null)
    {
        var validation = ValidateValue(value);
        if (validation.IsFailure)
        {
            return validation;
        }

        if (declaredPlaceholders is not null)
        {
            var undeclared = MessageTemplate.UndeclaredPlaceholders(value, declaredPlaceholders);
            if (undeclared.Count > 0)
            {
                return Error.Validation(
                    "i18n.placeholder_undeclared",
                    "This translation uses a variable the original string does not provide.")
                    .With("placeholders", undeclared);
            }
        }

        Value = value;
        return Result.Success();
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public IReadOnlyList<string> Placeholders() => MessageTemplate.Placeholders(Value);

    /// <summary>
    /// Blank is rejected at the door.
    /// </summary>
    /// <remarks>
    /// A blank button is worse than an untranslated one: an untranslated button says "Get my price"
    /// in English and still works, while a blank one is an unlabelled rectangle that a screen reader
    /// announces as nothing at all. Storing empty is therefore not permitted — a string that should
    /// not appear is deactivated, which makes it fall back rather than vanish.
    /// </remarks>
    private static Result ValidateValue(string value)
        => string.IsNullOrWhiteSpace(value)
            ? Error.Validation("i18n.value_empty", "A resource value cannot be blank.")
            : Result.Success();
}

/// <summary>
/// A supported locale and the locale it falls back to.
/// </summary>
/// <remarks>
/// The list of locales is data, not an enum, for the same reason everything else here is: launching
/// Kannada should be an insert and a translation import, not a release. The fallback edge is stored
/// per-locale rather than derived from the language tag, because the right parent is a product
/// decision — Marathi falls back to Hindi (shared script, largely intelligible) while Tamil falls
/// back to English (different script, no mutual intelligibility), and no algorithm knows that.
/// </remarks>
public sealed class I18nLocale : AggregateRoot, IAuditable
{
    private I18nLocale(Guid id, string code, string englishName, string nativeName)
        : base(id)
    {
        Code = code;
        EnglishName = englishName;
        NativeName = nativeName;
    }

    private I18nLocale() { }

    /// <summary>BCP-47, e.g. <c>mr-IN</c>.</summary>
    public string Code { get; private set; } = string.Empty;

    public string EnglishName { get; private set; } = string.Empty;

    /// <summary>
    /// The name in its own script — मराठी, தமிழ். A language picker that lists "Marathi" in Latin
    /// is unusable by exactly the person who needs it.
    /// </summary>
    public string NativeName { get; private set; } = string.Empty;

    /// <summary>Where a missing key looks next. Null means this locale is the end of a chain.</summary>
    public string? FallbackCode { get; private set; }

    public bool IsActive { get; private set; } = true;

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static I18nLocale Create(
        string code,
        string englishName,
        string nativeName,
        string? fallbackCode = null,
        int sortOrder = 0)
        => new(UuidV7.New(), code, englishName, nativeName)
        {
            FallbackCode = fallbackCode,
            SortOrder = sortOrder,
        };

    public void Deactivate() => IsActive = false;
}
