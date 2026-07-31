using System.Text.Json.Nodes;
using AutoHub.Modules.Auction.Domain;
using AutoHub.Platform.Pricing;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Auction.Application;

/// <summary>
/// How much a bid must clear the standing price by.
/// </summary>
/// <remarks>
/// <b>A bid increment ladder is a slab table, so it is the pricing registry's job.</b> "₹1,000 up to
/// ₹1 lakh, ₹2,500 to ₹5 lakh, ₹5,000 above" is precisely a <c>FeeBasis.Slab</c> component keyed on
/// the base amount, and the registry already versions it, explains it, previews it and scopes it by
/// city, category and dealer tier. Reimplementing the same table as a constant here would give the
/// auction team a ladder nobody else can change and an auction that jumps differently in Pune for
/// reasons no operator can see.
/// <para>
/// The scope comes from the lot's own settings, so a wholesale auction and a retail one can ladder
/// differently without either being special-cased.
/// </para>
/// </remarks>
public interface IBidIncrementPolicy
{
    Task<Result<Money>> MinimumIncrementAsync(
        AuctionLot lot,
        Money standingAmount,
        CancellationToken cancellationToken);
}

public sealed class BidIncrementPolicy : IBidIncrementPolicy
{
    private readonly IPricingEngine _pricing;

    public BidIncrementPolicy(IPricingEngine pricing) => _pricing = pricing;

    /// <summary>The fee component code the increment ladder is authored under.</summary>
    /// <remarks>
    /// A technical identifier — the name of the number this code reads out of the quote, the same way
    /// a workflow effect code names the handler it invokes. The ladder itself, its bands, its values
    /// and which lots it applies to are entirely rows.
    /// </remarks>
    public const string ComponentCode = "bid_increment";

    public async Task<Result<Money>> MinimumIncrementAsync(
        AuctionLot lot,
        Money standingAmount,
        CancellationToken cancellationToken)
    {
        var quote = await _pricing.QuoteAsync(
            new PriceQuoteRequest
            {
                Scope = lot.Settings.BidIncrementScope,
                BaseAmount = standingAmount,
                Context = BuildContext(lot),
            },
            cancellationToken);

        if (quote.IsFailure)
        {
            return Result.Failure<Money>(quote.Error);
        }

        var line = quote.Value.Lines.FirstOrDefault(l =>
            string.Equals(l.Code, ComponentCode, StringComparison.Ordinal));

        if (line is null)
        {
            // Refusing beats defaulting. A default increment is a number nobody authored being
            // applied to a live auction, and the first anyone hears of it is a dealer asking why the
            // step changed.
            return Error.Unexpected(
                "auction.bid_increment_unconfigured",
                "No bid increment is configured for this auction's pricing scope.")
                .With("scope", lot.Settings.BidIncrementScope);
        }

        if (!line.Amount.IsPositive)
        {
            return Error.Unexpected(
                "auction.bid_increment_not_positive",
                "The configured bid increment is not positive, so bids could not rise.")
                .With("scope", lot.Settings.BidIncrementScope);
        }

        return line.Amount;
    }

    private static JsonObject BuildContext(AuctionLot lot) => new()
    {
        ["auction"] = new JsonObject
        {
            ["lotId"] = lot.Id.ToString("D", System.Globalization.CultureInfo.InvariantCulture),
            ["bidCount"] = lot.BidCount,
        },
        ["location"] = new JsonObject
        {
            ["city"] = lot.CityCode,
            ["state"] = lot.StateCode,
        },
    };
}
