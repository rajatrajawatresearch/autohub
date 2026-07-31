using AutoHub.Modules.Deals.Domain;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AutoHub.Modules.Deals.Application;

/// <summary>
/// Opens the settlement record for one commercial moment.
/// </summary>
/// <remarks>
/// Called by every intent module and never by a customer directly: the listings module opens one
/// when an offer is accepted, the auction module when a lot is knocked down, rental when a booking
/// is confirmed. That is why <see cref="SourceType"/> and <see cref="SourceId"/> are effectively
/// required in practice — they are what makes the call idempotent.
/// </remarks>
public sealed record OpenDealCommand : IRequest<Result<DealView>>
{
    public required WorkflowActor Actor { get; init; }

    public required string IntentCode { get; init; }

    public required string PricingScope { get; init; }

    public required long BaseAmountMinorUnits { get; init; }

    public string CurrencyCode { get; init; } = "INR";

    public string? SourceType { get; init; }

    public Guid? SourceId { get; init; }

    public Guid? ListingId { get; init; }

    public Guid? VehicleId { get; init; }

    public Guid? ParentDealId { get; init; }

    public DateTimeOffset? PeriodStart { get; init; }

    public DateTimeOffset? PeriodEnd { get; init; }

    public string? RecurrenceCode { get; init; }

    public int? InstalmentNumber { get; init; }

    public string? CityCode { get; init; }

    public string? StateCode { get; init; }

    public IReadOnlyList<DealPartySpecification> Parties { get; init; } = [];
}

public sealed class OpenDealHandler : IRequestHandler<OpenDealCommand, Result<DealView>>
{
    private readonly IDealRepository _deals;
    private readonly IWorkflowEngine _workflow;
    private readonly IClock _clock;
    private readonly ILogger<OpenDealHandler> _logger;

    public OpenDealHandler(
        IDealRepository deals,
        IWorkflowEngine workflow,
        IClock clock,
        ILogger<OpenDealHandler> logger)
    {
        _deals = deals;
        _workflow = workflow;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<DealView>> Handle(OpenDealCommand request, CancellationToken cancellationToken)
    {
        if (!request.Actor.Holds(DealPermissions.Open))
        {
            return DealErrors.PermissionDenied(DealPermissions.Open);
        }

        // Idempotency before creation, not after. A retried lot-closed effect that got as far as the
        // insert last time must find the deal it made, or the seller is paid twice for one vehicle.
        if (request.SourceType is not null && request.SourceId is { } sourceId)
        {
            var existing = await _deals.FindBySourceAsync(request.SourceType, sourceId, cancellationToken);
            if (existing is not null)
            {
                return DealView.From(existing, await ReadWorkflowAsync(existing, request.Actor, cancellationToken));
            }
        }

        Currency currency;
        try
        {
            currency = Currency.FromCode(request.CurrencyCode);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Error.Validation("deal.currency_unsupported", "That currency is not supported.")
                .With("currency", request.CurrencyCode);
        }

        var now = _clock.UtcNow;

        var opened = Deal.Open(
            new DealSpecification
            {
                IntentCode = request.IntentCode,
                PricingScope = request.PricingScope,
                BaseAmount = Money.FromMinorUnits(request.BaseAmountMinorUnits, currency),
                SourceType = request.SourceType,
                SourceId = request.SourceId,
                ListingId = request.ListingId,
                VehicleId = request.VehicleId,
                ParentDealId = request.ParentDealId,
                PeriodStart = request.PeriodStart,
                PeriodEnd = request.PeriodEnd,
                RecurrenceCode = request.RecurrenceCode,
                InstalmentNumber = request.InstalmentNumber,
                CityCode = request.CityCode,
                StateCode = request.StateCode,
            },
            now);

        if (opened.IsFailure)
        {
            return Result.Failure<DealView>(opened.Error);
        }

        var deal = opened.Value;

        foreach (var party in request.Parties)
        {
            var attached = deal.AttachParty(party, now);
            if (attached.IsFailure)
            {
                return Result.Failure<DealView>(attached.Error);
            }
        }

        await _deals.AddAsync(deal, cancellationToken);
        await _deals.SaveChangesAsync(cancellationToken);

        // The workflow instance is started after the aggregate is committed, and deliberately so.
        // Starting it first would leave an orphaned lifecycle pointing at a deal that never existed
        // if the insert failed, and StartAsync is idempotent, so a retry converges.
        var snapshot = await _workflow.StartAsync(
            DealWorkflow.Key,
            deal.Id,
            deal.ToPricingContext(),
            request.Actor.UserId,
            cancellationToken);

        if (snapshot.IsFailure)
        {
            _logger.LogError(
                "Deal {DealId} was created but its workflow could not start: {Code}",
                deal.Id, snapshot.Error.Code);

            return Result.Failure<DealView>(snapshot.Error);
        }

        return DealView.From(deal, snapshot.Value);
    }

    private async Task<WorkflowSnapshot?> ReadWorkflowAsync(
        Deal deal, WorkflowActor actor, CancellationToken cancellationToken)
    {
        var snapshot = await _workflow.GetSnapshotAsync(
            DealWorkflow.Key, deal.Id, deal.ToPricingContext(), actor, cancellationToken);

        return snapshot.IsSuccess ? snapshot.Value : null;
    }
}
