# ADR-0011 — Flutter for the customer, dealer and partner apps

**Status:** Accepted — 2026-07-29
**Deciders:** Platform architecture
**Related:** ADR-0003 (metadata-driven), ADR-0009 (generated clients), ADR-0012 (web)

---

## Context

North Star §3 locks Flutter and states the reason: *"PRD choice; shares the same Form Schema +
Workflow registries."* This ADR records what that actually implies, and the India-specific costs.

Three apps, three very different jobs:

| App | User | Hard requirement |
|---|---|---|
| **Customer** | Buyer / C2C seller | Install size and cold start on a 2019 Android with 3 GB RAM; sell flow with a 12-photo capture; plate-based valuation |
| **Dealer** | Dealer principal + staff | Live auction bidding with sub-second state, dense inventory tables, works while walking a yard |
| **Partner** | Inspector / driver / RC agent | **Offline-first.** Inspections happen in basement parkings and on highways with no signal. 60–120 photos per inspection, GPS-stamped, uploaded later. |

Device reality: the modal user is on Android 11–13, a mid-range Snapdragon/Helio, 4 GB RAM, 64 GB
storage that is 85% full, on a metered 4G plan. iOS is ~8% of users but disproportionately dealers
and admins.

The failure being avoided:

> **The six-codebase tax.** Native Android + iOS × three apps. The Form Schema Registry adds a field
> to the sell flow. It renders on web the same day (schema-driven), on Android in the next sprint,
> and on iOS three sprints later because the iOS dev is on the dealer app. **The registry's entire
> value proposition is destroyed by the client that has not shipped yet** — the field exists but two
> thirds of users cannot fill it, so the business cannot rely on it, so nobody uses the registry.

And the second one:

> **The bridge that dropped frames.** A React Native inspection flow: 90 photos, each compressed and
> written to a local queue. The JS↔native bridge serialises image metadata; the list view janks at
> 12 fps on a mid-range device; the inspector's tap on "Next section" is dropped and they redo the
> section. Field staff stop using the app and go back to WhatsApp photos, which destroys the
> inspection data model.

## Decision

**One Flutter workspace under `mobile/`, three app packages, one shared core. Registry-driven
surfaces are rendered, not written.**

```
mobile/
├── packages/
│   ├── autohub_api/        generated dart-dio client (ADR-0009) + AutoHubClient wrapper
│   ├── autohub_core/       auth, session, secure storage, connectivity, sync engine, logging
│   ├── autohub_forms/      JSON-Schema evaluator + widget renderer (Form Schema Registry #2)
│   ├── autohub_workflow/   state_category / caps_json helpers (Workflow Registry #3)
│   ├── autohub_i18n/       runtime locale bundles from Localization Registry #9
│   └── autohub_ui/         design tokens + PlateBlock, VehicleCard, SpecStrip, FieldBlock,
│                           StatusPill, MoneyValue, OdometerValue, BidTicket
└── apps/
    ├── customer/   ├── dealer/   └── partner/
```

### 1. Design tokens are generated, never re-authored

`packages/ui` (the React design system) exports its token set to `design-tokens.json` at build time;
a Dart codegen step emits `autohub_ui/lib/tokens.g.dart`. A CI gate fails if the committed Dart
tokens differ from a fresh generation. **`--ah-highway` is defined once, in one place, and Flutter
consumes it.** Hand-copying hex values into Dart is how two platforms drift into two products.

Components are re-implemented in Flutter (there is no way around that), but the *values* — colour,
spacing scale, radius (2px interactive / 4px card / 0 plate), type scale, the two shadows — are
generated. `PlateBlock` in Dart uses the same JetBrains Mono 700 at +0.14em tracking, the same 2px
`--ah-tarmac` border, the same plate-class colour rules (white=private, yellow=commercial,
green stripe=EV).

### 2. Registry-driven at runtime

- **Forms:** `autohub_forms` fetches the schema from `GET /api/v1/platform/forms/{key}`, caches it
  against `X-AutoHub-Registry-Version`, and renders. A new sell-flow field ships **without an app
  release** — this is the single largest reason Flutter was chosen over three native codebases.
- **Workflow:** action buttons come from `available_actions[]`; status pills from `state_key` +
  `label_key` + `ui_token`. The app never names a state (ADR-0004).
- **Localization:** locale bundles fetched from Registry #9 and cached to disk, with the compiled-in
  English bundle as the cold-start fallback. Tamil ships without a store release.
- **Feature flags:** Registry #6 gates modules per city/persona, evaluated server-side and returned
  in the session bootstrap, so a Lease launch in Bengaluru only is a registry change.

### 3. Offline-first is a first-class subsystem, not a retrofit

The partner app (and the dealer app's inventory edits) use a **local outbox mirroring ADR-0005**:

```
Drift (SQLite) tables:  local_inspections, local_media_queue, local_outbox(op, payload,
                        idempotency_key, attempts, next_attempt_at, state)
```

- Every queued mutation carries a **client-generated `Idempotency-Key`** (uuid v7), stable across
  retries — the same key the generated client sends (ADR-0009 §4). A reconnect after 6 hours in a
  basement replays 40 operations and produces exactly 40 server-side effects, not 80.
- Media uploads are chunked, resumable, and queued separately with a WiFi-only default (data plans
  are metered), backed by presigned S3/R2 URLs.
- Conflict policy: **last-writer-wins is banned for inspections.** A server-side change to an
  inspection already edited offline produces a `sync.conflict` entry the inspector resolves
  explicitly. Silent overwrite of a completed inspection section is a data-integrity failure with
  legal consequences for a vehicle grade.
- The sync engine is in `autohub_core` and shared, so the three apps cannot each invent a different
  broken version.

### 4. Platform channels — the unavoidable native surface

| Need | Why native |
|---|---|
| UPI intent (`upi://pay`) / Razorpay SDK | Payment app handoff; no Flutter-only path |
| DigiLocker / Aadhaar eKYC redirect | Custom Tabs / `ASWebAuthenticationSession` with a return URI |
| Camera with plate/VIN OCR | ML Kit text recognition; on-device, offline, no round trip |
| HSRP plate + odometer capture | Fixed-aspect overlay, torch control, EXIF + GPS stamping |
| SMS autofill for OTP | Android SMS Retriever API (no READ_SMS permission — Play policy) |
| Background upload | WorkManager / BGTaskScheduler |

These are wrapped in `autohub_core` federated plugins with a stub implementation for tests.

## Consequences

**Positive**

- A registry change reaches web, Android and iOS on the same day. The Prime Directive survives
  contact with mobile — which it does not in a three-native-codebase world.
- One Dart codebase for three apps: ~65% of code is in shared packages. Auth, sync, forms,
  workflow, i18n and design tokens are written once.
- Rendering is Flutter's own (Impeller/Skia), so a 120-item inventory list or a live bid ticker
  behaves identically on both platforms — no bridge, no per-platform list-performance investigation.
- Auction bidding gets a single, testable state machine for WebSocket reconnect + optimistic bid
  display + server reconciliation, rather than two.
- Hiring is one Dart pool instead of Kotlin + Swift pools, which matters for a team building 20
  backend modules simultaneously.

**Negative — stated honestly**

- **App size.** A Flutter release APK baseline is ~7–9 MB per ABI before assets; with three apps and
  media handling, expect 18–25 MB. In a market where install size affects conversion on metered
  connections this is a real cost. Mitigation: `--split-per-abi`, Play App Bundle, deferred
  components for the auction and inspection modules, and **no bundled font beyond the three faces**
  (Design System §3) with Devanagari/Tamil subsets loaded on demand.
- **Indic text rendering needs explicit testing.** Devanagari conjuncts (क्ष, ज्ञ), Tamil ligatures,
  and matra positioning are a genuine Flutter bug class, and the failure is *silent* — text renders
  as boxes or mis-stacked glyphs only on some Android versions. Mitigation: a golden-image test suite
  per locale in CI, and a Noto fallback chain configured explicitly rather than relying on the
  platform default.
- **Design-system drift is a codegen job, not a shared runtime.** Tokens are generated; *components*
  are not. A `VehicleCard` change on web does not appear in Flutter. Mitigation: the component canon
  (Design System §6) is a shared spec with visual-regression goldens on both platforms, and a
  design-system change is one ticket with two implementations — deliberately tracked as such.
- **Dart talent pool in India is thinner than React Native's.** Real hiring friction. Partially
  offset by Dart being learnable in a week for anyone with C#/TS experience — which describes this
  team.
- **Platform-channel work does not disappear.** Six native surfaces above, each needing Android +
  iOS implementations. Flutter reduces UI duplication, not OS-integration duplication.
- **Google's roadmap risk.** A single vendor controls the framework. Mitigated only by the fact that
  business logic lives on the server and in registries — a Flutter exit would be a UI rewrite, not a
  product rewrite. That is a deliberate architectural hedge.
- **iOS build/release infrastructure is still required** (certificates, provisioning, TestFlight).
  "One codebase" does not mean one release pipeline.

## Alternatives considered

| Alternative | Why rejected |
|---|---|
| **React Native (+ Expo)** | Strongest alternative: shares TypeScript, the generated TS client (ADR-0009), and Zod validators with web, and the talent pool is deeper in India. Rejected on the two hardest surfaces — the inspection app's 90-photo offline capture and the auction's high-frequency state updates — where the JS/native boundary is exactly where our load is. The New Architecture improves this materially; it is the named revisit trigger. |
| **Native Kotlin + Swift** | Best per-platform quality, and the six-codebase tax in Context. For a team building 20 backend modules in parallel, three native app pairs is not a staffing plan. |
| **PWA / Capacitor for all three** | Fails the partner app outright: reliable background upload, resumable chunked media, on-device OCR, GPS-stamped EXIF, and multi-day offline storage are not dependable in a mobile browser on mid-range Android. Also fails UPI intent handoff and Play-compliant SMS autofill. |
| **Kotlin Multiplatform + Compose Multiplatform** | Genuinely appealing: shared business logic in Kotlin, native-ish UI. Rejected on iOS maturity for a dense, media-heavy product in 2026 and on the fact that our shared logic is thin (the server holds it) — KMP's main benefit is sharing logic we deliberately do not have on the client. |
| **PWA for customer only + Flutter for dealer/partner** | Tempting for install-friction reasons, and it splits the design system across two component models for the *same* customer-facing surfaces. We get the PWA benefit differently: the Next.js customer web app (ADR-0012) already serves low-intent mobile traffic, and the app is for returning users. |
| **One Flutter app with three modes** | Rejected: a buyer should not download inspection tooling; the dealer app needs a different auth posture (staff scoping, TOTP) and a different density; and Play Store listings/ASO for "sell your car" and "dealer auction" target different audiences. |

## Revisit when

1. **React Native's New Architecture (Fabric/TurboModules) demonstrably handles the inspection
   capture path** — measured on a real mid-range Android with 90 photos, not a benchmark. If it does,
   the shared-TypeScript argument becomes strong enough to reconsider for the *customer* app first.
2. Customer app install size exceeds **30 MB** or cold start exceeds **2.5 s** on the P50 device —
   time for deferred components, or a PWA-first customer experience with the app reserved for
   returning users.
3. Shared-package code drops below **~40%** of total Dart lines across the three apps — the apps have
   diverged enough that the workspace is overhead, and they should be split.
4. Indic rendering defects reach **> 2 per quarter** despite golden tests — the text stack needs a
   platform-view escape hatch for Indic-heavy screens.
5. Flutter's release cadence breaks us twice in a year (a major version requiring more than a sprint
   of migration) — that is the vendor-risk signal, and the response is to freeze on an LTS-ish
   version rather than to rewrite.
