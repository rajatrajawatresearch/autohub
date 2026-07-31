using System.Text.RegularExpressions;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Identity.Domain;

/// <summary>
/// An E.164 phone number, canonicalised once.
/// </summary>
/// <remarks>
/// This is the primary identity in an India-first product: users sign in with it, OTPs go to it,
/// and duplicate accounts are prevented by it. Users type it as "98123 45678", a dealer's CSV has
/// "+91-9812345678", and a WhatsApp webhook returns "919812345678" — all three are one person, so
/// canonicalisation happens here, once, and everything else compares <see cref="Value"/>.
/// <para>
/// <b>Structure is validated here; reachability is not.</b> Whether a prefix is a live Indian mobile
/// series, and whether our SMS provider can actually deliver to it, is data in the geography and
/// integration registries — an operations concern that must never require a deployment.
/// </para>
/// <para>
/// The default country code is configuration rather than a constant for the same reason: the PRD's
/// roadmap has a Gulf export market on it, and a hardcoded <c>+91</c> is the kind of assumption that
/// is cheap to parameterise now and expensive to unpick later.
/// </para>
/// </remarks>
public readonly partial record struct PhoneNumber
{
    private PhoneNumber(string value) => Value = value;

    /// <summary>E.164 with the leading plus, e.g. <c>+919812345678</c>. The stored, indexed form.</summary>
    public string Value { get; }

    [GeneratedRegex(@"^\+[1-9]\d{7,14}$")]
    private static partial Regex E164Pattern { get; }

    [GeneratedRegex(@"[\s\-()\.]")]
    private static partial Regex NoisePattern { get; }

    public static Result<PhoneNumber> TryParse(string? input, string defaultCallingCode)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Error.Validation("identity.phone.required", "Enter a mobile number.");
        }

        var stripped = NoisePattern.Replace(input, string.Empty);

        // "00" is the ITU international prefix people dial from a landline habit; "+" is the same
        // intent typed differently. Normalising both before validating means the check below has
        // exactly one shape to reason about.
        if (stripped.StartsWith("00", StringComparison.Ordinal))
        {
            stripped = string.Concat("+", stripped.AsSpan(2));
        }

        if (!stripped.StartsWith('+'))
        {
            // A national number typed without a country code. Prefixing is the forgiving reading and
            // the correct one: nobody in India types +91 into a phone field.
            stripped = string.Concat(defaultCallingCode, stripped.TrimStart('0'));
        }

        return E164Pattern.IsMatch(stripped)
            ? new PhoneNumber(stripped)
            : Error.Validation(
                "identity.phone.malformed",
                "That does not look like a mobile number. Example: 98123 45678.")
                .With("input", input);
    }

    /// <summary>
    /// The form shown back to the user before they wait for an SMS — <c>+91 98••• ••678</c>.
    /// </summary>
    /// <remarks>
    /// Masked because an OTP screen is a phishing surface: a caller who can read the full number off
    /// a shoulder-surfed screen has half of a social-engineering attack. Enough digits survive for
    /// the owner to recognise their own number, which is the only job this string has.
    /// </remarks>
    public string ToMaskedDisplay()
    {
        if (Value.Length < 7)
        {
            return Value;
        }

        var head = Value[..4];
        var tail = Value[^3..];
        return $"{head}{new string('•', Value.Length - 7)}{tail}";
    }

    public override string ToString() => Value;

    public static implicit operator string(PhoneNumber phone) => phone.Value;
}
