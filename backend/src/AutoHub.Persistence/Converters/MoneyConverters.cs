using AutoHub.SharedKernel.Money;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AutoHub.Persistence.Converters;

/// <summary>
/// Money between the domain and PostgreSQL: a <c>bigint</c> of minor units paired with an ISO 4217
/// code. Never a decimal of rupees, never a bare number.
/// </summary>
/// <remarks>
/// <b>Why an amount is always two columns.</b> A <c>Money</c> stripped of its currency is a number,
/// and a number is what somebody eventually adds to a different currency's number. So an amount is
/// mapped as a pair and named as a pair — <c>price_minor_units</c> next to <c>price_currency</c> —
/// which makes the relationship obvious to the next person reading the column list in psql, where no
/// C# type is there to enforce it.
/// <para>
/// <b>Why not <c>numeric(18,2)</c>.</b> Exactness is not the objection; <c>numeric</c> is exact. The
/// objection is that it makes a fractional rupee <i>representable</i>, and once it is, one service
/// rounds a 2% commission at two decimals, another at four, and month-end stops balancing over a
/// difference nobody can locate. Minor units make the unrepresentable actually unrepresentable and
/// push every rounding decision through <c>Money.MultiplyByRate</c> or <c>Money.Allocate</c>, where
/// it is named, reviewed and tested.
/// </para>
/// <para>
/// <b>How to use them.</b> An entity that holds an amount declares the amount and its currency as
/// two properties and maps them together:
/// <code>
/// builder.Property(d => d.Amount)
///     .HasConversion(MoneyConverters.ForCurrency(Currency.Inr))
///     .HasColumnName("amount_minor_units");
///
/// builder.Property(d => d.Currency)
///     .HasConversion(MoneyConverters.Code)
///     .HasColumnName("amount_currency")
///     .HasColumnType("char(3)");
/// </code>
/// A single-currency ledger — GST output tax is INR by law rather than by convention — may map the
/// amount alone and omit the code. That is the only case where dropping the pair is honest.
/// </para>
/// </remarks>
public static class MoneyConverters
{
    /// <summary>
    /// Amount as <c>bigint</c> minor units, reconstructed against a currency the column already
    /// implies.
    /// </summary>
    /// <remarks>
    /// A value converter sees one column and cannot consult its sibling, so the currency is supplied
    /// at mapping time. Where the currency is genuinely variable, the code column is authoritative
    /// and the reading side rebuilds the <c>Money</c> from both — see the type's remarks.
    /// </remarks>
    public static ValueConverter<Money, long> ForCurrency(Currency currency)
        => new(
            money => money.MinorUnits,
            minorUnits => Money.FromMinorUnits(minorUnits, currency));

    /// <summary>
    /// An amount that may not have been decided yet: a reserve nobody set, a penalty nobody charged.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="ForCurrency"/> because a converter over <c>Money</c> cannot be reused
    /// for <c>Money?</c> — EF would treat the null as the struct's default and write a zero, which is
    /// a reserve of nothing rather than no reserve, and the difference decides whether a lot sells.
    /// </remarks>
    public static ValueConverter<Money?, long?> ForOptionalCurrency(Currency currency)
        => new(
            money => money == null ? null : money.Value.MinorUnits,
            minorUnits => minorUnits == null ? null : Money.FromMinorUnits(minorUnits.Value, currency));

    /// <summary>ISO 4217 code as <c>char(3)</c>.</summary>
    /// <remarks>
    /// <c>Currency.FromCode</c> throws on an unknown code. That is right here rather than forgiving:
    /// a code this converter cannot read means a row was written by a version of the platform that
    /// supported a currency this one does not, and quietly substituting INR would silently restate
    /// somebody's money.
    /// </remarks>
    public static ValueConverter<Currency, string> Code { get; } = new(
        currency => currency.Code,
        code => Currency.FromCode(code));
}
