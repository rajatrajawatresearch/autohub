using AutoHub.Platform.Forms;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.Persistence.Stores;

/// <summary>
/// Loads a form definition and every version of it.
/// </summary>
/// <remarks>
/// Loaded whole because variant selection has to compare every published version — the SUV sell form
/// against the generic one — and because a form is small: a few hundred rows at the very worst.
/// <para>
/// Steps, fields and rules travel with the versions. Serving reads the compiled
/// <see cref="FormVersion.SchemaJson"/>, but the compiler falls back to projecting from the rows
/// when a version predates compilation, and a fallback that silently produces a form with no fields
/// is worse than no fallback at all.
/// </para>
/// <para>
/// Tracked, unlike the snapshot stores, because this store also writes: the registry caches the
/// compiled document rather than the entity graph, so nothing tracked outlives the request.
/// </para>
/// </remarks>
public sealed class EfFormStore : IFormStore
{
    private readonly AutoHubDbContext _context;

    public EfFormStore(AutoHubDbContext context) => _context = context;

    public async Task<FormDefinition?> GetAsync(string key, CancellationToken cancellationToken)
        => await _context.FormDefinitions
            .Include(
                $"{nameof(FormDefinition.Versions)}.{nameof(FormVersion.Steps)}"
                + $".{nameof(FormStep.Fields)}.{nameof(FormField.Rules)}")
            .AsSplitQuery()
            .FirstOrDefaultAsync(definition => definition.Key == key, cancellationToken);

    public async Task AddAsync(FormDefinition definition, CancellationToken cancellationToken)
        => await _context.FormDefinitions.AddAsync(definition, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);
}
