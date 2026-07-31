using AutoHub.Platform.Taxonomy;
using AutoHub.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using CatalogStore = AutoHub.Modules.Catalog.Application.Abstractions.IEntityAttributeStore;
using ListingsStore = AutoHub.Modules.Listings.Application.Abstractions.IEntityAttributeStore;

namespace AutoHub.Persistence.Repositories;

/// <summary>
/// Writes validated attribute values into the platform's shared EAV table.
/// </summary>
/// <remarks>
/// One class implementing both modules' interfaces. The modules declare their own copies so neither
/// depends on the other or on a shared assembly — that is what keeps extraction a build-file change
/// — and the duplication costs an interface declaration and nothing at runtime, because the
/// behaviour genuinely is identical.
/// <para>
/// <b>Replace, not merge.</b> Values the caller did not send are deleted, because a merge cannot
/// express a seller unticking a box, and a listing that keeps claiming a sunroof it does not have is
/// a dispute rather than a bug report.
/// </para>
/// </remarks>
public sealed class EfEntityAttributeStore : CatalogStore, ListingsStore
{
    private readonly AutoHubDbContext _context;

    public EfEntityAttributeStore(AutoHubDbContext context) => _context = context;

    public async Task<Result> ReplaceAsync(
        string entityType,
        Guid entityId,
        IReadOnlyDictionary<string, TypedAttributeValue> values,
        CancellationToken cancellationToken)
    {
        var codes = values.Keys.ToArray();

        var definitions = codes.Length == 0
            ? []
            : await _context.AttributeDefinitions
                .Where(definition => codes.Contains(definition.Code))
                .Select(definition => new { definition.Id, definition.Code })
                .ToListAsync(cancellationToken);

        var idsByCode = definitions.ToDictionary(d => d.Code, d => d.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var code in codes)
        {
            if (!idsByCode.ContainsKey(code))
            {
                // The taxonomy registry validated these codes moments ago, so reaching this means a
                // definition was hard-deleted between validation and write. Failing loudly beats
                // storing an orphan row nobody can ever read back.
                return Error.Unexpected(
                    "taxonomy.attribute.definition_missing",
                    $"Attribute '{code}' has no definition to store a value against.")
                    .With("attributeCode", code);
            }
        }

        var existing = await _context.EntityAttributeValues
            .Where(value => value.EntityType == entityType && value.EntityId == entityId)
            .ToListAsync(cancellationToken);

        var byDefinition = existing.ToDictionary(value => value.AttributeDefinitionId);

        foreach (var (code, value) in values)
        {
            var definitionId = idsByCode[code];

            if (byDefinition.TryGetValue(definitionId, out var row))
            {
                // Updated in place rather than deleted and reinserted, so the audit columns keep
                // saying when the value was first recorded — which is what an inspection dispute
                // about "the seller changed the odometer reading" turns on.
                row.Replace(value);
                byDefinition.Remove(definitionId);
                continue;
            }

            await _context.EntityAttributeValues.AddAsync(
                EntityAttributeValue.Create(entityType, entityId, definitionId, value),
                cancellationToken);
        }

        // Whatever is left was not resubmitted, which means the caller cleared it.
        _context.EntityAttributeValues.RemoveRange(byDefinition.Values);

        return Result.Success();
    }
}
