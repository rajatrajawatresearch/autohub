using AutoHub.Modules.Listings.Domain.Listings;
using AutoHub.Platform.Taxonomy;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Listings.Application.Abstractions;

/// <summary>
/// Writes validated attribute values into the platform's shared EAV table.
/// </summary>
/// <remarks>
/// Declared per module rather than shared, and that is the isolation rule being taken literally: a
/// single interface in a common place would be one more thing every module depends on and one more
/// reason extraction is not the build-file change it is supposed to be. One EF class in the
/// persistence layer implements every module's copy, so the duplication costs an interface
/// declaration and nothing at runtime.
/// <para>
/// A full replace, not a merge — a merge cannot express a seller unticking a box.
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

/// <summary>
/// Turns a listing into a quality score a seller can act on.
/// </summary>
/// <remarks>
/// An interface rather than a method on the aggregate because scoring needs two registries — the
/// taxonomy's attribute list for the category and the form registry's photo-set configuration — and
/// an aggregate that reached out to a registry would no longer be testable without one.
/// </remarks>
public interface IListingQualityScorer
{
    Task<ListingQuality> ScoreAsync(Listing listing, CancellationToken cancellationToken);
}
