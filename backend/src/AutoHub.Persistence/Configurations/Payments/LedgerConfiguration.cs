using AutoHub.Modules.Payments.Domain.Ledger;
using AutoHub.Persistence.Converters;
using AutoHub.SharedKernel.Money;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Payments;

/// <summary>
/// The chart of accounts. Which accounts exist is data; what the five classes mean is not.
/// </summary>
/// <remarks>
/// <see cref="LedgerAccount.Type"/> is stored as its integer rather than its name because it is the
/// definition of double-entry bookkeeping and not configuration — nobody will ever want a sixth
/// class, and the direction that increases each one is not a business preference. What operations
/// does edit is which accounts exist and what they are called, and those are rows.
/// <para>
/// An owner-scoped account shares its <c>code</c> with the control account it was cloned from, so a
/// trial balance groups by code and a dealer's sub-ledger filters by owner. That is why the unique
/// index is over the triple and not over the code alone.
/// </para>
/// </remarks>
internal sealed class LedgerAccountConfiguration : IEntityTypeConfiguration<LedgerAccount>
{
    public void Configure(EntityTypeBuilder<LedgerAccount> builder)
    {
        builder.ToTable(
            "ledger_accounts",
            LedgerSchema.Schema,
            table => table.HasCheckConstraint(
                "ck_ledger_accounts_type",
                "type BETWEEN 1 AND 5"));

        builder.HasKey(account => account.Id);

        builder.Property(account => account.Code).HasMaxLength(64).IsRequired();
        builder.Property(account => account.Name).HasMaxLength(160).IsRequired();
        builder.Property(account => account.OwnerType).HasMaxLength(96);

        builder.Property(account => account.Currency)
            .HasConversion(MoneyConverters.Code)
            .HasColumnType(ColumnTypes.CurrencyCode)
            .IsRequired();

        // One account per code per owner per currency. Two rows for one dealer's wallet is two
        // balances, and which one a payout reads depends on the query plan.
        builder.HasIndex(account => new { account.Code, account.OwnerId, account.Currency })
            .IsUnique()
            .HasDatabaseName("ux_ledger_accounts_scope");

        builder.HasIndex(account => new { account.OwnerType, account.OwnerId })
            .HasFilter("owner_id IS NOT NULL");
    }
}

/// <summary>
/// The journal header. Balanced by construction in the domain and by a deferred trigger in the
/// database, because the domain protects only the writes that go through it.
/// </summary>
/// <remarks>
/// The row-level checks come from <see cref="LedgerSchema.JournalChecks"/> so the specification and
/// the schema cannot drift. The cross-row invariant — that the entries sum to the header's totals —
/// is a <c>CONSTRAINT TRIGGER … DEFERRABLE INITIALLY DEFERRED</c> that a migration installs, because
/// a <c>CHECK</c> sees one row and the invariant spans a journal's rows.
/// <para>
/// <c>currency</c> is stored on the header as well as on each total because it is the journal's own
/// fact — a journal cannot mix currencies at all — and the check below refuses a row where the three
/// could disagree.
/// </para>
/// </remarks>
internal sealed class JournalConfiguration : IEntityTypeConfiguration<Journal>
{
    public void Configure(EntityTypeBuilder<Journal> builder)
    {
        builder.ToTable(
            "ledger_journals",
            LedgerSchema.Schema,
            table =>
            {
                table.HasCheckConstraint(
                    LedgerSchema.JournalChecks.BalancedName,
                    LedgerSchema.JournalChecks.Balanced);

                table.HasCheckConstraint(
                    LedgerSchema.JournalChecks.NonEmptyName,
                    LedgerSchema.JournalChecks.NonEmpty);

                table.HasCheckConstraint(
                    "ck_ledger_journals_single_currency",
                    "currency = total_debits_currency AND currency = total_credits_currency");
            });

        builder.HasKey(journal => journal.Id);

        builder.Property(journal => journal.Reference).HasMaxLength(32).IsRequired();
        builder.Property(journal => journal.ScopeCode).HasMaxLength(64).IsRequired();
        builder.Property(journal => journal.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(journal => journal.SourceType).HasMaxLength(96);
        builder.Property(journal => journal.Description).AsNullableText();

        builder.Property(journal => journal.Currency)
            .HasConversion(MoneyConverters.Code)
            .HasColumnType(ColumnTypes.CurrencyCode)
            .IsRequired();

        builder.ComplexProperty(journal => journal.TotalDebits).HasMoneyColumns("total_debits");
        builder.ComplexProperty(journal => journal.TotalCredits).HasMoneyColumns("total_credits");

        builder.HasMany(journal => journal.Entries)
            .WithOne()
            .HasForeignKey(entry => entry.JournalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.UsesBackingField(nameof(Journal.Entries));

        // The whole point of the key. Unique across the ledger, so a redelivered outbox message, a
        // retried workflow effect and an operator re-running a batch all collide here rather than
        // posting the same money three times.
        builder.HasIndex(journal => journal.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("ux_ledger_journals_idempotency");

        builder.HasIndex(journal => journal.Reference).IsUnique();

        // "Show me everything posted for this deal" — the first query support runs.
        builder.HasIndex(journal => new { journal.SourceType, journal.SourceId })
            .HasFilter("source_id IS NOT NULL");

        builder.HasIndex(journal => new { journal.ScopeCode, journal.PostedAt });
    }
}

/// <summary>
/// The entries. Append-only, and the table two database triggers exist to keep that way.
/// </summary>
/// <remarks>
/// There is no <c>signed_minor_units</c> column: the sign lives in <c>direction</c>, and a stored
/// signed amount is a second representation that a repair script can put out of step with the first.
/// Reports derive it the same way <see cref="LedgerEntry.SignedMinorUnits"/> does.
/// <para>
/// The foreign key to the account is <c>Restrict</c> rather than cascade, and deliberately: an
/// account with entries against it is an account that cannot be deleted, because deleting it would
/// delete history. Closing one is <c>is_active = false</c>.
/// </para>
/// </remarks>
internal sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable(
            "ledger_entries",
            LedgerSchema.Schema,
            table =>
            {
                table.HasCheckConstraint(
                    LedgerSchema.EntryChecks.AmountPositiveName,
                    LedgerSchema.EntryChecks.AmountPositive);

                table.HasCheckConstraint(
                    LedgerSchema.EntryChecks.DirectionName,
                    LedgerSchema.EntryChecks.Direction);

                table.HasCheckConstraint("ck_ledger_entries_line_number", "line_number >= 0");
            });

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.AccountCode).HasMaxLength(64).IsRequired();
        builder.Property(entry => entry.SubjectType).HasMaxLength(96);
        builder.Property(entry => entry.LineCode).HasMaxLength(64);
        builder.Property(entry => entry.Memo).AsNullableText();

        builder.ComplexProperty(entry => entry.Amount).HasMoneyColumns("amount");

        builder.HasOne<LedgerAccount>()
            .WithMany()
            .HasForeignKey(entry => entry.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // The statement query: one account's entries in a window, oldest first. Owner is in the key
        // because a dealer reads their own sub-ledger and never the whole control account.
        builder.HasIndex(entry => new { entry.AccountCode, entry.AccountOwnerId, entry.PostedAt })
            .HasDatabaseName("ix_ledger_entries_statement");

        // The balance query, which reconciliation runs per account and must not scan the table for.
        builder.HasIndex(entry => new { entry.AccountId, entry.PostedAt });

        // One line number per journal, so a posting is reproducible line for line.
        builder.HasIndex(entry => new { entry.JournalId, entry.LineNumber })
            .IsUnique()
            .HasDatabaseName("ux_ledger_entries_line");

        builder.HasIndex(entry => new { entry.SubjectType, entry.SubjectId })
            .HasFilter("subject_id IS NOT NULL");
    }
}

/// <summary>
/// Which accounts a priced line touches. The reason a new fee type needs no deploy.
/// </summary>
/// <remarks>
/// A pricing rule emits a line called <c>auction_commission</c>; a row here says that line debits the
/// buyer's wallet and credits commission income. Neither half is in code, so launching a new fee is
/// two rows — one in the pricing registry, one here.
/// </remarks>
internal sealed class PostingTemplateConfiguration : IEntityTypeConfiguration<PostingTemplate>
{
    public void Configure(EntityTypeBuilder<PostingTemplate> builder)
    {
        builder.ToTable(
            "posting_templates",
            LedgerSchema.Schema,
            table => table.HasCheckConstraint(
                "ck_posting_templates_accounts_differ",
                "debit_account_code <> credit_account_code OR debit_owner_role IS DISTINCT FROM credit_owner_role"));

        builder.HasKey(template => template.Id);

        builder.Property(template => template.ScopeCode).HasMaxLength(64).IsRequired();
        builder.Property(template => template.LineCode).HasMaxLength(64).IsRequired();
        builder.Property(template => template.DebitAccountCode).HasMaxLength(64).IsRequired();
        builder.Property(template => template.CreditAccountCode).HasMaxLength(64).IsRequired();
        builder.Property(template => template.DebitOwnerRole).HasMaxLength(64);
        builder.Property(template => template.CreditOwnerRole).HasMaxLength(64);
        builder.Property(template => template.ConditionJson).AsNullableJsonb();
        builder.Property(template => template.Rationale).AsNullableText();

        // The store's only query: every template for one scope, in order.
        builder.HasIndex(template => new { template.ScopeCode, template.SortOrder })
            .HasFilter("is_active");

        // A GIN index over the condition so "which postings mention this state?" is a query rather
        // than a full scan the day somebody audits the chart of accounts.
        builder.HasIndex(template => template.ConditionJson)
            .HasMethod("gin")
            .HasFilter("condition_json IS NOT NULL");
    }
}
