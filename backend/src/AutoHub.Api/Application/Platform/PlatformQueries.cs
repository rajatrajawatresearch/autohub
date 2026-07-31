using System.Text.Json.Nodes;
using AutoHub.Api.Security;
using AutoHub.Platform.Features;
using AutoHub.Platform.Geography;
using AutoHub.Platform.Localization;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Security;
using MediatR;

namespace AutoHub.Api.Application.Platform;

/// <summary>
/// <c>GET /platform/features</c> — §5 of the contract.
/// </summary>
/// <remarks>
/// The audience is assembled server-side from the token and the headers, never from the request.
/// A flag a client can talk its way into by posting <c>{"tier":"gold"}</c> is not a flag.
/// </remarks>
internal sealed record GetFeaturesQuery : IRequest<Result<FeatureSnapshot>>;

internal sealed class GetFeaturesHandler : IRequestHandler<GetFeaturesQuery, Result<FeatureSnapshot>>
{
    private readonly IFeatureRegistry _registry;
    private readonly ICurrentUser _currentUser;
    private readonly IRequestContext _request;

    public GetFeaturesHandler(
        IFeatureRegistry registry,
        ICurrentUser currentUser,
        IRequestContext request)
    {
        _registry = registry;
        _currentUser = currentUser;
        _request = request;
    }

    public async Task<Result<FeatureSnapshot>> Handle(
        GetFeaturesQuery query,
        CancellationToken cancellationToken)
    {
        var attributes = new JsonObject
        {
            // The client string lets a flag target an app version, which is how a forced upgrade is
            // rolled out without a release: the rule targets "web/0.1.x" and the shell reads a
            // boolean it cannot argue with.
            ["client"] = _request.Client,
            ["authenticated"] = _currentUser.UserId is not null,
        };

        var snapshot = await _registry.EvaluateAsync(
            new FeatureAudience
            {
                UserId = _currentUser.UserId,
                CityCode = _request.ActiveCityCode,
                Attributes = attributes,
            },
            cancellationToken);

        return snapshot;
    }
}

/// <summary><c>GET /platform/i18n/{locale}</c> — §6 of the contract.</summary>
internal sealed record GetI18nBundleQuery(string Locale, string Namespace) : IRequest<Result<I18nBundle>>;

internal sealed class GetI18nBundleHandler : IRequestHandler<GetI18nBundleQuery, Result<I18nBundle>>
{
    private readonly II18nRegistry _registry;

    public GetI18nBundleHandler(II18nRegistry registry) => _registry = registry;

    public Task<Result<I18nBundle>> Handle(GetI18nBundleQuery query, CancellationToken cancellationToken)
        => _registry.GetBundleAsync(query.Locale, query.Namespace, cancellationToken);
}

/// <summary>
/// <c>GET /platform/geo/cities</c> — §7 of the contract.
/// </summary>
/// <remarks>
/// <see cref="Module"/> narrows the list to cities where a module actually runs. It is the
/// difference between "where can I buy a car?" and "where can I bid at an auction?", and without it
/// the auction city picker lists cities with no yard.
/// </remarks>
internal sealed record GetCitiesQuery(string? Module) : IRequest<Result<GeographyList<CityView>>>;

internal sealed class GetCitiesHandler : IRequestHandler<GetCitiesQuery, Result<GeographyList<CityView>>>
{
    private readonly IGeographyRegistry _registry;

    public GetCitiesHandler(IGeographyRegistry registry) => _registry = registry;

    public Task<Result<GeographyList<CityView>>> Handle(
        GetCitiesQuery query,
        CancellationToken cancellationToken)
        => _registry.GetServiceableCitiesAsync(query.Module, cancellationToken);
}
