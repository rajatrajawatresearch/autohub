using System.Globalization;
using System.Text.Json.Nodes;
using AutoHub.Modules.Bookings.Domain;

namespace AutoHub.Modules.Bookings.Application;

/// <summary>
/// The facts a workflow guard or a pricing rule may match on for a booking.
/// </summary>
/// <remarks>
/// Assembled in one place so every handler presents the same shape. Three handlers each building
/// their own is three chances for a cancellation-fee rule that matches when the fee is previewed and
/// misses when it is charged — which the customer experiences as being quoted one number and billed
/// another.
/// <para>
/// <c>hoursUntilStart</c> is here because it is the fact almost every cancellation rule turns on
/// ("free more than 24 hours ahead"), and computing it in the rule would mean the rule needed date
/// arithmetic. JsonLogic deliberately has none.
/// </para>
/// </remarks>
internal static class BookingContext
{
    public static JsonObject Build(Booking booking, BookableResource resource, DateTimeOffset? now = null)
    {
        var reference = now ?? booking.StartsAt;

        return new JsonObject
        {
            ["booking"] = new JsonObject
            {
                ["purpose"] = booking.PurposeCode,
                ["locationKind"] = booking.LocationKindCode,
                ["subjectType"] = booking.SubjectType,
                ["startsAt"] = booking.StartsAt.ToString("O", CultureInfo.InvariantCulture),
                ["hoursUntilStart"] = (int)(booking.StartsAt - reference).TotalHours,
                ["verified"] = booking.VerifiedAt is not null,
                ["durationMinutes"] = (int)(booking.EndsAt - booking.StartsAt).TotalMinutes,
            },
            ["resource"] = new JsonObject
            {
                ["type"] = resource.ResourceTypeCode,
                ["capacity"] = resource.Capacity,
                ["ownerType"] = resource.OwnerType,
            },
            ["location"] = new JsonObject
            {
                ["city"] = resource.CityCode,
                ["pincode"] = booking.Pincode,
            },
        };
    }
}
