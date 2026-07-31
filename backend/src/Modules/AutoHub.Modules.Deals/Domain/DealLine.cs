using AutoHub.Platform.Pricing;
using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Money;

namespace AutoHub.Modules.Deals.Domain;

/// <summary>
/// One priced component of a deal, frozen at the moment it was quoted.
/// </summary>
/// <remarks>
/// <b>Why the lines are stored and not recomputed.</b> A quote is a function of rules that change.
/// Recomputing a two-year-old deal's commission against today's rule set produces a number that
/// disagrees with the invoice the dealer holds, the GST return already filed, and the ledger. So the
/// engine's output is written down — amount, rate, basis, and which rule produced it — and the deal
/// is answerable forever without the rules that made it still existing.
/// <para>
/// <b>Why there is no "line kind" enum.</b> The obvious next field is a discriminator saying whether
/// a line is a fee, a tax, a refundable deposit or a payout, so settlement knows which ledger
/// accounts to touch. That discriminator would be a hardcoded option list, and worse, a second place
/// where the meaning of <c>security_deposit</c> is decided. The mapping from a line's
/// <see cref="Code"/> to its pair of ledger accounts is data in the payments module, so a new fee
/// type is a pricing rule plus a posting rule — two rows, no deploy.
/// </para>
/// </remarks>
public sealed class DealLine : Entity, IAuditable
{
    private DealLine(Guid id, Guid dealId, string code, string label, Money amount)
        : base(id)
    {
        DealId = dealId;
        Code = code;
        Label = label;
        Amount = amount;
    }

    private DealLine() { }

    /// <summary>The principal's line code. The one line the pricing registry does not emit.</summary>
    /// <remarks>
    /// A constant because the ledger's posting rules key off it and the deal owns the number. It is a
    /// technical identifier, not a business one: no operator will ever want the vehicle's price to
    /// stop being the principal.
    /// </remarks>
    public const string PrincipalCode = "principal";

    public Guid DealId { get; private set; }

    /// <summary>
    /// The pricing registry's <c>FeeComponent.Code</c>, e.g. <c>auction_commission</c>, <c>gst</c>.
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Default English label from the rule. The rendered label comes from the i18n registry.</summary>
    public string Label { get; private set; } = string.Empty;

    public Money Amount { get; private set; }

    /// <summary>How the amount was arrived at. Null on the principal, which was not computed at all.</summary>
    public FeeBasis? Basis { get; private set; }

    /// <summary>Fraction, not percent — 2% is <c>0.02</c>. Kept so the invoice can show the working.</summary>
    public decimal? Rate { get; private set; }

    /// <summary>Which pricing rule emitted this line, for the audit trail and for support.</summary>
    public string? RuleCode { get; private set; }

    /// <summary>For a tax line, the line codes it was charged on. GST applies to commission, not to price.</summary>
    public string? AppliesToCsv { get; private set; }

    public bool IsPrincipal { get; private set; }

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    internal static DealLine Principal(Guid dealId, Money amount)
        => new(UuidV7.New(), dealId, PrincipalCode, "Principal", amount) { IsPrincipal = true };

    internal static DealLine FromQuote(Guid dealId, PriceLine line, string? ruleCode, int sortOrder)
        => new(UuidV7.New(), dealId, line.Code, line.Label, line.Amount)
        {
            Basis = line.Basis,
            Rate = line.Rate,
            RuleCode = ruleCode,
            AppliesToCsv = line.AppliesTo.Count == 0 ? null : string.Join(',', line.AppliesTo),
            SortOrder = sortOrder,
        };

    public IReadOnlyList<string> AppliesTo()
        => string.IsNullOrWhiteSpace(AppliesToCsv)
            ? []
            : AppliesToCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
