using AutoHub.Modules.Deals.Contracts;
using AutoHub.Modules.Deals.Domain;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Results;
using MediatR;

namespace AutoHub.Modules.Deals.Application;

/// <summary>
/// The in-process implementation of <see cref="IDealGateway"/>.
/// </summary>
/// <remarks>
/// Runs the same handlers a request would, rather than reaching into the repository, so a deal
/// opened by the auction module goes through identical validation, identical workflow start and
/// identical events as one opened by a person. A second code path for machine callers is a second
/// code path that eventually diverges, and the divergence is always discovered in production.
/// <para>
/// The actor is <see cref="WorkflowActor.System"/> because the caller is a module reacting to its own
/// committed state, not a user making a request. The user who caused it — the dealer whose bid won —
/// is already recorded on the auction side, and re-asserting them here would let a permission check
/// on a bid decide whether a settlement record may exist.
/// </para>
/// </remarks>
public sealed class DealGateway : IDealGateway
{
    private readonly ISender _sender;
    private readonly IDealRepository _deals;

    public DealGateway(ISender sender, IDealRepository deals)
    {
        _sender = sender;
        _deals = deals;
    }

    public async Task<Result<DealHandle>> OpenAsync(
        OpenDealRequest request,
        CancellationToken cancellationToken)
    {
        var opened = await _sender.Send(
            new OpenDealCommand
            {
                Actor = WorkflowActor.System,
                IntentCode = request.IntentCode,
                PricingScope = request.PricingScope,
                BaseAmountMinorUnits = request.BaseAmountMinorUnits,
                CurrencyCode = request.CurrencyCode,
                SourceType = request.SourceType,
                SourceId = request.SourceId,
                ListingId = request.ListingId,
                VehicleId = request.VehicleId,
                PeriodStart = request.PeriodStart,
                PeriodEnd = request.PeriodEnd,
                RecurrenceCode = request.RecurrenceCode,
                InstalmentNumber = request.InstalmentNumber,
                CityCode = request.CityCode,
                StateCode = request.StateCode,
                Parties = request.Parties.Select(ToSpecification).ToList(),
            },
            cancellationToken);

        if (opened.IsFailure)
        {
            return Result.Failure<DealHandle>(opened.Error);
        }

        if (!request.QuoteImmediately)
        {
            return ToHandle(opened.Value);
        }

        var quoted = await _sender.Send(
            new QuoteFeesCommand
            {
                Actor = WorkflowActor.System,
                DealId = opened.Value.Id,
            },
            cancellationToken);

        // A pricing failure is returned rather than swallowed. An auction lot whose commission could
        // not be priced must not quietly settle at zero commission; the lot stays where it is and
        // the alert fires, which is recoverable. Revenue that silently vanishes is not.
        return quoted.IsSuccess
            ? ToHandle(quoted.Value)
            : Result.Failure<DealHandle>(quoted.Error);
    }

    public async Task<Result<DealHandle>> FindBySourceAsync(
        string sourceType,
        Guid sourceId,
        CancellationToken cancellationToken)
    {
        var deal = await _deals.FindBySourceAsync(sourceType, sourceId, cancellationToken);

        return deal is null
            ? Error.NotFound("deal.not_found", "No deal has been opened for that source.")
                .With("sourceType", sourceType)
                .With("sourceId", sourceId)
            : new DealHandle(
                deal.Id,
                deal.Reference,
                deal.Payable.MinorUnits,
                deal.Payable.Currency.Code,
                deal.QuotedAt is not null);
    }

    private static DealHandle ToHandle(DealView view)
        => new(view.Id, view.Reference, view.PayableMinorUnits, view.Currency, view.AppliedRuleCode is not null);

    private static DealPartySpecification ToSpecification(DealPartyRequest request)
        => new()
        {
            RoleCode = request.RoleCode,
            PartyId = request.PartyId,
            PartyType = request.PartyType,
            IsPayer = request.IsPayer,
            IsPayee = request.IsPayee,
            RequiresAcceptance = request.RequiresAcceptance,
            SettlementShareRatio = request.SettlementShareRatio,
            PayoutAccountRef = request.PayoutAccountRef,
        };
}
