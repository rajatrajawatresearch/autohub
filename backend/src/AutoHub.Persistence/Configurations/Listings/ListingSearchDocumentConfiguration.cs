using AutoHub.Persistence.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Listings;

internal sealed class ListingSearchDocumentConfiguration : IEntityTypeConfiguration<ListingSearchDocument>
{
    public void Configure(EntityTypeBuilder<ListingSearchDocument> builder)
    {
        builder.ToTable("listing_search_documents", Schemas.Listings);

        // Keyed by listing rather than by its own id, so re-indexing is an upsert on the natural key
        // and a listing can never end up with two documents that disagree.
        builder.HasKey(document => document.ListingId);

        builder.Property(document => document.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(document => document.CityCode).HasMaxLength(64).IsRequired();
        builder.Property(document => document.CoverObjectKey).HasMaxLength(512);
        builder.Property(document => document.SearchText).AsText().IsRequired();

        // The browse query: a city, an intent, ordered by recency. Recency is the listing id, which
        // is UUID v7 and therefore already time-ordered — no separate index on a timestamp.
        builder.HasIndex(document => new { document.CityCode, document.Intent, document.ListingId });

        builder.HasIndex(document => new { document.CategoryId, document.PriceMinorUnits });

        builder.HasIndex(document => document.KilometresDriven)
            .HasFilter("kilometres_driven IS NOT NULL");

        // Trigram rather than full text: the queries this serves are plate lookups and partial model
        // names, where stemming actively hurts — "swif" should match "Swift", and no stemmer does
        // that.
        builder.HasIndex(document => document.SearchText)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
    }
}
