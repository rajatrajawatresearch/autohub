using System.Text.RegularExpressions;
using AutoHub.SharedKernel.Results;

namespace AutoHub.SharedKernel.Vehicles;

/// <summary>
/// An Indian vehicle registration number, parsed into its parts.
/// </summary>
/// <remarks>
/// This is the single most-typed, most-misread, most-searched string in the product. Users type it
/// as "mh-12 ab 1234", RC OCR returns "MH12AB1234", and a dealer's bulk upload has "MH 12AB1234".
/// All three are the same vehicle, so canonicalisation happens here, once, and everything else —
/// search, duplicate detection, the plate rendered on screen — works from
/// <see cref="Canonical"/>.
/// <para>
/// <b>Structure is validated here; meaning is not.</b> Whether "MH" is a real state and whether
/// RTO 12 exists is a question for the geography registry, which is data an operations team edits
/// when a new RTO opens. Hardcoding a list of state codes into a compiled assembly would make
/// opening an RTO a deployment, which is exactly what this codebase refuses to do.
/// </para>
/// <para>
/// A registration number is <b>not</b> a permanent identifier. Vehicles are re-registered on
/// interstate transfer, and a released number can be reissued. Uniqueness therefore lives on
/// <c>vehicle_identities</c> with a validity period, not on this value object.
/// </para>
/// </remarks>
public readonly partial record struct RegistrationNumber
{
    private RegistrationNumber(
        string canonical,
        RegistrationFormat format,
        string stateOrYearCode,
        string rtoOrSeriesCode,
        string series,
        string number)
    {
        Canonical = canonical;
        Format = format;
        StateOrYearCode = stateOrYearCode;
        RtoOrSeriesCode = rtoOrSeriesCode;
        Series = series;
        Number = number;
    }

    /// <summary>Uppercase, separators removed. The form stored and indexed. e.g. <c>MH12AB1234</c>.</summary>
    public string Canonical { get; }

    public RegistrationFormat Format { get; }

    /// <summary>State code for a state series (<c>MH</c>); the two-digit year for a BH series (<c>24</c>).</summary>
    public string StateOrYearCode { get; }

    /// <summary>RTO district number for a state series (<c>12</c>); the literal <c>BH</c> for a BH series.</summary>
    public string RtoOrSeriesCode { get; }

    /// <summary>Letter series (<c>AB</c>). Empty on the oldest single-series registrations.</summary>
    public string Series { get; }

    /// <summary>The numeric part, as written — leading zeros are significant on a plate.</summary>
    public string Number { get; }

    // MH 12 AB 1234 — state, RTO district, optional series (Delhi uses three: e.g. DL 8C AF)
    [GeneratedRegex(@"^(?<state>[A-Z]{2})(?<rto>\d{1,2})(?<series>[A-Z]{0,3})(?<number>\d{1,4})$")]
    private static partial Regex StateSeriesPattern { get; }

    // 24 BH 1234 AA — the Bharat series for vehicles that move between states without re-registration
    [GeneratedRegex(@"^(?<year>\d{2})BH(?<number>\d{4})(?<series>[A-Z]{1,2})$")]
    private static partial Regex BharatSeriesPattern { get; }

    [GeneratedRegex(@"[^A-Za-z0-9]")]
    private static partial Regex SeparatorPattern { get; }

    public static Result<RegistrationNumber> TryParse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Error.Validation(
                "registration.required",
                "Enter the registration number shown on the number plate.");
        }

        var canonical = SeparatorPattern.Replace(input, string.Empty).ToUpperInvariant();

        if (canonical.Length is < 5 or > 11)
        {
            return Error.Validation(
                "registration.malformed",
                "That does not look like an Indian registration number. Example: MH12AB1234.")
                .With("input", input);
        }

        // BH is checked first: "24BH1234AA" would otherwise be mis-read by the state pattern.
        var bharat = BharatSeriesPattern.Match(canonical);
        if (bharat.Success)
        {
            return new RegistrationNumber(
                canonical,
                RegistrationFormat.BharatSeries,
                bharat.Groups["year"].Value,
                "BH",
                bharat.Groups["series"].Value,
                bharat.Groups["number"].Value);
        }

        var state = StateSeriesPattern.Match(canonical);
        if (state.Success)
        {
            return new RegistrationNumber(
                canonical,
                RegistrationFormat.StateSeries,
                state.Groups["state"].Value,
                state.Groups["rto"].Value,
                state.Groups["series"].Value,
                state.Groups["number"].Value);
        }

        return Error.Validation(
            "registration.malformed",
            "That does not look like an Indian registration number. Example: MH12AB1234.")
            .With("input", input);
    }

    /// <summary>
    /// Spaced the way the number is grouped on a physical plate: <c>MH 12 AB 1234</c>.
    /// </summary>
    /// <remarks>
    /// Grouping is not decoration — it is how people proofread a plate they are copying from a
    /// windscreen, and ungrouped it is measurably easier to transpose two digits.
    /// </remarks>
    public string ToPlateDisplay() => Format switch
    {
        RegistrationFormat.BharatSeries =>
            string.Join(' ', StateOrYearCode, "BH", Number, Series),
        _ when Series.Length == 0 =>
            string.Join(' ', StateOrYearCode, RtoOrSeriesCode, Number),
        _ =>
            string.Join(' ', StateOrYearCode, RtoOrSeriesCode, Series, Number),
    };

    public override string ToString() => Canonical;
}

public enum RegistrationFormat
{
    /// <summary>State + RTO district series, e.g. <c>MH12AB1234</c>. The overwhelming majority.</summary>
    StateSeries = 1,

    /// <summary>Bharat series, e.g. <c>24BH1234AA</c>. No re-registration on interstate transfer.</summary>
    BharatSeries = 2,
}
