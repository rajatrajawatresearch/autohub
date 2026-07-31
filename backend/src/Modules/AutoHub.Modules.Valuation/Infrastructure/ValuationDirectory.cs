using AutoHub.Modules.Valuation.Application.Abstractions;
using AutoHub.Modules.Valuation.Contracts;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;

namespace AutoHub.Modules.Valuation.Infrastructure;

/// <summary>
/// The valuation contract, served in-process today and over HTTP after extraction.
/// </summary>
/// <remarks>
/// Read-only, and deliberately narrow: it answers "what is the current opinion of this vehicle's
/// worth?" and nothing else. Listings uses it for the price-band guard and Finance for an indicative
/// loan-to-value base; neither needs the adjustment ledger, and exposing it would invite a second
/// module to re-derive a price from parts rather than asking for one.
/// </remarks>
internal sealed class ValuationDirectory : IValuationDirectory
{
    private readonly IValuationRepository _valuations;
    private readonly IClock _clock;

    public ValuationDirectory(IValuationRepository valuations, IClock clock)
    {
        _valuations = valuations;
        _clock = clock;
    }

    public async Task<Result<ValuationSummary>> GetLatestForVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        var valuation = await _valuations.GetLatestForVehicleAsync(vehicleId, _clock.UtcNow, cancellationToken);

        if (valuation is null)
        {
            // "Expired" and "never quoted" are the same answer to the caller: there is no number they
            // may rely on. Distinguishing them would invite a guard to fall back on a stale quote,
            // which is the failure this method's expiry filter exists to prevent.
            return Error.NotFound(
                "valuation.not_found",
                "There is no current valuation for that vehicle.")
                .With("vehicleId", vehicleId);
        }

        return new ValuationSummary
        {
            QuoteId = valuation.Id,
            VehicleId = valuation.VehicleId,
            Currency = valuation.CurrencyCode,
            PointMinorUnits = valuation.PointMinorUnits,
            LowMinorUnits = valuation.LowMinorUnits,
            HighMinorUnits = valuation.HighMinorUnits,
            Confidence = valuation.Confidence,
            ConfidenceBand = valuation.ConfidenceBandCode,
            ModelVersion = valuation.ModelVersion,
            ExpiresAt = valuation.ExpiresAt,
            Channels = [.. valuation.Channels
                .OrderBy(c => c.SortOrder)
                .Select(c => new ValuationChannelView(c.ChannelCode, c.Label, c.AmountMinorUnits, c.IsPrimary))],
        };
    }
}
