using System.Text.RegularExpressions;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Taxonomy;

/// <summary>
/// The shape every taxonomy code must have.
/// </summary>
/// <remarks>
/// A code is not a label. It appears in URLs (<c>/platform/taxonomy/models?make=maruti-suzuki</c>),
/// as a JSON key on an attribute bag, as an OpenSearch field suffix, and inside cache keys — and
/// each of those breaks differently on a space, a slash, or a capital letter. Validating once here
/// is cheaper than discovering in production that a category someone typed as "SUV / Crossover"
/// produced an unroutable URL and a field name OpenSearch refuses.
/// <para>
/// Codes are <b>validated, never silently lowercased</b>. Quietly folding "SUV" into "suv" means an
/// admin can create what looks like two categories and get one, or believe they created one and get
/// two. Refusing the input costs a form error; accepting it costs a data-repair script.
/// </para>
/// <para>
/// Both hyphen and underscore are allowed because the API contract already uses both, deliberately:
/// makes and models are hyphenated slugs because they sit in URLs and are read by humans and search
/// engines, while attribute codes are underscored because they become identifiers in JSON, in
/// OpenSearch, and in generated form field names.
/// </para>
/// </remarks>
internal static partial class TaxonomyCode
{
    // Two to sixty-four characters: long enough for `construction_equipment_boom_length`, short
    // enough to stay inside an index entry and a cache key without truncation.
    [GeneratedRegex("^[a-z0-9][a-z0-9_-]{1,63}$")]
    private static partial Regex Pattern { get; }

    public static Result<string> Validate(string? code, string errorCode)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Error.Validation(errorCode, "A code is required.");
        }

        var trimmed = code.Trim();

        return Pattern.IsMatch(trimmed)
            ? trimmed
            : Error.Validation(
                errorCode,
                "A code must be lowercase letters, digits, hyphens or underscores, 2–64 characters.")
                .With("code", code);
    }
}
