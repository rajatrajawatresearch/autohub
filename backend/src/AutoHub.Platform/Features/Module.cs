using System.Text.RegularExpressions;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Features;

/// <summary>
/// A product surface that can be switched off without a deploy.
/// </summary>
/// <remarks>
/// The PRD ships buy, sell, rent, lease, auction, attachment, finance and insurance. They will not
/// all launch on the same day, in the same city, to the same audience — lease goes live in Bengaluru
/// to dealers only, attachment goes live where there are enough fleet operators to make it work.
/// Without this row, each of those is a feature branch and a release; with it, each is a toggle.
/// <para>
/// A module being off is not a hidden menu item. It is the API refusing the endpoint, which is the
/// only version of "off" that survives someone typing the URL.
/// </para>
/// </remarks>
public sealed partial class Module : AggregateRoot, IAuditable
{
    private Module(Guid id, string code, string name, string? description, bool isEnabled) : base(id)
    {
        Code = code;
        Name = name;
        Description = description;
        IsEnabled = isEnabled;
    }

    private Module() { }

    /// <summary>Matches the module's code namespace, e.g. <c>auction</c>, <c>lease</c>.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    /// <summary>The master switch. Off here means off everywhere, regardless of any flag.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// An optional feature flag that further narrows who and where the module is available.
    /// </summary>
    /// <remarks>
    /// Reusing the flag machinery instead of giving modules their own targeting rules is the whole
    /// design of this registry. "Launch lease in Bengaluru, to dealers only" becomes
    /// <see cref="IsEnabled"/> plus a flag with a city-and-party rule — no second rule evaluator, no
    /// second admin screen, and no second set of bugs to find in it. A module with no gating flag is
    /// available wherever it is enabled.
    /// </remarks>
    public string? GatingFlagKey { get; private set; }

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    [GeneratedRegex("^[a-z][a-z0-9_]*$")]
    private static partial Regex CodePattern { get; }

    public static Result<Module> Create(
        string code,
        string name,
        bool isEnabled = false,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code) || !CodePattern.IsMatch(code.Trim()))
        {
            return Error.Validation(
                "module.code.malformed",
                "A module code is lowercase letters, digits and underscores, e.g. 'auction'.")
                .With("input", code);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("module.name.required", "A module needs a human-readable name.");
        }

        return new Module(UuidV7.New(), code.Trim(), name.Trim(), description, isEnabled);
    }

    public void Enable() => IsEnabled = true;

    public void Disable() => IsEnabled = false;

    /// <summary>Points the module at a flag, or clears the gate by passing null.</summary>
    public Result GateBehind(string? flagKey)
    {
        if (flagKey is null)
        {
            GatingFlagKey = null;
            return Result.Success();
        }

        var parsed = FeatureFlag.ParseKey(flagKey);
        if (parsed.IsFailure)
        {
            return Result.Failure(parsed.Error);
        }

        GatingFlagKey = parsed.Value;
        return Result.Success();
    }

    public void Reorder(int sortOrder) => SortOrder = sortOrder;
}
