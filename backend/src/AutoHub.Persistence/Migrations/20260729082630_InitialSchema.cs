using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "platform");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gin", ",,")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,")
                .Annotation("Npgsql:PostgresExtension:unaccent", ",,");

            migrationBuilder.CreateTable(
                name: "attribute_definitions",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 512, nullable: false),
                    label = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    data_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    is_searchable = table.Column<bool>(type: "boolean", nullable: false),
                    facet_style = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    display_group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    min_number = table.Column<decimal>(type: "numeric", nullable: true),
                    max_number = table.Column<decimal>(type: "numeric", nullable: true),
                    step = table.Column<decimal>(type: "numeric", nullable: true),
                    min_length = table.Column<int>(type: "integer", nullable: true),
                    max_length = table.Column<int>(type: "integer", nullable: true),
                    pattern = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
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
                    table.PrimaryKey("pk_attribute_definitions", x => x.id);
                    table.CheckConstraint("ck_attribute_definitions_bounds_ordered", "min_number IS NULL OR max_number IS NULL OR max_number >= min_number");
                    table.CheckConstraint("ck_attribute_definitions_lengths_ordered", "(min_length IS NULL OR min_length >= 0) AND (min_length IS NULL OR max_length IS NULL OR max_length >= min_length)");
                    table.CheckConstraint("ck_attribute_definitions_step_positive", "step IS NULL OR step > 0");
                });

            migrationBuilder.CreateTable(
                name: "countries",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 2, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    dial_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    currency_code = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
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
                    table.PrimaryKey("pk_countries", x => x.id);
                    table.CheckConstraint("ck_countries_dial_code_e164", "dial_code ~ '^\\+[0-9]{1,4}$'");
                });

            migrationBuilder.CreateTable(
                name: "feature_flags",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    default_value = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feature_flags", x => x.id);
                    table.CheckConstraint("ck_feature_flags_key_shape", "key ~ '^[a-z][a-z0-9_]*$'");
                });

            migrationBuilder.CreateTable(
                name: "form_definitions",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    entity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
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
                    table.PrimaryKey("pk_form_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "i18n_locales",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 16, nullable: false),
                    english_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    native_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    fallback_code = table.Column<string>(type: "citext", maxLength: 16, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_i18n_locales", x => x.id);
                    table.CheckConstraint("ck_i18n_locales_code_shape", "code ~ '^[a-z]{2}(-[A-Z]{2})?$'");
                    table.CheckConstraint("ck_i18n_locales_fallback_is_not_self", "fallback_code IS NULL OR fallback_code <> code");
                });

            migrationBuilder.CreateTable(
                name: "i18n_resources",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    @namespace = table.Column<string>(name: "namespace", type: "citext", maxLength: 48, nullable: false),
                    key = table.Column<string>(type: "citext", maxLength: 160, nullable: false),
                    locale = table.Column<string>(type: "citext", maxLength: 16, nullable: false),
                    value = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_i18n_resources", x => x.id);
                    table.CheckConstraint("ck_i18n_resources_value_not_blank", "length(btrim(value)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "idempotency_records",
                schema: "platform",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    endpoint = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    request_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    response_status = table.Column<int>(type: "integer", nullable: true),
                    response_body = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_idempotency_records", x => x.key);
                    table.CheckConstraint("ck_idempotency_records_status_is_http", "response_status IS NULL OR response_status BETWEEN 100 AND 599");
                    table.CheckConstraint("ck_idempotency_records_window_ordered", "expires_at > created_at");
                });

            migrationBuilder.CreateTable(
                name: "integration_capabilities",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_essential = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integration_capabilities", x => x.id);
                    table.UniqueConstraint("ak_integration_capabilities_code", x => x.code);
                    table.CheckConstraint("ck_integration_capabilities_code_shape", "code ~ '^[a-z][a-z0-9_]*\\.[a-z][a-z0-9_]*$'");
                });

            migrationBuilder.CreateTable(
                name: "modules",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    gating_flag_key = table.Column<string>(type: "citext", maxLength: 64, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_modules", x => x.id);
                    table.CheckConstraint("ck_modules_code_shape", "code ~ '^[a-z][a-z0-9_]*$'");
                });

            migrationBuilder.CreateTable(
                name: "notification_events",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 128, nullable: false),
                    event_type = table.Column<string>(type: "citext", maxLength: 96, nullable: false),
                    category_code = table.Column<string>(type: "citext", maxLength: 48, nullable: false),
                    audience_kind = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    audience_selector = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    channels_csv = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    template_key = table.Column<string>(type: "citext", maxLength: 96, nullable: false),
                    condition_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: true),
                    urgency = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    dedupe_window_seconds = table.Column<int>(type: "integer", nullable: true),
                    dedupe_key_template = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    throttle_max_per_window = table.Column<int>(type: "integer", nullable: true),
                    throttle_window_seconds = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_events", x => x.id);
                    table.CheckConstraint("ck_notification_events_active_has_channels", "NOT is_active OR length(channels_csv) > 0");
                    table.CheckConstraint("ck_notification_events_dedupe_window", "dedupe_window_seconds IS NULL OR dedupe_window_seconds > 0");
                    table.CheckConstraint("ck_notification_events_throttle_paired", "(throttle_max_per_window IS NULL) = (throttle_window_seconds IS NULL)");
                    table.CheckConstraint("ck_notification_events_throttle_positive", "(throttle_max_per_window IS NULL OR throttle_max_per_window > 0) AND (throttle_window_seconds IS NULL OR throttle_window_seconds > 0)");
                });

            migrationBuilder.CreateTable(
                name: "notification_subscriptions",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_code = table.Column<string>(type: "citext", maxLength: 48, nullable: true),
                    quiet_hours_start_minute_ist = table.Column<int>(type: "integer", nullable: true),
                    quiet_hours_end_minute_ist = table.Column<int>(type: "integer", nullable: true),
                    do_not_disturb = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_subscriptions", x => x.id);
                    table.CheckConstraint("ck_notification_subscriptions_quiet_hours_in_range", "(quiet_hours_start_minute_ist IS NULL OR quiet_hours_start_minute_ist BETWEEN 0 AND 1439) AND (quiet_hours_end_minute_ist IS NULL OR quiet_hours_end_minute_ist BETWEEN 0 AND 1439)");
                    table.CheckConstraint("ck_notification_subscriptions_quiet_hours_paired", "(quiet_hours_start_minute_ist IS NULL) = (quiet_hours_end_minute_ist IS NULL)");
                });

            migrationBuilder.CreateTable(
                name: "notification_templates",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "citext", maxLength: 96, nullable: false),
                    channel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    locale = table.Column<string>(type: "citext", maxLength: 16, nullable: false),
                    subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    body = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    provider_template_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    sender_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_templates", x => x.id);
                    table.CheckConstraint("ck_notification_templates_registered_before_active", "NOT is_active OR channel NOT IN ('Sms', 'Whatsapp') OR provider_template_id IS NOT NULL");
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    event_type = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    error = table.Column<string>(type: "text", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                    table.CheckConstraint("ck_outbox_messages_attempts_not_negative", "attempts >= 0");
                    table.CheckConstraint("ck_outbox_messages_payload_is_object", "jsonb_typeof(payload) = 'object'");
                    table.CheckConstraint("ck_outbox_messages_processed_has_no_error", "processed_at IS NULL OR error IS NULL");
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 128, nullable: false),
                    module = table.Column<string>(type: "citext", maxLength: 48, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_dangerous = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                    table.CheckConstraint("ck_permissions_module_is_code_prefix", "code LIKE module || '.%'");
                });

            migrationBuilder.CreateTable(
                name: "pricing_rule_sets",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pricing_rule_sets", x => x.id);
                    table.CheckConstraint("ck_pricing_rule_sets_publish_state", "(is_published AND published_at IS NOT NULL) OR (NOT is_published AND published_at IS NULL)");
                    table.CheckConstraint("ck_pricing_rule_sets_version_positive", "version >= 1");
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_categories",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 512, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    icon = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_vehicle_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicle_categories_vehicle_categories_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "platform",
                        principalTable: "vehicle_categories",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "vehicle_makes",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 512, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    country_of_origin = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: true),
                    logo_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_vehicle_makes", x => x.id);
                    table.CheckConstraint("ck_vehicle_makes_country_alpha2", "country_of_origin IS NULL OR country_of_origin ~ '^[A-Z]{2}$'");
                });

            migrationBuilder.CreateTable(
                name: "workflow_definitions",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_definitions", x => x.id);
                    table.CheckConstraint("ck_workflow_definitions_publish_state", "(is_published AND published_at IS NOT NULL) OR (NOT is_published AND published_at IS NULL)");
                    table.CheckConstraint("ck_workflow_definitions_version_positive", "version >= 1");
                });

            migrationBuilder.CreateTable(
                name: "workflow_instances",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_key = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    definition_version = table.Column<int>(type: "integer", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_state_code = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    state_entered_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    due_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_instances", x => x.id);
                    table.CheckConstraint("ck_workflow_instances_completed_has_no_timer", "completed_at IS NULL OR due_at IS NULL");
                    table.CheckConstraint("ck_workflow_instances_entered_after_started", "state_entered_at >= started_at");
                    table.CheckConstraint("ck_workflow_instances_version_positive", "definition_version >= 1");
                });

            migrationBuilder.CreateTable(
                name: "attribute_options",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    attribute_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "citext", maxLength: 512, nullable: false),
                    label = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attribute_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_attribute_options_attribute_definitions_attribute_definitio~",
                        column: x => x.attribute_definition_id,
                        principalSchema: "platform",
                        principalTable: "attribute_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "entity_attribute_values",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attribute_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value_text = table.Column<string>(type: "citext", maxLength: 512, nullable: true),
                    value_number = table.Column<decimal>(type: "numeric", nullable: true),
                    value_bool = table.Column<bool>(type: "boolean", nullable: true),
                    value_date = table.Column<DateOnly>(type: "date", nullable: true),
                    value_money_minor_units = table.Column<long>(type: "bigint", nullable: true),
                    value_currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: true),
                    value_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_entity_attribute_values", x => x.id);
                    table.CheckConstraint("ck_entity_attribute_values_has_a_value", "num_nonnulls(value_text, value_number, value_bool, value_date, value_money_minor_units, value_json) >= 1");
                    table.CheckConstraint("ck_entity_attribute_values_money_pair", "(value_money_minor_units IS NULL) = (value_currency IS NULL)");
                    table.ForeignKey(
                        name: "fk_entity_attribute_values_attribute_definitions_attribute_def~",
                        column: x => x.attribute_definition_id,
                        principalSchema: "platform",
                        principalTable: "attribute_definitions",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "states",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 2, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    gst_state_code = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_states", x => x.id);
                    table.CheckConstraint("ck_states_code_shape", "code ~ '^[A-Z]{2}$'");
                    table.CheckConstraint("ck_states_gst_code_shape", "gst_state_code IS NULL OR gst_state_code ~ '^[0-9]{2}$'");
                    table.ForeignKey(
                        name: "fk_states_countries_country_id",
                        column: x => x.country_id,
                        principalSchema: "platform",
                        principalTable: "countries",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "flag_rules",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature_flag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    condition_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: true),
                    value = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_flag_rules", x => x.id);
                    table.CheckConstraint("ck_flag_rules_condition_shape", "condition_json IS NULL OR jsonb_typeof(condition_json) IN ('object', 'boolean')");
                    table.ForeignKey(
                        name: "fk_flag_rules_feature_flags_feature_flag_id",
                        column: x => x.feature_flag_id,
                        principalSchema: "platform",
                        principalTable: "feature_flags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "form_versions",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    submit_label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    submit_label_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    variant_key = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    selector_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    schema_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_form_versions", x => x.id);
                    table.CheckConstraint("ck_form_versions_published_is_compiled", "(is_published AND published_at IS NOT NULL AND schema_json IS NOT NULL) OR NOT is_published");
                    table.CheckConstraint("ck_form_versions_version_positive", "version >= 1");
                    table.ForeignKey(
                        name: "fk_form_versions_form_definitions_form_definition_id",
                        column: x => x.form_definition_id,
                        principalSchema: "platform",
                        principalTable: "form_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "integration_providers",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    capability_code = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    environment = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    config_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: true),
                    credential_ref = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    timeout_seconds = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integration_providers", x => x.id);
                    table.CheckConstraint("ck_integration_providers_attempts_positive", "max_attempts >= 1 AND max_attempts <= 5");
                    table.CheckConstraint("ck_integration_providers_config_is_object", "config_json IS NULL OR jsonb_typeof(config_json) = 'object'");
                    table.CheckConstraint("ck_integration_providers_credential_is_a_reference", "credential_ref ~ '^[a-z][a-z0-9+.-]*:' AND length(credential_ref) <= 128");
                    table.CheckConstraint("ck_integration_providers_timeout_positive", "timeout_seconds > 0 AND timeout_seconds <= 300");
                    table.ForeignKey(
                        name: "fk_integration_providers_integration_capabilities_capability_c~",
                        column: x => x.capability_code,
                        principalSchema: "platform",
                        principalTable: "integration_capabilities",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "provider_routes",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    capability_code = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    provider_code = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    condition_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: true),
                    traffic_percent = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    effective_to = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_provider_routes", x => x.id);
                    table.CheckConstraint("ck_provider_routes_condition_shape", "condition_json IS NULL OR jsonb_typeof(condition_json) IN ('object', 'boolean')");
                    table.CheckConstraint("ck_provider_routes_traffic_percent", "traffic_percent IS NULL OR traffic_percent BETWEEN 1 AND 100");
                    table.CheckConstraint("ck_provider_routes_window_ordered", "effective_from IS NULL OR effective_to IS NULL OR effective_to > effective_from");
                    table.ForeignKey(
                        name: "fk_provider_routes_integration_capabilities_capability_code",
                        column: x => x.capability_code,
                        principalSchema: "platform",
                        principalTable: "integration_capabilities",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_channel_preferences",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_channel_preferences", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_channel_preferences_notification_subscriptions~",
                        column: x => x.notification_subscription_id,
                        principalSchema: "platform",
                        principalTable: "notification_subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_template_variables",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    sample_value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_template_variables", x => x.id);
                    table.CheckConstraint("ck_notification_template_variables_name_shape", "name ~ '^[a-zA-Z][a-zA-Z0-9_]*$'");
                    table.ForeignKey(
                        name: "fk_notification_template_variables_notification_templates_noti~",
                        column: x => x.notification_template_id,
                        principalSchema: "platform",
                        principalTable: "notification_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pricing_rules",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_set_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    condition_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: true),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    effective_to = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    explanation = table.Column<string>(type: "text", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pricing_rules", x => x.id);
                    table.CheckConstraint("ck_pricing_rules_effective_window", "effective_from IS NULL OR effective_to IS NULL OR effective_to > effective_from");
                    table.ForeignKey(
                        name: "fk_pricing_rules_pricing_rule_sets_rule_set_id",
                        column: x => x.rule_set_id,
                        principalSchema: "platform",
                        principalTable: "pricing_rule_sets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalSchema: "platform",
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "platform",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_role_assignments",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: true),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_role_assignments", x => x.id);
                    table.CheckConstraint("ck_user_role_assignments_scope_id_matches_type", "(scope_type = 'Global' AND scope_id IS NULL) OR (scope_type <> 'Global' AND scope_id IS NOT NULL)");
                    table.CheckConstraint("ck_user_role_assignments_window_ordered", "valid_from IS NULL OR valid_to IS NULL OR valid_to > valid_from");
                    table.ForeignKey(
                        name: "fk_user_role_assignments_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "platform",
                        principalTable: "roles",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "category_attributes",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attribute_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_required_override = table.Column<bool>(type: "boolean", nullable: true),
                    is_searchable_override = table.Column<bool>(type: "boolean", nullable: true),
                    facet_style_override = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    display_group_override = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    sort_order_override = table.Column<int>(type: "integer", nullable: true),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category_attributes", x => x.id);
                    table.ForeignKey(
                        name: "fk_category_attributes_attribute_definitions_attribute_definit~",
                        column: x => x.attribute_definition_id,
                        principalSchema: "platform",
                        principalTable: "attribute_definitions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_category_attributes_vehicle_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "platform",
                        principalTable: "vehicle_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_models",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    make_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 512, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    production_from = table.Column<int>(type: "integer", nullable: true),
                    production_to = table.Column<int>(type: "integer", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_vehicle_models", x => x.id);
                    table.CheckConstraint("ck_vehicle_models_production_range", "production_from IS NULL OR production_to IS NULL OR production_to >= production_from");
                    table.ForeignKey(
                        name: "fk_vehicle_models_vehicle_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "platform",
                        principalTable: "vehicle_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_vehicle_models_vehicle_makes_make_id",
                        column: x => x.make_id,
                        principalSchema: "platform",
                        principalTable: "vehicle_makes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "workflow_states",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_initial = table.Column<bool>(type: "boolean", nullable: false),
                    is_terminal = table.Column<bool>(type: "boolean", nullable: false),
                    sla_minutes = table.Column<int>(type: "integer", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    workflow_definition_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_states", x => x.id);
                    table.CheckConstraint("ck_workflow_states_not_initial_and_terminal", "NOT (is_initial AND is_terminal)");
                    table.CheckConstraint("ck_workflow_states_sla_positive", "sla_minutes IS NULL OR sla_minutes > 0");
                    table.ForeignKey(
                        name: "fk_workflow_states_workflow_definitions_workflow_definition_id",
                        column: x => x.workflow_definition_id,
                        principalSchema: "platform",
                        principalTable: "workflow_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_transitions",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_code = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    from_state_code = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    to_state_code = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    required_permission = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    guard_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: true),
                    requires_reason = table.Column<bool>(type: "boolean", nullable: false),
                    automatic_after_minutes = table.Column<int>(type: "integer", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    workflow_definition_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_transitions", x => x.id);
                    table.CheckConstraint("ck_workflow_transitions_timer_positive", "automatic_after_minutes IS NULL OR automatic_after_minutes > 0");
                    table.ForeignKey(
                        name: "fk_workflow_transitions_workflow_definitions_workflow_definiti~",
                        column: x => x.workflow_definition_id,
                        principalSchema: "platform",
                        principalTable: "workflow_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_history_entries",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_state_code = table.Column<string>(type: "citext", maxLength: 64, nullable: true),
                    to_state_code = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    action_code = table.Column<string>(type: "citext", maxLength: 64, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    context_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: true),
                    time_in_previous_state = table.Column<TimeSpan>(type: "interval", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_history_entries", x => x.id);
                    table.CheckConstraint("ck_workflow_history_entries_origin_consistent", "(from_state_code IS NULL AND action_code IS NULL) OR (from_state_code IS NOT NULL AND action_code IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_workflow_history_entries_workflow_instances_workflow_instan~",
                        column: x => x.workflow_instance_id,
                        principalSchema: "platform",
                        principalTable: "workflow_instances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cities",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    state_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    is_serviceable = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    enabled_modules = table.Column<List<string>>(type: "text[]", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cities", x => x.id);
                    table.CheckConstraint("ck_cities_coordinates_in_range", "latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180");
                    table.ForeignKey(
                        name: "fk_cities_states_state_id",
                        column: x => x.state_id,
                        principalSchema: "platform",
                        principalTable: "states",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "form_steps",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    description_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_form_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_form_steps_form_versions_form_version_id",
                        column: x => x.form_version_id,
                        principalSchema: "platform",
                        principalTable: "form_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fee_components",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pricing_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    label = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    basis = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    flat_minor_units = table.Column<long>(type: "bigint", nullable: true),
                    min_minor_units = table.Column<long>(type: "bigint", nullable: true),
                    max_minor_units = table.Column<long>(type: "bigint", nullable: true),
                    applies_to_csv = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fee_components", x => x.id);
                    table.CheckConstraint("ck_fee_components_basis_has_its_input", "(basis = 'Flat' AND flat_minor_units IS NOT NULL) OR (basis IN ('Percent', 'PercentOfLines') AND rate IS NOT NULL) OR (basis IN ('Slab', 'Tiered'))");
                    table.CheckConstraint("ck_fee_components_clamp_ordered", "min_minor_units IS NULL OR max_minor_units IS NULL OR max_minor_units >= min_minor_units");
                    table.CheckConstraint("ck_fee_components_rate_is_a_fraction", "rate IS NULL OR (rate >= 0 AND rate <= 1)");
                    table.ForeignKey(
                        name: "fk_fee_components_pricing_rules_pricing_rule_id",
                        column: x => x.pricing_rule_id,
                        principalSchema: "platform",
                        principalTable: "pricing_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_variants",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 512, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    year_from = table.Column<int>(type: "integer", nullable: true),
                    year_to = table.Column<int>(type: "integer", nullable: true),
                    fuel_code = table.Column<string>(type: "citext", maxLength: 512, nullable: true),
                    transmission_code = table.Column<string>(type: "citext", maxLength: 512, nullable: true),
                    body_type_code = table.Column<string>(type: "citext", maxLength: 512, nullable: true),
                    seats = table.Column<int>(type: "integer", nullable: true),
                    engine_cc = table.Column<int>(type: "integer", nullable: true),
                    power_bhp = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    spec_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_vehicle_variants", x => x.id);
                    table.CheckConstraint("ck_vehicle_variants_engine_cc_positive", "engine_cc IS NULL OR engine_cc > 0");
                    table.CheckConstraint("ck_vehicle_variants_seats_positive", "seats IS NULL OR seats > 0");
                    table.CheckConstraint("ck_vehicle_variants_spec_is_object", "spec_json IS NULL OR jsonb_typeof(spec_json) = 'object'");
                    table.CheckConstraint("ck_vehicle_variants_year_range", "year_from IS NULL OR year_to IS NULL OR year_to >= year_from");
                    table.ForeignKey(
                        name: "fk_vehicle_variants_vehicle_models_model_id",
                        column: x => x.model_id,
                        principalSchema: "platform",
                        principalTable: "vehicle_models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_effects",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    effect_code = table.Column<string>(type: "citext", maxLength: 96, nullable: false),
                    arguments_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: true),
                    continue_on_failure = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    workflow_transition_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_effects", x => x.id);
                    table.CheckConstraint("ck_workflow_effects_arguments_are_object", "arguments_json IS NULL OR jsonb_typeof(arguments_json) = 'object'");
                    table.ForeignKey(
                        name: "fk_workflow_effects_workflow_transitions_workflow_transition_id",
                        column: x => x.workflow_transition_id,
                        principalSchema: "platform",
                        principalTable: "workflow_transitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pincodes",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character(6)", fixedLength: true, maxLength: 6, nullable: false),
                    state_id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: true),
                    district = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
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
                    table.PrimaryKey("pk_pincodes", x => x.id);
                    table.CheckConstraint("ck_pincodes_coordinates_in_range", "(latitude IS NULL OR latitude BETWEEN -90 AND 90) AND (longitude IS NULL OR longitude BETWEEN -180 AND 180)");
                    table.CheckConstraint("ck_pincodes_coordinates_paired", "(latitude IS NULL) = (longitude IS NULL)");
                    table.CheckConstraint("ck_pincodes_shape", "code ~ '^[1-9][0-9]{5}$'");
                    table.ForeignKey(
                        name: "fk_pincodes_cities_city_id",
                        column: x => x.city_id,
                        principalSchema: "platform",
                        principalTable: "cities",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_pincodes_states_state_id",
                        column: x => x.state_id,
                        principalSchema: "platform",
                        principalTable: "states",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "rto_offices",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    state_id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "citext", maxLength: 4, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    address = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
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
                    table.PrimaryKey("pk_rto_offices", x => x.id);
                    table.CheckConstraint("ck_rto_offices_code_shape", "code ~ '^[A-Z]{2}[0-9]{1,2}$'");
                    table.ForeignKey(
                        name: "fk_rto_offices_cities_city_id",
                        column: x => x.city_id,
                        principalSchema: "platform",
                        principalTable: "cities",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_rto_offices_states_state_id",
                        column: x => x.state_id,
                        principalSchema: "platform",
                        principalTable: "states",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "service_areas",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    module_code = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    centre_latitude = table.Column<double>(type: "double precision", nullable: true),
                    centre_longitude = table.Column<double>(type: "double precision", nullable: true),
                    radius_metres = table.Column<int>(type: "integer", nullable: true),
                    boundary_geo_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_service_areas", x => x.id);
                    table.CheckConstraint("ck_service_areas_centre_in_range", "(centre_latitude IS NULL OR centre_latitude BETWEEN -90 AND 90) AND (centre_longitude IS NULL OR centre_longitude BETWEEN -180 AND 180)");
                    table.CheckConstraint("ck_service_areas_shape_complete", "(kind = 'Radius' AND centre_latitude IS NOT NULL AND centre_longitude IS NOT NULL AND radius_metres IS NOT NULL AND radius_metres > 0) OR (kind = 'Polygon' AND boundary_geo_json IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_service_areas_cities_city_id",
                        column: x => x.city_id,
                        principalSchema: "platform",
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "form_fields",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_step_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "citext", maxLength: 64, nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    label_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    control = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    required = table.Column<bool>(type: "boolean", nullable: false),
                    unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    placeholder = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    help_text = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    attribute_code = table.Column<string>(type: "citext", maxLength: 64, nullable: true),
                    config_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: true),
                    visible_when_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_form_fields", x => x.id);
                    table.CheckConstraint("ck_form_fields_config_is_object", "config_json IS NULL OR jsonb_typeof(config_json) = 'object'");
                    table.CheckConstraint("ck_form_fields_visible_when_shape", "visible_when_json IS NULL OR jsonb_typeof(visible_when_json) IN ('object', 'boolean')");
                    table.ForeignKey(
                        name: "fk_form_fields_form_steps_form_step_id",
                        column: x => x.form_step_id,
                        principalSchema: "platform",
                        principalTable: "form_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fee_slabs",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fee_component_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    to_minor_units = table.Column<long>(type: "bigint", nullable: true),
                    rate = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    flat_minor_units = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fee_slabs", x => x.id);
                    table.CheckConstraint("ck_fee_slabs_band_ordered", "to_minor_units IS NULL OR to_minor_units > from_minor_units");
                    table.CheckConstraint("ck_fee_slabs_from_not_negative", "from_minor_units >= 0");
                    table.CheckConstraint("ck_fee_slabs_has_a_charge", "rate IS NOT NULL OR flat_minor_units IS NOT NULL");
                    table.CheckConstraint("ck_fee_slabs_rate_is_a_fraction", "rate IS NULL OR (rate >= 0 AND rate <= 1)");
                    table.ForeignKey(
                        name: "fk_fee_slabs_fee_components_fee_component_id",
                        column: x => x.fee_component_id,
                        principalSchema: "platform",
                        principalTable: "fee_components",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "form_validation_rules",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    value_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: true),
                    code = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_form_validation_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_form_validation_rules_form_fields_form_field_id",
                        column: x => x.form_field_id,
                        principalSchema: "platform",
                        principalTable: "form_fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_attribute_definitions_code",
                schema: "platform",
                table: "attribute_definitions",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_attribute_definitions_is_searchable_is_active",
                schema: "platform",
                table: "attribute_definitions",
                columns: new[] { "is_searchable", "is_active" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_attribute_options_attribute_definition_id_sort_order",
                schema: "platform",
                table: "attribute_options",
                columns: new[] { "attribute_definition_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_attribute_options_attribute_definition_id_value",
                schema: "platform",
                table: "attribute_options",
                columns: new[] { "attribute_definition_id", "value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_category_attributes_attribute_definition_id",
                schema: "platform",
                table: "category_attributes",
                column: "attribute_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_category_attributes_category_id_attribute_definition_id",
                schema: "platform",
                table: "category_attributes",
                columns: new[] { "category_id", "attribute_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cities_code",
                schema: "platform",
                table: "cities",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_cities_is_serviceable_sort_order",
                schema: "platform",
                table: "cities",
                columns: new[] { "is_serviceable", "sort_order" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_cities_name",
                schema: "platform",
                table: "cities",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_cities_state_id",
                schema: "platform",
                table: "cities",
                column: "state_id");

            migrationBuilder.CreateIndex(
                name: "ix_countries_code",
                schema: "platform",
                table: "countries",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_entity_attribute_values_attribute_definition_id_value_date",
                schema: "platform",
                table: "entity_attribute_values",
                columns: new[] { "attribute_definition_id", "value_date" },
                filter: "value_date IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_entity_attribute_values_attribute_definition_id_value_money~",
                schema: "platform",
                table: "entity_attribute_values",
                columns: new[] { "attribute_definition_id", "value_money_minor_units" },
                filter: "value_money_minor_units IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_entity_attribute_values_attribute_definition_id_value_number",
                schema: "platform",
                table: "entity_attribute_values",
                columns: new[] { "attribute_definition_id", "value_number" },
                filter: "value_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_entity_attribute_values_attribute_definition_id_value_text",
                schema: "platform",
                table: "entity_attribute_values",
                columns: new[] { "attribute_definition_id", "value_text" },
                filter: "value_text IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_entity_attribute_values_entity_type_entity_id",
                schema: "platform",
                table: "entity_attribute_values",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_entity_attribute_values_entity_type_entity_id_attribute_def~",
                schema: "platform",
                table: "entity_attribute_values",
                columns: new[] { "entity_type", "entity_id", "attribute_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_entity_attribute_values_value_json",
                schema: "platform",
                table: "entity_attribute_values",
                column: "value_json",
                filter: "value_json IS NOT NULL")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_feature_flags_key",
                schema: "platform",
                table: "feature_flags",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fee_components_pricing_rule_id_code",
                schema: "platform",
                table: "fee_components",
                columns: new[] { "pricing_rule_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fee_components_pricing_rule_id_sort_order",
                schema: "platform",
                table: "fee_components",
                columns: new[] { "pricing_rule_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_fee_slabs_fee_component_id_from_minor_units",
                schema: "platform",
                table: "fee_slabs",
                columns: new[] { "fee_component_id", "from_minor_units" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_flag_rules_feature_flag_id_priority",
                schema: "platform",
                table: "flag_rules",
                columns: new[] { "feature_flag_id", "priority" },
                unique: true,
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_form_definitions_key",
                schema: "platform",
                table: "form_definitions",
                column: "key",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_form_fields_attribute_code",
                schema: "platform",
                table: "form_fields",
                column: "attribute_code",
                filter: "attribute_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_form_fields_form_step_id_key",
                schema: "platform",
                table: "form_fields",
                columns: new[] { "form_step_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_form_fields_form_step_id_sort_order",
                schema: "platform",
                table: "form_fields",
                columns: new[] { "form_step_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_form_steps_form_version_id_key",
                schema: "platform",
                table: "form_steps",
                columns: new[] { "form_version_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_form_steps_form_version_id_sort_order",
                schema: "platform",
                table: "form_steps",
                columns: new[] { "form_version_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_form_validation_rules_form_field_id_sort_order",
                schema: "platform",
                table: "form_validation_rules",
                columns: new[] { "form_field_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_form_versions_form_definition_id_priority",
                schema: "platform",
                table: "form_versions",
                columns: new[] { "form_definition_id", "priority" },
                descending: new[] { false, true },
                filter: "is_published");

            migrationBuilder.CreateIndex(
                name: "ix_form_versions_form_definition_id_version",
                schema: "platform",
                table: "form_versions",
                columns: new[] { "form_definition_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_form_versions_schema_json",
                schema: "platform",
                table: "form_versions",
                column: "schema_json",
                filter: "schema_json IS NOT NULL")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_i18n_locales_code",
                schema: "platform",
                table: "i18n_locales",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_i18n_resources_locale_namespace",
                schema: "platform",
                table: "i18n_resources",
                columns: new[] { "locale", "namespace" },
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_i18n_resources_namespace_key_locale",
                schema: "platform",
                table: "i18n_resources",
                columns: new[] { "namespace", "key", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_records_expires_at",
                schema: "platform",
                table: "idempotency_records",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_records_user_id_created_at",
                schema: "platform",
                table: "idempotency_records",
                columns: new[] { "user_id", "created_at" },
                filter: "user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_integration_capabilities_code",
                schema: "platform",
                table: "integration_capabilities",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_integration_providers_capability_code_code_environment",
                schema: "platform",
                table: "integration_providers",
                columns: new[] { "capability_code", "code", "environment" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_modules_code",
                schema: "platform",
                table: "modules",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_channel_preferences_notification_subscription_~",
                schema: "platform",
                table: "notification_channel_preferences",
                columns: new[] { "notification_subscription_id", "channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_events_active_by_event_type",
                schema: "platform",
                table: "notification_events",
                column: "event_type",
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_notification_events_code",
                schema: "platform",
                table: "notification_events",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_subscriptions_user_id_category_code",
                schema: "platform",
                table: "notification_subscriptions",
                columns: new[] { "user_id", "category_code" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_notification_template_variables_notification_template_id_na~",
                schema: "platform",
                table: "notification_template_variables",
                columns: new[] { "notification_template_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_templates_key_channel_locale",
                schema: "platform",
                table: "notification_templates",
                columns: new[] { "key", "channel", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_pending",
                schema: "platform",
                table: "outbox_messages",
                columns: new[] { "next_attempt_at", "aggregate_id", "occurred_at" },
                filter: "processed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_pending_by_aggregate",
                schema: "platform",
                table: "outbox_messages",
                columns: new[] { "aggregate_id", "occurred_at" },
                filter: "processed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_processed",
                schema: "platform",
                table: "outbox_messages",
                column: "processed_at",
                filter: "processed_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_permissions_code",
                schema: "platform",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_permissions_module",
                schema: "platform",
                table: "permissions",
                column: "module");

            migrationBuilder.CreateIndex(
                name: "ix_pincodes_city_id",
                schema: "platform",
                table: "pincodes",
                column: "city_id",
                filter: "city_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pincodes_code",
                schema: "platform",
                table: "pincodes",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pincodes_state_id",
                schema: "platform",
                table: "pincodes",
                column: "state_id");

            migrationBuilder.CreateIndex(
                name: "ix_pricing_rule_sets_scope_version",
                schema: "platform",
                table: "pricing_rule_sets",
                columns: new[] { "scope", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_pricing_rule_sets_one_published_per_scope",
                schema: "platform",
                table: "pricing_rule_sets",
                column: "scope",
                unique: true,
                filter: "is_published");

            migrationBuilder.CreateIndex(
                name: "ix_pricing_rules_rule_set_id_code",
                schema: "platform",
                table: "pricing_rules",
                columns: new[] { "rule_set_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pricing_rules_rule_set_id_priority",
                schema: "platform",
                table: "pricing_rules",
                columns: new[] { "rule_set_id", "priority" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_provider_routes_capability_code_priority",
                schema: "platform",
                table: "provider_routes",
                columns: new[] { "capability_code", "priority" });

            migrationBuilder.CreateIndex(
                name: "ix_provider_routes_capability_code_provider_code",
                schema: "platform",
                table: "provider_routes",
                columns: new[] { "capability_code", "provider_code" },
                unique: true,
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_permission_id",
                schema: "platform",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_role_id_permission_id",
                schema: "platform",
                table: "role_permissions",
                columns: new[] { "role_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_roles_code",
                schema: "platform",
                table: "roles",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_rto_offices_city_id",
                schema: "platform",
                table: "rto_offices",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "ix_rto_offices_code",
                schema: "platform",
                table: "rto_offices",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_rto_offices_state_id",
                schema: "platform",
                table: "rto_offices",
                column: "state_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_areas_city_id_code",
                schema: "platform",
                table: "service_areas",
                columns: new[] { "city_id", "code" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_service_areas_city_id_module_code_priority",
                schema: "platform",
                table: "service_areas",
                columns: new[] { "city_id", "module_code", "priority" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_states_code",
                schema: "platform",
                table: "states",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_states_country_id_sort_order",
                schema: "platform",
                table: "states",
                columns: new[] { "country_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_user_role_assignments_role_id",
                schema: "platform",
                table: "user_role_assignments",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_role_assignments_scope_type_scope_id",
                schema: "platform",
                table: "user_role_assignments",
                columns: new[] { "scope_type", "scope_id" },
                filter: "scope_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_role_assignments_user_id",
                schema: "platform",
                table: "user_role_assignments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_role_assignments_user_id_role_id_scope_type_scope_id",
                schema: "platform",
                table: "user_role_assignments",
                columns: new[] { "user_id", "role_id", "scope_type", "scope_id" },
                unique: true,
                filter: "valid_to IS NULL")
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_categories_code",
                schema: "platform",
                table: "vehicle_categories",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_categories_parent_id_sort_order",
                schema: "platform",
                table: "vehicle_categories",
                columns: new[] { "parent_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_makes_code",
                schema: "platform",
                table: "vehicle_makes",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_makes_name",
                schema: "platform",
                table: "vehicle_makes",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_models_category_id_is_active",
                schema: "platform",
                table: "vehicle_models",
                columns: new[] { "category_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_models_code",
                schema: "platform",
                table: "vehicle_models",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_models_make_id_sort_order",
                schema: "platform",
                table: "vehicle_models",
                columns: new[] { "make_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_models_name",
                schema: "platform",
                table: "vehicle_models",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_variants_model_id_code",
                schema: "platform",
                table: "vehicle_variants",
                columns: new[] { "model_id", "code" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_variants_model_id_is_active_sort_order",
                schema: "platform",
                table: "vehicle_variants",
                columns: new[] { "model_id", "is_active", "sort_order" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_variants_spec_json",
                schema: "platform",
                table: "vehicle_variants",
                column: "spec_json")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_definitions_published_key_version",
                schema: "platform",
                table: "workflow_definitions",
                columns: new[] { "key", "version" },
                unique: true,
                descending: new[] { false, true },
                filter: "is_published");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_effects_workflow_transition_id_sort_order",
                schema: "platform",
                table: "workflow_effects",
                columns: new[] { "workflow_transition_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_history_entries_workflow_instance_id_occurred_at",
                schema: "platform",
                table: "workflow_history_entries",
                columns: new[] { "workflow_instance_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_instances_due",
                schema: "platform",
                table: "workflow_instances",
                column: "due_at",
                filter: "due_at IS NOT NULL AND completed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_instances_open_by_state",
                schema: "platform",
                table: "workflow_instances",
                columns: new[] { "workflow_key", "current_state_code", "state_entered_at" },
                filter: "completed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_instances_workflow_key_entity_id",
                schema: "platform",
                table: "workflow_instances",
                columns: new[] { "workflow_key", "entity_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workflow_states_workflow_definition_id_code",
                schema: "platform",
                table: "workflow_states",
                columns: new[] { "workflow_definition_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_workflow_states_one_initial_per_definition",
                schema: "platform",
                table: "workflow_states",
                column: "workflow_definition_id",
                unique: true,
                filter: "is_initial");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_transitions_workflow_definition_id_automatic_after~",
                schema: "platform",
                table: "workflow_transitions",
                columns: new[] { "workflow_definition_id", "automatic_after_minutes" },
                filter: "automatic_after_minutes IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_transitions_workflow_definition_id_from_state_code~",
                schema: "platform",
                table: "workflow_transitions",
                columns: new[] { "workflow_definition_id", "from_state_code", "action_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attribute_options",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "category_attributes",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "entity_attribute_values",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "fee_slabs",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "flag_rules",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "form_validation_rules",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "i18n_locales",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "i18n_resources",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "idempotency_records",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "integration_providers",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "modules",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "notification_channel_preferences",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "notification_events",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "notification_template_variables",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "pincodes",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "provider_routes",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "role_permissions",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "rto_offices",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "service_areas",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "user_role_assignments",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "vehicle_variants",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "workflow_effects",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "workflow_history_entries",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "workflow_states",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "attribute_definitions",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "fee_components",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "feature_flags",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "form_fields",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "notification_subscriptions",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "notification_templates",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "integration_capabilities",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "permissions",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "cities",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "vehicle_models",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "workflow_transitions",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "workflow_instances",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "pricing_rules",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "form_steps",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "states",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "vehicle_categories",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "vehicle_makes",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "workflow_definitions",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "pricing_rule_sets",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "form_versions",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "countries",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "form_definitions",
                schema: "platform");
        }
    }
}
