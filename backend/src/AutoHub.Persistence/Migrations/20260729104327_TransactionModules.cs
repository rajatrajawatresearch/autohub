using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TransactionModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "auction");

            migrationBuilder.EnsureSchema(
                name: "booking");

            migrationBuilder.EnsureSchema(
                name: "deals");

            migrationBuilder.EnsureSchema(
                name: "payments");

            migrationBuilder.CreateTable(
                name: "auction_type_profiles",
                schema: "auction",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    anti_snipe_extension_seconds = table.Column<int>(type: "integer", nullable: false),
                    anti_snipe_window_seconds = table.Column<int>(type: "integer", nullable: false),
                    awards_instantly = table.Column<bool>(type: "boolean", nullable: false),
                    bid_increment_scope = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    bidder_identity_visible = table.Column<bool>(type: "boolean", nullable: false),
                    bids_visible = table.Column<bool>(type: "boolean", nullable: false),
                    buy_now_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    max_extensions = table.Column<int>(type: "integer", nullable: false),
                    payment_window_minutes = table.Column<int>(type: "integer", nullable: false),
                    proxy_bidding_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    reserve_required = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auction_type_profiles", x => x.id);
                    table.CheckConstraint("ck_auction_type_profiles_settings", "anti_snipe_window_seconds >= 0 AND anti_snipe_extension_seconds >= 0 AND max_extensions >= 0 AND payment_window_minutes > 0");
                });

            migrationBuilder.CreateTable(
                name: "auctions",
                schema: "auction",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    city_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    state_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    win_pricing_scope = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    preview_opens_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    opens_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    closes_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    is_invite_only = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    anti_snipe_extension_seconds = table.Column<int>(type: "integer", nullable: false),
                    anti_snipe_window_seconds = table.Column<int>(type: "integer", nullable: false),
                    awards_instantly = table.Column<bool>(type: "boolean", nullable: false),
                    bid_increment_scope = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    bidder_identity_visible = table.Column<bool>(type: "boolean", nullable: false),
                    bids_visible = table.Column<bool>(type: "boolean", nullable: false),
                    buy_now_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    max_extensions = table.Column<int>(type: "integer", nullable: false),
                    payment_window_minutes = table.Column<int>(type: "integer", nullable: false),
                    proxy_bidding_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    reserve_required = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auctions", x => x.id);
                    table.CheckConstraint("ck_auctions_preview", "preview_opens_at <= opens_at");
                    table.CheckConstraint("ck_auctions_settings", "anti_snipe_window_seconds >= 0 AND anti_snipe_extension_seconds >= 0 AND max_extensions >= 0 AND payment_window_minutes > 0");
                    table.CheckConstraint("ck_auctions_window", "closes_at > opens_at");
                });

            migrationBuilder.CreateTable(
                name: "bookable_resources",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    owner_type = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    city_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    travel_buffer_minutes = table.Column<int>(type: "integer", nullable: false),
                    slot_minutes = table.Column<int>(type: "integer", nullable: false),
                    booking_horizon_days = table.Column<int>(type: "integer", nullable: false),
                    minimum_notice_minutes = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bookable_resources", x => x.id);
                    table.CheckConstraint("ck_bookable_resources_capacity", "capacity >= 1");
                    table.CheckConstraint("ck_bookable_resources_slot", "slot_minutes >= 5 AND booking_horizon_days > 0 AND travel_buffer_minutes >= 0 AND minimum_notice_minutes >= 0");
                });

            migrationBuilder.CreateTable(
                name: "deals",
                schema: "deals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    intent_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    pricing_scope = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    source_type = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    listing_id = table.Column<Guid>(type: "uuid", nullable: true),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_deal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    period_start = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    period_end = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    recurrence_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    instalment_number = table.Column<int>(type: "integer", nullable: true),
                    city_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    state_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    applied_rule_code = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    pricing_rule_set_version = table.Column<int>(type: "integer", nullable: true),
                    pricing_explanation = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    quoted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    settled_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    settlement_reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    cancellation_reason = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    base_amount_currency = table.Column<string>(type: "char(3)", nullable: false),
                    base_amount_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    fee_total_currency = table.Column<string>(type: "char(3)", nullable: false),
                    fee_total_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    payable_currency = table.Column<string>(type: "char(3)", nullable: false),
                    payable_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deals", x => x.id);
                    table.CheckConstraint("ck_deals_amounts_non_negative", "base_amount_minor_units >= 0 AND fee_total_minor_units >= 0 AND payable_minor_units >= 0");
                    table.CheckConstraint("ck_deals_payable_balances", "payable_minor_units = base_amount_minor_units + fee_total_minor_units");
                    table.CheckConstraint("ck_deals_period", "period_end IS NULL OR period_start IS NULL OR period_end > period_start");
                    table.CheckConstraint("ck_deals_single_currency", "base_amount_currency = fee_total_currency AND base_amount_currency = payable_currency");
                });

            migrationBuilder.CreateTable(
                name: "ledger_accounts",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    currency = table.Column<string>(type: "char(3)", nullable: false),
                    owner_type = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    allows_contra_balance = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ledger_accounts", x => x.id);
                    table.CheckConstraint("ck_ledger_accounts_type", "type BETWEEN 1 AND 5");
                });

            migrationBuilder.CreateTable(
                name: "ledger_journals",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    scope_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    currency = table.Column<string>(type: "char(3)", nullable: false),
                    source_type = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    reverses_journal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    posted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    total_credits_currency = table.Column<string>(type: "char(3)", nullable: false),
                    total_credits_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    total_debits_currency = table.Column<string>(type: "char(3)", nullable: false),
                    total_debits_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ledger_journals", x => x.id);
                    table.CheckConstraint("ck_ledger_journals_balanced", "total_debits_minor_units = total_credits_minor_units");
                    table.CheckConstraint("ck_ledger_journals_non_empty", "total_debits_minor_units > 0");
                    table.CheckConstraint("ck_ledger_journals_single_currency", "currency = total_debits_currency AND currency = total_credits_currency");
                });

            migrationBuilder.CreateTable(
                name: "payout_batches",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    currency = table.Column<string>(type: "char(3)", nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    instructed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    total_currency = table.Column<string>(type: "char(3)", nullable: false),
                    total_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payout_batches", x => x.id);
                    table.CheckConstraint("ck_payout_batches_single_currency", "currency = total_currency");
                    table.CheckConstraint("ck_payout_batches_total_non_negative", "total_minor_units >= 0");
                });

            migrationBuilder.CreateTable(
                name: "posting_templates",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    line_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    debit_account_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    debit_owner_role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    credit_account_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    credit_owner_role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    condition_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    rationale = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_posting_templates", x => x.id);
                    table.CheckConstraint("ck_posting_templates_accounts_differ", "debit_account_code <> credit_account_code OR debit_owner_role IS DISTINCT FROM credit_owner_role");
                });

            migrationBuilder.CreateTable(
                name: "reconciliation_runs",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    business_date = table.Column<DateOnly>(type: "date", nullable: false),
                    currency = table.Column<string>(type: "char(3)", nullable: false),
                    matched_count = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    ledger_total_currency = table.Column<string>(type: "char(3)", nullable: false),
                    ledger_total_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    provider_total_currency = table.Column<string>(type: "char(3)", nullable: false),
                    provider_total_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reconciliation_runs", x => x.id);
                    table.CheckConstraint("ck_reconciliation_runs_matched", "matched_count >= 0");
                    table.CheckConstraint("ck_reconciliation_runs_single_currency", "currency = provider_total_currency AND currency = ledger_total_currency");
                });

            migrationBuilder.CreateTable(
                name: "wallets",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_type = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    currency = table.Column<string>(type: "char(3)", nullable: false),
                    ledger_version = table.Column<Guid>(type: "uuid", nullable: true),
                    is_frozen = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    available_currency = table.Column<string>(type: "char(3)", nullable: false),
                    available_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    held_currency = table.Column<string>(type: "char(3)", nullable: false),
                    held_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallets", x => x.id);
                    table.CheckConstraint("ck_wallets_held_within_available", "held_minor_units <= available_minor_units");
                    table.CheckConstraint("ck_wallets_non_negative", "available_minor_units >= 0 AND held_minor_units >= 0");
                    table.CheckConstraint("ck_wallets_single_currency", "currency = available_currency AND currency = held_currency");
                });

            migrationBuilder.CreateTable(
                name: "auction_lots",
                schema: "auction",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    auction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_number = table.Column<int>(type: "integer", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    listing_id = table.Column<Guid>(type: "uuid", nullable: true),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_type = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    reserve_price_minor_units = table.Column<long>(type: "bigint", nullable: true),
                    buy_now_price_minor_units = table.Column<long>(type: "bigint", nullable: true),
                    opens_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    closes_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    scheduled_close_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    extension_count = table.Column<int>(type: "integer", nullable: false),
                    win_pricing_scope = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    city_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    state_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    high_bid_amount_minor_units = table.Column<long>(type: "bigint", nullable: true),
                    high_bidder_id = table.Column<Guid>(type: "uuid", nullable: true),
                    bid_sequence = table.Column<long>(type: "bigint", nullable: false),
                    bid_count = table.Column<int>(type: "integer", nullable: false),
                    winning_bid_id = table.Column<Guid>(type: "uuid", nullable: true),
                    winner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    winning_amount_minor_units = table.Column<long>(type: "bigint", nullable: true),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_due_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    relist_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    anti_snipe_extension_seconds = table.Column<int>(type: "integer", nullable: false),
                    anti_snipe_window_seconds = table.Column<int>(type: "integer", nullable: false),
                    awards_instantly = table.Column<bool>(type: "boolean", nullable: false),
                    bid_increment_scope = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    bidder_identity_visible = table.Column<bool>(type: "boolean", nullable: false),
                    bids_visible = table.Column<bool>(type: "boolean", nullable: false),
                    buy_now_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    max_extensions = table.Column<int>(type: "integer", nullable: false),
                    payment_window_minutes = table.Column<int>(type: "integer", nullable: false),
                    proxy_bidding_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    reserve_required = table.Column<bool>(type: "boolean", nullable: false),
                    start_price_currency = table.Column<string>(type: "char(3)", nullable: false),
                    start_price_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auction_lots", x => x.id);
                    table.CheckConstraint("ck_auction_lots_counts", "bid_count >= 0 AND extension_count >= 0 AND relist_count >= 0 AND bid_sequence >= 0");
                    table.CheckConstraint("ck_auction_lots_currency", "start_price_currency = 'INR'");
                    table.CheckConstraint("ck_auction_lots_prices_positive", "start_price_minor_units >= 0 AND (reserve_price_minor_units IS NULL OR reserve_price_minor_units >= 0) AND (buy_now_price_minor_units IS NULL OR buy_now_price_minor_units >= 0)");
                    table.CheckConstraint("ck_auction_lots_reserve_above_start", "reserve_price_minor_units IS NULL OR reserve_price_minor_units >= start_price_minor_units");
                    table.CheckConstraint("ck_auction_lots_settings", "anti_snipe_window_seconds >= 0 AND anti_snipe_extension_seconds >= 0 AND max_extensions >= 0 AND payment_window_minutes > 0");
                    table.CheckConstraint("ck_auction_lots_window", "closes_at > opens_at");
                    table.ForeignKey(
                        name: "fk_auction_lots_auctions_auction_id",
                        column: x => x.auction_id,
                        principalSchema: "auction",
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    bookable_resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_type = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_type = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    occupied_from = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    occupied_to = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    is_exclusive = table.Column<bool>(type: "boolean", nullable: false),
                    location_kind_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    address_line = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    pincode = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    otp_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    otp_issued_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    otp_expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    otp_attempts = table.Column<int>(type: "integer", nullable: false),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    cancellation_reason = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    penalty_minor_units = table.Column<long>(type: "bigint", nullable: true),
                    penalty_rule_code = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    penalty_deal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bookings", x => x.id);
                    table.CheckConstraint("ck_bookings_occupied_window", "occupied_from <= starts_at AND occupied_to >= ends_at");
                    table.CheckConstraint("ck_bookings_otp_attempts", "otp_attempts >= 0");
                    table.CheckConstraint("ck_bookings_penalty_non_negative", "penalty_minor_units IS NULL OR penalty_minor_units >= 0");
                    table.CheckConstraint("ck_bookings_window", "ends_at > starts_at");
                    table.ForeignKey(
                        name: "fk_bookings_bookable_resources_bookable_resource_id",
                        column: x => x.bookable_resource_id,
                        principalSchema: "booking",
                        principalTable: "bookable_resources",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "calendar_exceptions",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bookable_resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false),
                    opens_at = table.Column<TimeOnly>(type: "time", nullable: true),
                    closes_at = table.Column<TimeOnly>(type: "time", nullable: true),
                    reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calendar_exceptions", x => x.id);
                    table.CheckConstraint("ck_calendar_exceptions_hours", "is_closed OR (opens_at IS NOT NULL AND closes_at IS NOT NULL AND closes_at > opens_at)");
                    table.ForeignKey(
                        name: "fk_calendar_exceptions_bookable_resources_bookable_resource_id",
                        column: x => x.bookable_resource_id,
                        principalSchema: "booking",
                        principalTable: "bookable_resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "calendar_windows",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bookable_resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    opens_at = table.Column<TimeOnly>(type: "time", nullable: false),
                    closes_at = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calendar_windows", x => x.id);
                    table.CheckConstraint("ck_calendar_windows_order", "closes_at > opens_at");
                    table.ForeignKey(
                        name: "fk_calendar_windows_bookable_resources_bookable_resource_id",
                        column: x => x.bookable_resource_id,
                        principalSchema: "booking",
                        principalTable: "bookable_resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "deal_lines",
                schema: "deals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    label = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    basis = table.Column<int>(type: "integer", nullable: true),
                    rate = table.Column<decimal>(type: "numeric(10,6)", nullable: true),
                    rule_code = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    applies_to_csv = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_principal = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    amount_currency = table.Column<string>(type: "char(3)", nullable: false),
                    amount_minor_units = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deal_lines", x => x.id);
                    table.CheckConstraint("ck_deal_lines_rate", "rate IS NULL OR (rate >= 0 AND rate <= 1)");
                    table.ForeignKey(
                        name: "fk_deal_lines_deals_deal_id",
                        column: x => x.deal_id,
                        principalSchema: "deals",
                        principalTable: "deals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "deal_parties",
                schema: "deals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: true),
                    party_type = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    is_payer = table.Column<bool>(type: "boolean", nullable: false),
                    is_payee = table.Column<bool>(type: "boolean", nullable: false),
                    requires_acceptance = table.Column<bool>(type: "boolean", nullable: false),
                    settlement_share_ratio = table.Column<int>(type: "integer", nullable: false),
                    payout_account_ref = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deal_parties", x => x.id);
                    table.CheckConstraint("ck_deal_parties_share_non_negative", "settlement_share_ratio >= 0");
                    table.ForeignKey(
                        name: "fk_deal_parties_deals_deal_id",
                        column: x => x.deal_id,
                        principalSchema: "deals",
                        principalTable: "deals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ledger_entries",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    journal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    account_owner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    direction = table.Column<int>(type: "integer", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    subject_type = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    line_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    memo = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    posted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    amount_currency = table.Column<string>(type: "char(3)", nullable: false),
                    amount_minor_units = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ledger_entries", x => x.id);
                    table.CheckConstraint("ck_ledger_entries_amount_positive", "amount_minor_units > 0");
                    table.CheckConstraint("ck_ledger_entries_direction", "direction IN (1, 2)");
                    table.CheckConstraint("ck_ledger_entries_line_number", "line_number >= 0");
                    table.ForeignKey(
                        name: "fk_ledger_entries_ledger_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "payments",
                        principalTable: "ledger_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ledger_entries_ledger_journals_journal_id",
                        column: x => x.journal_id,
                        principalSchema: "payments",
                        principalTable: "ledger_journals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_intents",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    posting_scope = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    payer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payer_type = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    method_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    provider_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    provider_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    captured_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    failed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    failure_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    journal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    amount_currency = table.Column<string>(type: "char(3)", nullable: false),
                    amount_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_intents", x => x.id);
                    table.CheckConstraint("ck_payment_intents_amount_positive", "amount_minor_units > 0");
                    table.CheckConstraint("ck_payment_intents_capture_posted", "captured_at IS NULL OR journal_id IS NOT NULL");
                    table.CheckConstraint("ck_payment_intents_status", "status BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "fk_payment_intents_ledger_journals_journal_id",
                        column: x => x.journal_id,
                        principalSchema: "payments",
                        principalTable: "ledger_journals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payout_instructions",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payout_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payee_type = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    payee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payout_account_ref = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    outcome = table.Column<int>(type: "integer", nullable: false),
                    provider_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    failure_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    settled_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    settlement_journal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    amount_currency = table.Column<string>(type: "char(3)", nullable: false),
                    amount_minor_units = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payout_instructions", x => x.id);
                    table.CheckConstraint("ck_payout_instructions_amount_positive", "amount_minor_units > 0");
                    table.CheckConstraint("ck_payout_instructions_outcome", "outcome BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_payout_instructions_settlement_posted", "settled_at IS NULL OR settlement_journal_id IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_payout_instructions_ledger_journals_settlement_journal_id",
                        column: x => x.settlement_journal_id,
                        principalSchema: "payments",
                        principalTable: "ledger_journals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payout_instructions_payout_batches_payout_batch_id",
                        column: x => x.payout_batch_id,
                        principalSchema: "payments",
                        principalTable: "payout_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bids",
                schema: "auction",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    auction_lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bidder_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bidder_type = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    maximum_amount_minor_units = table.Column<long>(type: "bigint", nullable: true),
                    sequence = table.Column<long>(type: "bigint", nullable: false),
                    outcome = table.Column<int>(type: "integer", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    placed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    applied_increment_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    device_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    amount_currency = table.Column<string>(type: "char(3)", nullable: false),
                    amount_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    standing_amount_after_currency = table.Column<string>(type: "char(3)", nullable: false),
                    standing_amount_after_minor_units = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bids", x => x.id);
                    table.CheckConstraint("ck_bids_amount_positive", "amount_minor_units > 0");
                    table.CheckConstraint("ck_bids_currency", "amount_currency = 'INR'");
                    table.CheckConstraint("ck_bids_sequence", "sequence >= 0");
                    table.ForeignKey(
                        name: "fk_bids_auction_lots_auction_lot_id",
                        column: x => x.auction_lot_id,
                        principalSchema: "auction",
                        principalTable: "auction_lots",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "reconciliation_differences",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reconciliation_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    provider_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    provider_amount_minor_units = table.Column<long>(type: "bigint", nullable: true),
                    ledger_amount_minor_units = table.Column<long>(type: "bigint", nullable: true),
                    payment_intent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "text", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reconciliation_differences", x => x.id);
                    table.CheckConstraint("ck_reconciliation_differences_has_side", "provider_amount_minor_units IS NOT NULL OR ledger_amount_minor_units IS NOT NULL");
                    table.CheckConstraint("ck_reconciliation_differences_kind", "kind BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "fk_reconciliation_differences_payment_intents_payment_intent_id",
                        column: x => x.payment_intent_id,
                        principalSchema: "payments",
                        principalTable: "payment_intents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reconciliation_differences_reconciliation_runs_reconciliati~",
                        column: x => x.reconciliation_run_id,
                        principalSchema: "payments",
                        principalTable: "reconciliation_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_auction_lots_due_for_close",
                schema: "auction",
                table: "auction_lots",
                column: "closes_at",
                filter: "closed_at IS NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_auction_lots_payment_due_at",
                schema: "auction",
                table: "auction_lots",
                column: "payment_due_at",
                filter: "payment_due_at IS NOT NULL AND deal_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_auction_lots_seller_id_id",
                schema: "auction",
                table: "auction_lots",
                columns: new[] { "seller_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_auction_lots_vehicle_id",
                schema: "auction",
                table: "auction_lots",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ux_auction_lots_number",
                schema: "auction",
                table: "auction_lots",
                columns: new[] { "auction_id", "lot_number" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_auction_type_profiles_type_code",
                schema: "auction",
                table: "auction_type_profiles",
                column: "type_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_auctions_city_code_opens_at",
                schema: "auction",
                table: "auctions",
                columns: new[] { "city_code", "opens_at" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_auctions_code",
                schema: "auction",
                table: "auctions",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bids_bidder_id_id",
                schema: "auction",
                table: "bids",
                columns: new[] { "bidder_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_bids_device_id_placed_at",
                schema: "auction",
                table: "bids",
                columns: new[] { "device_id", "placed_at" },
                filter: "device_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bids_lot_sequence",
                schema: "auction",
                table: "bids",
                columns: new[] { "auction_lot_id", "sequence" });

            migrationBuilder.CreateIndex(
                name: "ux_bids_idempotency",
                schema: "auction",
                table: "bids",
                columns: new[] { "auction_lot_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bookable_resources_city_code_resource_type_code",
                schema: "booking",
                table: "bookable_resources",
                columns: new[] { "city_code", "resource_type_code" },
                filter: "is_active AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bookable_resources_owner_id_resource_type_code",
                schema: "booking",
                table: "bookable_resources",
                columns: new[] { "owner_id", "resource_type_code" },
                filter: "owner_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_customer_id_starts_at",
                schema: "booking",
                table: "bookings",
                columns: new[] { "customer_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_occupied",
                schema: "booking",
                table: "bookings",
                columns: new[] { "bookable_resource_id", "occupied_from", "occupied_to" },
                filter: "cancelled_at IS NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_subject_type_subject_id",
                schema: "booking",
                table: "bookings",
                columns: new[] { "subject_type", "subject_id" },
                filter: "subject_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_calendar_exceptions_date",
                schema: "booking",
                table: "calendar_exceptions",
                columns: new[] { "bookable_resource_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_calendar_windows_bookable_resource_id_day_of_week",
                schema: "booking",
                table: "calendar_windows",
                columns: new[] { "bookable_resource_id", "day_of_week" });

            migrationBuilder.CreateIndex(
                name: "ix_deal_lines_deal_id_sort_order",
                schema: "deals",
                table: "deal_lines",
                columns: new[] { "deal_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ux_deal_lines_code",
                schema: "deals",
                table: "deal_lines",
                columns: new[] { "deal_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_deal_lines_principal",
                schema: "deals",
                table: "deal_lines",
                column: "deal_id",
                unique: true,
                filter: "is_principal");

            migrationBuilder.CreateIndex(
                name: "ix_deal_parties_party_id_id",
                schema: "deals",
                table: "deal_parties",
                columns: new[] { "party_id", "id" },
                filter: "party_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_deal_parties_role",
                schema: "deals",
                table: "deal_parties",
                columns: new[] { "deal_id", "role_code", "party_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_deals_intent_code_id",
                schema: "deals",
                table: "deals",
                columns: new[] { "intent_code", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_deals_listing_id",
                schema: "deals",
                table: "deals",
                column: "listing_id",
                filter: "listing_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_deals_parent_deal_id_instalment_number",
                schema: "deals",
                table: "deals",
                columns: new[] { "parent_deal_id", "instalment_number" },
                filter: "parent_deal_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_deals_reference",
                schema: "deals",
                table: "deals",
                column: "reference",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_deals_vehicle_id",
                schema: "deals",
                table: "deals",
                column: "vehicle_id",
                filter: "vehicle_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_deals_source",
                schema: "deals",
                table: "deals",
                columns: new[] { "source_type", "source_id" },
                unique: true,
                filter: "source_type IS NOT NULL AND source_id IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ledger_accounts_owner_type_owner_id",
                schema: "payments",
                table: "ledger_accounts",
                columns: new[] { "owner_type", "owner_id" },
                filter: "owner_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_ledger_accounts_scope",
                schema: "payments",
                table: "ledger_accounts",
                columns: new[] { "code", "owner_id", "currency" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_account_id_posted_at",
                schema: "payments",
                table: "ledger_entries",
                columns: new[] { "account_id", "posted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_statement",
                schema: "payments",
                table: "ledger_entries",
                columns: new[] { "account_code", "account_owner_id", "posted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_subject_type_subject_id",
                schema: "payments",
                table: "ledger_entries",
                columns: new[] { "subject_type", "subject_id" },
                filter: "subject_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_ledger_entries_line",
                schema: "payments",
                table: "ledger_entries",
                columns: new[] { "journal_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ledger_journals_reference",
                schema: "payments",
                table: "ledger_journals",
                column: "reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ledger_journals_scope_code_posted_at",
                schema: "payments",
                table: "ledger_journals",
                columns: new[] { "scope_code", "posted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ledger_journals_source_type_source_id",
                schema: "payments",
                table: "ledger_journals",
                columns: new[] { "source_type", "source_id" },
                filter: "source_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_ledger_journals_idempotency",
                schema: "payments",
                table: "ledger_journals",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_intents_deal_id_id",
                schema: "payments",
                table: "payment_intents",
                columns: new[] { "deal_id", "id" },
                filter: "deal_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payment_intents_journal_id",
                schema: "payments",
                table: "payment_intents",
                column: "journal_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_intents_payer_id_id",
                schema: "payments",
                table: "payment_intents",
                columns: new[] { "payer_id", "id" },
                filter: "payer_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_payment_intents_idempotency",
                schema: "payments",
                table: "payment_intents",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_payment_intents_provider_reference",
                schema: "payments",
                table: "payment_intents",
                columns: new[] { "provider_code", "provider_reference" },
                unique: true,
                filter: "provider_code IS NOT NULL AND provider_reference IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payout_batches_business_date",
                schema: "payments",
                table: "payout_batches",
                column: "business_date");

            migrationBuilder.CreateIndex(
                name: "ix_payout_batches_reference",
                schema: "payments",
                table: "payout_batches",
                column: "reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_payout_batches_open",
                schema: "payments",
                table: "payout_batches",
                columns: new[] { "currency", "business_date" },
                unique: true,
                filter: "instructed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payout_instructions_payee_id_id",
                schema: "payments",
                table: "payout_instructions",
                columns: new[] { "payee_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_payout_instructions_provider_reference",
                schema: "payments",
                table: "payout_instructions",
                column: "provider_reference",
                filter: "provider_reference IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payout_instructions_settlement_journal_id",
                schema: "payments",
                table: "payout_instructions",
                column: "settlement_journal_id");

            migrationBuilder.CreateIndex(
                name: "ux_payout_instructions_payee",
                schema: "payments",
                table: "payout_instructions",
                columns: new[] { "payout_batch_id", "payee_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_posting_templates_condition_json",
                schema: "payments",
                table: "posting_templates",
                column: "condition_json",
                filter: "condition_json IS NOT NULL")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_posting_templates_scope_code_sort_order",
                schema: "payments",
                table: "posting_templates",
                columns: new[] { "scope_code", "sort_order" },
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_reconciliation_differences_payment_intent_id",
                schema: "payments",
                table: "reconciliation_differences",
                column: "payment_intent_id");

            migrationBuilder.CreateIndex(
                name: "ix_reconciliation_differences_provider_reference",
                schema: "payments",
                table: "reconciliation_differences",
                column: "provider_reference");

            migrationBuilder.CreateIndex(
                name: "ix_reconciliation_differences_reconciliation_run_id_kind",
                schema: "payments",
                table: "reconciliation_differences",
                columns: new[] { "reconciliation_run_id", "kind" });

            migrationBuilder.CreateIndex(
                name: "ix_reconciliation_runs_provider_code_business_date",
                schema: "payments",
                table: "reconciliation_runs",
                columns: new[] { "provider_code", "business_date" });

            migrationBuilder.CreateIndex(
                name: "ix_wallets_owner_id",
                schema: "payments",
                table: "wallets",
                column: "owner_id",
                filter: "NOT is_frozen AND available_minor_units > held_minor_units");

            migrationBuilder.CreateIndex(
                name: "ux_wallets_owner",
                schema: "payments",
                table: "wallets",
                columns: new[] { "owner_type", "owner_id", "account_code", "currency" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auction_type_profiles",
                schema: "auction");

            migrationBuilder.DropTable(
                name: "bids",
                schema: "auction");

            migrationBuilder.DropTable(
                name: "bookings",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "calendar_exceptions",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "calendar_windows",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "deal_lines",
                schema: "deals");

            migrationBuilder.DropTable(
                name: "deal_parties",
                schema: "deals");

            migrationBuilder.DropTable(
                name: "ledger_entries",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "payout_instructions",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "posting_templates",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "reconciliation_differences",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "wallets",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "auction_lots",
                schema: "auction");

            migrationBuilder.DropTable(
                name: "bookable_resources",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "deals",
                schema: "deals");

            migrationBuilder.DropTable(
                name: "ledger_accounts",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "payout_batches",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "payment_intents",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "reconciliation_runs",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "auctions",
                schema: "auction");

            migrationBuilder.DropTable(
                name: "ledger_journals",
                schema: "payments");
        }
    }
}
