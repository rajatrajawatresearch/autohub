using AutoHub.SharedKernel.Domain;
using AutoHub.SharedKernel.Ids;
using AutoHub.SharedKernel.Money;
using AutoHub.SharedKernel.Results;

namespace AutoHub.Modules.Bookings.Domain;

/// <summary>
/// One appointment. The same object for a test drive, an inspection, a delivery and a handover.
/// </summary>
/// <remarks>
/// <b>The claim: these are one thing.</b> A test drive, an inspection, a home visit, a delivery, a
/// pickup, a rental handover, an auction preview and an attachment meeting are all "a customer and a
/// resource meet at a place, at a time, and somebody proves it happened". They differ in
/// <see cref="PurposeCode"/>, in which resource is booked, in how long they take, and in whether the
/// meeting is at the resource or at the customer — four fields.
/// <para>
/// Eight booking tables would mean eight availability queries, eight double-booking guards, eight
/// reschedule flows, eight cancellation-fee paths and eight OTP implementations, of which about six
/// would be subtly wrong. This module has one of each.
/// </para>
/// <para>
/// <b>There is no status column.</b> Requested, confirmed, in progress, completed, no-show,
/// cancelled — the workflow registry's, keyed on this booking's id. An operator adding "awaiting
/// customer confirmation" before "confirmed" for home visits only changes rows.
/// </para>
/// <para>
/// <b><see cref="OccupiedFrom"/> and <see cref="OccupiedTo"/> are the fields the calendar collides
/// on</b>, not <see cref="StartsAt"/> and <see cref="EndsAt"/>. They include the resource's travel
/// buffer, so an inspector's 11:00 home visit blocks 10:30–12:30 rather than 11:00–12:00. Storing the
/// occupied range rather than deriving it means the exclusion constraint in
/// <see cref="BookingSchema"/> can be expressed at all — a constraint cannot call back into another
/// table for a buffer.
/// </para>
/// </remarks>
public sealed class Booking : AggregateRoot, IAuditable, ISoftDeletable
{
    private Booking(Guid id, string purposeCode, Guid resourceId, DateTimeOffset startsAt, DateTimeOffset endsAt)
        : base(id)
    {
        PurposeCode = purposeCode;
        BookableResourceId = resourceId;
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    private Booking() { }

    /// <summary>
    /// Taxonomy code: <c>test_drive</c>, <c>inspection</c>, <c>home_visit</c>, <c>delivery</c>,
    /// <c>pickup</c>, <c>rental_handover</c>, <c>auction_preview</c>, <c>attachment_meeting</c>.
    /// </summary>
    public string PurposeCode { get; private set; } = string.Empty;

    public Guid BookableResourceId { get; private set; }

    /// <summary>What the appointment is about — a listing, an auction lot, a deal.</summary>
    public string? SubjectType { get; private set; }

    public Guid? SubjectId { get; private set; }

    public Guid CustomerId { get; private set; }

    public string? CustomerType { get; private set; }

    public DateTimeOffset StartsAt { get; private set; }

    public DateTimeOffset EndsAt { get; private set; }

    /// <summary>Start minus the travel buffer. What the calendar actually collides on.</summary>
    public DateTimeOffset OccupiedFrom { get; private set; }

    /// <summary>End plus the travel buffer.</summary>
    public DateTimeOffset OccupiedTo { get; private set; }

    /// <summary>
    /// Copied from the resource's capacity at booking time.
    /// </summary>
    /// <remarks>
    /// True when the resource takes one booking at a time, which is what lets the database's
    /// exclusion constraint apply to exactly the rows it can decide. A capacity of four is a counting
    /// rule, and no pairwise constraint can express counting — see <see cref="BookingSchema"/>.
    /// </remarks>
    public bool IsExclusive { get; private set; }

    /// <summary>Taxonomy code: <c>at_resource</c> or <c>at_customer</c>. Decides whether travel applies.</summary>
    public string LocationKindCode { get; private set; } = string.Empty;

    public string? AddressLine { get; private set; }

    public string? Pincode { get; private set; }

    public double? Latitude { get; private set; }

    public double? Longitude { get; private set; }

    /// <summary>
    /// Hash of the handover OTP. Never the code itself.
    /// </summary>
    /// <remarks>
    /// The code proves that the person who booked is the person standing there, which matters most on
    /// exactly the appointments where a vehicle changes hands. Storing the hash means a database dump
    /// does not let somebody collect a car; storing the code means it does.
    /// </remarks>
    public string? OtpHash { get; private set; }

    public DateTimeOffset? OtpIssuedAt { get; private set; }

    public DateTimeOffset? OtpExpiresAt { get; private set; }

    /// <summary>Failed attempts. Capped, because six digits is 10^6 and a determined script is fast.</summary>
    public int OtpAttempts { get; private set; }

    public DateTimeOffset? VerifiedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public string? CancellationReason { get; private set; }

    /// <summary>
    /// The fee charged for cancelling or not turning up, priced by the registry.
    /// </summary>
    /// <remarks>
    /// Recorded, never computed here. Whether a no-show costs ₹500, whether the first one is free,
    /// and whether either applies in a city that has just launched are pricing rules.
    /// </remarks>
    public Money? PenaltyAmount { get; private set; }

    public string? PenaltyRuleCode { get; private set; }

    /// <summary>The deal the penalty settles through, once one is opened. Null when nothing was charged.</summary>
    public Guid? PenaltyDealId { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }

    public static Result<Booking> Request(BookingSpecification specification, BookableResource resource, DateTimeOffset now)
    {
        if (specification.EndsAt <= specification.StartsAt)
        {
            return Error.Validation("booking.window_inverted", "A booking must end after it starts.");
        }

        if (specification.StartsAt < now.AddMinutes(resource.MinimumNoticeMinutes))
        {
            return Error.Rule(
                "booking.too_soon",
                "That slot is inside this resource's notice period.")
                .With("minimumNoticeMinutes", resource.MinimumNoticeMinutes);
        }

        if (specification.StartsAt > now.AddDays(resource.BookingHorizonDays))
        {
            return Error.Rule(
                "booking.too_far_ahead",
                "That slot is beyond how far ahead this resource can be booked.")
                .With("bookingHorizonDays", resource.BookingHorizonDays);
        }

        // Travel applies only when somebody has to travel. A test drive at the showroom does not
        // block the salesperson's morning for the sake of symmetry.
        var buffer = string.Equals(specification.LocationKindCode, LocationKinds.AtCustomer, StringComparison.Ordinal)
            ? resource.TravelBufferMinutes
            : 0;

        var booking = new Booking(
            UuidV7.New(now),
            specification.PurposeCode,
            resource.Id,
            specification.StartsAt,
            specification.EndsAt)
        {
            SubjectType = specification.SubjectType,
            SubjectId = specification.SubjectId,
            CustomerId = specification.CustomerId,
            CustomerType = specification.CustomerType,
            OccupiedFrom = specification.StartsAt.AddMinutes(-buffer),
            OccupiedTo = specification.EndsAt.AddMinutes(buffer),
            IsExclusive = resource.Capacity == 1,
            LocationKindCode = specification.LocationKindCode,
            AddressLine = specification.AddressLine,
            Pincode = specification.Pincode,
            Latitude = specification.Latitude,
            Longitude = specification.Longitude,
            Notes = specification.Notes,
        };

        booking.Raise(new BookingRequested(
            booking.Id, now, booking.PurposeCode, resource.Id, booking.CustomerId,
            booking.StartsAt, booking.EndsAt));

        return booking;
    }

    /// <summary>Moves a booking, recomputing the occupied range so the calendar stays correct.</summary>
    public Result Reschedule(DateTimeOffset startsAt, DateTimeOffset endsAt, BookableResource resource, DateTimeOffset now)
    {
        if (CompletedAt is not null || CancelledAt is not null)
        {
            return Error.Conflict(
                "booking.finished",
                "A finished booking cannot be moved. Make a new one.");
        }

        if (endsAt <= startsAt)
        {
            return Error.Validation("booking.window_inverted", "A booking must end after it starts.");
        }

        var buffer = string.Equals(LocationKindCode, LocationKinds.AtCustomer, StringComparison.Ordinal)
            ? resource.TravelBufferMinutes
            : 0;

        var previousStart = StartsAt;

        StartsAt = startsAt;
        EndsAt = endsAt;
        OccupiedFrom = startsAt.AddMinutes(-buffer);
        OccupiedTo = endsAt.AddMinutes(buffer);

        // The code is tied to the appointment, so moving the appointment retires it. A code that
        // outlived a reschedule would let somebody collect a vehicle at the old time.
        OtpHash = null;
        OtpIssuedAt = null;
        OtpExpiresAt = null;
        OtpAttempts = 0;

        Raise(new BookingRescheduled(Id, now, previousStart, startsAt, endsAt));

        return Result.Success();
    }

    /// <summary>Attaches a freshly issued handover code.</summary>
    public void IssueOtp(string otpHash, DateTimeOffset issuedAt, DateTimeOffset expiresAt)
    {
        OtpHash = otpHash;
        OtpIssuedAt = issuedAt;
        OtpExpiresAt = expiresAt;
        OtpAttempts = 0;
    }

    /// <summary>
    /// Checks a code presented at handover.
    /// </summary>
    /// <remarks>
    /// The comparison itself is the hasher's job so it can be constant-time; what is here is the
    /// policy around it — expiry, attempt cap, and the fact that a used code cannot be reused. The
    /// attempt cap exists because six digits is a million possibilities and a script exhausts that in
    /// minutes on an endpoint with no limit.
    /// </remarks>
    public Result VerifyHandover(bool codeMatches, DateTimeOffset now, int maximumAttempts)
    {
        if (VerifiedAt is not null)
        {
            return Result.Success();
        }

        if (OtpHash is null)
        {
            return Error.Rule("booking.otp_not_issued", "No handover code has been issued for this booking.");
        }

        if (OtpExpiresAt is { } expiresAt && now > expiresAt)
        {
            return Error.Rule("booking.otp_expired", "That code has expired. Ask for a new one.");
        }

        if (OtpAttempts >= maximumAttempts)
        {
            return Error.Rule(
                "booking.otp_attempts_exhausted",
                "Too many incorrect codes. Ask for a new one.")
                .With("maximumAttempts", maximumAttempts);
        }

        if (!codeMatches)
        {
            OtpAttempts++;
            return Error.Validation("booking.otp_incorrect", "That code is not correct.");
        }

        VerifiedAt = now;
        Raise(new BookingVerified(Id, now, CustomerId));

        return Result.Success();
    }

    public Result Complete(DateTimeOffset now)
    {
        if (CancelledAt is not null)
        {
            return Error.Conflict("booking.cancelled", "A cancelled booking cannot be completed.");
        }

        CompletedAt ??= now;
        Raise(new BookingCompleted(Id, now, PurposeCode, SubjectType, SubjectId));

        return Result.Success();
    }

    /// <summary>Cancels, recording whatever the pricing registry said the cancellation costs.</summary>
    public Result Cancel(string reason, Money? penalty, string? penaltyRuleCode, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Error.Validation("booking.cancel.reason_required", "Cancelling a booking needs a reason.");
        }

        if (CompletedAt is not null)
        {
            return Error.Conflict("booking.completed", "A completed booking cannot be cancelled.");
        }

        if (CancelledAt is not null)
        {
            return Result.Success();
        }

        CancelledAt = now;
        CancellationReason = reason;
        PenaltyAmount = penalty;
        PenaltyRuleCode = penaltyRuleCode;

        Raise(new BookingCancelled(
            Id, now, reason, penalty?.MinorUnits, penalty?.Currency.Code, false));

        return Result.Success();
    }

    /// <summary>
    /// Records that nobody turned up, with whatever fee the registry said applies.
    /// </summary>
    /// <remarks>
    /// Distinct from a cancellation because the cost is different and so is the trust signal: a
    /// customer who cancels an hour ahead freed the slot, and one who did not turn up wasted an
    /// inspector's afternoon. The two feed different pricing scopes and different reputation scores.
    /// </remarks>
    public Result MarkNoShow(Money? penalty, string? penaltyRuleCode, DateTimeOffset now)
    {
        if (CompletedAt is not null || CancelledAt is not null)
        {
            return Error.Conflict("booking.finished", "This booking has already finished.");
        }

        CancelledAt = now;
        CancellationReason = NoShowReason;
        PenaltyAmount = penalty;
        PenaltyRuleCode = penaltyRuleCode;

        Raise(new BookingCancelled(
            Id, now, NoShowReason, penalty?.MinorUnits, penalty?.Currency.Code, true));

        return Result.Success();
    }

    /// <summary>Links the deal the penalty is being collected through.</summary>
    public void AttachPenaltyDeal(Guid dealId) => PenaltyDealId ??= dealId;

    private const string NoShowReason = "no_show";
}

/// <summary>Where the appointment happens. Two codes, because only two things follow from it.</summary>
/// <remarks>
/// Reproduced here because the travel-buffer rule compares against one of them. Everything else about
/// a location — what an address looks like, which pincodes are serviceable — is the geography
/// registry's.
/// </remarks>
public static class LocationKinds
{
    public const string AtResource = "at_resource";

    public const string AtCustomer = "at_customer";
}

public sealed record BookingSpecification
{
    public required string PurposeCode { get; init; }

    public required Guid CustomerId { get; init; }

    public required DateTimeOffset StartsAt { get; init; }

    public required DateTimeOffset EndsAt { get; init; }

    public string LocationKindCode { get; init; } = LocationKinds.AtResource;

    public string? CustomerType { get; init; }

    public string? SubjectType { get; init; }

    public Guid? SubjectId { get; init; }

    public string? AddressLine { get; init; }

    public string? Pincode { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public string? Notes { get; init; }
}
