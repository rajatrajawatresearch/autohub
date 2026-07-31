# AutoHub

India's automobile commerce operating system — buy, sell, rent, lease, auction, attach, finance,
insure. One vehicle model, many transaction intents, one settlement spine.

> **Start here:** [`docs/00-north-star.md`](docs/00-north-star.md). It is the anchor every other
> document and every line of code derives from. Nothing in this repo should contradict it.

---

## The one idea

Everything else follows from this: **nothing that a business person could plausibly want to change
is written in code.** Ten registries hold it instead — in the database, versioned, audited, cached.

| Registry | What stops being a deployment |
|---|---|
| Taxonomy | Adding "Construction Equipment" with a `boom_length` attribute |
| Form Schema | Adding a field to the sell flow across web, Android and iOS at once |
| Workflow | Inserting a "Legal Hold" state before payout |
| Pricing & Commission | Changing auction commission to 1.5% in Pune only |
| RBAC | Creating "Regional Auction Moderator — West" |
| Feature & Module | Launching Lease in Bengaluru only, to dealers only |
| Notification | Changing the outbid WhatsApp message in Marathi |
| Integration | Swapping Razorpay for Cashfree, keeping Razorpay as fallback |
| Localisation | Shipping Tamil without an app release |
| Geography | Opening a new service city |

The test applied to every pull request:

> Does this code contain a business constant, a status string, a percentage, a label, a list of
> options, or an `if (type == "…")`? **Then it belongs in a registry.**

---

## Getting it running

```bash
# 1. Infrastructure — Postgres + Redis (ports chosen to avoid clashing with other local stacks)
pnpm infra:up

# 2. Backend — migrates and seeds on first run in Development
pnpm api                       # http://localhost:5180  ·  Scalar docs at /scalar

# 3. Frontends
pnpm dev                       # customer portal  → http://localhost:3100
pnpm dev:dealer                # dealer portal    → http://localhost:3101
pnpm dev:admin                 # admin console    → http://localhost:3102
pnpm dev:all                   # all three at once

# 4. Mobile (Flutter 3.44+)
cd mobile && flutter pub get
flutter run --dart-define=API_URL=http://10.0.2.2:5180/api/v1   # 10.0.2.2 = host, from an emulator
```

The frontends run **without the backend**. Set `NEXT_PUBLIC_API_MOCK=1` and `@autohub/api` serves
realistic Indian fixture data for every endpoint. That is deliberate: the portals had to be
buildable and demoable while the API was still being written.

### Ports

| Service | Port | Note |
|---|---|---|
| Customer web | **3100** | |
| Dealer portal | **3101** | compact density |
| Admin console | **3102** | compact density |
| API | 5180 | Scalar UI at `/scalar` |
| PostgreSQL | **5433** | not 5432 — avoids other local Postgres instances |
| Redis | **6380** | not 6379 |
| OpenSearch | 9201 | `--profile search` |
| MinIO | 9010 / 9011 | `--profile storage` |
| Mailpit | 1026 / 8026 | `--profile full` |
| Jaeger | 16687 | `--profile observability` |

`pnpm infra:up` starts only Postgres and Redis. Everything else is behind a compose profile, so a
laptop with 8 GB of RAM stays usable and the default start stays fast:

```bash
docker compose -f infra/docker-compose.yml --profile full up -d
```

---

## Layout

```
docs/          Analysis, architecture, per-module specs, ADRs, the API contract
backend/       ASP.NET Core 9 modular monolith
  src/
    AutoHub.SharedKernel/    Result, Entity, Money, RegistrationNumber, UuidV7, IClock
    AutoHub.Platform/        THE TEN REGISTRIES + workflow, pricing and form engines
    AutoHub.Persistence/     EF Core, migrations, outbox, interceptors
    AutoHub.Integrations/    Payment, SMS, OCR, KYC adapters
    AutoHub.Api/             Host, auth, middleware, endpoints
    Modules/                 20 domain modules
apps/          web · dealer · admin  (Next.js 15 App Router)
packages/      ui (design system) · api (typed client) · forms (schema renderer) · config
mobile/        Flutter pub workspace
  packages/
    autohub_core/    transport, models, registry cache, offline outbox, JsonLogic
    autohub_ui/      the Registry design language as widgets
    autohub_forms/   the Form Schema Registry's Flutter renderer
  apps/              customer · dealer · partner
infra/         docker-compose, Postgres init
```

**Why the mobile app exists at all.** Three web portals sharing a TypeScript package agree with each
other because they share code. A Flutter app, in a different language, compiled ahead of time and
shipped through an app store, is the first real test of the claim on the front page: that adding a
field to the sell flow reaches web, Android and iOS from one database row. The success criterion is
written down in [`docs/10-mobile-architecture.md`](docs/10-mobile-architecture.md) §1, and if it
stops being true the registries are decoration.

**Module isolation:** a module may reference `SharedKernel`, `Platform`, and other modules'
`Contracts` folder — never another module's `Domain`, `Application` or `DbContext`. Cross-module
reads go through contracts; cross-module writes go through events. That rule is what makes
extracting a module into its own service a project-reference change rather than a rewrite.

---

## The parts worth reading first

| File | Why |
|---|---|
| [`docs/00-north-star.md`](docs/00-north-star.md) | The whole architecture in one document |
| [`docs/01-design-system.md`](docs/01-design-system.md) | The visual language and why it is not another retail marketplace |
| [`docs/contracts/registry-api.md`](docs/contracts/registry-api.md) | The exact seam between frontend and backend |
| `backend/src/AutoHub.Platform/Workflow/WorkflowDefinition.cs` | Why there is no `status` column anywhere |
| `backend/src/AutoHub.Platform/Pricing/PricingEngine.cs` | Every rupee the platform earns, as data |
| `backend/src/AutoHub.SharedKernel/Money/Money.cs` | Why money is `long` paise and `Allocate` exists |
| `backend/src/AutoHub.SharedKernel/Vehicles/RegistrationNumber.cs` | The most-typed string in the product |
| `packages/ui/src/canon/plate-block.tsx` | The design signature — a plate as the visible primary key |

---

## Conventions

- **Money** is always minor units (paise) as `long`, paired with a currency. Never `decimal` rupees,
  never `float`. Formatting happens at the edge, in lakh/crore — `₹8.4L`, not `$840,000.00`.
- **Ids** are UUID v7 — time-sortable, so inserts land at the right edge of the index instead of
  scattering across it.
- **Time** comes from `IClock`, never `DateTime.UtcNow`. Auction correctness depends on one clock.
- **Database** is `snake_case`, plural tables, schema per module, every table audited and soft-deletable.
- **API** is `/api/v1/{module}/{resource}`, cursor-paginated, RFC 9457 Problem Details with a stable
  machine `code` on every error, `Idempotency-Key` required on every write.
- **Frontend** is Server Components by default; `"use client"` is a justified decision, not a habit.

---

## Tests

```bash
cd backend && dotnet test          # domain + engine unit tests
pnpm typecheck                     # every workspace
pnpm build                         # every package and app
cd mobile && dart analyze          # zero issues; warnings are errors here too
cd mobile && ./tool/test_all.sh    # all six suites, no device needed
```

Generated Dart sources (`*.g.dart`, `*.freezed.dart`) are committed so a fresh clone analyzes
without knowing to run a code generator first. Regenerate after changing a model:

```bash
cd mobile && dart run build_runner build --delete-conflicting-outputs
```

The unit suite covers the parts where being wrong is expensive: money allocation and rounding,
registration-number parsing, pricing slab/tiered/cap/GST evaluation, and the workflow engine's
guards, permissions, timers and effect-failure semantics.

---

## What is deliberately not here

Kubernetes, service mesh, event sourcing, GraphQL, multi-region. Each is an answer to a scaling
problem this system does not have yet, and each is recorded in `docs/adr/` with the concrete signal
that would make us revisit it.
