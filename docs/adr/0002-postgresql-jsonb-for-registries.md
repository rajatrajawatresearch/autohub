# ADR-0002 — PostgreSQL JSONB as the storage substrate for the ten registries

**Status:** Accepted — 2026-07-29
**Deciders:** Platform architecture
**Related:** ADR-0001 (modular monolith), ADR-0003 (metadata-driven everything), ADR-0004 (workflow engine)

---

## Context

North Star §2 commits us to ten registries whose *shapes are not knowable in advance*:

| Registry | The un-schemable part |
|---|---|
| Form Schema (#2) | An arbitrary JSON-Schema-ish document with layout, conditional visibility, and per-field validation |
| Workflow (#3) | `guard_expr`, `effect_json` — a rule expression and an ordered list of side-effect specs |
| Pricing (#4) | `fee_slabs` with open-ended dimensions: category × city × dealer-tier × payment-method × duration |
| Integration (#8) | Provider config, differing per provider (Razorpay needs `route_account_id`, Cashfree needs `vendor_id`, Karza needs `consent_artefact_ttl`) |
| Taxonomy (#1) | `attribute_definitions.validation_json`, `ui_json`, and per-category attribute values |

If we model these relationally we get a table with 60 nullable columns and a `CHECK` constraint
nobody can read. If we model them as blobs we lose queryability and validation. The specific failure
we are avoiding:

> A "flexible config" table shaped `(key text, value text)`. Two years in it holds 40,000 rows, six
> different serialisation formats, one row where someone stored a Python dict repr, and no way to
> answer "which cities have a commission override?" without a full scan and a regex.

Second constraint from ADR-0001: registries are read by every module inside the **same transaction**
that writes domain rows. A registry that lives in a different datastore means a pricing row committed
to Mongo while the `deals.deal_line_items` insert that referenced it rolls back — a dangling
reference with no FK to catch it and no way to detect it except a reconciliation job.

Third: registry writes are **audited, versioned, approvable, and rollback-able** (Registry change
sets, `platform.registry_change_sets` / `platform.registry_entity_versions`). That is a
transactional workload, not a document-store workload.

## Decision

**PostgreSQL 16 is the only registry store. Structure that is queried or constrained lives in
columns. Structure that is polymorphic lives in `jsonb`, validated by a schema that itself lives in
the registry.**

The rules, concretely:

1. **Domain types, not raw `jsonb`.**
   ```sql
   CREATE DOMAIN platform.jsonb_object AS jsonb
       CHECK (VALUE IS NULL OR jsonb_typeof(VALUE) = 'object');
   CREATE DOMAIN platform.code_key AS text
       CHECK (VALUE ~ '^[a-z][a-z0-9_.]{1,63}$');
   ```
   A `jsonb` column that can hold `"null"`, `42`, or `[]` is a bug waiting for a `->>` on the wrong
   type. Every registry JSON column is `platform.jsonb_object`.

2. **Every JSONB column has a JSON Schema, and the schema is a registry row.** Validation runs in
   `Platform` on write (Ajv-equivalent in .NET: `JsonSchema.Net`) *and* as a
   `jsonb_matches_schema()` CHECK constraint where `pg_jsonschema` is available. Belt and braces —
   a `psql` hotfix at 2am must not be able to write a form schema the renderer will crash on.

3. **Anything filtered, sorted, joined or FK'd is a real column.** `key`, `version`, `is_active`,
   `effective_from`, `scope_type`, `city_key`, `category_key`, `sort_order`, `entity_type` are
   columns. `ui_json`, `validation_json`, `effect_json`, `config_json` are `jsonb`.

4. **Hot JSONB paths get generated columns.**
   ```sql
   ALTER TABLE listings.listings
     ADD COLUMN attr_fuel_type text
     GENERATED ALWAYS AS (attributes ->> 'fuel_type') STORED;
   CREATE INDEX ix_listings__fuel ON listings.listings (attr_fuel_type)
     WHERE deleted_at IS NULL;
   ```
   These are generated *from the taxonomy registry* by a migration emitter, not hand-written — the
   list of promoted attributes is itself `attribute_definitions.is_promoted = true`. They are the
   OpenSearch-is-down fallback path (ADR-0006).

5. **GIN with `jsonb_path_ops`** on containment-queried columns; `fastupdate = off` on
   `listings.attributes` so a bulk dealer upload does not leave a pending list that makes the next
   reader pay for the write.

6. **Cache invalidation is an epoch, not a TTL.** `platform.registry_epochs(registry_key, epoch bigint)`
   is bumped in the same transaction as any registry write. Every API response carries
   `X-AutoHub-Registry-Version`; clients and the in-process cache compare and refetch. No registry
   read ever hits the DB on the request path (North Star §6).

## Consequences

**Positive**

- One transaction covers a registry write, its version row, its audit row, and its epoch bump.
  A pricing change is atomically visible or atomically absent.
- FKs still work where they matter: `fee_slabs.component_id → fee_components.id` is a real
  constraint, even though `fee_slabs.dimensions_json` is polymorphic.
- `jsonb_path_query`, `@>`, and `jsonb_to_recordset` make registries queryable for admin tooling
  without a bespoke index service.
- Backups, PITR, replication, and RLS apply to registry data for free. A separate config store would
  need all four re-solved.
- One skill set. No team member has to be fluent in a second query language to debug a form schema.

**Negative — stated honestly**

- **JSONB has no referential integrity.** `effect_json: {"notify": "listing.published.v1"}` can name
  a notification event that does not exist. Mitigation: a `platform.registry_lint` job runs on every
  change set and on a nightly cron, resolving every symbolic reference inside every JSON document and
  failing the change set. This job is not optional; without it JSONB rots.
- **Detoast cost is real.** A 40 KB form schema is out-of-line TOAST; `SELECT ui_json` is an extra
  read. Mitigation: never `SELECT *` a registry table on a hot path, and the in-process cache holds
  the deserialised object, not the row.
- **Schema evolution of a JSON document is app-level.** Changing `fee_slabs.dimensions_json` from
  `{"city": "..."}` to `{"geo": {"city": "..."}}` needs a data migration written by hand, with a
  dual-read window. There is no `ALTER COLUMN` for this. We version the JSON document
  (`schema_version` key inside every registry JSON object) so the reader can branch — this is the one
  place a version branch in code is permitted.
- **GIN indexes are large and slow to build.** A GIN on `listings.attributes` over 500k rows is
  hundreds of MB and a `REINDEX CONCURRENTLY` is a maintenance window's worth of I/O.
- **`jsonb` sorts by an internal ordering, not a natural one.** Never `ORDER BY a_jsonb_column`.
- **Developers will over-JSONB.** The gravitational pull is to put everything in `config_json`
  because it avoids a migration. Code review rule: if a value appears in a `WHERE` clause anywhere,
  it is a column.

## Alternatives considered

| Alternative | Why rejected |
|---|---|
| **MongoDB (or any document DB) for registries** | Registry writes and domain writes must share a transaction (see Context). Two datastores = dual write = dangling FKs with no constraint to catch them. Also doubles ops surface for zero gain: PG's JSONB is not meaningfully worse at document storage at our volume (registries are thousands of rows, not billions). |
| **Pure relational EAV** (`registry_values(entity_id, key, value_text, value_num, value_bool)`) | A form schema with nested layout becomes 400 rows. Reconstructing it is a recursive CTE per request. Validation is impossible. This is the classic "inner platform" trap where you rebuild a worse JSON parser in SQL. |
| **Registries as versioned JSON/YAML files in git, loaded at boot** | Directly violates the Prime Directive: a business person cannot change a commission without a deploy. Also makes per-city overrides a merge-conflict machine. We *do* keep the **seed** in git (`infra/seed/`), but the running truth is the DB. |
| **A dedicated config service** (Consul / LaunchDarkly / AWS AppConfig) | Solves feature flags (Registry #6) and nothing else. No relational integrity, no change sets with approval workflow, no audit trail joined to `platform.audit_log`, no way to express `fee_slabs` with slab arithmetic. DPDP data-residency questions on a US-hosted control plane. |
| **`hstore`** | Flat string→string only. Cannot express nested form layout or slab arrays. Legacy. |
| **XML + XMLTYPE** | No. |

## Revisit when

1. A single registry table exceeds **~5 million rows** with a hot GIN index and index maintenance
   becomes the top write cost — most likely `platform.entity_attribute_values`. The answer then is a
   column-promotion pass, not a new datastore.
2. `platform.i18n_strings` (Registry #9) growth × locale count makes registry cache warm-up exceed
   **10 seconds** on process start — move localization specifically to Redis-backed lazy load with a
   per-key epoch, keeping PG as the write side.
3. `pg_jsonschema` becomes unavailable on our managed PG offering — then app-side validation is the
   only gate and the registry-lint job gets promoted to a blocking pre-commit CI stage.
4. We need cross-region registry reads with < 50 ms latency (multi-region is explicitly deferred in
   North Star §3). At that point the answer is a read-only registry replica plus epoch-based
   invalidation — still PostgreSQL.
