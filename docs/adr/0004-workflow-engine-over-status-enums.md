# ADR-0004 — A registry-driven workflow engine instead of status enums

**Status:** Accepted — 2026-07-29
**Deciders:** Platform architecture
**Related:** ADR-0002 (JSONB), ADR-0003 (metadata-driven), ADR-0005 (outbox)

---

## Context

Nine aggregates in AutoHub are stateful: `Listing`, `Deal`, `AuctionLot`, `Booking`,
`RentalAgreement`, `LeaseAgreement`, `AttachmentContract`, `LoanApplication`, `InsuranceQuote`,
plus `Inspection` and `KycRecord`. Each has 6–20 states, and the states differ **per intent** — a
C2C sale listing and a dealer auction listing are the same table with different lifecycles.

The failure mode is specific and we have seen its exact shape:

> **The legal-hold ticket.** Compliance asks for a "Legal Hold" state between `LIVE` and payout, for
> vehicles flagged in a court order. In an enum-based system that is: a C# enum member, a PG check
> constraint migration, a `switch` in `ListingService.CanEdit()`, another in `CanPublish()`, another
> in `SearchIndexer.ShouldIndex()`, a TS union, a Dart enum, a status-pill colour map, a
> notification template mapping, and a report filter. **Sixty-one files, eleven of them in two other
> repos, and a QA matrix nobody can enumerate.** Meanwhile the court order has a 48-hour deadline.

And the slow-rot version:

> Someone adds `PENDING_APPROVAL_2` because changing the semantics of `PENDING_APPROVAL` would break
> a report. Eighteen months later the codebase contains `PENDING`, `PENDING_APPROVAL`,
> `PENDING_APPROVAL_2`, `AWAITING_APPROVAL` and `IN_REVIEW`, and no living person can say which is
> which.

North Star §2 bans `if (listing.Status == "PENDING_APPROVAL")` outright. This ADR records the
mechanism that makes the ban survivable.

## Decision

**States, transitions, guards, effects, permissions and SLAs are rows in the Workflow Registry
(Registry #3). Application code never names a state.**

### Schema (Data Model §3.1, ERD `platform.*`)

```
platform.workflow_definitions (id, entity_type, key, version, is_active,
                               effective_from, scope_json)
platform.workflow_states      (id, definition_id, state_key, label_key, category,
                               is_initial, is_terminal, sort_order, ui_token, caps_json)
platform.workflow_transitions (id, definition_id, from_state_key, to_state_key,
                               event_key, guard_expr, effect_json,
                               required_permission, sla_seconds, priority)
platform.workflow_instances   (id, entity_type, entity_id, definition_id, version,
                               state_key, state_category, state_entered_at)
platform.workflow_history     (…, from_state, to_state, event_key, actor_id, reason_code, at)
platform.workflow_timers      (id, instance_id, fire_at, transition_id, claimed_by, claimed_until)
```

Every stateful aggregate carries **exactly five columns**, never an enum:

```sql
workflow_definition_id uuid        NOT NULL,
workflow_version       int         NOT NULL,
state_key              platform.code_key NOT NULL,   -- business data, opaque to code
state_category         text        NOT NULL,          -- projected, closed vocabulary
state_entered_at       timestamptz NOT NULL
```

### The two things code IS allowed to branch on

1. **`state_category`** — a closed technical vocabulary the registry assigns to each state. This is
   the *only* state-ish thing in a `WHERE`, an index predicate, or an `if`:

   ```
   DRAFT · PENDING · ACTIVE · ON_HOLD · TERMINAL_SUCCESS · TERMINAL_FAILURE · TERMINAL_CANCELLED
   ```

2. **Capability flags** in `workflow_states.caps_json` — named booleans, also registry data:

   ```json
   { "allows_edit": false, "publicly_visible": true, "blocks_new_deal": true,
     "permits_payout": false, "permits_bid": true, "counts_as_inventory": true,
     "requires_reason_code": true }
   ```

   Code asks `state.Caps.PermitsPayout`, never `state.Key == "SETTLED"`. Adding a capability is a
   code change (a new named question); adding a *state that answers it differently* is not.

### The engine's contract

```
IWorkflowEngine.Fire(entityType, entityId, eventKey, actor, payload, idempotencyKey)
  → Result<TransitionOutcome>
```

1. Load instance + definition **at the instance's pinned `workflow_version`**.
2. Select candidate transitions matching `(from_state_key, event_key)`, ordered by `priority`.
3. Evaluate `guard_expr` in the sandboxed rule DSL against a typed context
   (entity snapshot, actor, registry values, `now()`). First guard that passes wins.
4. Check `required_permission` through the RBAC registry **with scope**.
5. In **one transaction**: update the aggregate's five columns, insert `workflow_history`, schedule
   or cancel `workflow_timers`, and write every `effect_json` step to `platform.outbox_messages`
   (ADR-0005). Effects are *never* executed inline.
6. Emit `<module>.<aggregate>.state_changed.v1` with `{from, to, category, reason_code}`.

Failure codes are stable and registry-independent: `workflow.transition_not_allowed` (409),
`workflow.guard_failed` (422, with the failing guard's `reason_code`),
`workflow.permission_denied` (403), `workflow.instance_stale` (409, optimistic concurrency),
`workflow.definition_inactive` (503).

### Version pinning — the non-obvious part

An in-flight entity **finishes on the definition version it started on**. `workflow_instances.version`
is immutable. Publishing `LISTING.SALE.C2C@5` affects only entities created after
`effective_from`. Migrating live entities across versions is an explicit, audited
`workflow.migrate_instances` admin operation with a state-mapping table — never implicit.

> Without this, publishing a new version strands every entity whose current state was removed. That
> is the single most common way a workflow engine takes down production.

### SLA and timers

`transitions.sla_seconds` + `workflow_timers` give us "auto-cancel an unpaid deal after 30 minutes",
"auto-close a lot at `effective_end_at`", "escalate an unmoderated listing after 4 hours" — all as
registry data. A single `AUTOHUB_ROLE=worker` dispatcher claims due timers with
`FOR UPDATE SKIP LOCKED` and fires the transition through the same `Fire()` path, so a timer-driven
transition is indistinguishable from a user-driven one in history.

### UI is generated, not written

`StatusPill` (Design System §6) renders `label_key` through Localization and `ui_token` through the
status colour set. `available_actions[]` on every stateful API response is computed as "transitions
whose `from_state_key` matches, whose guard passes, and for which the caller holds
`required_permission`". **The dealer portal's action buttons therefore appear and disappear as a
registry edit, with no frontend change.** `WorkflowTimeline` renders `workflow_history` directly.

## Consequences

**Positive**

- Legal Hold ships as one change set: 1 state row, 2 transition rows, 1 localization key. Zero
  deploys, zero QA matrix outside the workflow's own conformance tests.
- Per-intent lifecycles cost nothing: `LISTING.SALE.C2C@4`, `LISTING.SALE.DEALER@2`,
  `LISTING.RENTAL@3` are three definitions over one table.
- History is universal and free — one `workflow_history` table answers "who moved this deal to
  `PAYOUT_BLOCKED` and why" for every aggregate in the system.
- Permission-per-transition kills the "any dealer can force-close a lot" class of bug, because
  authorization is attached to the transition rather than re-derived in each handler.
- Reports and indexes are stable: they filter on `state_category`, which does not change when a
  business state is renamed.

**Negative — stated honestly**

- **You cannot grep for states.** Answering "what states can a Deal be in?" requires a query or the
  Registry Explorer. This is genuinely worse for a new engineer, and North Star §8 accepts it
  deliberately ("Nobody ever greps the codebase for a status string"). The Explorer is Phase 0.
- **No compile-time exhaustiveness.** A C# `switch` over an enum warns on a missing case; a
  registry cannot. Mitigation — **workflow conformance tests generated from the registry**, run in
  CI against the seeded registry *and* nightly against production registry structure:
  - every non-terminal state has ≥ 1 outgoing transition (no dead ends),
  - every state is reachable from `is_initial`,
  - every `is_terminal` state has `category LIKE 'TERMINAL_%'`,
  - every `guard_expr` parses and references only whitelisted context paths,
  - every `effect_json` step names a registered effect handler and an existing notification event,
  - every `required_permission` exists in the RBAC registry,
  - every `label_key` exists in Localization for every active locale.
- **A bad change set can strand entities.** Mitigated by version pinning, dry-run simulation over N
  production entities, and `is_active` + `effective_from` staging. Not eliminated.
- **Guard expressions are a mini-language.** They will grow. We cap them deliberately: no loops, no
  I/O, no user-defined functions, whitelisted context paths, 5 ms evaluation budget, and a static
  parser in CI. When a guard genuinely needs to call a service, it names a **registered named
  predicate** implemented in C# (`vehicle_has_open_hypothecation`) rather than growing the DSL.
- **Two round trips to know what a user can do.** `available_actions[]` requires evaluating every
  candidate transition's guard. Cached per (definition version, state, permission set) where guards
  are permission-only; evaluated live where guards touch entity data.
- **Debugging is indirection.** Mitigation: every `Fire()` writes a decision trace
  (`candidates[], guard_results[], chosen_transition_id`) to the request trace, and to
  `workflow_history.trace_json` for money-bearing transitions.

## Alternatives considered

| Alternative | Why rejected |
|---|---|
| **C# enums + State pattern classes** | Type-safe and readable — and requires a deploy for every business state change, which is the entire requirement. Also duplicates the state set into TS and Dart (the four-repo problem). |
| **Temporal / Camunda / Elsa** | Real workflow engines, and genuinely better at long-running orchestration. Rejected for the *entity lifecycle* job: they own the state store (a second source of truth alongside `listings.listings`), they cannot participate in our single PG transaction (ADR-0001's whole point), and business users would still not be editing BPMN. We keep Temporal on the table for **cross-service orchestration** if ADR-0001 is ever revisited — a different problem from "what state is this listing in". |
| **Event sourcing the aggregates** | Gives perfect history for free, but replaces one hard problem (state) with three (projections, schema evolution of events, and rebuild time), and North Star §3 defers it explicitly. `workflow_history` gives us the 90% of event sourcing we actually need — the audit trail. |
| **DB `CHECK` constraints / a `statuses` lookup table with FK** | A lookup table stops typos but does nothing about *transitions*, *guards*, *permissions*, *effects* or *SLAs* — the parts that actually cost sixty-one files. Half a solution that feels like a whole one. |
| **Status enum + a separate transition table** | The hybrid trap: the transition table is registry data but the enum still requires a deploy to add a state, so nothing is gained and now there are two sources of truth that can disagree. |

## Revisit when

1. **Guard expressions exceed ~10 named predicates or any single guard exceeds ~200 characters** —
   the DSL is being asked to be a programming language. Response: promote the logic into named C#
   predicates, not a bigger DSL.
2. A workflow legitimately needs **parallel branches, compensation, or human-task queues with
   assignment and delegation** (likely candidates: Insurance claims, RC transfer with a multi-party
   RTO process). That is orchestration, not lifecycle — introduce Temporal for *those* processes only
   and keep `workflow_instances` as the entity-facing projection.
3. `Fire()` p99 exceeds **25 ms** with guard evaluation as the dominant term — compile guard
   expressions to delegates at registry-load time and cache per definition version.
4. More than **5 workflow definitions per entity type** exist purely to express a variant that
   differs in one transition — that means the model needs composition (base + overlay) rather than
   copies, and the registry schema needs a `parent_definition_id`.
