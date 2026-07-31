using System.Text.Json.Nodes;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoHub.Tests.Unit.Platform;

public class WorkflowEngineTests
{
    private static readonly DateTimeOffset Start = new(2026, 7, 29, 6, 0, 0, TimeSpan.Zero);

    private static readonly HashSet<string> Permissions =
        new(StringComparer.Ordinal) { "listing.submit", "listing.moderate" };

    [Fact]
    public async Task Starting_places_the_entity_in_the_initial_state()
    {
        var (engine, _, _) = Build();
        var listingId = UuidV7.New();

        var snapshot = await engine.StartAsync("listing", listingId, null, null, default);

        snapshot.IsSuccess.Should().BeTrue();
        snapshot.Value.CurrentState.Code.Should().Be("draft");
        snapshot.Value.History.Should().ContainSingle();
    }

    [Fact]
    public async Task Starting_twice_does_not_create_a_second_lifecycle()
    {
        // A retried create must not give one listing two histories and two due timers.
        var (engine, store, _) = Build();
        var listingId = UuidV7.New();

        await engine.StartAsync("listing", listingId, null, null, default);
        await engine.StartAsync("listing", listingId, null, null, default);

        store.Instances.Should().ContainSingle();
    }

    [Fact]
    public async Task A_guard_that_fails_blocks_the_transition()
    {
        var (engine, _, _) = Build();
        var listingId = UuidV7.New();
        await engine.StartAsync("listing", listingId, null, null, default);

        // The listing workflow requires six photos before a moderator sees it.
        var result = await Fire(engine, listingId, "submit", PhotoCount(3));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("workflow.guard_failed");
    }

    [Fact]
    public async Task A_guard_that_passes_allows_the_transition()
    {
        var (engine, _, _) = Build();
        var listingId = UuidV7.New();
        await engine.StartAsync("listing", listingId, null, null, default);

        var result = await Fire(engine, listingId, "submit", PhotoCount(8));

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentState.Code.Should().Be("pending_review");
    }

    [Fact]
    public async Task An_actor_without_the_permission_is_refused()
    {
        var (engine, _, _) = Build();
        var listingId = UuidV7.New();
        await engine.StartAsync("listing", listingId, null, null, default);

        var result = await engine.FireAsync(
            new WorkflowFireRequest
            {
                WorkflowKey = "listing",
                EntityId = listingId,
                ActionCode = "submit",
                Actor = new WorkflowActor(UuidV7.New(), new HashSet<string>(StringComparer.Ordinal)),
                Context = PhotoCount(8),
            },
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("workflow.permission_denied");
    }

    [Fact]
    public async Task An_action_that_does_not_exist_on_the_current_state_returns_the_current_state()
    {
        // This is what a stale browser tab does. The client needs the current state back so it can
        // refresh rather than guess.
        var (engine, _, _) = Build();
        var listingId = UuidV7.New();
        await engine.StartAsync("listing", listingId, null, null, default);

        var result = await Fire(engine, listingId, "approve", null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("workflow.transition_not_allowed");
        result.Error.Metadata["currentState"].Should().Be("draft");
    }

    [Fact]
    public async Task A_transition_that_demands_a_reason_refuses_an_empty_one()
    {
        var (engine, _, _) = Build();
        var listingId = await LiveListing(engine, moderated: false);

        var result = await engine.FireAsync(
            new WorkflowFireRequest
            {
                WorkflowKey = "listing",
                EntityId = listingId,
                ActionCode = "reject",
                Actor = new WorkflowActor(UuidV7.New(), Permissions),
                Reason = "   ",
            },
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("workflow.reason_required");
    }

    [Fact]
    public async Task Available_actions_are_filtered_by_permission_and_by_guard()
    {
        var (engine, _, _) = Build();
        var listingId = UuidV7.New();
        await engine.StartAsync("listing", listingId, null, null, default);

        var withoutPhotos = await engine.GetSnapshotAsync(
            "listing", listingId, PhotoCount(2),
            new WorkflowActor(UuidV7.New(), Permissions), default);

        var withPhotos = await engine.GetSnapshotAsync(
            "listing", listingId, PhotoCount(9),
            new WorkflowActor(UuidV7.New(), Permissions), default);

        withoutPhotos.Value.AvailableActions.Should().BeEmpty();
        withPhotos.Value.AvailableActions.Select(a => a.ActionCode).Should().Contain("submit");
    }

    [Fact]
    public async Task A_failing_critical_effect_aborts_the_transition_and_leaves_the_state_alone()
    {
        // A commission that fails to post must never produce a "sold" listing with no money moved.
        var (engine, _, effects) = Build();
        effects.FailOn("ledger.post_commission");

        var listingId = await LiveListing(engine, moderated: true);

        var result = await Fire(engine, listingId, "mark_sold", null);

        result.IsFailure.Should().BeTrue();

        var after = await engine.GetSnapshotAsync(
            "listing", listingId, null, new WorkflowActor(UuidV7.New(), Permissions), default);
        after.Value.CurrentState.Code.Should().Be("live");
    }

    [Fact]
    public async Task A_failing_non_critical_effect_does_not_block_the_transition()
    {
        // A dead push-notification connection must never block a moderator approving a listing.
        var (engine, _, effects) = Build();
        effects.FailOn("notification.send");

        var listingId = await LiveListing(engine, moderated: true);

        var snapshot = await engine.GetSnapshotAsync(
            "listing", listingId, null, new WorkflowActor(UuidV7.New(), Permissions), default);
        snapshot.Value.CurrentState.Code.Should().Be("live");
    }

    [Fact]
    public async Task Reaching_a_terminal_state_completes_the_instance_and_closes_it_to_further_actions()
    {
        var (engine, _, _) = Build();
        var listingId = await LiveListing(engine, moderated: true);

        var sold = await Fire(engine, listingId, "mark_sold", null);
        sold.IsSuccess.Should().BeTrue();
        sold.Value.IsComplete.Should().BeTrue();
        sold.Value.AvailableActions.Should().BeEmpty();

        var again = await Fire(engine, listingId, "mark_sold", null);
        again.Error.Code.Should().Be("workflow.already_complete");
    }

    [Fact]
    public async Task A_timer_transition_fires_only_once_it_is_due()
    {
        var (engine, store, _) = Build(out var clock);
        var listingId = await LiveListing(engine, moderated: true);

        // The live listing expires after 60 days.
        (await engine.RunDueTransitionsAsync(10, default)).Value.Should().Be(0);

        clock.Advance(TimeSpan.FromDays(61));
        (await engine.RunDueTransitionsAsync(10, default)).Value.Should().Be(1);

        store.Instances[0].CurrentStateCode.Should().Be("expired");
    }

    [Fact]
    public async Task A_timer_transition_is_never_offered_as_a_button()
    {
        var (engine, _, _) = Build();
        var listingId = await LiveListing(engine, moderated: true);

        var snapshot = await engine.GetSnapshotAsync(
            "listing", listingId, null, new WorkflowActor(UuidV7.New(), Permissions), default);

        snapshot.Value.AvailableActions.Select(a => a.ActionCode).Should().NotContain("expire");
        snapshot.Value.AvailableActions.Select(a => a.ActionCode).Should().Contain("mark_sold");
    }

    [Fact]
    public async Task The_history_records_who_did_what_and_how_long_it_took()
    {
        var (engine, _, _) = Build(out var clock);
        var actor = UuidV7.New();
        var listingId = UuidV7.New();
        await engine.StartAsync("listing", listingId, null, actor, default);

        clock.Advance(TimeSpan.FromHours(17));

        var result = await engine.FireAsync(
            new WorkflowFireRequest
            {
                WorkflowKey = "listing",
                EntityId = listingId,
                ActionCode = "submit",
                Actor = new WorkflowActor(actor, Permissions),
                Context = PhotoCount(8),
            },
            default);

        var entry = result.Value.History[^1];
        entry.FromStateCode.Should().Be("draft");
        entry.ToStateCode.Should().Be("pending_review");
        entry.ActionCode.Should().Be("submit");
        entry.ActorId.Should().Be(actor);
        entry.TimeInPreviousState.Should().Be(TimeSpan.FromHours(17));
    }

    // ── harness ──────────────────────────────────────────────────────────────────

    private static JsonObject PhotoCount(int count)
        => new JsonObject { ["listing"] = new JsonObject { ["photoCount"] = count } };

    private static Task<Result<WorkflowSnapshot>> Fire(
        IWorkflowEngine engine, Guid entityId, string action, JsonNode? context)
        => engine.FireAsync(
            new WorkflowFireRequest
            {
                WorkflowKey = "listing",
                EntityId = entityId,
                ActionCode = action,
                Actor = new WorkflowActor(UuidV7.New(), Permissions),
                Context = context,
                Reason = "test",
            },
            default);

    private static async Task<Guid> LiveListing(IWorkflowEngine engine, bool moderated)
    {
        var listingId = UuidV7.New();
        await engine.StartAsync("listing", listingId, null, null, default);
        await Fire(engine, listingId, "submit", PhotoCount(8));

        if (moderated)
        {
            await Fire(engine, listingId, "approve", null);
        }

        return listingId;
    }

    private static (IWorkflowEngine Engine, InMemoryStore Store, RecordingEffects Effects) Build()
        => Build(out _);

    private static (IWorkflowEngine Engine, InMemoryStore Store, RecordingEffects Effects) Build(
        out MutableClock clock)
    {
        var definition = WorkflowDefinitionTests.ListingWorkflow();
        var effects = new RecordingEffects();
        definition.Publish(effects.KnownEffectCodes.ToHashSet(StringComparer.Ordinal), Start)
            .IsSuccess.Should().BeTrue();

        var store = new InMemoryStore(definition);
        clock = new MutableClock(Start);

        return (
            new WorkflowEngine(store, effects, clock, NullLogger<WorkflowEngine>.Instance),
            store,
            effects);
    }

    private sealed class InMemoryStore(WorkflowDefinition definition) : IWorkflowStore
    {
        public List<WorkflowInstance> Instances { get; } = [];

        public Task<WorkflowDefinition?> GetPublishedDefinitionAsync(string key, CancellationToken ct)
            => Task.FromResult<WorkflowDefinition?>(definition.Key == key ? definition : null);

        public Task<WorkflowDefinition?> GetDefinitionVersionAsync(string key, int version, CancellationToken ct)
            => Task.FromResult<WorkflowDefinition?>(
                definition.Key == key && definition.Version == version ? definition : null);

        public Task<WorkflowInstance?> GetInstanceAsync(string key, Guid entityId, CancellationToken ct)
            => Task.FromResult(Instances.Find(i => i.WorkflowKey == key && i.EntityId == entityId));

        public Task<IReadOnlyList<WorkflowInstance>> GetDueInstancesAsync(
            DateTimeOffset asOf, int batchSize, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<WorkflowInstance>>(
                Instances
                    .Where(i => !i.IsComplete && i.DueAt is { } due && due != default && due <= asOf)
                    .Take(batchSize)
                    .ToList());

        public Task AddInstanceAsync(WorkflowInstance instance, CancellationToken ct)
        {
            Instances.Add(instance);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class RecordingEffects : IWorkflowEffectDispatcher
    {
        private readonly HashSet<string> _failing = new(StringComparer.Ordinal);

        public IReadOnlySet<string> KnownEffectCodes { get; } =
            new HashSet<string>(StringComparer.Ordinal) { "notification.send", "ledger.post_commission" };

        public List<string> Dispatched { get; } = [];

        public void FailOn(string effectCode) => _failing.Add(effectCode);

        public Task<Result> DispatchAsync(WorkflowEffectContext context, CancellationToken ct)
        {
            foreach (var effect in context.Transition.Effects)
            {
                Dispatched.Add(effect.EffectCode);

                if (_failing.Contains(effect.EffectCode) && !effect.ContinueOnFailure)
                {
                    return Task.FromResult(
                        Result.Failure(global::AutoHub.SharedKernel.Results.Error.Upstream(
                            "test.effect_failed", $"{effect.EffectCode} failed.")));
                }
            }

            return Task.FromResult(Result.Success());
        }
    }

    private sealed class MutableClock(DateTimeOffset start) : IClock
    {
        private DateTimeOffset _now = start;

        public DateTimeOffset UtcNow => _now;

        public DateOnly TodayInIst => DateOnly.FromDateTime(_now.ToOffset(SystemClock.IstOffset).Date);

        public void Advance(TimeSpan by) => _now = _now.Add(by);
    }
}
