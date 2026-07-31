using AutoHub.SharedKernel.Results;

namespace AutoHub.SharedKernel.Money;

/// <summary>
/// An exact amount of money, held in minor units (paise for INR).
/// </summary>
/// <remarks>
/// Never <c>decimal</c> with rupees, never <c>double</c>. A 2% auction commission on ₹8,47,350
/// is ₹16,947.00 exactly; the moment a fractional rupee is representable, two systems disagree
/// about a paisa and the month-end reconciliation stops balancing.
/// <para>
/// Rounding is therefore an explicit, named decision — see <see cref="Allocate"/> and
/// <see cref="MultiplyByRate"/> — and never an accident of the type system.
/// </para>
/// <para>
/// This type deliberately has no formatting. Rendering "₹8.47L" is a presentation concern that
/// depends on the viewer's locale and the surface; the domain does not know or care.
/// </para>
/// </remarks>
public readonly record struct Money : IComparable<Money>
{
    private Money(long minorUnits, Currency currency)
    {
        MinorUnits = minorUnits;
        Currency = currency;
    }

    /// <summary>Paise for INR. Signed — a refund and a credit note are negative.</summary>
    public long MinorUnits { get; }

    public Currency Currency { get; }

    public bool IsZero => MinorUnits == 0;

    public bool IsPositive => MinorUnits > 0;

    public bool IsNegative => MinorUnits < 0;

    public static Money Zero(Currency currency) => new(0, currency);

    public static Money ZeroInr => new(0, Currency.Inr);

    public static Money FromMinorUnits(long minorUnits, Currency currency) => new(minorUnits, currency);

    public static Money FromRupees(decimal rupees) => FromMajorUnits(rupees, Currency.Inr);

    public static Money FromMajorUnits(decimal majorUnits, Currency currency)
    {
        var scaled = majorUnits * currency.MinorUnitsPerMajor;
        var rounded = Math.Round(scaled, 0, MidpointRounding.ToEven);

        if (rounded != scaled)
        {
            throw new ArgumentException(
                $"{majorUnits} cannot be represented exactly in {currency.Code}. " +
                "Use MultiplyByRate or Allocate so the rounding decision is explicit.",
                nameof(majorUnits));
        }

        return new Money((long)rounded, currency);
    }

    public static Result<Money> TryAdd(Money left, Money right)
        => left.Currency == right.Currency
            ? Result.Success(new Money(left.MinorUnits + right.MinorUnits, left.Currency))
            : CurrencyMismatch(left, right);

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.MinorUnits + right.MinorUnits, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.MinorUnits - right.MinorUnits, left.Currency);
    }

    public static Money operator -(Money value) => new(-value.MinorUnits, value.Currency);

    public static Money operator *(Money value, long factor) => new(value.MinorUnits * factor, value.Currency);

    /// <summary>
    /// Applies a rate (a 0.02m commission, an 18% GST) with an explicit rounding mode.
    /// </summary>
    /// <remarks>
    /// Banker's rounding is the default because it does not systematically favour one party;
    /// over a million transactions, always-round-up quietly transfers real money to the platform,
    /// which auditors notice and regulators do not enjoy.
    /// </remarks>
    public Money MultiplyByRate(decimal rate, MidpointRounding rounding = MidpointRounding.ToEven)
        => new((long)Math.Round(MinorUnits * rate, 0, rounding), Currency);

    /// <summary>
    /// Splits an amount into parts by ratio, distributing the remainder one minor unit at a time so
    /// the parts always sum back to the original.
    /// </summary>
    /// <remarks>
    /// This is the function that stops ₹100.00 split three ways from becoming ₹99.99. It matters
    /// wherever one payment is divided between seller, platform commission, and GST.
    /// </remarks>
    public IReadOnlyList<Money> Allocate(IReadOnlyList<int> ratios)
    {
        ArgumentOutOfRangeException.ThrowIfZero(ratios.Count);

        var total = 0L;
        foreach (var ratio in ratios)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(ratio);
            total += ratio;
        }

        ArgumentOutOfRangeException.ThrowIfZero(total);

        var results = new Money[ratios.Count];
        var remainder = MinorUnits;

        for (var i = 0; i < ratios.Count; i++)
        {
            var share = MinorUnits * ratios[i] / total;
            results[i] = new Money(share, Currency);
            remainder -= share;
        }

        // Hand the leftover minor units out in order, largest ratio first would bias the winner;
        // sequential distribution is the convention accountants expect and can re-derive.
        for (var i = 0; remainder != 0; i = (i + 1) % ratios.Count)
        {
            var step = remainder > 0 ? 1 : -1;
            results[i] = new Money(results[i].MinorUnits + step, Currency);
            remainder -= step;
        }

        return results;
    }

    public Money Clamp(Money? floor, Money? cap)
    {
        var value = this;

        if (floor is { } f)
        {
            EnsureSameCurrency(value, f);
            if (value.MinorUnits < f.MinorUnits)
            {
                value = f;
            }
        }

        if (cap is { } c)
        {
            EnsureSameCurrency(value, c);
            if (value.MinorUnits > c.MinorUnits)
            {
                value = c;
            }
        }

        return value;
    }

    public int CompareTo(Money other)
    {
        EnsureSameCurrency(this, other);
        return MinorUnits.CompareTo(other.MinorUnits);
    }

    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;

    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;

    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;

    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

    public override string ToString() => $"{Currency.Code} {MinorUnits}";

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new InvalidOperationException(
                $"Cannot combine {left.Currency.Code} with {right.Currency.Code}.");
        }
    }

    private static Error CurrencyMismatch(Money left, Money right)
        => Error.Validation(
            "money.currency_mismatch",
            $"Cannot combine {left.Currency.Code} with {right.Currency.Code}.");
}
