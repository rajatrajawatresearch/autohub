using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Catalog.Contracts;

/// <summary>One taxonomy node, reduced to the three things a caller ever needs from it.</summary>
public sealed record TaxonomyRef(Guid Id, string Code, string Name);

/// <summary>
/// A vehicle's position in the taxonomy, resolved from codes to ids.
/// </summary>
/// <remarks>
/// Everything below the category is nullable and that is not laziness. A plate typed into the
/// homepage hero produces a vehicle before anybody knows the trim, and the sell flow fills the rest
/// in over the following two minutes. A classification that demanded a variant would refuse the one
/// piece of information the seller actually had.
/// </remarks>
public sealed record VehicleClassification(
    TaxonomyRef Category,
    TaxonomyRef? Make,
    TaxonomyRef? Model,
    TaxonomyRef? Variant);

/// <summary>Taxonomy ids for whichever codes a search supplied. All three are optional.</summary>
public sealed record TaxonomyFilters(Guid? CategoryId, Guid? MakeId, Guid? ModelId)
{
    public static TaxonomyFilters None { get; } = new(null, null, null);
}

/// <summary>
/// The taxonomy read side, as the rest of the platform's modules consume it.
/// </summary>
/// <remarks>
/// The north star gives this module "vehicles + taxonomy read side", and this interface is the
/// second half of that. It exists because the taxonomy registry publishes <b>codes</b> — that is
/// what URLs, facets and form fields are built from — while every foreign key in the database is an
/// <b>id</b>, and something has to bridge the two exactly once.
/// <para>
/// Doing it here rather than in each module means one cache, one set of not-found error codes, and
/// one place where "the make code changed" is handled. Doing it in each module means three
/// implementations that drift, and the drift shows up as a listing whose category id points at a
/// category its code no longer names.
/// </para>
/// <para>
/// Reads only. Editing the taxonomy is the platform registry's admin path, which has its own
/// permission, its own audit and its own cache invalidation; a module that could write here could
/// change what every other module's search facets look like.
/// </para>
/// </remarks>
public interface IVehicleTaxonomy
{
    Task<Result<TaxonomyRef>> ResolveCategoryAsync(string categoryCode, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves a full code set in one pass, failing on the first code that does not exist.
    /// </summary>
    /// <remarks>
    /// Also checks that the model belongs to the make and the variant to the model. Without that a
    /// bulk upload can create a "Maruti Suzuki Nexon", which no query would ever flag and which
    /// quietly poisons every model-level price statistic it lands in.
    /// </remarks>
    Task<Result<VehicleClassification>> ResolveClassificationAsync(
        string categoryCode,
        string? makeCode,
        string? modelCode,
        string? variantCode,
        CancellationToken cancellationToken);

    /// <summary>
    /// Resolves whichever of the three codes a search actually supplied.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="ResolveClassificationAsync"/> because a filter set is optional by
    /// nature and a classification is not — a vehicle without a category cannot exist, and a search
    /// without one is the normal case. Folding the two into one nullable-everything method would
    /// have meant the creation path could no longer rely on the compiler for the one field it must
    /// have.
    /// <para>
    /// A code that does not resolve is an error rather than an ignored filter. Silently dropping it
    /// returns every vehicle on the platform to a user who asked for one make, which reads as a
    /// broken filter and is worse than a message saying the make is unknown.
    /// </para>
    /// </remarks>
    Task<Result<TaxonomyFilters>> ResolveFiltersAsync(
        string? categoryCode,
        string? makeCode,
        string? modelCode,
        CancellationToken cancellationToken);

    /// <summary>
    /// The codes behind a batch of ids — the reverse direction, for rendering.
    /// </summary>
    /// <remarks>
    /// Batched for the same reason every other read here is: a search results page holds twenty-four
    /// category ids and needs twenty-four codes to build twenty-four links.
    /// </remarks>
    Task<Result<IReadOnlyDictionary<Guid, TaxonomyRef>>> DescribeAsync(
        IReadOnlyCollection<Guid> taxonomyIds,
        CancellationToken cancellationToken);
}
