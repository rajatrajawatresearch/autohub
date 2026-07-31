using System.Text.Json.Nodes;
using AutoHub.Platform.Caching;
using AutoHub.Platform.Geography;
using AutoHub.SharedKernel.Vehicles;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AutoHub.Tests.Unit.Platform;

/// <summary>
/// The split this registry exists for: <c>RegistrationNumber</c> proves a plate is well-formed,
/// and only this turns "MH12" into a real office. Both halves have to hold for a valuation to be
/// able to say where a car is from.
/// </summary>
public class GeographyRegistryTests
{
    [Fact]
    public async Task A_plate_resolves_to_its_RTO_office_city_and_state()
    {
        var registry = Build();
        var plate = RegistrationNumber.TryParse("mh-12 ab 1234").Value;

        var result = await registry.ResolveRtoAsync(plate, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RtoCode.Should().Be("MH12");
        result.Value.RtoName.Should().Be("Pune (Pimpri-Chinchwad)");
        result.Value.State.Code.Should().Be("MH");
        result.Value.State.GstStateCode.Should().Be("27");
        result.Value.City!.Code.Should().Be("pune");
    }

    [Fact]
    public async Task A_leading_zero_on_the_district_number_still_resolves()
    {
        // The plate reads "MH 01 AB 1234" so the parser hands back "01", while the office is keyed
        // "MH1". Neither form is wrong; failing to match would break every Mumbai registration.
        var registry = Build();
        var plate = RegistrationNumber.TryParse("MH01AB1234").Value;

        var result = await registry.ResolveRtoAsync(plate, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RtoCode.Should().Be("MH1");
    }

    [Fact]
    public async Task A_Bharat_series_plate_is_told_it_has_no_RTO_rather_than_not_found()
    {
        // The BH series exists precisely so a vehicle is not tied to one RTO. Reporting "not found"
        // would send the caller hunting for a data gap that does not exist.
        var registry = Build();
        var plate = RegistrationNumber.TryParse("24BH1234AA").Value;

        var result = await registry.ResolveRtoAsync(plate, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("geo.rto.not_applicable");
    }

    [Fact]
    public async Task An_unknown_but_well_formed_RTO_is_a_data_gap_not_a_format_error()
    {
        var registry = Build();
        var plate = RegistrationNumber.TryParse("MH99XX1234").Value;

        var result = await registry.ResolveRtoAsync(plate, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("geo.rto.not_found");
    }

    [Fact]
    public async Task A_pincode_resolves_to_its_city_and_state()
    {
        var registry = Build();

        var result = await registry.ResolveByPincodeAsync("411001", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.City!.Name.Should().Be("Pune");
        result.Value.State.Code.Should().Be("MH");
    }

    [Fact]
    public async Task A_malformed_pincode_and_an_unknown_one_fail_differently()
    {
        var registry = Build();

        (await registry.ResolveByPincodeAsync("41100", CancellationToken.None))
            .Error.Code.Should().Be("geo.pincode.invalid");

        (await registry.ResolveByPincodeAsync("999999", CancellationToken.None))
            .Error.Code.Should().Be("geo.pincode.not_found");
    }

    [Fact]
    public async Task The_module_filter_is_what_stops_a_city_picker_listing_cities_with_no_yard()
    {
        var registry = Build();

        var all = await registry.GetServiceableCitiesAsync(null, CancellationToken.None);
        var auction = await registry.GetServiceableCitiesAsync("auction", CancellationToken.None);

        all.Value.Items.Select(c => c.Code).Should().BeEquivalentTo(["pune", "nashik"]);
        auction.Value.Items.Select(c => c.Code).Should().BeEquivalentTo(["pune"]);
    }

    [Fact]
    public async Task A_radius_area_covers_a_point_inside_it_and_not_one_outside()
    {
        var registry = Build();

        // Pune Station is ~2 km from the centroid used in the fixture; Mumbai is ~120 km.
        var near = await registry.FindCoveringAreasAsync(18.5289, 73.8744, "inspection", CancellationToken.None);
        var far = await registry.FindCoveringAreasAsync(19.0760, 72.8777, "inspection", CancellationToken.None);

        near.Value.Items.Select(a => a.Code).Should().Contain("pune-inspection");
        far.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public void A_polygon_boundary_honours_GeoJSON_longitude_first_ordering()
    {
        // Transposing the pair silently relocates Pune to the Indian Ocean, and because both numbers
        // are plausible latitudes nothing downstream notices.
        var boundary = JsonNode.Parse("""
            { "type": "Polygon",
              "coordinates": [[[73.7, 18.4], [74.0, 18.4], [74.0, 18.7], [73.7, 18.7], [73.7, 18.4]]] }
            """)!;

        var area = ServiceArea.CreatePolygon(Guid.NewGuid(), "pune-delivery", "Pune delivery", "delivery", boundary).Value;

        area.Ring()![0].Should().Be(new GeoPoint(18.4, 73.7));
        area.Covers(18.52, 73.85).Should().BeTrue();
        area.Covers(19.07, 72.87).Should().BeFalse();
    }

    [Fact]
    public void A_malformed_boundary_is_refused_rather_than_silently_covering_nothing()
    {
        var boundary = JsonNode.Parse("""{ "type": "Polygon", "coordinates": [[[73.7, 18.4]]] }""")!;

        var result = ServiceArea.CreatePolygon(Guid.NewGuid(), "bad", "Bad", "delivery", boundary);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("geo.service_area.boundary_degenerate");
    }

    private static GeographyRegistry Build()
    {
        var india = Country.Create("IN", "India", "+91", "INR").Value;

        var maharashtra = State.Create(india.Id, "MH", "Maharashtra", "27").Value;

        var pune = City.Create(maharashtra.Id, "pune", "Pune", 18.5204, 73.8567).Value;
        pune.OpenFor(["buy", "sell", "auction", "inspection"]);

        var nashik = City.Create(maharashtra.Id, "nashik", "Nashik", 19.9975, 73.7898).Value;
        nashik.OpenFor(["buy", "sell"]);

        var mumbaiCentral = RtoOffice.Create(maharashtra.Id, "MH1", "Mumbai (Central)").Value;
        var pimpri = RtoOffice.Create(maharashtra.Id, "MH12", "Pune (Pimpri-Chinchwad)", pune.Id).Value;

        var pincode = Pincode.Create("411001", maharashtra.Id, pune.Id, "Pune").Value;

        var inspection = ServiceArea.CreateRadius(
            pune.Id, "pune-inspection", "Pune inspection", "inspection",
            18.5204, 73.8567, radiusMetres: 40_000).Value;

        var snapshot = new GeographySnapshot(
            [india], [maharashtra], [pune, nashik], [mumbaiCentral, pimpri], [pincode], [inspection]);

        return new GeographyRegistry(
            new FixedStore(snapshot),
            BuildRegistryCache(),
            new MemoryCache(new MemoryCacheOptions()),
            new GeographyRegistryOptions(),
            NullLogger<GeographyRegistry>.Instance);
    }

    private static TwoLevelRegistryCache BuildRegistryCache()
        => new(
            new MemoryCache(new MemoryCacheOptions()),
            new InMemoryRegistryVersionStore(),
            Options.Create(new RegistryCacheOptions { UseDistributedTier = false }),
            NullLogger<TwoLevelRegistryCache>.Instance);

    private sealed class FixedStore(GeographySnapshot snapshot) : IGeographyStore
    {
        public Task<GeographySnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
            => Task.FromResult(snapshot);
    }
}
