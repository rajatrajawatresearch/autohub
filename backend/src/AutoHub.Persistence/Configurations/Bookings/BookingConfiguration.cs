using AutoHub.Modules.Bookings.Domain;
using AutoHub.SharedKernel.Money;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoHub.Persistence.Configurations.Bookings;

/// <summary>
/// The appointment table, and the buffered range the calendar actually collides on.
/// </summary>
/// <remarks>
/// <c>occupied_from</c>/<c>occupied_to</c> are stored rather than derived because the exclusion
/// constraint in <see cref="BookingSchema"/> cannot call back into <c>bookable_resources</c> for a
/// travel buffer — a constraint sees the row in front of it and nothing else. The row-level checks
/// below come from <see cref="BookingSchema.Checks"/> so the specification and the schema cannot
/// drift apart; the exclusion constraint itself is DDL a migration runs, because EF has no model for
/// <c>EXCLUDE USING gist</c>.
/// </remarks>
internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable(
            "bookings",
            Schemas.Booking,
            table =>
            {
                table.HasCheckConstraint(BookingSchema.Checks.WindowName, BookingSchema.Checks.Window);
                table.HasCheckConstraint(BookingSchema.Checks.OccupiedName, BookingSchema.Checks.Occupied);
                table.HasCheckConstraint(BookingSchema.Checks.OtpAttemptsName, BookingSchema.Checks.OtpAttempts);

                // A penalty is charged, never refunded, so a negative one is a data error rather than
                // a credit. The credit path is a deal, which has its own sign.
                table.HasCheckConstraint(
                    "ck_bookings_penalty_non_negative",
                    "penalty_minor_units IS NULL OR penalty_minor_units >= 0");
            });

        builder.HasKey(booking => booking.Id);

        builder.Property(booking => booking.PurposeCode).HasMaxLength(64).IsRequired();
        builder.Property(booking => booking.SubjectType).HasMaxLength(96);
        builder.Property(booking => booking.CustomerType).HasMaxLength(96);
        builder.Property(booking => booking.LocationKindCode).HasMaxLength(32).IsRequired();
        builder.Property(booking => booking.AddressLine).HasMaxLength(512);
        builder.Property(booking => booking.Pincode).HasMaxLength(12);
        builder.Property(booking => booking.OtpHash).HasMaxLength(128);
        builder.Property(booking => booking.CancellationReason).AsNullableText();
        builder.Property(booking => booking.PenaltyRuleCode).HasMaxLength(96);
        builder.Property(booking => booking.Notes).AsNullableText();

        // A booking carries no currency column of its own, deliberately. The authoritative record of
        // a penalty is the deal it settles through — `penalty_deal_id` — which stores the pair; this
        // column is the denormalised copy the calendar shows without joining to the deals schema.
        builder.HasOptionalMoneyColumn(booking => booking.PenaltyAmount, "penalty", Currency.Inr);

        builder.HasOne<BookableResource>()
            .WithMany()
            .HasForeignKey(booking => booking.BookableResourceId)
            .OnDelete(AggregateConfiguration.WhenPrincipalIsSoftDeleted);

        // The overlap query, which the availability list and the double-booking check both run. Both
        // ends of the buffered range are in the index so a range overlap is answered by a scan of one
        // resource's slice rather than by reading its whole calendar.
        builder.HasIndex(booking => new { booking.BookableResourceId, booking.OccupiedFrom, booking.OccupiedTo })
            .HasFilter("cancelled_at IS NULL AND deleted_at IS NULL")
            .HasDatabaseName("ix_bookings_occupied");

        // "My appointments" — the customer's upcoming list, newest first.
        builder.HasIndex(booking => new { booking.CustomerId, booking.StartsAt });

        // The subject's own page: every viewing booked against one listing or lot.
        builder.HasIndex(booking => new { booking.SubjectType, booking.SubjectId })
            .HasFilter("subject_id IS NOT NULL");
    }
}

/// <summary>
/// Anything whose time can be booked, and its calendar.
/// </summary>
/// <remarks>
/// Nothing in the module branches on <see cref="BookableResource.ResourceTypeCode"/>; the behaviour
/// that differs is capacity, buffer and calendar, all of which are columns here. That is why adding
/// "charging bay" is a row rather than a class.
/// </remarks>
internal sealed class BookableResourceConfiguration : IEntityTypeConfiguration<BookableResource>
{
    public void Configure(EntityTypeBuilder<BookableResource> builder)
    {
        builder.ToTable(
            "bookable_resources",
            Schemas.Booking,
            table =>
            {
                table.HasCheckConstraint("ck_bookable_resources_capacity", "capacity >= 1");

                table.HasCheckConstraint(
                    "ck_bookable_resources_slot",
                    "slot_minutes >= 5 AND booking_horizon_days > 0 "
                    + "AND travel_buffer_minutes >= 0 AND minimum_notice_minutes >= 0");
            });

        builder.HasKey(resource => resource.Id);

        builder.Property(resource => resource.ResourceTypeCode).HasMaxLength(64).IsRequired();
        builder.Property(resource => resource.Name).HasMaxLength(160).IsRequired();
        builder.Property(resource => resource.OwnerType).HasMaxLength(96);
        builder.Property(resource => resource.CityCode).HasMaxLength(64);

        builder.HasMany(resource => resource.Windows)
            .WithOne()
            .HasForeignKey(window => window.BookableResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(resource => resource.Exceptions)
            .WithOne()
            .HasForeignKey(exception => exception.BookableResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.UsesBackingField(nameof(BookableResource.Windows));
        builder.UsesBackingField(nameof(BookableResource.Exceptions));

        // "Which inspectors serve Pune?" — the query the slot search starts from.
        builder.HasIndex(resource => new { resource.CityCode, resource.ResourceTypeCode })
            .HasFilter("is_active AND deleted_at IS NULL");

        builder.HasIndex(resource => new { resource.OwnerId, resource.ResourceTypeCode })
            .HasFilter("owner_id IS NOT NULL");
    }
}

/// <summary>The recurring weekly opening hours, stored as wall-clock times.</summary>
/// <remarks>
/// <c>time</c> rather than <c>timestamptz</c> because a workshop opens at nine, not at 03:30 UTC.
/// India has one time zone and no daylight saving, which is what makes combining the time with a
/// date at query time correct here and would not make it correct elsewhere.
/// </remarks>
internal sealed class CalendarWindowConfiguration : IEntityTypeConfiguration<CalendarWindow>
{
    public void Configure(EntityTypeBuilder<CalendarWindow> builder)
    {
        builder.ToTable(
            "calendar_windows",
            Schemas.Booking,
            table => table.HasCheckConstraint("ck_calendar_windows_order", "closes_at > opens_at"));

        builder.HasKey(window => window.Id);

        builder.Property(window => window.OpensAt).HasColumnType("time");
        builder.Property(window => window.ClosesAt).HasColumnType("time");

        builder.HasIndex(window => new { window.BookableResourceId, window.DayOfWeek });
    }
}

/// <summary>A date that overrides the weekly pattern: a holiday, leave, or extended hours.</summary>
internal sealed class CalendarExceptionConfiguration : IEntityTypeConfiguration<CalendarException>
{
    public void Configure(EntityTypeBuilder<CalendarException> builder)
    {
        builder.ToTable(
            "calendar_exceptions",
            Schemas.Booking,
            table => table.HasCheckConstraint(
                "ck_calendar_exceptions_hours",
                "is_closed OR (opens_at IS NOT NULL AND closes_at IS NOT NULL AND closes_at > opens_at)"));

        builder.HasKey(exception => exception.Id);

        builder.Property(exception => exception.OpensAt).HasColumnType("time");
        builder.Property(exception => exception.ClosesAt).HasColumnType("time");
        builder.Property(exception => exception.Reason).HasMaxLength(256);

        // One override per resource per date. Two rows for Diwali is a resource that is both closed
        // and open, and which one wins depends on the query plan.
        builder.HasIndex(exception => new { exception.BookableResourceId, exception.Date })
            .IsUnique()
            .HasDatabaseName("ux_calendar_exceptions_date");
    }
}
