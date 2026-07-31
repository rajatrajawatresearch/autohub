using System.Text.RegularExpressions;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Access;

/// <summary>
/// A permission code in the form <c>module.resource.action</c>.
/// </summary>
/// <remarks>
/// The shape is enforced rather than merely encouraged because the code is a namespace, and a
/// namespace only works if everybody spells it the same way. <c>listing.moderate</c> and
/// <c>listings.moderate</c> are two permissions, one of which nobody holds, and the failure mode is
/// a moderator who quietly cannot moderate.
/// <para>
/// The first segment is the module, which is what makes the admin permission matrix groupable
/// without a second column that can disagree with the code. Two to four segments are allowed:
/// <c>listing.moderate</c> reads better than an invented resource, and <c>auction.bid.place</c>
/// needs the resource to distinguish it from <c>auction.bid.retract</c>.
/// </para>
/// <para>
/// Wildcards are deliberately absent. A grant of <c>deal.*</c> looks harmless the day it is written
/// and silently swallows <c>deal.payout.approve</c> the day that permission is added — an
/// escalation nobody reviewed because nobody edited the role.
/// </para>
/// </remarks>
public readonly partial record struct PermissionCode
{
    private PermissionCode(string value, string module)
    {
        Value = value;
        Module = module;
    }

    /// <summary>The full dotted code, e.g. <c>auction.bid.place</c>.</summary>
    public string Value { get; }

    /// <summary>The first segment, e.g. <c>auction</c>. Groups the admin permission matrix.</summary>
    public string Module { get; }

    // Lowercase snake segments only. Mixed case is how the same permission ends up in the table twice.
    [GeneratedRegex(@"^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*){1,3}$")]
    private static partial Regex Pattern { get; }

    public static Result<PermissionCode> TryParse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Error.Validation("permission.code.required", "A permission code is required.");
        }

        var trimmed = input.Trim();

        if (!Pattern.IsMatch(trimmed))
        {
            return Error.Validation(
                "permission.code.malformed",
                "A permission code looks like 'module.resource.action', lowercase, 2–4 segments.")
                .With("input", input);
        }

        return new PermissionCode(trimmed, trimmed[..trimmed.IndexOf('.', StringComparison.Ordinal)]);
    }

    /// <summary>
    /// Parses a code that is a literal in the source, throwing if it is malformed.
    /// </summary>
    /// <remarks>
    /// Used only by <see cref="PermissionCatalog"/> declarations, which are compile-time constants.
    /// A malformed literal there is a programming error, and failing at startup is strictly better
    /// than shipping a permission nobody can ever be granted.
    /// </remarks>
    public static PermissionCode Declare(string code)
    {
        var parsed = TryParse(code);

        return parsed.IsSuccess
            ? parsed.Value
            : throw new ArgumentException(parsed.Error.Message, nameof(code));
    }

    public override string ToString() => Value;

    public static implicit operator string(PermissionCode code) => code.Value;
}
