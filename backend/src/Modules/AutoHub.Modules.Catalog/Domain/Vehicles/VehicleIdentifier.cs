using System.Text.RegularExpressions;
using AutoHub.Modules.Catalog.Contracts;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Vehicles;

namespace AutoHub.Modules.Catalog.Domain.Vehicles;

/// <summary>
/// A typed, canonicalised vehicle identifier — the value deduplication actually compares.
/// </summary>
/// <remarks>
/// Every one of these arrives as a human-typed or OCR'd string, and every one of them arrives in
/// several spellings of the same thing: <c>mh-12 ab 1234</c>, <c>MA3EYD1S100123456</c> with a
/// transposed letter, a chassis number a dealer pasted with a trailing space. Canonicalisation
/// happens here, once, so that "is this the same vehicle?" is a string equality and not a fuzzy
/// judgement made differently by the sell form, the bulk importer and the auction lot builder.
/// <para>
/// <b>Structure is validated; existence is not.</b> Whether a VIN's world-manufacturer prefix is a
/// real manufacturer is a question for a data provider, and whether an RTO exists is a question for
/// the geography registry. Both are data that changes without a deploy, and neither belongs in a
/// compiled regex.
/// </para>
/// </remarks>
public readonly partial record struct VehicleIdentifier
{
    private VehicleIdentifier(VehicleIdentifierType type, string value)
    {
        Type = type;
        Value = value;
    }

    public VehicleIdentifierType Type { get; }

    /// <summary>Uppercase, separators removed. The form stored, indexed and compared.</summary>
    public string Value { get; }

    /// <summary>
    /// ISO 3779 excludes I, O and Q from a VIN precisely because they are misread as 1, 0 and 0.
    /// Accepting them would let a mistyped VIN look like a new vehicle and create a duplicate.
    /// </summary>
    [GeneratedRegex("^[A-HJ-NPR-Z0-9]{17}$")]
    private static partial Regex VinPattern { get; }

    /// <summary>
    /// Indian chassis numbers as printed on an RC run from the VIN's last six characters up to the
    /// full seventeen, depending on the manufacturer and the year. The bound is deliberately loose
    /// because refusing a real RC is a seller who cannot list their car.
    /// </summary>
    [GeneratedRegex("^[A-Z0-9]{6,25}$")]
    private static partial Regex ChassisPattern { get; }

    [GeneratedRegex("^[A-Z0-9]{5,25}$")]
    private static partial Regex EnginePattern { get; }

    [GeneratedRegex("[^A-Za-z0-9]")]
    private static partial Regex SeparatorPattern { get; }

    public static Result<VehicleIdentifier> TryParse(VehicleIdentifierType type, string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Error.Validation(
                "catalog.identifier.required",
                "An identifier value is required.")
                .With("identifierType", type.ToWireName());
        }

        // Registration has its own value object because its parts carry meaning the others do not —
        // the state and RTO segments resolve to a real office through the geography registry.
        if (type == VehicleIdentifierType.Registration)
        {
            var registration = RegistrationNumber.TryParse(input);

            return registration.IsSuccess
                ? new VehicleIdentifier(type, registration.Value.Canonical)
                : Result.Failure<VehicleIdentifier>(registration.Error);
        }

        var canonical = SeparatorPattern.Replace(input, string.Empty).ToUpperInvariant();

        var matches = type switch
        {
            VehicleIdentifierType.Vin => VinPattern.IsMatch(canonical),
            VehicleIdentifierType.Chassis => ChassisPattern.IsMatch(canonical),
            VehicleIdentifierType.Engine => EnginePattern.IsMatch(canonical),
            _ => false,
        };

        return matches
            ? new VehicleIdentifier(type, canonical)
            : Error.Validation(
                "catalog.identifier.malformed",
                $"That does not look like a {type.ToWireName()} number.")
                .With("identifierType", type.ToWireName())
                .With("input", input);
    }

    public override string ToString() => $"{Type.ToWireName()}:{Value}";
}

/// <summary>
/// The published spelling of <see cref="VehicleIdentifierType"/>.
/// </summary>
/// <remarks>
/// Written down rather than derived from the member names, for the same reason
/// <c>TaxonomyWireNames</c> is: three clients branch on these strings, and a C# rename must not be
/// able to change the wire format without a compile error somewhere.
/// </remarks>
public static class VehicleIdentifierWireNames
{
    public static string ToWireName(this VehicleIdentifierType type) => type switch
    {
        VehicleIdentifierType.Registration => "registration",
        VehicleIdentifierType.Engine => "engine",
        VehicleIdentifierType.Chassis => "chassis",
        VehicleIdentifierType.Vin => "vin",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unmapped identifier type."),
    };

    public static Result<VehicleIdentifierType> Parse(string? wireName) => wireName switch
    {
        "registration" => VehicleIdentifierType.Registration,
        "engine" => VehicleIdentifierType.Engine,
        "chassis" => VehicleIdentifierType.Chassis,
        "vin" => VehicleIdentifierType.Vin,
        _ => Error.Validation(
            "catalog.identifier.type_unknown",
            $"'{wireName}' is not a supported vehicle identifier type.")
            .With("identifierType", wireName),
    };
}
