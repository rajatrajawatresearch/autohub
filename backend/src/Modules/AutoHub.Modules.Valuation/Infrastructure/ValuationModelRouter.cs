using System.Text.Json.Nodes;
using AutoHub.Modules.Valuation.Application.Abstractions;
using AutoHub.Platform.Integrations;
using AutoHub.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace AutoHub.Modules.Valuation.Infrastructure;

/// <summary>
/// Routes a valuation to a model through the integration registry, with shadow mode and fallback.
/// </summary>
/// <remarks>
/// <b>Why this is not <c>IIntegrationInvoker</c>.</b> The invoker walks a provider chain and returns
/// the first success, which is exactly right for a payment and one function short for a model
/// promotion: it has no notion of running a second provider whose answer is thrown away. Shadow mode
/// is the entire reason a valuation model can be promoted safely, so the walk is done here — reusing
/// the registry's resolution, its conditions, its canary split and its circuit breaker, and adding
/// the one behaviour a price model needs and a payment must never have.
/// <para>
/// <b>Shadow is a provider config flag</b> — <c>{"shadow": true}</c> — rather than a separate table,
/// so promoting a model is editing one JSON field and rolling back is editing it again. Nothing is
/// deployed in either direction, which is the whole claim.
/// </para>
/// </remarks>
public sealed class ValuationModelRouter : IValuationModelRouter
{
    /// <summary>
    /// The provider-config key that marks a route as observe-only.
    /// </summary>
    /// <remarks>
    /// A technical key in a config document, like a cache key: it names a behaviour the code
    /// implements. Which providers carry it is entirely data.
    /// </remarks>
    private const string ShadowFlag = "shadow";

    private readonly IIntegrationRouter _router;
    private readonly Dictionary<string, IValuationModel> _models;
    private readonly IValuationModel _default;
    private readonly ILogger<ValuationModelRouter> _logger;

    public ValuationModelRouter(
        IIntegrationRouter router,
        IEnumerable<IValuationModel> models,
        ILogger<ValuationModelRouter> logger)
    {
        _router = router;
        _models = models.ToDictionary(m => m.ModelKey, StringComparer.Ordinal);
        _logger = logger;

        // The model that answers when the registry has nothing to say. Resolved by key rather than
        // by taking the first registration, so container ordering cannot silently change which model
        // prices every car on a fresh database.
        _default = _models.TryGetValue(HedonicValuationModel.Key, out var hedonic)
            ? hedonic
            : throw new InvalidOperationException(
                $"No '{HedonicValuationModel.Key}' valuation model is registered. "
                + "The module cannot answer a request without a default.");
    }

    public async Task<Result<ValuationRun>> RunAsync(
        ValuationInputs inputs,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var chain = await _router.ResolveAsync(
            IntegrationCapabilities.ValuationPredict,
            new IntegrationContext { Facts = inputs.ToJson(), AffinityKey = idempotencyKey },
            cancellationToken);

        if (chain.IsFailure || chain.Value.Count == 0)
        {
            // A fresh database has no routes, and a marketplace whose homepage hero returns an error
            // because nobody seeded a config row is a marketplace that looks broken on day one. The
            // warning is what gets the routes seeded; the price still gets served.
            _logger.LogWarning(
                "No valuation.predict route resolved; falling back to the {Model} model",
                _default.ModelKey);

            var fallback = await _default.PredictAsync(inputs, cancellationToken);

            return fallback.IsSuccess
                ? new ValuationRun(fallback.Value, [])
                : Result.Failure<ValuationRun>(fallback.Error);
        }

        var shadowResults = new List<ValuationPrediction>();
        ValuationPrediction? served = null;
        Error? lastError = null;

        foreach (var provider in chain.Value)
        {
            if (!_models.TryGetValue(provider.ProviderCode, out var model))
            {
                // Routed to a model this build does not contain. Skipping keeps the fallback working;
                // the log is what tells an operator why their new model is receiving no traffic.
                _logger.LogError(
                    "Valuation route names provider {Provider}, for which no model is registered",
                    provider.ProviderCode);

                continue;
            }

            var isShadow = IsShadow(provider);

            // A shadow provider must never be asked before the real one — nor instead of it if the
            // real one has already answered. Skipping it once served is set keeps the served answer
            // free of the shadow model's latency.
            if (!isShadow && served is not null)
            {
                continue;
            }

            var prediction = await model.PredictAsync(inputs, cancellationToken);

            if (prediction.IsFailure)
            {
                lastError = prediction.Error;

                _logger.LogWarning(
                    "Valuation model {Model} failed ({Code}); {Disposition}",
                    provider.ProviderCode,
                    prediction.Error.Code,
                    isShadow ? "shadow run discarded" : "trying the next provider");

                await _router.RecordOutcomeAsync(
                    provider.ProviderCode, IntegrationCapabilities.ValuationPredict, false, cancellationToken);

                continue;
            }

            await _router.RecordOutcomeAsync(
                provider.ProviderCode, IntegrationCapabilities.ValuationPredict, true, cancellationToken);

            if (isShadow)
            {
                shadowResults.Add(prediction.Value);
                continue;
            }

            served = prediction.Value;
        }

        if (served is null)
        {
            return Result.Failure<ValuationRun>(lastError ?? Error.Upstream(
                "valuation.model.all_failed",
                "No valuation model could price this vehicle."));
        }

        LogDivergence(served, shadowResults);

        return new ValuationRun(served, shadowResults);
    }

    private static bool IsShadow(ResolvedProvider provider)
        => provider.Config is JsonObject config
           && config.TryGetPropertyValue(ShadowFlag, out var node)
           && node is JsonValue value
           && value.TryGetValue<bool>(out var shadow)
           && shadow;

    /// <summary>
    /// Records how far each shadow model was from the served answer.
    /// </summary>
    /// <remarks>
    /// Basis points rather than rupees, because the question a promotion decision turns on is
    /// "how far off, proportionally" and a ₹40,000 gap means something different on a hatchback and
    /// on a bus. The served prediction is persisted alongside the shadow ones by the handler, so this
    /// line is the alerting signal and the rows are the analysis.
    /// </remarks>
    private void LogDivergence(ValuationPrediction served, List<ValuationPrediction> shadows)
    {
        if (shadows.Count == 0)
        {
            return;
        }

        var anchor = Anchor(served);

        foreach (var shadow in shadows)
        {
            var candidate = Anchor(shadow);

            var basisPoints = anchor == 0
                ? 0
                : (int)Math.Round((candidate - anchor) * 10_000m / anchor, MidpointRounding.AwayFromZero);

            _logger.LogInformation(
                "Shadow model {Shadow} differed from served {Served} by {BasisPoints} bps ({Candidate} vs {Anchor})",
                shadow.ModelVersion, served.ModelVersion, basisPoints, candidate, anchor);
        }
    }

    private static long Anchor(ValuationPrediction prediction)
        => prediction.Channels.FirstOrDefault(c => c.IsPrimary)?.AmountMinorUnits ?? 0;
}
