using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Geography;

/// <summary>
/// A state or union territory, keyed by the two letters that appear on a number plate.
/// </summary>
/// <remarks>
/// <see cref="Code"/> is the <b>RTO</b> code — <c>MH</c>, <c>KA</c>, <c>DL</c> — not the ISO 3166-2
/// subdivision code (<c>IN-MH</c>) and not a name. It is chosen that way because it is the join key
/// the product actually uses: <c>RegistrationNumber</c> parses it structurally out of every plate a
/// user types, and this registry is what turns those two letters into a real place. Any other key
/// would need a translation table between the plate and the row, which is one more thing to be
/// wrong about Telangana.
/// <para>
/// <see cref="GstStateCode"/> is a different two characters for the same state — the numeric code
/// that opens every GSTIN (<c>27</c> for Maharashtra). It lives here because the invoice engine
/// needs it and the alternative is a second lookup table that has to agree with this one. Getting it
/// wrong means charging IGST where CGST plus SGST was due, which is a filing correction, not a bug
/// report.
/// </para>
/// </remarks>
public sealed class State : AggregateRoot, IAuditable, ISoftDeletable
{
    private State(Guid id, Guid countryId, string code, string name, string? gstStateCode) : base(id)
    {
        CountryId = countryId;
        Code = code;
        Name = name;
        GstStateCode = gstStateCode;
        IsActive = true;
    }

    private State() { }

    public Guid CountryId { get; private set; }

    /// <summary>Two-letter RTO code as printed on a plate — <c>MH</c>, <c>KA</c>, <c>TS</c>.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>Two-digit GST state code — the first two characters of every GSTIN issued here.</summary>
    public string? GstStateCode { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public static Result<State> Create(Guid countryId, string code, string name, string? gstStateCode = null)
    {
        var validCode = GeographyCode.ValidateState(code, "geo.state.code_invalid");
        if (validCode.IsFailure)
        {
            return Result.Failure<State>(validCode.Error);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("geo.state.name_required", "A state needs a name.");
        }

        return new State(UuidV7.New(), countryId, validCode.Value, name.Trim(), gstStateCode?.Trim());
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
