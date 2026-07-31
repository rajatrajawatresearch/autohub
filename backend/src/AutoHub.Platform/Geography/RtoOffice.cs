using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Platform.Geography;

/// <summary>
/// A Regional Transport Office — <c>MH12</c>, "Pune (Pimpri-Chinchwad)".
/// </summary>
/// <remarks>
/// This is the row the <c>RegistrationNumber</c> value object deliberately refuses to know about.
/// That type validates a plate <i>structurally</i> — two letters, a district number, a series, a
/// number — and stops there, precisely so that opening a new RTO is an insert here rather than a
/// deployment of a compiled list of state codes. This entity is the other half of that split: it
/// turns the structural <c>MH12</c> into a real office, in a real city, in a real state.
/// <para>
/// <see cref="StateId"/> is stored even though <see cref="CityId"/> would imply it, and that is not
/// denormalisation for speed. An RTO's jurisdiction is a district, and several districts have no
/// city in our list at all; mapping every office to a city would either invent cities or drop
/// offices. The state is always known and always right, so it is the required link and the city is
/// the optional one.
/// </para>
/// </remarks>
public sealed class RtoOffice : AggregateRoot, IAuditable, ISoftDeletable
{
    private RtoOffice(Guid id, Guid stateId, string code, string name) : base(id)
    {
        StateId = stateId;
        Code = code;
        Name = name;
        IsActive = true;
    }

    private RtoOffice() { }

    public Guid StateId { get; private set; }

    /// <summary>Nullable on purpose — a district RTO need not sit in a city we serve. See the remarks.</summary>
    public Guid? CityId { get; private set; }

    /// <summary>State code plus district number, uppercase — <c>MH12</c>. Globally unique.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Office name as the RTO itself writes it, e.g. "Pune (Pimpri-Chinchwad)".</summary>
    public string Name { get; private set; } = string.Empty;

    public string? Address { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public static Result<RtoOffice> Create(Guid stateId, string code, string name, Guid? cityId = null)
    {
        var validCode = GeographyCode.ValidateRto(code, "geo.rto.code_invalid");
        if (validCode.IsFailure)
        {
            return Result.Failure<RtoOffice>(validCode.Error);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("geo.rto.name_required", "An RTO office needs a name.");
        }

        return new RtoOffice(UuidV7.New(), stateId, validCode.Value, name.Trim()) { CityId = cityId };
    }

    public void AssignToCity(Guid? cityId) => CityId = cityId;

    public void SetAddress(string? address) => Address = address;

    public void Activate() => IsActive = true;

    /// <summary>
    /// Retires an office without deleting it.
    /// </summary>
    /// <remarks>
    /// RTOs are reorganised and merged, and every vehicle registered under the old code keeps that
    /// code on its plate for the rest of its life. Deleting the row would leave those registrations
    /// unresolvable, which shows up as a valuation that cannot state where a car is from.
    /// </remarks>
    public void Deactivate() => IsActive = false;
}
