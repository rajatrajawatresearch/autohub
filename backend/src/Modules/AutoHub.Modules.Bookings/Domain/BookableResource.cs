using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Bookings.Domain;

/// <summary>
/// Anything whose time can be booked: a car, an inspector, a delivery van, a yard bay, an agent.
/// </summary>
/// <remarks>
/// <b>Why one resource type and not five.</b> A test drive books a car and a salesperson. An
/// inspection books an inspector and a bay. A home visit books an inspector and a vehicle to get
/// there in. A rental handover books a car, a bay and a person. Modelled as separate calendars per
/// kind, every one of those needs bespoke cross-checking code, and the sixth purpose — auction
/// preview, which books a yard and a marshal — needs it written again.
/// <para>
/// Modelled as one type with a <see cref="ResourceTypeCode"/> from the taxonomy registry, adding
/// "charging bay" is a row. Nothing in this module branches on the type; the behaviour that actually
/// differs is <see cref="Capacity"/>, <see cref="TravelBufferMinutes"/> and the calendar, and all
/// three are numbers on the row.
/// </para>
/// </remarks>
public sealed class BookableResource : AggregateRoot, IAuditable, ISoftDeletable
{
    private readonly List<CalendarWindow> _windows = [];
    private readonly List<CalendarException> _exceptions = [];

    private BookableResource(Guid id, string resourceTypeCode, string name, int capacity)
        : base(id)
    {
        ResourceTypeCode = resourceTypeCode;
        Name = name;
        Capacity = capacity;
    }

    private BookableResource() { }

    /// <summary>Taxonomy code: <c>vehicle</c>, <c>inspector</c>, <c>bay</c>, <c>agent</c>, <c>van</c>.</summary>
    public string ResourceTypeCode { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>Who runs this resource — a dealer, a branch, the platform. Null for platform-owned.</summary>
    public Guid? OwnerId { get; private set; }

    public string? OwnerType { get; private set; }

    public string? CityCode { get; private set; }

    /// <summary>
    /// How many bookings may overlap.
    /// </summary>
    /// <remarks>
    /// One for an inspector, who cannot be in two places. Four for a test-drive centre with four
    /// identical demo cars. Modelling capacity rather than creating four resources means the
    /// availability query answers "is there a slot" once instead of four times, and a fifth car is a
    /// number change rather than a new row with its own calendar to keep in step.
    /// </remarks>
    public int Capacity { get; private set; } = 1;

    /// <summary>
    /// Dead time either side of a booking that leaves this resource's base.
    /// </summary>
    /// <remarks>
    /// An inspector doing a home visit in Kothrud at 11:00 cannot start another in Hinjawadi at
    /// 12:00, and a calendar that ignores travel produces a schedule that looks full and is
    /// impossible. The buffer is deliberately crude — a flat per-resource number rather than a
    /// distance matrix — because a wrong-by-fifteen-minutes buffer still prevents the failure, and a
    /// routing integration on the availability path would put a third-party API in front of every
    /// slot list.
    /// </remarks>
    public int TravelBufferMinutes { get; private set; }

    /// <summary>Shortest bookable unit. Slots are generated on this grid.</summary>
    public int SlotMinutes { get; private set; } = 30;

    /// <summary>How far ahead this resource can be booked. Stops a calendar being filled to 2030.</summary>
    public int BookingHorizonDays { get; private set; } = 30;

    /// <summary>How soon a booking may be made. An inspector needs warning; a test drive may not.</summary>
    public int MinimumNoticeMinutes { get; private set; }

    public bool IsActive { get; private set; } = true;

    /// <summary>The recurring weekly opening hours.</summary>
    public IReadOnlyList<CalendarWindow> Windows => _windows;

    /// <summary>Holidays, leave, one-off extended hours. Overrides the weekly pattern for a date.</summary>
    public IReadOnlyList<CalendarException> Exceptions => _exceptions;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public static Result<BookableResource> Create(ResourceSpecification specification)
    {
        if (specification.Capacity < 1)
        {
            return Error.Validation(
                "booking.resource.capacity_invalid",
                "A resource must be able to take at least one booking.");
        }

        if (specification.SlotMinutes < 5)
        {
            return Error.Validation(
                "booking.resource.slot_too_short",
                "A slot shorter than five minutes is not a slot anyone can attend.");
        }

        return new BookableResource(
            UuidV7.New(),
            specification.ResourceTypeCode,
            specification.Name,
            specification.Capacity)
        {
            OwnerId = specification.OwnerId,
            OwnerType = specification.OwnerType,
            CityCode = specification.CityCode,
            TravelBufferMinutes = specification.TravelBufferMinutes,
            SlotMinutes = specification.SlotMinutes,
            BookingHorizonDays = specification.BookingHorizonDays,
            MinimumNoticeMinutes = specification.MinimumNoticeMinutes,
        };
    }

    public void AddWindow(CalendarWindow window) => _windows.Add(window);

    public void AddException(CalendarException exception) => _exceptions.Add(exception);

    /// <summary>
    /// The opening windows that apply on one date, exceptions taken into account.
    /// </summary>
    /// <remarks>
    /// An exception wins outright rather than merging with the weekly pattern. Merging is what
    /// produces a resource that is closed for Diwali and somehow still bookable from 09:00 to 09:30
    /// because a window survived the override.
    /// </remarks>
    public IReadOnlyList<CalendarWindow> WindowsOn(DateOnly date)
    {
        var exception = _exceptions.Find(e => e.Date == date);

        if (exception is not null)
        {
            return exception.IsClosed
                ? []
                : [new CalendarWindow(date.DayOfWeek, exception.OpensAt!.Value, exception.ClosesAt!.Value)];
        }

        return _windows.Where(w => w.DayOfWeek == date.DayOfWeek).ToList();
    }
}

/// <summary>One recurring opening window in a week.</summary>
public sealed class CalendarWindow
{
    public CalendarWindow(DayOfWeek dayOfWeek, TimeOnly opensAt, TimeOnly closesAt)
    {
        DayOfWeek = dayOfWeek;
        OpensAt = opensAt;
        ClosesAt = closesAt;
    }

    private CalendarWindow() { }

    public Guid Id { get; private set; } = UuidV7.New();

    public Guid BookableResourceId { get; private set; }

    public DayOfWeek DayOfWeek { get; private set; }

    /// <summary>
    /// Local wall-clock time, in IST.
    /// </summary>
    /// <remarks>
    /// Opening hours are a wall-clock fact — a workshop opens at nine, not at 03:30 UTC — so they are
    /// stored as a time and combined with a date at query time. India has one time zone and no
    /// daylight saving, which is what makes this simple here and would not make it simple elsewhere.
    /// </remarks>
    public TimeOnly OpensAt { get; private set; }

    public TimeOnly ClosesAt { get; private set; }
}

/// <summary>A date that does not follow the weekly pattern: a holiday, leave, or extended hours.</summary>
public sealed class CalendarException
{
    public CalendarException(DateOnly date, bool isClosed, TimeOnly? opensAt = null, TimeOnly? closesAt = null, string? reason = null)
    {
        Date = date;
        IsClosed = isClosed;
        OpensAt = opensAt;
        ClosesAt = closesAt;
        Reason = reason;
    }

    private CalendarException() { }

    public Guid Id { get; private set; } = UuidV7.New();

    public Guid BookableResourceId { get; private set; }

    public DateOnly Date { get; private set; }

    public bool IsClosed { get; private set; }

    public TimeOnly? OpensAt { get; private set; }

    public TimeOnly? ClosesAt { get; private set; }

    public string? Reason { get; private set; }
}

public sealed record ResourceSpecification
{
    public required string ResourceTypeCode { get; init; }

    public required string Name { get; init; }

    public int Capacity { get; init; } = 1;

    public Guid? OwnerId { get; init; }

    public string? OwnerType { get; init; }

    public string? CityCode { get; init; }

    public int TravelBufferMinutes { get; init; }

    public int SlotMinutes { get; init; } = 30;

    public int BookingHorizonDays { get; init; } = 30;

    public int MinimumNoticeMinutes { get; init; }
}
