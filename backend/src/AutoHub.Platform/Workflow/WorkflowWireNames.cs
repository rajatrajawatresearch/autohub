using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Workflow;

/// <summary>
/// The published spelling of <see cref="StateCategory"/>.
/// </summary>
/// <remarks>
/// Written down for the same reason <c>TaxonomyWireNames</c> is: the design system maps
/// <c>awaiting_action</c> to a <c>StatusPill</c> tone, three clients already branch on the string,
/// and a serializer naming policy would let a harmless C# rename silently repaint every status
/// badge in the field with nothing in the compiler to notice.
/// <para>
/// Adding a member to <see cref="StateCategory"/> without adding it here is a compile error, which
/// is the only kind of reminder that works.
/// </para>
/// </remarks>
public static class WorkflowWireNames
{
    public static string ToWireName(this StateCategory category) => category switch
    {
        StateCategory.Draft => "draft",
        StateCategory.AwaitingAction => "awaiting_action",
        StateCategory.InProgress => "in_progress",
        StateCategory.Succeeded => "succeeded",
        StateCategory.Failed => "failed",
        StateCategory.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unmapped state category."),
    };

    /// <summary>Parses an inbound wire value — admin writes and seed files both arrive as strings.</summary>
    public static Result<StateCategory> ParseCategory(string? wireName) => wireName switch
    {
        "draft" => StateCategory.Draft,
        "awaiting_action" => StateCategory.AwaitingAction,
        "in_progress" => StateCategory.InProgress,
        "succeeded" => StateCategory.Succeeded,
        "failed" => StateCategory.Failed,
        "cancelled" => StateCategory.Cancelled,
        _ => Error.Validation(
            "workflow.state.category_unknown",
            $"'{wireName}' is not a supported state category.")
            .With("category", wireName),
    };
}
