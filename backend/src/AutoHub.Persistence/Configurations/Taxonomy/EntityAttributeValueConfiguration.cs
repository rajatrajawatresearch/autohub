using AutoHub.Platform.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Taxonomy;

/// <summary>
/// The EAV table, and the indexes that make it survive contact with a search page.
/// </summary>
/// <remarks>
/// The entity's own remarks argue why the values are typed columns rather than one JSONB bag. This
/// configuration is where that argument turns into index definitions: a B-tree on
/// <c>(attribute_definition_id, value_number)</c> answers <c>kms_driven BETWEEN 0 AND 40000</c> with
/// one index seek, which is the query every facet on the marketplace runs and the one a GIN index
/// over JSONB cannot answer at all.
/// </remarks>
internal sealed class EntityAttributeValueConfiguration : IEntityTypeConfiguration<EntityAttributeValue>
{
    public void Configure(EntityTypeBuilder<EntityAttributeValue> builder)
    {
        builder.ToTable(
            "entity_attribute_values",
            Schemas.Platform,
            table =>
            {
                // An amount without its currency is a number, and a number is what somebody
                // eventually reads as rupees. The pair is enforced here because `Replace` writes
                // both and nothing else should ever write one.
                table.HasCheckConstraint(
                    "ck_entity_attribute_values_money_pair",
                    "(value_money_minor_units IS NULL) = (value_currency IS NULL)");

                // A row with nothing in it is not "no value" — the absence of a value is the absence
                // of a row. An all-null row is an import bug, and it silently makes a required
                // attribute look answered.
                table.HasCheckConstraint(
                    "ck_entity_attribute_values_has_a_value",
                    "num_nonnulls(value_text, value_number, value_bool, value_date, "
                    + "value_money_minor_units, value_json) >= 1");
            });

        builder.HasKey(value => value.Id);

        builder.Property(value => value.EntityType).HasMaxLength(64).IsRequired();

        builder.Property(value => value.ValueText).AsNullableCaseInsensitive();
        builder.Property(value => value.ValueNumber).HasColumnType("numeric");
        builder.Property(value => value.ValueCurrency).HasMaxLength(3).IsFixedLength();
        builder.Property(value => value.ValueJson).AsNullableJsonb();

        // Never cascaded, because these rows are the vehicle's actual specification. Following a
        // definition's deletion into them would erase a listing's mileage along with the attribute
        // that described it.
        builder.HasOne<AttributeDefinition>()
            .WithMany()
            .HasForeignKey(value => value.AttributeDefinitionId)
            .OnDelete(AggregateConfiguration.WhenPrincipalIsSoftDeleted);

        // One value per attribute per entity. Without this a retried save produces two rows and the
        // reader silently picks whichever the planner returned first.
        builder.HasIndex(value => new { value.EntityType, value.EntityId, value.AttributeDefinitionId })
            .IsUnique();

        // Read path: every attribute of one batch of entities, which is what a search results page
        // and a spec sheet both ask for.
        builder.HasIndex(value => new { value.EntityType, value.EntityId });

        // Facet paths. Each of these is one attribute filtered by one kind of value, which is
        // exactly the shape of `attr.kms_driven=0..40000` and `attr.fuel_type=petrol,cng`.
        builder.HasIndex(value => new { value.AttributeDefinitionId, value.ValueNumber })
            .HasFilter("value_number IS NOT NULL");

        builder.HasIndex(value => new { value.AttributeDefinitionId, value.ValueText })
            .HasFilter("value_text IS NOT NULL");

        builder.HasIndex(value => new { value.AttributeDefinitionId, value.ValueMoneyMinorUnits })
            .HasFilter("value_money_minor_units IS NOT NULL");

        builder.HasIndex(value => new { value.AttributeDefinitionId, value.ValueDate })
            .HasFilter("value_date IS NOT NULL");

        // Multi-enum lives in JSONB because it has no scalar column, and containment
        // (`value_json @> '["cng"]'`) is precisely what GIN is good at.
        builder.HasIndex(value => value.ValueJson)
            .HasMethod("gin")
            .HasFilter("value_json IS NOT NULL");
    }
}
