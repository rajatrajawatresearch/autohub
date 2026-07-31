using AutoHub.Platform.Access;

namespace AutoHub.Modules.Bookings;

/// <summary>The permissions the bookings module checks. Which roles hold them is data.</summary>
/// <remarks>
/// One set for all eight purposes. A permission per purpose — <c>booking.test_drive.request</c>,
/// <c>booking.inspection.request</c> — would put the purpose taxonomy into the RBAC registry, and
/// adding a ninth purpose would then need a code change to be grantable at all.
/// </remarks>
public static class BookingPermissions
{
    public const string Request = "booking.request";

    public const string View = "booking.view";

    public const string Reschedule = "booking.reschedule";

    public const string Cancel = "booking.cancel";

    public const string MarkNoShow = "booking.no_show";

    /// <summary>Issuing and checking the handover code. Held by field staff, not by customers.</summary>
    public const string HandoverVerify = "booking.handover.verify";

    public const string ResourceManage = "booking.resource.manage";
}

/// <summary>Declares <see cref="BookingPermissions"/> to the RBAC catalog.</summary>
public sealed class BookingPermissionContributor : IPermissionContributor
{
    public IReadOnlyList<PermissionDefinition> Permissions { get; } =
    [
        PermissionDefinition.Declare(
            BookingPermissions.Request, "Make a booking",
            description: "Book a test drive, inspection, visit, delivery, pickup or handover."),

        PermissionDefinition.Declare(
            BookingPermissions.View, "View bookings",
            description: "Read a booking and its calendar."),

        PermissionDefinition.Declare(
            BookingPermissions.Reschedule, "Reschedule a booking",
            description: "Move a booking to another slot."),

        PermissionDefinition.Declare(
            BookingPermissions.Cancel, "Cancel a booking",
            description: "Cancel a booking, charging whatever the pricing registry says it costs."),

        PermissionDefinition.Declare(
            BookingPermissions.MarkNoShow, "Record a no-show",
            description: "Record that nobody attended, and charge the no-show fee."),

        PermissionDefinition.Declare(
            BookingPermissions.HandoverVerify, "Verify a handover",
            description: "Issue and check the OTP that proves a vehicle changed hands."),

        PermissionDefinition.Declare(
            BookingPermissions.ResourceManage, "Manage bookable resources",
            description: "Create resources and edit their calendars, capacity and buffers."),
    ];
}
