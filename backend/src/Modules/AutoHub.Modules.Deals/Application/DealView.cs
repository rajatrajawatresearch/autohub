using AutoHub.Modules.Deals.Domain;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Deals.Application;

/// <summary>
/// A deal as the API publishes it, including where it sits in its lifecycle.
/// </summary>
/// <remarks>
/// The workflow snapshot travels with the deal rather than behind a second call, because every
/// screen that renders a deal also renders its actions, and two requests to paint one card is a
/// spinner the user watches twice. Amounts are minor units and the server never formats them —
/// "₹8.47L" is a decision that depends on the viewer's locale and the surface.
/// </remarks>
public sealed record DealView
{
    public required Guid Id { get; init; }

    public required string Reference { get; init; }

    public required string IntentCode { get; init; }

    public required string PricingScope { get; init; }

    public string? SourceType { get; init; }

    public Guid? SourceId { get; init; }

    public Guid? ListingId { get; init; }

    public Guid? VehicleId { get; init; }

    public required string Currency { get; init; }

    public required long BaseAmountMinorUnits { get; init; }

    public required long FeeTotalMinorUnits { get; init; }

    public required long PayableMinorUnits { get; init; }

    public DateTimeOffset? PeriodStart { get; init; }

    public DateTimeOffset? PeriodEnd { get; init; }

    public string? RecurrenceCode { get; init; }

    public int? InstalmentNumber { get; init; }

    public string? AppliedRuleCode { get; init; }

    public string? PricingExplanation { get; init; }

    public required IReadOnlyList<DealPartyView> Parties { get; init; }

    public required IReadOnlyList<DealLineView> Lines { get; init; }

    public DateTimeOffset? AcceptedAt { get; init; }

    public DateTimeOffset? SettledAt { get; init; }

    public string? SettlementReference { get; init; }

    public DateTimeOffset? CancelledAt { get; init; }

    /// <summary>Null only when the workflow instance could not be read — never a fabricated state.</summary>
    public WorkflowSnapshot? Workflow { get; init; }

    public static DealView From(Deal deal, WorkflowSnapshot? workflow) => new()
    {
        Id = deal.Id,
        Reference = deal.Reference,
        IntentCode = deal.IntentCode,
        PricingScope = deal.PricingScope,
        SourceType = deal.SourceType,
        SourceId = deal.SourceId,
        ListingId = deal.ListingId,
        VehicleId = deal.VehicleId,
        Currency = deal.BaseAmount.Currency.Code,
        BaseAmountMinorUnits = deal.BaseAmount.MinorUnits,
        FeeTotalMinorUnits = deal.FeeTotal.MinorUnits,
        PayableMinorUnits = deal.Payable.MinorUnits,
        PeriodStart = deal.PeriodStart,
        PeriodEnd = deal.PeriodEnd,
        RecurrenceCode = deal.RecurrenceCode,
        InstalmentNumber = deal.InstalmentNumber,
        AppliedRuleCode = deal.AppliedRuleCode,
        PricingExplanation = deal.PricingExplanation,
        Parties = deal.Parties
            .Select(p => new DealPartyView(
                p.Id, p.RoleCode, p.PartyId, p.PartyType, p.IsPayer, p.IsPayee,
                p.RequiresAcceptance, p.SettlementShareRatio, p.AcceptedAt))
            .ToList(),
        Lines = deal.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l => new DealLineView(
                l.Code, l.Label, l.Amount.MinorUnits, l.Basis?.ToString(), l.Rate,
                l.RuleCode, l.IsPrincipal, l.AppliesTo()))
            .ToList(),
        AcceptedAt = deal.AcceptedAt,
        SettledAt = deal.SettledAt,
        SettlementReference = deal.SettlementReference,
        CancelledAt = deal.CancelledAt,
        Workflow = workflow,
    };
}

public sealed record DealPartyView(
    Guid Id,
    string RoleCode,
    Guid? PartyId,
    string? PartyType,
    bool IsPayer,
    bool IsPayee,
    bool RequiresAcceptance,
    int SettlementShareRatio,
    DateTimeOffset? AcceptedAt);

public sealed record DealLineView(
    string Code,
    string Label,
    long AmountMinorUnits,
    string? Basis,
    decimal? Rate,
    string? RuleCode,
    bool IsPrincipal,
    IReadOnlyList<string> AppliesTo);

/// <summary>The failures every deal handler can produce, named once so their codes never drift.</summary>
internal static class DealErrors
{
    public static Error NotFound(Guid dealId)
        => Error.NotFound("deal.not_found", "No deal with that id.").With("dealId", dealId);

    public static Error PermissionDenied(string permission)
        => Error.Forbidden("deal.permission_denied", "You do not have permission to do this.")
            .With("requiredPermission", permission);
}
