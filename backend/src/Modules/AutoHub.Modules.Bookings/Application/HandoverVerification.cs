using AutoHub.Modules.Bookings.Domain;
using AutoHub.Platform.Workflow;
using AutoHub.SharedKernel.Results;
using AutoHub.SharedKernel.Time;
using MediatR;

namespace AutoHub.Modules.Bookings.Application;

/// <summary>
/// Issues the code the customer will read out at handover.
/// </summary>
/// <remarks>
/// The plaintext is returned once, to be delivered over the notification registry's channels, and is
/// never stored. Reissuing replaces the old hash, which is why a lost code is a reissue rather than a
/// lookup — there is nothing to look up.
/// </remarks>
public sealed record IssueHandoverCodeCommand : IRequest<Result<string>>
{
    public required WorkflowActor Actor { get; init; }

    public required Guid BookingId { get; init; }

    /// <summary>How long the code is good for. Short, because it is used face to face.</summary>
    public int ValidForMinutes { get; init; } = 30;
}

public sealed class IssueHandoverCodeHandler : IRequestHandler<IssueHandoverCodeCommand, Result<string>>
{
    private readonly IBookingRepository _bookings;
    private readonly IHandoverOtp _otp;
    private readonly IClock _clock;

    public IssueHandoverCodeHandler(IBookingRepository bookings, IHandoverOtp otp, IClock clock)
    {
        _bookings = bookings;
        _otp = otp;
        _clock = clock;
    }

    public async Task<Result<string>> Handle(
        IssueHandoverCodeCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.Actor.Holds(BookingPermissions.HandoverVerify))
        {
            return Error.Forbidden("booking.permission_denied", "You cannot issue a handover code.")
                .With("requiredPermission", BookingPermissions.HandoverVerify);
        }

        var booking = await _bookings.GetAsync(request.BookingId, cancellationToken);
        if (booking is null)
        {
            return Error.NotFound("booking.not_found", "No booking with that id.")
                .With("bookingId", request.BookingId);
        }

        if (booking.CancelledAt is not null)
        {
            return Error.Conflict("booking.cancelled", "A cancelled booking has nothing to hand over.");
        }

        var now = _clock.UtcNow;
        var issued = _otp.Issue(booking.Id);

        booking.IssueOtp(issued.Hash, now, now.AddMinutes(request.ValidForMinutes));

        await _bookings.SaveChangesAsync(cancellationToken);

        return issued.Code;
    }
}

/// <summary>
/// Checks the code at the point of handover, and completes the booking.
/// </summary>
/// <remarks>
/// <b>Why the code exists at all.</b> On the appointments where a vehicle moves — a delivery, a
/// pickup, a rental handover — the platform has to be able to prove afterwards that the person who
/// took the car is the person who booked it. Without a code, "I never received the vehicle" and "the
/// driver gave it to the wrong person" are unanswerable, and both arrive.
/// <para>
/// Verification and completion are one transition rather than two. A handover that is verified but
/// not completed is a state with no meaning that somebody would eventually have to write a
/// reconciliation job for.
/// </para>
/// </remarks>
public sealed record VerifyHandoverCommand : IRequest<Result<BookingView>>
{
    public required WorkflowActor Actor { get; init; }

    public required Guid BookingId { get; init; }

    public required string Code { get; init; }
}

public sealed class VerifyHandoverHandler : IRequestHandler<VerifyHandoverCommand, Result<BookingView>>
{
    private readonly IBookingRepository _bookings;
    private readonly IBookableResourceRepository _resources;
    private readonly IHandoverOtp _otp;
    private readonly IWorkflowEngine _workflow;
    private readonly IClock _clock;

    public VerifyHandoverHandler(
        IBookingRepository bookings,
        IBookableResourceRepository resources,
        IHandoverOtp otp,
        IWorkflowEngine workflow,
        IClock clock)
    {
        _bookings = bookings;
        _resources = resources;
        _otp = otp;
        _workflow = workflow;
        _clock = clock;
    }

    /// <summary>
    /// Wrong codes allowed before a new one is needed.
    /// </summary>
    /// <remarks>
    /// A technical parameter of the brute-force defence, not a business rule: six digits is 10^6, and
    /// five attempts leaves the odds at one in two hundred thousand. Someone standing at a car with a
    /// crackly phone line needs a few tries; nobody needs fifty.
    /// </remarks>
    private const int MaximumAttempts = 5;

    public async Task<Result<BookingView>> Handle(
        VerifyHandoverCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.Actor.Holds(BookingPermissions.HandoverVerify))
        {
            return Error.Forbidden("booking.permission_denied", "You cannot verify this handover.")
                .With("requiredPermission", BookingPermissions.HandoverVerify);
        }

        var booking = await _bookings.GetAsync(request.BookingId, cancellationToken);
        if (booking is null)
        {
            return Error.NotFound("booking.not_found", "No booking with that id.")
                .With("bookingId", request.BookingId);
        }

        var matches = booking.OtpHash is not null
            && _otp.Matches(booking.Id, request.Code, booking.OtpHash);

        var now = _clock.UtcNow;

        var verified = booking.VerifyHandover(matches, now, MaximumAttempts);
        if (verified.IsFailure)
        {
            // Saved even on failure: the attempt counter is the brute-force defence, and a counter
            // that is rolled back with the failed attempt counts nothing.
            await _bookings.SaveChangesAsync(cancellationToken);
            return Result.Failure<BookingView>(verified.Error);
        }

        var completed = booking.Complete(now);
        if (completed.IsFailure)
        {
            return Result.Failure<BookingView>(completed.Error);
        }

        var resource = await _resources.GetAsync(booking.BookableResourceId, cancellationToken);

        var snapshot = await _workflow.FireAsync(
            new WorkflowFireRequest
            {
                WorkflowKey = BookingWorkflow.Key,
                EntityId = booking.Id,
                ActionCode = BookingWorkflow.Complete,
                Actor = request.Actor,
                Context = resource is null ? null : BookingContext.Build(booking, resource, now),
            },
            cancellationToken);

        if (snapshot.IsFailure)
        {
            return Result.Failure<BookingView>(snapshot.Error);
        }

        await _bookings.SaveChangesAsync(cancellationToken);

        return BookingView.From(booking, snapshot.Value);
    }
}
