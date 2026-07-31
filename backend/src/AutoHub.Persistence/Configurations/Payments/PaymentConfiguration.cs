using AutoHub.Modules.Payments.Domain;
using AutoHub.Modules.Payments.Domain.Ledger;
using AutoHub.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Payments;

/// <summary>
/// The balance projection a dealer's dashboard reads, rebuilt from the ledger when they disagree.
/// </summary>
/// <remarks>
/// <b>This table is not the truth.</b> <c>payments.ledger_entries</c> is. The wallet exists because
/// re-deriving a balance from every entry ever posted is a query nobody wants on a page load, and
/// <c>ledger_version</c> is what makes the projection auditable: reconciliation replays from that
/// journal forward and the two disagreeing is the incident, not a rounding difference to absorb.
/// <para>
/// <c>withdrawable</c> is deliberately not a column. It is <c>available - held</c>, and a stored
/// third number is a third thing that can be wrong — the one a dealer would quote back.
/// </para>
/// </remarks>
internal sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable(
            "wallets",
            LedgerSchema.Schema,
            table =>
            {
                // A wallet that can go negative is the platform lending money it never agreed to
                // lend, discovered at payout time when the dealer has already spent it.
                table.HasCheckConstraint(
                    "ck_wallets_non_negative",
                    "available_minor_units >= 0 AND held_minor_units >= 0");

                // More held than exists is a payout batch claiming money that is not there.
                table.HasCheckConstraint(
                    "ck_wallets_held_within_available",
                    "held_minor_units <= available_minor_units");

                table.HasCheckConstraint(
                    "ck_wallets_single_currency",
                    "currency = available_currency AND currency = held_currency");
            });

        builder.HasKey(wallet => wallet.Id);

        builder.Property(wallet => wallet.OwnerType).HasMaxLength(96).IsRequired();
        builder.Property(wallet => wallet.AccountCode).HasMaxLength(64).IsRequired();

        builder.Property(wallet => wallet.Currency)
            .HasConversion(MoneyConverters.Code)
            .HasColumnType(ColumnTypes.CurrencyCode)
            .IsRequired();

        builder.ComplexProperty(wallet => wallet.Available).HasMoneyColumns("available");
        builder.ComplexProperty(wallet => wallet.Held).HasMoneyColumns("held");

        // One wallet per owner per account per currency, which is what makes GetOrOpen safe to race.
        // Without it, two concurrent first-credits open two wallets and half the money is invisible.
        builder.HasIndex(wallet => new { wallet.OwnerType, wallet.OwnerId, wallet.AccountCode, wallet.Currency })
            .IsUnique()
            .HasDatabaseName("ux_wallets_owner");

        // The payout assembler's query: wallets with something to pay out that are not frozen.
        builder.HasIndex(wallet => wallet.OwnerId)
            .HasFilter("NOT is_frozen AND available_minor_units > held_minor_units");
    }
}

/// <summary>
/// One attempt to take money, and the row that makes a gateway's duplicate webhooks harmless.
/// </summary>
/// <remarks>
/// <see cref="PaymentIntent.Status"/> is stored as its integer and is <b>not</b> a workflow state: it
/// is the normalised translation of what a vendor said, because Razorpay, Cashfree and PayU do not
/// agree on vocabulary. What the platform <i>does</i> about a captured or failed payment is workflow
/// registry data.
/// <para>
/// The two unique indexes are the two ways the same payment arrives twice — the caller retrying with
/// the same <c>Idempotency-Key</c>, and the vendor redelivering a webhook for the same reference.
/// Both must find this row rather than create a second one.
/// </para>
/// </remarks>
internal sealed class PaymentIntentConfiguration : IEntityTypeConfiguration<PaymentIntent>
{
    public void Configure(EntityTypeBuilder<PaymentIntent> builder)
    {
        builder.ToTable(
            "payment_intents",
            LedgerSchema.Schema,
            table =>
            {
                table.HasCheckConstraint("ck_payment_intents_amount_positive", "amount_minor_units > 0");
                table.HasCheckConstraint("ck_payment_intents_status", "status BETWEEN 1 AND 4");

                // Money cannot be captured without a journal recording where it went. The pair is
                // what stops a gateway callback moving a deal forward with nothing in the ledger.
                table.HasCheckConstraint(
                    "ck_payment_intents_capture_posted",
                    "captured_at IS NULL OR journal_id IS NOT NULL");
            });

        builder.HasKey(intent => intent.Id);

        builder.Property(intent => intent.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(intent => intent.PostingScope).HasMaxLength(64).IsRequired();
        builder.Property(intent => intent.PayerType).HasMaxLength(96);
        builder.Property(intent => intent.MethodCode).HasMaxLength(32);
        builder.Property(intent => intent.ProviderCode).HasMaxLength(64);
        builder.Property(intent => intent.ProviderReference).HasMaxLength(200);
        builder.Property(intent => intent.FailureCode).HasMaxLength(128);

        builder.ComplexProperty(intent => intent.Amount).HasMoneyColumns("amount");

        builder.HasOne<Journal>()
            .WithMany()
            .HasForeignKey(intent => intent.JournalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(intent => intent.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("ux_payment_intents_idempotency");

        // The webhook's lookup. Scoped by provider because two vendors may mint the same reference
        // and neither is wrong.
        builder.HasIndex(intent => new { intent.ProviderCode, intent.ProviderReference })
            .IsUnique()
            .HasFilter("provider_code IS NOT NULL AND provider_reference IS NOT NULL")
            .HasDatabaseName("ux_payment_intents_provider_reference");

        builder.HasIndex(intent => new { intent.DealId, intent.Id })
            .HasFilter("deal_id IS NOT NULL");

        builder.HasIndex(intent => new { intent.PayerId, intent.Id })
            .HasFilter("payer_id IS NOT NULL");
    }
}

/// <summary>
/// A banking day's payouts, assembled once and instructed once.
/// </summary>
/// <remarks>
/// The partial unique index over <c>(currency, business_date)</c> filtered to uninstructed rows is
/// what makes <c>FindOpenBatch</c> safe under concurrency: two schedulers that both decide to open
/// today's batch collide in the database instead of sending the same dealer two transfers.
/// </remarks>
internal sealed class PayoutBatchConfiguration : IEntityTypeConfiguration<PayoutBatch>
{
    public void Configure(EntityTypeBuilder<PayoutBatch> builder)
    {
        builder.ToTable(
            "payout_batches",
            LedgerSchema.Schema,
            table =>
            {
                table.HasCheckConstraint("ck_payout_batches_total_non_negative", "total_minor_units >= 0");

                table.HasCheckConstraint(
                    "ck_payout_batches_single_currency",
                    "currency = total_currency");
            });

        builder.HasKey(batch => batch.Id);

        builder.Property(batch => batch.Reference).HasMaxLength(32).IsRequired();

        builder.Property(batch => batch.Currency)
            .HasConversion(MoneyConverters.Code)
            .HasColumnType(ColumnTypes.CurrencyCode)
            .IsRequired();

        builder.Property(batch => batch.BusinessDate).HasColumnType("date");

        builder.ComplexProperty(batch => batch.Total).HasMoneyColumns("total");

        builder.HasMany(batch => batch.Instructions)
            .WithOne()
            .HasForeignKey(instruction => instruction.PayoutBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(PayoutBatch.Instructions));

        builder.HasIndex(batch => batch.Reference).IsUnique();

        builder.HasIndex(batch => new { batch.Currency, batch.BusinessDate })
            .IsUnique()
            .HasFilter("instructed_at IS NULL")
            .HasDatabaseName("ux_payout_batches_open");

        builder.HasIndex(batch => batch.BusinessDate);
    }
}

/// <summary>
/// One payee's share of a batch.
/// </summary>
/// <remarks>
/// <c>sent</c> and <c>succeeded</c> are separate outcomes because they are different facts: UPI and
/// IMPS reverse hours later and NEFT settles in batches, so "accepted by the bank" is not "the
/// beneficiary has it". Collapsing them would let a statement claim a dealer was paid before the
/// money moved.
/// <para>
/// Each instruction carries its own settlement journal rather than the batch carrying one, so a
/// single refused rail leaves the other nine hundred payouts correctly accounted for.
/// </para>
/// </remarks>
internal sealed class PayoutInstructionConfiguration : IEntityTypeConfiguration<PayoutInstruction>
{
    public void Configure(EntityTypeBuilder<PayoutInstruction> builder)
    {
        builder.ToTable(
            "payout_instructions",
            LedgerSchema.Schema,
            table =>
            {
                table.HasCheckConstraint("ck_payout_instructions_amount_positive", "amount_minor_units > 0");
                table.HasCheckConstraint("ck_payout_instructions_outcome", "outcome BETWEEN 1 AND 4");

                table.HasCheckConstraint(
                    "ck_payout_instructions_settlement_posted",
                    "settled_at IS NULL OR settlement_journal_id IS NOT NULL");
            });

        builder.HasKey(instruction => instruction.Id);

        builder.Property(instruction => instruction.PayeeType).HasMaxLength(96).IsRequired();
        builder.Property(instruction => instruction.PayoutAccountRef).HasMaxLength(200).IsRequired();
        builder.Property(instruction => instruction.ProviderReference).HasMaxLength(200);
        builder.Property(instruction => instruction.FailureCode).HasMaxLength(128);

        builder.ComplexProperty(instruction => instruction.Amount).HasMoneyColumns("amount");

        builder.HasOne<Journal>()
            .WithMany()
            .HasForeignKey(instruction => instruction.SettlementJournalId)
            .OnDelete(DeleteBehavior.Restrict);

        // One instruction per payee per batch, matching the aggregate's own rule. Two rows for one
        // dealer is two bank transfers, and the second is noticed by the recipient and nobody else.
        builder.HasIndex(instruction => new { instruction.PayoutBatchId, instruction.PayeeId })
            .IsUnique()
            .HasDatabaseName("ux_payout_instructions_payee");

        // The reconciliation lookup when the bank's file comes back.
        builder.HasIndex(instruction => instruction.ProviderReference)
            .HasFilter("provider_reference IS NOT NULL");

        builder.HasIndex(instruction => new { instruction.PayeeId, instruction.Id });
    }
}
