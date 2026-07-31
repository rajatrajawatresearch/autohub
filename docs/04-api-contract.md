# AutoHub — API Contract

> Derives from `00-north-star.md`. Stack, repo shape, naming and the Prime Directive are settled
> there and are **not** re-litigated here. This document is what every client — `apps/web`,
> `apps/dealer`, `apps/admin`, the three Flutter apps, and every bank/insurer partner — codes against.
>
> **Contract rule:** if a behaviour is not in this document or in the generated OpenAPI document,
> it does not exist. Clients may not depend on undocumented fields, undocumented ordering, or the
> shape of an ID beyond "opaque UUID string".

---

## 0. The two things that make this API different

Most marketplace APIs leak business meaning into the transport layer: `status: "PENDING"`,
`commission: 2.0`, `fuel_types: ["Petrol","Diesel"]`. Ours must not, because North Star §2 bans it.
So two patterns are enforced on **every** endpoint in this document:

**1. Stateful resources never return a bare status string.**

```jsonc
"status": {
  "code": "pending_approval",              // stable, from workflow registry
  "label": "Pending approval",             // resolved via i18n registry for the caller's locale
  "category": "in_progress",               // draft|in_progress|blocked|success|failure|terminal
  "color_token": "--ah-caution",           // design-system token (01-design-system §2)
  "definition_key": "listing.sale.v3",
  "definition_version": 3,
  "entered_at": "2026-07-29T06:14:22Z"
},
"available_actions": [
  { "action": "submit_for_review", "label": "Submit for review",
    "to_state": "under_review", "requires_confirmation": false,
    "form_schema_key": null },
  { "action": "withdraw", "label": "Withdraw listing",
    "to_state": "withdrawn", "requires_confirmation": true,
    "form_schema_key": "listing.withdraw_reason.v1" }
]
```

`available_actions` is the **user-specific, guard-evaluated** subset of transitions from the
Workflow Registry (#3), filtered by the caller's permissions and the guard expressions. A client
renders buttons from this array. A client that hardcodes a button for a status is broken by design.

**2. Every rupee is returned with the pricing rule that produced it.**

```jsonc
"fees": [
  { "code": "auction.buyer_premium", "label": "Buyer premium",
    "base_minor": 84000000, "rate_bps": 200, "amount_minor": 1680000,
    "gst_rate_bps": 1800, "gst_minor": 302400, "hsn_sac": "998599",
    "pricing_rule_id": "0193c2f1-...", "plan_code": "auction.premium.west.v4" }
]
```

Support can answer "why was I charged this?" by resolving `pricing_rule_id` in the
Pricing & Commission Registry (#4). No number in a money response is computed by a literal in C#.

---

## 1. REST conventions

### 1.1 URL shape

```
https://api.autohub.in/api/v{major}/{module}/{resource}[/{id}[/{sub-resource}]]
```

- `{module}` is exactly one of the module names in North Star §4, lower-case, plus `platform`
  (the registry plane). `AutoHub.Modules.Listings` → `/api/v1/listings/...`.
- `{resource}` is **plural, kebab-case**: `/api/v1/dealer/inventory-items`,
  `/api/v1/auction/auctions/{id}/bid-increments`.
- Sub-resources express containment only. A bid belongs to an auction:
  `/api/v1/auction/auctions/{auctionId}/bids`. A bid does not exist outside one.
- Actions that are not CRUD are **sub-resource POSTs with a noun**, never verbs in the path:
  `POST /api/v1/listings/listings/{id}/transitions` (body carries `action`), not `/publish`.
  This is what keeps the Workflow Registry authoritative — new states need no new routes.
- Non-`/api` planes: `/hubs/{hub}` (SignalR), `/openapi/v{major}.json`, `/health/live`,
  `/health/ready`, `/.well-known/autohub-webhook-keys.json`.

### 1.2 Wire casing — `snake_case`

JSON bodies use `snake_case` for **all** keys, request and response.

This is not a style preference. Taxonomy attribute codes, form-schema field keys, workflow state
codes and permission strings are all `snake_case` **data** (North Star §5, DB conventions). A
listing's `attributes` object contains keys the server has never seen at compile time
(`boom_length_m`, `refrigeration_unit_hours`). Any casing transformation at the client boundary
would have to guess whether a dynamic key is data or a property name — and would silently corrupt
`attr_2wd` → `attr2wd`. One casing everywhere, no transformer, no ambiguity.

`System.Text.Json` is configured with `JsonNamingPolicy.SnakeCaseLower`; enums are never serialized
as .NET enums (they're registry data — see §0).

### 1.3 Method semantics

| Method | Meaning | Idempotency-Key | Body | Success |
|---|---|---|---|---|
| `GET` | Read. Never mutates. Cacheable per §7. | — | none | `200`, `304` |
| `POST` /collection | Create | **required** | resource | `201` + `Location` |
| `POST` /{id}/transitions | Workflow action | **required** | `{action, …}` | `200` (or `202` if async effects) |
| `POST` /{id}/{action-noun} | Non-CRUD command | **required** | command | `200`/`202` |
| `PATCH` | Partial update, RFC 7396 merge-patch semantics on `application/merge-patch+json` | **required** | delta | `200` |
| `PUT` | Full replace. Used only for registry drafts and settings docs. | **required** | resource | `200` |
| `DELETE` | Soft delete (`deleted_at`) unless doc says otherwise | **required** | none | `204` |

`PATCH` with `Content-Type: application/json` is rejected with `common.request.unsupported_media_type`.
We require the explicit merge-patch media type so "field absent" vs "field set to null" is never
ambiguous — critical when a listing's dynamic `attributes` map is being edited.

### 1.4 Status codes we actually use

| Code | When | Notes |
|---|---|---|
| 200 | OK | |
| 201 | Created | `Location` + full body |
| 202 | Accepted | Long-running: valuation refresh, bulk upload, payout. Body carries `job_id` + `poll_url` |
| 204 | No content | DELETE, and `POST /auth/logout` |
| 206 | Partial | Only on `/documents/*/download` range requests |
| 304 | Not modified | Registry endpoints, media manifests |
| 400 | Malformed syntax, bad filter grammar, bad cursor | |
| 401 | No/expired/revoked credential | Always with `WWW-Authenticate: Bearer error="…"` |
| 402 | Payment required | Dealer subscription lapsed (Feature Registry gate) |
| 403 | Authenticated, not permitted | Problem Details lists `required_permission` + `scope` |
| 404 | Not found **or** not visible to caller | We never distinguish — see §5.6 |
| 409 | Conflict: version mismatch, in-flight idempotent replay, workflow guard | |
| 410 | Gone: sunset API version, expired auction snapshot cursor | |
| 412 | Precondition failed: `If-Match` mismatch | |
| 415 | Unsupported media type | |
| 422 | Semantically invalid: validation failures against a **Form Schema Registry** schema | `errors[]` populated |
| 423 | Locked: entity under legal hold / fraud freeze | |
| 425 | Too early: bid before auction `starts_at` within clock skew window | |
| 428 | Precondition required: mutating a versioned registry without `If-Match` | |
| 429 | Rate limited | §8 headers |
| 451 | Unavailable for legal reasons: DPDP erasure completed, geo-blocked listing | |
| 500 | Unhandled. Never leaks a stack trace. `trace_id` always present | |
| 502/503 | Upstream integration failed / circuit open | `Retry-After`, `integration_code` |
| 504 | Upstream timeout | |

**400 vs 422 is a hard line.** 400 = we could not parse your request. 422 = we parsed it and the
business rules (from a registry) rejected it. Clients render 422 `errors[]` inline on form fields
by `pointer`; 400 is a developer bug and shows a generic failure.

### 1.5 Versioning policy

- **One major version in the path.** `v1` today. A new major is minted only for a change that
  cannot be expressed additively.
- **Additive changes ship into the current major, unannounced.** New optional request fields, new
  response fields, new enumerated *registry* values, new endpoints. Clients MUST ignore unknown
  response fields and MUST NOT validate responses with `additionalProperties: false`. The generated
  Zod schemas use `.passthrough()` on response objects for exactly this reason (§14.3).
- **Preview surface.** Anything under `/api/v1/**` returning
  `X-AutoHub-Stability: experimental` may break with 14 days' notice and is excluded from the
  breaking-change CI gate. Used for Phase 7 (intelligence) endpoints.
- **What counts as breaking:** removing/renaming a field, narrowing a type, adding a required
  request field, removing an error code from a documented set, tightening a rate limit tier,
  changing a default sort, changing pagination semantics.

### 1.6 Deprecation policy

A deprecated endpoint or field goes through four stations. The clock is **calendar time from the
first response carrying the header**, and it is enforced by CI, not by memory.

| Station | Duration | Response signals |
|---|---|---|
| **Announced** | T+0 | `Deprecation: @1785283200` (RFC 9745, unix seconds), `Sunset: Wed, 29 Jul 2026 00:00:00 GMT` (RFC 8594), `Link: <https://developers.autohub.in/deprecations/DEP-0031>; rel="deprecation"` |
| **Warned** | T+90d | Adds `Warning: 299 - "listings.listing.legacy_price field removed on 2027-01-29"` and a `meta.deprecations[]` entry |
| **Brownout** | T+150d | Endpoint returns `410 common.api.version_sunset` for 5 minutes at 11:00 IST on the 1st, 8th, 15th of the month. Forces client teams to notice. |
| **Sunset** | T+180d | Permanent `410`. Minimum 180 days for public API, 90 days for partner-only, 30 days for internal admin-only. |

Partner (bank/insurer) integrations get **270 days** and a named migration contact, because their
change windows are quarterly.

`GET /api/v1/platform/deprecations` returns the live list so a dealer's ERP integration can alert
on its own.

### 1.7 Standard request headers

| Header | Required | Purpose |
|---|---|---|
| `Authorization: Bearer <jwt>` | on protected routes | §5 |
| `Idempotency-Key` | **all writes** | §6. UUIDv4 or ULID, ≤ 128 chars |
| `X-AutoHub-Device-Id` | mobile + web | Device binding (§5.4). Opaque, generated once per install |
| `X-AutoHub-Client` | yes | `web/2026.7.3`, `flutter-customer/3.1.0 (android 14)` — used for feature gating and forced-upgrade |
| `X-AutoHub-Scope` | when ambiguous | Active scope, e.g. `dealer:0193c2f1-…` or `city:PUNE`. See §5.7 |
| `Accept-Language` | recommended | `hi-IN, en-IN;q=0.8`. Falls back to user profile, then `en-IN` |
| `X-AutoHub-City` | recommended | Geography Registry city code (`MUM`, `PUN`). Drives Feature Registry gating and pricing geo-scope |
| `X-Request-Id` | optional | Client trace correlation; echoed back. If absent, server mints one |
| `If-Match` / `If-None-Match` | conditional | Optimistic concurrency (§1.8) and registry caching (§7) |
| `Prefer: return=minimal` | optional | Suppresses full body on writes → `204`. Used by bulk dealer sync |

### 1.8 Concurrency

Every mutable resource carries `row_version` (North Star §5) and serves it as a weak ETag:
`ETag: W/"7"`. `PATCH`/`PUT`/`DELETE` on a resource **should** send `If-Match`. If sent and stale →
`412 common.resource.version_conflict` with the current version in the problem detail. If omitted on
a resource class flagged `strict_concurrency` in the Feature Registry (deals, auctions, payouts,
registry drafts) → `428 common.request.precondition_required`.

---

## 2. The response envelope

### 2.1 Success — `Result<T>`

```jsonc
{
  "data": { /* T, or an array for collections */ },
  "meta": {
    "request_id": "0193c2f1-8a44-7b31-9e02-1f7c2d3a4b55",
    "server_time": "2026-07-29T06:14:22.317Z",
    "registry_versions": {                        // see §2.3
      "taxonomy": 412, "form_schema": 88, "workflow": 17,
      "pricing": 231, "rbac": 44, "feature": 63,
      "notification": 51, "integration": 29, "i18n": 205, "geography": 12
    },
    "page": {                                     // collections only
      "next_cursor": "eyJrIjpbIjIwMjYtMDctMjlU…",
      "prev_cursor": null,
      "has_more": true,
      "page_size": 24,
      "total_estimate": 18420,                    // never exact on search; see §7.1
      "total_is_exact": false
    },
    "degraded": false,                            // true when a fallback path served this (§11)
    "warnings": [],
    "deprecations": []
  }
}
```

`data` is **always** present on 2xx and is **always** the payload — never `data.items`, never a
bare array at the root. Collections put the array directly in `data` and the paging in `meta.page`.
This means one generic unwrap in every client: `res.data`.

### 2.2 Failure — RFC 9457 Problem Details

`Content-Type: application/problem+json`. There is no `data` key on an error response, ever.

```jsonc
{
  "type": "https://developers.autohub.in/errors/auction.bid.below_increment",
  "title": "Bid is below the minimum increment",
  "status": 422,
  "detail": "Minimum next bid is ₹8,45,000. You bid ₹8,42,000.",
  "instance": "/api/v1/auction/auctions/0193c2.../bids",

  // ---- AutoHub extension members (stable, part of the contract) ----
  "code": "auction.bid.below_increment",
  "trace_id": "0193c2f1-8a44-7b31-9e02-1f7c2d3a4b55",
  "request_id": "0193c2f1-8a44-7b31-9e02-1f7c2d3a4b55",
  "timestamp": "2026-07-29T06:14:22.317Z",
  "errors": [
    { "pointer": "/amount_minor",
      "code": "auction.bid.below_increment",
      "message_key": "err.auction.bid.below_increment",
      "message": "Minimum next bid is ₹8,45,000.",
      "params": { "min_next_minor": 84500000, "increment_minor": 300000 } }
  ],
  "retryable": false,
  "retry_after_seconds": null,
  "help_url": "https://help.autohub.in/auction/bidding",
  "support_reference": "AH-2026-07-29-8A44"
}
```

Contract details that matter:

- `code` is the **only** thing a client may branch on. `title` and `detail` are human text and may
  be reworded at any time; they are resolved from the Localization Registry (#9) using
  `message_key`, so a Marathi client gets Marathi `detail` with the same `code`.
- `detail` is safe to display. It never contains PII of another user, never contains an internal ID
  the caller can't already see, and never contains a stack frame.
- `errors[]` is present on `400`, `403`, `422`, `409` and empty otherwise. `pointer` is a
  **JSON Pointer into the request body**, so `SchemaForm` (design system §6) can attach the message
  to the exact field — including dynamic attributes: `/attributes/boom_length_m`.
- `params` exists so the client can re-render the message with `MoneyValue` and lakh/crore
  formatting instead of printing the server's pre-formatted string. Amounts in `params` are always
  minor units.
- `support_reference` is short, sayable on a phone call, and resolves in admin to the full trace.

### 2.3 `registry_versions` — the cache-coherence spine

Every response — success or error — also carries:

```
X-AutoHub-Registry-Version: tax=412;form=88;wf=17;price=231;rbac=44;feat=63;notif=51;intg=29;i18n=205;geo=12
```

Clients keep the registry bundles they've cached (§10) keyed by these integers. Any response whose
header disagrees with the local cache triggers a **background** refetch of only the drifted
registries. This is how a taxonomy change reaches a Flutter app on a 2G connection in Nashik within
one API call, with no push, no polling loop, and no full-bundle download.

The counters are monotonic `bigint` sequences bumped by the registry publish transaction. They are
**global per registry**, not per row — cheap to compare, deliberately coarse.

---

## 3. Machine error-code catalogue

Format: `{module}.{resource}.{condition}` — three segments, lower `snake_case`.
(North Star §5 writes `listing.not_found` illustratively; the normative form is the three-segment
one below, so `code` can be routed by module without a lookup table.)

Codes are **append-only**. A code's meaning never changes; a code is never reused; a code is never
deleted, only marked deprecated in `/api/v1/platform/error-codes`. The full list lives in
`AutoHub.SharedKernel/Errors/ErrorCodes.cs` as `const string` (a technical invariant, permitted by
North Star §2) and is emitted into the OpenAPI document as an enum so the TypeScript client gets a
union type.

### 3.1 `common.*` — transport & cross-cutting

| Code | HTTP | Condition |
|---|---|---|
| `common.request.malformed_json` | 400 | Body is not parseable JSON |
| `common.request.unsupported_media_type` | 415 | Wrong `Content-Type` (e.g. `PATCH` without merge-patch) |
| `common.request.payload_too_large` | 413 | Body over route limit (default 1 MiB; 25 MiB on media) |
| `common.request.precondition_required` | 428 | `If-Match` missing on a strict-concurrency resource |
| `common.request.invalid_header` | 400 | Malformed `X-AutoHub-Scope`, `Idempotency-Key`, `Accept-Language` |
| `common.resource.not_found` | 404 | Generic; module-specific codes preferred |
| `common.resource.version_conflict` | 412 | `If-Match` row_version stale |
| `common.resource.locked` | 423 | Legal hold / fraud freeze / DPDP erasure in progress |
| `common.idempotency.key_required` | 400 | Write without `Idempotency-Key` |
| `common.idempotency.key_reused_with_different_body` | 409 | Fingerprint mismatch (§6.3) |
| `common.idempotency.request_in_flight` | 409 | Original still executing; `retry_after_seconds` set |
| `common.pagination.invalid_cursor` | 400 | Cursor fails HMAC or decodes to a different sort signature |
| `common.pagination.cursor_expired` | 410 | Cursor older than TTL (search snapshots: 15 min) |
| `common.filter.unknown_field` | 400 | Filter names a field that isn't filterable for this resource |
| `common.filter.unsupported_operator` | 400 | e.g. `within:` on a non-geo field |
| `common.filter.invalid_value` | 400 | Value not coercible to the attribute's `data_type` |
| `common.sort.unsupported_field` | 400 | Sort field not in the resource's sortable set |
| `common.fields.unknown_field` | 400 | Sparse fieldset names an unknown field |
| `common.rate_limit.exceeded` | 429 | §8 |
| `common.api.version_sunset` | 410 | Sunset endpoint or version (§1.6) |
| `common.client.upgrade_required` | 426 | `X-AutoHub-Client` below Feature Registry `min_supported_version` |
| `common.internal.unhandled` | 500 | Nothing else fits; always paged |

### 3.2 `identity.*` — auth, users, KYC

| Code | HTTP | Condition |
|---|---|---|
| `identity.otp.invalid_phone` | 422 | Not a valid E.164 `+91` mobile (or an allowed international format) |
| `identity.otp.send_failed` | 502 | All SMS providers in the Integration Registry fallback chain failed |
| `identity.otp.throttled` | 429 | Per-phone send throttle (§5.2 ladder) |
| `identity.otp.incorrect` | 422 | Wrong code; `params.attempts_remaining` |
| `identity.otp.expired` | 422 | Past `expires_at` (default 300 s, registry-driven) |
| `identity.otp.max_attempts_exceeded` | 429 | Challenge burned; must request a new one |
| `identity.otp.challenge_not_found` | 404 | Unknown or consumed `challenge_id` |
| `identity.token.invalid` | 401 | Signature, `aud`, `iss`, or `nbf` failure |
| `identity.token.expired` | 401 | Access token past `exp` |
| `identity.refresh.reused` | 401 | **Reuse detection fired — entire family revoked** (§5.3) |
| `identity.refresh.revoked` | 401 | Token family revoked by logout / admin / password change |
| `identity.refresh.device_mismatch` | 401 | `X-AutoHub-Device-Id` ≠ token's bound device |
| `identity.session.revoked` | 401 | Session terminated remotely |
| `identity.session.concurrent_limit` | 409 | Device count over Feature Registry cap for the tier |
| `identity.user.suspended` | 403 | Trust Registry suspension; `params.until` |
| `identity.user.phone_already_linked` | 409 | Phone belongs to another account |
| `identity.mfa.required` | 401 | Step-up needed (admin, payout change); `params.methods[]` |
| `identity.consent.required` | 403 | DPDP consent purpose not granted (§5.9) |
| `identity.kyc.required` | 403 | Action gated behind a KYC level the user lacks |
| `identity.kyc.level_insufficient` | 403 | Has L1, needs L2; `params.required_level` |
| `identity.kyc.aadhaar_otp_failed` | 502 | UIDAI/DigiLocker rejected the OTP exchange |
| `identity.kyc.digilocker_link_expired` | 410 | Consent artefact expired before fetch |
| `identity.kyc.pan_name_mismatch` | 422 | PAN name ≠ Aadhaar/RC name beyond fuzzy threshold |
| `identity.kyc.document_unreadable` | 422 | OCR confidence below Integration Registry threshold |

### 3.3 `platform.*` — the registry plane

| Code | HTTP | Condition |
|---|---|---|
| `platform.registry.key_not_found` | 404 | Unknown form/workflow/i18n key |
| `platform.registry.version_not_found` | 404 | Pinned version doesn't exist |
| `platform.registry.draft_conflict` | 409 | Concurrent draft edit; `If-Match` stale |
| `platform.registry.publish_validation_failed` | 422 | Draft failed the publish-time validator |
| `platform.registry.circular_reference` | 422 | Form schema / taxonomy parent loop |
| `platform.form.schema_invalid` | 422 | Not valid JSON Schema 2020-12 + AutoHub extensions |
| `platform.form.submission_invalid` | 422 | Submitted payload fails the schema; `errors[]` per field |
| `platform.workflow.transition_not_allowed` | 409 | No transition `from → action` in the active definition |
| `platform.workflow.guard_failed` | 409 | Transition exists, guard expression returned false; `params.guard` |
| `platform.workflow.definition_inactive` | 409 | Entity pinned to a retired definition version |
| `platform.workflow.terminal_state` | 409 | Entity is terminal; no transitions remain |
| `platform.taxonomy.node_not_found` | 404 | Unknown category/make/model/variant id |
| `platform.taxonomy.attribute_not_applicable` | 422 | Attribute not defined for that category |
| `platform.taxonomy.node_in_use` | 409 | Cannot deactivate — live listings reference it |
| `platform.feature.disabled` | 403 | Module/feature off for this user/city/tier; `params.feature_key` |
| `platform.feature.geo_unavailable` | 403 | Not launched in caller's city |
| `platform.pricing.no_matching_rule` | 500 | Pricing Registry has no rule for the context — configuration bug, paged |
| `platform.pricing.plan_expired` | 409 | Quote references a plan past `valid_to` |
| `platform.i18n.locale_unsupported` | 404 | Locale not in the Localization Registry |
| `platform.geo.pincode_unserviceable` | 422 | Outside every service area polygon |
| `platform.integration.provider_unavailable` | 503 | Primary + all fallbacks down; `params.integration_code` |
| `platform.integration.circuit_open` | 503 | Circuit breaker open; `retry_after_seconds` set |

### 3.4 `catalog.*` — vehicle identity, RTO, VAHAN

| Code | HTTP | Condition |
|---|---|---|
| `catalog.registration.invalid_format` | 422 | Not a valid Indian reg number (old series, BH series, or new) |
| `catalog.registration.already_registered` | 409 | Another active vehicle holds this plate |
| `catalog.vahan.record_not_found` | 404 | VAHAN has no record — offer manual entry |
| `catalog.vahan.upstream_unavailable` | 503 | PARIVAHAN/VAHAN down; `retryable: true` |
| `catalog.vahan.rate_limited_upstream` | 503 | Provider quota exhausted for the day |
| `catalog.vin.checksum_invalid` | 422 | 17-char VIN fails ISO 3779 check digit |
| `catalog.vehicle.ownership_unverified` | 403 | Caller is not the RC holder and hasn't passed seller verification |
| `catalog.challan.upstream_unavailable` | 503 | e-Challan service down |

### 3.5 `listings.*`

| Code | HTTP | Condition |
|---|---|---|
| `listings.listing.not_found` | 404 | |
| `listings.listing.not_owned` | 403 | Caller is neither owner nor scoped dealer/admin |
| `listings.listing.duplicate_active` | 409 | Same vehicle already has an active listing of this intent |
| `listings.listing.intent_not_enabled` | 403 | Intent (lease/auction) off for city/tier — Feature Registry |
| `listings.listing.incomplete_for_publish` | 422 | Required schema fields or media missing; `errors[]` lists them |
| `listings.listing.media_minimum_not_met` | 422 | Below Feature Registry `min_photos` for the category |
| `listings.listing.price_out_of_band` | 422 | Outside ±N% of valuation band; `params.band_min_minor/band_max_minor` |
| `listings.listing.moderation_rejected` | 409 | AI/human moderation blocked publish; `params.reason_codes[]` |
| `listings.listing.expired` | 409 | Past `expires_at`; must renew |
| `listings.listing.reserved_by_deal` | 409 | An active deal holds it; cannot edit price or withdraw |
| `listings.media.upload_expired` | 410 | Presigned URL past expiry |
| `listings.media.virus_detected` | 422 | ClamAV positive; asset quarantined |
| `listings.media.format_unsupported` | 422 | Not in the allowed MIME list |
| `listings.media.limit_exceeded` | 422 | Over per-listing asset cap |

### 3.6 `search.*` / `valuation.*` / `inspection.*`

| Code | HTTP | Condition |
|---|---|---|
| `search.query.too_complex` | 400 | Filter tree over depth/clause budget |
| `search.query.geo_radius_too_large` | 400 | Over the registry-configured max radius |
| `search.index.unavailable` | 503 | OpenSearch down **and** Postgres fallback also failed |
| `search.saved_search.limit_exceeded` | 422 | Over the tier's saved-search cap |
| `valuation.request.insufficient_inputs` | 422 | Cannot price without make/model/year/km |
| `valuation.model.no_coverage` | 422 | No model coverage for that category/region yet |
| `valuation.model.confidence_too_low` | 422 | Below publish threshold; returns range only, no point estimate |
| `valuation.quote.expired` | 410 | Quote past `valid_until` (default 7 days) |
| `valuation.quote.superseded` | 409 | A newer quote exists for the same vehicle |
| `inspection.booking.slot_unavailable` | 409 | Slot taken between quote and confirm |
| `inspection.booking.no_inspector_in_area` | 422 | Geography Registry: no coverage for the pincode |
| `inspection.report.not_ready` | 409 | Report still in progress |
| `inspection.report.checklist_incomplete` | 422 | Mandatory checklist items unanswered |
| `inspection.report.already_submitted` | 409 | Immutable after submit; use amendment flow |

### 3.7 `auction.*`

| Code | HTTP | Condition |
|---|---|---|
| `auction.auction.not_found` | 404 | |
| `auction.auction.not_started` | 425 | Before `starts_at` (beyond clock-skew grace) |
| `auction.auction.closed` | 409 | Past `ends_at` / already settled |
| `auction.auction.paused` | 409 | Admin pause (workflow state `paused`) |
| `auction.auction.reserve_not_set` | 409 | Cannot open without a reserve |
| `auction.auction.extension_limit_reached` | 409 | Anti-snipe extension cap hit |
| `auction.registration.required` | 403 | Dealer not registered for this auction |
| `auction.registration.deposit_required` | 402 | EMD unpaid |
| `auction.registration.deposit_insufficient` | 402 | Bid would exceed deposit-backed bidding limit |
| `auction.registration.dealer_not_approved` | 403 | KYC/trust gate unmet |
| `auction.bid.below_increment` | 422 | Below `min_next_bid_minor` |
| `auction.bid.below_start_price` | 422 | First bid below opening price |
| `auction.bid.not_multiple_of_increment` | 422 | Increment ladder violation |
| `auction.bid.outbid_race` | 409 | `seen_bid_seq` stale — someone bid first (§12.4) |
| `auction.bid.self_bid_forbidden` | 403 | Bidder is the seller or a linked entity |
| `auction.bid.limit_exceeded` | 403 | Per-dealer exposure cap across live auctions |
| `auction.bid.retracted_not_allowed` | 403 | Retraction window closed |
| `auction.bid.proxy_below_current` | 422 | Proxy max below current highest |
| `auction.lot.reserve_not_met` | 409 | Close attempted below reserve without seller approval |
| `auction.settlement.window_expired` | 409 | Winner missed the payment window; forfeit flow triggered |

### 3.8 `deals.*` / `payments.*`

| Code | HTTP | Condition |
|---|---|---|
| `deals.deal.not_found` | 404 | |
| `deals.deal.party_not_authorised` | 403 | Caller is not buyer/seller/agent on this deal |
| `deals.deal.already_exists_for_listing` | 409 | An active deal already holds the listing |
| `deals.offer.expired` | 410 | Past `expires_at` |
| `deals.offer.below_minimum` | 422 | Below seller's floor |
| `deals.offer.superseded` | 409 | Counter-offer already sent |
| `deals.deal.milestone_out_of_order` | 409 | Workflow guard: RC transfer before payment |
| `deals.deal.documents_incomplete` | 422 | Form 29/30, NOC, insurance transfer missing |
| `deals.deal.hypothecation_active` | 409 | Loan not closed; NOC required before transfer |
| `deals.deal.challan_outstanding` | 409 | Unpaid challans over threshold; `params.total_minor` |
| `deals.escrow.insufficient_balance` | 409 | Payout exceeds escrow held |
| `deals.settlement.hold_active` | 423 | Dispute/legal hold blocks payout |
| `payments.order.not_found` | 404 | |
| `payments.order.amount_mismatch` | 422 | Client amount ≠ server-computed amount |
| `payments.order.currency_unsupported` | 422 | Only `INR` in v1 |
| `payments.payment.already_captured` | 409 | |
| `payments.payment.failed` | 402 | Gateway declined; `params.gateway_code` |
| `payments.payment.pending_confirmation` | 409 | UPI collect not yet acted on |
| `payments.upi.collect_expired` | 410 | UPI mandate/collect request lapsed |
| `payments.upi.vpa_invalid` | 422 | VPA failed validation |
| `payments.gateway.signature_invalid` | 400 | Inbound webhook signature failed |
| `payments.gateway.all_providers_unavailable` | 503 | Razorpay + Cashfree fallback chain exhausted |
| `payments.refund.window_expired` | 409 | Past refundable window |
| `payments.refund.exceeds_captured` | 422 | Refund > captured amount |
| `payments.payout.beneficiary_unverified` | 403 | Penny-drop bank verification not passed |
| `payments.payout.nodal_limit_exceeded` | 409 | RBI nodal per-transaction ceiling |
| `payments.invoice.gst_number_invalid` | 422 | GSTIN checksum/format failure |
| `payments.invoice.irn_generation_failed` | 502 | IRP (e-invoice) rejected; `params.irp_code` |
| `payments.tcs.pan_missing` | 422 | Sale ≥ ₹10L requires PAN for TCS u/s 206C(1F) |

### 3.9 `bookings.*` / `rental.*` / `lease.*` / `attachment.*`

| Code | HTTP | Condition |
|---|---|---|
| `bookings.slot.unavailable` | 409 | Taken during confirm |
| `bookings.slot.outside_operating_hours` | 422 | Geography Registry hours |
| `bookings.booking.cancellation_window_passed` | 409 | Cancellation fee applies or blocked |
| `bookings.booking.reschedule_limit_exceeded` | 409 | Over the registry-configured reschedule count |
| `bookings.booking.duplicate_for_listing` | 409 | Same user, same listing, overlapping window |
| `rental.availability.conflict` | 409 | Overlapping confirmed rental |
| `rental.driver_licence.invalid` | 422 | DL check failed / expired |
| `rental.driver_licence.class_mismatch` | 422 | DL class doesn't cover the vehicle class |
| `rental.security_deposit.required` | 402 | Deposit unpaid |
| `rental.return.overdue` | 409 | Return past due; late fee from Pricing Registry |
| `lease.application.credit_check_failed` | 422 | Bureau score below plan threshold |
| `lease.plan.tenure_unsupported` | 422 | Tenure not in the plan's allowed set |
| `attachment.aggregator.not_onboarded` | 403 | Fleet aggregator not approved |
| `attachment.vehicle.permit_invalid` | 422 | Commercial permit expired / wrong state |
| `attachment.vehicle.fitness_expired` | 422 | Fitness certificate lapsed |

### 3.10 `finance.*` / `insurance.*` / `dealer.*` / `chat.*` / `documents.*` / `trust.*`

| Code | HTTP | Condition |
|---|---|---|
| `finance.application.duplicate_in_progress` | 409 | Same user+vehicle already applied to this lender |
| `finance.application.lender_declined` | 200* | Terminal workflow state, not an HTTP error |
| `finance.bureau.consent_missing` | 403 | No bureau-pull consent artefact |
| `finance.bureau.upstream_unavailable` | 503 | CIBIL/Experian down |
| `finance.offer.expired` | 410 | Sanction letter validity lapsed |
| `finance.disbursal.account_mismatch` | 422 | Payout account ≠ verified seller account |
| `insurance.quote.vehicle_ineligible` | 422 | Age/category outside insurer appetite |
| `insurance.quote.expired` | 410 | Insurer quote validity lapsed |
| `insurance.policy.previous_policy_required` | 422 | NCB claim needs prior policy details |
| `insurance.policy.inspection_required` | 409 | Break-in case requires pre-inspection |
| `insurance.claim.policy_inactive` | 409 | Claim on lapsed policy |
| `insurance.partner.upstream_unavailable` | 503 | Insurer API down |
| `dealer.dealer.not_found` | 404 | |
| `dealer.dealer.not_verified` | 403 | Onboarding incomplete |
| `dealer.subscription.expired` | 402 | Plan lapsed — Feature Registry gate |
| `dealer.inventory.limit_exceeded` | 402 | Over plan's active-listing cap |
| `dealer.bulk_upload.file_invalid` | 422 | Bad CSV/XLSX structure |
| `dealer.bulk_upload.row_limit_exceeded` | 422 | Over max rows for the plan |
| `dealer.branch.not_in_scope` | 403 | Branch outside caller's RBAC scope |
| `chat.thread.not_participant` | 403 | |
| `chat.thread.closed` | 409 | Deal closed / thread archived |
| `chat.message.blocked_content` | 422 | Contact-info leak / abuse filter; `params.reason` |
| `chat.message.rate_exceeded` | 429 | Anti-spam throttle |
| `documents.document.not_found` | 404 | |
| `documents.document.access_denied` | 403 | Not a party to the owning entity |
| `documents.document.generation_failed` | 500 | PDF pipeline error |
| `documents.esign.aadhaar_esign_failed` | 502 | eSign ASP failure |
| `documents.esign.already_signed` | 409 | |
| `trust.user.blacklisted` | 403 | Trust Registry blacklist |
| `trust.fraud.manual_review_required` | 202 | Action queued, not rejected; poll for outcome |
| `trust.device.blocked` | 403 | Device fingerprint blocked |
| `trust.velocity.limit_exceeded` | 429 | Behavioural velocity rule tripped |

\* `finance.application.lender_declined` is deliberately **not** an HTTP error. A declined loan is a
valid workflow terminal state (`declined`) on a successfully created resource. Treating business
outcomes as transport failures is the mistake that makes clients guess.

**Count: 130 codes across 20 modules.** Every one is exercised by at least one contract test in
`backend/tests/AutoHub.Api.ContractTests`; a code that no test can produce fails the build.

---

## 4. Authentication

### 4.1 Why OTP-first

India is phone-first. Password is an **optional secondary** credential attached to an account that
already owns a verified phone. There is no email/password signup path in v1. Email is a
notification channel and an optional recovery factor, never the identity.

### 4.2 The OTP flow

```
Client                                     API                          SMS/WA provider
  │                                         │                                  │
  │ POST /auth/otp/start                    │                                  │
  │ {channel:"sms", phone:"+919820012345",  │                                  │
  │  purpose:"login", device_id, captcha?}  │                                  │
  ├────────────────────────────────────────►│                                  │
  │                                         │ rate-limit ladder (§4.2.1)       │
  │                                         │ Integration Registry → provider  │
  │                                         │ DLT template id from             │
  │                                         │   Notification Registry (#7)     │
  │                                         ├─────────────────────────────────►│
  │ 200 {challenge_id, masked_to:"+91 98200 1****5",                           │
  │      expires_at, resend_after_seconds:30,                                  │
  │      attempts_allowed:5, delivery_channel:"sms",                           │
  │      hint:"AH-4821"}   ← shown in-app AND in SMS body, anti-phishing       │
  │◄────────────────────────────────────────┤                                  │
  │                                         │                                  │
  │ POST /auth/otp/verify                   │                                  │
  │ {challenge_id, code:"482913", device_id, device_info}                      │
  ├────────────────────────────────────────►│                                  │
  │                                         │ constant-time compare on         │
  │                                         │   HMAC(code, challenge_salt)     │
  │ 200 {access_token, refresh_token,       │                                  │
  │      expires_in:900, token_type:"Bearer",                                  │
  │      session_id, is_new_user:false,     │                                  │
  │      next_step:null|"complete_profile"|"kyc_required"}                     │
  │◄────────────────────────────────────────┤                                  │
```

Server never returns the OTP. The `hint` is a 4-char code echoed in the SMS body
(*"AH-4821: 482913 is your AutoHub code"*) so a user being social-engineered on a phone call can
see the hint doesn't match their screen. Cheap, effective, India-relevant.

**WhatsApp-first fallback ladder:** the Notification Registry (#7) row for
`identity.otp.login` declares `channel_priority: ["whatsapp","sms","voice"]`. If WhatsApp delivery
fails or the number has no WhatsApp, the API auto-falls to SMS and the response `delivery_channel`
reflects what was actually used. `POST /auth/otp/resend` may force `channel: "voice"` — matters for
users on feature phones and in low-signal areas.

#### 4.2.1 OTP throttle ladder (Feature Registry `otp.throttle.v1`)

| Window | Limit | On breach |
|---|---|---|
| per phone | 1 send / 30 s | `identity.otp.throttled`, `retry_after_seconds` |
| per phone | 5 sends / 1 h | `identity.otp.throttled` |
| per phone | 10 sends / 24 h | throttled + Trust Registry velocity event |
| per device_id | 20 sends / 24 h | throttled |
| per IP (/24 for v4, /48 for v6) | 60 sends / 1 h | CAPTCHA required on next `otp/start` |
| verify attempts | 5 per challenge | `identity.otp.max_attempts_exceeded`, challenge burned |
| failed verifies | 10 / phone / 1 h | 15-min phone lockout |

`otp_challenges(id uuid7 pk, phone_e164, purpose, channel, code_hash, salt, attempts, max_attempts, expires_at, consumed_at, device_id, ip_hash, created_at)` — `code_hash` is HMAC-SHA256, never the raw code. Row is deleted 24 h after `expires_at` (DPDP data minimisation).

### 4.3 Tokens

**Access token** — JWT, `RS256`, 15 min, never stored in `localStorage` on web (memory + a
`__Host-` cookie carrying only the refresh token, `HttpOnly; Secure; SameSite=Strict; Path=/api/v1/identity/auth`).

```jsonc
{
  "iss": "https://api.autohub.in", "aud": "autohub-api",
  "sub": "0193c2f1-8a44-7b31-9e02-1f7c2d3a4b55",   // user_id
  "sid": "0193c2f2-...",                            // session_id
  "did": "0193c2f3-...",                            // device_id — bound
  "jti": "0193c2f4-...",
  "iat": 1785283200, "exp": 1785284100, "nbf": 1785283200,
  "typ": "at+jwt",
  "scp": ["dealer:0193ab..", "city:PUN", "city:MUM"], // resolved scopes, ≤ 32 entries
  "rol": ["dealer_owner", "auction_bidder"],          // role codes, for logging only
  "kyc": 2,                                            // KYC level attained
  "amr": ["otp"],                                      // auth methods used
  "ver": 44                                            // rbac registry version at mint time
}
```

**Permissions are NOT in the token.** Roles change, permissions are re-scoped, and a 15-minute
stale permission set is a real security hole on a money platform. The API resolves permissions
per-request from a Redis-cached RBAC snapshot keyed `rbac:v{ver}:user:{sub}` (TTL 300 s, invalidated
by the RBAC publish event). `ver` in the token lets the middleware detect a stale snapshot instantly.
A token carrying `rol` claims is for **audit logs only** — no authorisation decision reads it.

**Refresh token** — opaque 256-bit random, base64url, 60 days sliding, single-use, device-bound.
Stored hashed.

### 4.4 Rotating refresh + reuse detection

```
auth_refresh_tokens
  id            uuid7 pk
  family_id     uuid7        -- constant across the whole rotation chain
  user_id       uuid7
  session_id    uuid7
  device_id     uuid7
  token_hash    bytea        -- SHA-256 of the opaque token; unique
  parent_id     uuid7 null   -- the token this one replaced
  issued_at     timestamptz
  expires_at    timestamptz
  used_at       timestamptz null
  revoked_at    timestamptz null
  revoked_reason text null    -- 'rotated' | 'logout' | 'reuse_detected' | 'admin' | 'password_change'
  ip_hash       bytea
  user_agent    text
```

Rotation on `POST /auth/token/refresh`:

```
1. hash(presented) → lookup. Not found            → 401 identity.refresh.revoked
2. revoked_at != null                             → 401 identity.refresh.revoked
3. expires_at < now                               → 401 identity.refresh.revoked
4. device_id != X-AutoHub-Device-Id               → 401 identity.refresh.device_mismatch
5. used_at != null   ── REUSE DETECTED ──►  revoke ENTIRE family (all rows, family_id)
                                            revoke the session
                                            emit identity.refresh_reuse_detected → Trust module
                                            notify user (Notification Registry: security.session_revoked)
                                            → 401 identity.refresh.reused
6. else: mark used_at, insert child row, return new pair
```

Step 5 is the whole point: a stolen refresh token is only useful until either the thief or the
victim next refreshes — after which both are logged out and the user is told. The rotation write is
a single Postgres transaction with `SELECT … FOR UPDATE` on the presented row, so a double-submit
from a flaky mobile network cannot create two live children.

**Network-retry grace:** if `used_at` is within 10 s **and** the presented token's existing child
was issued to the same `device_id` and same `ip_hash`, we return the **already-issued child pair**
instead of firing reuse detection. Without this, every Indian train-tunnel timeout logs a user out.
The grace window is in the Feature Registry (`auth.refresh.replay_grace_seconds`), default 10.

### 4.5 Device binding

`X-AutoHub-Device-Id` is generated once per install (Flutter: secure storage; web: a `__Host-ah_did`
cookie + IndexedDB mirror) and registered on first `otp/verify`. Every refresh and every
`sensitive` route re-checks it. `auth_devices(id, user_id, device_id, platform, model, os_version,
app_version, push_token, first_seen_at, last_seen_at, trusted_at, blocked_at, fingerprint_hash)`.

A new device on an existing account triggers a Notification Registry event
(`security.new_device_login`) with the city resolved from IP via the Geography Registry — *"New login
from Pune on Xiaomi 14. Not you?"*.

### 4.6 Session revocation

| Action | Effect |
|---|---|
| `POST /auth/logout` | Revokes current refresh family + session. Access token lives out its ≤15 min. |
| `POST /auth/logout-all` | Revokes every family for the user. Bumps `users.token_epoch`. |
| `DELETE /auth/sessions/{id}` | Revokes one session (used from "active devices" screen) |
| Admin suspend / Trust blacklist | Bumps `token_epoch`, publishes to the revocation channel |
| Payout-account change | Bumps `token_epoch` (money-relevant credential change) |

Access tokens are stateless, so instant revocation uses a **Redis revocation set**:
`SETEX revoked:sid:{session_id} 900 1` and `SET user:{id}:token_epoch <n>`. The auth middleware does
one pipelined Redis read per request (already needed for the RBAC snapshot, so it's the same
round-trip). If Redis is unavailable, the middleware **fails closed for `sensitive: true` routes**
(payments, payouts, bids, admin) and fails open for reads — an explicit, documented trade so a Redis
blip doesn't take down browsing but can never let a revoked session move money.

### 4.7 Step-up authentication

Routes marked `step_up: true` in the route registry require `amr` to contain a factor completed
within `max_age_seconds`. Failure → `401 identity.mfa.required` with
`params: { methods: ["otp","totp"], challenge_hint: "…" }`. Applied to: adding/changing a payout
bank account, first bid on an auction over ₹25L, admin registry publish, bulk price change over
N listings, DPDP data export/erasure requests.

### 4.8 Auth endpoints

| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `/api/v1/identity/auth/otp/start` | anon | Body `{phone, channel?, purpose, device_id, captcha_token?}` |
| POST | `/api/v1/identity/auth/otp/resend` | anon | `{challenge_id, channel?}` — honours `resend_after_seconds` |
| POST | `/api/v1/identity/auth/otp/verify` | anon | Returns token pair + `next_step` |
| POST | `/api/v1/identity/auth/token/refresh` | refresh | Rotation (§4.4). `Idempotency-Key` **not** used — the token itself is the key |
| POST | `/api/v1/identity/auth/password/set` | user, step-up | Optional secondary credential |
| POST | `/api/v1/identity/auth/password/login` | anon | Only for accounts with a verified phone **and** a set password |
| POST | `/api/v1/identity/auth/logout` | user | 204 |
| POST | `/api/v1/identity/auth/logout-all` | user | 204 |
| GET | `/api/v1/identity/auth/sessions` | user | Active sessions + devices, current flagged |
| DELETE | `/api/v1/identity/auth/sessions/{id}` | user | |
| GET | `/api/v1/identity/auth/context` | user | **The bootstrap call.** Profile + permissions + scopes + enabled features + registry versions. One call, everything the shell needs |
| POST | `/api/v1/identity/auth/oauth/{provider}/exchange` | anon | Google/Apple → still requires phone verification before a full token |
| GET | `/.well-known/jwks.json` | anon | Public keys, 2 active (rotation), 24 h cache |

---

## 5. Authorisation

### 5.1 Permission strings

`{module}.{resource}.{action}` — the same three-segment grammar as error codes.

```
listings.listing.create        auction.bid.place           deals.deal.approve_payout
listings.listing.read_any      auction.auction.moderate    platform.registry.publish
dealer.inventory.bulk_import   payments.payout.release     identity.user.impersonate
```

Rows live in the RBAC Registry (#5):

```
rbac_permissions(code pk, module, resource, action, min_scope, is_sensitive,
                 label_key, description_key, created_at)
rbac_roles(code pk, label_key, tenant_kind, is_system, is_assignable, created_at)
rbac_role_permissions(role_code, permission_code, scope_kind, constraint_json)
rbac_role_assignments(id, user_id, role_code, scope_kind, scope_ref, granted_by,
                      valid_from, valid_to, reason)
```

Example rows:

| role_code | permission_code | scope_kind | constraint_json |
|---|---|---|---|
| `dealer_owner` | `dealer.inventory.write` | `dealer` | `null` |
| `dealer_sales_exec` | `dealer.inventory.write` | `branch` | `{"max_price_minor": 200000000}` |
| `regional_auction_moderator_west` | `auction.auction.moderate` | `city` | `{"cities":["MUM","PUN","NGP","AMD"]}` |
| `ops_settlement` | `payments.payout.release` | `global` | `{"max_amount_minor": 500000000, "requires_maker_checker": true}` |

Creating "Regional Auction Moderator — West" (North Star §2) is four inserts. No deploy, no code.

### 5.2 Scope kinds and resolution

```
global  ⊃  city  ⊃  branch
global  ⊃  dealer  ⊃  branch
self  (the caller's own resources — always implicitly granted)
```

The middleware resolves, per request:

```
effective = ⋃ over the user's active role assignments of (permission_code → scope set)
```

An endpoint declares `[RequirePermission("listings.listing.update", Scope.Resource)]`. The handler
receives an `IScopeGuard` and must call `guard.Assert(resource)`, which compares the resource's
`owner_user_id` / `dealer_id` / `branch_id` / `city_code` against the resolved scope set. Failing to
call it is a build error — an analyser (`AH0007`) fails any handler annotated
`Scope.Resource` that doesn't invoke the guard on every return path.

`X-AutoHub-Scope` narrows, never widens. A dealer employee at two dealerships sends
`X-AutoHub-Scope: dealer:0193ab…` to say "I'm acting as this dealer now". If the header names a
scope the user doesn't hold → `403 dealer.branch.not_in_scope`. If it's absent and the user holds
exactly one dealer scope, that one is implied. If absent and the user holds several, list endpoints
return the union and write endpoints return
`400 common.request.invalid_header` with `params.candidates[]`.

### 5.3 The 403 body

```jsonc
{
  "type": "https://developers.autohub.in/errors/platform.feature.disabled",
  "status": 403, "code": "platform.feature.disabled",
  "title": "Lease is not available in your city yet",
  "detail": "Lease is live in Bengaluru. We'll notify you when it opens in Nagpur.",
  "errors": [{ "code": "platform.feature.disabled",
               "params": { "feature_key": "module.lease", "city": "NGP",
                           "available_cities": ["BLR"] } }]
}
```

For a genuine permission failure:
`params: { required_permission: "auction.auction.moderate", required_scope: "city:PUN", held_scopes: ["city:MUM"] }`.
Telling the caller exactly what they lack is safe here (they're authenticated and it's their own
permission set) and eliminates a whole class of support tickets.

### 5.4 404 vs 403

If the caller lacks even **read** visibility on a resource, we return `404`, not `403` — otherwise
the API becomes an existence oracle (enumerate deal IDs to learn who's transacting). If the caller
can read it but not perform the action, `403`. This rule is implemented in the scope guard, not
per-handler.

### 5.5 DPDP consent as authorisation

Certain actions require a granted **consent purpose**, not just a permission:
bureau pull (`finance.bureau_pull`), marketing contact (`marketing.promotional`), telematics
(`vehicle.telemetry`), sharing profile with a dealer (`profile.share_with_dealer`).
Missing → `403 identity.consent.required` with
`params: { purpose: "finance.bureau_pull", consent_form_key: "consent.bureau.v2" }`. The client
renders `consent_form_key` through the Form Schema Registry and posts to
`POST /api/v1/identity/consents`. Every consent grant/withdrawal is an immutable audit row with the
policy version, timestamp, IP and the exact text version shown.

---

## 6. Idempotency

### 6.1 Scope

**Every** `POST`, `PUT`, `PATCH`, `DELETE` under `/api/v1/**` requires `Idempotency-Key`, with three
exemptions: `/auth/token/refresh` (the token is the key), `/auth/otp/verify` (the challenge is the
key), and inbound provider webhooks (they carry their own event ID). Missing → `400
common.idempotency.key_required`. There is no opt-out flag; a route that "doesn't need it" still
gets it, because retries are a property of the network, not of the endpoint.

### 6.2 Storage

```sql
CREATE TABLE api_idempotency_keys (
  id                uuid        PRIMARY KEY,           -- uuid v7
  key               text        NOT NULL,
  user_id           uuid        NULL,                  -- NULL for anonymous
  scope_ref         text        NULL,                  -- X-AutoHub-Scope, part of identity
  endpoint          text        NOT NULL,              -- 'POST /api/v1/auction/auctions/{id}/bids'
  request_hash      bytea       NOT NULL,              -- SHA-256 of canonical body + path params
  status            text        NOT NULL,              -- 'in_flight' | 'completed' | 'failed'
  lease_expires_at  timestamptz NULL,                  -- in_flight lease, 60 s
  response_status   int         NULL,
  response_headers  jsonb       NULL,                  -- Location, ETag only
  response_body     jsonb       NULL,                  -- capped at 256 KiB; larger → store URI
  resource_id       uuid        NULL,                  -- what got created, for cheap replay
  created_at        timestamptz NOT NULL DEFAULT now(),
  expires_at        timestamptz NOT NULL,              -- created_at + ttl
  CONSTRAINT uq_idem UNIQUE (key, COALESCE(user_id,'00000000-0000-0000-0000-000000000000'::uuid),
                             COALESCE(scope_ref,''), endpoint)
);
CREATE INDEX ix_idem_expiry ON api_idempotency_keys (expires_at);
```

**The key's identity is `(key, user, scope, endpoint)`.** A key is not global — user A's key never
collides with user B's, and the same key on a different endpoint is a different operation. This is
deliberate: mobile clients that generate one key per user-gesture would otherwise collide across a
"create listing + upload media" gesture pair.

**TTL by route class** (Feature Registry `idempotency.ttl`):

| Class | TTL | Why |
|---|---|---|
| payments, payouts, refunds, invoices | 7 days | Reconciliation windows and gateway retries |
| auction bids | 24 h | Auctions are short; disputes are same-day |
| deals, bookings, offers | 48 h | |
| everything else | 24 h | |

A partitioned-by-day table with `DROP PARTITION` beats a `DELETE … WHERE expires_at < now()` at this
write volume; the sweeper runs at 03:30 IST.

### 6.3 Replay rules

```
INSERT ... ON CONFLICT (key,user,scope,endpoint) DO NOTHING RETURNING id
 ├─ inserted → status='in_flight', lease 60s → execute handler in the SAME transaction
 │              as the business write, then UPDATE to 'completed' with the captured response.
 │              Handler throws → status='failed', row kept (see below).
 └─ conflict → read existing row
      ├─ request_hash ≠ ours          → 409 common.idempotency.key_reused_with_different_body
      ├─ status='in_flight' & lease live → 409 common.idempotency.request_in_flight
      │                                     Retry-After: 1, retry_after_seconds: 1
      ├─ status='in_flight' & lease expired → steal the lease (UPDATE … WHERE lease_expires_at < now()),
      │                                        re-execute. Safe because the business write is in the
      │                                        same transaction as the completion — a crashed handler
      │                                        left nothing behind.
      ├─ status='completed'           → replay stored response verbatim,
      │                                  add Idempotency-Replayed: true
      └─ status='failed' & error was retryable → re-execute (clears the row first)
         status='failed' & error was terminal  → replay the stored 4xx problem+json
```

`Idempotent-Replayed: true` is added to every replayed response along with
`X-AutoHub-Original-Request-Id`. Clients should treat a replay exactly like the original — the point
is that they don't have to care.

**The critical invariant:** the idempotency row and the business write commit in **one Postgres
transaction**. Storing the key before the write (or after) creates a window where a crash produces
either a phantom key or a duplicate charge. Single transaction, no window. For handlers with
external side effects (charging Razorpay), the external call is an outbox-driven step *after*
commit, so the transaction stays short and the external call is itself idempotent via the gateway's
own key.

**Race the design assumes:** two devices, same user, same gesture, different keys → two real
operations. That is correct behaviour; deduplication of *intent* is the business rule's job
(`listings.listing.duplicate_active`, `deals.deal.already_exists_for_listing`), not the transport's.

---

## 7. Collections: pagination, sorting, sparse fields, filtering

### 7.1 Cursor pagination

Offset pagination is banned. Every list endpoint is keyset-paginated.

```
GET /api/v1/listings/listings?page[size]=24&page[cursor]=eyJ2IjoxLCJzIjoiOWY…
```

- `page[size]` default 20, max 100 (max 50 on search, max 500 on dealer export routes).
- `page[cursor]` is opaque, base64url of
  `{"v":1,"s":"<sha256(sort_spec+filter_spec)[0:12]>","k":[<last row's sort key tuple>],"t":<issued_unix>,"snap":"<opensearch pit id|null>"}`,
  HMAC-SHA256 signed with a server key. Tampering or a mismatched `s` →
  `400 common.pagination.invalid_cursor`. This prevents the classic bug where a client changes
  filters but keeps the cursor and gets nonsense.
- Search cursors embed an OpenSearch **PIT (point-in-time)** id so a paging session is stable while
  new listings arrive. PIT TTL 15 min → `410 common.pagination.cursor_expired` after that, and the
  client restarts from page 1 (the UI says *"New results available — refresh"*).
- `meta.page.total_estimate` is `total_is_exact:false` for search (OpenSearch
  `track_total_hits: 10000`) and `true` for small, indexed relational lists (my listings, my deals).
  Never lie about exactness; the UI renders "18,000+" vs "18,423" differently.
- Sort key tuples **always** end in `id` so ordering is total and the keyset is unambiguous.

### 7.2 Sorting

```
?sort=-published_at,price_minor,id
```

Leading `-` = descending. Only fields in the resource's `sortable` set (published in the OpenAPI
schema and at `GET /api/v1/platform/query-capabilities/{resource}`) are accepted; anything else →
`400 common.sort.unsupported_field`. Dynamic taxonomy attributes are sortable when the attribute row
has `is_sortable = true`: `?sort=-attr.boom_length_m`.

Default sorts are **registry data**, not code: `search_defaults` rows in the Feature Registry give
per-resource, per-city, per-user-tier default sorts (a fleet buyer's default differs from a retail
buyer's). Changing the default sort is a config change; it is also a **breaking change** under §1.5
and must be announced.

### 7.3 Sparse fieldsets

```
?fields[listing]=id,title,price_minor,plate,status,primary_media
?fields[listing]=+attr.boom_length_m        // '+' adds to the default projection
```

Reduces payload on the Flutter list screen from ~4 KB to ~600 B per row — a real difference on a
2G connection. Unknown field → `400 common.fields.unknown_field`. Server always returns `id`,
`row_version` and `status` regardless of the projection, because clients break without them.

Related-resource expansion is explicit and capped at depth 1:
`?include=dealer,inspection_summary,valuation_band`. Each `include` is a separately permissioned
projection — a caller without `dealer.dealer.read` gets the listing with `dealer` omitted and a
`meta.warnings[]` entry, not a 403.

### 7.4 The filter grammar

One grammar for every list endpoint, and it must work over attributes that don't exist at compile
time. That is the whole requirement.

```
filter[<path>]=<op>:<value>[,<value>…]
```

`<path>` is either a **first-class field** (`price_minor`, `city_code`, `year`) or a
**dynamic taxonomy attribute** prefixed `attr.` (`attr.fuel_type`, `attr.boom_length_m`,
`attr.refrigeration_hours`).

| Operator | Applies to | Example |
|---|---|---|
| `eq`, `ne` | all | `filter[city_code]=eq:PUN` |
| `in`, `nin` | scalar, enum | `filter[attr.fuel_type]=in:petrol,cng` |
| `gt`,`gte`,`lt`,`lte` | number, date | `filter[km_driven]=lte:60000` |
| `between` | number, date | `filter[price_minor]=between:40000000,80000000` |
| `prefix` | string | `filter[registration_number]=prefix:MH12` |
| `contains` | text, array | `filter[attr.features]=contains:sunroof` |
| `any`, `all` | array attributes | `filter[attr.features]=all:sunroof,abs` |
| `exists` | all | `filter[attr.boom_length_m]=exists:true` |
| `within` | geo | `filter[location]=within:18.5204,73.8567,25km` |
| `bbox` | geo | `filter[location]=bbox:18.4,73.7,18.6,73.9` |

Composition: repeated `filter[...]` params are **AND**ed. `OR` within one field is expressed by
`in`. Cross-field `OR` is deliberately not supported in the query string — the two real use cases
(saved searches, admin segments) use `POST /api/v1/search/queries` with a JSON filter tree, which
supports `{"or":[…]}`, `{"and":[…]}`, `{"not":…}` to a depth of 4 and a clause budget of 64
(`search.query.too_complex` beyond).

**How dynamic attributes are validated and executed**

The Taxonomy Registry (#1) row is the contract:

```
taxonomy_attributes(
  id, category_id, code,               -- 'boom_length_m'
  data_type,                           -- string|int|decimal|bool|date|enum|multi_enum|geo
  unit_code,                           -- 'm' | 'km' | 'litre' | null
  is_filterable, is_facetable, is_sortable, is_required_for_listing,
  enum_set_code,                       -- FK to taxonomy_enum_sets when data_type is enum
  index_field,                         -- 'attr_num.boom_length_m' — the OpenSearch field
  pg_expression,                       -- "(attributes->>'boom_length_m')::numeric" — the PG fallback
  facet_order, label_key, help_key, validation_json
)
```

Request → validation → execution:

```
filter[attr.boom_length_m]=gte:12
   │
   ├─ resolve category context (from filter[category_id] or the endpoint's default)
   ├─ lookup attribute by (category, code) in the Redis-cached taxonomy snapshot
   │     not found                    → 400 common.filter.unknown_field
   │     is_filterable = false        → 400 common.filter.unknown_field  (same code — don't leak schema)
   ├─ check operator ∈ allowed_ops(data_type)
   │     mismatch                     → 400 common.filter.unsupported_operator
   ├─ coerce "12" per data_type=decimal
   │     fail                         → 400 common.filter.invalid_value  (params.expected_type, params.unit)
   └─ emit  OpenSearch: {"range":{"attr_num.boom_length_m":{"gte":12}}}
            Postgres  : (attributes->>'boom_length_m')::numeric >= 12   [GIN + expression index]
```

Attributes are indexed into OpenSearch under **type-partitioned dynamic templates** —
`attr_kw.*` (keyword), `attr_num.*` (double), `attr_bool.*`, `attr_date.*`, `attr_txt.*` — so adding
"Construction Equipment" with `boom_length_m` needs **no mapping change and no deploy**. The
registry publish emits `taxonomy.attribute.created`; the indexer backfills asynchronously and the
attribute flips `is_filterable = true` only when backfill completes. Until then it accepts the
filter and answers from Postgres with `meta.degraded: true`.

**Facets** come back on search responses in registry-declared order:

```jsonc
"facets": [
  { "code": "attr.fuel_type", "label": "Fuel type", "kind": "terms",
    "buckets": [ {"value":"petrol","label":"Petrol","count":8421},
                 {"value":"diesel","label":"Diesel","count":6120},
                 {"value":"cng","label":"CNG","count":1902} ] },
  { "code": "price_minor", "label": "Price", "kind": "range", "unit": "INR_minor",
    "buckets": [ {"from":0,"to":30000000,"count":2210}, … ] }
]
```

`GET /api/v1/platform/query-capabilities/{resource}?category_id=…` returns the machine-readable
filter/sort/facet capability document. Admin filter builders and the dealer's ERP integration read
it instead of hardcoding a field list.

---

## 8. Rate limiting

Policies are **Feature Registry (#6)** rows (`rate_limit_policies`), not `appsettings.json`, so ops
can lift a dealer's ceiling during an auction event without a deploy.

```
rate_limit_policies(code, tier, bucket_class, window_seconds, limit, burst,
                    scope_dimension, city_override, valid_from, valid_to)
```

Buckets are per `(subject, bucket_class)` where subject = user_id, or device_id + IP for anonymous.
Algorithm: sliding-window counter in Redis via a Lua script (atomic read-increment-expire).

| Tier | `read` | `write` | `search` | `bid` | `otp` | `media_upload` | `export` |
|---|---|---|---|---|---|---|---|
| `anonymous` | 60/min | — | 30/min | — | §4.2.1 | — | — |
| `customer` | 300/min | 60/min | 120/min | — | §4.2.1 | 30/hr | 2/day |
| `customer_verified` | 600/min | 120/min | 240/min | — | §4.2.1 | 100/hr | 5/day |
| `dealer_basic` | 1200/min | 300/min | 600/min | 30/min | | 300/hr | 10/day |
| `dealer_pro` | 3000/min | 900/min | 1800/min | 120/min | | 1000/hr | 50/day |
| `partner_api` | 6000/min | 1200/min | 3000/min | — | | — | 200/day |
| `internal_admin` | 6000/min | 1800/min | 3000/min | — | | — | unlimited |
| `service_account` | unmetered, quota-billed | | | | | | |

Notes that matter:

- **Bids get their own bucket.** A dealer hammering the search API must never be able to starve
  their own ability to bid in the last 30 seconds of an auction. `bid` bucket is never shared and
  is never shed under load (§11).
- Bid limits are *per auction* as well: `bid` also carries a per-`(user, auction)` cap of 1 bid /
  600 ms, enforced in the same Lua script as the bid apply, to stop click-storms.
- `429` responses do **not** consume quota.
- Exceeding a bucket 5× in 10 minutes raises a Trust Registry velocity event.

**Headers on every response** (RFC 9331 style, with legacy aliases for one major version):

```
RateLimit-Policy: read;q=600;w=60, write;q=120;w=60
RateLimit: limit=600, remaining=417, reset=23
X-RateLimit-Limit: 600            ← deprecated alias, sunset with v1
X-RateLimit-Remaining: 417
X-RateLimit-Reset: 1785283260
```

On `429`, add `Retry-After: 23` and return `common.rate_limit.exceeded` with
`params: { bucket: "search", limit: 600, window_seconds: 60, tier: "customer_verified" }`.

Clients **must** honour `Retry-After` with full jitter: `sleep = random(0, min(cap, base·2^n))`.
The generated TS client and Dart client do this by default; the retry policy is part of the
generated package, not something each app writes.

---

## 9. Endpoint catalogue

`Perm` column is the required permission string; `self` means "own resource, implicitly granted";
`anon` means no auth. All writes require `Idempotency-Key`. All list endpoints support §7.

### 9.1 `/platform` — the registry plane (detailed in §10)

| Method | Path | Perm | Purpose |
|---|---|---|---|
| GET | `/platform/bootstrap` | anon | One call: versions + feature flags + i18n manifest + geo seed. First call every app makes |
| GET | `/platform/manifest` | anon | Just `registry_versions` + ETags per registry. ~200 B. Cheap poll |
| GET | `/platform/taxonomy/tree` | anon | Full or partial category tree |
| GET | `/platform/taxonomy/nodes/{id}` | anon | Node + attributes + applicable child types |
| GET | `/platform/taxonomy/attributes` | anon | `?category_id=` — attribute definitions |
| GET | `/platform/taxonomy/makes` | anon | `?category_id=` |
| GET | `/platform/taxonomy/models` | anon | `?make_id=&year=` |
| GET | `/platform/taxonomy/variants` | anon | `?model_id=&year=&fuel=` |
| GET | `/platform/forms/{key}` | anon | Form schema + UI schema, `?version=` pinnable |
| POST | `/platform/forms/{key}/validate` | user | Server-side dry-run validation of a draft payload |
| GET | `/platform/workflows/{key}` | anon | States, transitions, guards (client-evaluable subset) |
| GET | `/platform/workflows/{key}/graph` | `platform.registry.read` | Mermaid/DOT for admin visualisation |
| GET | `/platform/i18n/{locale}` | anon | String bundle, `?namespace=` scoped |
| GET | `/platform/i18n/{locale}/diff` | anon | `?since_version=` — delta bundle |
| GET | `/platform/features` | anon/user | Resolved flags for caller × city × tier |
| GET | `/platform/pricing/preview` | user | Fee preview for a hypothetical transaction |
| GET | `/platform/geography/states` | anon | |
| GET | `/platform/geography/cities` | anon | `?state=&serviceable=true` |
| GET | `/platform/geography/pincodes/{pincode}` | anon | City, state, RTO, serviceability, service areas |
| GET | `/platform/geography/rto-offices` | anon | `?state=&code=MH12` |
| GET | `/platform/query-capabilities/{resource}` | anon | Filter/sort/facet capability doc (§7.4) |
| GET | `/platform/error-codes` | anon | Machine catalogue of §3, with deprecations |
| GET | `/platform/deprecations` | anon | Live deprecation schedule (§1.6) |
| GET | `/platform/config/public` | anon | Non-secret client config: upload limits, polling intervals, hub URLs |
| **Admin (registry authoring)** | | | |
| GET | `/platform/admin/registries/{registry}/drafts` | `platform.registry.read` | |
| POST | `/platform/admin/registries/{registry}/drafts` | `platform.registry.write` | Create draft from current published |
| PUT | `/platform/admin/registries/{registry}/drafts/{id}` | `platform.registry.write` | `If-Match` required |
| POST | `/platform/admin/registries/{registry}/drafts/{id}/validate` | `platform.registry.write` | Static + referential validation |
| POST | `/platform/admin/registries/{registry}/drafts/{id}/simulate` | `platform.registry.write` | Replay N historical requests against the draft. **The safety net** |
| POST | `/platform/admin/registries/{registry}/drafts/{id}/publish` | `platform.registry.publish` | step-up; maker-checker; bumps version; invalidates cache |
| POST | `/platform/admin/registries/{registry}/versions/{v}/rollback` | `platform.registry.publish` | step-up |
| GET | `/platform/admin/registries/{registry}/versions` | `platform.registry.read` | Version history + diff |
| GET | `/platform/admin/registries/{registry}/versions/{a}/diff/{b}` | `platform.registry.read` | |
| GET | `/platform/admin/audit` | `platform.audit.read` | who/what/before/after/when/where |

`{registry}` ∈ `taxonomy | forms | workflows | pricing | rbac | features | notifications | integrations | i18n | geography`. **One authoring API for all ten** — that is what makes the tenth registry cost nothing to add.

### 9.2 `/identity`

| Method | Path | Perm | Notes |
|---|---|---|---|
| — | `/identity/auth/*` | | §4.8 |
| GET | `/identity/me` | self | Profile |
| PATCH | `/identity/me` | self | Fields validated against form key `profile.customer.v2` |
| GET | `/identity/me/preferences` | self | Locale, notification channels, density |
| PUT | `/identity/me/preferences` | self | |
| POST | `/identity/me/phone/change` | self, step-up | Dual OTP: old + new |
| GET | `/identity/consents` | self | DPDP consent ledger |
| POST | `/identity/consents` | self | Grant a purpose |
| DELETE | `/identity/consents/{purpose}` | self | Withdraw |
| POST | `/identity/me/data-export` | self, step-up | DPDP §11 access right. 202 + job |
| POST | `/identity/me/erasure-request` | self, step-up | DPDP §12. 202; legal-retention holds surface in the response |
| POST | `/identity/kyc/sessions` | self | Start KYC. `{level, method:"digilocker"\|"aadhaar_otp"\|"manual"}` |
| GET | `/identity/kyc/sessions/{id}` | self | Poll status |
| POST | `/identity/kyc/sessions/{id}/aadhaar/otp` | self | |
| POST | `/identity/kyc/sessions/{id}/aadhaar/verify` | self | |
| POST | `/identity/kyc/sessions/{id}/documents` | self | PAN/DL/RC upload → OCR |
| GET | `/identity/kyc/status` | self | Current level + what each level unlocks |
| GET | `/identity/users` | `identity.user.read_any` | Admin search |
| POST | `/identity/users/{id}/roles` | `identity.role.assign` | Scoped assignment |
| POST | `/identity/users/{id}/suspend` | `identity.user.suspend` | step-up |

**KYC levels are registry data** (`identity_kyc_levels` in the Feature Registry): L0 phone verified,
L1 + PAN, L2 + Aadhaar/DigiLocker, L3 + bank penny-drop, L4 + physical/dealer premises. Each level's
unlocked capabilities are a permission list in the registry, so "bidding now needs L3" is a config
change.

### 9.3 `/catalog` — vehicle identity

| Method | Path | Perm | Notes |
|---|---|---|---|
| GET | `/catalog/vehicles/{id}` | self / `catalog.vehicle.read_any` | Canonical vehicle + provenance timeline |
| POST | `/catalog/vehicles` | `catalog.vehicle.create` | From RC lookup or manual |
| PATCH | `/catalog/vehicles/{id}` | self | |
| POST | `/catalog/rc-lookups` | user | `{registration_number}` → VAHAN. Cached 24 h. Returns masked owner name |
| GET | `/catalog/rc-lookups/{id}` | self | Poll if 202 |
| POST | `/catalog/vin-decodes` | user | `{vin}` → make/model/variant/year |
| POST | `/catalog/challan-checks` | user | e-Challan; `{registration_number}` → pending challans + total |
| GET | `/catalog/vehicles/{id}/hypothecation` | self | Financier, loan account (masked), NOC status |
| POST | `/catalog/vehicles/{id}/ownership-claims` | user | Claim ownership → OTP to RC-registered mobile |
| GET | `/catalog/vehicles/{id}/history` | self | Ownership, insurance, inspection, accident, odometer timeline |

**RC lookup response shape** — the thing three screens depend on:

```jsonc
{ "data": {
  "registration_number": "MH12AB1234",
  "plate_class": "private",                         // private|commercial|ev — drives PlateBlock color
  "registered_at": "2019-08-14",
  "rto": { "code": "MH12", "name": "Pune", "city_code": "PUN", "state_code": "MH" },
  "owner_name_masked": "R**** R*******",
  "owner_serial": 2,                                 // "2nd owner" — a top-3 pricing factor in India
  "maker": "Maruti Suzuki", "model": "Baleno", "variant_hint": "Zeta 1.2 Petrol",
  "taxonomy": { "category_id": "…", "make_id": "…", "model_id": "…", "variant_id": null,
                "confidence": 0.86, "needs_confirmation": true },
  "fuel_type": "petrol", "cubic_capacity": 1197, "seating_capacity": 5,
  "chassis_masked": "MA3********12345", "engine_masked": "K12*****",
  "fitness_valid_till": "2034-08-13",
  "insurance": { "valid_till": "2026-11-02", "insurer": "ICICI Lombard", "policy_masked": "…4821" },
  "puc_valid_till": "2026-09-30",
  "hypothecation": { "active": true, "financier": "HDFC Bank Ltd" },
  "is_blacklisted": false, "is_scrapped": false,
  "hsrp_status": "affixed",
  "source": "vahan", "as_of": "2026-07-29T05:02:11Z", "cache_hit": true
} }
```

`taxonomy.needs_confirmation` is why this endpoint exists in this shape: VAHAN's `model` string is
free text (`"BALENO ZETA 1.2 K12N"`), so we map it to registry IDs with a confidence score and make
the user confirm the variant. Never silently trust an upstream string mapping.

### 9.4 `/listings`

| Method | Path | Perm | Notes |
|---|---|---|---|
| GET | `/listings/listings` | anon | Public browse. Supports §7 fully |
| GET | `/listings/listings/{id}` | anon | 404 if not visible |
| GET | `/listings/listings/slug/{slug}` | anon | SEO route for Next.js SSR |
| POST | `/listings/listings` | `listings.listing.create` | Body = form-schema payload (§9.4.1) |
| PATCH | `/listings/listings/{id}` | `listings.listing.update` + scope | merge-patch, `If-Match` |
| DELETE | `/listings/listings/{id}` | `listings.listing.delete` + scope | Soft delete |
| POST | `/listings/listings/{id}/transitions` | per-transition perm | **The only state-change route** |
| GET | `/listings/listings/{id}/transitions` | self | Available actions with reasons for unavailability |
| GET | `/listings/listings/{id}/history` | self | Workflow timeline for `WorkflowTimeline` |
| POST | `/listings/listings/{id}/media/upload-intents` | scope | Returns presigned S3 PUT URLs |
| POST | `/listings/listings/{id}/media` | scope | Confirm uploads, set order/primary |
| PATCH | `/listings/listings/{id}/media/{mediaId}` | scope | Reorder, caption, tag (`exterior_front`, `odometer`) |
| DELETE | `/listings/listings/{id}/media/{mediaId}` | scope | |
| POST | `/listings/listings/{id}/boost` | `listings.listing.promote` | Paid promotion; creates a payment order |
| POST | `/listings/listings/{id}/renew` | scope | Extends `expires_at` per Pricing Registry |
| GET | `/listings/listings/{id}/similar` | anon | Vector + attribute similarity |
| GET | `/listings/listings/{id}/price-insight` | anon | Where this price sits vs the valuation band |
| GET | `/listings/my` | self | My listings across intents |
| POST | `/listings/enquiries` | user | Creates enquiry + chat thread + lead |
| GET | `/listings/enquiries` | self / `listings.enquiry.read_any` | |
| POST | `/listings/listings/{id}/report` | user | Abuse report → Trust module |
| GET | `/listings/saved` | self | Wishlist |
| PUT | `/listings/saved/{listingId}` | self | |

#### 9.4.1 Create-listing request — how form-schema drives it

```http
POST /api/v1/listings/listings
Idempotency-Key: 0193c2f1-8a44-7b31-9e02-1f7c2d3a4b55
X-AutoHub-Scope: dealer:0193ab...
Content-Type: application/json
```
```jsonc
{
  "form_schema_key": "listing.sell.car.v7",       // REQUIRED. Client echoes the schema it rendered
  "form_schema_version": 7,
  "intent": "sale",                                // registry-driven: sale|auction|rent|lease|attach|exchange
  "vehicle": { "vehicle_id": "0193…", "registration_number": "MH12AB1234" },
  "category_id": "0193…",
  "price": { "amount_minor": 84000000, "currency": "INR", "is_negotiable": true },
  "location": { "pincode": "411045", "city_code": "PUN", "lat": 18.5679, "lng": 73.7761 },
  "attributes": {                                  // validated against taxonomy for category_id
    "km_driven": 42800, "owner_count": 2, "fuel_type": "petrol",
    "transmission": "manual", "insurance_valid_till": "2026-11-02",
    "features": ["abs","touchscreen","reverse_camera"]
  },
  "description": "…",
  "seller_preferences": { "allow_exchange": true, "finance_assist": true }
}
```

`form_schema_version` mismatch with the current published version → the write still succeeds if the
old version is within the compatibility window, and `meta.warnings[]` carries
`{"code":"platform.form.schema_outdated","new_version":8}` so the client can refresh its cached form
before the next render. If the old version is outside the window → `409 platform.registry.version_not_found`.
This is the difference between "we shipped a form change and every stale mobile app broke" and
"stale apps degrade gracefully for two weeks".

Validation is **the same JSON Schema** the client rendered, executed server-side. There is no second
hand-written validator. `422` `errors[]` pointers map 1:1 onto the rendered fields.

#### 9.4.2 Transition request

```http
POST /api/v1/listings/listings/{id}/transitions
```
```jsonc
{ "action": "submit_for_review",
  "expected_state": "draft",               // optimistic guard; mismatch → 409
  "payload": { }                            // validated against the transition's form_schema_key
}
```

Response is the full updated resource with the new `status` + `available_actions`, plus
`meta.effects[]` naming side effects fired (`notification.sent`, `moderation.queued`,
`search.reindex_queued`) so an integration test can assert on them.

Errors: `platform.workflow.transition_not_allowed`, `platform.workflow.guard_failed`
(with `params.guard: "listing_has_min_photos"` and `params.failed_conditions[]`),
`platform.workflow.terminal_state`, `listings.listing.incomplete_for_publish`.

### 9.5 `/search`

| Method | Path | Perm | Notes |
|---|---|---|---|
| GET | `/search/listings` | anon | Full §7 grammar + `q=` + facets |
| POST | `/search/queries` | anon | JSON filter tree for complex/OR queries; returns same shape |
| GET | `/search/suggest` | anon | Typeahead: makes, models, cities, plates. ≤ 60 ms p99 |
| GET | `/search/facets` | anon | Facets only, no hits — for filter panels |
| GET | `/search/trending` | anon | `?city_code=` |
| POST | `/search/saved-searches` | self | Name + filter tree + alert cadence |
| GET | `/search/saved-searches` | self | |
| DELETE | `/search/saved-searches/{id}` | self | |
| POST | `/search/saved-searches/{id}/mute` | self | |
| GET | `/search/recent` | self | Server-side recent searches (cross-device) |

Search response adds to `meta`: `query_id` (for click-through attribution),
`took_ms`, `engine` (`opensearch` \| `postgres_fallback`), `applied_filters[]` (echoed and
normalised so the UI renders chips from the server's interpretation, not its own).

**Degradation:** OpenSearch unavailable → automatic Postgres fallback with reduced capability
(no fuzzy `q`, no facets, no relevance sort). Response carries `meta.degraded: true`,
`X-AutoHub-Degraded: search`, and `meta.warnings[{"code":"search.index.degraded"}]`. The UI shows
*"Showing basic results — full search is recovering."* Only if Postgres also fails →
`503 search.index.unavailable`.

### 9.6 `/valuation`

| Method | Path | Perm | Notes |
|---|---|---|---|
| POST | `/valuation/quotes` | anon | **The homepage hero.** Plate or manual input |
| GET | `/valuation/quotes/{id}` | anon (token-scoped) | Poll if 202 |
| POST | `/valuation/quotes/{id}/refine` | anon | Add condition answers → tighter band |
| GET | `/valuation/quotes/{id}/breakdown` | anon | Factor contributions for the explainer UI |
| POST | `/valuation/quotes/{id}/accept-offer` | user | Converts to an instant-sale deal |
| GET | `/valuation/history/{vehicleId}` | self | Depreciation curve |
| GET | `/valuation/market-trends` | anon | `?model_id=&city_code=` |
| POST | `/valuation/bulk` | `valuation.bulk.request` | Dealer: up to 500 vehicles, 202 + job |

```jsonc
// POST /valuation/quotes → 200
{ "data": {
  "quote_id": "0193…",
  "vehicle_summary": { "make":"Maruti Suzuki","model":"Baleno","variant":"Zeta 1.2",
                       "year":2019, "plate":"MH12AB1234", "plate_class":"private" },
  "estimate": { "point_minor": 61500000, "low_minor": 57800000, "high_minor": 65200000,
                "currency":"INR", "confidence": 0.82, "confidence_band":"high" },
  "channels": [                                   // India: the same car is worth 3 different numbers
    { "code":"instant_sale",   "label":"Sell to AutoHub now",  "point_minor": 57500000,
      "valid_until":"2026-08-05T18:29:59Z" },
    { "code":"dealer_auction", "label":"Dealer auction",       "low_minor": 59000000, "high_minor": 63000000 },
    { "code":"private_sale",   "label":"Sell privately",       "low_minor": 62000000, "high_minor": 68000000,
      "expected_days": 21 }
  ],
  "factors": [
    { "code":"owner_count", "label":"2nd owner", "impact_minor": -2100000, "direction":"down" },
    { "code":"km_driven",   "label":"42,800 km", "impact_minor":  900000,  "direction":"up" },
    { "code":"city_demand", "label":"Pune demand", "impact_minor": 450000, "direction":"up" },
    { "code":"insurance_expiring", "label":"Insurance expires in 3 months", "impact_minor": -150000, "direction":"down" }
  ],
  "as_of": "2026-07-29T06:14:22Z",
  "valid_until": "2026-08-05T06:14:22Z",
  "model_version": "val-car-v4.2.1",
  "missing_inputs": ["condition_grade"],           // drives the "refine" CTA
  "next_form_schema_key": "valuation.refine.car.v3"
}}
```

Design-system tie: `estimate.point_minor` is the number that gets the one odometer roll
(01-design-system §5). `as_of` is mandatory in the response because §7 of that doc says a valuation
without a date is a rumor — the API makes it impossible to render one without it.

**Anonymous quotes** are allowed (the hero promises "no phone number needed") and are bound to a
short-lived `quote_token` returned in a `Set-Cookie` + response body, so the poll and refine calls
work without an account. Converting to a deal requires auth.

### 9.7 `/inspection`

| Method | Path | Perm | Notes |
|---|---|---|---|
| GET | `/inspection/packages` | anon | Pricing Registry driven, city-scoped |
| GET | `/inspection/slots` | anon | `?pincode=&date=&package_code=` |
| POST | `/inspection/bookings` | user | Creates booking + payment order |
| GET | `/inspection/bookings/{id}` | self | |
| POST | `/inspection/bookings/{id}/transitions` | scoped | reschedule / cancel / start / complete |
| GET | `/inspection/reports/{id}` | self / party | Full report |
| GET | `/inspection/reports/{id}/pdf` | self / party | Signed URL, 15 min |
| POST | `/inspection/reports/{id}/dispute` | self | |
| **Inspector app** | | | |
| GET | `/inspection/assignments` | `inspection.assignment.read` | Today's jobs |
| POST | `/inspection/assignments/{id}/checklist` | `inspection.report.write` | **Offline-first**: array of answers with client timestamps |
| POST | `/inspection/assignments/{id}/media` | `inspection.report.write` | Photos with required tags + EXIF geo |
| POST | `/inspection/assignments/{id}/submit` | `inspection.report.write` | Locks the report |

The checklist is a **Form Schema Registry** document (`inspection.checklist.car.v5`) with 180+ points
grouped in sections. The inspector app caches it by ETag and works fully offline; each answer carries
`client_recorded_at` and a client-generated `answer_id`, and the sync endpoint is idempotent per
`answer_id` so a partial upload over a bad connection resumes without duplicating. Conflict rule:
**last write by `client_recorded_at` wins within one assignment**, because there is exactly one
inspector per assignment and the server clock is not the truth about when a car was photographed.

### 9.8 `/auction`

| Method | Path | Perm | Notes |
|---|---|---|---|
| GET | `/auction/auctions` | `auction.auction.read` | Live / upcoming / ended |
| GET | `/auction/auctions/{id}` | `auction.auction.read` | Full lot + current state |
| GET | `/auction/auctions/{id}/state` | `auction.auction.read` | **Tiny polling fallback**: `{current_bid_minor, bid_count, ends_at, seq, your_position}`. ~120 B, `Cache-Control: no-store` |
| POST | `/auction/auctions` | `auction.auction.create` | Admin/dealer-consignment |
| POST | `/auction/auctions/{id}/transitions` | per-transition | schedule/open/pause/close/settle |
| POST | `/auction/auctions/{id}/registrations` | `auction.registration.create` | Register to bid; may require EMD |
| GET | `/auction/auctions/{id}/registrations/me` | self | Deposit status, bidding limit |
| **POST** | **`/auction/auctions/{id}/bids`** | **`auction.bid.place`** | **§9.8.1** |
| GET | `/auction/auctions/{id}/bids` | `auction.bid.read` | Bid history (masked bidder identities) |
| POST | `/auction/auctions/{id}/proxy-bids` | `auction.bid.place` | Max-bid autobid |
| DELETE | `/auction/auctions/{id}/proxy-bids/me` | self | |
| POST | `/auction/bids/{bidId}/retract` | `auction.bid.retract` | Within registry-configured window |
| GET | `/auction/auctions/{id}/increments` | `auction.auction.read` | Increment ladder (Pricing Registry) |
| POST | `/auction/auctions/{id}/reserve` | `auction.auction.set_reserve` | Seller-only, hidden from bidders |
| POST | `/auction/auctions/{id}/close-offers` | `auction.auction.moderate` | Below-reserve negotiation |
| GET | `/auction/my/watchlist` | self | |
| PUT | `/auction/my/watchlist/{auctionId}` | self | |
| GET | `/auction/my/bids` | self | Across auctions, with position |
| GET | `/auction/my/wins` | self | Won lots + settlement deadlines |
| GET | `/auction/events` | `auction.auction.read` | Auction events/sales calendar |

#### 9.8.1 Place bid — the most contended endpoint in the product

```http
POST /api/v1/auction/auctions/0193c2f1-.../bids
Idempotency-Key: 01J8XK3M2E7QF4N9P0R5T6V7W8
```
```jsonc
{ "amount_minor": 84500000,
  "currency": "INR",
  "seen_bid_seq": 41,              // the last bid sequence this client had rendered
  "client_ts": "2026-07-29T06:14:22.104Z",
  "max_proxy_minor": 92000000      // optional: also arm/raise a proxy bid
}
```

```jsonc
// 201 Created
{ "data": {
  "bid_id": "0193c2f5-…",
  "seq": 42,
  "amount_minor": 84500000,
  "placed_at": "2026-07-29T06:14:22.311Z",
  "is_winning": true,
  "your_position": 1,
  "auction": {
    "current_bid_minor": 84500000,
    "min_next_bid_minor": 84800000,
    "bid_count": 42,
    "ends_at": "2026-07-29T06:20:00Z",
    "extended_by_seconds": 120,      // anti-snipe fired
    "extension_count": 3,
    "reserve_met": true,             // boolean only — never the reserve amount
    "status": { "code": "live", … }
  },
  "server_seq": 42                   // reconcile with the WebSocket stream (§12)
}}
```

**Server-side ordering and the race.** Truth for a live auction is a Redis hash
`auction:{id}:state` mutated only by a Lua script that, atomically:

```
1. assert state == 'live' and now within [starts_at, ends_at + skew]
2. assert bidder registered, not seller, exposure within limit
3. read current_bid, current_seq
4. if req.seen_bid_seq < current_seq and req.amount <= current_bid  → return OUTBID_RACE
5. if req.amount < min_next_bid                                     → return BELOW_INCREMENT
6. resolve proxy bids: if an existing proxy max >= req.amount + inc,
   the proxy auto-counters and the incoming bid becomes seq n, proxy becomes seq n+1
7. current_bid = req.amount; current_seq += 1
8. if (ends_at - now) < soft_close_window and extension_count < max:
       ends_at += extension_seconds; extension_count += 1
9. XADD auction:{id}:stream  → the WebSocket fan-out and the replay buffer
10. return {seq, ends_at, min_next_bid, winning}
```

The Postgres write (`auction_bids`, unique on `(auction_id, seq)`) happens immediately after via the
outbox consumer. Redis is the **sequencer**; Postgres is the **record**. If Redis loses the hash
(failover), the auction transitions to `paused`, the state is rebuilt from `auction_bids` by max
`seq`, and it resumes — with the `ends_at` extended by the pause duration, because bidders were
denied their last seconds. That rule is a workflow-registry transition (`pause → resume`) with
`effects_json: {"extend_end_by_pause_duration": true}`, not code.

`seen_bid_seq` is what prevents the ugliest UX in auctions: you see ₹8,42,000, tap "+₹3,000", and by
the time your packet lands the price is ₹8,50,000 — so your ₹8,45,000 either loses silently or, worse,
is accepted as a losing bid. With `seen_bid_seq`, the server returns
`409 auction.bid.outbid_race` with `params: { current_bid_minor, min_next_bid_minor, current_seq }`
and the client re-renders and asks again. Explicit, one round-trip, no surprise.

**Anti-snipe (soft close), increments, EMD %, exposure multiplier, retraction window** are all
Pricing/Feature Registry rows on the auction event:

| Registry key | Example value | Effect |
|---|---|---|
| `auction.soft_close.window_seconds` | 120 | Bid inside last 120 s extends |
| `auction.soft_close.extension_seconds` | 120 | By this much |
| `auction.soft_close.max_extensions` | 20 | Hard stop |
| `auction.increment.ladder` | `[{to:50000000,inc:100000},{to:200000000,inc:250000},{to:null,inc:500000}]` | `min_next_bid` |
| `auction.emd.percent_bps` | 500 (5%) | Deposit required |
| `auction.exposure.multiplier` | 10 | Max live exposure = EMD × 10 |
| `auction.buyer_premium` | rate_bps 200, min ₹5,000, max ₹50,000, GST 18% | Fee |
| `auction.settlement.window_hours` | 48 | Miss it → forfeit flow |

Bidder identity in `GET /bids` is masked to `"Dealer #7 (Pune)"` unless the caller has
`auction.bid.read_identities` — anti-collusion, and it's how real dealer auctions run.

### 9.9 `/deals` — the settlement spine

| Method | Path | Perm | Notes |
|---|---|---|---|
| POST | `/deals/deals` | `deals.deal.create` | From listing / auction win / valuation offer |
| GET | `/deals/deals/{id}` | party | Full deal: parties, money, milestones, documents |
| GET | `/deals/deals` | self | My deals, `?role=buyer\|seller` |
| POST | `/deals/deals/{id}/transitions` | per-transition | The whole lifecycle |
| GET | `/deals/deals/{id}/transitions` | party | Available actions |
| POST | `/deals/deals/{id}/offers` | party | Offer / counter-offer |
| POST | `/deals/offers/{id}/accept` | party | |
| POST | `/deals/offers/{id}/reject` | party | |
| POST | `/deals/quotes` | user | **Fee preview before committing.** Full Pricing Registry breakdown |
| GET | `/deals/deals/{id}/ledger` | party / `deals.ledger.read_any` | Double-entry rows |
| GET | `/deals/deals/{id}/documents` | party | RC transfer set, Form 29/30, delivery note |
| POST | `/deals/deals/{id}/documents/{type}/sign` | party | Aadhaar eSign |
| POST | `/deals/deals/{id}/rc-transfer` | party | Initiate RTO transfer; tracks Form 29/30, NOC, HSRP |
| GET | `/deals/deals/{id}/rc-transfer` | party | Status + what's blocking |
| POST | `/deals/deals/{id}/disputes` | party | Freezes payout (`common.resource.locked` on payout routes) |
| POST | `/deals/deals/{id}/cancel` | party | Cancellation fee per Pricing Registry |
| GET | `/deals/deals/{id}/invoice` | party | GST invoice + IRN + QR |
| POST | `/deals/deals/{id}/payouts` | `payments.payout.release` | Maker-checker, step-up |

**Deal quote response** — this is the endpoint that makes "no hardcoded percentage" tangible:

```jsonc
{ "data": {
  "quote_id": "0193…", "deal_kind": "c2c_sale", "vehicle_price_minor": 84000000,
  "lines": [
    { "code":"platform.buyer_fee",  "label":"Platform fee",
      "base_minor":84000000, "rate_bps":150, "amount_minor":1260000,
      "min_minor":300000, "max_minor":2500000, "applied":"rate",
      "gst_rate_bps":1800, "gst_minor":226800, "hsn_sac":"998599",
      "pricing_rule_id":"0193ab…", "plan_code":"c2c.pune.v4" },
    { "code":"rc_transfer.assist", "label":"RC transfer assistance",
      "flat_minor":250000, "amount_minor":250000,
      "gst_rate_bps":1800, "gst_minor":45000, "hsn_sac":"998599",
      "pricing_rule_id":"0193ac…" },
    { "code":"tcs.206c_1f", "label":"TCS (1%)", "base_minor":84000000,
      "rate_bps":100, "amount_minor":840000, "gst_rate_bps":0,
      "statutory":true, "collected_on_behalf_of":"income_tax_department",
      "condition":"sale_value >= 1000000 && buyer_pan_present",
      "pricing_rule_id":"0193ad…" }
  ],
  "totals": { "buyer_pays_minor": 86621800, "seller_receives_minor": 82513200,
              "platform_revenue_minor": 1510000, "gst_total_minor": 271800,
              "statutory_total_minor": 840000 },
  "escrow": { "required": true, "hold_until_milestone": "rc_transfer_initiated",
              "nodal_account_ref": "RZP-NODAL-…" },
  "valid_until": "2026-07-29T07:14:22Z",
  "pricing_registry_version": 231
}}
```

TCS under §206C(1F) applies on motor vehicle sale consideration ≥ ₹10,00,000 — modelled as a
**pricing rule with a condition expression**, not an `if` in C#, so a Finance Act change is a
registry publish. If the buyer's PAN is missing on a qualifying sale →
`422 payments.tcs.pan_missing` before the deal can proceed to payment.

**Escrow / RBI reality:** collected funds sit in a **nodal/escrow account** operated by the PA
(Razorpay/Cashfree) under RBI's Payment Aggregator directions. AutoHub never holds customer funds in
an operating account. Settlement to the seller is triggered by a workflow transition
(`rc_transfer_initiated` or `delivery_confirmed`, per the deal type's workflow definition) and the
T+n release is a registry row, not a constant. A dispute sets a hold that makes every payout route
return `423 deals.settlement.hold_active`.

**Hypothecation and challans block transfer:** the workflow guard `can_transfer_ownership` checks
`hypothecation.active == false || noc_document_verified == true` and
`outstanding_challan_minor <= threshold`. Both surface as
`409 deals.deal.hypothecation_active` / `409 deals.deal.challan_outstanding` with
`params` naming exactly what to clear. This is the single most common real-world reason an Indian
used-car sale stalls, so it is a first-class API state, not an afterthought.

### 9.10 `/payments`

| Method | Path | Perm | Notes |
|---|---|---|---|
| POST | `/payments/orders` | user | `{purpose, ref_id, amount_minor}` → gateway order + client key |
| GET | `/payments/orders/{id}` | self | |
| POST | `/payments/orders/{id}/verify` | user | Client-side handshake verification |
| GET | `/payments/methods` | user | Integration Registry: UPI, cards, netbanking, EMI, wallet |
| POST | `/payments/upi/collect` | user | Push a UPI collect request to a VPA |
| GET | `/payments/payments/{id}` | self | |
| POST | `/payments/payments/{id}/refunds` | `payments.refund.create` | |
| GET | `/payments/wallet` | self | Balance + holds |
| GET | `/payments/wallet/transactions` | self | |
| POST | `/payments/payout-accounts` | self, step-up | Bank/UPI + penny-drop verification |
| GET | `/payments/payout-accounts` | self | |
| POST | `/payments/payouts` | `payments.payout.release` | Maker; needs a checker |
| POST | `/payments/payouts/{id}/approve` | `payments.payout.approve` | Checker; must differ from maker |
| GET | `/payments/invoices/{id}` | party | GST invoice, IRN, signed QR |
| GET | `/payments/invoices/{id}/pdf` | party | |
| POST | `/webhooks/payments/{provider}` | signature | **Inbound**, not `/api/v1` (§13.4) |

### 9.11 `/bookings` — one module, many appointment types

| Method | Path | Perm | Notes |
|---|---|---|---|
| GET | `/bookings/types` | anon | Registry-driven: `test_drive`, `inspection`, `showroom_visit`, `rental_pickup`, `doorstep_valuation` |
| GET | `/bookings/availability` | anon | `?type=&ref_id=&date_from=&date_to=&pincode=` |
| POST | `/bookings/bookings` | user | Payload validated by the type's form schema |
| GET | `/bookings/bookings/{id}` | party | |
| GET | `/bookings/bookings` | self | |
| POST | `/bookings/bookings/{id}/transitions` | party | confirm/reschedule/cancel/no_show/complete |
| POST | `/bookings/bookings/{id}/otp/start` | party | Start-of-service OTP (driver/inspector arrives) |
| POST | `/bookings/bookings/{id}/otp/verify` | party | Proof-of-service. The anti-fraud primitive for doorstep ops |
| GET | `/bookings/bookings/{id}/tracking` | party | Live agent location during the window |

Availability is computed from Geography Registry operating hours, the resource's calendar, and a
Redis soft-hold. `POST /bookings` takes a 90-second **hold** on the slot at quote time
(`slot_hold_token` returned by `/availability`); confirming without a valid hold races and may return
`409 bookings.slot.unavailable`. Without the hold, two users on the same slot at the same second both
see success and one gets a call from ops — the classic Indian test-drive double-book.

### 9.12 `/dealer` — inventory, CRM, subscription

| Method | Path | Perm | Notes |
|---|---|---|---|
| POST | `/dealer/dealers` | user | Onboarding application |
| GET | `/dealer/dealers/{id}` | scope | Profile |
| PATCH | `/dealer/dealers/{id}` | `dealer.dealer.update` | |
| POST | `/dealer/dealers/{id}/transitions` | `dealer.dealer.verify` | Verification workflow |
| GET | `/dealer/dealers/{id}/branches` | scope | |
| POST | `/dealer/dealers/{id}/branches` | `dealer.branch.create` | |
| GET | `/dealer/dealers/{id}/staff` | `dealer.staff.read` | |
| POST | `/dealer/dealers/{id}/staff` | `dealer.staff.invite` | Assigns a scoped role |
| **Inventory** | | | |
| GET | `/dealer/inventory-items` | `dealer.inventory.read` + scope | Full §7 grammar; `?branch_id=&status=&aging_days_gte=` |
| POST | `/dealer/inventory-items` | `dealer.inventory.write` | Stock-in (may precede a listing) |
| GET | `/dealer/inventory-items/{id}` | scope | Item + linked listing + costs + aging |
| PATCH | `/dealer/inventory-items/{id}` | `dealer.inventory.write` | |
| POST | `/dealer/inventory-items/{id}/transitions` | scoped | in_stock → listed → reserved → sold → returned |
| POST | `/dealer/inventory-items/{id}/costs` | `dealer.inventory.write` | Refurb, transport, parking — feeds margin |
| POST | `/dealer/inventory-items/bulk` | `dealer.inventory.bulk_import` | 202 + `job_id` |
| GET | `/dealer/inventory-items/bulk/{jobId}` | scope | Per-row results with row numbers and error codes |
| GET | `/dealer/inventory-items/bulk/template` | scope | CSV template **generated from the taxonomy registry** for the dealer's categories |
| POST | `/dealer/inventory-items/bulk/price-update` | `dealer.inventory.bulk_price` | step-up over N items |
| GET | `/dealer/inventory-items/export` | `dealer.inventory.export` | 202 → S3 signed URL |
| GET | `/dealer/inventory-items/aging` | `dealer.inventory.read` | Aging buckets + suggested price cuts |
| **Demand & CRM** | | | |
| GET | `/dealer/leads` | `dealer.lead.read` + scope | |
| POST | `/dealer/leads/{id}/assign` | `dealer.lead.assign` | |
| POST | `/dealer/leads/{id}/activities` | `dealer.lead.write` | Call/visit/note |
| GET | `/dealer/requirements` | `dealer.requirement.read` | Buyer requirement board |
| POST | `/dealer/requirements/{id}/proposals` | `dealer.requirement.propose` | |
| **Commercials** | | | |
| GET | `/dealer/subscription` | scope | Plan, limits, usage |
| POST | `/dealer/subscription/change` | `dealer.subscription.manage` | |
| GET | `/dealer/statements` | `dealer.statement.read` | Monthly commission statement |
| GET | `/dealer/analytics/summary` | `dealer.analytics.read` | Views, leads, conversion, avg days-to-sell |

Bulk upload is the endpoint dealers judge us on. Rules: streaming parse, per-row idempotency using
`(job_id, row_number)`, **partial success is the default** (valid rows are created, invalid rows are
reported), and the result file contains the original row plus `error_code` + `error_message` +
`pointer`, so a dealer's ops person fixes and re-uploads only the failures. The template's columns
are generated from the taxonomy registry — add an attribute, the template grows, no release.

### 9.13 `/rental`, `/lease`, `/attachment`

Same listing/deal spine, different intents (North Star §1). Only intent-specific routes listed.

| Method | Path | Perm | Notes |
|---|---|---|---|
| GET | `/rental/availability/{listingId}` | anon | Calendar + blackout dates |
| POST | `/rental/bookings` | user | Dates, driver option, delivery mode |
| POST | `/rental/bookings/{id}/transitions` | party | confirm/pickup/extend/return/close |
| POST | `/rental/bookings/{id}/handover` | party | Odometer, fuel, damage photos, OTP |
| POST | `/rental/bookings/{id}/damages` | party | Deposit deduction proposal |
| GET | `/rental/pricing-preview` | anon | Per-day/per-km/deposit from Pricing Registry |
| POST | `/rental/driver-licences/verify` | user | DL validity + class check |
| GET | `/lease/plans` | anon | Tenure × down-payment × km-cap matrix |
| POST | `/lease/applications` | user | |
| GET | `/lease/applications/{id}` | self | Includes lender decisions |
| POST | `/lease/applications/{id}/documents` | self | |
| POST | `/lease/applications/{id}/transitions` | scoped | |
| GET | `/attachment/aggregators` | anon | Ola/Uber/local fleet partners |
| POST | `/attachment/enrolments` | user | Attach my car to a fleet |
| GET | `/attachment/enrolments/{id}` | self | Permit/fitness/badge compliance checklist |
| POST | `/attachment/enrolments/{id}/transitions` | scoped | |
| GET | `/attachment/earnings-estimate` | anon | City × vehicle-class earning bands |

### 9.14 `/finance`, `/insurance`

| Method | Path | Perm | Notes |
|---|---|---|---|
| GET | `/finance/lenders` | anon | Active lenders for city/vehicle-age |
| POST | `/finance/eligibility` | user | Soft check, no bureau pull |
| POST | `/finance/applications` | user | Requires `finance.bureau_pull` consent |
| GET | `/finance/applications/{id}` | self | Per-lender offers + statuses |
| POST | `/finance/applications/{id}/documents` | self | |
| POST | `/finance/applications/{id}/offers/{offerId}/accept` | self | |
| POST | `/finance/applications/{id}/transitions` | scoped | |
| GET | `/finance/emi-calculator` | anon | Pure function; Pricing Registry rate table |
| GET | `/insurance/quotes` | anon | `?registration_number=` or vehicle params |
| POST | `/insurance/quotes` | user | Multi-insurer fan-out; 202 + poll |
| GET | `/insurance/quotes/{id}` | self | Per-insurer premiums, IDV slider range, add-ons |
| POST | `/insurance/policies` | user | Purchase → payment → policy issuance |
| GET | `/insurance/policies/{id}` | self | |
| GET | `/insurance/policies/{id}/document` | self | |
| POST | `/insurance/claims` | self | |
| GET | `/insurance/renewal-reminders` | self | |

Insurance responses carry `irdai_disclosures[]` and the intermediary registration number, sourced
from the Integration Registry per insurer — regulatory text is registry data, and it must appear
verbatim in every quote surface.

### 9.15 `/chat`, `/notifications`, `/documents`, `/reviews`, `/trust`

| Method | Path | Perm | Notes |
|---|---|---|---|
| GET | `/chat/threads` | self | |
| POST | `/chat/threads` | user | Idempotent on `(participants, context_ref)` |
| GET | `/chat/threads/{id}/messages` | participant | Cursor paged, newest-first |
| POST | `/chat/threads/{id}/messages` | participant | Text / offer-card / media / template |
| POST | `/chat/threads/{id}/read` | participant | `{up_to_seq}` |
| POST | `/chat/threads/{id}/typing` | participant | Fire-and-forget, hub-preferred |
| POST | `/chat/threads/{id}/block` | participant | |
| GET | `/notifications/inbox` | self | |
| POST | `/notifications/inbox/{id}/read` | self | |
| PUT | `/notifications/preferences` | self | Per event-category × channel opt-in |
| POST | `/notifications/devices` | self | Register FCM/APNs token |
| GET | `/documents/documents/{id}` | party | Metadata |
| GET | `/documents/documents/{id}/download` | party | 302 → signed URL, 15 min, supports `Range` |
| POST | `/documents/documents` | user | Upload with `document_type` from the registry |
| POST | `/documents/esign/sessions` | party | Aadhaar eSign; returns redirect URL |
| GET | `/reviews/reviews` | anon | `?subject_type=dealer&subject_id=` |
| POST | `/reviews/reviews` | user | Only after a completed deal/booking |
| POST | `/reviews/reviews/{id}/respond` | subject owner | |
| POST | `/trust/reports` | user | Report listing/user/message |
| GET | `/trust/risk-signals/{subjectType}/{id}` | `trust.signal.read` | Admin |
| POST | `/trust/blacklist` | `trust.blacklist.write` | step-up |

Chat enforces a **contact-info masking** rule at the API: phone numbers and UPI IDs in message bodies
are redacted until the thread reaches a workflow state that permits exchange
(`deal.in_progress`), returning `422 chat.message.blocked_content` with the offending span. Both the
detection patterns and the permitting state are registry rows — anti-disintermediation policy
changes weekly and must never be a redeploy.

---

## 10. Registry-serving endpoints in detail

These five endpoints are hit by every client on every cold start. They must be cacheable to the point
of being nearly free.

### 10.1 Caching model

| Mechanism | Value |
|---|---|
| `ETag` | Strong: `"tax:412:cat_car:hi-IN"` — registry, version, scope, locale |
| `Cache-Control` | `public, max-age=300, stale-while-revalidate=86400, stale-if-error=604800` |
| `Vary` | `Accept-Language, X-AutoHub-City, Authorization` (last only on `/features`) |
| Version pinning | `?version=412` → `Cache-Control: public, max-age=31536000, immutable` |
| CDN | All anonymous registry GETs are CDN-cacheable; purge on publish by surrogate key `reg:taxonomy` |
| Compression | Brotli; bundles are also served as pre-compressed `.br` blobs from S3 for the CDN |

`stale-if-error=604800` is deliberate: if the registry service is down, **a week-old taxonomy is
infinitely better than a broken app**. The mobile clients replicate this: they persist the last
successful bundle to disk and use it forever until a newer version is fetched.

### 10.2 The mobile cold-start sequence

```
App start
   │
   ├─ GET /platform/manifest                    (~200 B, If-None-Match)
   │     304 → everything cached is current → render immediately. Done.
   │     200 → compare per-registry versions with local
   │
   ├─ for each drifted registry, in priority order:
   │     i18n      → GET /platform/i18n/hi-IN?since_version=204   (delta, ~2 KB)
   │     features  → GET /platform/features                        (~1 KB)
   │     taxonomy  → GET /platform/taxonomy/tree?depth=2           (~40 KB brotli)
   │     forms     → lazy: fetched when the screen that needs the key opens
   │     workflows → lazy: fetched with the first entity of that type
   │
   └─ render
```

Forms and workflows are **lazy and keyed**, never bundled — a customer app that never opens the
inspection checklist should never download `inspection.checklist.car.v5` (180 fields, ~60 KB).

### 10.3 `GET /platform/forms/{key}`

```
GET /api/v1/platform/forms/listing.sell.car.v7?category_id=0193…&locale=hi-IN
If-None-Match: "form:listing.sell.car:7:0193…:hi-IN"
```

```jsonc
{ "data": {
  "key": "listing.sell.car", "version": 7, "title_key": "form.sell.car.title",
  "schema": {                                  // JSON Schema 2020-12
    "type": "object",
    "required": ["registration_number","km_driven","owner_count","price"],
    "properties": {
      "registration_number": { "type":"string", "pattern":"^[A-Z]{2}[0-9]{1,2}[A-Z]{0,3}[0-9]{4}$",
                               "x-ah-format":"registration_number" },
      "km_driven": { "type":"integer", "minimum":0, "maximum":1000000, "x-ah-unit":"km" },
      "owner_count": { "type":"integer", "minimum":1, "maximum":15 },
      "price": { "type":"integer", "x-ah-money":"INR_minor", "minimum":1000000 },
      "fuel_type": { "type":"string", "x-ah-enum-source":"taxonomy:attr:fuel_type" }
    }
  },
  "ui_schema": {                                // rendering, not validation
    "order": ["section.vehicle","section.condition","section.price","section.media"],
    "sections": [
      { "key":"section.vehicle", "label_key":"form.sell.section.vehicle",
        "fields":["registration_number","km_driven","owner_count","fuel_type"],
        "columns": 2 }
    ],
    "widgets": { "km_driven":{"widget":"stepper","step":1000},
                 "price":{"widget":"money","show_valuation_hint":true},
                 "fuel_type":{"widget":"chip_group"} }
  },
  "rules": [                                    // conditional logic, evaluated client AND server
    { "when":"$.fuel_type == 'ev'", "then":{"show":["battery_health_pct","charging_type"],
                                             "require":["battery_health_pct"]} },
    { "when":"$.owner_count > 3", "then":{"warn":"form.sell.warn.many_owners"}},
    { "when":"$.km_driven > 200000", "then":{"require":["engine_condition_note"]}}
  ],
  "server_only_rules_present": true,            // client knows validation isn't complete locally
  "submit": { "method":"POST", "path":"/api/v1/listings/listings",
              "idempotency":"required" },
  "prefill_sources": [
    { "field":"registration_number", "source":"catalog.rc_lookup" },
    { "field":"fuel_type", "source":"catalog.rc_lookup.fuel_type" }
  ]
}}
```

`x-ah-enum-source` is what keeps this from being a static form: the renderer resolves
`taxonomy:attr:fuel_type` against the **already-cached** taxonomy bundle, so adding "Flex Fuel"
appears in every client on the next taxonomy version bump without touching the form.

`server_only_rules_present: true` is honest engineering: some rules (duplicate detection, price-band
checks, trust rules) cannot run on the client. The renderer disables optimistic "all clear" UI and
expects a possible `422`.

### 10.4 `GET /platform/workflows/{key}`

```jsonc
{ "data": {
  "key": "listing.sale", "version": 3, "entity_type": "listing",
  "initial_state": "draft",
  "states": [
    { "code":"draft","label_key":"wf.listing.draft","category":"draft",
      "color_token":"--ah-n-500","is_terminal":false,"sla_hours":null },
    { "code":"under_review","label_key":"wf.listing.under_review","category":"in_progress",
      "color_token":"--ah-info","is_terminal":false,"sla_hours":4 },
    { "code":"active","label_key":"wf.listing.active","category":"success",
      "color_token":"--ah-clear","is_terminal":false },
    { "code":"reserved","label_key":"wf.listing.reserved","category":"blocked",
      "color_token":"--ah-caution","is_terminal":false },
    { "code":"sold","label_key":"wf.listing.sold","category":"terminal",
      "color_token":"--ah-highway","is_terminal":true },
    { "code":"rejected","label_key":"wf.listing.rejected","category":"failure",
      "color_token":"--ah-flag","is_terminal":false },
    { "code":"expired","label_key":"wf.listing.expired","category":"failure",
      "color_token":"--ah-flag","is_terminal":false }
  ],
  "transitions": [
    { "action":"submit_for_review","from":["draft","rejected"],"to":"under_review",
      "permission":"listings.listing.update",
      "guards":[
        {"code":"min_photos","client_evaluable":true,
         "expr":"count($.media) >= $registry.feature('listing.min_photos')",
         "message_key":"wf.guard.min_photos"},
        {"code":"kyc_level","client_evaluable":true,
         "expr":"$user.kyc_level >= 1","message_key":"wf.guard.kyc"},
        {"code":"no_duplicate_active","client_evaluable":false}
      ],
      "form_schema_key":null,
      "effects":["notification:listing.submitted","queue:moderation"] },
    { "action":"approve","from":["under_review"],"to":"active",
      "permission":"listings.listing.moderate",
      "effects":["notification:listing.approved","search:index","outbox:listing.activated"] },
    { "action":"reject","from":["under_review"],"to":"rejected",
      "permission":"listings.listing.moderate",
      "form_schema_key":"listing.reject_reason.v2" }
  ],
  "auto_transitions":[
    { "action":"expire","from":["active"],"to":"expired",
      "trigger":"schedule","after":"$.expires_at" }
  ]
}}
```

`client_evaluable: true` guards let the client **grey out a button with the right reason before the
round-trip** — the difference between a responsive product and a form that fails after you tap it.
`client_evaluable: false` guards are simply not evaluated locally; the client shows the action
enabled and handles a `409 platform.workflow.guard_failed`.

`packages/workflow` consumes this document; `StatusPill` and `WorkflowTimeline`
(01-design-system §6) render entirely from it, including the color token. **No status string, no
status color, and no button label exists in any client codebase.**

### 10.5 `GET /platform/taxonomy/tree`

```
GET /api/v1/platform/taxonomy/tree?root=vehicle&depth=2&include=attributes&locale=hi-IN
```

Returns nodes with `id, code, parent_id, kind (category|make|model|variant), label, icon,
sort_order, is_active, child_count, attribute_ids[]`, plus a flat `attributes[]` dictionary so the
tree stays small and attributes aren't duplicated per node.

`depth` is required to be ≤ 3 on anonymous calls (the full make/model/variant graph is ~180k nodes).
Deeper levels are fetched on demand via `/taxonomy/models?make_id=` and `/taxonomy/variants?model_id=`,
each independently ETagged. Full-graph download is a dealer/admin-only route
(`GET /platform/taxonomy/export?format=ndjson`, permission `platform.registry.read`).

### 10.6 `GET /platform/i18n/{locale}`

```
GET /api/v1/platform/i18n/mr-IN?namespace=auction,common&since_version=203
```

- Delta responses when `since_version` is given: `{"added":{…},"changed":{…},"removed":[…],"from_version":203,"to_version":205}`.
- Missing key policy: server returns the `en-IN` value with `"__fallback":["key1","key2"]` listed in
  `meta`, so translation gaps are observable in production instead of rendering raw keys to users.
- `plural_rules` and `number_format` per locale ship in the bundle, including the **Indian digit
  grouping** (`8,40,000` not `840,000`) and the lakh/crore abbreviation thresholds that `MoneyValue`
  uses. Money formatting rules are localization data, not a hardcoded formatter.

### 10.7 `GET /platform/features`

```jsonc
{ "data": {
  "resolved_for": { "user_id":"0193…", "city_code":"PUN", "tier":"dealer_pro", "kyc_level":3 },
  "flags": {
    "module.lease":            { "enabled": false, "reason":"geo", "available_cities":["BLR"] },
    "module.auction":          { "enabled": true },
    "listing.min_photos":      { "enabled": true, "value": 6 },
    "auction.proxy_bidding":   { "enabled": true },
    "search.semantic_ranking": { "enabled": true, "variant":"b", "experiment":"exp-2026-07-rank" },
    "payments.upi_autopay":    { "enabled": false, "reason":"rollout", "rollout_pct": 20 }
  },
  "min_supported_client": { "flutter-customer":"3.0.0", "web":"2026.6.0" },
  "kill_switches": { "auction.bidding": false, "payments.payouts": false }
}}
```

`value` on a flag makes the Feature Registry a **config registry**, not just booleans — `min_photos`
lives here and is referenced by the workflow guard expression in §10.4. One source of truth, two
consumers.

`kill_switches` is polled on the 30-second heartbeat by clients on money screens. Flipping
`payments.payouts` to `true` makes every payout route return `403 platform.feature.disabled`
within one cache TTL — the incident-response lever that doesn't need a deploy.

---

## 11. Degradation contract

What clients can rely on when a dependency is down. All of this is Integration Registry (#8)
configuration — fallback chains, timeouts, breaker thresholds — not code.

| Dependency down | Behaviour | Client signal |
|---|---|---|
| OpenSearch | Postgres fallback: exact filters only, no fuzzy, no facets, relevance→recency | `meta.degraded:true`, `X-AutoHub-Degraded: search` |
| Redis | RBAC/registry read from Postgres (slow path). **Bids and payments fail closed** — `503 platform.integration.provider_unavailable` | `retryable:true`, `Retry-After` |
| VAHAN / PARIVAHAN | Serve cached RC ≤ 30 days with `source:"cache"`; else `503 catalog.vahan.upstream_unavailable` + manual-entry form key | `params.fallback_form_key` |
| Razorpay | Integration Registry fallback → Cashfree. Both down → `503 payments.gateway.all_providers_unavailable` | `retryable:true` |
| SMS provider | Fallback chain WhatsApp → SMS-B → voice. All down → `502 identity.otp.send_failed` | |
| Bureau (CIBIL) | Application accepted, parked in workflow state `awaiting_bureau`; retried by outbox | `202` |
| e-Invoice IRP | Deal completes, invoice queued; `invoice.irn_status:"pending"`. Never blocks settlement | `meta.warnings[]` |
| Insurer API | That insurer omitted from the quote fan-out with `partial_results:true` | `meta.warnings[]` |
| S3 / R2 | Uploads `503`; reads served from CDN cache | |

**Never degrade silently.** Every degraded response sets `meta.degraded` and a `warnings[]` entry,
because a dealer making a ₹40L decision on stale data without knowing it is worse than an error.

**Load shedding order** when the API is saturated (enforced at the gateway, priorities in the Feature
Registry): shed `analytics` → `search.suggest` → `search` → `read` → `write`. Never shed
`auction.bid`, `payments.*`, or `auth`. A dropped bid in the last ten seconds is an unrecoverable
business event; a dropped analytics ping is nothing.

---

## 12. Realtime

Transport: **ASP.NET Core SignalR** over WebSocket, with automatic fallback to Server-Sent Events and
then long-polling (Indian corporate networks and some ISPs still break WS). Backplane: Redis Streams,
which doubles as the replay buffer.

### 12.1 Hubs

| Hub | Path | Purpose |
|---|---|---|
| Auction | `/hubs/auction` | Bids, extensions, close, outbid alerts |
| Chat | `/hubs/chat` | Messages, typing, read receipts, presence |
| Notifications | `/hubs/notifications` | In-app inbox pushes, kill-switch broadcasts |

Hub URLs come from `GET /platform/config/public` — never hardcoded in a client.

### 12.2 Connect and authenticate

```
1. GET /api/v1/identity/auth/hub-tickets?hub=auction   → { ticket, expires_in: 60 }
2. wss://rt.autohub.in/hubs/auction?ticket=<ticket>&device_id=<did>
   (SignalR negotiate then WebSocket upgrade)
3. Server validates ticket (single-use, 60 s, bound to user+device+hub), attaches the
   connection to the user's principal, and starts a periodic re-auth timer.
```

A short-lived **ticket** rather than the access token in the query string: query strings land in
proxy logs, and browsers cannot set headers on a WebSocket handshake. The ticket is single-use and
useless after 60 seconds.

**Mid-connection expiry:** every 10 minutes the server sends `{"type":"auth.refresh_required"}`. The
client calls `Reauth(accessToken)` on the hub. No response in 60 s → server closes with code
`4401`. Session revocation (§4.6) publishes to `rt:revoke:{session_id}`; every node closes matching
connections with `4403` immediately. Revocation must reach a live bidder in under a second.

### 12.3 Subscribe

```jsonc
→ invoke: "Subscribe", { "channels": ["auction:0193c2f1-…", "auction:0193c2f2-…"],
                         "resume_from": { "auction:0193c2f1-…": 41 } }
← result: { "subscribed": ["auction:0193c2f1-…"],
            "rejected": [ { "channel":"auction:0193c2f2-…",
                            "code":"auction.registration.required" } ],
            "current_seq": { "auction:0193c2f1-…": 47 },
            "replay_count": { "auction:0193c2f1-…": 6 } }
```

Subscription is permission-checked per channel at subscribe time **and** re-checked on the 10-minute
re-auth — a dealer whose EMD is forfeited mid-auction stops receiving the stream. Max 50 channels
per connection.

### 12.4 Message envelope

Every server→client message, on every hub, has the same shape:

```jsonc
{
  "v": 1,
  "channel": "auction:0193c2f1-…",
  "seq": 42,                                  // monotonic per channel, gap-free
  "type": "auction.bid.placed",
  "ts": "2026-07-29T06:14:22.311Z",
  "server_ts_ms": 1785283462311,              // for clock-skew correction on countdowns
  "payload": {
    "bid_id":"0193c2f5-…","amount_minor":84500000,
    "bidder_masked":"Dealer #7 (Pune)","bidder_is_you":false,
    "current_bid_minor":84500000,"min_next_bid_minor":84800000,
    "bid_count":42,"ends_at":"2026-07-29T06:20:00Z",
    "extension_count":3,"reserve_met":true
  },
  "causation_id": "0193c2f1-8a44-…"           // the HTTP request_id that caused this
}
```

`causation_id` lets a client that just POSTed a bid recognise its **own** event arriving on the
socket and reconcile optimistic UI without double-counting — a real bug in every naive
optimistic-update implementation.

Event types:

| Hub | Types |
|---|---|
| auction | `auction.bid.placed`, `auction.bid.retracted`, `auction.outbid` (targeted), `auction.extended`, `auction.state_changed`, `auction.closed`, `auction.reserve_met`, `auction.proxy_triggered` |
| chat | `chat.message.created`, `chat.message.read`, `chat.typing`, `chat.thread.state_changed`, `chat.presence` |
| notifications | `notification.created`, `platform.registry.version_bumped`, `platform.kill_switch.changed`, `security.session_revoked` |

`platform.registry.version_bumped` is how a taxonomy publish reaches connected clients instantly
instead of on their next poll — the push counterpart to the §2.3 header.

### 12.5 Reconnect and missed-message replay

```
Client keeps last_seq per channel.

disconnect
   │
   ├─ reconnect with exponential backoff + full jitter: 0, 2, 5, 10, 20, 30s (cap 30s)
   ├─ new hub ticket (the old one is single-use)
   └─ Subscribe with resume_from: { channel: last_seq }
        │
        ├─ gap ≤ buffer (Redis Stream, 1000 msgs / 2 h per channel)
        │     → server replays msgs (last_seq, current_seq] in order, then
        │       sends { "type":"channel.resumed", "from":41, "to":47 }
        │
        └─ gap > buffer OR stream trimmed
              → { "type":"channel.resync_required", "current_seq":312 }
                Client MUST call GET /auction/auctions/{id}/state (and
                GET /auction/auctions/{id}/bids?since_seq=…) and rebuild.
                Never silently continue — a bidder with a missing bid is a lawsuit.
```

Sequence numbers are assigned by the same Redis Lua script that applies the bid (§9.8.1), so **the
socket sequence and the bid sequence are the same number**. There is no second ordering authority,
therefore no way for them to disagree.

**Polling fallback:** clients that cannot hold a socket poll `GET /auction/auctions/{id}/state` at
the interval in `/platform/config/public` (`auction.poll_interval_ms`, default 2000 when a socket is
impossible, 15000 as a socket-health backstop). That endpoint is ~120 B and served from Redis; it is
designed to survive being polled by every bidder in the last minute of an auction.

**Countdown correctness:** clients must compute remaining time as
`ends_at - (client_now + skew)` where `skew = server_ts_ms - client_receive_ms`, recalculated on
every message. Trusting the device clock produces bidders who think they have 8 seconds when they
have 0. Non-negotiable; it is implemented once in `packages/workflow` and in the Flutter shared
package.

---

## 13. Webhooks

### 13.1 Outbound to partners

Partners (lenders, insurers, fleet aggregators, dealer ERPs) register endpoints:

```
webhook_endpoints(id, partner_id, url, secret_current, secret_previous, secret_rotated_at,
                  event_types text[], is_active, description,
                  max_attempts, timeout_ms, created_at)

webhook_deliveries(id uuid7 pk, endpoint_id, event_id, event_type, payload jsonb,
                   attempt int, status,            -- pending|delivering|delivered|failed|dead
                   response_status int, response_body_snippet text,
                   next_attempt_at timestamptz, first_attempted_at, delivered_at,
                   signature text, idempotency_key text)
```

Events are produced by the **outbox**, so a webhook is never emitted for a transaction that rolled
back — the single most common webhook bug in payment-adjacent systems.

### 13.2 Signing

```http
POST https://partner.example.com/hooks/autohub
X-AutoHub-Event: finance.application.approved
X-AutoHub-Event-Id: 0193c2f8-1a2b-7c3d-9e4f-5a6b7c8d9e0f
X-AutoHub-Delivery-Id: 0193c2f9-…
X-AutoHub-Attempt: 1
X-AutoHub-Timestamp: 1785283462
X-AutoHub-Signature: t=1785283462,v1=5f8c…,v1=9a2d…
Content-Type: application/json
Idempotency-Key: 0193c2f8-1a2b-7c3d-9e4f-5a6b7c8d9e0f
```

`v1 = HMAC_SHA256(secret, "{timestamp}.{raw_body}")`, hex. **Two `v1` values appear during secret
rotation** (current + previous) so a partner can roll their secret with zero failed deliveries;
`secret_previous` is valid for 7 days after rotation.

Verification requirements we publish to partners:
1. Reject if `|now - t| > 300 s`.
2. Recompute over the **raw body bytes** — do not re-serialize the JSON.
3. Compare in constant time.
4. Dedupe on `X-AutoHub-Event-Id` (retries reuse it; it is the idempotency key).
5. Respond `2xx` within `timeout_ms` (default 5000). Body ignored. Any non-2xx or timeout = retry.

Public keys/secrets metadata and the current signature scheme are published at
`/.well-known/autohub-webhook-keys.json`.

### 13.3 Retry, backoff, and death

| Attempt | Delay after previous | Cumulative |
|---|---|---|
| 1 | immediate | 0 |
| 2 | 10 s | 10 s |
| 3 | 1 min | ~1 min |
| 4 | 5 min | ~6 min |
| 5 | 30 min | ~36 min |
| 6 | 2 h | ~2.6 h |
| 7 | 6 h | ~8.6 h |
| 8 | 12 h | ~21 h |
| 9 | 24 h | ~45 h |

All delays get **full jitter** (`random(0, delay)`) so a partner recovering from an outage isn't
hit by a thundering herd of every queued delivery at once. After attempt 9 → `dead`, an alert to the
partner's technical contact, and the delivery stays replayable for 30 days via
`POST /api/v1/platform/admin/webhooks/deliveries/{id}/replay`.

**Circuit breaking per endpoint:** 20 consecutive failures → endpoint marked `degraded`, new events
queue (up to 10,000, then oldest dropped with an alert) and a single probe delivery is attempted
every 5 minutes. Protects our worker pool from a partner who returns 500s for a week.

**Ordering is not guaranteed.** Every payload carries `sequence` (per partner, per subject) and
`occurred_at`; partners must discard out-of-order state updates using `sequence`. Promising ordered
delivery over a retrying HTTP channel is a lie, so we don't.

### 13.4 Inbound (payment/KYC providers)

`POST /webhooks/{provider}/{eventType}` — outside `/api/v1` because the shape is the provider's,
not ours. Verified by the provider's own signature scheme (Razorpay `X-Razorpay-Signature` over the
raw body with the webhook secret). Behaviour:

1. Verify signature → fail: `400 payments.gateway.signature_invalid`, log, do not retry.
2. Persist raw event to `inbound_webhook_events` (unique on `(provider, provider_event_id)`) —
   **dedupe first, process second**.
3. Return `200` immediately; process asynchronously from the stored row.
4. Never trust the webhook's amount. Always re-fetch from the provider's API before crediting money.
   Webhook says "captured ₹8.4L" → we ask Razorpay what it actually was.

Rule 4 is the one that prevents a forged-or-replayed webhook from moving real money.

---

## 14. OpenAPI and code generation

### 14.1 Producing the document

- ASP.NET Core 9's built-in `Microsoft.AspNetCore.OpenApi` document generation, one document per
  major version, served at `/openapi/v1.json` and written to
  `packages/api/openapi/v1.json` at build time via `Microsoft.Extensions.ApiDescription.Server`.
- Endpoints are minimal-API groups per module; every one declares
  `.Produces<T>()` / `.ProducesProblem()` for **each** documented error code. A document transformer
  attaches `x-ah-error-codes: ["listings.listing.not_found", …]` per operation, and
  `x-ah-permission`, `x-ah-scope`, `x-ah-idempotent`, `x-ah-rate-limit-bucket`,
  `x-ah-stability`, `x-ah-registry-driven` extensions.
- **`x-ah-error-codes` is generated from the handler's actual `Result` failure paths**, not from
  attributes a developer remembers to add. A Roslyn source generator walks each handler and emits the
  set; drift between the code and the contract becomes impossible rather than merely discouraged.
- Registry-driven enums are **not** emitted as OpenAPI enums (they change without a deploy). They are
  `type: string` with `x-ah-enum-source: "taxonomy:attr:fuel_type"` or
  `"workflow:listing.sale:states"`. Emitting them as static enums would bake registry data into
  generated client code — a direct Prime Directive violation, and it would break every client the
  moment someone adds a fuel type.

### 14.2 CI gates

| Gate | Tool | Fails on |
|---|---|---|
| Lint | Spectral, custom AutoHub ruleset | Missing `operationId`, missing `x-ah-permission`, non-kebab path, missing 429/401 responses, undocumented error code |
| Breaking change | `oasdiff breaking` vs the `main` document | Any §1.5 breaking change without a `x-ah-stability: experimental` marker or an approved `BREAKING-CHANGE:` commit trailer |
| Error-code coverage | Custom | A code in `ErrorCodes.cs` that no operation declares, or vice versa |
| Example validity | `openapi-examples-validator` | Examples that don't validate against their schema |
| Client drift | `git diff --exit-code packages/api/src` after regeneration | Committed client out of sync with the document |

### 14.3 Generating the TypeScript client

```
packages/api/openapi/v1.json
        │
        ├─ orval  →  src/generated/
        │              ├─ schemas.ts       ← TS types
        │              ├─ zod.ts           ← Zod schemas (request: strict, response: .passthrough())
        │              └─ hooks.ts         ← TanStack Query hooks, typed keys
        │
        └─ custom generator → src/generated/
                       ├─ error-codes.ts   ← union of all codes + per-operation code unions
                       └─ permissions.ts   ← union of all permission strings
```

```ts
// generated hook usage — apps/dealer
const { data, error } = useSearchListings({
  filter: { 'attr.fuel_type': 'in:petrol,cng', price_minor: 'between:4000000,8000000' },
  sort: '-published_at,id',
  page: { size: 24 },
});

// generated, exhaustive error narrowing
if (error?.code === 'auction.bid.below_increment') {
  //         ^? narrowed to that operation's declared code union
  showMinBid(error.errors[0].params.min_next_minor);   // typed as number
}
```

Rules that keep generation honest:

- **`packages/api/src/generated/**` is committed** and CI-verified. Reviewers see contract changes as
  a diff in the PR, which is the entire point.
- **Response schemas use `.passthrough()`**; request schemas use `.strict()`. Additive server changes
  must never break a deployed client (§1.5), but a client sending a typo'd field should fail loudly.
- **Runtime response validation is dev/staging only** (`NODE_ENV !== 'production'`) — full Zod parsing
  of a 100-item search response on a mid-range Android device costs real milliseconds. Prod trusts
  the contract and relies on the CI gates.
- A shared `fetcher` injects `Authorization`, `Idempotency-Key` (auto-generated per mutation call,
  stable across retries), `X-AutoHub-Device-Id`, `X-AutoHub-Client`, `Accept-Language`,
  `X-AutoHub-Scope`; parses the envelope; converts `problem+json` into a typed `ApiError`; retries
  `429`/`503`/network with full jitter honouring `Retry-After`; and compares
  `X-AutoHub-Registry-Version` against the registry cache, invalidating drifted TanStack Query keys.
  **Every client behaviour this document mandates is implemented once, in the generated package.**

### 14.4 Flutter client

`openapi-generator` (`dart-dio`) from the same document into `mobile/packages/autohub_api`, plus a
hand-written `AutoHubClient` wrapper implementing the identical fetcher rules. Freezed models,
`json_serializable`, Dio interceptors for auth/idempotency/retry. The generator runs in the same CI
job as orval, so web and mobile can never be generated from different document versions.

### 14.5 What is NOT generated from OpenAPI

Dynamic form payloads. A listing's `attributes` object is `additionalProperties: true` in OpenAPI —
its real schema comes from `GET /platform/forms/{key}` at runtime and is compiled to a validator by
`packages/forms` (Ajv on web, a JSON-Schema evaluator in Dart on mobile). Generating a static type
for a dynamic form is the exact hardcoding this platform exists to avoid. The generated `Listing`
type therefore has `attributes: Record<string, unknown>`, and the form layer narrows it per schema.

---

## 15. Conformance checklist

A PR touching the API surface must satisfy all of these; CI enforces the starred ones.

- [ ] ★ Path matches `/api/v1/{module}/{kebab-plural-resource}`.
- [ ] ★ Every write declares and enforces `Idempotency-Key`.
- [ ] ★ Every operation declares `x-ah-permission` and calls the scope guard on every return path.
- [ ] ★ Every list endpoint is cursor-paginated with a bounded `page[size]`.
- [ ] ★ Every failure path maps to a code in `ErrorCodes.cs` and is declared on the operation.
- [ ] No status string, percentage, fee, label, or option list appears in a request/response schema
      as a static enum. Registry-sourced values carry `x-ah-enum-source`.
- [ ] Stateful resources return `status{}` + `available_actions[]`.
- [ ] Money is minor units + currency code, with `pricing_rule_id` on every computed fee.
- [ ] Timestamps are RFC 3339 UTC with `Z`. No local times on the wire.
- [ ] Degraded paths set `meta.degraded` and a `warnings[]` entry.
- [ ] PII is masked by default; unmasking requires an explicit permission and writes an audit row.
- [ ] Contract test exists that produces each newly declared error code.

---

## 16. Appendix — canonical scalar formats

| Concept | Wire format | Example |
|---|---|---|
| ID | UUID v7, lowercase, hyphenated string | `"0193c2f1-8a44-7b31-9e02-1f7c2d3a4b55"` |
| Money | `{ "amount_minor": int64, "currency": "INR" }` — or a bare `*_minor` int64 field | `84000000` = ₹8,40,000 |
| Timestamp | RFC 3339, UTC, ms precision, `Z` | `"2026-07-29T06:14:22.311Z"` |
| Date | `YYYY-MM-DD` | `"2026-11-02"` |
| Phone | E.164 | `"+919820012345"` |
| Registration number | Uppercase, no spaces, no hyphens | `"MH12AB1234"`, `"22BH1234AA"` |
| VIN / chassis | Uppercase, 17 chars, ISO 3779 | `"MA3EYD32S00123456"` |
| GSTIN | 15 chars, uppercase | `"27AAECR1234F1Z5"` |
| PAN | 10 chars, uppercase | `"ABCDE1234F"` |
| Pincode | 6 digits, string (leading zeros) | `"110001"` |
| IFSC | 11 chars, uppercase | `"HDFC0000123"` |
| Locale | BCP 47 | `"hi-IN"`, `"mr-IN"`, `"ta-IN"`, `"en-IN"` |
| City / state | Geography Registry codes, not names | `"PUN"`, `"MH"` |
| Percent | Basis points, integer | `rate_bps: 150` = 1.50% |
| Duration | Integer seconds, field suffixed `_seconds` | `retry_after_seconds: 23` |
| Geo | Separate `lat`/`lng` decimals, WGS 84 | `18.5204`, `73.8567` |
| Sequence | int64, monotonic within its channel | `42` |
| Enum-like | Registry code string, never a numeric enum | `"pending_approval"` |

**Never** on the wire: floats for money, formatted currency strings, localized date strings, numeric
enum ordinals, unpadded pincodes, phone numbers without a country code, `null` where an empty array
is meant.
