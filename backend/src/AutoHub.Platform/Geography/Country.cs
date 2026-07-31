using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Geography;

/// <summary>
/// A country. There is one today.
/// </summary>
/// <remarks>
/// AutoHub is India-only, and this row exists anyway for the same reason
/// <see cref="SharedKernel.Money.Currency"/> exists: the roadmap has vehicle export on it, and
/// retrofitting a country above a states table that assumed one is a migration through every
/// address, every RTO office and every service area on the platform. One row and one foreign key
/// now costs nothing; adding it later costs a weekend.
/// </remarks>
public sealed class Country : AggregateRoot, IAuditable, ISoftDeletable
{
    private Country(Guid id, string code, string name, string dialCode, string currencyCode) : base(id)
    {
        Code = code;
        Name = name;
        DialCode = dialCode;
        CurrencyCode = currencyCode;
        IsActive = true;
    }

    private Country() { }

    /// <summary>ISO 3166-1 alpha-2, uppercase — <c>IN</c>.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>E.164 prefix including the plus — <c>+91</c>. India is phone-first; this is load-bearing.</summary>
    public string DialCode { get; private set; } = string.Empty;

    /// <summary>ISO 4217 code. Resolved against <see cref="SharedKernel.Money.Currency"/> at the edge.</summary>
    public string CurrencyCode { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public static Result<Country> Create(string code, string name, string dialCode, string currencyCode)
    {
        var validCode = GeographyCode.ValidateState(code, "geo.country.code_invalid");
        if (validCode.IsFailure)
        {
            return Result.Failure<Country>(validCode.Error);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("geo.country.name_required", "A country needs a name.");
        }

        if (string.IsNullOrWhiteSpace(dialCode) || !dialCode.StartsWith('+'))
        {
            return Error.Validation("geo.country.dial_code_invalid", "A dial code starts with '+', e.g. +91.");
        }

        return new Country(UuidV7.New(), validCode.Value, name.Trim(), dialCode.Trim(), currencyCode.Trim());
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
