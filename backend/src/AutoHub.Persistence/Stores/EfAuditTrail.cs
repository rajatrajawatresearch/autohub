using AutoHub.Platform.Auditing;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Stores;

/// <summary>
/// Appends audit rows into the caller's unit of work.
/// </summary>
/// <remarks>
/// <see cref="Record"/> deliberately does not save. The row has to commit with the change it
/// describes or not at all: an audit trail that survives a rolled-back publish claims a change that
/// never happened, and a trail that can lie is worse than no trail, because it is believed.
/// </remarks>
public sealed class EfAuditTrail : IAuditTrail
{
    private readonly AutoHubDbContext _context;

    public EfAuditTrail(AutoHubDbContext context) => _context = context;

    public void Record(AuditEntry entry) => _context.AuditEntries.Add(entry);

    public async Task<IReadOnlyList<AuditEntry>> RecentAsync(
        string? action,
        int limit,
        CancellationToken cancellationToken)
        => await _context.AuditEntries
            .AsNoTracking()
            .Where(entry => action == null || entry.Action == action)
            .OrderByDescending(entry => entry.OccurredAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
}
