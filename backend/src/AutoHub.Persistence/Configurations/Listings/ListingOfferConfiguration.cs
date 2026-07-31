using AutoHub.Modules.Listings.Domain.Listings;
using AutoHub.Modules.Listings.Domain.Offers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Listings;

internal sealed class ListingOfferConfiguration : IEntityTypeConfiguration<ListingOffer>
{
    public void Configure(EntityTypeBuilder<ListingOffer> builder)
    {
        builder.ToTable(
            "listing_offers",
            Schemas.Listings,
            table => table.HasCheckConstraint(
                // Bidding on your own listing is the cheapest way to fake demand. The aggregate
                // refuses it; the constraint means a bulk insert cannot.
                "ck_listing_offers_distinct_parties",
                "buyer_party_id <> seller_party_id"));

        builder.HasKey(offer => offer.Id);

        builder.Property(offer => offer.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();

        builder.HasOne<Listing>()
            .WithMany()
            .HasForeignKey(offer => offer.ListingId)
            .OnDelete(AggregateConfiguration.WhenPrincipalIsSoftDeleted);

        builder.HasMany(offer => offer.Turns)
            .WithOne()
            .HasForeignKey(turn => turn.OfferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(ListingOffer.Turns));

        // The seller's inbox and the "is this buyer already negotiating?" check, which is what stops
        // two open chains on one listing producing two prices a seller can accept.
        builder.HasIndex(offer => new { offer.ListingId, offer.BuyerPartyId });

        builder.HasIndex(offer => new { offer.SellerPartyId, offer.Id });
    }
}

internal sealed class OfferTurnConfiguration : IEntityTypeConfiguration<OfferTurn>
{
    public void Configure(EntityTypeBuilder<OfferTurn> builder)
    {
        builder.ToTable(
            "listing_offer_turns",
            Schemas.Listings,
            table => table.HasCheckConstraint(
                "ck_listing_offer_turns_amount_positive",
                "amount_minor_units > 0"));

        builder.HasKey(turn => turn.Id);

        builder.Property(turn => turn.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(turn => turn.Message).HasMaxLength(1000);

        // The chain is append-only and its order is the record of who moved when. A duplicate
        // sequence number is a concurrent double-submit, and rejecting it at the database is what
        // stops two counters at the same position from making the history ambiguous.
        builder.HasIndex(turn => new { turn.OfferId, turn.Sequence }).IsUnique();
    }
}
