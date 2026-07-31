# ADR-0003 — Metadata-driven everything (the Prime Directive), and its exact limits

**Status:** Accepted — 2026-07-29
**Deciders:** Platform architecture + client sponsor
**Related:** ADR-0002 (JSONB substrate), ADR-0004 (workflow), ADR-0006 (search mapping), ADR-0009 (codegen)

> This ADR does not re-argue *whether* the platform is metadata-driven — North Star §2 settles that.
> It records **how far the directive goes, where it deliberately stops, and what it costs.**
> A directive without a stated boundary becomes an excuse to configure the square-root function.

---

## Context

The client's requirement is one sentence: *"no hardcoding, metadata driven."* Read naively that is
either meaningless or infinite. We convert it into an operational test (North Star §2) and ten
registries. The failure this is aimed at is not hypothetical:

> **The four-repo fuel type.** A marketplace adds CNG-retrofit as a fuel option. It requires: a C#
> enum member, a PG `CHECK` constraint migration, a TypeScript union in the web repo, a Dart enum in
> the Android repo, a new localization key in three files, a new filter in the search mapping, and a
> QA cycle across three release trains. Elapsed: **19 days**. Business value of the change: one
> dropdown option. That is the disease.

The second, subtler failure — the one that kills metadata platforms — is the opposite:

> **The configurable universe.** Everything is a rule. `guard_expr` grows a Turing-complete DSL.
> Someone encodes GST arithmetic as 40 registry rows. A production incident's root cause is a
> mis-typed JSONPath in a `effect_json` blob, and the stack trace points at `RuleEvaluator.cs:212`
> for all 300 possible causes. Debuggability goes to zero, tests become fixtures-of-fixtures, and
> the team quietly starts adding `if (categoryKey == "two_wheeler")` again.

Both failures are real. This ADR draws the line between them.

## Decision

### 1. The ten registries are the *complete* set of configurable surfaces

Not nine, not eleven. Every "can we make X configurable?" request must map onto one of the ten
(North Star §2) or it is a code change. New registries require a new ADR. This is the anti-sprawl
rule.

### 2. The PR test, mechanised

North Star §2 states the test. We enforce it with a **Roslyn analyzer pack** (`AutoHub.Analyzers`)
that fails the build, not a checklist someone remembers:

| Rule | Fails on | Fix |
|---|---|---|
| `AH0001` | String literal compared against a state (`state_key ==`, `.Status ==`, `switch` on a state) | Use `state_category` or a `caps_json` capability |
| `AH0002` | `decimal`/`double` literal used in an arithmetic expression producing money | Pricing registry (Registry #4) |
| `AH0003` | Role-name comparison (`role == "Dealer"`, `[Authorize(Roles=...)]`) | Permission check via RBAC registry |
| `AH0004` | Collection initialiser of business strings (`new[]{"Petrol","Diesel"}`) | Taxonomy registry |
| `AH0005` | User-facing string literal reaching a response body or notification payload | Localization + Notification registries |
| `AH0006` | `if` on `category_key` / `intent_key` / `city_key` | Rule set or capability flag |

The equivalent on the frontend is an ESLint plugin (`eslint-plugin-autohub`) with the same six rules
plus `AH0007` — a hardcoded label in JSX.

### 3. What is explicitly **NOT** metadata (the escape hatch, closed)

This list is exhaustive and changing it requires an ADR. These are **technical invariants and
safety properties** — configuring them makes the platform unsafe, not flexible.

| Never configurable | Why |
|---|---|
| Double-entry balance invariant (`Σ DR = Σ CR`) | It is arithmetic, not policy. A registry row that could disable it is a fraud vector (ADR-0007). |
| `allow_negative = false` on escrow account `1000` | Same. A config toggle that lets escrow go negative is an unfunded payout. |
| Money as minor units in `bigint` | Representation, not policy. |
| Idempotency semantics, `Idempotency-Key` handling | Correctness primitive. |
| Outbox atomicity, `event_id` dedupe (ADR-0005) | Correctness primitive. |
| Cryptographic parameters — HMAC algorithm, JWT signing, password KDF, TLS policy | A registry row must never be able to weaken crypto. Rotation is config; *algorithm choice* is a deploy. |
| RBAC **enforcement mechanism** (the scope guard call itself) | Roles and permissions are data; the check being executed is code. |
| Concurrency control — row locks, `sequence_no`, advisory lock keys | Correctness primitive. |
| PII classification enforcement (`platform.pii_registry` drives masking; *whether* masking runs does not) | DPDP Act 2023 §8 obligation. Not a toggle. |
| Audit logging on/off | Regulatory. Audit *retention* is registry data; audit *emission* is not. |
| Tax **arithmetic** (how CGST/SGST/IGST split, how TCS 206C(1F) is computed) | Statutory formulae. The **rates, thresholds, HSN/SAC codes and applicability conditions** ARE registry data (`platform.tax_rules`); the algorithm that consumes them is code, unit-tested against CBIC examples. |

The general shape of the line: **rates, lists, labels, states, routes, thresholds and eligibility →
registry. Algorithms, invariants, and safety properties → code.**

### 4. Registry data is production code and is governed like it

Because a registry row can break production as thoroughly as a deploy:

```
draft change set → validate (JSON Schema + registry-lint referential check)
                 → dry-run simulation against N production entities
                 → diff rendered for the approver
                 → approval (RBAC-scoped; pricing needs Finance role)
                 → apply in one transaction + epoch bump + audit row
                 → rollback = re-apply the previous registry_entity_versions snapshot
```

`platform.registry_change_sets` and `platform.registry_entity_versions` exist for this. **No single
row update path exists in the admin API for governed registries** (Pricing, Workflow, RBAC, Tax) —
only change sets. Non-governed registries (Localization strings, Notification template copy) allow
direct edits, because the blast radius is a wrong word, not a wrong rupee.

### 5. Every artefact carries the registry version that produced it

An audit row, a deal line item, a valuation, an invoice, and a workflow instance all persist the
registry version(s) they were computed under. `deals.deal_line_items.fee_component_version`,
`workflow_instances.workflow_version`, `valuations.model_version`. **Without this, "why was this
seller charged 2.5%?" is unanswerable six months later.** This is the single most important
consequence of going metadata-driven and it is easy to forget.

## Consequences

**Positive**

- The North Star §8 acceptance test becomes true: a new vehicle category with its own attributes,
  form, and commission slab goes live with zero deploys.
- One schema drives web, Android and iOS forms — the sell-flow field ships everywhere at once.
- Business change velocity decouples from release velocity. Pricing experiments in Pune do not queue
  behind a Flutter store review.
- Every state, fee, role and template is *enumerable by query*, which makes compliance answers
  (DPDP, GST audit) a `SELECT`, not an archaeology project.

**Negative — stated honestly**

- **Debuggability drops hard.** "Why did this fail?" no longer has a code answer. Mitigation, and it
  is mandatory: every registry-driven decision emits a structured **decision trace** —
  `{registry, key, version, inputs, matched_row_id, output}` — attached to the request's trace and
  persisted for money decisions. A pricing computation without a trace is an incident waiting to be
  unresolvable.
- **Onboarding is slower.** A new engineer cannot read the code to learn the states. Mitigation: an
  admin "Registry Explorer" that renders workflows as diagrams and fee slabs as tables is a
  **Phase 0 deliverable**, not a nice-to-have.
- **Static typing is weaker at the seams.** `state_key`, `fee_component_key`, `permission_key` are
  `string` at compile time. Mitigation: `code_key` domain type in PG, generated constant classes for
  *seeded* keys only (documentation, never used in a branch), and the analyzers above.
- **Testing needs a registry fixture layer.** Every integration test loads a seeded registry snapshot
  (`infra/seed/registry/*.sql`). Snapshot drift between test seed and production registry is a real
  bug class; a nightly job diffs prod registry structure against the seed and reports.
- **A bad registry edit is a production incident with no rollback deploy.** Hence change sets,
  dry-run, and versioned snapshots. Budget for building that machinery — it is roughly 20% of Phase 0.
- **Performance floor.** Ten registry lookups per request is unacceptable without caching; the cache
  is therefore load-bearing infrastructure with its own failure mode (stale epoch → serving old
  commission). Epoch checks are cheap; skipping them is not an option.

## Alternatives considered

| Alternative | Why rejected |
|---|---|
| **Code + feature flags** | Flags configure *branches that already exist*. They cannot add a vehicle category, a form field, or a workflow state. Solves 5% of the requirement and creates permanent dead branches. |
| **Build-time codegen from a DSL** (schemas in git → generate C#/TS/Dart) | Type-safe and debuggable — genuinely attractive. Rejected because it still requires a **deploy** for every business change, which is precisely the requirement. We keep the idea in a reduced form: generated constants and OpenSearch mappings are emitted *from* the registry (ADR-0006, ADR-0009). |
| **A low-code/BPM platform** (Camunda, Appian, OutSystems) | Owns the runtime, fights the domain model, licenses per-process, and makes the double-entry ledger and auction bid path second-class. We would be building a marketplace inside someone else's opinion. |
| **Hardcode now, extract later** | The North Star's closing line: "Every day of 'we'll make it configurable later' costs a month later." Extraction requires finding every branch — and the branches are exactly what a registry-less codebase makes unfindable. |
| **Fully generic "rules engine for everything"** (one rule table, no ten registries) | The opposite failure described in Context. Ten *typed* registries with real columns beat one untyped rule store; each registry has a schema, an editor, and a lint rule. Genericity is bounded on purpose. |

## Revisit when

1. **The escape hatch list needs a fourteenth entry within a year.** Two exceptions is judgment;
   fourteen means the line is drawn wrong and the directive needs re-scoping.
2. Median incident **MTTR exceeds 90 minutes** with root cause "registry misconfiguration" in more
   than 30% of P1/P2s — the tooling (dry-run, decision trace, explorer) is under-built and needs
   investment before any new registry surface is added.
3. Registry cache warm-up on cold start exceeds **10 seconds**, or p99 request latency attributable
   to registry resolution exceeds **15 ms** — move to a compiled-registry approach (registry snapshot
   → in-memory immutable graph rebuilt on epoch change, rather than per-key lookups).
4. A business stakeholder has not made a **single** unaccompanied registry change in 90 days after
   the admin tooling ships. That means we built configurability for engineers, which is all cost and
   no benefit, and the honest response is to freeze new registry surface and fix the editors.
