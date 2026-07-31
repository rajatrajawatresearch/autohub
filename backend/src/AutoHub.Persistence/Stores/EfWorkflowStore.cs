using AutoHub.Platform.Workflow;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Stores;

/// <summary>
/// Loads workflow definitions and instances, whole.
/// </summary>
/// <remarks>
/// Every read here eager-loads, and that is not an optimisation — it is correctness. A definition
/// without its states, transitions and effects is not a partially loaded definition, it is a
/// workflow with no moves: <c>FindTransition</c> returns null, the engine reports
/// <c>transition_not_allowed</c>, and the bug presents as "the Approve button does nothing" rather
/// than as a missing include.
/// <para>
/// Split queries throughout. A definition with twelve states, thirty transitions and sixty effects
/// joined in one statement returns twelve times thirty times sixty rows of duplicated parent
/// columns; split, it is three statements and three result sets the size of the actual data.
/// </para>
/// </remarks>
public sealed class EfWorkflowStore : IWorkflowStore
{
    private readonly AutoHubDbContext _context;

    public EfWorkflowStore(AutoHubDbContext context) => _context = context;

    public async Task<WorkflowDefinition?> GetPublishedDefinitionAsync(
        string key,
        CancellationToken cancellationToken)
        => await Definitions()
            .Where(definition => definition.Key == key && definition.IsPublished)
            // The newest published version. Older published versions still exist and are still
            // loaded by key-and-version for the instances pinned to them; new instances start on
            // the latest.
            .OrderByDescending(definition => definition.Version)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<WorkflowDefinition?> GetDefinitionVersionAsync(
        string key,
        int version,
        CancellationToken cancellationToken)
        => await Definitions()
            .FirstOrDefaultAsync(
                definition => definition.Key == key && definition.Version == version,
                cancellationToken);

    public async Task<WorkflowInstance?> GetInstanceAsync(
        string workflowKey,
        Guid entityId,
        CancellationToken cancellationToken)
        => await _context.WorkflowInstances
            .Include(nameof(WorkflowInstance.History))
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                instance => instance.WorkflowKey == workflowKey && instance.EntityId == entityId,
                cancellationToken);

    /// <summary>
    /// Instances whose timer has come due.
    /// </summary>
    /// <remarks>
    /// History is deliberately not loaded. The sweeper fires a transition and appends one row; the
    /// entries already on the instance are never read on this path, and loading a year of history
    /// for every expired payment window would make the batch size meaningless.
    /// </remarks>
    public async Task<IReadOnlyList<WorkflowInstance>> GetDueInstancesAsync(
        DateTimeOffset asOf,
        int batchSize,
        CancellationToken cancellationToken)
        => await _context.WorkflowInstances
            .Where(instance =>
                instance.DueAt != null
                && instance.DueAt <= asOf
                && instance.CompletedAt == null)
            .OrderBy(instance => instance.DueAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task AddInstanceAsync(WorkflowInstance instance, CancellationToken cancellationToken)
        => await _context.WorkflowInstances.AddAsync(instance, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);

    private IQueryable<WorkflowDefinition> Definitions()
        => _context.WorkflowDefinitions
            .Include(nameof(WorkflowDefinition.States))
            .Include($"{nameof(WorkflowDefinition.Transitions)}.{nameof(WorkflowTransition.Effects)}")
            .AsSplitQuery();
}
