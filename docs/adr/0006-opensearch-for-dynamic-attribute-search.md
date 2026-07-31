# ADR-0006 — OpenSearch for faceted search over registry-defined attributes

**Status:** Accepted — 2026-07-29
**Deciders:** Platform architecture
**Related:** ADR-0002 (JSONB), ADR-0003 (metadata-driven), ADR-0005 (outbox)

---

## Context

The Taxonomy Registry (Registry #1) means the set of searchable attributes is **unbounded and
changes without a deploy**. `platform.attribute_definitions` already carries `is_facet` and
`opensearch_field` for exactly this reason. On the day someone adds "Construction Equipment" with a
`boom_length` attribute, buyers must be able to filter on `boom_length: 12–18 m` — with correct
facet counts — without an engineer touching anything.

A concrete search request from the Indian market, all in one query:

```
"swift dzire"        typo-tolerant, matches "swift dezire", "dzire vxi"
category=car, city=pune within 40 km
price 4L–8L, kms < 60,000, owners ≤ 2, fuel in (petrol, cng)
attr.transmission = automatic, attr.sunroof = true
sort: relevance, then verified-inspection first
facets: 14 buckets with live counts, including counts for filters the user has NOT applied
```

The failure being avoided is specific:

> **The EAV facet join.** Attributes live in `platform.entity_attribute_values`. A 14-facet page
> becomes 14 correlated aggregate subqueries over a 3-million-row EAV table. On a 200k-listing
> corpus the p99 is ~8 s, the query plan flips between hash and nested loop depending on the city
> filter's selectivity, and adding a filter means a new composite index and a migration. Someone
> then "fixes" it by caching facet counts for 15 minutes, and buyers see "12 cars" and land on 4.

And the second failure, from over-correcting:

> **The search engine as source of truth.** Prices are edited in the search index because it is
> faster. A reindex loses a price change. Now the listing page and the search results disagree about
> money, and there is no way to know which is right.

## Decision

**OpenSearch serves discovery. PostgreSQL remains the source of truth. The index mapping is
generated from the Taxonomy Registry.**

### 1. Mapping is registry-generated, never hand-written

```
platform.attribute_definitions ──► mapping emitter ──► listings_v{n} mapping
   data_type      → es type      (int→long, decimal→scaled_float, enum→keyword,
                                  bool→boolean, text→text+keyword, date→date, geo→geo_point)
   is_facet=true  → indexed + docvalues under attr.{key}
   is_facet=false → inside a `flattened` catch-all field `attr_extra`
   opensearch_field → explicit override when the derived name would collide
   unit_key       → normalised to a canonical unit before indexing (km vs miles, cc vs L)
```

A registry change set that adds a facetable attribute triggers: create `listings_v{n+1}` with the
new mapping → reindex from PG (not from `listings_v{n}`, so a mapping bug cannot be laundered
forward) → verify doc count and a sampled field-level diff → atomically move the
`listings_read` / `listings_write` aliases. Zero downtime, and the previous index stays for one
cycle as a rollback target.

### 2. Index topology

| Index | Contents | Refresh | Notes |
|---|---|---|---|
| `listings_v{n}` (alias `listings_read`/`listings_write`) | Every listing with `state_category='ACTIVE'` and `caps.publicly_visible=true` | `1s` | Denormalised: vehicle snapshot, plate class, city/geo, price, inspection grade, dealer trust score |
| `vehicles_v{n}` | Catalog vehicles, for dealer inventory + admin | `5s` | Includes non-public states |
| `auction_lots_v{n}` | Live and upcoming lots | `1s` | `effective_end_at` for the "ending soon" sort |
| `taxonomy_suggest_v{n}` | Make/model/variant completion | on-change | `search_as_you_type` + Hindi/Devanagari analyzer |

Sharding: **1 primary shard per index** at our volume (< 2 M docs). A 5-shard default on a 200k-doc
index is pure coordination overhead and worse relevance (per-shard IDF). Replicas: 1 in prod.

### 3. India-specific analysis chain

- **Registration numbers** are `keyword` on the normalised form (`MH01AB1234`), plus a
  `ngram`-backed field for partial plate search — dealers type "01AB12" from memory.
- **Transliteration matters.** `स्विफ्ट` and "swift" must match. A custom analyzer chain:
  `icu_normalizer → indic_normalization → lowercase → asciifolding → synonym_graph`. The synonym
  set is **registry data** (`platform.search_synonyms`), reloaded via `_reload_search_analyzers`
  on epoch bump — "dzire/desire/dezire", "bolero/bollero", "scorpio/scorpion", "kms/kilometers".
- Fuzziness: `AUTO` with `prefix_length: 2` — without the prefix length, "800" fuzzily matches
  everything and Maruti 800 searches return garbage.
- **Money is indexed in paise as `long`**, and range filters are built from lakh/crore UI inputs at
  the edge. Never index formatted money.

### 4. Indexing path — outbox-driven, never dual-write-in-handler

```
listings.listing.published.v1 ─┐
listings.listing.price_changed ├─► SearchIndexer consumer ─► bulk buffer (500 docs / 1 s)
catalog.vehicle.attributes_changed ┘        │
inspection.inspection.completed ────────────┘  reads current state via module Contracts,
                                               never from the event payload
```

The indexer **re-reads from PG** rather than trusting the payload (ADR-0005 rule 2). That makes
out-of-order event delivery harmless: whatever order the events land, the last read wins with a
`version` = `row_version` and `version_type: external_gte`, so an older read can never overwrite a
newer document.

### 5. Read-your-writes

A seller who publishes and immediately searches must see their listing. The search endpoint
overlays: for the authenticated party, `UNION` the OpenSearch result with a PG query for that
party's listings modified in the last **30 s** that are not yet in the result set, de-duplicated by
listing id, and marked `"pending_index": true`. Only for the owner. Buyers get eventual consistency
and are never lied to about counts.

### 6. When OpenSearch is down

Domain Model §18.1 already commits to this: fall back to a PostgreSQL query over the **promoted
generated columns** (ADR-0002 §4) with a reduced facet set — the top 5 facets only, computed from a
materialised `listings_facet_counts` rollup refreshed every 5 minutes. Users see fewer filters and a
banner ("Filters limited right now"), **never zero results**. A circuit breaker flips after 5
consecutive failures or a 2 s p99, and flips back on 10 consecutive successes.

## Consequences

**Positive**

- Facet counts are one aggregation, not fourteen subqueries. p99 under 150 ms at 500k docs.
- A new facetable attribute is a registry change set + an automated reindex. No migration, no deploy.
- Geo, typo tolerance, transliteration, and relevance tuning are solved problems rather than
  bespoke SQL.
- Relevance is tunable as data: boosts for `has_inspection`, `dealer_trust_score`, `photo_count`,
  and recency live in a registry-backed `function_score` config, so growth can A/B ranking without
  an engineer.
- PG stays the correctness authority — the search index can always be rebuilt from it, and a search
  bug can never corrupt a price.

**Negative — stated honestly**

- **Eventual consistency is now a product surface.** Publish → visible is ~1–3 s typical, minutes if
  the indexer is backed up. Product and support must know this; the read-your-writes overlay covers
  only the owner.
- **Two systems can disagree, and the disagreement is invisible.** Mitigation: a nightly
  reconciliation job comparing PG (`state_category='ACTIVE'`) against index doc IDs, reporting
  orphans and missing docs to an ops dashboard, plus a per-document `indexed_at` staleness alert at
  p99 > 60 s. Without this job the index rots silently for months.
- **Mapping explosion.** OpenSearch's default `index.mapping.total_fields.limit` is 1000. With ~40
  categories × ~25 attributes we can exceed it. Mitigation: only `is_facet=true` attributes get real
  fields (hard cap **20 facetable attributes per category**, enforced in the registry change set
  validator); everything else goes into a `flattened` field that is retrievable but not aggregatable.
- **Reindex is expensive and human-supervised.** A full rebuild of 500k listings takes ~20 min and
  saturates bulk threads. Reindexes run in a maintenance window with a throttled bulk rate; the
  alias swap is the only instant part.
- **Cluster ops is real work.** JVM heap sizing (≤ 31 GB, compressed oops), shard-per-node limits,
  snapshot policy, version upgrades. This is the largest new operational surface in the stack.
- **Relevance is never finished.** Somebody owns "why is this listing #7" forever. Budget it.
- **Cost.** A 3-node OpenSearch cluster is a permanent line item that a PG-only design would not have.

## Alternatives considered

| Alternative | Why rejected |
|---|---|
| **PostgreSQL full-text + GIN + trigram only** | Handles matching acceptably; fails on **facet counts at scale** (the actual bottleneck), on `geo_distance` ranking combined with relevance, and on cheap per-attribute aggregations over dynamic attributes. We keep it as the *fallback* path precisely because it is adequate-but-degraded, which is the right role for it. |
| **Elasticsearch** | Functionally equivalent. Rejected on the SSPL/Elastic License question for a commercial multi-tenant product and on managed-offering flexibility in Indian regions. OpenSearch keeps the Apache-2.0 story clean and the query DSL is the same, so this is a low-regret choice either way. |
| **Typesense / Meilisearch** | Excellent DX and fast. Rejected on: weaker geo + faceting combinations, no `function_score`-class relevance shaping, limited nested/flattened handling for our dynamic-attribute model, and thinner operational tooling for a system that will hold the entire acquisition funnel. |
| **Algolia** | Best-in-class relevance and zero ops. Rejected on unit economics at 500k+ records with high update churn (every price change is a write), and on **DPDP Act 2023 data-residency** posture — search queries carry buyer intent and location, and a US/EU-hosted index makes the consent and cross-border story materially harder for no architectural gain. |
| **ClickHouse for search+analytics in one** | Superb for `analytics.search_events` (and we may use it there). Wrong tool for typo tolerance, transliteration, and relevance ranking. |
| **Materialised facet-count tables in PG only** | This is our fallback, not our primary. Refresh cost grows with `categories × facets × cities`, and stale counts that disagree with results is the exact bug in Context. |

## Revisit when

1. Index doc count exceeds **~5 million** or a single index exceeds **~50 GB** — time to shard
   properly and to split `listings` by `intent_key` (sale vs rental have different query shapes).
2. Nightly reconciliation reports **> 0.05%** drift for a week — the outbox consumer or the version
   guard is wrong; fix before adding features.
3. Search-driven revenue justifies **learning-to-rank** — at that point index the click/impression
   signals from `analytics.search_events` and move ranking from a hand-tuned `function_score` to an
   LTR plugin model (this is a Phase 7 concern, per North Star §7).
4. A facet is needed over a **non-registered** attribute (i.e. someone wants to filter on something
   the taxonomy does not model). That is a taxonomy gap, not a search gap — resist adding a bespoke
   index field.
5. Mapping fields approach **800** on any index — the per-category cap is being circumvented and the
   model needs per-category indices with a routing alias rather than one wide index.
