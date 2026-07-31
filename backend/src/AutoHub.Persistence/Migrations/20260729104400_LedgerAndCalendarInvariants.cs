using AutoHub.Modules.Bookings.Domain;
using AutoHub.Modules.Payments.Domain.Ledger;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoHub.Persistence.Migrations;

/// <summary>
/// The three invariants EF has no model for, applied as the DDL their aggregates specify.
/// </summary>
/// <remarks>
/// <b>Why this is its own migration and not part of the one that created the tables.</b> A scaffolded
/// migration is rewritten from scratch every time the model changes, and anything hand-added to it is
/// thrown away with it. These three are not schema shape — they are the guarantees that make an
/// unbalanced ledger and a double-booked inspector impossible rather than merely unlikely — so losing
/// them silently to a routine <c>migrations add</c> is the exact failure worth engineering against.
/// Kept apart, they survive every regeneration of the table definitions they sit on.
/// <para>
/// <b>Why the SQL is not written here.</b> Every statement below comes from a constant next to the
/// aggregate whose invariant it is — <see cref="LedgerSchema"/> and <see cref="BookingSchema"/> — so
/// the specification and the thing applied are the same text. A copy in a migration is a copy that
/// stops matching the aggregate the first time either is edited, and the one that is wrong is the one
/// nobody reads.
/// </para>
/// <para>
/// All three statements are idempotent (<c>CREATE OR REPLACE</c>, <c>DROP … IF EXISTS</c> before
/// <c>CREATE</c>), so re-running this migration against a database that already has them is a no-op
/// rather than an error.
/// </para>
/// </remarks>
/// <remarks>
/// The attributes are written here rather than in a generated <c>.Designer.cs</c> because this
/// migration changes no model shape and therefore has no target model to build: EF diffs the next
/// scaffold against <c>AutoHubDbContextModelSnapshot</c>, which this file leaves untouched.
/// </remarks>
[DbContext(typeof(AutoHubDbContext))]
[Migration("20260729104400_LedgerAndCalendarInvariants")]
public sealed partial class LedgerAndCalendarInvariants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // A CHECK sees one row; a journal balancing spans its rows, and they do not all exist until
        // the inserting statement has finished. A DEFERRABLE INITIALLY DEFERRED constraint trigger
        // fires at COMMIT, when the whole journal is present.
        migrationBuilder.Sql(LedgerSchema.CreateBalanceConstraint);

        // The balance trigger catches an update that unbalances a journal. This one catches an update
        // that keeps it balanced while changing what it says — swapping two accounts, moving an entry
        // to another owner. There is no legitimate reason for either.
        migrationBuilder.Sql(LedgerSchema.CreateImmutabilityConstraint);

        // Pairwise, so it covers exactly the capacity-1 resources it can decide: an inspector, a bay,
        // one specific vehicle. Capacity above one is a counting rule no exclusion constraint can
        // express, and is held by the advisory lock instead.
        migrationBuilder.Sql(BookingSchema.CreateExclusionConstraint);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(BookingSchema.DropExclusionConstraint);
        migrationBuilder.Sql(LedgerSchema.DropImmutabilityConstraint);
        migrationBuilder.Sql(LedgerSchema.DropBalanceConstraint);
    }
}
