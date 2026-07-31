using System.Text.Json.Nodes;
using AutoHub.Platform.Taxonomy;

namespace AutoHub.Tests.Unit.Platform;

/// <summary>
/// Attribute inheritance is the mechanism the whole "no deploy for a new vehicle type" claim rests
/// on, and it fails silently — a child that quietly loses its parent's <c>fuel_type</c> produces a
/// sell form with a missing field rather than an exception.
/// </summary>
public class TaxonomySnapshotTests
{
    [Fact]
    public void A_child_category_inherits_its_parents_attributes()
    {
        var fixture = Fixture.Build();

        var carAttributes = fixture.Snapshot.EffectiveAttributesOf(fixture.Car);
        var suvAttributes = fixture.Snapshot.EffectiveAttributesOf(fixture.Suv);

        carAttributes.Select(a => a.Code).Should().Contain("fuel_type");

        // The SUV declares nothing of its own beyond an override; everything else arrives from car.
        suvAttributes.Select(a => a.Code).Should().Contain(["fuel_type", "kms_driven"]);
    }

    [Fact]
    public void The_nearest_ancestor_wins_when_a_child_overrides()
    {
        var fixture = Fixture.Build();

        var onCar = fixture.Snapshot.EffectiveAttributesOf(fixture.Car).Single(a => a.Code == "fuel_type");
        var onSuv = fixture.Snapshot.EffectiveAttributesOf(fixture.Suv).Single(a => a.Code == "fuel_type");

        onCar.FacetStyle.Should().Be(FacetStyle.List);
        onSuv.FacetStyle.Should().Be(FacetStyle.Chips);
        onSuv.InheritedFromCategoryId.Should().Be(fixture.Suv.Id);
    }

    [Fact]
    public void A_child_can_hide_an_attribute_it_would_otherwise_inherit()
    {
        var fixture = Fixture.Build();

        var truckAttributes = fixture.Snapshot.EffectiveAttributesOf(fixture.Truck);

        // Commercial attaches kms_driven; truck hides it. A sibling of truck would still see it.
        truckAttributes.Select(a => a.Code).Should().NotContain("kms_driven");
        truckAttributes.Select(a => a.Code).Should().Contain("payload_kg");
    }

    [Fact]
    public void A_cycle_in_the_tree_does_not_hang_the_ancestry_walk()
    {
        // Two categories parented to each other — one bad admin re-parent away, and an unguarded
        // walk would spin a request thread forever rather than fail.
        var left = VehicleCategory.Create("left", "Left").Value;
        var right = VehicleCategory.Create("right", "Right", left.Id).Value;
        left.MoveTo(right.Id);

        var snapshot = new TaxonomySnapshot([left, right], [], [], [], []);

        snapshot.AncestryOf(left).Should().HaveCountLessThanOrEqualTo(16);
        snapshot.SubtreeIdsOf(left).Should().Contain(right.Id);
    }

    [Fact]
    public void A_subtree_includes_every_descendant_so_a_parent_filter_is_not_empty()
    {
        var fixture = Fixture.Build();

        fixture.Snapshot.SubtreeIdsOf(fixture.Car)
            .Should().Contain([fixture.Car.Id, fixture.Suv.Id]);
    }

    private sealed record Fixture(
        TaxonomySnapshot Snapshot,
        VehicleCategory Car,
        VehicleCategory Suv,
        VehicleCategory Commercial,
        VehicleCategory Truck)
    {
        public static Fixture Build()
        {
            var car = VehicleCategory.Create("car", "Car", icon: "car", sortOrder: 1).Value;
            var suv = VehicleCategory.Create("suv", "SUV", car.Id, sortOrder: 2).Value;
            var commercial = VehicleCategory.Create("commercial", "Commercial", sortOrder: 5).Value;
            var truck = VehicleCategory.Create("truck", "Truck", commercial.Id).Value;

            var fuel = AttributeDefinition.Create("fuel_type", "Fuel", AttributeDataType.Enum).Value;
            fuel.SetPresentation(null, "engine", 10, isRequired: true, isSearchable: true, FacetStyle.List);
            fuel.AddOption("petrol", "Petrol", 1);
            fuel.AddOption("diesel", "Diesel", 2);

            var kms = AttributeDefinition.Create("kms_driven", "Kms driven", AttributeDataType.Integer).Value;
            kms.SetPresentation("km", "usage", 20, isRequired: true, isSearchable: true, FacetStyle.Range);
            kms.SetNumericBounds(0, 500_000, 1_000);

            var payload = AttributeDefinition.Create("payload_kg", "Payload", AttributeDataType.Integer).Value;
            payload.SetPresentation("kg", "capacity", 30, isRequired: false, isSearchable: true, FacetStyle.Range);

            var suvOverride = CategoryAttribute.Attach(suv.Id, fuel.Id);
            suvOverride.Override(facetStyle: FacetStyle.Chips);

            List<CategoryAttribute> links =
            [
                CategoryAttribute.Attach(car.Id, fuel.Id),
                CategoryAttribute.Attach(car.Id, kms.Id),
                suvOverride,
                CategoryAttribute.Attach(commercial.Id, fuel.Id),
                CategoryAttribute.Attach(commercial.Id, kms.Id),
                CategoryAttribute.Attach(commercial.Id, payload.Id),
                CategoryAttribute.Hide(truck.Id, kms.Id),
            ];

            var snapshot = new TaxonomySnapshot(
                [car, suv, commercial, truck],
                [fuel, kms, payload],
                links,
                [],
                []);

            return new Fixture(snapshot, car, suv, commercial, truck);
        }
    }
}

/// <summary>
/// The validator is the only thing standing between a ten-thousand-row dealer upload and a corpus
/// of listings the sell form would have refused.
/// </summary>
public class AttributeValueValidatorTests
{
    [Fact]
    public void An_integer_outside_its_bounds_is_refused_with_a_stable_code()
    {
        var attribute = Integer("kms_driven", "Kms driven", min: 0, max: 500_000, step: null);

        var result = AttributeValueValidator.Validate(attribute, JsonValue.Create(900_000));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("kms_driven.too_high");
    }

    [Fact]
    public void A_number_arriving_as_a_quoted_string_is_still_a_number()
    {
        // CSV uploads and clients that quote every field are the norm, not the exception.
        var attribute = Integer("kms_driven", "Kms driven", min: 0, max: 500_000, step: null);

        var result = AttributeValueValidator.Validate(attribute, JsonValue.Create("38400"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Number.Should().Be(38_400m);
    }

    [Fact]
    public void A_value_off_the_step_grid_is_refused_relative_to_the_floor_not_to_zero()
    {
        var attribute = Integer("kms_driven", "Kms driven", min: 1_000, max: 500_000, step: 1_000);

        AttributeValueValidator.Validate(attribute, JsonValue.Create(1_000)).IsSuccess.Should().BeTrue();
        AttributeValueValidator.Validate(attribute, JsonValue.Create(1_500)).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void A_value_that_is_not_an_option_is_refused()
    {
        var result = AttributeValueValidator.Validate(Fuel(), JsonValue.Create("hydrogen"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("fuel_type.not_an_option");
    }

    [Fact]
    public void Money_is_read_as_minor_units_never_as_rupees()
    {
        var definition = AttributeDefinition.Create("idv", "Insured value", AttributeDataType.Money).Value;
        var attribute = Effective(definition, required: false);

        var result = AttributeValueValidator.Validate(attribute, JsonValue.Create(54_900_000L));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount!.Value.MinorUnits.Should().Be(54_900_000);
        result.Value.Amount.Value.Currency.Code.Should().Be("INR");
    }

    [Fact]
    public void A_missing_required_value_fails_and_a_missing_optional_one_does_not()
    {
        AttributeValueValidator.Validate(Effective(Fuel().Definition, required: true), null)
            .IsFailure.Should().BeTrue();

        var optional = AttributeValueValidator.Validate(Effective(Fuel().Definition, required: false), null);
        optional.IsSuccess.Should().BeTrue();
        optional.Value.Should().BeNull();
    }

    [Fact]
    public void Every_field_error_is_collected_not_just_the_first()
    {
        // A dealer fixing a four-hundred-row spreadsheet one error per round trip is the failure
        // mode this exists to prevent.
        List<EffectiveAttribute> attributes =
        [
            Integer("kms_driven", "Kms driven", 0, 500_000, null),
            Fuel(),
        ];

        var payload = new JsonObject
        {
            ["kms_driven"] = 900_000,
            ["fuel_type"] = "hydrogen",
        };

        var result = AttributeValueValidator.ValidateAll(attributes, payload);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("attribute.values_invalid");

        var fieldErrors = result.Error.Metadata["fieldErrors"].Should()
            .BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;

        fieldErrors.Should().ContainKeys("kms_driven", "fuel_type");
    }

    [Fact]
    public void A_catastrophically_backtracking_pattern_does_not_hang_the_thread()
    {
        // Patterns are authored in the admin panel, which makes them untrusted input to the regex
        // engine. Without a match timeout this call never returns.
        var definition = AttributeDefinition.Create("vin", "VIN", AttributeDataType.String).Value;
        definition.SetTextBounds(null, null, "^(a+)+$");

        var result = AttributeValueValidator.Validate(
            Effective(definition, required: false),
            JsonValue.Create(new string('a', 40) + "!"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("vin.malformed");
    }

    [Fact]
    public void A_typed_value_round_trips_through_its_storage_row()
    {
        var definition = AttributeDefinition.Create("kms_driven", "Kms driven", AttributeDataType.Integer).Value;

        var row = EntityAttributeValue.Create(
            "catalog.vehicle", Guid.NewGuid(), definition.Id, TypedAttributeValue.OfInteger(38_400));

        row.ValueNumber.Should().Be(38_400m);
        row.ValueText.Should().BeNull();

        // Rewriting with a different type must clear the old column, or a stale value_number keeps
        // showing up in range facets long after the attribute stopped being a number.
        var enumDefinition = AttributeDefinition.Create("kms_driven", "Kms", AttributeDataType.Enum).Value;
        enumDefinition.AddOption("high", "High");
        row.Replace(TypedAttributeValue.OfEnum("high"));

        row.ValueNumber.Should().BeNull();
        row.Read(enumDefinition).Value.Text.Should().Be("high");
    }

    private static EffectiveAttribute Fuel()
    {
        var definition = AttributeDefinition.Create("fuel_type", "Fuel", AttributeDataType.Enum).Value;
        definition.AddOption("petrol", "Petrol", 1);
        definition.AddOption("cng", "CNG", 2);
        return Effective(definition, required: true);
    }

    private static EffectiveAttribute Integer(string code, string label, decimal? min, decimal? max, decimal? step)
    {
        var definition = AttributeDefinition.Create(code, label, AttributeDataType.Integer).Value;
        definition.SetNumericBounds(min, max, step);
        return Effective(definition, required: true);
    }

    private static EffectiveAttribute Effective(AttributeDefinition definition, bool required)
        => new(definition, required, false, FacetStyle.List, null, 0, Guid.Empty);
}
