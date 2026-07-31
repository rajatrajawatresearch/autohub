using System.Linq.Expressions;
using AutoHub.Persistence.Converters;
using AutoHub.SharedKernel.Money;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations;

/// <summary>
/// Maps a <see cref="Money"/> property to the amount/currency column pair it has always been.
/// </summary>
/// <remarks>
/// <b>Why a complex property and not a value converter.</b> A converter sees exactly one column, so
/// mapping <c>Money</c> through one means supplying the currency at model-build time — honest only
/// where the currency is fixed by law rather than by convention. The modules mapped through here
/// settle deals, hold wallets and instruct payouts, and the export marketplace on the roadmap makes
/// "it is always INR" a statement that will stop being true after the rows exist. A complex property
/// keeps both halves of the pair in the row, so a deal written in AED reads back as AED instead of
/// silently restating somebody's money.
/// <para>
/// <b>Why the currency is a converted scalar rather than a nested complex type.</b>
/// <see cref="Currency"/> carries a computed <c>MinorUnitsPerMajor</c> derived from
/// <see cref="Currency.Decimals"/>; mapping it structurally would persist a scale that is a property
/// of the ISO code and let a repair script write a row where the two disagree. Converting to the
/// three-character code and rebuilding through <c>Currency.FromCode</c> means the code is the only
/// stored fact and the scale is always the one this build believes.
/// </para>
/// <para>
/// The column names follow the pair convention the rest of the model uses — <c>amount_minor_units</c>
/// next to <c>amount_currency</c> — so the relationship is visible in <c>psql</c>, where no C# type
/// is there to enforce it.
/// </para>
/// </remarks>
internal static class MoneyMapping
{
    /// <summary>Maps a required amount to <c>{prefix}_minor_units</c> and <c>{prefix}_currency</c>.</summary>
    public static ComplexPropertyBuilder<Money> HasMoneyColumns(
        this ComplexPropertyBuilder<Money> builder,
        string prefix)
    {
        builder.Property(money => money.MinorUnits)
            .HasColumnName($"{prefix}_minor_units")
            .IsRequired();

        builder.Property(money => money.Currency)
            .HasConversion(MoneyConverters.Code)
            .HasColumnName($"{prefix}_currency")
            .HasColumnType(ColumnTypes.CurrencyCode)
            .IsRequired();

        return builder;
    }

    /// <summary>
    /// Maps an amount that may not have been decided yet — a reserve nobody set, a penalty nobody
    /// charged — to a single nullable <c>{prefix}_minor_units</c> column.
    /// </summary>
    /// <remarks>
    /// <b>Why this one is not a pair, and why that is a limitation rather than a design.</b> EF Core 9
    /// refuses to configure a complex property as optional (<c>efcore#31376</c>), so a nullable
    /// <c>Money</c> cannot carry its own currency column the way a required one does. The remaining
    /// options were all worse: a non-nullable pair turns "no reserve" into "a reserve of zero", which
    /// is the difference between a lot that cannot sell and one that sells for nothing; and a shadow
    /// currency column that EF fills from a default would write a currency onto rows that hold no
    /// amount, so "both or neither" would be false in the table rather than merely unenforced.
    /// <para>
    /// So the currency is supplied at mapping time, and <see cref="MustMatchRowCurrency"/> exists to
    /// pin that assumption in the database rather than leave it in a comment. Callers pass the column
    /// that <i>is</i> authoritative for the row — a lot's <c>start_price_currency</c>, a journal's
    /// <c>currency</c> — and the constraint refuses any row where the two could disagree. The day a
    /// second currency arrives, the write fails loudly at the boundary instead of the read quietly
    /// restating the number.
    /// </para>
    /// </remarks>
    public static PropertyBuilder<Money?> HasOptionalMoneyColumn<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, Money?>> selector,
        string prefix,
        Currency currency)
        where TEntity : class
        => builder.Property(selector)
            .HasConversion(MoneyConverters.ForOptionalCurrency(currency))
            .HasColumnName($"{prefix}_minor_units");

    /// <summary>
    /// The check that keeps <see cref="HasOptionalMoneyColumn{TEntity}"/>'s assumption true.
    /// </summary>
    /// <remarks>
    /// Reads as "the currency this row is denominated in is the one the optional amounts were mapped
    /// against". It is deliberately a constraint and not a comment: the failure it prevents is a read
    /// that returns the right number under the wrong currency, which no test catches and no report
    /// shows, because every number involved is individually plausible.
    /// </remarks>
    public static string MustMatchRowCurrency(string currencyColumn, Currency currency)
        => $"{currencyColumn} = '{currency.Code}'";
}
