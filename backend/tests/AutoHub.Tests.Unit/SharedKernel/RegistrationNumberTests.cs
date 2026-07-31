using AutoHub.SharedKernel.Vehicles;

namespace AutoHub.Tests.Unit.SharedKernel;

public class RegistrationNumberTests
{
    [Theory]
    [InlineData("MH12AB1234", "MH12AB1234")]
    [InlineData("mh 12 ab 1234", "MH12AB1234")] // user typing
    [InlineData("MH-12-AB-1234", "MH12AB1234")] // dealer bulk upload
    [InlineData("  MH12 AB1234 ", "MH12AB1234")] // RC OCR output
    public void Canonicalises_every_way_the_same_plate_arrives(string input, string expected)
    {
        var result = RegistrationNumber.TryParse(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Canonical.Should().Be(expected);
    }

    [Theory]
    [InlineData("MH12AB1234", "MH", "12", "AB", "1234")]
    [InlineData("DL8CAF5031", "DL", "8", "CAF", "5031")] // Delhi carries a vehicle-class letter
    [InlineData("KA01MA9999", "KA", "01", "MA", "9999")]
    [InlineData("TN10K1234", "TN", "10", "K", "1234")]
    public void Splits_a_state_series_into_its_parts(
        string input, string state, string rto, string series, string number)
    {
        var plate = RegistrationNumber.TryParse(input).Value;

        plate.Format.Should().Be(RegistrationFormat.StateSeries);
        plate.StateOrYearCode.Should().Be(state);
        plate.RtoOrSeriesCode.Should().Be(rto);
        plate.Series.Should().Be(series);
        plate.Number.Should().Be(number);
    }

    [Fact]
    public void Recognises_the_Bharat_series_before_the_state_pattern_can_claim_it()
    {
        // "24BH1234AA" is structurally close enough to a state series to be mis-parsed, which would
        // file a BH-series vehicle under a state that does not exist.
        var plate = RegistrationNumber.TryParse("24BH1234AA").Value;

        plate.Format.Should().Be(RegistrationFormat.BharatSeries);
        plate.StateOrYearCode.Should().Be("24");
        plate.RtoOrSeriesCode.Should().Be("BH");
        plate.Number.Should().Be("1234");
        plate.Series.Should().Be("AA");
    }

    [Theory]
    [InlineData("MH12AB1234", "MH 12 AB 1234")]
    [InlineData("DL8CAF5031", "DL 8 CAF 5031")]
    [InlineData("24BH1234AA", "24 BH 1234 AA")]
    public void Groups_for_display_the_way_a_physical_plate_is_grouped(string input, string expected)
    {
        RegistrationNumber.TryParse(input).Value.ToPlateDisplay().Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_input_asks_for_the_number_rather_than_calling_it_malformed(string? input)
    {
        var result = RegistrationNumber.TryParse(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("registration.required");
    }

    [Theory]
    [InlineData("ABCD")]
    [InlineData("1234567890123")]
    [InlineData("MH12AB12345")]  // five digits — no Indian series goes that far
    [InlineData("M12AB1234")]    // one-letter state
    public void Rejects_structurally_impossible_numbers(string input)
    {
        var result = RegistrationNumber.TryParse(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("registration.malformed");
    }

    [Fact]
    public void Does_not_pass_judgement_on_whether_the_state_code_is_real()
    {
        // Structure is validated here; meaning lives in the geography registry. "ZZ" is structurally
        // a plate — deciding it is not a real state is an operations question, not a compile-time one.
        RegistrationNumber.TryParse("ZZ99XX0001").IsSuccess.Should().BeTrue();
    }
}
