# 08 — Gap Analysis

> Reviewing principal architect's read of `00`–`07`, `docs/modules/10`–`16`, `docs/adr/0001`–`0012`,
> `docs/contracts/registry-api.md`, **and the repository as it stands on disk today**.
>
> This document's only value is how uncomfortable it is. It contains no praise. Where a document is
> right, it is named only as the winner of a fight.
>
> **The one-line verdict:** the doc set is unusually strong per-document and unusually weak
> *between* documents. There are three different state vocabularies, four different auction bid
> architectures, three different charts of accounts, three different analyzer-ID registries and two
> different wire formats — and the code already on disk follows a fourth seam document that
> contradicts the API contract. Nothing here is unfixable. Almost all of it gets more expensive every
> week it is not fixed.

---

## 0. Method, and what "covered" means here

| I checked | Against |
|---|---|
| Every claim in a module spec | The anchor it says it derives from |
| Every table, column and constraint | `03-data-model-erd.md` **and** `backend/src/AutoHub.Persistence/Migrations/*` |
| Every wire shape | `04-api-contract.md` **and** `packages/api/src/**` as built |
| Every account code, rate and threshold | `02` §15, `03` §4.5, `06` §6, `14` §7.3 |
| Every workflow state / analyzer id / error code | Every other document that names one |

**Severity legend used throughout:**

| | Meaning |
|---|---|
| **S1** | Two documents make mutually exclusive claims about something already being built. Blocks work today. |
| **S2** | Contradiction that will surface as a rewrite in Phase 1–4 if not settled now. |
| **S3** | Inconsistency that costs review time and a migration, not a rewrite. |
| **U** | Unbuildable as written — the DDL, the API shape or the type does not compose. |

Counts: **32 contradictions** (11 × S1, 13 × S2, 8 × S3), of which **5 are U**.
**14 open decisions** (10 business, 14 engineering — three of them blocking today).

---

## 1. Contradictions

### 1.0 Index

| # | Subject | Who disagrees | Sev | Winner |
|---|---|---|---|---|
| C1 | Registry schema name | `02` vs everyone | S1 | `03` (`platform`) |
| C2 | State-category vocabulary | `02` vs `03` vs `04` vs modules | S1 | New ADR required |
| C3 | State capability flags | `02` `caps_json` vs `03` boolean columns | S1 | `03` + two missing columns |
| C4 | Workflow columns on the entity row | `02` five columns vs `03` two | S2 | `03` |
| C5 | Who owns `Party` | `02` (`trust`) vs `03`/`05`/`12`/`16` (`identity`) | S1 | `03` |
| C6 | Attribute source of truth | `02` (EAV table) vs `03` (jsonb) | S1 | Split decision — see below |
| C7 | Multi-asset deals / EXCHANGE | `02` `deal_assets` vs `03` `vehicle_id NOT NULL` | S2 | `02` on modelling; EXCHANGE has no owner |
| C8 | Listing ↔ intent cardinality | `02` (1:1, immutable) vs `03`/`10`/`12` (1:N) | S1 | `03` |
| C9 | Auction bid architecture | `ADR-0010`/`02` vs `03` vs `04` vs `11` | S1 | `11`+`03`; `ADR-0010` superseded |
| C10 | Auction vs Lot as the API resource | `04` vs `02`/`03`/`11` | S1, **U** | Lot |
| C11 | Attachment money flow | `06` (off-platform) vs `02`/`12` (through escrow) | S1 | `06` for v1 |
| C12 | TDS §194-O rate | `02` (1%) vs `06`/`14` (0.1%) | S2 | `06`/`14` |
| C13 | TDS §194H threshold | `06` (₹20,000) vs `14` (₹15,000) | S3 | `06` |
| C14 | Chart of accounts | `02` numeric vs `03`/`06` semantic vs `14` hybrid vs `10`/`12` ad-hoc | S1 | `14` §7.3 mechanism, incomplete map |
| C15 | Inclusive-GST rounding | `03` §4.5 vs `06` §4.3 | S3 | `06` |
| C16 | TCS on a sub-₹10 L sale | `04` §9.9 vs `06` §2.4 | S3 | `06` |
| C17 | Error-code grammar | `04` (3-segment) vs `02`/`03`/`contracts` (2-segment) | S2 | `04` |
| C18 | Roslyn analyzer IDs | `ADR-0003` vs `07` vs `05` | S1 | New allocation table |
| C19 | Registry admin API path | `04` vs `07` vs `16` | S2 | `04`, unified with `03` §3.12 |
| C20 | Wire casing and envelope | `04` vs `contracts/registry-api.md` vs shipped code | S1 | `04` |
| C21 | Generated vs hand-written API client | `00`/`ADR-0009`/`04` vs `packages/api` on disk | S1 | The anchors |
| C22 | Money on the wire | `02` §20.9 (`amountMinor`) vs `04` (`amount_minor`) | S3 | `04` |
| C23 | Inspector client | `02`/`03`/`13`/`ADR-0011` (Flutter) vs `07` (PWA) | S2 | CTO decision |
| C24 | Next.js version | `00`/`ADR-0012` (16) vs README + `package.json` (15) | S3 | Decide, then fix one side |
| C25 | Module list | `00` §4 (20) vs `07`/`04`/`10` (adds Search, Dealer) | S3 | `07`/`04` |
| C26 | Schema names for comms modules | `02` vs `03` vs `07` (`comms`) vs `15` §0.1 | S3 | `15` §0.1 |
| C27 | Where `Offer` lives | `02` (Chat) vs `10` (`listings.offers`) | S2 | `10` |
| C28 | Slot-collision error code | `02` vs `03` vs `04`/`15` | S3 | `04`/`15` |
| C29 | Encrypted columns vs domain types | `05` §5.4 vs `03` §4.1 | S2, **U** | `05` on intent, `03` must change types |
| C30 | Required Postgres extensions | `03` §2.1 vs `infra/postgres/init/01-extensions.sql` | S1, **U** | `03` |
| C31 | One DbContext or many | `03` §1 vs `AutoHubDbContextModelSnapshot.cs` | S1 | `03` |
| C32 | Registry table names and shape | `03` §3 vs the shipped `InitialSchema` migration | S1, **U** | Decide, then rewrite one |

---

### 1.1 The spine-level fights (settle these before another line of Platform code)

#### C2 — Three state-category vocabularies. This is the worst one.

`02-domain-model.md` §3.1:

> **The category rule (important).** `state_key` is business data and code may *never* branch on it.
> `state_category` is a closed technical vocabulary … `DRAFT · PENDING · ACTIVE · ON_HOLD ·
> TERMINAL_SUCCESS · TERMINAL_FAILURE · TERMINAL_CANCELLED`

`03-data-model-erd.md` §3.4:

> ```sql
> CREATE TABLE platform.workflow_state_kinds (   -- engine semantics, not business vocabulary
>     code platform.code_key PRIMARY KEY,   -- 'initial','active','waiting','terminal_success','terminal_failure'
> ```

`04-api-contract.md` §0:

> ```jsonc
> "category": "in_progress",   // draft|in_progress|blocked|success|failure|terminal
> ```

Three sets. Seven values, five values, six values, with almost no overlap. And `10` §4.1 and
`14` §7.9 both use a sixth value, `terminal_cancelled`, that exists in `02` and in neither `03` nor
`04`. `02` §20.2 declares its vocabulary a cross-module assumption other docs "must honour"; two of
them don't.

**Why it matters more than it looks.** `02` says `state_category` is *"the only thing application
code, DB constraints and indexes may depend on"*. `03` says the same job is done by boolean columns.
`04` ships a third vocabulary to every client, where `StatusPill` and `WorkflowTimeline` consume it.
That means the same listing state is `ACTIVE` in the domain, `active` in the DB, and `success` on the
wire — and a Flutter developer writing `if (category == 'blocked')` is writing against a vocabulary
no server document defines.

**Ruling.** `03`'s `workflow_state_kinds` **table** wins the mechanism (it is itself a registry, so a
sixth kind is a row, not an ADR). It must be seeded with `02`'s seven values, mapping `waiting` →
`pending` and adding `on_hold` and `terminal_cancelled`. `04`'s `category` becomes a *derived UI
tone* sourced from `workflow_states.ui.tone`, not a second vocabulary — because a client that needs
"is this blocked?" should read a capability flag, not a bucket. Write it as ADR-0013 and make
`registry-lint` fail on any state row whose `kind_code` is outside the seeded set.

#### C3 — `caps_json` vs boolean columns, and the two flags that vanished

`02` §3.1 puts capabilities in JSON:

> ```json
> { "allows_edit": false, "publicly_visible": true, "blocks_new_deal": true,
>   "permits_payout": false, "permits_bid": true, "counts_as_inventory": true, ... }
> ```

`03` §3.4 puts them in columns: `is_publicly_visible`, `is_editable`, `holds_resource`,
`holds_funds`, `counts_as_open`, `blocks_deletion`.

`03` is right — a column is indexable and, critically, usable inside a constraint predicate. The
booking exclusion constraint in `03` §4.7 keys off `holds_resource`, and that is the single best
pattern in the whole doc set.

**But `03` dropped `permits_payout` and `permits_bid`, and three domain invariants depend on them:**

- `02` D2: *"A Deal in a state with `caps_json.permits_payout = false` cannot originate a `Payout`"*
- `02` A2: *"Bidder must have an ACTIVE `BidderRegistration` with EMD in a `permits_bid` state"*
- `02` §4.4: *"a provisional vehicle … may not reach a Deal state whose `caps_json.permits_payout` is true"*

`holds_funds` is not the same predicate — it means "escrow may not release", which is the *inverse*
condition on a *different* entity. As written, D2 and A2 are unimplementable. Add
`permits_payout boolean` and `permits_bid boolean` to `platform.workflow_states` and delete
`caps_json` from `02`.

#### C5 — `trust.parties` or `identity.parties`

`02` §2.1: *"| `Trust` | `Party`, `KycCase`, `RiskProfile`, `Blacklist` | `trust` |"*, and §6 defines
`trust.parties`. §20.11 states it as a binding assumption: *"Trust owns `Party`; Identity owns
`User`."*

`03` §4.1 defines `identity.parties`. So do `05` §5.4 (the column-encryption register),
`10`, `12`, `13`, `14`, `16` — and the shipped `infra/postgres/init/01-extensions.sql`.

`02` is alone. It loses. But the consequence is not cosmetic: `02` §6's DPDP design ("`parties` is
the only home of direct identifiers … crypto-shred the per-party data key") has to be rewritten onto
`identity.parties`, and `05` §5.4 already assigns `identity.parties.display_name` an
`encryption_class` of `none` *with a justification* — which is a different design from `02`'s
crypto-shred. Reconcile in `05`, not in `02`.

#### C6 — Which store is the truth for a vehicle attribute

`02` §16.2:

> ```
> catalog.vehicle_attribute_values      ← the TRUTH: one row per (vehicle, attribute, source)
> catalog.vehicles.attributes (jsonb)   ← the PROJECTION
> ```
> **Consistency contract:** the normalised table is transactional truth.

`03` §3.2:

> **The EAV decision, stated plainly.** The canonical store for dynamic attributes is a single
> `attributes jsonb` column on the owning row … `entity_attribute_values` is a **derived
> projection** … **If the two ever disagree, the jsonb wins** and the projection is rebuilt. We never
> write the projection directly.

This is not a naming clash. It inverts the direction of truth, and `03`'s primary key
`(entity_type, entity_id, attribute_definition_id)` **cannot represent `02`'s per-source rows at
all**. That kills:

- `02` §16.3's provenance ladder (`RTO_VAHAN 100 · DIGILOCKER_RC 98 · INSPECTION 90 · … ML_INFERRED 20`)
- `10` §5.4's odometer resolution, which says verbatim *"EAV precedence (02 §16.3) decides the winner"*
- `13` D2.2's rule that *"a fact without provenance is not a fact this platform will price"*

**Ruling — a split, not a winner.** `02`'s multi-source model is load-bearing for trust and must
survive; `03`'s jsonb-as-read-path is right for latency. So:
`catalog.vehicle_attribute_values` (with `source_key`, `confidence`, `observed_at`, `superseded_at`)
is the truth; `catalog.vehicles.attributes` is a trigger-maintained *winner* projection written in
the same transaction; **`platform.entity_attribute_values` is deleted** — it is a third copy that
serves neither purpose. Amend `03` §3.2 accordingly. Note that
`platform.entity_attribute_values` **already exists in the shipped migration**, so this is a
delete-before-it-fills decision.

#### C8 — One listing, one intent, or one listing, many intents

`02` §7.2, Invariant L1: *"`intent_key` is immutable after first publish. Changing intent = new
listing, linked by `superseded_by_listing_id`."* And §7.3 defines six satellite tables
(`listings.terms_sale`, `terms_auction`, `terms_rental`, …).

`03` §4.3: *"One listing, many intents, is the whole architecture in one table. A fleet operator
lists a Bolero as `sale` (₹6.4L) **and** `rental` (₹2,200/day) **and** `attachment` (18% revenue
share) simultaneously. Each intent carries its own workflow instance."*

`10` and `12` are both built on `listings.listing_intents`. `12` §7.3 makes it a binding assumption.
`02` loses, and §7.2 L1, §7.3's six satellite tables, and §4.5's `registry.listing_intent_exclusions`
must all be struck and rewritten against `listing_intents.is_exclusive` + the `pause_siblings`
transition.

#### C9 — Four auction bid architectures. One of them can lose a bid that was acknowledged.

| Doc | Admission authority | Clock | When is Postgres written | Sequence assigned by |
|---|---|---|---|---|
| `02` §17.3, `ADR-0010` | **Postgres** `SELECT … FOR UPDATE` | App server; `TIME` **forbidden** in Lua | Before the response | `lot.bid_count + 1` in PG |
| `03` §10.1 | Redis Lua CAS (liveness) + PG CAS (record) | Not stated | Same request, dual CAS | Redis, mirrored to a PG sequence |
| `04` §9.8.1 | Redis Lua | Not stated | *"immediately after **via the outbox consumer**"* | Redis |
| `11` D.5 / D.12 | Redis Lua | **`redis.call('TIME')` is "THE clock. One clock. Always."** | Before the 201 | Redis `lot_seq` |

`ADR-0010`, status **Accepted**:

> We still forbid `TIME`/`RANDOMKEY` inside scripts and pass the server timestamp in as an argument,
> because the *decision* clock must be the application server's, consistently.

`11` D.9:

> | Bid admission | `redis.call('TIME')` on `redis-auction` | The only authority for open/closed/extend |
> App-node NTP drift, which is routinely 20–200 ms across autoscaled instances, cannot decide who bid
> before the bell.

These are exact opposites, and the ADR is the one with "Accepted" on it.

**Ruling.** `11` + `03` win. `11` is right about the clock (an autoscaled ECS fleet is exactly where
"the application server's clock, consistently" is a fiction), and right that the 201 must follow the
Postgres commit. `ADR-0010` must be **superseded** — its `FOR UPDATE`-first path cannot meet
`07` E4.1's 40 bids/s on one hot lot, and it says so obliquely in `11` D.12 ("~40 ms/bid instead of
~6 ms", i.e. 25/s per lot).

**`04` §9.8.1 must be struck outright.** *"The Postgres write … happens immediately after via the
outbox consumer"* means a bid is acknowledged `201` before it is durable anywhere but Redis. `11`
D.12 states the correct rule and names the failure: *"Redis never holds the only copy of a bid the
bidder has been told was accepted."* Also settle idempotency: `ADR-0010` puts it in a PG
`UNIQUE (lot_id, idempotency_key)`; `11` D.5 puts it in a Redis hash; `03` §6.3 makes
`uq_bids__idempotency` **unique per monthly partition**, and says so. Three answers, and the middle
one is not durable.

#### C10 — The bid endpoint has no way to name the lot **(U)**

`04` §9.8: `POST /auction/auctions/{id}/bids`, `POST /auction/auctions/{id}/reserve`,
`GET /auction/auctions/{id}/state` returning `{current_bid_minor, bid_count, ends_at, seq}`, and
`12.3`'s realtime channel `auction:0193c2f1-…`.

But `02` §8.1, `03` §4.6 and `11` D.5 all model bids, reserve, `closes_at` and `current_bid` on the
**lot**, and an auction has up to 500 open lots (`11` D.11). `11` D.4's own sequence diagram shows
`POST /auction/auctions/{a}/bids` calling a Lua script keyed `ah:lot:{lot}:state` — with no lot
identifier anywhere in the request.

As written, you cannot place a bid. Fix `04` §9.8 to `/auction/auctions/{auctionId}/lots/{lotId}/bids`
(or a flat `/auction/lots/{lotId}/bids`), move `reserve`, `state`, `increments` and `proxy-bids` to
the lot, and change the realtime channel to `lot:{lotId}`. `04` §12.3's `resume_from` keying follows.

#### C11 — Does attachment money go through our escrow?

`06` §6.8:

> Design decision that shapes the ledger: **attachment payouts do not flow through AutoHub.** The
> aggregator pays the fleet, the fleet pays the owner, and AutoHub invoices the owner for its
> management commission. This eliminates §194-O exposure on ride earnings, removes a large
> working-capital and payment-aggregator burden, and keeps AutoHub out of §9(5).

And `06` §13: *"**Attachment** must not route owner earnings through AutoHub's nodal account in v1;
doing so creates §194-O and §9(5) exposure that this document's economics do not model."*

`02` §9.3, Invariant AT2:

> Money flows aggregator → platform escrow → owner, minus platform commission. The ledger postings
> are structurally identical to a SALE payout — **that is the point.**

`12` §5 step 10: *"Operator funds the escrow (RBI PA escrow/nodal…). Payout to the owner at
T+`attach.payout_delay_days`"*, with *"TDS 194-O @0.1% on the gross facilitated amount, deducted BY
THE PLATFORM"*.

**Ruling: `06` wins for v1.** It is the only version that did the tax and cash arithmetic, and `14`
§7.6 independently agrees (*"Where we only introduce parties and never touch money,
`intent_types.tds_194o_applicable` is false"*). Consequences that must be written down, not
absorbed:

1. `02` §9.3's claim that attachment is *"the best proof of the spine"* collapses. `RECURRING_REVERSE`
   still exists, but as an invoice-and-collect shape, not an escrow shape. `02` §1.2's proof table
   row "Money direction: **Fleet → Owner**" is no longer something the platform ledger sees.
2. `12` §5.9–§5.10 must be rewritten to owner autopay (`06` §6.8's `autopay_collect`), which deletes
   the nodal float, the 194-O deduction, and the `attach.payout_delay_days` timer.
3. Revenue is unchanged (`06` models ₹38.4 L/month either way); the risk is not.

---

### 1.2 Money and tax

| # | The disagreement | Ruling |
|---|---|---|
| **C12** | `02` §15.4(a) J5: *"TDS 194-O withheld @1% of ₹6,50,000 … 6,500"*. `06` §4.5 and `14` §7.6: **0.1%** since 01-Oct-2024. | `06`/`14`. `02`'s journal is internally balanced and legally wrong by 10×, and it is the worked example everyone will copy. Fix the number **and** the trial balance beneath it. |
| **C13** | `06` §4.5: §194H threshold *"₹20,000/FY"*. `14` §7.6: *"₹15,000 per FY per payee"*. | `06`. ₹15,000 is the pre-FY-2025-26 figure. |
| **C14** | Four chart-of-accounts vocabularies. `02` §15.3 numeric (`1000 Escrow bank`, `2010 Party payable`). `03` §4.5 + `06` §6 semantic (`escrow_holding`, `nodal_bank`, `operating_bank`, `pg_receivable`, `seller_payable`). `14` §7.3 a `gl_code`↔`account_code` map. `10` §12.7 invents `2100 HOLDBACK_LIABILITY`; `12` §5 uses `2035 Holdback held`, `2060 TDS payable`, `2010 Party payable`. | `14` §7.3's mechanism is correct and should be adopted — but its map is **incomplete**: it has no `nodal_bank`, no `operating_bank`, no `seller_payable`, and renames `1010` to `pg_settlement_account` while `03` and `06` post to `pg_receivable`. Complete the map against every code actually posted in `06` §6 and `12` §5, then delete `02` §15.3 and `10`'s `2100`. |
| **C15** | `03` §4.5: ₹1,999 inclusive → *"taxable ₹1,694.07, GST ₹304.92"* (₹1,998.99). `06` §4.3 R4: ₹1,694.07 + ₹304.93 = ₹1,999.00. | `06`, which already issues the correction. Fold it into `03` so nobody copies the broken split. |
| **C16** | `04` §9.9 fires `tcs.206c_1f` on an ₹8,40,000 sale with `condition: "sale_value >= 1000000"`. `06` §2.4 shows it is a paise/rupee unit-mixing bug — the threshold is ₹10,00,000. | `06`. Fix the `04` example; it is the one clients will copy into a test fixture. |

Two further money facts nobody has reconciled:

- **GST TCS §52 @ 0.5%** is a first-class obligation in `06` §4.5 and appears in every `06` §6 worked
  journal. It appears **nowhere** in `02` §15, `03` §4.5 or `14` §7.6. `14`'s close checklist item 11
  lists only 194-O, 194H and 206C(1F). GSTR-8 by the 10th is a monthly filing obligation with no
  owner.
- `02` §15.4(a)'s C2C example posts `Dr 1000 Escrow / Cr 2000 Customer advance` on capture. `03` §4.5
  and `06` §6 post `Dr pg_receivable / Cr escrow_holding`. Both balance; they are different charts
  and different templates. Whichever survives, the 15 golden files in `06` §12 are the arbiter.

---

### 1.3 Contract and transport

#### C20 / C21 — The seam document, the API contract, and the code already written **(S1)**

`README.md` names `docs/contracts/registry-api.md` as *"the exact seam between frontend and
backend"*. That file's own header:

> Frontend and backend are built in parallel against **this file**. It is the seam.
> If an implementation disagrees with this document, the implementation is wrong.

Its envelope:

```jsonc
{ "items": [ … ], "nextCursor": "eyJpZCI6…", "totalEstimate": 1284 }
{ "code": "listing.not_found", "traceId": "00-4bf92f…-01", "metadata": { "listingId": "…" } }
```

`04-api-contract.md` §1.2 and §2.1:

> JSON bodies use `snake_case` for **all** keys, request and response. This is not a style preference.
> …
> `data` is **always** present on 2xx and is **always** the payload — never `data.items`, never a bare
> array at the root.

And the header on `X-Registry-Version: 41` (one integer) versus `04` §2.3's
`X-AutoHub-Registry-Version: tax=412;form=88;wf=17;…` (ten). And `Cache-Control: max-age=60,
stale-while-revalidate=600` versus `04` §10.1's `max-age=300, stale-while-revalidate=86400,
stale-if-error=604800`. And two-segment error codes versus `04` §3's *"the normative form is the
three-segment one"*.

**This is not theoretical. The code followed the seam document.** In `packages/api`, `apps/web`,
`apps/dealer` and `apps/admin`:

```
amountMinor    403 occurrences
stateCode      252
totalEstimate   84
nextCursor      82
```

Zero occurrences of `amount_minor`, `state_code`, `next_cursor`, `total_estimate`.

And `packages/api` has **no `openapi/` directory and no generator config**. It is
`src/{endpoints,schemas,hooks,mock}`, hand-written — against `00-north-star.md` §3 (*"API types:
Generated from OpenAPI. Hand-written types are hardcoding."*), `ADR-0009` in full, and `04` §14.3's
committed-and-drift-checked generation.

`ADR-0009` names the exact failure it was written to prevent:

> **The frozen union.** A frontend dev types `status: 'draft' | 'live' | 'sold'` … In July the
> Workflow Registry gains `LEGAL_HOLD` — **without a deploy**, which is the entire point of ADR-0004
> — and the dealer portal renders a blank status pill.

252 hand-written `stateCode` references is that failure, pre-built.

**Ruling.** `04` wins. `docs/contracts/registry-api.md` is deleted, not amended — a second seam
document is how this happened. Approximately 800 identifiers change. This is a two-day job now and a
two-week job after Phase 2.

#### C17 — Error-code grammar

`04` §3 declares `{module}.{resource}.{condition}` normative and explicitly demotes the north star's
form: *"North Star §5 writes `listing.not_found` illustratively; the normative form is the
three-segment one below."* But `02` §19 registers ~30 two-segment codes (`bid.below_increment`,
`concurrency.stale_write`, `payout.state_not_permitted`) and `03` §11 maps ~20 more
(`booking.slot_taken`, `common.stale_write`, `media.duplicate_photo`) — and `03` §11 says
*"renaming a constraint is an API-breaking change, and is treated as one"*, which now applies to
about fifty renames.

`15` §9.6 already shows the resolution pattern for one of them. Do it for all of them, in one PR,
before `04`'s CI gate (*"a code that no test can produce fails the build"*) is switched on.

Related count problem: `04` §3 claims *"130 codes across 20 modules"*. Modules `10` (20), `11` (~15),
`14` (19), `06` (12), `15` and `16` add roughly a hundred more. The real number is **~250**, each
needing a contract test. That is a Phase-0 line item nobody costed.

#### C19 — Three registry-admin API shapes

- `04` §9.1: `POST /platform/admin/registries/{registry}/drafts/{id}/publish`, with `{registry}` ∈ ten names
- `07` §5.6: `POST /api/v1/platform/registry/{code}/rollback {to_version}`
- `16` F9: *"The registry-editing API is `/api/v1/platform/registry/change-sets/*` and every registry write endpoint requires an open `change_set_id`."*

Underneath is a real modelling difference: `04` models a **draft per registry**; `03` §3.12 and `16`
model a **change set spanning registries** (`registry_codes platform.code_key[]`). `ADR-0003` §4
sides with change sets. `04` must adopt the change-set model, keep its paths, and drop the
per-registry draft.

---

### 1.4 Naming, vocabulary, and things that will merely cost migrations

| # | Split | Ruling |
|---|---|---|
| C1 | `registry.*` (`02` §3.1, §3.2, §4.5) vs `platform.*` (everything else, plus the shipped migration and init SQL) | `platform` |
| C4 | `02` §20.4 mandates five workflow columns on every stateful table (`workflow_definition_id, workflow_version, state_key, state_category, state_entered_at`); `03` §12.5 mandates `workflow_instance_id` + `state_code` | `03`. Add `state_kind_code` only if a DB constraint needs it |
| C18 | `ADR-0003` allocates AH0001–AH0006 (+ frontend AH0007 = hardcoded JSX label). `07` §5.4 allocates AH0001–AH0008 with **different meanings** for AH0002, AH0006, AH0007, AH0008. `05` §4.2/§10 allocates AH0007 (scope guard), AH0008 (sensitive field), AH0009–AH0012. | One allocation table, one owner. Suggested: `ADR-0003` keeps AH0001–AH0006; `05` keeps AH0010–AH0019; `07`'s conventions rules renumber to AH0020+. `AH0007` currently means three different things and is a required check in two documents. |
| C22 | `02` §20.9: money crosses the API as `{ amountMinor, currency }` | `04` §16: `amount_minor` |
| C25 | `00` §4 lists 20 modules with no `Search` and no `Dealer`. `07` §0 lists 26 delivery units including both. `04` §9.5/§9.12 route `/search/*` and `/dealer/*`. `10` §0 names `AutoHub.Modules.Search`. `backend/src/Modules` has the north star's 20. | Add `Search` and `Dealer` to `00` §4, or say where their code lives. Today `/dealer/requirements` is a facade over `listings.requirements` with no project to put it in. |
| C26 | Comms schemas: `02` (`bookings`, `notify`, `reviews`), `03` (`booking`, `notification`, `review`), `07` §0 (**`comms`** for both Chat and Notifications), `15` §0.1 (resolves `02` vs `03`), init SQL (`booking`, `notification`, `review`, `chat`) | `15` §0.1 + init SQL. `07` must drop `comms`. |
| C27 | `02` §2.1 gives `Offer` to Chat; `10` §12.4 says *"`listings.offers` is the only negotiation object"* | `10` |
| C28 | `rental.slot_taken` (`02` §19) / `booking.slot_taken` (`03` §11) / `bookings.slot.unavailable` (`04` §3.9, `15` §9.6) | `04`/`15` |
| C23 | Flutter partner app (`02` §10 IN2, `03` §3.3 *"— Flutter inspector app"*, `13` B3, `ADR-0011`) vs `07` §11's *"native inspector PWA with offline support"* | Decision E9. Note `ADR-0011`'s own "bridge that dropped frames" argument applies to a PWA doing 60–120 GPS-stamped photos with background upload on mid-range Android. |
| C24 | Next.js 16 (`00` §3, `ADR-0012` title) vs Next 15.1.4 in all three `apps/*/package.json` and *"Next.js 15 App Router"* in `README.md` | Trivial, but two anchors currently lie about the stack. |

Also unreconciled: `04` §9.4.1 writes intent codes as `sale|auction|rent|lease|attach|exchange`
while `02` §3.3, `03` §4.0 and `12` §1.1 use `rental` and `attachment`. Two of the six differ.

---

### 1.5 The documents versus the repository, today

This is the section that should be uncomfortable. Task #4 ("Build the Platform spine") is marked
in-progress; here is what is actually on disk against what `03` specifies.

#### C30 — Five required extensions are missing, and every exclusion constraint depends on one of them **(U)**

`03` §2.1 requires `pgcrypto, pg_trgm, btree_gin, btree_gist, citext, ltree, postgis,
pg_stat_statements, pg_partman`.

`infra/postgres/init/01-extensions.sql` creates `pg_trgm, btree_gin, citext, pgcrypto, unaccent`.

Missing: **`btree_gist`, `ltree`, `postgis`, `pg_partman`, `pg_stat_statements`.**

Without `btree_gist`, **none** of these can be created:

| Constraint | Doc | What breaks |
|---|---|---|
| `ex_bookings__resource_overlap` | `03` §4.7 | Double-booked inspectors and test drives. `07` E2.5 is untestable. |
| `ex_fee_slabs__overlap` | `03` §3.5 | Overlapping commission slabs ship silently |
| `rental.availability_blocks` GiST exclusion | `02` §9.1 | The rental double-booking answer, which `07` E5.3 makes an exit criterion |
| `attachment.commitment_blocks` | `12` §7.5 | Same |
| `ex_reg_no_temporal` on `catalog.vehicle_identifiers` | `02` §4.2 | The entire temporal-registration identity model, and clone-plate detection |

Without `ltree`, `platform.categories.path` and every `path_under` / `category_in` operator in `06`
§2.3 fail. Without `postgis`, every `geography(Point,4326)` column in `03` fails. `pg_partman` is
assumed by `03` §6.2 and `07` §5.2.

`03` §10.5 goes so far as to write a runbook for a missed partition — against an extension that is
not installed.

Second problem in the same file: `uuid_generate_v7()` is created in **`public`**, but `03` §2.2
names it `platform.uuid_generate_v7()` and `03` §1.4 mandates `search_path = ''`. With an empty
search path, every `DEFAULT uuid_generate_v7()` in the shipped migration fails to resolve.

#### C31 — There is one DbContext

`03` §1: *"There is no `AutoHubDbContext` that knows every table."*
On disk: `backend/src/AutoHub.Persistence/Migrations/AutoHubDbContextModelSnapshot.cs`.

The module-isolation story in `03` §1 (per-module DB roles, `GRANT`, `42501 permission denied →
common.module_boundary`, *"A developer who writes `SELECT * FROM deals.deals` inside the Listings
module gets `42501` in integration tests"*) has no mechanism behind it yet. `05` §5.1 goes further
and assumes Vault-issued per-module credentials.

#### C32 — The shipped registry schema is a different schema **(U)**

Twenty schemas are created by the init SQL; **one** (`platform`) has tables. Within it, the shipped
`InitialSchema` migration and `03` §3 disagree on names, on shape, and on what exists at all.

**Renamed:**

| `03` says | Migration says |
|---|---|
| `platform.categories` / `makes` / `models` / `variants` | `vehicle_categories` / `vehicle_makes` / `vehicle_models` / `vehicle_variants` |
| `platform.rule_sets` / `rules` | `pricing_rule_sets` / `pricing_rules` |
| `platform.idempotency_keys` | `idempotency_records` |
| effects as `jsonb` on `workflow_transitions` | a `workflow_effects` **table** |
| `attribute_definitions.category_id` | a `category_attributes` join table |

**Absent entirely** (verified by grep across all migrations):

`code_sets`, `codes`, `entity_types`, `registry_epochs`, `registry_change_sets`,
`registry_entity_versions`, `pii_registry`, `tax_rules`, `intent_types`, `party_roles`,
`document_types`, `account_kinds`, `plate_formats`, `scope_types`, `units`, `generations`,
`make_categories`, `calendar_days`, `workflow_timers`, `provider_credentials`, `provider_health`,
`user_permission_grants`, `locales`, `i18n_keys`, `audit_log`.

Consequences that are not negotiable:

- **No `registry_epochs`** ⇒ no epoch-keyed cache ⇒ `04` §2.3's `X-AutoHub-Registry-Version` header,
  `03` §3.0's `reg:*:{epoch}` keys, `07` E0.5's *"a registry write invalidates in ≤ 1 s across all
  API nodes"* and `06` §3.8's quote pinning all have nothing to read.
- **No `registry_change_sets` / `registry_entity_versions`** ⇒ no draft→review→approve→apply→rollback
  lifecycle ⇒ `07` §5.6 Tier-1 rollback ("< 2 min") does not exist, `ADR-0003` §4's governance does
  not exist, and `16` C5's registry editors have no substrate.
- **No `tax_rules`** ⇒ the pricing engine cannot compute a single GST split.
- **No `codes` / `code_sets`** ⇒ every `reason_code`, `grade_code`, `condition_code` in `03` §4 has a
  dangling composite FK.
- **No `workflow_timers`** ⇒ every SLA, expiry, anti-snipe extension and auto-transition in `10` §4.3,
  `11` D.13 and `14` §7.9 has no engine.
- **No `audit_log`** (only `audit_entries`) ⇒ `05` §9.2's hash chain and daily WORM anchoring have no
  table.

**And the form model is architecturally different.** `03` §3.3 and `ADR-0002` store a JSON Schema
document (`form_versions.schema jsonb` + `ui_schema` + `layout` + `form_bindings`). The migration
ships a relational `form_steps` / `form_fields` / `form_validation_rules` model. That changes
`packages/forms`, the form compiler in `10` §3.3, the `checksum`, and — most importantly — kills
`10` §3.8's *"One schema, three executions"* claim, because there is no schema to execute.

**Ruling.** This is a fork, not a drift. Either `03` §3 is amended to the relational model (and
`ADR-0002`, `10` §3, `packages/forms` and `13` B2 follow), or the migration is rewritten. Do not
proceed to Phase 1 with both. My recommendation is to rewrite the migration: the JSON-Schema
document is what makes the client/server parity story and the Flutter offline validator possible,
and it is what `ADR-0002` was argued for.

#### C29 — Encrypted columns cannot live in the declared domains **(U)**

`05` §5.4 assigns `identity.users.phone_e164` `encryption_class = 'column_aead'` with a
`phone_bidx` blind index, and specifies the ciphertext layout as `bytea`.

`03` §4.1 declares:

```sql
phone_e164    platform.phone_e164,     -- CHECK (VALUE ~ '^\+[1-9][0-9]{7,14}$')
email         platform.email_addr,     -- citext, CHECK (VALUE ~ '^[^@\s]+@[^@\s]+\.[^@\s]+$')
CONSTRAINT ck_users__has_handle CHECK (phone_e164 IS NOT NULL OR email IS NOT NULL)
```

AEAD ciphertext is not a `+91…` string and is not a citext email. `05` also requires a unique index
on the blind index for login, which `03` puts on the plaintext. Same conflict for
`identity.addresses.line1/line2`, `trust.kyc_records.pan_name`, `chat.messages.body`.

Fix in `03`: those columns become `bytea`, the format checks move to the application, the uniqueness
and the `has_handle` check move to the `*_bidx` columns, and `platform.pii_registry.blind_index_column`
becomes mandatory for `encryption_class='column_aead'` on any column used in a lookup.

---

## 2. PRD coverage — the explicit audit

### 2.1 The 26 modules

Every PRD module number is claimed by exactly one module spec. Coverage on paper is complete. Depth
is not.

| # | PRD module | Spec | Depth | What is actually missing |
|---|---|---|---|---|
| 1 | Buy | `10` | Full | — |
| 2 | Sell | `10` | Full | — |
| 3 | Dealer marketplace | `11` A/B | Full | Dealer *buying* on credit is spec'd (`11` B.5); the ECL/recovery ladder lives in `06` §8.4 with no module owner |
| 4 | Search & discovery | `10` §7 | Full | Saved-search alerting cadence; personalisation is Phase 7 |
| 5 | Rental | `12` §3 | Full | — |
| 6 | Lease | `12` §4 | Full | Lease-end buyout tax treatment named, not worked |
| 7 | Taxi attachment | `12` §5 | Full but **contradicted** | See C11 — the money flow is wrong for v1 |
| 8 | Requirement board | `10` §9 | Full | — |
| 9 | Dealer CRM | `11` C | Full | — |
| 10 | Valuation | `13` A | Full | — |
| 11 | Inspection | `13` B | **4-wheeler only** | No 2W, 3W, CV, bus or tractor checklist scheme; `13` B2 is `inspection_4w` + `auction_grade` |
| 12 | Finance | `14` A | Full | — |
| 13 | Insurance | `14` B | Full, licence-gated | Blocked on decision B1 |
| 14 | Payments & settlement | `14` C–E | Full | GST TCS §52 / GSTR-8 has no owner (see §1.2) |
| 15 | Chat | `15` §1 | Full | — |
| 16 | Bookings & test drive | `15` §2 | Full | — |
| 17 | Vehicle history | `13` C | Full | Depends on spike SP5 |
| 18 | Live auction | `11` D | Full but **contradicted** | See C9, C10 |
| 19 | Fraud & trust | `16` A | Full | — |
| 20 | Admin | `16` C | Full | — |
| 21 | Notifications | `15` §3 | Full | — |
| 22 | Documents & e-Sign | `15` §4 | Full | — |
| 23 | Reviews & ratings | `15` §5 | Full | — |
| 24 | Super admin | `16` D | Full | — |
| 25 | AI | `16` B | Full | — |
| 26 | Analytics | `16` E | Full | — |

**Named in the north star and owned by no document:**

| Capability | Where it is promised | Status |
|---|---|---|
| **EXCHANGE intent** | `00` §1's diagram; `02` §1.2's six-intent proof table; `04` §9.4.1's `intent` enum; `03` §4.0's `intent_types` comment | **No module spec. No tables (`03` has `deals.deals.vehicle_id NOT NULL`). No workflow. No fee components. No test.** It is on the front page of the architecture and it does not exist. Either build `deals.deal_assets` and spec it, or delete it from `00` and `02`. |
| **Logistics / vehicle delivery** | `15`'s own title; `00` §1's `LOGISTICS` node; `15` §0's "logistics leg of delivery/pickup" | No carrier entity, no transporter onboarding, no e-way-bill flow, no GTA reverse-charge handling beyond a one-line mention in `06` §4.4, and **no revenue stream** in `06`'s fifteen. A booking type exists; the business does not. |
| **RC-transfer assist as an operation** | `06` §6.5 charges ₹1,499; `04` §9.9 exposes `POST /deals/{id}/rc-transfer`; `03` seeds `org_type='rto_agent'` | Nobody spec'd who performs it, how RTO agents are onboarded, how they are assigned, or how they are paid. It is a priced product with no operating model. |
| **Ad serving (revenue stream 12)** | `06` §6.12 works the journals, assumes a *"signed viewability beacon"* and `discrepancy_tolerance_bps` | No campaign entity, no creative store, no impression pipeline, no advertiser onboarding, no self-serve. `16` E has no ad facts. |
| **Featured-slot auction** | `06` §6.11 recommends a monthly second-price sealed-bid auction for city slots | `11` D's auction engine is for vehicle lots. Nobody owns slot auctions. |
| **Premium membership entitlements** | `06` §6.15: benefits fire on `payer.entitlements` containing `membership.plus` | Nothing writes `entitlements`. No membership entity, no renewal, no benefit-consumption tracking beyond a journal line. |
| **Workshop / service booking** | `03` seeds `org_type='workshop'`; `13` C9 reads service history | No module, no revenue stream |
| **Warranty / RSA products** | `13` mentions warranty as a badge | No product, no revenue stream, no partner |
| Spare parts, driver marketplace, accessories | `00` §1 and `07` §11 defer them explicitly to v2 | Correctly deferred |

### 2.2 The 14 user types

| User type | Onboarding spec | RBAC | Payout path | Verdict |
|---|---|---|---|---|
| Individual buyer | `04` §4 OTP | ✔ | n/a | Covered |
| Individual seller (C2C) | `04` §4 + `10` §3 | ✔ | `14` §7.9 | Covered |
| Dealer principal | `11` A.1–A.3 | ✔ | ✔ | Covered |
| Dealer staff (branch-scoped) | `11` A.5 | ✔ | n/a | Covered |
| Fleet operator | `12` §3 host onboarding | ✔ | ✔ | Covered |
| Inspector | `13` B3 app, B12 audit; `16` F19 credentials | ✔ | `06` §6.13 `partner_payable` | Covered, thin on onboarding |
| Lender (RE) | `14` §3.2 | ✔ | receivable | Covered |
| Insurer | `14` §4.2 | ✔ | receivable | Covered |
| Admin / ops | `16` C | ✔ | n/a | Covered |
| Super admin | `16` D | ✔ | n/a | Covered |
| **Aggregator (Ola/Uber/local fleet)** | — | — | — | **GAP.** `12` treats it as a counterparty and `04` §3.9 defines `attachment.aggregator.not_onboarded`, but no onboarding workflow, no contract, no API integration spec beyond an Integration Registry capability name |
| **Driver** | partial (`12` §5.8 assignment) | role only | — | **Thin.** PSV badge and DL verification are named; no driver onboarding funnel, no driver-facing app scope, no driver payouts |
| **Broker / DSA / POSP agent** | `02` §6 invariants; `16` F19 credentials; `14` `*_agent_share` fee rows | partial | `06` §6.9 J3–J5 | **Thin.** Commission-share payout is journalled; the onboarding, licence check and self-serve dashboard are unspecified |
| **RTO agent / logistics partner / workshop / OEM advertiser** | `03` seeds the `org_type` values | — | — | **GAP.** Four party kinds exist in the taxonomy with no module, no workflow, no permissions |

Fourteen was the target. Ten are genuinely covered.

### 2.3 The 11 vehicle types

The doc set handles exactly **four** category branches. `10` §3.4's `$defs` has four:
`attrs_four_wheeler`, `attrs_two_wheeler`, `attrs_commercial`, `attrs_construction`.

| Vehicle type | Taxonomy | Sell form attrs | Inspection scheme | Valuation coverage |
|---|---|---|---|---|
| Car / 4-wheeler | ✔ | ✔ | ✔ `inspection_4w` | ✔ |
| Two-wheeler | ✔ | ✔ (4 fields) | ✖ | ✖ (`13` A is car-shaped throughout) |
| Commercial LCV/HCV/truck | ✔ (one bucket) | ✔ | ✖ | ✖ |
| Construction equipment | ✔ (the demo case) | ✔ | ✖ | ✖ (`13` A5 cold-start is variant-level, not category-level) |
| **Three-wheeler / auto / e-rickshaw** | ✖ | ✖ | ✖ | ✖ |
| **Bus** | folded into "commercial" | ✖ | ✖ | ✖ |
| **Tractor / farm equipment** | ✖ | ✖ | ✖ | ✖ |
| **EV as a class** | modelled as a *fuel*, not a category | partial (`battery_health_percent`) | ✖ | ✖ |
| **Vintage / classic** | `03` §3.2 seeds a `vintage` plate format only | ✖ | ✖ | ✖ |
| **Salvage / scrap (ELV)** | tag only (`06` §2.1 `has_tag "salvage"`) | ✖ | ✖ | ✖ |
| **Imported / CBU** | ✖ (`03` allows `make_text` fallback) | ✖ | ✖ | ✖ |

Two structural notes:

1. `03` §3.2's `platform.variants` promotes **car-shaped** columns to first class —
   `seating_capacity`, `mileage_kmpl`, `battery_kwh`, `range_km`, `gvw_kg` — with the judgment call
   *"no business person is going to delete 'fuel type'"*. That is true for cars. A tractor's PTO
   horsepower, a bus's seating layout and a crane's lifting capacity all have to go through EAV,
   which is fine — but it means the "add Construction Equipment with no deploy" demo is the *only*
   category that has been thought through end to end.
2. **The inspection module is the binding constraint on category expansion**, not the taxonomy. A new
   category needs a checklist form version, a scoring rule set, grade bands and repair rate cards.
   `13` B7's rubric is 4-wheeler-specific arithmetic. Adding two-wheelers is not a registry row; it
   is a content project with a domain expert.

### 2.4 The 15 revenue streams

`06` §6 works all fifteen with journals. This is the strongest coverage in the doc set. The gaps are
ownership, not definition.

| # | Stream | Priced | Journalled | Module owner | Issue |
|---|---|---|---|---|---|
| 1 | Listing fee | ✔ | ✔ | `10` | — |
| 2 | Premium listing / boost | ✔ | ✔ | `10` §6.4 | — |
| 3 | Dealer subscription | ✔ | ✔ | `11` A.6 / B.5 | Plan-limit enforcement is `402` in `04`; entitlement source unclear |
| 4 | Auction commission | ✔ | ✔ | `11` D.13 | — |
| 5 | Buy/sell commission | ✔ | ✔ | `10` §8 / `14` | — |
| 6 | Rental commission | ✔ | ✔ | `12` §3 | — |
| 7 | Lease commission | ✔ | ✔ | `12` §4 | — |
| 8 | Attachment commission | ✔ | ✔ | `12` §5 | **Contradicted (C11)** |
| 9 | Loan referral | ✔ | ✔ | `14` A | — |
| 10 | Insurance referral | ✔ | ✔ | `14` B | Blocked on B1 |
| 11 | Featured dealer | ✔ | ✔ | **none** | `06` §6.11's slot auction has no owner |
| 12 | Ad banners | ✔ | ✔ | **none** | No campaign, creative, impression or advertiser entity anywhere |
| 13 | Inspection | ✔ | ✔ | `13` B13 | — |
| 14 | Valuation report | ✔ | ✔ | `13` A13 | — |
| 15 | Premium membership | ✔ | ✔ | **none** | Nothing writes `payer.entitlements` |

Three of fifteen streams are fully priced and fully journalled with **no module that builds them**.
That is ₹18.9 L/month of the ₹4.81 Cr in `06` §7.1 — small in revenue, but it means `06`'s golden
files cannot all be produced.

---

## 3. Decisions still open, and who must make them

### 3.1 Blocking today

| # | Decision | Owner | Blocks | Cost of a further month's delay |
|---|---|---|---|---|
| **D1** | State-category vocabulary + `caps_json` vs columns (C2, C3) | Principal architect | Every workflow definition, every `StatusPill`, `registry-lint` | ~50 workflow definitions and ~250 state rows get authored twice |
| **D2** | Wire casing + envelope; retire `contracts/registry-api.md` (C20) | Principal + Experience lead | Every endpoint, every generated type | 800 identifiers today; every new screen adds more |
| **D3** | Is the shipped `platform` migration or `03` §3 canonical (C32) | Principal | All ten registries, `packages/forms`, the seed pipeline | The spine is being built against a schema no document describes |

### 3.2 Business decisions

| # | Decision | Owner | Latest useful date | What hangs on it |
|---|---|---|---|---|
| B1 | Insurance intermediary licence: **Direct Broker** (`14` §1, `05` §8.1) vs **corporate agent** (`06` §6.10) vs **web aggregator** (`07` Phase 6) | Founder + compliance counsel | Month 4 (licence lead time) | Whether `14` Part B exists, whether comparative quotes are legal, which fee rows may fire. Three documents currently state three different answers as fact. |
| B2 | Attachment money flow (C11) | CFO + tax counsel | Before Phase 5 | §194-O and §9(5) exposure; ~₹17 Cr of nodal float; `12` §5.9–5.10 rewrite |
| B3 | **Does AutoHub ever own a car?** `10` §4.6's Instant Offer names *"the platform-designated buying entity"* as the buyer | Board | Before Phase 1 exit | If yes: we become a seller under §206C(1F), the GST margin scheme applies, `06` §7.3's asset-light thesis is false, and `07`'s working-capital model is wrong by an order of magnitude. This is the largest undecided item in the doc set and it is buried in one paragraph of a module spec. |
| B4 | Transaction take rate: `06` §7.1 models 2.51%, `06` §7.2 recommends 3.0% split buyer/seller | CEO + CFO | Before Phase 2 pricing seed | Every `fee_components` seed row |
| B5 | Launch city and the single-city gate (`07` R3) | CEO | Phase 1 | Feature-flag scope, supply targets, `07` R3's 500-listing gate |
| B6 | Do we extend dealer credit (`11` B.5, `06` §8.4's ECL matrix)? | CFO + Board | Phase 3 | A credit line is a lending-adjacent activity with provisioning, a recovery ladder and a bad-debt line nobody has budgeted |
| B7 | Significant Data Fiduciary posture — `05` §6.7 *assumes* designation | Board + DPO | Month 6 | A named India-based DPO, an annual independent data audit, DPIAs gating every provider route. `05` builds for it; nobody approved the cost. |
| B8 | Ads: real ad server or ops-sold direct (stream 12) | CRO | Phase 6 | Whether an entire module exists |
| B9 | Featured slots: rate card or second-price auction (`06` §6.11) | CRO | Phase 6 | Same |
| B10 | Which three locales (`07` §11: *"English + Hindi + one regional"*) | CMO | Phase 1 | Localization seed volume; DLT template registration lead time (`05` §6.6 warns this is 48 h per template) |

### 3.3 Engineering decisions

| # | Decision | Owner | Notes |
|---|---|---|---|
| E1 | Registry schema name (C1) | Principal | Trivial; `platform` |
| E2 | Auction bid architecture; supersede `ADR-0010` (C9) | Principal + Trade lead | Settle clock, durability point and idempotency store together, or you settle none of them |
| E3 | Attribute source of truth (C6) | Principal | Recommend the split ruling above; delete `platform.entity_attribute_values` |
| E4 | Party ownership (C5) | Principal | `identity` |
| E5 | Listing↔intent cardinality (C8) | Principal | `listing_intents` |
| E6 | Does EXCHANGE exist in v1? `deals.deal_assets` yes/no | Product + Principal | If no, delete it from `00` §1 and `02` §1.2. A diagram promise nobody builds is worse than an absence |
| E7 | Analyzer ID allocation (C18) | DevEx lead | One table, one owner, before `TreatWarningsAsErrors` is switched on |
| E8 | Form model: JSON-Schema document or relational fields (C32) | Principal + Experience lead | Determines `packages/forms`, the Flutter validator, and `10` §3.8's parity claim |
| E9 | Flutter in year one, or an inspector PWA (C23) | CTO | `00` §8's acceptance test (*"The dealer app, customer app, and web portal render the same sell form"*) is unverifiable without it |
| E10 | Next.js 15 or 16 (C24) | Experience lead | Fix the anchors or the lockfile |
| E11 | Auction API granularity: lot resource (C10) | Trade lead | Blocking for `04` regeneration |
| E12 | Where `Offer` lives (C27) | Demand lead | `listings.offers` |
| E13 | OpenSearch field budget: single index vs per-category, and the `total_fields.limit` ceiling | Demand lead | See spike SP3 |
| E14 | `search_path = ''` + `platform.uuid_generate_v7()` vs app-generated ids | Platform | Currently broken in the init SQL |

---

## 4. Five places the metadata-driven approach is over-applied

`ADR-0003` §3 already draws a line and draws it well. These are five places where other documents
crossed it after the ADR was written, with the boundary I would enforce.

### 4.1 Workflow definitions over engineering and statutory state machines

**The over-application.** Fifty distinct workflow definitions are named across the doc set. Among
them: `crypto_key` (`05` §5.2), `accounting_period` (`14` §7.10), `payment_intent`, `payout`,
`refund`, `chargeback`, `recon_break` (`14` §2), `threat_item` and `breach_incident` (`05` §2.0,
§6.6), `ml_model_version` and `dual_control_request` (`16` F1), and the media pipeline's
`status_code` (`10` §5.1).

None of these is a business lifecycle a business person should change. `05` §5.2 admits it:

> **Key states are workflow-registry rows**, and `retired → revoked` has a guard rule set that
> refuses to fire while any row still references that `key_id`. Revoking a key that still has live
> ciphertext is data loss, **so the machine will not let a human do it.**

If the machine will not let a human do it, the machine is code and the registry row is decoration —
plus an attack surface, because `05` §2.5 identifies the registry editor as the highest-privilege
flow in the product. A registry row that can move an accounting period from `hard_closed` to `open`
is a financial-controls hole with a UI.

**The boundary.** Add `platform.workflow_definitions.is_business_editable boolean NOT NULL`. `false`
means: the states and transitions ship in a migration, the change-set publish validator rejects any
write, and the definition exists in the registry **only** so `StatusPill` and `WorkflowTimeline`
render it uniformly. Set it `false` for `crypto_key`, `accounting_period`, `payment_intent`,
`payout`, `refund`, `chargeback`, `recon_break`, `dual_control_request`, `ml_model_version`, and the
media pipeline. That is ten of fifty; the other forty are genuinely business-owned.

**Secondary cost nobody costed:** fifty definitions × ~15 states × ~20 transitions ≈ **1,750 registry
rows**, each needing an i18n key in three locales, a `ui.tone`, a permission code and a
`registry-lint` reachability proof. `07` §1.0's 100 engineer-week Phase 0 has zero line items for
authoring registry *content*. That is the most likely cause of `07`'s own R1 ("Phase 0 becomes a
20-week research project") — not the engine, the content.

### 4.2 Posting templates with an expression grammar over the ledger

**The over-application.** `06` §1.1 makes ledger legs data:

```sql
amount_selector  text NOT NULL,    -- 'line.taxable_value_paise' | 'line.cgst_paise'
                                   -- | 'sum(lines[kind=fee].amount_paise)'
```

That is a query language over deal lines, evaluated at posting time, editable by a finance analyst.
`ADR-0003` §3 lists *"Double-entry balance invariant"* as never-configurable and says *"A registry
row that could disable it is a fraud vector"*. A registry row that can change which lines a debit
sums is functionally the same lever with two more steps.

`14` §7.2 does the same thing correctly:

> `amount_expr` names a value from the priced fee context (`gross`, `taxable`, `cgst`, `sgst`,
> `igst`, `tcs`, `tds`, `net`).

A closed set of nine names. No grammar. No filters. No aggregation.

**The boundary.** `06`'s free-form `amount_selector text` is replaced by `14`'s closed
`amount_expr` enum (extend to `cess`, `discount`, `deposit`, `holdback` — a fixed vocabulary, added
by migration). Posting templates may configure **which accounts, in which direction, under which
condition** — never how an amount is computed. And no posting template reaches `production` without
passing the fifteen golden files in `06` §12 plus a generated property test asserting the template
balances for 10,000 random fee combinations. `06` §11's `/simulate` covers pricing; posting has no
equivalent gate and needs one more than pricing does.

### 4.3 RBAC `row_filter jsonb` compiled into SQL

**The over-application.** `03` §3.6:

```sql
row_filter   jsonb,        -- {"city_id":{"var":"scope.city_id"}} → appended to the query
field_mask   text[],       -- columns this role may NOT read: '{phone_e164,email}'
```

An admin-editable JSON document that becomes a SQL predicate on every query for that role, plus an
admin-editable list of column names. Three problems, all security-shaped:

1. It is a JSON→SQL compiler on the authorisation path. `05` §10.1 mandates that the *filter grammar*
   is parsed into a whitelisted AST with parameterised values — the same discipline is not applied to
   `row_filter`, which is strictly more dangerous because it is the thing enforcing tenancy.
2. `field_mask` names physical columns. A migration that renames `phone_e164` silently un-masks it.
   `05` §5.4 is simultaneously renaming that column to a `bytea` (C29).
3. An arbitrary predicate is an arbitrary query plan. A `row_filter` on an unindexed jsonb path turns
   the dealer inventory list into a sequential scan, and nobody will connect the incident to a
   registry edit made three weeks earlier.

**The boundary.** `row_filter` becomes a **closed set of named, parameterised scope predicates** —
`own_party`, `own_dealer`, `own_branch`, `city_in(:scope)`, `state_in(:scope)`, `assigned_case` —
each implemented in code, each with an index behind it, selected by code from the registry row. Adding
a seventh predicate is a code change and an ADR. `field_mask` references **`pii_registry` entries**,
not raw column names, so a rename is caught by the existing CI check.

### 4.4 The Form Schema registry over regulated artefacts

**The over-application.** `14` §2 puts `finance.application.v*`, `insurance.proposal_motor.v*`,
`payments.payout_account.v*` in the Form Schema registry alongside the sell flow. `05` §6.2 puts DPDP
consent notices there. `15` §4 puts document templates there.

A consent form is a legal artefact under DPDP §5–§6; a KYC form is a CDD process the PA and the REs
audit; a payout-account form is the front door to account-takeover fraud. `05` already has to bolt on
`ConsentMayNotBeBundled`, `consent_min_dwell_ms` dark-pattern detection, and a signed
`rendered_notice_sha256` — three compensating controls that exist because the underlying artefact is
freely editable.

**The boundary.** Add `platform.form_definitions.governance_class ∈ {open, reviewed, regulated}`.

- `open` — sell flow, listing forms, dealer forms. Today's behaviour.
- `reviewed` — anything whose fields land on an invoice or a contract. Change set + a second approver.
- `regulated` — `consent.*`, `kyc.*`, `payments.payout_account.*`, insurance proposal, finance KFS.
  Change set + a **compliance/DPO approver role**, *and* the field set is pinned by a code-side
  contract: adding or removing a required field fails the publish validator. Copy and layout stay
  editable; the data contract does not.

That is what `05` §2.5's four-eyes control was reaching for, expressed once in the schema instead of
three times in CI tests.

### 4.5 Registry rows that emit DDL, and registry-generated index predicates

**The over-application.** Two places where "no deploy" is a lie told to the operator.

`02` §16.4 / `03` §3.2 — `is_hot` attributes get a generated column and an index:

> This is the one place where a registry change produces DDL. It is gated: proposed by the registry,
> reviewed, applied by the migration runner, and reversible.

`10` §7.4 then lists it as step 6 of a flow whose other five steps are labelled "(nothing)", framed
as part of the no-deploy story.

`03` §5.3 does something worse:

> ```sql
> CREATE INDEX ix_auction_lots__closing ON auction.auction_lots (closes_at)
>     WHERE state_code IN ('open','extended');
> ```
> The resolution: the index predicate is generated from `SELECT code FROM platform.workflow_states
> … WHERE counts_as_open` by the migration generator, and a CI check fails the build if the index
> predicate and the registry disagree.

So adding a workflow state can fail the build of an unrelated module, and the fix is an index rebuild
on the hottest table in the auction path — during an auction.

**The boundary.**

1. `is_hot = true` is a **request**, never an apply. It opens a migration PR and the registry UI shows
   "requested — pending migration". `10` §7.4 must stop presenting it as step 6 of a no-deploy flow.
2. **Delete the generated index predicate entirely.** `auction.auction_lots` already has to carry a
   projected boolean (the same pattern `03` §4.7 uses for `holds_resource`); index
   `(is_open, closes_at)` and let the workflow engine maintain the boolean in the same transaction as
   the state change. Zero DDL when a state is added, zero CI coupling, and the pattern is already
   proven elsewhere in the same document.

---

## 5. Five technical assumptions that need a spike before Phase 1

Each is stated as a falsifiable question, with a deliverable and a named consequence if it fails.

### SP1 — Does the epoch-keyed registry cache survive a bulk write? (2 weeks, Platform)

**Assumption.** `03` §3.0: every registry write bumps that registry's epoch, every cache key embeds
the epoch, *"cache invalidation therefore needs no fan-out message"*. `07` E0.5: ≥99% hit ratio at
40k reads/s, invalidation in ≤1 s.

**Why it might be false.** An epoch bump invalidates **the entire registry**, not a row. `07` §7.5
already alerts on `RegistryCacheCollapse` — *"epoch thrash — someone is writing registry rows in a
loop. This is how the DB dies"*. The reference seed alone is ~19,000 pincodes, ~1,200 RTO offices and
a make/model/variant graph `04` §10.5 sizes at **~180k nodes**. If each insert bumps an epoch, the
seed run is 200,000 full-registry invalidations against a cold cache while an API is serving.

**The spike.** Load the full reference seed against a warm 4-node API with the `k6-registry-read`
profile running. Measure: epoch bumps, cache miss storms, p99 during the load, and the cost of a
single cold fill of taxonomy + pricing + workflow + i18n.

**Deliverable.** A stampede-protected loader (single-flight per key), a decision on per-registry vs
per-partition epochs, and a rule that seeds run inside one change set with **one** epoch bump.

**If it fails.** The epoch design becomes per-registry-per-scope, which changes `03` §3.0, the cache
key format in every module, and `04` §2.3's header. Cheaper to learn now.

### SP2 — Which auction architecture, and can it actually do 40 bids/s on one lot? (3 weeks, Trade + Platform)

**Assumption.** `07` E4.1/E4.2: 4,000 bids/min platform-wide, 40 bids/s on one hot lot, p99 ≤120 ms,
**zero incorrect winners over 1,000,000 replayed bids**.

**Why it might be false.** Four documents describe four architectures (C9), and one of them —
`ADR-0010`'s `SELECT … FOR UPDATE` — is measured in `11` D.12 at ~40 ms/bid, i.e. **25 bids/s per
lot**, below the target. `11`'s Lua path is measured at ~6 ms but has never been run.

**The spike.** Build `07`'s deterministic replay harness **first**, then implement both candidates
behind it. Inject ±2 s clock skew across nodes, duplicate `Idempotency-Key`s, a Redis failover mid-lot
and a partition in the final 5 s. Measure correctness and p99 for both.

**Deliverable.** A superseding ADR that settles, in one place: the clock (Redis `TIME` or app server),
the durability point (before or after the 201), the idempotency store, and where `lot_seq` comes from.

**If it fails.** `11` D.12's `degrade_to_pg` mode becomes the *only* mode, the scale target drops, and
`07`'s Phase 4 demo (killing the Redis primary in front of thirty dealers) is not a demo, it is the
architecture.

### SP3 — Does OpenSearch dynamic mapping survive 300 attribute codes? (2 weeks, Demand)

**Assumption.** `ADR-0006` and `10` §7.2: `attr_num_*` / `attr_kw_*` / `attr_bool_*` / `attr_dt_*`
dynamic templates mean a new attribute is a facet *"with no mapping change and no reindex"*, with
correct counts for filters the user has **not** applied.

**Why it might be false.** Dynamic templates create a real field per attribute code, per index.
`index.mapping.total_fields.limit` defaults to 1,000. `03` estimates 12–15 *hot* attributes but the
taxonomy is unbounded — `10` §3.4 already defines four category branches with ~25 codes, and
construction equipment alone adds three. Eleven vehicle types × 30 codes is 330 fields before
`facet_terms`, `embedding` and the 40 first-class properties. Sparse fields also degrade cardinality
aggregations, which is exactly what facet counts are.

**The spike.** Index 500,000 synthetic listings across 8 categories with 300 distinct attribute codes
in one `listings-public` index. Measure: mapping size, cluster state size, facet p95 at 250 QPS with
6 active facets, and the point at which the field limit is hit.

**Deliverable.** A decision on single index vs index-per-category-family, a documented field budget
in `attribute_definitions` publish validation, and a measured cost for the reindex that
`10` §7.1's blue/green alias flip assumes.

**If it fails.** The "no reindex" promise is qualified, `10` §7 needs an index-routing layer, and
`04` §7.4's `common.filter.unknown_field` gets a new sibling meaning "not indexed yet".

### SP4 — Will a licensed PA actually give us per-deal escrow with a 6.5-day average hold? (4 weeks, commercial + Money)

**Assumption.** `14` §1 and `05` §7.2: *"Customer funds sit in the PA's escrow. Our `escrow_holding`
accounts are a **mirror** of that balance."* `06` §8.1 models a 6.5-day average hold on C2C, 4.0 on
auction, 11.0 on rental deposits, with a 30-day hard ceiling and ~₹17 Cr average balance.

**Why it might be false.** The standard Indian PA product settles to a *merchant account* on Ts+1.
Split settlement and marketplace escrow with a **merchant-controlled, per-deal, multi-day hold** is a
different, negotiated product, and for vehicle-sized tickets it involves underwriting a new
marketplace has not earned. If the PA settles to us on T+1 and we hold the money for six more days,
we are holding customer funds in an account we operate — which is precisely what `05` §7.4's first
"must not" row and `ck_ledger_accounts__escrow_backing` exist to prevent.

**The spike.** Not code. Get, in writing, from two PAs: the exact product name, the maximum permitted
hold, whether the hold is per-order, the settlement account structure (nodal vs escrow vs tri-party),
and the fee. Then re-derive the chart of accounts against the answer.

**Deliverable.** A one-page structure diagram signed off by counsel, and a decision on whether
`06` §8.2's ₹17 Cr float exists at all.

**If it fails.** The licence posture in `05` §7 and `14` §1 collapses, the C2C escrow product cannot
ship, and `07` R5's *"documented fallback commercial model"* (direct dealer-to-buyer payment with a
success fee) becomes the primary model — which changes `06`'s take-rate model, `10` §8's deal flow
and the entire buyer-trust story. `07` R5 correctly says start in month 1. Nothing has started.

### SP5 — Is VAHAN access real, legal, complete and affordable? (3 weeks, Supply)

**Assumption.** `vahan.rc`, `vahan.challan`, `vahan.blacklist` as Integration Registry capabilities
with two providers and a fallback chain (`03` §3.9, `13` C2, `07` §8 at ₹3.50/call × 30k).

**Why it might be false, in four separate ways.**

1. **Access.** Official bulk VAHAN access is contractual and restricted. Most resellers are
   aggregators of varying provenance; a two-provider fallback may be two resellers of the same
   upstream, which is not a fallback.
2. **Completeness.** Field coverage differs by state. `13` C4's RC particulars, `10` §5.4's RC-OCR
   cross-check and `02` §4.4's identity ladder all assume a consistent field set.
3. **Legality.** An RC pull discloses the registered owner's personal data. `05` §8.5 flags this and
   requires purpose binding, per-party caps and velocity alerts. Whether repeat lookups on plates the
   user does not own are contractually permitted is unknown, and the homepage hero (`01` §5) is
   exactly that pattern, anonymously.
4. **Economics.** `07` R6 is right that this is the most expensive endpoint in the product and is
   exposed anonymously.

**The spike.** Sign one provider. Run 14 days of production-shaped traffic across five states.
Measure availability, p95 latency, per-state field coverage, and the real per-call cost including
failed lookups. Get the contract's position on repeat/bulk lookups in writing.

**Deliverable.** A go/no-go on the anonymous plate hero, a measured cache-hit model for the 90-day
`reg_no` cache in `07` R6, and a fallback design if availability is below ~95%.

**If it fails.** `02` §4's entire identity spine degrades to T3 (registration-number-only, confidence
60, *"Do not attach"*), the leakage detector's only ground-truth signal (`06` §9.2 signal 1, weight
50) disappears, `13` Part C is a shell, and the homepage hero becomes a manual-entry form. Four
documents assume this works.

---

## 6. The next 20 engineering tasks, in order

Ordering rule: nothing that produces durable artefacts (registry content, generated clients, module
code) starts until the contradiction it depends on is settled. Tasks 1–8 are cheap and almost all of
them are deletions or decisions. They are first because everything after them is expensive to redo.

| # | Task | Module / owner | Why now |
|---|---|---|---|
| 1 | **ADR-0013 — one state vocabulary.** Seed `platform.workflow_state_kinds` with the merged seven values; add `permits_payout` and `permits_bid` columns; delete `caps_json` from `02`; make `04`'s `category` a derived UI tone | Platform | C2 + C3. Every workflow row authored before this is authored twice |
| 2 | **Delete `docs/contracts/registry-api.md`;** amend `README.md`; declare `04` the sole seam | Platform + Experience | C20. A second seam document is how 800 camelCase identifiers happened |
| 3 | **ADR-0014 — analyzer ID allocation table**, one owner, `AH0001–AH0006` / `AH0010–AH0019` / `AH0020+` | DevEx | C18. `AH0007` currently means three things and is a required CI check in two documents |
| 4 | **Supersede ADR-0010.** One bid architecture: clock, durability point, idempotency store, sequence source. Strike `04` §9.8.1's outbox-write sentence | Trade + Platform | C9. Blocks SP2, which blocks Phase 4 |
| 5 | **Fix `04` §9.8 to a lot resource**, move `reserve`/`state`/`increments`/`proxy-bids`, rename the realtime channel to `lot:{id}` | Trade | C10 — the bid endpoint currently cannot name the lot |
| 6 | **Rewrite `infra/postgres/init/01-extensions.sql`:** add `btree_gist`, `ltree`, `postgis`, `pg_partman`, `pg_stat_statements`; move `uuid_generate_v7` to `platform`; create the `archive` schema | Platform | C30. Five exclusion constraints, the category tree and every geo column are currently uncreatable |
| 7 | **Decide C32** (shipped migration vs `03` §3), including the form model (E8). Write the decision into `03` in the same PR | Platform + Principal | The spine is being built against a schema no document describes |
| 8 | **Reconcile the error-code register**: rewrite `02` §19 and `03` §11 to `04`'s three-segment grammar (~50 codes); publish the merged ~250-code catalogue | Platform | Before `04`'s "a code no test can produce fails the build" gate is enabled |
| 9 | **Registry core:** `code_sets`, `codes`, `entity_types`, `registry_epochs` + the epoch trigger, `registry_change_sets`, `registry_entity_versions`, `pii_registry`. Redis cache with epoch keys and single-flight fill | Platform | Nine of `03`'s registry primitives do not exist. Nothing else in Phase 0 can be true without them. Carries **SP1** |
| 10 | **Workflow engine v1**: the four-write transaction (instance → history → outbox → entity row), `workflow_timers`, `from_state_id IS NULL`, version pinning, `is_business_editable` (§4.1) | Platform | Ten module specs assume it. `07` E0.1 tests it |
| 11 | **Rules engine:** the JsonLogic sandbox — `06` §2.3's operator allow-list, 64 AST nodes, depth 8, 5,000 steps, 50 ms, RE2. One evaluator, four callers | Platform | `05` §2.5 makes it an RCE surface. Build it once, sandboxed, not four times |
| 12 | **Split `AutoHubDbContext`** into per-module contexts with `HasDefaultSchema`, per-module DB roles, contract views, and the `42501 → common.module_boundary` integration test | Platform | C31. Module isolation is currently a code-review convention |
| 13 | **Idempotency middleware** — the row and the business write in **one** transaction (`04` §6.3), lease stealing, replay semantics, `platform.idempotency_keys` | Platform | Every write endpoint in every module doc requires it |
| 14 | **Outbox**: daily partitions, `FOR UPDATE SKIP LOCKED` claim, single-writer advisory lock, retry ladder, dead-letter, replay-without-reinsert | Platform | `ADR-0005`; `07` E0.6 and chaos C1 |
| 15 | **Pricing engine + `posting_templates`** with the closed `amount_expr` enum (§4.2), `pricing.quotes`, `quote_hash`, `pricing_trace`, and the 15 golden files from `06` §6 as the acceptance suite | Money | `07` E0.3. Golden files first — they are the only defence against a silent repricing |
| 16 | **Double-entry ledger**: `ledger_transactions` / `ledger_entries`, the deferred balance trigger, `journal_templates`, the completed chart of accounts (C14), and the three daily proofs from `14` §7.8 | Money | `07` S1 has no error budget. Build it before anything can post to it |
| 17 | **OpenAPI production + client generation**: document transformer with `x-ah-*`, orval + dart-dio, committed output, `api-client-drift` CI gate. **Delete hand-written `packages/api/src/{endpoints,schemas}`** | Experience + Platform | C21. `ADR-0009`'s "frozen union" failure is already built, 252 times |
| 18 | **Migrate the three Next apps to the generated client**: snake_case wire, `{data, meta}` envelope, `status{} + available_actions[]` rendering. ~800 identifiers. Settle Next 15/16 in the same PR | Experience | C20/C24. Two days now; two weeks after Phase 2 |
| 19 | **Seed pipeline**: baseline + reference change sets (36 states, ~1,200 RTO offices, ~19,000 pincodes, 4 category branches, tax rules, plate formats, permissions, the ~50 workflow definitions), `*_ref` resolution, one epoch bump per change set, idempotent re-run | Platform | The "no deploy" promise has nothing to demonstrate on. Also the load case for **SP1** |
| 20 | **`registry-lint` + the `phase0-gate` job**: bidirectional referential completeness, unreachable-state detection, i18n key existence, and `07` E0.1–E0.10 as one CI job | DevEx + SDET | `07` §1.0: *"If it does not land, Phase 1 does not start."* The gate must exist before the demo is booked |

**Spikes SP4 (PA product) and SP5 (VAHAN) run in parallel from day one** — both are commercial
lead-time items, neither is on this list because neither is code, and both can invalidate a phase.
`07` R5 already says start PA onboarding in month 1; that has not happened.

**Not on this list, deliberately:** any module domain code. Tasks 1–20 are Phase 0. The single
biggest risk to this programme, per `07`'s own R1, is Phase 0 sprawling — and the second biggest,
which `07` does not name, is Phase 0 being declared done while nine registry tables, the change-set
lifecycle, the epoch cache and the workflow timers do not exist.

---

## 7. What this document does not claim

- It does not say the architecture is wrong. The one-vehicle/many-intents/one-deal spine holds, and
  `12` §1.2's column-level proof is the best single page in the doc set.
- It does not say the registries are over-built. `ADR-0003` §3's boundary is right; §4 of this
  document is about five specific places other documents crossed it, not about the idea.
- It does not price the fixes. Tasks 1–8 are days. Tasks 9–20 are `07`'s Phase 0, which is costed at
  100 engineer-weeks and, on the evidence of what nine missing registry tables and 1,750 unauthored
  registry rows imply, is probably 140.
- It does not rank documents. But for the record: `06` and `14` are the only two documents that
  actively went looking for other documents' errors and corrected them in writing. Every document
  should have a "corrections other docs must honour" section, and only two do.

**The single most important line in this analysis:** three documents describe the workflow state
vocabulary, four describe the auction bid path, and the frontend on disk follows a fifth document
that the API contract contradicts. None of that is visible from inside any one document. It is only
visible to someone whose job is to read all of them, and it will only get more expensive from here.
