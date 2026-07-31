using AutoHub.Modules.Listings.Domain.Listings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Listings;

/// <summary>
/// The listing table, and the column it deliberately does not have.
/// </summary>
/// <remarks>
/// <b>There is no <c>status</c> column, and no index on one.</b> A reviewer looking for it should
/// stop here: the listing's state lives in <c>platform.workflow_instances</c>, keyed by entity id,
/// and every query that wants "live listings" joins to it. Adding one here would be a second source
/// of truth for the fact that changes most often in the product, and the two would first disagree
/// on the day a transition failed halfway.
/// <para>
/// The price is two columns rather than a converted value object, following
/// <c>EntityAttributeValue</c>: an amount and its currency are a pair, and a pair that a reader can
/// see in <c>psql</c> is a pair nobody accidentally reads as rupees.
/// </para>
/// </remarks>
internal sealed class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        builder.ToTable(
            "listings",
            Schemas.Listings,
            table =>
            {
                table.HasCheckConstraint(
                    "ck_listings_price_non_negative",
                    "price_minor_units >= 0");

                table.HasCheckConstraint(
                    "ck_listings_quality_score",
                    "quality_score BETWEEN 0 AND 100");
            });

        builder.HasKey(listing => listing.Id);

        builder.Property(listing => listing.CityCode).HasMaxLength(64).IsRequired();
        builder.Property(listing => listing.PriceCurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(listing => listing.PricePeriodCode).HasMaxLength(32);
        builder.Property(listing => listing.FormKey).HasMaxLength(64).IsRequired();
        builder.Property(listing => listing.Title).HasMaxLength(160);
        builder.Property(listing => listing.Description).AsNullableText();
        builder.Property(listing => listing.QualitySignalsJson).AsNullableJsonb();
        builder.Property(listing => listing.AttributesJson).AsNullableJsonb();

        builder.HasMany(listing => listing.Media)
            .WithOne()
            .HasForeignKey(media => media.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(Listing.Media));

        // The browse path: a city's listings of one intent, newest first. The primary key is UUID v7,
        // so "newest first" is a descending scan of this index rather than a sort.
        builder.HasIndex(listing => new { listing.CityCode, listing.Intent, listing.Id });

        // A seller's own inventory, which is the dealer portal's landing query.
        builder.HasIndex(listing => new { listing.SellerPartyId, listing.Id });

        // "Is this car already listed?" — asked on every sell-flow submission to stop a seller
        // creating a second listing for a vehicle they are already selling.
        builder.HasIndex(listing => listing.VehicleId);

        builder.HasIndex(listing => new { listing.CategoryId, listing.PriceMinorUnits });

        builder.HasIndex(listing => listing.AttributesJson)
            .HasMethod("gin")
            .HasFilter("attributes_json IS NOT NULL");
    }
}

internal sealed class ListingMediaConfiguration : IEntityTypeConfiguration<ListingMedia>
{
    public void Configure(EntityTypeBuilder<ListingMedia> builder)
    {
        builder.ToTable(
            "listing_media",
            Schemas.Listings,
            table => table.HasCheckConstraint(
                "ck_listing_media_dimensions",
                "(width IS NULL OR width > 0) AND (height IS NULL OR height > 0)"));

        builder.HasKey(media => media.Id);

        builder.Property(media => media.ObjectKey).HasMaxLength(512).IsRequired();
        builder.Property(media => media.MediaTypeCode).HasMaxLength(32).IsRequired();
        builder.Property(media => media.PromptCode).HasMaxLength(64);
        builder.Property(media => media.ContentHash).HasMaxLength(128);

        builder.HasIndex(media => new { media.ListingId, media.SortOrder });

        // One cover per listing, enforced by a partial unique index rather than by trusting the
        // aggregate alone. The aggregate unsets the previous cover in the same transaction, so this
        // never fires in normal operation — it fires when a repair script sets a second one, which
        // would otherwise render as a card whose image changes depending on the query plan.
        builder.HasIndex(media => media.ListingId)
            .IsUnique()
            .HasFilter("is_cover")
            .HasDatabaseName("ux_listing_media_cover");

        // The scraped-listing signal: the same image bytes under two sellers in two cities. It is
        // the most common fraud pattern on Indian classifieds, and this index is what makes finding
        // it a query rather than a batch job.
        builder.HasIndex(media => media.ContentHash)
            .HasFilter("content_hash IS NOT NULL");
    }
}
