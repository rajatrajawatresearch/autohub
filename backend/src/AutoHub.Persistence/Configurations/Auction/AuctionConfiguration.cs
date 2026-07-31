using AutoHub.Modules.Auction.Domain;
using AutoHub.SharedKernel.Money;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Auction;

/// <summary>
/// The terms an auction's settings occupy, mapped once and reused by all three tables that copy them.
/// </summary>
/// <remarks>
/// A complex property rather than a table of its own, because the whole point of
/// <see cref="AuctionSettings"/> is that a lot carries its terms in its own row: the bid path reads
/// one lot and never joins to the auction or to the profile the terms came from. At four hundred lots
/// and several thousand bids a minute, that join is the difference between a bid costing one row read
/// and costing three.
/// <para>
/// It is also why the settings are copied rather than referenced. An operator improving the default
/// anti-snipe window must not change the rules of an auction dealers are already bidding in.
/// </para>
/// </remarks>
internal static class AuctionSettingsMapping
{
    public static ComplexPropertyBuilder<AuctionSettings> HasSettingsColumns(
        this ComplexPropertyBuilder<AuctionSettings> builder)
    {
        builder.Property(settings => settings.BidsVisible).HasColumnName("bids_visible");
        builder.Property(settings => settings.BidderIdentityVisible).HasColumnName("bidder_identity_visible");
        builder.Property(settings => settings.ProxyBiddingAllowed).HasColumnName("proxy_bidding_allowed");
        builder.Property(settings => settings.BuyNowAllowed).HasColumnName("buy_now_allowed");
        builder.Property(settings => settings.ReserveRequired).HasColumnName("reserve_required");
        builder.Property(settings => settings.AntiSnipeWindowSeconds).HasColumnName("anti_snipe_window_seconds");
        builder.Property(settings => settings.AntiSnipeExtensionSeconds).HasColumnName("anti_snipe_extension_seconds");
        builder.Property(settings => settings.MaxExtensions).HasColumnName("max_extensions");
        builder.Property(settings => settings.BidIncrementScope).HasColumnName("bid_increment_scope").HasMaxLength(96);
        builder.Property(settings => settings.PaymentWindowMinutes).HasColumnName("payment_window_minutes");
        builder.Property(settings => settings.AwardsInstantly).HasColumnName("awards_instantly");

        return builder;
    }

    /// <summary>The row-level sanity the settings columns must hold wherever they are embedded.</summary>
    public const string Checks =
        "anti_snipe_window_seconds >= 0 AND anti_snipe_extension_seconds >= 0 "
        + "AND max_extensions >= 0 AND payment_window_minutes > 0";
}

/// <summary>The sale event: a set of lots that open and close together under one set of terms.</summary>
internal sealed class AuctionEventConfiguration : IEntityTypeConfiguration<Modules.Auction.Domain.Auction>
{
    public void Configure(EntityTypeBuilder<Modules.Auction.Domain.Auction> builder)
    {
        builder.ToTable(
            "auctions",
            Schemas.Auction,
            table =>
            {
                table.HasCheckConstraint("ck_auctions_window", "closes_at > opens_at");
                table.HasCheckConstraint("ck_auctions_preview", "preview_opens_at <= opens_at");
                table.HasCheckConstraint("ck_auctions_settings", AuctionSettingsMapping.Checks);
            });

        builder.HasKey(auction => auction.Id);

        builder.Property(auction => auction.Code).HasMaxLength(48).IsRequired();
        builder.Property(auction => auction.Title).HasMaxLength(200).IsRequired();
        builder.Property(auction => auction.TypeCode).HasMaxLength(64).IsRequired();
        builder.Property(auction => auction.WinPricingScope).HasMaxLength(96).IsRequired();
        builder.Property(auction => auction.CityCode).HasMaxLength(64);
        builder.Property(auction => auction.StateCode).HasMaxLength(8);

        builder.ComplexProperty(auction => auction.Settings).HasSettingsColumns();

        builder.HasMany(auction => auction.Lots)
            .WithOne()
            .HasForeignKey(lot => lot.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(Modules.Auction.Domain.Auction.Lots));

        // Printed on the catalogue and read out on a call, so it identifies exactly one live auction.
        builder.HasIndex(auction => auction.Code).IsUniqueAmongLiveRows();

        // "What is running in Pune this week?" — the catalogue's landing query.
        builder.HasIndex(auction => new { auction.CityCode, auction.OpensAt })
            .HasFilter("deleted_at IS NULL");
    }
}

/// <summary>
/// A named, operator-editable auction behaviour profile.
/// </summary>
/// <remarks>
/// There is no <c>TimedAuction</c> table and no type discriminator anywhere in this schema. The six
/// auction "types" in the brief are five independent switches that real auctions combine freely, so
/// they are columns on this row and an auction takes a copy of them.
/// </remarks>
internal sealed class AuctionTypeProfileConfiguration : IEntityTypeConfiguration<AuctionTypeProfile>
{
    public void Configure(EntityTypeBuilder<AuctionTypeProfile> builder)
    {
        builder.ToTable(
            "auction_type_profiles",
            Schemas.Auction,
            table => table.HasCheckConstraint("ck_auction_type_profiles_settings", AuctionSettingsMapping.Checks));

        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.TypeCode).HasMaxLength(64).IsRequired();
        builder.Property(profile => profile.Name).HasMaxLength(160).IsRequired();
        builder.Property(profile => profile.Description).AsNullableText();

        builder.ComplexProperty(profile => profile.Settings).HasSettingsColumns();

        builder.HasIndex(profile => profile.TypeCode).IsUnique();
    }
}

/// <summary>
/// One vehicle in a sale, and the hot row in the product.
/// </summary>
/// <remarks>
/// <b>What is not here.</b> No status column — the lot's lifecycle lives in
/// <c>platform.workflow_instances</c> — and no bid list on the read path. <c>high_bid_*</c> and
/// <c>bid_sequence</c> are the standing state the gate writes back, so rendering a lot card never
/// touches <c>auction.bids</c>.
/// <para>
/// <b>The optional amounts carry no currency column</b>, and <c>ck_auction_lots_currency</c> is why
/// that is safe: the lot's currency is <c>start_price_currency</c>, and the constraint refuses a row
/// where the assumption the mapping makes could be wrong. See <see cref="MoneyMapping"/>.
/// </para>
/// </remarks>
internal sealed class AuctionLotConfiguration : IEntityTypeConfiguration<AuctionLot>
{
    public void Configure(EntityTypeBuilder<AuctionLot> builder)
    {
        builder.ToTable(
            "auction_lots",
            Schemas.Auction,
            table =>
            {
                table.HasCheckConstraint("ck_auction_lots_window", "closes_at > opens_at");
                table.HasCheckConstraint("ck_auction_lots_settings", AuctionSettingsMapping.Checks);

                table.HasCheckConstraint(
                    "ck_auction_lots_prices_positive",
                    "start_price_minor_units >= 0 "
                    + "AND (reserve_price_minor_units IS NULL OR reserve_price_minor_units >= 0) "
                    + "AND (buy_now_price_minor_units IS NULL OR buy_now_price_minor_units >= 0)");

                // A reserve below the opening price is a reserve that is always met, which the
                // aggregate refuses and a bulk import would not.
                table.HasCheckConstraint(
                    "ck_auction_lots_reserve_above_start",
                    "reserve_price_minor_units IS NULL OR reserve_price_minor_units >= start_price_minor_units");

                table.HasCheckConstraint(
                    "ck_auction_lots_counts",
                    "bid_count >= 0 AND extension_count >= 0 AND relist_count >= 0 AND bid_sequence >= 0");

                table.HasCheckConstraint(
                    "ck_auction_lots_currency",
                    MoneyMapping.MustMatchRowCurrency("start_price_currency", Currency.Inr));
            });

        builder.HasKey(lot => lot.Id);

        builder.Property(lot => lot.SellerType).HasMaxLength(96).IsRequired();
        builder.Property(lot => lot.WinPricingScope).HasMaxLength(96).IsRequired();
        builder.Property(lot => lot.CityCode).HasMaxLength(64);
        builder.Property(lot => lot.StateCode).HasMaxLength(8);

        builder.ComplexProperty(lot => lot.StartPrice).HasMoneyColumns("start_price");
        builder.ComplexProperty(lot => lot.Settings).HasSettingsColumns();

        builder.HasOptionalMoneyColumn(lot => lot.ReservePrice, "reserve_price", Currency.Inr);
        builder.HasOptionalMoneyColumn(lot => lot.BuyNowPrice, "buy_now_price", Currency.Inr);
        builder.HasOptionalMoneyColumn(lot => lot.HighBidAmount, "high_bid_amount", Currency.Inr);
        builder.HasOptionalMoneyColumn(lot => lot.WinningAmount, "winning_amount", Currency.Inr);

        // The catalogue's order, and what an auctioneer says out loud. Unique so two lots cannot both
        // be "lot 14" in the same sale.
        builder.HasIndex(lot => new { lot.AuctionId, lot.LotNumber })
            .IsUniqueAmongLiveRows()
            .HasDatabaseName("ux_auction_lots_number");

        // The close sweeper's query: lots whose time has passed and which have not been closed out.
        // Filtered so the scan is over the handful that are due rather than over every lot ever run.
        builder.HasIndex(lot => lot.ClosesAt)
            .HasFilter("closed_at IS NULL AND deleted_at IS NULL")
            .HasDatabaseName("ix_auction_lots_due_for_close");

        // "Has this vehicle been through a sale before?" — the relist count's own source.
        builder.HasIndex(lot => lot.VehicleId);

        builder.HasIndex(lot => new { lot.SellerId, lot.Id });

        // The forfeiture timer: winners whose payment window has run out.
        builder.HasIndex(lot => lot.PaymentDueAt)
            .HasFilter("payment_due_at IS NOT NULL AND deal_id IS NULL");
    }
}

/// <summary>
/// Every admission decision the gate made, including the ones it refused.
/// </summary>
/// <remarks>
/// A rejected bid is the evidence that answers "I bid and it did not go through", and that question
/// arrives from a certain dealer within an hour of every busy sale. So the table is append-only in
/// practice and there is no update path in the repository.
/// <para>
/// <c>maximum_amount_minor_units</c> is the bidder's ceiling and never leaves the server. It is
/// deliberately not in any index, so it cannot be inferred from a query plan or an ordering.
/// </para>
/// </remarks>
internal sealed class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.ToTable(
            "bids",
            Schemas.Auction,
            table =>
            {
                table.HasCheckConstraint("ck_bids_amount_positive", "amount_minor_units > 0");
                table.HasCheckConstraint("ck_bids_sequence", "sequence >= 0");

                table.HasCheckConstraint(
                    "ck_bids_currency",
                    MoneyMapping.MustMatchRowCurrency("amount_currency", Currency.Inr));
            });

        builder.HasKey(bid => bid.Id);

        builder.Property(bid => bid.BidderType).HasMaxLength(96).IsRequired();
        builder.Property(bid => bid.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(bid => bid.DeviceId).HasMaxLength(128);
        builder.Property(bid => bid.IpAddress).HasMaxLength(64);

        builder.ComplexProperty(bid => bid.Amount).HasMoneyColumns("amount");
        builder.ComplexProperty(bid => bid.StandingAmountAfter).HasMoneyColumns("standing_amount_after");

        builder.HasOptionalMoneyColumn(bid => bid.MaximumAmount, "maximum_amount", Currency.Inr);

        builder.HasOne<AuctionLot>()
            .WithMany()
            .HasForeignKey(bid => bid.AuctionLotId)
            .OnDelete(AggregateConfiguration.WhenPrincipalIsSoftDeleted);

        // The replay cursor the socket reconnects on, and the winner query's index. Ordered by the
        // gate's sequence rather than by amount or timestamp: amount ties on an exact proxy match and
        // timestamps tie at millisecond resolution, so the sequence is the only total order.
        builder.HasIndex(bid => new { bid.AuctionLotId, bid.Sequence })
            .HasDatabaseName("ix_bids_lot_sequence");

        // The dedupe that makes a dropped connection safe to retry. Unique across the lot, because
        // the same key resubmitted must find the original decision rather than place a second bid.
        builder.HasIndex(bid => new { bid.AuctionLotId, bid.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_bids_idempotency");

        // Fraud scoring: two "different" dealers bidding from one device is a ring.
        builder.HasIndex(bid => new { bid.DeviceId, bid.PlacedAt })
            .HasFilter("device_id IS NOT NULL");

        builder.HasIndex(bid => new { bid.BidderId, bid.Id });
    }
}
