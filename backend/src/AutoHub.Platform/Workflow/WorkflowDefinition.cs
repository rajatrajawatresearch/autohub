using System.Text.Json.Nodes;
using AutoHub.Platform.Expressions;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Workflow;

/// <summary>
/// The states an entity can be in and the moves between them — stored as data, not as an enum.
/// </summary>
/// <remarks>
/// A listing, a deal, an auction lot, a rental agreement, a loan application and a KYC case all have
/// a lifecycle, and in every marketplace that lifecycle changes more often than anything else in the
/// product. Operations wants a "legal hold" state before payout; compliance wants a second approval
/// above ₹10 lakh; a state gets renamed the week before launch.
/// <para>
/// Encoded as a C# enum plus <c>switch</c> statements, each of those is a deployment and a migration.
/// Encoded here, it is a row and an audit entry.
/// </para>
/// <para>
/// Definitions are <b>immutable once published</b> and versioned. Editing a live workflow would
/// strand in-flight entities in states that no longer exist; instead a new version is published and
/// existing instances continue to run against the version they started on.
/// </para>
/// </remarks>
public sealed class WorkflowDefinition : AggregateRoot, IAuditable
{
    private readonly List<WorkflowState> _states = [];
    private readonly List<WorkflowTransition> _transitions = [];

    private WorkflowDefinition(Guid id, string key, int version, string entityType, string name)
        : base(id)
    {
        Key = key;
        Version = version;
        EntityType = entityType;
        Name = name;
    }

    private WorkflowDefinition() { }

    /// <summary>Stable identifier, e.g. <c>listing</c>, <c>auction_lot</c>, <c>loan_application</c>.</summary>
    public string Key { get; private set; } = string.Empty;

    public int Version { get; private set; }

    /// <summary>The aggregate this workflow governs, e.g. <c>listings.listing</c>.</summary>
    public string EntityType { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    /// <summary>
    /// Unpublished definitions can be edited freely and are invisible to the engine. Publishing is
    /// the one-way door, and it runs <see cref="Validate"/> first.
    /// </summary>
    public bool IsPublished { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public IReadOnlyList<WorkflowState> States => _states;

    public IReadOnlyList<WorkflowTransition> Transitions => _transitions;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public static WorkflowDefinition Draft(string key, int version, string entityType, string name)
        => new(UuidV7.New(), key, version, entityType, name);

    public WorkflowState? FindState(string code)
        => _states.Find(s => string.Equals(s.Code, code, StringComparison.Ordinal));

    public WorkflowState? InitialState => _states.Find(s => s.IsInitial);

    /// <summary>Transitions leaving a state, in authored order — the order the UI shows actions in.</summary>
    public IEnumerable<WorkflowTransition> TransitionsFrom(string stateCode)
        => _transitions.Where(t => string.Equals(t.FromStateCode, stateCode, StringComparison.Ordinal));

    public WorkflowTransition? FindTransition(string fromStateCode, string actionCode)
        => _transitions.Find(t =>
            string.Equals(t.FromStateCode, fromStateCode, StringComparison.Ordinal) &&
            string.Equals(t.ActionCode, actionCode, StringComparison.Ordinal));

    public Result AddState(WorkflowState state)
    {
        if (IsPublished)
        {
            return PublishedIsImmutable();
        }

        if (FindState(state.Code) is not null)
        {
            return Error.Conflict("workflow.state.duplicate", $"State '{state.Code}' already exists.");
        }

        _states.Add(state);
        return Result.Success();
    }

    public Result AddTransition(WorkflowTransition transition)
    {
        if (IsPublished)
        {
            return PublishedIsImmutable();
        }

        if (FindTransition(transition.FromStateCode, transition.ActionCode) is not null)
        {
            return Error.Conflict(
                "workflow.transition.duplicate",
                $"Action '{transition.ActionCode}' is already defined on state '{transition.FromStateCode}'.");
        }

        _transitions.Add(transition);
        return Result.Success();
    }

    /// <summary>
    /// Proves the definition is runnable before anything depends on it.
    /// </summary>
    /// <remarks>
    /// Every check here exists because the alternative is discovering the problem at 2am with a
    /// customer's money in escrow. An unreachable state is a listing that can never be delisted;
    /// a missing terminal state is a deal that can never close; a dangling effect code is a
    /// commission that silently never posts.
    /// </remarks>
    public Result<IReadOnlyList<string>> Validate(IReadOnlySet<string> knownEffectCodes)
    {
        var problems = new List<string>();

        var initialStates = _states.Where(s => s.IsInitial).ToList();
        if (initialStates.Count != 1)
        {
            problems.Add($"Expected exactly one initial state, found {initialStates.Count}.");
        }

        if (!_states.Exists(s => s.IsTerminal))
        {
            problems.Add("No terminal state: entities in this workflow could never finish.");
        }

        foreach (var transition in _transitions)
        {
            if (FindState(transition.FromStateCode) is null)
            {
                problems.Add($"Transition '{transition.ActionCode}' leaves unknown state '{transition.FromStateCode}'.");
            }

            if (FindState(transition.ToStateCode) is null)
            {
                problems.Add($"Transition '{transition.ActionCode}' enters unknown state '{transition.ToStateCode}'.");
            }

            if (ConditionExpression.FromJson(transition.GuardJson).IsFailure)
            {
                problems.Add($"Transition '{transition.ActionCode}' has an unparseable guard.");
            }

            foreach (var effect in transition.Effects)
            {
                if (!knownEffectCodes.Contains(effect.EffectCode))
                {
                    problems.Add(
                        $"Transition '{transition.ActionCode}' references effect '{effect.EffectCode}', " +
                        "which no handler is registered for.");
                }
            }
        }

        // Reachability from the initial state. A state nobody can reach is either dead configuration
        // or — worse — a state someone believes is reachable and is relying on.
        if (initialStates.Count == 1)
        {
            var reachable = new HashSet<string>(StringComparer.Ordinal) { initialStates[0].Code };
            var frontier = new Queue<string>();
            frontier.Enqueue(initialStates[0].Code);

            while (frontier.Count > 0)
            {
                foreach (var transition in TransitionsFrom(frontier.Dequeue()))
                {
                    if (reachable.Add(transition.ToStateCode))
                    {
                        frontier.Enqueue(transition.ToStateCode);
                    }
                }
            }

            foreach (var state in _states.Where(s => !reachable.Contains(s.Code)))
            {
                problems.Add($"State '{state.Code}' is unreachable from the initial state.");
            }

            foreach (var state in _states.Where(s => !s.IsTerminal && !TransitionsFrom(s.Code).Any()))
            {
                problems.Add($"State '{state.Code}' is not terminal but has no outgoing transitions — a dead end.");
            }
        }

        return problems.Count == 0
            ? Result.Success<IReadOnlyList<string>>([])
            : Error.Validation("workflow.invalid", "This workflow cannot be published.")
                .With("problems", problems);
    }

    public Result Publish(IReadOnlySet<string> knownEffectCodes, DateTimeOffset now)
    {
        if (IsPublished)
        {
            return Error.Conflict("workflow.already_published", "This version is already published.");
        }

        var validation = Validate(knownEffectCodes);
        if (validation.IsFailure)
        {
            return Result.Failure(validation.Error);
        }

        IsPublished = true;
        PublishedAt = now;
        return Result.Success();
    }

    private static Error PublishedIsImmutable()
        => Error.Conflict(
            "workflow.published_immutable",
            "A published workflow cannot be changed. Create the next version instead.");
}

/// <summary>One state in a lifecycle.</summary>
public sealed class WorkflowState
{
    public WorkflowState(
        string code,
        string name,
        StateCategory category,
        bool isInitial = false,
        bool isTerminal = false,
        int? slaMinutes = null)
    {
        Code = code;
        Name = name;
        Category = category;
        IsInitial = isInitial;
        IsTerminal = isTerminal;
        SlaMinutes = slaMinutes;
    }

    private WorkflowState() { }

    public Guid Id { get; private set; } = UuidV7.New();

    /// <summary>Machine code, e.g. <c>pending_inspection</c>. Never shown to a user.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Default English label. The user-facing label is resolved from the i18n registry.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Drives presentation. The design system maps category to a <c>StatusPill</c> tone, so a new
    /// state gets the right colour without anyone storing a hex value in the database.
    /// </summary>
    public StateCategory Category { get; private set; }

    public bool IsInitial { get; private set; }

    public bool IsTerminal { get; private set; }

    /// <summary>How long an entity may sit here before it is escalated. Null means no SLA.</summary>
    public int? SlaMinutes { get; private set; }

    public int SortOrder { get; private set; }
}

public enum StateCategory
{
    Draft = 1,
    AwaitingAction = 2,
    InProgress = 3,
    Succeeded = 4,
    Failed = 5,
    Cancelled = 6,
}

/// <summary>A permitted move between two states.</summary>
public sealed class WorkflowTransition
{
    public WorkflowTransition(
        string actionCode,
        string label,
        string fromStateCode,
        string toStateCode,
        string? requiredPermission = null,
        string? guardJson = null,
        bool requiresReason = false,
        int? automaticAfterMinutes = null)
    {
        ActionCode = actionCode;
        Label = label;
        FromStateCode = fromStateCode;
        ToStateCode = toStateCode;
        RequiredPermission = requiredPermission;
        GuardJson = guardJson;
        RequiresReason = requiresReason;
        AutomaticAfterMinutes = automaticAfterMinutes;
    }

    private WorkflowTransition() { }

    private readonly List<WorkflowEffect> _effects = [];

    public Guid Id { get; private set; } = UuidV7.New();

    /// <summary>The verb, e.g. <c>submit_for_review</c>. Also the API action name.</summary>
    public string ActionCode { get; private set; } = string.Empty;

    /// <summary>Default button label. "Place bid", never "Submit".</summary>
    public string Label { get; private set; } = string.Empty;

    public string FromStateCode { get; private set; } = string.Empty;

    public string ToStateCode { get; private set; } = string.Empty;

    /// <summary>Permission the actor must hold, resolved against the RBAC registry.</summary>
    public string? RequiredPermission { get; private set; }

    /// <summary>JsonLogic evaluated against the transition context. Null means unconditional.</summary>
    public string? GuardJson { get; private set; }

    /// <summary>Rejections and cancellations need a reason on the audit trail.</summary>
    public bool RequiresReason { get; private set; }

    /// <summary>
    /// Fires without a user after this long in the source state — payment windows expiring,
    /// unsold lots relisting, unconfirmed bookings releasing their slot.
    /// </summary>
    public int? AutomaticAfterMinutes { get; private set; }

    /// <summary>What happens as a consequence. Ordered; each is a registered handler.</summary>
    public IReadOnlyList<WorkflowEffect> Effects => _effects;

    public int SortOrder { get; private set; }

    public void AddEffect(WorkflowEffect effect) => _effects.Add(effect);
}

/// <summary>
/// A named consequence of a transition.
/// </summary>
/// <remarks>
/// This is the boundary between data and code, and it is drawn deliberately.
/// <b>Which</b> effects run, in what order, with what arguments, is data an operator edits.
/// <b>What an effect can do</b> is code a developer writes, reviews, and tests.
/// <para>
/// The alternative — a scripting language in the database — hands production write access to
/// whoever can reach the admin panel. Named handlers keep the blast radius of a bad rule to
/// "the wrong notification was sent", never "the ledger was rewritten".
/// </para>
/// </remarks>
public sealed class WorkflowEffect
{
    public WorkflowEffect(string effectCode, JsonNode? arguments = null, bool continueOnFailure = false)
    {
        EffectCode = effectCode;
        ArgumentsJson = arguments?.ToJsonString();
        ContinueOnFailure = continueOnFailure;
    }

    private WorkflowEffect() { }

    public Guid Id { get; private set; } = UuidV7.New();

    /// <summary>Handler key, e.g. <c>notification.send</c>, <c>ledger.post_commission</c>.</summary>
    public string EffectCode { get; private set; } = string.Empty;

    public string? ArgumentsJson { get; private set; }

    /// <summary>
    /// Whether a failure here aborts the transition. Posting a commission must abort; sending a
    /// push notification must not — a dead APNs connection should never block a sale.
    /// </summary>
    public bool ContinueOnFailure { get; private set; }

    public int SortOrder { get; private set; }

    public JsonNode? Arguments()
        => string.IsNullOrWhiteSpace(ArgumentsJson) ? null : JsonNode.Parse(ArgumentsJson);
}
