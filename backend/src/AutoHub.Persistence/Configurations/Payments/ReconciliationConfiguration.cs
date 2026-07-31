using AutoHub.Modules.Payments.Application;
using AutoHub.Modules.Payments.Domain;
using AutoHub.Modules.Payments.Domain.Ledger;
using AutoHub.Persistence.Converters;
using AutoHub.SharedKernel.Money;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Payments;

/// <summary>
/// One night's comparison of a provider's settlement file against the ledger.
/// </summary>
/// <remarks>
/// Kept as a table rather than a log line because a reconciliation that found nothing is evidence
/// too: the question a finance team asks in March is not "what broke last night" but "did this run
/// at all, every night, since January". A run with zero differences is the row that answers it.
/// <para>
/// The totals are stored per side rather than as a single gap, so a run where both sides moved by
/// the same amount is distinguishable from one where neither did. A stored gap alone collapses those
/// two into the same zero.
/// </para>
/// </remarks>
internal sealed class ReconciliationRunConfiguration : IEntityTypeConfiguration<ReconciliationRun>
{
    public void Configure(EntityTypeBuilder<ReconciliationRun> builder)
    {
        builder.ToTable(
            "reconciliation_runs",
            LedgerSchema.Schema,
            table =>
            {
                table.HasCheckConstraint("ck_reconciliation_runs_matched", "matched_count >= 0");

                table.HasCheckConstraint(
                    "ck_reconciliation_runs_single_currency",
                    "currency = provider_total_currency AND currency = ledger_total_currency");
            });

        builder.HasKey(run => run.Id);

        builder.Property(run => run.ProviderCode).HasMaxLength(64).IsRequired();
        builder.Property(run => run.BusinessDate).HasColumnType("date");

        builder.Property(run => run.Currency)
            .HasConversion(MoneyConverters.Code)
            .HasColumnType(ColumnTypes.CurrencyCode)
            .IsRequired();

        builder.ComplexProperty(run => run.ProviderTotal).HasMoneyColumns("provider_total");
        builder.ComplexProperty(run => run.LedgerTotal).HasMoneyColumns("ledger_total");

        builder.HasMany(run => run.Differences)
            .WithOne()
            .HasForeignKey(difference => difference.ReconciliationRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(ReconciliationRun.Differences));

        // "Did last night's run happen, and did it finish?" — the query the morning check runs, and
        // the one that makes a silently dead scheduler visible.
        builder.HasIndex(run => new { run.ProviderCode, run.BusinessDate });
    }
}

/// <summary>
/// One line the two sides disagree about.
/// </summary>
/// <remarks>
/// Both amounts are nullable and by design: a difference is often the absence of one side. Money
/// present at the provider and missing in the ledger has no ledger amount, and vice versa — which is
/// exactly the fact <see cref="ReconciliationDifferenceKind"/> names.
/// <para>
/// The optional amounts carry no currency column, and the run's <c>currency</c> is authoritative for
/// both — a run compares one provider's file for one day in one currency, so a difference in another
/// currency is not a difference, it is a bug. <c>ck_reconciliation_runs_single_currency</c> on the
/// parent is what pins that.
/// </para>
/// </remarks>
internal sealed class ReconciliationDifferenceConfiguration
    : IEntityTypeConfiguration<ReconciliationDifference>
{
    public void Configure(EntityTypeBuilder<ReconciliationDifference> builder)
    {
        builder.ToTable(
            "reconciliation_differences",
            LedgerSchema.Schema,
            table =>
            {
                table.HasCheckConstraint("ck_reconciliation_differences_kind", "kind BETWEEN 1 AND 4");

                // A difference with neither side is not a difference. It would count towards the
                // failure total while describing nothing an operator can act on.
                table.HasCheckConstraint(
                    "ck_reconciliation_differences_has_side",
                    "provider_amount_minor_units IS NOT NULL OR ledger_amount_minor_units IS NOT NULL");
            });

        builder.HasKey(difference => difference.Id);

        builder.Property(difference => difference.ProviderReference).HasMaxLength(200).IsRequired();
        builder.Property(difference => difference.Note).AsNullableText();

        builder.HasOptionalMoneyColumn(
            difference => difference.ProviderAmount, "provider_amount", Currency.Inr);

        builder.HasOptionalMoneyColumn(
            difference => difference.LedgerAmount, "ledger_amount", Currency.Inr);

        builder.HasOne<PaymentIntent>()
            .WithMany()
            .HasForeignKey(difference => difference.PaymentIntentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(difference => new { difference.ReconciliationRunId, difference.Kind });

        // "Has this reference ever failed to reconcile?" — asked whenever a dealer disputes a capture.
        builder.HasIndex(difference => difference.ProviderReference);
    }
}
