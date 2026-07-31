using AutoHub.Modules.Deals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Deals;

/// <summary>
/// The settlement spine's table. One shape for every commercial intent the platform has.
/// </summary>
/// <remarks>
/// <b>There is no <c>status</c> column</b>, for the reason <c>listings.listings</c> has none: where a
/// deal sits lives in <c>platform.workflow_instances</c>. The timestamps here are facts about what
/// happened after the engine allowed a move, and nothing queries them to decide whether a move is
/// permitted.
/// <para>
/// <b>The source pair is not a foreign key.</b> <c>source_type</c>/<c>source_id</c> point at an
/// auction lot, a listing, a rental booking or a lease agreement, and a real constraint to any of
/// them would give the deals schema a table-level dependency on five other modules — the one thing
/// that stops a module being extracted. The uniqueness that actually matters is enforced instead: one
/// deal per source, so a redelivered outbox message cannot open a second deal and pay the seller
/// twice.
/// </para>
/// </remarks>
internal sealed class DealConfiguration : IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> builder)
    {
        builder.ToTable(
            "deals",
            Schemas.Deals,
            table =>
            {
                table.HasCheckConstraint(
                    "ck_deals_amounts_non_negative",
                    "base_amount_minor_units >= 0 AND fee_total_minor_units >= 0 AND payable_minor_units >= 0");

                // The arithmetic the whole module rests on, asserted where a repair script also has to
                // obey it. A deal whose payable does not equal base plus fees is an invoice that
                // disagrees with its own lines, and it is found by the customer.
                table.HasCheckConstraint(
                    "ck_deals_payable_balances",
                    "payable_minor_units = base_amount_minor_units + fee_total_minor_units");

                table.HasCheckConstraint(
                    "ck_deals_period",
                    "period_end IS NULL OR period_start IS NULL OR period_end > period_start");

                // One currency per deal. Three amounts that disagree cannot be summed, and the ledger
                // would balance to a number that means nothing.
                table.HasCheckConstraint(
                    "ck_deals_single_currency",
                    "base_amount_currency = fee_total_currency AND base_amount_currency = payable_currency");
            });

        builder.HasKey(deal => deal.Id);

        builder.Property(deal => deal.Reference).HasMaxLength(32).IsRequired();
        builder.Property(deal => deal.IntentCode).HasMaxLength(64).IsRequired();
        builder.Property(deal => deal.PricingScope).HasMaxLength(96).IsRequired();
        builder.Property(deal => deal.SourceType).HasMaxLength(96);
        builder.Property(deal => deal.RecurrenceCode).HasMaxLength(32);
        builder.Property(deal => deal.CityCode).HasMaxLength(64);
        builder.Property(deal => deal.StateCode).HasMaxLength(8);
        builder.Property(deal => deal.AppliedRuleCode).HasMaxLength(96);
        builder.Property(deal => deal.PricingExplanation).AsNullableText();
        builder.Property(deal => deal.SettlementReference).HasMaxLength(128);
        builder.Property(deal => deal.CancellationReason).AsNullableText();

        builder.ComplexProperty(deal => deal.BaseAmount).HasMoneyColumns("base_amount");
        builder.ComplexProperty(deal => deal.FeeTotal).HasMoneyColumns("fee_total");
        builder.ComplexProperty(deal => deal.Payable).HasMoneyColumns("payable");

        builder.HasMany(deal => deal.Parties)
            .WithOne()
            .HasForeignKey(party => party.DealId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(deal => deal.Lines)
            .WithOne()
            .HasForeignKey(line => line.DealId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(Deal.Parties));
        builder.UsesBackingField(nameof(Deal.Lines));

        // What a support agent types in. Unique among live rows so a reference read out over the
        // phone identifies exactly one deal.
        builder.HasIndex(deal => deal.Reference).IsUniqueAmongLiveRows();

        // The idempotency guard every module that opens a deal on an event runs first. Unique rather
        // than merely indexed: an auction lot that closes twice must collide here rather than settle
        // twice, and the constraint is what makes that true for writers that never call the handler.
        builder.HasIndex(deal => new { deal.SourceType, deal.SourceId })
            .IsUnique()
            .HasFilter("source_type IS NOT NULL AND source_id IS NOT NULL AND deleted_at IS NULL")
            .HasDatabaseName("ux_deals_source");

        // A lease's instalments, in order. The one query a recurring agreement's page is built from.
        builder.HasIndex(deal => new { deal.ParentDealId, deal.InstalmentNumber })
            .HasFilter("parent_deal_id IS NOT NULL");

        builder.HasIndex(deal => new { deal.IntentCode, deal.Id });

        builder.HasIndex(deal => deal.ListingId).HasFilter("listing_id IS NOT NULL");

        builder.HasIndex(deal => deal.VehicleId).HasFilter("vehicle_id IS NOT NULL");
    }
}

/// <summary>
/// The counterparties, and the three columns that are structural rather than configurable.
/// </summary>
/// <remarks>
/// <c>role_code</c> is taxonomy data, so a new kind of counterparty is a row. <c>is_payer</c>,
/// <c>is_payee</c> and <c>settlement_share_ratio</c> are not, because the ledger has to know which
/// direction money moves without interpreting a label it cannot read.
/// </remarks>
internal sealed class DealPartyConfiguration : IEntityTypeConfiguration<DealParty>
{
    public void Configure(EntityTypeBuilder<DealParty> builder)
    {
        builder.ToTable(
            "deal_parties",
            Schemas.Deals,
            table => table.HasCheckConstraint(
                "ck_deal_parties_share_non_negative",
                "settlement_share_ratio >= 0"));

        builder.HasKey(party => party.Id);

        builder.Property(party => party.RoleCode).HasMaxLength(64).IsRequired();
        builder.Property(party => party.PartyType).HasMaxLength(96);
        builder.Property(party => party.PayoutAccountRef).HasMaxLength(128);

        // One row per role per party, matching the aggregate's own idempotency on AttachParty. The
        // aggregate already refuses the duplicate; this refuses it for the retry that arrives on a
        // second connection while the first is still in flight.
        builder.HasIndex(party => new { party.DealId, party.RoleCode, party.PartyId })
            .IsUnique()
            .HasDatabaseName("ux_deal_parties_role");

        // "Every deal this dealer is on" — the dealer portal's landing query.
        builder.HasIndex(party => new { party.PartyId, party.Id })
            .HasFilter("party_id IS NOT NULL");
    }
}

/// <summary>
/// The priced lines, frozen at the moment the engine emitted them.
/// </summary>
/// <remarks>
/// Deliberately no discriminator saying whether a line is a fee, a tax or a deposit: that mapping
/// lives in <c>payments.posting_templates</c>, keyed on this row's <c>code</c>, so a new fee type is
/// a pricing rule plus a posting rule and no deploy.
/// </remarks>
internal sealed class DealLineConfiguration : IEntityTypeConfiguration<DealLine>
{
    public void Configure(EntityTypeBuilder<DealLine> builder)
    {
        builder.ToTable(
            "deal_lines",
            Schemas.Deals,
            table => table.HasCheckConstraint(
                "ck_deal_lines_rate",
                "rate IS NULL OR (rate >= 0 AND rate <= 1)"));

        builder.HasKey(line => line.Id);

        builder.Property(line => line.Code).HasMaxLength(64).IsRequired();
        builder.Property(line => line.Label).HasMaxLength(160).IsRequired();
        builder.Property(line => line.RuleCode).HasMaxLength(96);
        builder.Property(line => line.AppliesToCsv).HasMaxLength(512);
        builder.Property(line => line.Rate).HasColumnType("numeric(10,6)");

        builder.ComplexProperty(line => line.Amount).HasMoneyColumns("amount");

        // One line per code per deal. ApplyQuote clears and rebuilds, so a duplicate here means two
        // quotes were applied concurrently — which would double the commission on the invoice.
        builder.HasIndex(line => new { line.DealId, line.Code })
            .IsUnique()
            .HasDatabaseName("ux_deal_lines_code");

        builder.HasIndex(line => new { line.DealId, line.SortOrder });

        // One principal per deal, enforced rather than trusted: the principal is the deal's own fact
        // and no rule set is allowed to restate it.
        builder.HasIndex(line => line.DealId)
            .IsUnique()
            .HasFilter("is_principal")
            .HasDatabaseName("ux_deal_lines_principal");
    }
}
