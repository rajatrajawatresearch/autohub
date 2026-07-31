# 10 — Mobile Architecture

> The anchor for `mobile/`. Derives from [`00-north-star.md`](00-north-star.md) and consumes
> [`contracts/registry-api.md`](contracts/registry-api.md) — the same seam the three web portals
> were built against. Nothing here re-specifies a decision those two already made.

---

## 1. The point of building this at all

A second client is where a metadata-driven architecture either proves itself or is revealed as
decoration.

The claim in the north star is specific: *adding a field to the sell flow ships to web, Android and
iOS at once, from a database row.* Three web portals sharing a TypeScript package do not test that
claim — they share code, so of course they agree. A Flutter app written in a different language,
compiled ahead of time, shipped through an app store, is the actual test.

So the success criterion for this workspace is not "the app works". It is:

> **An operations user adds `battery_health_pct` to the EV category in the admin console, and the
> field appears in the Flutter sell flow on the next launch, with its validation, its unit and its
> conditional visibility — with nothing rebuilt and nothing submitted to an app store.**

If that is not true, the mobile app is a second codebase pretending to be a client, and every
registry in the platform is decoration.

---

## 2. Three apps, one core

The PRD asks for a customer app, a dealer app and a broker/attachment-partner app. They are three
binaries because their users are three people with three jobs — a buyer browses on a phone in the
evening, a dealer bids from a showroom floor, a fleet operator reconciles attachments monthly — and
one app trying to be all three is an app with a role-switcher on the login screen.

They are not three codebases.

```
mobile/
├── packages/
│   ├── autohub_core/     transport, registries, auth, storage, offline queue
│   ├── autohub_ui/       the Registry design language, as Flutter widgets
│   └── autohub_forms/    the Form Schema Registry's Flutter renderer
└── apps/
    ├── customer/         buy · sell · value · book · track
    ├── dealer/           inventory · auction floor · leads · payouts
    └── partner/          attachment supply · contracts · monthly settlement
```

`autohub_core` is the only package that knows a network exists. `autohub_ui` is the only package
that knows what a colour is. `autohub_forms` is the only package that knows what a form is. An app
is composition and navigation, and very little else.

---

## 3. What mobile must not re-derive

Everything below already has an answer on the server. The app asks; it does not decide.

| Question | Where the answer comes from |
|---|---|
| What fields does the sell form have? | `GET /platform/forms/sell_vehicle` |
| Is this listing live, and what can I do to it? | `GET /listings/{id}` → `workflow.availableActions` |
| What are the fuel types for this category? | `GET /platform/taxonomy/attributes?category=…` |
| What will this cost me? | `POST /platform/pricing/quote` |
| Is Lease available to this user in this city? | `GET /platform/features` |
| What does this button say in Marathi? | `GET /platform/i18n/mr-IN` |
| Which cities can I pick? | `GET /platform/geo/cities` |

**No enum in Dart mirrors a registry.** A `enum FuelType { petrol, diesel, cng, ev }` in this
workspace is the same bug as one in C#, with a worse blast radius: a web deploy fixes a stale web
enum in minutes, and a stale mobile enum sits on handsets until users update.

The one place Dart enums are correct is where the *contract* is closed and versioned — HTTP methods,
the six workflow state categories, the listing intents. Those change with the contract, not with a
database row, and a compile error when they change is the desired outcome.

---

## 4. Offline is a first-class state, not an error

This is the axis on which mobile genuinely differs from web, and the reason a responsive web app
would not have been enough.

- **The inspector works in a basement.** A 200-point checklist is filled in a parking structure with
  no signal, over twenty minutes, with photographs. Losing it is losing an hour of someone's day and
  a customer's appointment.
- **The dealer's showroom has patchy wifi.** A bid placed at the edge of coverage must either land
  exactly once or not at all — never twice, never silently lost.
- **The seller is on a train.** A half-finished sell form must survive the tunnel.

The design that follows:

| Concern | Decision |
|---|---|
| Registry cache | Persisted to disk with its `X-Registry-Version`. The app opens with the last known taxonomy, forms and strings, and revalidates in the background. A cold launch on a dead network still renders a sell form. |
| Draft state | Every schema form autosaves to local storage on each field change, keyed by form key + entity id. Recovered on relaunch without asking. |
| Writes | Queued with their `Idempotency-Key` **generated at the moment the user acted**, not at send time. A retry after a tunnel is the same key, so the server deduplicates it rather than creating a second listing. |
| Bids | **Never queued.** A bid that lands ninety seconds late is a bid on a lot that has closed, at a price that has moved. If the network is down, the bid fails loudly and immediately — see §6. |
| Media | Uploaded out of band with resumable multipart, so a 40-photo inspection survives the app being backgrounded. |
| Conflict | Last-writer-wins is wrong for anything with money in it. A queued write that fails on a `409` surfaces the server's current state and asks; it does not retry into a conflict. |

---

## 5. Stack

| Layer | Choice | Why |
|---|---|---|
| Framework | **Flutter 3.x**, Dart 3 | PRD's choice. One codebase, two platforms, ahead-of-time compiled — and a genuinely different client from the web stack, which is what makes it a real test of the registries |
| State | **Riverpod** | Compile-safe dependency graph, no `BuildContext` gymnastics, and providers are trivially overridable in tests — which matters because the interesting logic here is caching and retry, not widgets |
| Navigation | **go_router** | Declarative, deep-linkable. A push notification about an outbid lot must open that lot, not the home screen |
| HTTP | **dio** + interceptors | Mirrors `packages/api`: one place for auth refresh, idempotency keys, `Problem Details` parsing and the offline queue |
| Models | **freezed** + `json_serializable` | Immutable, exhaustively-matched unions. Generated from the same contract the TypeScript schemas describe |
| Local store | **drift** (SQLite) for the registry cache and outbox, `flutter_secure_storage` for tokens | Tokens in the keychain, not in shared preferences. The registry cache is queryable, which the taxonomy tree needs |
| Media | **camera** + `image_picker`, resumable upload | The inspection checklist is the demanding case, not the sell flow |

**Deliberately not used:** a code-generation bridge from the OpenAPI document. It is the right
long-term answer and it is blocked on the same thing `D2` in the decisions log is blocked on — the
API's OpenAPI document. Until then the models are hand-written against
`contracts/registry-api.md`, and the same Phase 2 exit criterion applies: generated or deleted.

---

## 6. The two hard screens

Everything else in these apps is a list, a form or a detail page. These two are not.

### 6.1 The sell flow — the registry's proof

One widget, `SchemaForm`, renders whatever `GET /platform/forms/sell_vehicle` returns. It has no
knowledge of vehicles. Its control registry maps a control name to a widget, and an unknown control
renders a visible placeholder rather than crashing — a bad registry row must never take down the
one screen that creates supply.

The Dart implementation must match `packages/forms` **behaviourally**, not structurally:

- the same validation rule vocabulary, producing the same machine codes
- the same `visibleWhen` JsonLogic evaluation against the form's own values
- hidden fields excluded from validation *and* from the submitted payload
- server `422` field errors applied back onto the right fields

A payload that passes on the phone must pass on the server. Two validators that disagree are worse
than one validator, because the disagreement is only discovered by users.

### 6.2 The auction floor — where correctness costs money

The dealer app's bidding screen is the only place in the product where being slightly wrong is
expensive and irreversible.

- **The socket is read-only.** Bids go over HTTP with an `Idempotency-Key`. A dropped connection can
  then neither lose a bid nor place it twice, which a socket-delivered bid cannot promise.
- **`seenBidSeq` travels with every bid.** It is the highest sequence this screen has actually
  rendered. A bid composed against a stale figure is rejected server-side as an outbid race, and the
  app refreshes the truth rather than guessing the next increment locally.
- **The server clock is authoritative.** The countdown renders against an offset measured at connect
  time, never against the handset clock — a phone two minutes fast would show a lot as closed while
  it is still taking bids.
- **A bid is confirmed before it is sent.** The amount is restated in a sheet the dealer must read.
  A mis-tap on a scrolling wall of countdowns is an expensive way to buy a truck.
- **No optimistic UI on the bid figure.** Showing a bid as leading before the server agrees is
  showing a dealer something that may be false about money.

---

## 7. Design

The Registry language from [`01-design-system.md`](01-design-system.md), unchanged in intent and
adapted where a phone demands it.

- **Tokens are ported, not reinvented.** The same hex values, the same three faces (Archivo,
  Instrument Sans, JetBrains Mono), the same near-square geometry, the same rationed yellow.
- **`PlateBlock` is still the signature**, and on mobile it earns more: the plate is what a user
  matches against the car in front of them, and they are frequently standing next to it.
- **Density is `comfortable` on customer, `compact` on dealer and partner**, exactly as on web.
- **Touch targets ≥ 44dp**, and the sell flow must be operable one-handed — a seller photographs
  their car holding the phone in one hand.
- **Dark mode at parity.** Dealers bid at night; inspectors work in basements.
- **Motion:** the odometer roll on a valuation reveal, and nothing else. `MediaQuery.disableAnimations`
  is respected.

The one place mobile diverges: **sunlight**. An inspector fills a checklist outdoors at midday, so
the inspection surfaces raise contrast beyond AA and never rely on the neutral ramp's lighter end
for anything load-bearing.

---

## 8. What is in scope now, and what is not

| Now | Later, and why |
|---|---|
| `autohub_core`, `autohub_ui`, `autohub_forms` | — |
| Customer app: valuation hero, search, listing detail, schema-driven sell flow, OTP auth | — |
| Dealer app: inventory, auction floor with live bidding | — |
| Partner app: attachment supply and contracts | — |
| Registry cache, offline drafts, idempotent write queue | — |
| Inspector app | Its own binary and its own offline model — a 200-point checklist with 40 photos in a basement is a different product from browsing listings, and folding it into the dealer app would compromise both |
| Push notifications | Needs Firebase project credentials, which is a client decision, not an engineering one |
| Biometric unlock, deep links from notifications | Depends on the above |

---

## 9. How this gets verified

The same standard as the rest of the repository: it compiles clean, its tests pass, and the claim in
§1 is checked rather than asserted.

```
cd mobile && dart analyze          # zero issues; warnings are errors here as they are in C#
cd mobile && ./tool/test_all.sh    # all six suites, in dependency order
```

`flutter test` at the workspace root does **not** work, and this document said it did until someone
tried it. Flutter has no workspace-wide runner: at the root it looks for `mobile/test/`, which does
not and should not exist. `tool/test_all.sh` runs the six suites and exits non-zero if any of them
fails, which is the thing CI actually needs.

The tests that matter are the ones that would catch the architecture failing:

- the `SchemaForm` renderer against the **real** `sell_vehicle` document, asserting that the
  EV-conditional field appears when `fuel_type` is `ev` and is absent from the payload otherwise
- the money formatter against lakh/crore boundaries — `₹94,500`, `₹8.4L`, `₹1.2Cr`
- `RegistrationNumber` parsing, ported with the same cases as the C# suite including the BH series
- the bid client refusing to queue offline, and carrying `seenBidSeq`
- the registry cache serving a form from disk with the network unreachable
