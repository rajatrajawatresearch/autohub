# ADR-0012 — Next.js 16 App Router, three separate web portals

**Status:** Accepted — 2026-07-29
**Deciders:** Platform architecture
**Related:** ADR-0003 (metadata-driven), ADR-0009 (generated clients), ADR-0011 (Flutter)

---

## Context

North Star §3 locks Next.js 16 App Router with React 19, and §4 locks three apps: `apps/web`
(customer), `apps/dealer`, `apps/admin`. This ADR records why *three* rather than one, and what the
App Router commitment actually costs.

The three surfaces are not variations of each other:

| | `apps/web` | `apps/dealer` | `apps/admin` |
|---|---|---|---|
| Traffic | ~95% of sessions, mostly organic search, mostly mobile 4G | Thousands of sessions, hours long, desktop | Dozens of internal users |
| Rendering need | **SEO-critical**, ~500k indexable listing pages | Auth-gated app shell, zero SEO | Auth-gated, zero SEO |
| Density (Design System §4) | `comfortable`, 72rem | `compact`, 88rem | `compact`, 88rem |
| Bundle budget | Ruthless — every KB is conversion | Generous | Irrelevant |
| Auth | Anonymous by default; the homepage plate input needs **no phone number** | Staff-scoped, org context, TOTP for money actions | High-privilege, TOTP mandatory |
| Deploy risk | Public revenue funnel | Dealer revenue | Internal |

The failure being avoided, first:

> **The unindexed long tail.** An SPA renders listing detail client-side. Google indexes the shell.
> For an Indian auto marketplace the long tail *is* the acquisition channel — "second hand swift
> dzire in pune under 5 lakh", "used bolero baramati", "i20 asta 2018 kolhapur" — tens of thousands
> of low-volume queries that collectively dwarf brand search. Losing them means paying for every
> visitor, forever. This is not an optimisation; it is the business model.

Second, and it is the reason for three apps rather than one:

> **The shared blast radius.** A single Next app with route groups. Someone ships a bad
> `middleware.ts` change for an admin RBAC feature. The middleware runs on every request, including
> `/listings/[slug]`. The public funnel 500s for eleven minutes during peak evening traffic. Admin
> has 30 users; the funnel has the entire business.

## Decision

**Three independent Next.js 16 applications in the monorepo, sharing `packages/{ui,api,forms,workflow,config}`.
The customer app is rendering-optimised; the other two are app-shell-optimised.**

### 1. `apps/web` — customer, server-first

- **React Server Components by default.** `"use client"` is an explicit, justified decision
  (North Star §5), enforced by an ESLint rule requiring a `// client: <reason>` comment.
- **Cache Components / PPR** on listing detail: the static shell (photos, specs, description,
  `PlateBlock`, structured data) is prerendered; the dynamic holes (live price, `state_key` pill,
  `available_actions[]`, saved-listing state) stream in. A crawler gets a complete document
  immediately; a buyer gets current state.
- **Revalidation is event-driven, not time-driven.** The outbox consumer (ADR-0005) for
  `listings.listing.price_changed.v1`, `listings.listing.state_changed.v1` and
  `listings.listing.reserved.v1` calls a signed internal revalidation endpoint that invokes
  `revalidateTag('listing:' + id)`. A sold car disappears from the indexable page in seconds, not at
  the next ISR window. A time-based `revalidate: 3600` on a marketplace means Google indexes sold
  cars — a ranking penalty and a support-ticket generator.
- **Structured data** (`Vehicle`, `Product`, `Offer`, `AggregateRating`) rendered server-side from
  the same data as the page, with `priceCurrency: "INR"` and price in **rupees** (schema.org wants
  major units; our storage is paise — the conversion happens exactly once, at this edge).
- **Money and dates are formatted only at the edge** (North Star §5): `MoneyValue` renders
  `₹8.4L` / `₹1.2Cr`; timestamps render IST. The server sends paise and UTC.

### 2. `apps/dealer` and `apps/admin` — client-first app shells

- A thin server layer for auth, scope resolution and the initial registry bootstrap; everything below
  is client components with TanStack Query. Server-rendering a 3,000-row inventory `DataTable` is
  paying SSR cost for zero SEO benefit and worse interactivity.
- `density="compact"` on `<html>`, 88rem container (Design System §4).
- Admin holds the **Registry Explorer** and change-set workflow (ADR-0003 §4) — diff view, dry-run
  results, approval, rollback. This is the tooling that makes the whole metadata bet usable, and it
  is a Phase 0 deliverable.
- Live surfaces (auction `BidTicket`, chat) use a SignalR hub at `/hubs/{hub}` with the generated
  client's auth, not a bespoke socket layer per app.

### 3. Localization: registry, not static catalogs

This is where the standard Next.js i18n approach collides with the Prime Directive. `next-intl` and
friends assume a compiled-in message catalog — which means Tamil requires a deploy, contradicting
North Star §2 (Registry #9).

Our resolution:

```
root layout (server) → fetch locale bundle from GET /api/v1/platform/i18n/{locale}
                     → cacheTag(`i18n:${locale}:${registryEpoch}`)
                     → provide via context; client components read from it
compiled-in `en` bundle exists ONLY as the cold-start / API-down fallback
```

The bundle is cached at the CDN keyed by registry epoch, so it is one fetch per epoch per locale, not
per request. A copy fix in Marathi is a registry write plus a tag invalidation.

### 4. Shared packages and the version-skew rule

`packages/ui`, `packages/api` (generated, ADR-0009), `packages/forms`, `packages/workflow`,
`packages/config`. Single pnpm lockfile, Turborepo pipeline, `workspace:*` versions. **All three apps
build against the same commit of every shared package** — no independent versioning. That is the
price of three deployables sharing a design system, and it is cheaper than the alternative
(published versions, three upgrade PRs, and a dealer portal running a six-week-old `PlateBlock`).

### 5. Proxy/middleware discipline

Each app has its own middleware doing only: session cookie → access token refresh, locale
resolution, `X-AutoHub-Scope` injection, and a redirect for unauthenticated app routes. **No
business logic, no registry evaluation, no RBAC decisions in middleware** — RBAC is a server
decision (the API is the authority), and middleware runs on paths you did not intend often enough
that putting authorization there is how you get an outage or a bypass.

## Consequences

**Positive**

- The long-tail funnel is server-rendered, fast on 4G, and indexable — the acquisition channel works.
- An admin deploy cannot break the customer funnel. Three pipelines, three blast radii.
- Bundle budgets are enforceable per app: the customer app can refuse a 40 KB dependency the admin
  app happily takes.
- One framework, one component model, one design system across all three — a `StatusPill` fix ships
  everywhere at once (Design System §6).
- RSC + the generated client means listing pages fetch server-side with no client waterfall, no
  loading spinner chorus, and no API token exposed to the browser for anonymous reads.
- PPR gives us the SEO of static and the correctness of dynamic on the same page, which is exactly
  the marketplace-listing problem.

**Negative — stated honestly**

- **The App Router's caching model has moved twice in two years.** `fetch` caching defaults, Cache
  Components, `revalidateTag` vs `updateTag` — this is real churn on a load-bearing dependency.
  Mitigation: pin the Next major, own the upgrade as a scheduled quarterly task with a dedicated PR,
  and keep caching behaviour **explicit at every call site** rather than relying on defaults. Implicit
  caching is the number-one source of "why is this page stale" incidents.
- **The server/client boundary is a real cognitive cost.** Passing a non-serialisable value across it,
  or accidentally pulling a 60 KB client dependency into a server component's client child, are
  ordinary mistakes. Mitigation: the `// client: <reason>` rule, and a CI bundle-size budget per
  route that fails the build.
- **Registry-driven forms are inherently client components.** `SchemaForm` needs interactivity, so
  the sell flow is a client island; its schema is fetched server-side and passed down as props to
  avoid a client waterfall. Hydration mismatches on conditionally-visible fields are a known bug
  class — the renderer must produce identical output server and client, so no `Math.random()` ids and
  no `Date.now()` in field keys.
- **Three build pipelines, three preview environments, three sets of env vars.** More CI minutes and
  more infrastructure config than one app.
- **SSR cost is real money.** Server-rendering listing pages needs Node capacity that a static SPA
  would not. Mitigated by PPR (the shell is static and CDN-served) and by event-driven revalidation
  keeping the dynamic surface small.
- **Shared-package changes ripple.** A `packages/ui` change rebuilds and retests three apps. Turbo's
  cache makes this tolerable; the coupling is genuine and accepted.
- **Vendor gravity.** Next.js is easiest to run on Vercel. We keep deployment portable
  (standalone output, containerised) and avoid platform-only primitives on the critical path — but
  this is a pressure, not a solved problem.

## Alternatives considered

| Alternative | Why rejected |
|---|---|
| **One Next.js app with route groups** `(customer)/(dealer)/(admin)` | Shares `middleware.ts`, the build, the deploy and the bundle graph. The Context failure applies directly: an admin change takes down the funnel. Also forces one auth posture and one density on three very different products. Rejected on blast radius, not on effort. |
| **Vite SPA + a separate prerender service for SEO** | Two rendering paths for the same pages means they *will* diverge, and the prerenderer becomes a permanently-behind shadow implementation of the listing page. Also loses streaming and per-route code splitting quality. |
| **Remix / React Router 7** | Genuinely good, especially its data/mutation model. Rejected because the RSC + PPR combination is the specific feature that solves our exact problem (static-shell-plus-live-price on 500k pages), and because North Star §3 already commits, so re-litigating costs the team more than it gains. |
| **Astro for the customer site + a React SPA for dealer/admin** | Best-in-class SEO output. Rejected because it forks the component model: `packages/ui` would need an Astro build and a React build, and the design system would drift within two quarters. The design system's cross-surface consistency (Design System §6) is worth more than Astro's marginal payload advantage. |
| **ASP.NET Core Razor / Blazor** (backend-rendered, one stack) | Genuinely tempting for a .NET backend team — one language, no API boundary for server-rendered pages. Rejected: the design system, form renderer and generated client are React/TypeScript, the Flutter apps already consume the same JSON API, and Blazor WASM payloads are wrong for a 4G-first consumer funnel. |
| **Static export + client-side data for the customer app** | Cannot express 500k listing pages with live prices. ISR/PPR exists precisely for this. |

## Revisit when

1. **Organic search traffic drops > 15% month-over-month** with no algorithm event — the rendering or
   revalidation path is broken (stale sold listings, or a soft-404 pattern); investigate before
   shipping anything else.
2. Customer app **LCP p75 on 4G exceeds 2.5 s** or the route JS budget exceeds **~120 KB gzipped** —
   the RSC boundary has leaked and client components are creeping into the critical path.
3. A Next major upgrade requires **> 1 sprint** of migration work twice in a row — that is the churn
   signal; freeze on the current major and evaluate Remix/React Router or a plainer SSR setup.
4. Server-render cost exceeds **~10%** of total infrastructure spend — push more of the listing page
   into the static PPR shell and shrink the dynamic holes to price + state only.
5. Dealer and admin diverge enough that they share **< 30%** of `packages/ui` usage — merge them into
   one internal console (they share auth posture and density anyway) and cut a pipeline.
6. Third-party partners (lenders, insurers) need embedded UI — that is a microfrontend or an iframe
   widget question, and it should get its own ADR rather than being smuggled into one of these three.
