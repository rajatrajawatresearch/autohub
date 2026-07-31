# ADR-0001 — Modular monolith over microservices

**Status:** Accepted — 2026-07-29
**Deciders:** Platform architecture
**Supersedes:** none
**Related:** ADR-0003 (metadata-driven), ADR-0005 (outbox), ADR-0007 (ledger)

---

## Context

North Star §4 lists **20 modules** — Identity, Catalog, Listings, Auction, Rental, Lease,
Attachment, Valuation, Inspection, Deals, Finance, Insurance, Payments, Chat, Bookings, Documents,
Notifications, Reviews, Trust, Analytics. On a whiteboard that reads as twenty services.

It is not twenty services. It is **one settlement spine with twenty faces**. Domain model §11
proves it: a SALE, an AUCTION win, a RENTAL booking, a LEASE signing, and an ATTACHMENT cycle all
converge on `deals.deals` → `payments.ledger_entries`. A single auction close writes to
`auction.lots`, `deals.deals`, `deals.deal_line_items`, `payments.ledger_transactions`,
`payments.ledger_entries` and `platform.outbox_messages` — and either **all six land or none do**.

The specific failure mode we are avoiding has a name and we have watched it happen:

> Auction service commits the lot close. Deal service accepts the `lot.won` event and opens a deal.
> Payment service is mid-deploy and 502s the escrow debit. Nobody owns the compensation. Three days
> later a dealer calls support because ₹4,80,000 of EMD is neither refunded nor applied, and the only
> way to find out is to join three databases by hand in a spreadsheet.

Distributed sagas are the correct answer to that — but only when you are *forced* into them. Buying
sagas, service discovery, per-service migrations, distributed tracing-as-a-prerequisite, and N
on-call rotations before the first dealer is onboarded is paying a tax on a problem we do not have.

Second forcing function: **the registries are shared by everyone** (North Star §2). Taxonomy,
Workflow, Pricing, RBAC, Feature, Notification, Integration, Localization, Geography — every module
reads all ten on almost every request. In a microservice world each registry becomes either a
network hop on the hot path or a replicated cache with its own consistency problem. In one process
it is a `MemoryCache` with an epoch check.

## Decision

**One ASP.NET Core 9 deployable. One PostgreSQL 16 database. Schema-per-module. Hard compile-time
module boundaries.**

| Concern | How |
|---|---|
| Physical boundary | One host process `AutoHub.Api`; modules are class libraries under `src/Modules/` |
| Data boundary | One database, **one PG schema per module** (`listings.`, `auction.`, `deals.`, `payments.`, `platform.`) |
| Reference rule | A module may reference `SharedKernel`, `Platform`, and other modules' `Contracts/` folder **only**. Never another module's `Domain`, `Application`, or `DbContext` |
| Enforcement | ArchUnitNET test in `tests/AutoHub.Architecture.Tests` fails the build on a forbidden namespace reference. Not a convention — a red test |
| Cross-module read | Synchronous call through `IListingContract` etc., resolved in-process, `Result<T>` return |
| Cross-module write | **Never direct.** Emit a domain event through the outbox (ADR-0005); the owning module handles it |
| Transaction scope | One `DbContext` per request, one `SaveChanges`, one PG transaction. Cross-module consistency inside a request is *real* ACID, not a saga |
| Scaling unit | The **same binary** started with `AUTOHUB_ROLE=api \| dispatcher \| auction \| worker`. Role gates which hosted services start, not which code is compiled |

The auction hot path gets its own deployment of that same binary (`AUTOHUB_ROLE=auction`) behind its
own ingress and its own connection pool, so a snipe spike on a hot lot cannot starve the sell-flow's
DB connections. **Same code, different pods, different pool.** That buys 90% of the isolation
benefit of microservices for 5% of the cost.

## Consequences

**Positive**

- A deal close is one PG transaction. There is no saga to write, no compensating action to forget,
  and no orphaned EMD.
- Registry reads are in-process. A page that touches Taxonomy + Workflow + Pricing + RBAC +
  Localization costs zero network hops.
- One migration pipeline. A change that adds `deals.deal_line_items.tcs_minor` and the pricing
  registry row that populates it ships atomically.
- Refactoring across module boundaries is a compiler problem, not an archaeology problem.
- Extraction later is a *build-file change*: the Contracts interface stays, its implementation moves
  from an in-process class to an HTTP/MassTransit proxy, the outbox already exists. North Star §8
  states this as an acceptance test and this is the mechanism that makes it true.

**Negative — stated honestly**

- **One blast radius.** A memory leak in the Chat module's SignalR backplane takes down valuations.
  Mitigation: role-split deployment, per-module `HealthCheck` registration, and a circuit breaker
  around every integration adapter — not wishful thinking.
- **One runtime.** One thread pool, one GC. A synchronous PDF render in Documents can add tail
  latency to bid placement. Mitigation: all CPU-heavy work (PDF, image derivatives, OpenSearch bulk
  indexing) runs in `AUTOHUB_ROLE=worker`, never in the request path.
- **One database.** Table bloat in `analytics.search_events` shares WAL with `payments.ledger_entries`.
  Mitigation: `analytics` is partitioned and on a separate tablespace from day one; heavy read
  analytics goes to a read replica.
- **Deploy coupling.** Six teams merging to one main branch means a broken Insurance test blocks an
  Auction hotfix. Mitigation: trunk-based, feature flags from Registry #6, `main` always deployable.
- **Team scaling ceiling.** This breaks somewhere around 40–50 engineers on one codebase.
- **The boundary rule is only as strong as the test.** If someone disables the ArchUnit test to
  "unblock a release", the whole ADR evaporates within two sprints. That test is a P0.

## Alternatives considered

| Alternative | Why rejected |
|---|---|
| **Microservices from day one** (service per module) | 20 services × (repo + pipeline + DB + dashboard + on-call) before the first ₹ of GMV. The deal/ledger transaction becomes a saga with 6 compensations. We would spend Phase 0 building infrastructure instead of the registries, which are the actual bet. |
| **Service per vertical** (buy / rent / auction as 3 services) | The verticals are not independent — they *share* the Deal and Ledger spine (Domain §11, §15). Splitting on the vertical axis puts the transaction boundary in exactly the wrong place, and duplicates the settlement engine 3×. |
| **Serverless functions** (Lambda / Azure Functions per endpoint) | Cold starts on a 500ms-budget auction bid; no in-process registry cache (every invocation refetches ten registries); connection pool exhaustion against PG under a snipe spike. |
| **Monolith with no module boundaries** ("we'll separate it later") | We have never once seen "later" arrive. Without the Contracts rule and the ArchUnit test, `ListingsDbContext` gets referenced from Auction in week 6 and extraction becomes a rewrite. The boundaries are cheap *now* and impossible *later*. |
| **Database per module, single process** | Loses the one property that justifies the monolith — the single ACID transaction across Deal + Ledger + Outbox. Pays the distributed-data cost without the distributed-deploy benefit. |

## Revisit when

Any **one** of these is a real signal, not a feeling:

1. A single module's steady-state CPU exceeds **40%** of total process CPU and cannot be tamed by
   role-splitting — that module has earned its own service (Auction is the likely first).
2. Deploy frequency drops below **1/day** because of cross-team merge contention, or mean time from
   merge to prod exceeds **45 minutes**.
3. More than **30 engineers** are committing to `backend/` in a month.
4. A module needs a genuinely different runtime profile — e.g. Analytics needing a 32 GB working set,
   or Intelligence (Phase 7) needing GPU nodes. Extract that one, keep the rest.
5. `platform.outbox_messages` sustains **> 2,000 events/sec**, at which point the in-process
   dispatcher should be swapped for MassTransit/RabbitMQ (already the designed path) and extraction
   becomes low-risk.

We extract **one module at a time**, starting with the one that triggered the signal — never a
"microservices migration" as a project.
