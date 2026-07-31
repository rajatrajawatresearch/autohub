# AutoHub — North Star

> This is the **anchor document**. Every other doc, ADR, and line of code derives from here.
> If something contradicts this file, this file wins until an ADR supersedes it.

---

## 1. What we are building

A **metadata-driven automotive commerce operating system** for India.

Not a listings site with extra pages bolted on. The platform models one thing — a **vehicle moving
through a commercial lifecycle** — and every module (buy, sell, rent, lease, auction, attach,
finance, insure, inspect) is a different *transaction shape* over that same lifecycle.

That single insight drives the entire architecture:

```
                     ┌───────────────────────────┐
                     │   VEHICLE (canonical)      │
                     │   identity + provenance    │
                     └────────────┬──────────────┘
                                  │
              ┌───────────────────┼───────────────────┐
              │                   │                   │
        ┌─────▼─────┐       ┌─────▼─────┐       ┌─────▼─────┐
        │  LISTING  │       │ VALUATION │       │INSPECTION │
        │ (an offer)│       │ (a price) │       │ (a truth) │
        └─────┬─────┘       └───────────┘       └───────────┘
              │
   ┌──────────┼──────────┬──────────┬──────────┬──────────┐
   │          │          │          │          │          │
 SALE      AUCTION     RENTAL     LEASE    ATTACHMENT  EXCHANGE*
   └──────────┴──────────┴──────────┴──────────┴──────────┘

   * EXCHANGE is declared in the intent enum and reserved, but NOT built. It is the only
     intent that needs a deal to carry two vehicles, and `deals` currently has a single
     non-null `vehicle_id`. Multi-asset deals are Phase 5 work. Until then this is a
     reservation, not a capability — see docs/09-decisions-log.md D5.
                          │
                   ┌──────▼──────┐
                   │    DEAL     │  ← one ledger, one escrow, one commission engine
                   └──────┬──────┘
                          │
              ┌───────────┼───────────┐
           FINANCE     INSURANCE   LOGISTICS
```

**One `Vehicle`. Many `Listing` intents. One `Deal` settlement spine.**
Adding "spare parts marketplace" or "driver marketplace" in v2 means adding a listing intent —
not a new vertical codebase.

---

## 2. The Prime Directive — nothing is hardcoded

The client's requirement, stated plainly: *"no hardcoding, metadata driven."*

We enforce it with **ten registries**. If a business person could plausibly want to change it
without a deploy, it lives in a registry — in the database, versioned, audited, cache-backed.

| # | Registry | Kills the hardcoding of | Change without deploy |
|---|----------|-------------------------|------------------------|
| 1 | **Taxonomy** | vehicle categories, makes, models, variants, fuel/body/transmission, per-category attributes | Add "Construction Equipment" with `boom_length` attribute |
| 2 | **Form Schema** | every input form in every app (sell flow, KYC, inspection checklist, dealer onboarding) | Add a field to the sell flow across web + Android + iOS |
| 3 | **Workflow** | statuses, transitions, guards, side-effects for Listing / Deal / Auction / Booking / Loan / Claim | Insert a "Legal Hold" state before payout |
| 4 | **Pricing & Commission** | every rupee the platform earns — slabs, tiers, city overrides, GST | Change auction commission to 1.5% in Pune only |
| 5 | **RBAC** | roles, permissions, and their *scope* (global / city / branch / dealer) | Create "Regional Auction Moderator — West" |
| 6 | **Feature & Module** | which modules exist, for whom, where | Launch Lease in Bengaluru only, to dealers only |
| 7 | **Notification** | templates × channel × locale × event, and who receives them | Change the WhatsApp bid-outbid message in Marathi |
| 8 | **Integration** | payment gateway, SMS, OCR, KYC, bureau, valuation model — with fallback chains | Swap Razorpay → Cashfree; keep Razorpay as fallback |
| 9 | **Localization** | all user-facing strings, served from API, cached | Ship Tamil without an app release |
| 10 | **Geography & Master Data** | states, cities, RTO offices, pincodes, service areas, operating hours | Open a new service city |

### The test we apply to every PR

> Does this code contain a business constant, a status string, a percentage, a label,
> a list of options, or an `if (type == "...")`?
> **If yes, it belongs in a registry.**

Concretely, these are **banned** in application code:

```csharp
if (listing.Status == "PENDING_APPROVAL")     // ✗ workflow registry
commission = amount * 0.02m;                   // ✗ pricing registry
if (user.Role == "Dealer")                     // ✗ RBAC registry (use permissions)
var fuelTypes = new[] { "Petrol", "Diesel" };  // ✗ taxonomy registry
"Your bid was outbid"                          // ✗ notification + localization registry
```

Allowed constants: technical invariants only (HTTP codes, cache keys, cryptographic params,
column names). Business meaning never.

---

## 3. Stack decisions (locked)

| Layer | Choice | Why this and not the alternative |
|-------|--------|----------------------------------|
| Backend | **ASP.NET Core 9**, modular monolith | PRD's first choice; team has .NET 9 installed; modular monolith gives service boundaries without day-one distributed-systems tax |
| Backend pattern | Clean Architecture per module + MediatR CQRS + Outbox | Modules stay extractable to microservices; outbox makes eventing reliable from day one |
| Primary DB | **PostgreSQL 16** | JSONB is load-bearing for the registries (schemas, rules, attributes); partial + GIN indexes; row-level security available later for multi-tenant |
| Cache | **Redis 7** | Registry cache, session, rate limit, auction bid state, distributed lock |
| Search | **OpenSearch** | Faceted vehicle search over dynamic attributes; geo queries; typo tolerance |
| Object store | **S3-compatible** (R2 in prod, MinIO local) | Media, inspection photos, generated PDFs |
| Events | In-process bus + **Outbox → RabbitMQ/MassTransit-ready** | Same handler code works in-process today, out-of-process tomorrow |
| Web | **Next.js 15 App Router**, React 19 | Server Components for SEO-critical listing pages; one framework across 3 portals |
| Styling | **Tailwind v4 + shadcn/ui** | Token-driven; design system lives in `packages/ui` |
| Client state | **TanStack Query** + Zustand (UI-only) | Server state ≠ client state |
| Forms | **React Hook Form + Zod**, driven by Form Schema Registry | Same schema validates on client and server |
| API types | **Generated from OpenAPI** | Hand-written types are hardcoding. Types drift = bugs. |
| Mobile | **Flutter** (customer / dealer / partner) | PRD choice; shares the same Form Schema + Workflow registries |
| Auth | JWT access + rotating refresh, OTP-first, OAuth | India is phone-first: OTP is the primary factor, password optional |

**Deliberately deferred** (documented in ADRs, not built on day one): Kubernetes, service mesh,
event sourcing, GraphQL, multi-region. Each is a scaling answer to a problem we do not yet have.

---

## 4. Repository shape

```
autohub/
├── docs/                    Analysis, architecture, module specs, ADRs
├── backend/
│   ├── AutoHub.sln
│   ├── src/
│   │   ├── AutoHub.Api/               Host: middleware, endpoints, OpenAPI, auth
│   │   ├── AutoHub.SharedKernel/      Result, Error, IDs, base entity, domain events
│   │   ├── AutoHub.Platform/          THE 10 REGISTRIES + engines (workflow, rules, forms)
│   │   ├── AutoHub.Persistence/       EF Core, DbContext, migrations, outbox, interceptors
│   │   ├── AutoHub.Integrations/      Adapters: payment, sms, whatsapp, ocr, kyc, storage
│   │   └── Modules/
│   │       ├── AutoHub.Modules.Identity/
│   │       ├── AutoHub.Modules.Catalog/        vehicles + taxonomy read side
│   │       ├── AutoHub.Modules.Listings/
│   │       ├── AutoHub.Modules.Auction/
│   │       ├── AutoHub.Modules.Rental/
│   │       ├── AutoHub.Modules.Lease/
│   │       ├── AutoHub.Modules.Attachment/     taxi attachment centre
│   │       ├── AutoHub.Modules.Valuation/
│   │       ├── AutoHub.Modules.Inspection/
│   │       ├── AutoHub.Modules.Deals/          orders, escrow, settlement
│   │       ├── AutoHub.Modules.Finance/        loan marketplace
│   │       ├── AutoHub.Modules.Insurance/
│   │       ├── AutoHub.Modules.Payments/
│   │       ├── AutoHub.Modules.Chat/
│   │       ├── AutoHub.Modules.Bookings/
│   │       ├── AutoHub.Modules.Documents/
│   │       ├── AutoHub.Modules.Notifications/
│   │       ├── AutoHub.Modules.Reviews/
│   │       ├── AutoHub.Modules.Trust/          fraud, KYC, blacklist
│   │       └── AutoHub.Modules.Analytics/
│   └── tests/
├── apps/
│   ├── web/                 Customer portal (Next.js)
│   ├── dealer/              Dealer portal (Next.js)
│   └── admin/               Admin + Super Admin (Next.js)
├── packages/
│   ├── ui/                  Design system — tokens, primitives, AutoHub components
│   ├── api/                 Generated OpenAPI client + Zod schemas + TanStack hooks
│   ├── forms/               JSON-Schema → React form renderer (Registry #2 consumer)
│   ├── workflow/            Client-side workflow state helpers (Registry #3 consumer)
│   └── config/              Shared tsconfig / eslint / tailwind preset
├── mobile/                  Flutter workspace (customer, dealer, partner)
├── infra/                   docker-compose, migrations runner, seed, CI
└── scripts/
```

**Module isolation rule:** a module may reference `SharedKernel`, `Platform`, and other modules'
`Contracts` folder **only**. Never another module's `Domain`, `Application`, or `DbContext`.
Cross-module reads go through contracts; cross-module writes go through events. This is what
makes extraction to a microservice a build-file change, not a rewrite.

---

## 5. Naming and conventions

- **Backend:** `PascalCase` types, `ILikeThis` interfaces, one class per file, feature folders
  (`Listings/Application/CreateListing/{Command,Handler,Validator,Response}.cs`).
- **Database:** `snake_case` tables and columns, plural tables (`vehicle_listings`),
  `id` is `uuid v7` (time-sortable), every table carries `created_at, created_by, updated_at,
  updated_by, deleted_at, row_version`.
- **API:** `/api/v1/{module}/{resource}`, kebab-case paths, cursor pagination, `Result<T>` envelope,
  RFC 9457 Problem Details for errors, every error has a stable machine code (`listing.not_found`).
- **Frontend:** `kebab-case` files, `PascalCase` components, colocated `*.test.tsx`,
  server components by default — `"use client"` is an explicit, justified decision.
- **Money:** always minor units (paise) as `long` / `bigint`. Never `float`. Always paired with
  currency code. Never format money in the domain layer.
- **Time:** `timestamptz`, UTC in storage, IST for display, formatting only at the edge.

---

## 6. Non-negotiable quality floor

- Every write endpoint is idempotent (`Idempotency-Key` header) — payments and bids demand it.
- Every state change emits a domain event through the outbox. No fire-and-forget side effects.
- Every registry read is cached with explicit invalidation on write. Never a per-request DB hit.
- Every list endpoint is paginated. No unbounded queries reach production.
- Every user-facing string comes from the localization registry. No literals in components.
- Every money movement writes a double-entry ledger row. The ledger is the truth, not the order row.
- Accessibility: keyboard reachable, visible focus, `prefers-reduced-motion` respected, AA contrast.
- Audit: who, what, before, after, when, from where — for every admin and money action.

---

## 7. Build order (what ships in what order and why)

| Phase | Ships | Rationale |
|-------|-------|-----------|
| **0 — Spine** | Registries, workflow engine, rules engine, auth, RBAC, outbox, design system | Nothing else can be built correctly without these. This is the whole bet. |
| **1 — Supply** | Vehicle + taxonomy, sell flow, listings, media pipeline, valuation v1, inspection | A marketplace with no cars is a website. Supply first. |
| **2 — Demand** | Search, listing detail, chat, bookings, test drive, C2C deal + escrow | Now buyers have something to buy. |
| **3 — Dealer** | Dealer onboarding, inventory, bulk upload, CRM, requirement board | Dealers are the liquidity engine and the revenue engine. |
| **4 — Auction** | Live dealer auction, bidding, reserve, auto-extension, settlement | Highest margin. Needs phase 3's verified dealers to exist. |
| **5 — Utility** | Rental, lease, taxi attachment centre | Same listing/deal spine, new intents. Cheap once the spine is right. |
| **6 — Money** | Loan marketplace, insurance marketplace, wallets, payouts, GST invoicing | Referral revenue rides on completed transactions from phases 2–5. |
| **7 — Intelligence** | AI moderation, damage detection, fraud scoring, pricing ML, analytics | Needs the data that phases 1–6 generate. Building it first would be guessing. |

We do not skip Phase 0. Every day of "we'll make it configurable later" costs a month later.

---

## 8. How we know it worked

- A new vehicle category, with its own attributes, its own sell form, its own commission slab,
  goes live **without a deployment**.
- A module is extracted to its own service by changing project references and a connection string.
- The dealer app, customer app, and web portal render the same sell form from the same schema.
- Nobody ever greps the codebase for a status string to find out what states exist.
