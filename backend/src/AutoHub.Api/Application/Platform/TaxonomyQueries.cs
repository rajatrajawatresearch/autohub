using AutoHub.Platform.Taxonomy;
using AutoHub.SharedKernel.Results;
using MediatR;

namespace AutoHub.Api.Application.Platform;

/// <summary>
/// The five taxonomy reads of §1 of the contract.
/// </summary>
/// <remarks>
/// Every handler is a straight pass-through to <see cref="ITaxonomyRegistry"/>, and that is the
/// point rather than a shortcoming: the registry already returns the contract's shapes — its view
/// records were written against them — so an intermediate DTO here would be a second place for the
/// wire format to live and a second place for it to drift.
/// <para>
/// They still go through MediatR because the pipeline is where cross-cutting behaviour lives.
/// A taxonomy read that bypassed it would be the one endpoint with no validation and no future
/// caching behaviour, discovered the day one is added.
/// </para>
/// </remarks>
internal sealed record GetTaxonomyTreeQuery : IRequest<Result<TaxonomyTree>>;

internal sealed class GetTaxonomyTreeHandler : IRequestHandler<GetTaxonomyTreeQuery, Result<TaxonomyTree>>
{
    private readonly ITaxonomyRegistry _registry;

    public GetTaxonomyTreeHandler(ITaxonomyRegistry registry) => _registry = registry;

    public Task<Result<TaxonomyTree>> Handle(GetTaxonomyTreeQuery query, CancellationToken cancellationToken)
        => _registry.GetTreeAsync(cancellationToken);
}

internal sealed record GetCategoryAttributesQuery(string Category) : IRequest<Result<CategoryAttributes>>;

internal sealed class GetCategoryAttributesHandler
    : IRequestHandler<GetCategoryAttributesQuery, Result<CategoryAttributes>>
{
    private readonly ITaxonomyRegistry _registry;

    public GetCategoryAttributesHandler(ITaxonomyRegistry registry) => _registry = registry;

    public Task<Result<CategoryAttributes>> Handle(
        GetCategoryAttributesQuery query,
        CancellationToken cancellationToken)
        => _registry.GetAttributesForCategoryAsync(query.Category, cancellationToken);
}

internal sealed record GetMakesQuery(string? Category) : IRequest<Result<TaxonomyList<MakeView>>>;

internal sealed class GetMakesHandler : IRequestHandler<GetMakesQuery, Result<TaxonomyList<MakeView>>>
{
    private readonly ITaxonomyRegistry _registry;

    public GetMakesHandler(ITaxonomyRegistry registry) => _registry = registry;

    public Task<Result<TaxonomyList<MakeView>>> Handle(
        GetMakesQuery query,
        CancellationToken cancellationToken)
        => _registry.GetMakesAsync(query.Category, cancellationToken);
}

internal sealed record GetModelsQuery(string Make) : IRequest<Result<TaxonomyList<ModelView>>>;

internal sealed class GetModelsHandler : IRequestHandler<GetModelsQuery, Result<TaxonomyList<ModelView>>>
{
    private readonly ITaxonomyRegistry _registry;

    public GetModelsHandler(ITaxonomyRegistry registry) => _registry = registry;

    public Task<Result<TaxonomyList<ModelView>>> Handle(
        GetModelsQuery query,
        CancellationToken cancellationToken)
        => _registry.GetModelsAsync(query.Make, cancellationToken);
}

internal sealed record GetVariantsQuery(string Model, int? Year) : IRequest<Result<TaxonomyList<VariantView>>>;

internal sealed class GetVariantsHandler
    : IRequestHandler<GetVariantsQuery, Result<TaxonomyList<VariantView>>>
{
    private readonly ITaxonomyRegistry _registry;

    public GetVariantsHandler(ITaxonomyRegistry registry) => _registry = registry;

    public Task<Result<TaxonomyList<VariantView>>> Handle(
        GetVariantsQuery query,
        CancellationToken cancellationToken)
        => _registry.GetVariantsAsync(query.Model, query.Year, cancellationToken);
}
