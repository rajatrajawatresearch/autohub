using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ModuleTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "listings");

            migrationBuilder.EnsureSchema(
                name: "valuation");

            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.CreateTable(
                name: "listing_search_documents",
                schema: "listings",
                columns: table => new
                {
                    listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    intent = table.Column<int>(type: "integer", nullable: false),
                    price_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    city_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    quality_score = table.Column<int>(type: "integer", nullable: false),
                    media_count = table.Column<int>(type: "integer", nullable: false),
                    cover_object_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    cover_aspect_ratio = table.Column<double>(type: "double precision", nullable: true),
                    search_text = table.Column<string>(type: "text", maxLength: 512, nullable: false),
                    kilometres_driven = table.Column<long>(type: "bigint", nullable: true),
                    indexed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_listing_search_documents", x => x.listing_id);
                });

            migrationBuilder.CreateTable(
                name: "listings",
                schema: "listings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    intent = table.Column<int>(type: "integer", nullable: false),
                    seller_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    price_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    price_currency_code = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    price_period_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    description = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    form_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    form_version = table.Column<int>(type: "integer", nullable: false),
                    quality_score = table.Column<int>(type: "integer", nullable: false),
                    quality_signals_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: true),
                    attributes_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: true),
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
                    table.PrimaryKey("pk_listings", x => x.id);
                    table.CheckConstraint("ck_listings_price_non_negative", "price_minor_units >= 0");
                    table.CheckConstraint("ck_listings_quality_score", "quality_score BETWEEN 0 AND 100");
                });

            migrationBuilder.CreateTable(
                name: "valuation_models",
                schema: "valuation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    condition_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    validity_days = table.Column<int>(type: "integer", nullable: false),
                    forecast_months = table.Column<int>(type: "integer", nullable: false),
                    resale_horizon_months = table.Column<int>(type: "integer", nullable: false),
                    explanation_template = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    required_inputs_csv = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_valuation_models", x => x.id);
                    table.CheckConstraint("ck_valuation_models_horizons", "forecast_months > 0 AND resale_horizon_months > 0 AND validity_days > 0");
                });

            migrationBuilder.CreateTable(
                name: "valuations",
                schema: "valuation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    registration_canonical = table.Column<string>(type: "citext", maxLength: 512, nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    model_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    model_version = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    currency_code = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    point_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    low_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    high_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    confidence = table.Column<decimal>(type: "numeric(4,3)", nullable: false),
                    confidence_band_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    inputs_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: false),
                    forecast_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: true),
                    explanation = table.Column<string>(type: "text", maxLength: 512, nullable: true),
                    as_of = table.Column<DateOnly>(type: "date", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    is_shadow = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_valuations", x => x.id);
                    table.CheckConstraint("ck_valuations_confidence", "confidence >= 0 AND confidence <= 1");
                    table.CheckConstraint("ck_valuations_range", "low_minor_units <= high_minor_units");
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    make_id = table.Column<Guid>(type: "uuid", nullable: true),
                    model_id = table.Column<Guid>(type: "uuid", nullable: true),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    manufacture_year = table.Column<int>(type: "integer", nullable: true),
                    manufacture_month = table.Column<int>(type: "integer", nullable: true),
                    first_registered_on = table.Column<DateOnly>(type: "date", nullable: true),
                    provenance_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    attributes_json = table.Column<string>(type: "jsonb", maxLength: 512, nullable: true),
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
                    table.PrimaryKey("pk_vehicles", x => x.id);
                    table.CheckConstraint("ck_vehicles_manufacture_month", "manufacture_month IS NULL OR (manufacture_month BETWEEN 1 AND 12)");
                });

            migrationBuilder.CreateTable(
                name: "listing_media",
                schema: "listings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    object_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    media_type_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    prompt_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_cover = table.Column<bool>(type: "boolean", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    content_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_listing_media", x => x.id);
                    table.CheckConstraint("ck_listing_media_dimensions", "(width IS NULL OR width > 0) AND (height IS NULL OR height > 0)");
                    table.ForeignKey(
                        name: "fk_listing_media_listings_listing_id",
                        column: x => x.listing_id,
                        principalSchema: "listings",
                        principalTable: "listings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "listing_offers",
                schema: "listings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_code = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
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
                    table.PrimaryKey("pk_listing_offers", x => x.id);
                    table.CheckConstraint("ck_listing_offers_distinct_parties", "buyer_party_id <> seller_party_id");
                    table.ForeignKey(
                        name: "fk_listing_offers_listings_listing_id",
                        column: x => x.listing_id,
                        principalSchema: "listings",
                        principalTable: "listings",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "valuation_channel_spreads",
                schema: "valuation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    spread = table.Column<decimal>(type: "numeric(10,6)", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_forecast = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_valuation_channel_spreads", x => x.id);
                    table.ForeignKey(
                        name: "fk_valuation_channel_spreads_valuation_models_model_definition~",
                        column: x => x.model_definition_id,
                        principalSchema: "valuation",
                        principalTable: "valuation_models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "valuation_confidence_bands",
                schema: "valuation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    minimum_score = table.Column<decimal>(type: "numeric(4,3)", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_valuation_confidence_bands", x => x.id);
                    table.CheckConstraint("ck_valuation_confidence_bands_range", "minimum_score >= 0 AND minimum_score <= 1");
                    table.ForeignKey(
                        name: "fk_valuation_confidence_bands_valuation_models_model_definitio~",
                        column: x => x.model_definition_id,
                        principalSchema: "valuation",
                        principalTable: "valuation_models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "valuation_confidence_weights",
                schema: "valuation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    input_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    weight = table.Column<decimal>(type: "numeric(6,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_valuation_confidence_weights", x => x.id);
                    table.ForeignKey(
                        name: "fk_valuation_confidence_weights_valuation_models_model_definit~",
                        column: x => x.model_definition_id,
                        principalSchema: "valuation",
                        principalTable: "valuation_models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "valuation_curves",
                schema: "valuation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_valuation_curves", x => x.id);
                    table.ForeignKey(
                        name: "fk_valuation_curves_valuation_models_model_definition_id",
                        column: x => x.model_definition_id,
                        principalSchema: "valuation",
                        principalTable: "valuation_models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "valuation_factors",
                schema: "valuation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    factor_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    match_value = table.Column<string>(type: "citext", maxLength: 512, nullable: false),
                    multiplier = table.Column<decimal>(type: "numeric(10,6)", nullable: true),
                    amount_minor_units = table.Column<long>(type: "bigint", nullable: true),
                    label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_valuation_factors", x => x.id);
                    table.CheckConstraint("ck_valuation_factors_one_value", "num_nonnulls(multiplier, amount_minor_units) = 1");
                    table.ForeignKey(
                        name: "fk_valuation_factors_valuation_models_model_definition_id",
                        column: x => x.model_definition_id,
                        principalSchema: "valuation",
                        principalTable: "valuation_models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "valuation_adjustments",
                schema: "valuation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    valuation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    factor_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    match_value = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    multiplier = table.Column<decimal>(type: "numeric(10,6)", nullable: true),
                    delta_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_valuation_adjustments", x => x.id);
                    table.ForeignKey(
                        name: "fk_valuation_adjustments_valuations_valuation_id",
                        column: x => x.valuation_id,
                        principalSchema: "valuation",
                        principalTable: "valuations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "valuation_channel_amounts",
                schema: "valuation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    valuation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    amount_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_valuation_channel_amounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_valuation_channel_amounts_valuations_valuation_id",
                        column: x => x.valuation_id,
                        principalSchema: "valuation",
                        principalTable: "valuations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_identities",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    identifier_type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<string>(type: "citext", maxLength: 512, nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    source_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicle_identities", x => x.id);
                    table.CheckConstraint("ck_vehicle_identities_period", "valid_to IS NULL OR valid_to >= valid_from");
                    table.ForeignKey(
                        name: "fk_vehicle_identities_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalSchema: "catalog",
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "listing_offer_turns",
                schema: "listings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    side = table.Column<int>(type: "integer", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_minor_units = table.Column<long>(type: "bigint", nullable: false),
                    currency_code = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    placed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_listing_offer_turns", x => x.id);
                    table.CheckConstraint("ck_listing_offer_turns_amount_positive", "amount_minor_units > 0");
                    table.ForeignKey(
                        name: "fk_listing_offer_turns_listing_offers_offer_id",
                        column: x => x.offer_id,
                        principalSchema: "listings",
                        principalTable: "listing_offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "valuation_curve_points",
                schema: "valuation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    curve_id = table.Column<Guid>(type: "uuid", nullable: false),
                    x = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    y = table.Column<decimal>(type: "numeric(12,6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_valuation_curve_points", x => x.id);
                    table.ForeignKey(
                        name: "fk_valuation_curve_points_valuation_curves_curve_id",
                        column: x => x.curve_id,
                        principalSchema: "valuation",
                        principalTable: "valuation_curves",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_listing_media_content_hash",
                schema: "listings",
                table: "listing_media",
                column: "content_hash",
                filter: "content_hash IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_listing_media_listing_id_sort_order",
                schema: "listings",
                table: "listing_media",
                columns: new[] { "listing_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ux_listing_media_cover",
                schema: "listings",
                table: "listing_media",
                column: "listing_id",
                unique: true,
                filter: "is_cover");

            migrationBuilder.CreateIndex(
                name: "ix_listing_offer_turns_offer_id_sequence",
                schema: "listings",
                table: "listing_offer_turns",
                columns: new[] { "offer_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_listing_offers_listing_id_buyer_party_id",
                schema: "listings",
                table: "listing_offers",
                columns: new[] { "listing_id", "buyer_party_id" });

            migrationBuilder.CreateIndex(
                name: "ix_listing_offers_seller_party_id_id",
                schema: "listings",
                table: "listing_offers",
                columns: new[] { "seller_party_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_listing_search_documents_category_id_price_minor_units",
                schema: "listings",
                table: "listing_search_documents",
                columns: new[] { "category_id", "price_minor_units" });

            migrationBuilder.CreateIndex(
                name: "ix_listing_search_documents_city_code_intent_listing_id",
                schema: "listings",
                table: "listing_search_documents",
                columns: new[] { "city_code", "intent", "listing_id" });

            migrationBuilder.CreateIndex(
                name: "ix_listing_search_documents_kilometres_driven",
                schema: "listings",
                table: "listing_search_documents",
                column: "kilometres_driven",
                filter: "kilometres_driven IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_listing_search_documents_search_text",
                schema: "listings",
                table: "listing_search_documents",
                column: "search_text")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_listings_attributes_json",
                schema: "listings",
                table: "listings",
                column: "attributes_json",
                filter: "attributes_json IS NOT NULL")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_listings_category_id_price_minor_units",
                schema: "listings",
                table: "listings",
                columns: new[] { "category_id", "price_minor_units" });

            migrationBuilder.CreateIndex(
                name: "ix_listings_city_code_intent_id",
                schema: "listings",
                table: "listings",
                columns: new[] { "city_code", "intent", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_listings_seller_party_id_id",
                schema: "listings",
                table: "listings",
                columns: new[] { "seller_party_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_listings_vehicle_id",
                schema: "listings",
                table: "listings",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ix_valuation_adjustments_valuation_id_sort_order",
                schema: "valuation",
                table: "valuation_adjustments",
                columns: new[] { "valuation_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_valuation_channel_amounts_valuation_id_channel_code",
                schema: "valuation",
                table: "valuation_channel_amounts",
                columns: new[] { "valuation_id", "channel_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_valuation_channel_spreads_model_definition_id_channel_code",
                schema: "valuation",
                table: "valuation_channel_spreads",
                columns: new[] { "model_definition_id", "channel_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_valuation_channel_spreads_primary",
                schema: "valuation",
                table: "valuation_channel_spreads",
                column: "model_definition_id",
                unique: true,
                filter: "is_primary");

            migrationBuilder.CreateIndex(
                name: "ix_valuation_confidence_bands_model_definition_id_code",
                schema: "valuation",
                table: "valuation_confidence_bands",
                columns: new[] { "model_definition_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_valuation_confidence_weights_model_definition_id_input_code",
                schema: "valuation",
                table: "valuation_confidence_weights",
                columns: new[] { "model_definition_id", "input_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_valuation_curve_points_curve_id_x",
                schema: "valuation",
                table: "valuation_curve_points",
                columns: new[] { "curve_id", "x" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_valuation_curves_model_definition_id_code",
                schema: "valuation",
                table: "valuation_curves",
                columns: new[] { "model_definition_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_valuation_factors_model_definition_id_factor_code_match_val~",
                schema: "valuation",
                table: "valuation_factors",
                columns: new[] { "model_definition_id", "factor_code", "match_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_valuation_models_is_published_priority",
                schema: "valuation",
                table: "valuation_models",
                columns: new[] { "is_published", "priority" });

            migrationBuilder.CreateIndex(
                name: "ix_valuation_models_key_version",
                schema: "valuation",
                table: "valuation_models",
                columns: new[] { "key", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_valuations_model_version_id",
                schema: "valuation",
                table: "valuations",
                columns: new[] { "model_version", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_valuations_registration_canonical",
                schema: "valuation",
                table: "valuations",
                column: "registration_canonical",
                filter: "registration_canonical IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_valuations_vehicle_id_expires_at",
                schema: "valuation",
                table: "valuations",
                columns: new[] { "vehicle_id", "expires_at" },
                filter: "is_shadow = false AND vehicle_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_identities_identifier_type_value_valid_from",
                schema: "catalog",
                table: "vehicle_identities",
                columns: new[] { "identifier_type", "value", "valid_from" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_identities_vehicle_id_identifier_type",
                schema: "catalog",
                table: "vehicle_identities",
                columns: new[] { "vehicle_id", "identifier_type" });

            migrationBuilder.CreateIndex(
                name: "ux_vehicle_identities_active",
                schema: "catalog",
                table: "vehicle_identities",
                columns: new[] { "identifier_type", "value" },
                unique: true,
                filter: "valid_to IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_attributes_json",
                schema: "catalog",
                table: "vehicles",
                column: "attributes_json",
                filter: "attributes_json IS NOT NULL")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_category_id",
                schema: "catalog",
                table: "vehicles",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_model_id_manufacture_year",
                schema: "catalog",
                table: "vehicles",
                columns: new[] { "model_id", "manufacture_year" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "listing_media",
                schema: "listings");

            migrationBuilder.DropTable(
                name: "listing_offer_turns",
                schema: "listings");

            migrationBuilder.DropTable(
                name: "listing_search_documents",
                schema: "listings");

            migrationBuilder.DropTable(
                name: "valuation_adjustments",
                schema: "valuation");

            migrationBuilder.DropTable(
                name: "valuation_channel_amounts",
                schema: "valuation");

            migrationBuilder.DropTable(
                name: "valuation_channel_spreads",
                schema: "valuation");

            migrationBuilder.DropTable(
                name: "valuation_confidence_bands",
                schema: "valuation");

            migrationBuilder.DropTable(
                name: "valuation_confidence_weights",
                schema: "valuation");

            migrationBuilder.DropTable(
                name: "valuation_curve_points",
                schema: "valuation");

            migrationBuilder.DropTable(
                name: "valuation_factors",
                schema: "valuation");

            migrationBuilder.DropTable(
                name: "vehicle_identities",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "listing_offers",
                schema: "listings");

            migrationBuilder.DropTable(
                name: "valuations",
                schema: "valuation");

            migrationBuilder.DropTable(
                name: "valuation_curves",
                schema: "valuation");

            migrationBuilder.DropTable(
                name: "vehicles",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "listings",
                schema: "listings");

            migrationBuilder.DropTable(
                name: "valuation_models",
                schema: "valuation");
        }
    }
}
