using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Pricing;

/// <summary>
/// The published spelling of <see cref="FeeBasis"/>.
/// </summary>
/// <remarks>
/// The quote is the one response a dealer reads line by line when they dispute a charge, and
/// <c>basis</c> is what tells them whether 2% was applied to the hammer price or to the fees above
/// it. Fixing the string here — rather than deriving it from the C# member name — keeps that
/// meaning stable across a rename.
/// </remarks>
public static class PricingWireNames
{
    public static string ToWireName(this FeeBasis basis) => basis switch
    {
        FeeBasis.Flat => "flat",
        FeeBasis.Percent => "percent",
        FeeBasis.Slab => "slab",
        FeeBasis.Tiered => "tiered",
        FeeBasis.PercentOfLines => "percent_of_lines",
        _ => throw new ArgumentOutOfRangeException(nameof(basis), basis, "Unmapped fee basis."),
    };

    public static Result<FeeBasis> ParseBasis(string? wireName) => wireName switch
    {
        "flat" => FeeBasis.Flat,
        "percent" => FeeBasis.Percent,
        "slab" => FeeBasis.Slab,
        "tiered" => FeeBasis.Tiered,
        "percent_of_lines" => FeeBasis.PercentOfLines,
        _ => Error.Validation("pricing.fee_basis_unknown", $"'{wireName}' is not a supported fee basis.")
            .With("basis", wireName),
    };
}
