namespace AutoHub.Modules.Deals.Domain;

/// <summary>
/// Loads and saves deals. Implemented in the persistence layer.
/// </summary>
/// <remarks>
/// Declared here for the reason <c>IWorkflowStore</c> and <c>IPricingRuleStore</c> are declared in
/// <c>AutoHub.Platform</c>: the domain and the handlers never reference EF Core, so the settlement
/// arithmetic — which is the part that must not be wrong — is testable against a list.
/// <para>
/// <see cref="SaveChangesAsync"/> is on the repository rather than a separate unit of work because
/// the handlers in this module each change exactly one aggregate, which is the boundary
/// <c>AggregateRoot</c> exists to enforce. A handler that needed two would be a handler that should
/// have been an event.
/// </para>
/// </remarks>
public interface IDealRepository
{
    Task<Deal?> GetAsync(Guid dealId, CancellationToken cancellationToken);

    /// <summary>
    /// Finds the deal already opened for a source aggregate, if there is one.
    /// </summary>
    /// <remarks>
    /// The idempotency guard for every module that opens a deal on an event. An auction lot that
    /// closes twice — a retried effect, a redelivered outbox message — must find its existing deal
    /// rather than open a second one and double the seller's payout.
    /// </remarks>
    Task<Deal?> FindBySourceAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken);

    Task AddAsync(Deal deal, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
