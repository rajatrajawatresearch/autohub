# ADR-0005 — Transactional outbox for every domain event

**Status:** Accepted — 2026-07-29
**Deciders:** Platform architecture
**Related:** ADR-0001 (modular monolith), ADR-0004 (workflow effects), ADR-0007 (ledger)

---

## Context

North Star §6 states it as a quality floor: *"Every state change emits a domain event through the
outbox. No fire-and-forget side effects."* This ADR records why that is non-negotiable and what it
costs.

The dual-write problem in our exact domain:

```
BEGIN;
  UPDATE auction.lots SET state_key='CLOSED', winning_bid_id=…;
  INSERT INTO deals.deals …;
COMMIT;                                   ← succeeded
_bus.Publish(new LotWon(...));            ← process killed here by a rolling deploy
```

The lot is closed, the deal exists, and the winner is never notified, the EMD is never converted,
and the seller is never paid. There is no retry, because the intent to publish existed only in a
dead process's memory. The inverse is equally bad:

```
_bus.Publish(new PaymentCaptured(...));   ← succeeded, consumers already crediting the seller
COMMIT;                                   ← deadlock, rolled back
```

Now the ledger says a payment was captured that the payments table has never heard of.

Concrete incidents this pattern is aimed at, both of which we have seen in production systems:

> **The lost payout.** A `deal.settled` event was published after commit; the broker connection had
> silently gone stale and the client's internal buffer dropped it. Discovered nine days later by a
> seller's support ticket. No log line, no dead letter, nothing to replay — the event never existed
> anywhere durable.

> **The double-credited refund.** A retried HTTP call re-invoked a handler with no dedupe key. The
> ledger got two `2070 Refund payable` credits. Because a ledger entry cannot be updated (ADR-0007),
> unwinding it required a manual reversing journal and a written explanation to Finance.

## Decision

**Every domain event is written to `platform.outbox_messages` inside the same transaction as the
state change that produced it. Nothing is published from application code. Ever.**

### Table (ERD `platform.outbox_messages`)

```sql
CREATE TABLE platform.outbox_messages (
    id             uuid NOT NULL DEFAULT platform.uuid_generate_v7(),
    occurred_at    timestamptz NOT NULL DEFAULT now(),
    aggregate_type platform.code_key NOT NULL,
    aggregate_id   uuid NOT NULL,
    event_type     platform.code_key NOT NULL,     -- 'auction.lot.bid_placed'
    event_version  smallint NOT NULL DEFAULT 1,
    payload        platform.jsonb_object NOT NULL,
    headers        jsonb NOT NULL DEFAULT '{}',
    partition_key  text NOT NULL,                  -- ordering scope; usually aggregate_id::text
    correlation_id uuid, causation_id uuid, trace_id text, actor_id uuid,
    published_at   timestamptz, attempts smallint NOT NULL DEFAULT 0,
    next_attempt_at timestamptz NOT NULL DEFAULT now(),
    locked_by text, locked_until timestamptz, last_error text, dead_lettered_at timestamptz,
    CONSTRAINT pk_outbox_messages PRIMARY KEY (id, occurred_at)
) PARTITION BY RANGE (occurred_at);

CREATE INDEX ix_outbox__pending ON platform.outbox_messages (next_attempt_at, id)
    WHERE published_at IS NULL AND dead_lettered_at IS NULL;   -- the ONLY hot-path index
```

Event naming and envelope follow Domain Model §14.1:
`autohub.<module>.<aggregate>.<past_tense>.v<N>`, with `event_id`, `occurred_at`, `aggregate_version`,
`partition_key`, `correlation_id`, `causation_id`, `actor`.

### Dispatch

- N dispatcher instances (`AUTOHUB_ROLE=dispatcher`) poll every **200 ms**, claiming with
  `FOR UPDATE SKIP LOCKED` + a `locked_until` lease, so a dispatcher that dies mid-batch releases
  its claim by lease expiry rather than by a heartbeat protocol.
- **Ordering is guaranteed per `partition_key` only.** A partition's messages are processed by
  exactly one dispatcher at a time, in `(occurred_at, id)` order — `uuid v7` makes `id` a tiebreaker
  that agrees with time. There is **no global ordering** and consumers must not assume one.
- Retry: exponential backoff with full jitter, `attempts` capped (registry-driven, default 12,
  ≈ 6 hours), then `dead_lettered_at` is stamped and an ops alert fires. A dead-lettered message is
  never silently dropped.
- A `LISTEN/NOTIFY` nudge on commit wakes the dispatcher early, so the 200 ms poll is a **floor for
  the worst case, not the typical latency** (typical p50 ≈ 5–15 ms). Polling remains the correctness
  mechanism; NOTIFY is pure latency optimisation and is allowed to be lossy.
- Delivery is **at-least-once**. Every consumer maintains an inbox:
  ```sql
  platform.consumed_events (consumer_key text, event_id uuid, consumed_at timestamptz,
                            PRIMARY KEY (consumer_key, event_id))
  ```
  Insert-on-conflict-do-nothing inside the consumer's own transaction. A consumer without an inbox
  row is a bug the architecture test catches.

### Rules that keep it honest

1. **No `IBus.Publish` in application code.** Handlers call `_outbox.Add(evt)`; the
   `SaveChangesInterceptor` writes it. An analyzer (`AH0010`) fails the build on a direct publish.
2. **Payloads carry IDs and immutable facts, never mutable projections.** A consumer needing current
   state calls the owning module's contract. This is what stops the outbox becoming a replication
   log with stale reads baked in.
3. **Effects of a workflow transition (ADR-0004) are outbox rows, not inline calls.** Sending an SMS
   inside the transaction that closes an auction lot means either an SMS sent for a rolled-back
   close, or a lock held across an HTTP call to a Delhi SMS gateway.
4. **Version bumps are additive-only.** A breaking payload change is `vN+1` published *in parallel*
   with `vN` for one release cycle.
5. **Retention by partition detach, not `DELETE`.** Published rows older than 30 days: the monthly
   partition is detached and archived to S3. `DELETE FROM outbox` on a hot table is how you get a
   bloat-driven autovacuum storm during a peak auction.

## Consequences

**Positive**

- Atomicity is real. If the lot closed, the `lot.won` event exists. Full stop.
- Free replay. A consumer bug is fixed by clearing its `consumed_events` rows for a window and
  resetting `published_at` — the events are still there.
- Free audit-adjacent trail. `correlation_id` + `causation_id` reconstruct "this payout happened
  because of this bid" across twenty modules.
- Migration to RabbitMQ/MassTransit (North Star §3) is a dispatcher swap. Handler code, ordering
  semantics and idempotency requirements are unchanged — which is the entire point of paying this
  cost now rather than later.
- Integration outages become latency, not loss. When the SMS provider is down the message waits in
  the outbox with backoff instead of vanishing.

**Negative — stated honestly**

- **Write amplification on the hot path.** Bid placement writes `auction.bids` + `auction.lots` +
  one outbox row per bid. At a snipe spike that is 3 writes × contention. Mitigation: the outbox row
  is narrow (payload is IDs + amounts, not a listing snapshot), the table is `RANGE`-partitioned by
  `occurred_at` so inserts hit a small hot partition, and the only index on the write path is the
  partial `ix_outbox__pending`.
- **Every consumer needs idempotency.** This is real work in every module, repeatedly. There is no
  way around it; at-least-once is the honest delivery guarantee and "exactly once" is a marketing term.
- **Latency floor.** Anything downstream of an event is asynchronous by construction. A user who
  publishes a listing and immediately searches for it will not find it (see ADR-0006's read-your-writes
  overlay). Product must be told; "it'll be instant" is a promise the architecture does not make.
- **Ordering is per-partition only.** `deal.priced` and `payment.captured` have different partition
  keys and can arrive out of order. Consumers must be written to tolerate it (Domain Model §14.3
  enumerates the accepted hazards) — this is the single most common source of subtle bugs in this
  pattern.
- **Poison messages block their partition.** A handler that always throws on event #5 for lot X
  stalls every later event for lot X. Mitigation: the retry cap + dead-letter *unblocks* the
  partition after ≈ 6 hours, deliberately choosing "one event lost to a DLQ with an alert" over
  "a lot permanently frozen".
- **The outbox is a queue in your OLTP database.** It competes for WAL, buffer cache and autovacuum
  with the ledger. Partitioning and detach-based retention are mandatory, not optional tuning.
- **Debugging requires a tool.** An admin outbox viewer (filter by aggregate, event type, dead
  letters; retry one; retry a batch) is a Phase 0 deliverable.

## Alternatives considered

| Alternative | Why rejected |
|---|---|
| **Publish after commit** (`IBus.Publish` in the handler) | The default, and the exact bug in Context. It works 99.9% of the time, which is precisely what makes it dangerous — the failures are rare, silent, and land on money. |
| **Two-phase commit / XA between PG and the broker** | Blocking protocol, in-doubt transactions that require manual DBA resolution, and vanishing support across managed brokers. Trades a rare loss for a rare hang, which is worse. |
| **CDC / Debezium off the WAL** | Genuinely good, and where we go at high volume. Rejected *now* because: it needs Kafka + Connect + Zookeeper-equivalent for a system that currently has one process; a stalled replication slot silently fills the PG data disk until the primary goes read-only (a named, brutal failure mode); and the "outbox table read by Debezium" hybrid still requires the outbox table — so we build the table first and can bolt Debezium on later without touching handler code. |
| **PG as the broker via `pgmq` / `LISTEN`-only** | `LISTEN/NOTIFY` is at-most-once and drops payloads > 8000 bytes and on reconnect — unusable as the durability mechanism, useful only as the latency nudge we already use it for. `pgmq` is a fine queue but does not solve the atomic-with-domain-write requirement any better than our own table, while adding an extension dependency. |
| **Event sourcing** (the event log *is* the state) | Deferred by North Star §3. Solves dual-write by eliminating one write, at the cost of projections, event schema evolution and rebuild time. The outbox gives us reliable eventing without changing how aggregates are stored. |
| **Idempotent consumers + a scheduled reconciliation job instead of an outbox** | "We'll detect and fix the drift nightly." That is a compensating control for a correctness defect, and it means a seller can be unpaid for up to 24 hours by design. |

## Revisit when

1. Sustained outbox throughput exceeds **~2,000 events/sec** or dispatcher lag p99 exceeds **5 s** —
   move to Debezium/Kafka or MassTransit with the outbox table retained as the write side.
2. The outbox table's share of total WAL exceeds **25%** — shorten retention, narrow payloads, or
   move dispatch to a logical replica.
3. Dead-letter rate exceeds **0.1%** of events for a rolling week — the retry policy or a specific
   consumer is broken; this is an alert threshold, not a philosophical revisit.
4. A consumer legitimately requires **cross-aggregate total ordering** (e.g. a regulatory event log).
   Then that consumer gets its own strictly-ordered projection keyed on a global sequence, rather
   than the outbox changing its ordering guarantee for everyone.
5. ADR-0001 is revisited and a module is extracted — at that point the dispatcher publishes to
   MassTransit for the extracted module's events and stays in-process for the rest. The outbox rows
   do not change.
