# 09 — Decisions Log

Settles the contradictions in [`08-gap-analysis.md`](08-gap-analysis.md) that touch **code already
written and verified**. The gap analysis found 32; this document decides the ones where a running
system is one of the two parties, because those cannot be left open — every day they stay open,
someone builds against the wrong side.

The remaining contradictions are between documents only. They are listed in §3 and belong to the
team that owns each module.

> **The tie-breaker used throughout:** when a document and working, verified code disagree on
> something with no user-visible benefit either way, the code wins and the document is corrected.
> When they disagree on something that would cost a user or a rupee, the correct answer wins and the
> code is changed. Consistency is not worth a migration on its own.

---

## D1 — Wire format is camelCase (settles C20, C22)

**Disagreement.** `04-api-contract.md` specifies snake_case on the wire (`amount_minor`).
`contracts/registry-api.md` and every shipped line of `packages/api`, `apps/web`, `apps/dealer` and
`apps/admin` use camelCase (`amountMinor`).

**Decision: camelCase. `04` is corrected, not the code.**

Why, in order of weight:

1. `contracts/registry-api.md` is the seam three portals were actually built against, and all three
   build, run and render. Changing it is a rewrite of every schema in `packages/api` plus every call
   site, for zero user-visible benefit.
2. JSON in a TypeScript codebase is camelCase by convention. snake_case on the wire means either a
   mapping layer at every boundary or `data.amount_minor` scattered through React components.
3. ASP.NET Core's default `JsonSerializerOptions` is camelCase. Choosing snake_case means
   configuring away the default in the API *and* in every partner integration test.

**What this obliges us to do:** the backend must not rely on `JsonNamingPolicy.SnakeCaseLower`, and
`04-api-contract.md` §0 and §9 need their examples rewritten. Until that edit lands, **`contracts/registry-api.md`
is authoritative for anyone writing code.**

Database columns remain `snake_case`. That is a different layer with a different convention, and
nobody has ever been confused by it.

---

## D2 — The hand-written typed client stands; generation is Phase 2 (settles C21)

**Disagreement.** `00-north-star.md` and `ADR-0009` say API clients are generated from OpenAPI.
`packages/api` is hand-written Zod schemas.

**Decision: keep the hand-written client for now. The ADR is not wrong, it is early.**

Generation requires an OpenAPI document, which requires the API host, which does not exist yet. The
portals could not wait for it, and the alternative — building three portals against nothing — is
worse than the drift the ADR warns about.

The ADR's actual concern is *hand-maintained types drifting from the server*. That risk is real and
starts the day the API ships. So:

- **Phase 2 exit criterion:** `packages/api/src/schemas` is generated from the API's OpenAPI document
  and the hand-written versions are deleted. This is not optional and it is not "if there is time".
- Until then, `contracts/registry-api.md` is the shared source both sides implement, and a change to
  it is a change both sides make in the same pull request.

The Zod schemas are not wasted work: generation will emit Zod, and the runtime validation they give
us at the network boundary is something a plain `openapi-typescript` client would not.

---

## D3 — Next.js 15, not 16 (settles C24)

`00-north-star.md` claimed Next.js 16. The repository runs **15.5.22**. The document was aspirational
and is now corrected.

Nothing in the architecture depends on 16. When the upgrade happens it will be a routine version bump
with its own codemod run, not an architectural event.

---

## D4 — Add `btree_gist` now; defer `postgis`, `ltree`, `pg_partman` (settles C30)

`03-data-model-erd.md` asks for nine extensions. `infra/postgres/init/01-extensions.sql` installs
five. Taking them one at a time rather than as a block:

| Extension | Decision | Reasoning |
|---|---|---|
| `pg_trgm`, `btree_gin`, `citext`, `pgcrypto` | **Installed** | Already in use — fuzzy model search, composite JSONB indexes, case-insensitive email, UUID v7 generation |
| `unaccent` | **Installed** (not in `03`) | Indian city and model names arrive with and without diacritics. `03` should add it |
| **`btree_gist`** | **Add now** | Booking double-booking prevention is specified as an `EXCLUDE` constraint over a time range plus a resource id. Without `btree_gist` that constraint cannot be created, and the alternative is application-level locking that races. This one is load-bearing |
| `postgis` | **Defer** | Service areas are radius-around-a-city in v1, which is a `point`/`earthdistance` problem. PostGIS is a large extension with its own upgrade cadence; adopt it when polygons are genuinely needed |
| `ltree` | **Defer** | The category tree is small (tens of nodes, four levels) and traversed by `parent_id` recursion. `ltree` earns its place at thousands of nodes, not at forty |
| `pg_stat_statements` | **Defer to infra** | A server-level `shared_preload_libraries` setting, not an application migration. Belongs in the managed-Postgres parameter group, not in `01-extensions.sql` |
| `pg_partman` | **Defer to Phase 4** | Partitioning matters for `bids`, `ledger_entries` and `audit_log` at volume we do not have. Until then it is a maintenance job with nothing to maintain |

---

## D5 — EXCHANGE is reserved, not built (settles C7)

The gap analysis is right, and the fault is in `00-north-star.md`, which I wrote.

EXCHANGE appears in the north star's own diagram as one of six proven intents. It is the only one of
the six that needs a **deal carrying two vehicles** — the customer's trade-in and the vehicle they
are buying — and `deals` has a single non-null `vehicle_id`. So the front page claimed a capability
that no table, workflow, fee component or test supports.

**Decision: keep the intent reserved, stop claiming it works.**

- `00-north-star.md` now marks EXCHANGE with a footnote saying exactly this.
- The `intent` enum keeps the value: reserving it costs nothing and removing it would churn the
  contract, the search facets and three portals.
- Building it means `deals.deal_assets` (a deal has many assets, each with a role and a valuation)
  plus a workflow where the trade-in valuation gates the transition. That is **Phase 5**, alongside
  Rental and Lease, because it shares their "one Deal, several things" shape.

The wider lesson the gap analysis is pointing at: a diagram on the front page of an architecture doc
is a claim, and claims need owners. Every other node in that diagram has a module spec; this one did
not, and nobody noticed for 32,000 lines.

---

## D6 — Workflow state categories: six, matching the backend enum (settles C2 for shipped code)

`packages/api` briefly carried `blocked` and omitted `cancelled`; `packages/ui` and
`AutoHub.Platform` carried the other six. Already reconciled in code to the backend's enum:

```
draft · awaiting_action · in_progress · succeeded · failed · cancelled
```

`AutoHub.Platform/Workflow/WorkflowDefinition.cs` is the source of truth, because it is the type the
engine actually switches on. The design system maps category → `StatusPill` tone, so **the server
never sends a colour** and a new state gets the right treatment without anyone storing a hex value.

`02-domain-model.md`, `03-data-model-erd.md` and `04-api-contract.md` should be corrected to these
six. The ADR the gap analysis asks for is not needed — this is a lookup table, not a decision.

---

## D7 — Guard conditions are never served on the public workflow endpoint

Not in the gap analysis, but decided while building the admin console and worth recording.

`GET /platform/workflows/{key}` returns states and transitions **without guards**. The condition that
decides whether a dealer may accept a deal is business logic; shipping it to a customer's phone hands
it to anyone with devtools, and hands a fraudster the exact shape of the check they need to defeat.

Guards are readable and editable only through the admin registry endpoints, behind
`registry.workflow.publish`. `apps/admin`'s workflow designer says so on the page, so nobody wastes
an afternoon wondering why the column is empty.

---

## 2. Corrections already applied to the repository

| Change | File | Why |
|---|---|---|
| Next.js 16 → 15 | `docs/00-north-star.md` | Matches what is installed |
| EXCHANGE footnoted as reserved | `docs/00-north-star.md` | It was claimed and does not exist |
| `blocked` → `cancelled` in state categories | `packages/api/src/schemas/workflow.ts` | Backend enum is authoritative |
| `btree_gist` added | `infra/postgres/init/01-extensions.sql` | Booking `EXCLUDE` constraints need it |
| `PlateClass` extended to five classes | `packages/ui/src/canon/plate-block.tsx` | The contract carries `government` and `defence`; the component had three |
| EV plate: green stripe → green plate | `packages/ui/src/canon/plate-block.tsx` | Real Indian EV HSRP plates are green with white lettering. "The plate colour is real information" has to survive contact with reality |
| `THEME_SCRIPT` exported from `@autohub/ui` | `packages/ui/src/index.ts` | Was duplicated as a string literal in `apps/admin`; web and dealer had no boot script and flashed white |

---

## 3. Still open — not mine to settle

These need the module owner or the CTO, and the gap analysis states each case fairly. Listed here so
they are tracked rather than rediscovered:

| # | Subject | Needs |
|---|---|---|
| C9, C10 | Auction bid architecture and whether the API resource is Auction or Lot | Engineering decision, gated on spike **SP2** |
| C11 | Whether attachment money flows through escrow or off-platform | Business + legal; `06` says off-platform for v1 |
| C14 | Chart of accounts — four documents, four schemes | Finance owns this; `14` §7.3's mechanism is right, the map is incomplete |
| C23 | Inspector client: Flutter or PWA | CTO. Offline-first in a basement with no signal is the deciding constraint |
| C29 | Encrypted columns vs domain types | Security + persistence, together |
| C31, C32 | One DbContext or many; registry table names vs the shipped migration | Resolve when the persistence layer lands — it is being written now |
| §2.1 gaps | Logistics, ad serving, RC-transfer operations, premium membership, featured-slot auction | Each is a priced product in `06` with no operating model. Product owns them |

---

## 4. What this log does not do

It does not resolve the contradictions between documents that no code depends on yet. That would be
guessing on behalf of teams that do not exist, and the gap analysis already states both sides of each
one better than a decision made in a hurry would.

It also does not treat the gap analysis as an attack. It found a hole in the front page of the
architecture, which is exactly what it was asked to do, and the architecture is better for it.
