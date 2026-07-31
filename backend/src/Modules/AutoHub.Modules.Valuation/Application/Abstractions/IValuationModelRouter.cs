using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Valuation.Application.Abstractions;

/// <summary>
/// One request's worth of model output: the answer that was served, and anything run beside it.
/// </summary>
/// <remarks>
/// <see cref="Shadow"/> is what makes promoting a model an evidence-based decision rather than a
/// leap. Shadow predictions are computed and stored and never returned, so a fortnight of them sits
/// next to the served answers for the same vehicles — which is the only comparison that means
/// anything, because a back-test on historical inputs cannot reproduce the traffic mix.
/// </remarks>
public sealed record ValuationRun(ValuationPrediction Served, IReadOnlyList<ValuationPrediction> Shadow);

/// <summary>
/// Decides which model prices a request, using the integration registry.
/// </summary>
/// <remarks>
/// The handler asks for a price and never learns which model produced it, exactly as a payment
/// handler never learns which gateway took the money. That is the point of routing through registry
/// 8: promoting a new model, canarying it at 5%, running it in shadow, and rolling it back are all
/// rows in <c>provider_routes</c> — decisions for whoever is watching the divergence, not for
/// whoever is next to cut a release.
/// </remarks>
public interface IValuationModelRouter
{
    /// <summary>
    /// Prices the inputs.
    /// </summary>
    /// <remarks>
    /// <paramref name="idempotencyKey"/> is the caller's <c>Idempotency-Key</c>, used as the routing
    /// affinity key. A retried request deterministically re-selects the model that answered the first
    /// attempt, so a seller refreshing a slow page does not get two different prices from two sides
    /// of a canary split.
    /// </remarks>
    Task<Result<ValuationRun>> RunAsync(
        ValuationInputs inputs,
        string idempotencyKey,
        CancellationToken cancellationToken);
}
