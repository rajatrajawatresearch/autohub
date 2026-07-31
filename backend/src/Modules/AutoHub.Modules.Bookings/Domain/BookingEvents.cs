using AutoHub.SharedKernel.Domain;

namespace AutoHub.Modules.Bookings.Domain;

public sealed record BookingRequested(
    Guid BookingId,
    DateTimeOffset At,
    string PurposeCode,
    Guid BookableResourceId,
    Guid CustomerId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt) : DomainEvent(BookingId, At)
{
    public override string EventType => "bookings.booking_requested.v1";
}

public sealed record BookingRescheduled(
    Guid BookingId,
    DateTimeOffset At,
    DateTimeOffset PreviousStartsAt,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt) : DomainEvent(BookingId, At)
{
    public override string EventType => "bookings.booking_rescheduled.v1";
}

/// <summary>
/// The handover code was accepted.
/// </summary>
/// <remarks>
/// The signal the rental and delivery modules wait for. A vehicle handed over without one is a
/// vehicle nobody can prove was collected, which is the fact a dispute turns on three weeks later.
/// </remarks>
public sealed record BookingVerified(
    Guid BookingId,
    DateTimeOffset At,
    Guid CustomerId) : DomainEvent(BookingId, At)
{
    public override string EventType => "bookings.booking_verified.v1";
}

public sealed record BookingCompleted(
    Guid BookingId,
    DateTimeOffset At,
    string PurposeCode,
    string? SubjectType,
    Guid? SubjectId) : DomainEvent(BookingId, At)
{
    public override string EventType => "bookings.booking_completed.v1";
}

/// <summary>
/// A booking ended without happening.
/// </summary>
/// <remarks>
/// One event with a flag rather than two events, because every consumer cares about the same thing —
/// the slot is free and somebody may owe a fee — and only trust scoring cares which it was.
/// </remarks>
public sealed record BookingCancelled(
    Guid BookingId,
    DateTimeOffset At,
    string Reason,
    long? PenaltyMinorUnits,
    string? Currency,
    bool WasNoShow) : DomainEvent(BookingId, At)
{
    public override string EventType => "bookings.booking_cancelled.v1";
}
