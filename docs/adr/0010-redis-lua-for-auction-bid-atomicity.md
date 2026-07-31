# ADR-0010 — Redis + Lua for auction bid admission; PostgreSQL remains the authority

**Status:** Accepted — 2026-07-29
**Deciders:** Platform architecture
**Related:** ADR-0001 (monolith), ADR-0005 (outbox), ADR-0007 (ledger)

---

## Context

A dealer auction closing at 16:00:00 IST looks like this: 40 live lots, ~2,000 connected dealer
devices, and **80% of all bids arriving in the final 90 seconds** of each lot. On a hot lot, 60–120
bids land inside 10 seconds, many within the same 50 ms window, from 4G connections with 200–400 ms
RTT and clocks that are minutes off.

The invariants that must hold, from Domain Model §8.2 / §17.3:

| # | Invariant |
|---|---|
| A1 | Each accepted bid strictly exceeds the current bid by the registry-driven increment |
| A2 | Bidder is eligible: KYC level ≥ 2, EMD funded, not blacklisted, not the seller |
| A3 | `sequence_no` is strictly monotonic per lot; two equal amounts are impossible |
| A4 | A bid accepted inside the anti-snipe window extends `effective_end_at` — **evaluated on server receipt time** |
| A5 | A retried bid (flaky 4G) returns the original result; it never places a second bid |

The failure modes this ADR is aimed at, all three of which are classics:

> **The lock you no longer own.** Lock acquired with `SET lock:lot:X token NX PX 3000`. The process
> pauses for a 4-second GC. The lock expires. Another node acquires it. The first node wakes and
> calls `DEL lock:lot:X` — **deleting someone else's lock.** Two bid handlers now run concurrently
> and both read the same current bid. Two winners, one lot, one very angry dealer.

> **The three-round-trip lost update.** `GET current_bid` → compare in the app → `SET current_bid`.
> Between the GET and the SET, another bid lands. The lower bid wins. At 400 ms RTT this window is
> enormous.

> **The Redis-as-truth catastrophe.** Bids stored in Redis for speed. A failover to a replica that
> was 2 seconds behind loses the last 14 bids of a lot that closed for ₹6.8 lakh. Redis async
> replication offers no durability guarantee for money, and `WAIT` does not make it a database.

## Decision

**Redis + Lua is the admission-control and hot-state layer. PostgreSQL `SELECT … FOR UPDATE` on
`auction.lots` is the authority. Redis being wrong, stale, or entirely absent makes auctions slower —
never incorrect.**

### The bid path (Domain Model §17.3, annotated with the Redis role)

```
 1  Redis  EVALSHA acquire_lock.lua   SET lock:lot:{id} {token} NX PX 3000
           fail → 2 retries @ 20 ms jitter → 409 auction.bid.contended
 2  Redis  EVALSHA bid_precheck.lua   read cached {current_bid, effective_end_at,
                                       increment_rule, eligibility_bitset}
           reject obviously-losing bids HERE  ← ~95% of rejects, zero DB load
 3  BEGIN
 4  SELECT … FROM auction.lots WHERE id = $1 FOR UPDATE       ← THE AUTHORITY
 5  re-validate A1..A4 against the LOCKED row (Redis may be stale)
 6  INSERT INTO auction.bids (…, sequence_no = lot.bid_count + 1)
 7  UPDATE auction.lots SET current_bid_minor, current_bid_id, current_winner_party_id,
        bid_count = bid_count + 1,
        effective_end_at = CASE WHEN now() >= effective_end_at - window
                                 AND extension_count < max
                            THEN now() + extend_by ELSE effective_end_at END,
        extension_count = …
 8  INSERT INTO platform.outbox_messages (auction.lot.bid_placed.v1, partition_key = lot_id)
 9  COMMIT
10  Redis  EVALSHA writethrough_release.lua   ← updates lot state AND releases the lock
                                                only if token still matches, in ONE atomic script
11  WebSocket fan-out from the outbox consumer, ordered by sequence_no
```

### Why Lua specifically, in three places

Each script exists because the operation is **read-decide-write and must be one atomic step**. This
is not a performance optimisation; it is a correctness requirement that Redis's single-threaded
execution of a script provides and that a client round-trip cannot.

| Script | Why it cannot be client-side |
|---|---|
| `lock_release_cas.lua` | `if redis.call('GET',KEYS[1]) == ARGV[1] then return redis.call('DEL',KEYS[1]) end` — the GET-compare-DEL must be atomic, or you get the "lock you no longer own" failure above. This is the canonical reason Lua exists in Redis. |
| `bid_precheck.lua` | Reads lot state + bidder eligibility + per-bidder rate window and returns a single verdict. Client-side this is 4 round trips at 400 ms RTT, and the state can change between them — producing rejections the user cannot understand. |
| `writethrough_release.lua` | Writes the post-commit lot snapshot **and** releases the lock, conditional on the token. Two commands means a window where the state is updated but the lock is orphaned (or vice versa). |

Operational rules for the scripts:

- Loaded with `SCRIPT LOAD` at startup; SHAs held in memory; **every call falls back to `EVAL` on
  `NOSCRIPT`** and re-caches the SHA. This is not theoretical — a Redis restart or a failover to a
  replica with an empty script cache produces `NOSCRIPT` for every in-flight request, and a client
  without the fallback takes a full outage.
- Scripts are **O(1)**, bounded, no loops over unbounded collections, no `KEYS *`, no `SCAN`.
  Redis is single-threaded: a slow script stalls *every* other command on that instance.
- All keys for one lot share a hash tag — `lot:{lotId}:state`, `lot:{lotId}:lock`,
  `lot:{lotId}:rate` — so the script is cluster-safe (all `KEYS` in one slot). Cross-lot scripts are
  forbidden.
- Redis 7 replicates **effects**, not the script, so a non-deterministic script cannot diverge a
  replica. We still forbid `TIME`/`RANDOMKEY` inside scripts and pass the server timestamp in as an
  argument, because the *decision* clock must be the application server's, consistently.
- **The auction workload gets its own Redis instance**, not a shared one. A slow script or a memory
  spike must not stall session lookups and rate limiting for the whole platform.

### What Redis is explicitly NOT allowed to be

- **Not the bid ledger.** Bids exist when they are in `auction.bids`, committed.
- **Not the idempotency authority.** A5 is enforced by a `UNIQUE (lot_id, idempotency_key)` index in
  PostgreSQL. Redis `SETNX` on the same key is a fast-path reject only.
- **Not the clock.** `effective_end_at` extension uses the DB's `now()` inside the locked
  transaction. Client timestamps are recorded for forensics and never used for A4.
- **Not the money.** EMD, deposits and settlement go through the ledger (ADR-0007).

### Proxy bids

Resolved **inside the same PG lock**, after the manual bid is inserted: outstanding proxies replay in
`(amount DESC, sequence_no ASC)` order and may insert additional `AUTO_INCREMENT` bids in the same
transaction. Doing this outside the lock is the classic bug that produces a proxy bid dated after the
lot closed.

### If Redis is down

Steps 1, 2 and 10 are skipped. Step 4's row lock still guarantees correctness. Consequences: every
bid — including the 95% that would have been cheaply rejected — reaches PostgreSQL, so lot-level
throughput drops and p99 rises. A circuit breaker disables the Redis path after 5 consecutive
failures and re-probes every 10 s. **Auctions get slower, not wrong**, and a banner tells bidders
that responses may lag.

## Consequences

**Positive**

- ~95% of losing bids are rejected in ~1 ms without touching PostgreSQL. On a hot lot this is the
  difference between 12 concurrent DB row-lock waiters and 200.
- The lock CAS removes the "two winners" class of bug structurally.
- PG connection pool is protected during the snipe spike, which protects the *rest of the platform*
  (ADR-0001's single-process blast radius).
- Live bid state for WebSocket fan-out is read from Redis, so 2,000 subscribers do not each generate
  a PG read.
- Correctness is not coupled to Redis durability, so we can run Redis with aggressive
  memory settings and no AOF fsync-per-write.

**Negative — stated honestly**

- **Lua is a second language in the codebase**, with no static typing, no debugger, and a testing
  story that requires a real Redis. Mitigation: scripts live in
  `AutoHub.Modules.Auction/Infrastructure/Scripts/*.lua`, are ≤ 40 lines each, and every one has an
  integration test against a Testcontainers Redis covering the contended path. **No new Lua script
  may be added without an ADR amendment** — this is a deliberately small, closed set.
- **Two sources of state means they can diverge.** A crash between step 9 (COMMIT) and step 10
  (write-through) leaves Redis stale and the lock held until TTL. Mitigation: step 5 always
  re-validates against PG, so a stale cache produces at worst an extra DB round trip; and a
  reconciler refreshes lot state from PG every 2 s for lots in their final 5 minutes.
- **Redis single-threadedness is a shared fate.** One slow script degrades everything on the
  instance. Hence the dedicated instance and the O(1) rule.
- **`EVALSHA`/`NOSCRIPT` after failover** is a real production surprise if the fallback is missing.
  It is a required part of the client wrapper, not an optimisation.
- **The 3-second lock TTL is a guess.** Too short and a slow transaction loses its lock mid-flight
  (harmless — PG still serialises — but produces spurious `bid.contended`); too long and a crashed
  node blocks a lot for 3 seconds during the closing spike. It is registry-tunable
  (`auction.lock.ttl_ms`) and monitored.
- **Temptation.** The pressure to "just keep bids in Redis, it's so much faster" will recur every
  time the auction is slow. The answer is permanently no, and this ADR is the artefact to point at.

## Alternatives considered

| Alternative | Why rejected |
|---|---|
| **PostgreSQL only — `SELECT … FOR UPDATE` with no Redis** | Correct, and our fallback. Rejected as the *primary* because on a hot lot every rejected bid still costs a connection, a row lock wait, and a WAL-adjacent round trip; at 120 bids in 10 s with 200 retrying clients this saturates the pool that the rest of the monolith shares. Redis sheds the load that has no chance of winning. |
| **PostgreSQL advisory locks (`pg_advisory_xact_lock`)** | We effectively get this from the row lock already. As a *replacement* for Redis it does nothing about the connection-pool pressure, which is the actual problem. |
| **Redlock across N independent Redis nodes** | Unnecessary and unsound for our use. Unnecessary because PG is the authority — we do not need a distributed lock to be *safe*, only to be *cheap*. Unsound because Redlock's safety argument depends on bounded clock drift and GC pauses (Kleppmann's critique), which is exactly the environment we cannot assume. |
| **`MULTI/EXEC` + `WATCH`** | Optimistic; under 120-bids-per-10-seconds contention the retry storm is worse than a lock, and the retry logic lives in the client where it can be got wrong per call site. Lua puts the whole decision in one server-side step. |
| **Redis Functions (Redis 7)** | Nicer packaging than `SCRIPT LOAD` — a real module registry with named functions. Rejected for portability: managed offerings and Valkey/ElastiCache-class services have inconsistent support, and `EVALSHA` works everywhere. Low-cost to switch later; named as a revisit item. |
| **Redis Streams as the bid intake queue** | Would serialise bids per lot beautifully. Rejected because it makes bid acceptance asynchronous — the bidder does not learn whether they won the increment until a consumer processes the stream. In an auction UI, "your bid is being processed" is not an acceptable answer at T-3 seconds. |
| **A dedicated in-memory auction service with an actor per lot** | The textbook answer (Akka/Orleans-style), and genuinely the right shape at 10× our volume. Rejected now: it reintroduces the dual-write problem with PG and the ledger (ADR-0001), needs its own durability and failover story, and is a large amount of machinery before we have run a single auction. Named as the primary revisit path. |

## Revisit when

1. A single lot sustains **> 50 bids/second**, or auction-window PG row-lock wait p99 exceeds
   **250 ms** — move to an actor-per-lot model (Orleans grains or a partitioned in-process actor
   keyed by lot, with PG persistence per accepted bid) rather than making Lua smarter.
2. `auction.bid.contended` exceeds **1% of bid attempts** — the lock TTL or retry policy is wrong;
   tune before re-architecting.
3. The Lua script set would grow beyond **five scripts**, or any script exceeds ~60 lines — the
   admission logic is migrating into Redis, which is the failure mode this ADR exists to prevent.
4. We adopt Redis Cluster (multi-slot) for other workloads — verify hash tags on every auction key
   before the migration; a cross-slot script fails at runtime, not at deploy.
5. Redis Functions become uniformly available on our managed offering and the script-cache/`NOSCRIPT`
   handling proves to be a recurring incident source — migrate the same three routines to Functions.
