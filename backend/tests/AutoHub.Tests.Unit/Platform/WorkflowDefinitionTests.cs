using AutoHub.Platform.Workflow;

namespace AutoHub.Tests.Unit.Platform;

public class WorkflowDefinitionTests
{
    private static readonly HashSet<string> KnownEffects =
        new(StringComparer.Ordinal) { "notification.send", "ledger.post_commission" };

    private static readonly DateTimeOffset Now = new(2026, 7, 29, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_well_formed_listing_lifecycle_validates()
    {
        var definition = ListingWorkflow();

        definition.Validate(KnownEffects).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Refuses_a_definition_with_no_terminal_state()
    {
        // Without one, a listing could never finish, and every instance would sit in the due-timer
        // sweep forever.
        var definition = WorkflowDefinition.Draft("listing", 1, "listings.listing", "Listing");
        definition.AddState(new WorkflowState("draft", "Draft", StateCategory.Draft, isInitial: true));
        definition.AddState(new WorkflowState("live", "Live", StateCategory.InProgress));
        definition.AddTransition(new WorkflowTransition("publish", "Publish", "draft", "live"));
        definition.AddTransition(new WorkflowTransition("unpublish", "Unpublish", "live", "draft"));

        var result = definition.Validate(KnownEffects);

        result.IsFailure.Should().BeTrue();
        Problems(result).Should().Contain(p => p.Contains("No terminal state"));
    }

    [Fact]
    public void Refuses_a_state_nobody_can_reach()
    {
        var definition = ListingWorkflow();
        definition.AddState(new WorkflowState("archived", "Archived", StateCategory.Cancelled, isTerminal: true));

        var result = definition.Validate(KnownEffects);

        result.IsFailure.Should().BeTrue();
        Problems(result).Should().Contain(p => p.Contains("'archived' is unreachable"));
    }

    [Fact]
    public void Refuses_a_dead_end_state()
    {
        var definition = ListingWorkflow();
        definition.AddState(new WorkflowState("on_hold", "On hold", StateCategory.AwaitingAction));
        definition.AddTransition(new WorkflowTransition("hold", "Put on hold", "live", "on_hold"));

        var result = definition.Validate(KnownEffects);

        result.IsFailure.Should().BeTrue();
        Problems(result).Should().Contain(p => p.Contains("'on_hold'") && p.Contains("dead end"));
    }

    [Fact]
    public void Refuses_an_effect_no_handler_is_registered_for()
    {
        // Publish-time is the only cheap moment to catch this. At runtime it is a commission that
        // silently never posted.
        var definition = ListingWorkflow();
        var transition = new WorkflowTransition("sell", "Mark sold", "live", "sold");
        transition.AddEffect(new WorkflowEffect("ledger.post_typo"));
        definition.AddTransition(transition);

        var result = definition.Validate(KnownEffects);

        result.IsFailure.Should().BeTrue();
        Problems(result).Should().Contain(p => p.Contains("ledger.post_typo"));
    }

    [Fact]
    public void Refuses_more_than_one_initial_state()
    {
        var definition = ListingWorkflow();
        definition.AddState(new WorkflowState("imported", "Imported", StateCategory.Draft, isInitial: true));

        Problems(definition.Validate(KnownEffects))
            .Should().Contain(p => p.Contains("exactly one initial state"));
    }

    [Fact]
    public void Publishing_an_invalid_definition_is_refused()
    {
        var definition = WorkflowDefinition.Draft("broken", 1, "x.y", "Broken");
        definition.AddState(new WorkflowState("only", "Only", StateCategory.Draft, isInitial: true));

        var result = definition.Publish(KnownEffects, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("workflow.invalid");
        definition.IsPublished.Should().BeFalse();
    }

    [Fact]
    public void A_published_definition_cannot_be_edited()
    {
        // Editing a live workflow would strand in-flight entities in states that no longer exist.
        // The answer is a new version, not a mutation.
        var definition = ListingWorkflow();
        definition.Publish(KnownEffects, Now).IsSuccess.Should().BeTrue();

        var addState = definition.AddState(
            new WorkflowState("extra", "Extra", StateCategory.Draft));

        addState.IsFailure.Should().BeTrue();
        addState.Error.Code.Should().Be("workflow.published_immutable");
    }

    [Fact]
    public void Duplicate_actions_on_the_same_state_are_refused()
    {
        var definition = ListingWorkflow();

        var duplicate = definition.AddTransition(
            new WorkflowTransition("submit", "Submit again", "draft", "pending_review"));

        duplicate.IsFailure.Should().BeTrue();
        duplicate.Error.Code.Should().Be("workflow.transition.duplicate");
    }

    /// <summary>The real listing lifecycle, including a guard and a timer, used across these tests.</summary>
    internal static WorkflowDefinition ListingWorkflow()
    {
        var definition = WorkflowDefinition.Draft("listing", 1, "listings.listing", "Listing");

        definition.AddState(new WorkflowState("draft", "Draft", StateCategory.Draft, isInitial: true));
        definition.AddState(new WorkflowState(
            "pending_review", "Under review", StateCategory.AwaitingAction, slaMinutes: 240));
        definition.AddState(new WorkflowState("live", "Live", StateCategory.InProgress));
        definition.AddState(new WorkflowState("sold", "Sold", StateCategory.Succeeded, isTerminal: true));
        definition.AddState(new WorkflowState("rejected", "Rejected", StateCategory.Failed, isTerminal: true));
        definition.AddState(new WorkflowState("expired", "Expired", StateCategory.Cancelled, isTerminal: true));

        // Guarded: a listing without photos must not reach a moderator's queue.
        definition.AddTransition(new WorkflowTransition(
            "submit", "Submit for review", "draft", "pending_review",
            requiredPermission: "listing.submit",
            guardJson: """{">=":[{"var":"listing.photoCount"},6]}"""));

        var approve = new WorkflowTransition(
            "approve", "Approve", "pending_review", "live", requiredPermission: "listing.moderate");
        approve.AddEffect(new WorkflowEffect("notification.send", continueOnFailure: true));
        definition.AddTransition(approve);

        definition.AddTransition(new WorkflowTransition(
            "reject", "Reject", "pending_review", "rejected",
            requiredPermission: "listing.moderate", requiresReason: true));

        var sell = new WorkflowTransition("mark_sold", "Mark sold", "live", "sold");
        sell.AddEffect(new WorkflowEffect("ledger.post_commission"));
        definition.AddTransition(sell);

        // Timer: a live listing nobody touches expires after 60 days.
        definition.AddTransition(new WorkflowTransition(
            "expire", "Expire", "live", "expired", automaticAfterMinutes: 60 * 24 * 60));

        return definition;
    }

    // Fully qualified: this assembly also has an AutoHub.Tests.Unit.SharedKernel namespace, which
    // would otherwise win the lookup for a relative "SharedKernel.Results".
    private static IReadOnlyList<string> Problems(
        global::AutoHub.SharedKernel.Results.Result<IReadOnlyList<string>> result)
        => result.Error.Metadata.TryGetValue("problems", out var value) && value is IReadOnlyList<string> list
            ? list
            : [];
}
