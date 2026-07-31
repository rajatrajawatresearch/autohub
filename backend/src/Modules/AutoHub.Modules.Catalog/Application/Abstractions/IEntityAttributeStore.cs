using AutoHub.Platform.Taxonomy;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Catalog.Application.Abstractions;

/// <summary>
/// Writes validated attribute values into the platform's shared EAV table.
/// </summary>
/// <remarks>
/// The taxonomy registry owns the <i>shape</i> of an attribute and validates submitted values;
/// storing them is a persistence concern, and this is the seam between the two. Values arrive keyed
/// by attribute <b>code</b> rather than by definition id because that is what
/// <c>ITaxonomyRegistry.ValidateAttributesAsync</c> produces, and translating codes to ids in the
/// handler would mean every handler carrying a second registry read it could get wrong.
/// <para>
/// <see cref="ReplaceAsync"/> is a full replace of the entity's values rather than a merge. A merge
/// cannot express "the seller cleared this field", and a sell form that cannot clear a field is a
/// listing that keeps claiming a sunroof it does not have.
/// </para>
/// </remarks>
public interface IEntityAttributeStore
{
    Task<Result> ReplaceAsync(
        string entityType,
        Guid entityId,
        IReadOnlyDictionary<string, TypedAttributeValue> values,
        CancellationToken cancellationToken);
}
