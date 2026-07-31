using AutoHub.Modules.Valuation.Application.Abstractions;
using AutoHub.Platform.Integrations;
using AutoHub.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace AutoHub.Modules.Valuation.Infrastructure;

/// <summary>
/// The place the trained model plugs in. Deliberately unimplemented, deliberately present.
/// </summary>
/// <remarks>
/// <b>Why this exists before there is a model.</b> A price model needs realised sales — eighteen
/// months of them, at volume, per segment — and on day one there are none. The question is not
/// whether ML arrives but how it arrives, and there are two bad answers and one good one.
/// <list type="bullet">
/// <item>Wait, and ship the rule-based model with its coefficients hardwired into a service.
/// Introducing ML then means editing every caller under time pressure, with revenue depending on the
/// answer.</item>
/// <item>Build the ML path now against a mock. The thing that runs in production would then be a
/// code path that has never served a request.</item>
/// <item>Define the abstraction, implement the model that works today behind it, and leave this
/// marked seam. Promotion becomes a routing change with a shadow period and a rollback.</item>
/// </list>
/// <para>
/// <b>Routing, not branching.</b> Which model serves a request is a <c>valuation.predict</c> route in
/// the integration registry, so the sequence is: deploy the adapter with no route, add a route at
/// 5% traffic in shadow, read the divergence for a fortnight, move it to primary, keep the hedonic
/// model as the fallback. Every step is a row, and every step has a way back that does not involve a
/// deployment at 2am.
/// </para>
/// <para>
/// It fails as <b>retryable</b> rather than indeterminate: a valuation has no side effect at the
/// vendor, so moving to the next provider in the chain — which in practice is the hedonic model — is
/// safe and is exactly what should happen. That is the difference between this and a payment, and
/// it is why the distinction lives in the error rather than in the invoker.
/// </para>
/// </remarks>
public sealed class MlValuationModel : IValuationModel
{
    /// <summary>The provider code this adapter answers to under <c>valuation.predict</c>.</summary>
    public const string Key = "ml-ensemble";

    private readonly ILogger<MlValuationModel> _logger;

    public MlValuationModel(ILogger<MlValuationModel> logger) => _logger = logger;

    public string ModelKey => Key;

    public Task<Result<ValuationPrediction>> PredictAsync(
        ValuationInputs inputs,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "The ML valuation model is a stub; routing sent a {Category} valuation to it",
            inputs.CategoryCode);

        return Task.FromResult(Result.Failure<ValuationPrediction>(
            IntegrationErrors.Retryable(
                "valuation.model.not_implemented",
                "The machine-learning valuation model is not available in this build.")));
    }
}
