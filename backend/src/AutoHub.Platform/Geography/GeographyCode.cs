using System.Text.RegularExpressions;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Geography;

/// <summary>
/// The code shapes Indian geography actually uses.
/// </summary>
/// <remarks>
/// Four different shapes, because the real world has four. A state's RTO code is two uppercase
/// letters stamped on a number plate (<c>MH</c>); an RTO office code adds the district number
/// (<c>MH12</c>); a pincode is six digits that never start with zero; a city code is a URL slug
/// (<c>pune</c>) because it appears in <c>?city=pune</c> and in <c>X-Active-City</c>.
/// <para>
/// The casing is not cosmetic and is not normalised away. <c>MH</c> is uppercase because that is how
/// it is printed on every plate, every RC and every VAHAN response, and lowercasing it would mean
/// converting back at three boundaries. <c>pune</c> is lowercase because it is a URL. Each is
/// validated in the form it will be stored and compared in; lookups forgive case, storage does not.
/// </para>
/// </remarks>
internal static partial class GeographyCode
{
    [GeneratedRegex("^[A-Z]{2}$")]
    private static partial Regex StatePattern { get; }

    // Delhi has RTOs numbered up to 13, Maharashtra past 50; two digits is the ceiling in practice
    // and single-digit codes (DL1) are written without a leading zero on the plate.
    [GeneratedRegex(@"^[A-Z]{2}[0-9]{1,2}$")]
    private static partial Regex RtoPattern { get; }

    // India Post never issues a pincode beginning with zero; the first digit is the postal region.
    [GeneratedRegex("^[1-9][0-9]{5}$")]
    private static partial Regex PincodePattern { get; }

    [GeneratedRegex("^[a-z0-9][a-z0-9_-]{1,63}$")]
    private static partial Regex SlugPattern { get; }

    public static Result<string> ValidateState(string? code, string errorCode)
        => Check(StatePattern, code, errorCode, "A state code is two uppercase letters, e.g. MH.");

    public static Result<string> ValidateRto(string? code, string errorCode)
        => Check(RtoPattern, code, errorCode, "An RTO code is a state code and a district number, e.g. MH12.");

    public static Result<string> ValidatePincode(string? code, string errorCode)
        => Check(PincodePattern, code, errorCode, "A pincode is six digits and does not start with zero.");

    public static Result<string> ValidateSlug(string? code, string errorCode)
        => Check(SlugPattern, code, errorCode,
            "A code must be lowercase letters, digits, hyphens or underscores, 2–64 characters.");

    private static Result<string> Check(Regex pattern, string? code, string errorCode, string message)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Error.Validation(errorCode, "A code is required.");
        }

        var trimmed = code.Trim();

        return pattern.IsMatch(trimmed)
            ? trimmed
            : Error.Validation(errorCode, message).With("code", code);
    }
}
