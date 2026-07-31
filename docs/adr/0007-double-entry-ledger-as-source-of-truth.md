# ADR-0007 — The double-entry ledger is the source of truth for money

**Status:** Accepted — 2026-07-29
**Deciders:** Platform architecture + Finance
**Related:** ADR-0001 (single transaction), ADR-0003 (what is not configurable), ADR-0005 (outbox)

---

## Context

AutoHub moves other people's money in at least eleven distinct shapes: sale consideration, auction
EMD, buyer premium, security deposits, holdbacks, rental variable charges, lease instalments,
attachment revenue-share cycles, finance/insurance referral fees, refunds, and penalties. Layered on
top are Indian statutory obligations that are **not optional and not symmetric**:

| Obligation | Effect on the money flow |
|---|---|
| **GST** (CGST/SGST intra-state, IGST inter-state) on platform fees | Platform's commission invoice must split tax by place of supply — a Pune dealer and a Bengaluru dealer produce different tax legs on identical commission |
| **e-invoicing (NIC IRP)** above the turnover threshold | An invoice is not legally final until an IRN is stamped; the IRP is routinely down |
| **TCS u/s 206C(1F)** on motor vehicle sale > ₹10 lakh | Collected *on behalf of the seller*, held, and remitted — it is never platform income |
| **TDS u/s 194-O** on e-commerce participant payouts, 194H/194J on commissions | Deducted from the payout, held as a liability, remitted by the 7th |
| **RBI PA/PG directions** — escrow / nodal account | Customer funds sit in an escrow account the platform cannot treat as its own; settlement timelines (T+1) are prescribed |
| **DPDP Act 2023** | Financial records are retained for statutory periods and cannot be erased on request — the ledger is an explicit erasure carve-out |

The failure this decision is aimed at is not exotic. It is the default design:

> **The `amount_paid` column.** `deals.deals` carries `amount_paid`, `amount_refunded`,
> `commission`, `payout_status`. A refund handler retries after a timeout and decrements
> `amount_paid` twice. Nobody notices, because the only check is the same column. Two months later
> Finance reconciles the PA escrow statement against the platform's own report and finds a
> **₹3.2 lakh gap across 1,900 deals**, with no way to attribute it. The forensic work is four weeks
> of SQL archaeology, and the answer is "we don't know".

And the sharper version:

> **The one that ends companies.** Escrow goes negative because a payout was initiated against a
> capture that was later charged back. The platform pays a seller with another buyer's escrowed
> money. That is not a bug, it is a regulatory event.

## Decision

**Every rupee that moves writes a balanced double-entry journal. The ledger is the truth; every
balance shown anywhere is derived from it. Entities do not carry money-state columns.**

### Tables (ERD `payments.*`)

```
payments.ledger_accounts      (id, code, name, type, normal_side, scope_kind,
                               scope_id, currency, allow_negative)
payments.ledger_transactions  (id, journal_template_key, template_version, deal_id,
                               occurred_at, posted_at, narration, correlation_id,
                               reversal_of_id, is_reversal)
payments.ledger_entries       (id, transaction_id, account_id, direction, amount_minor,
                               currency, party_id, deal_line_item_id, seq)
```

Invariants enforced **in the database**, not in a service:

1. `Σ(DR) = Σ(CR)` per `transaction_id` — deferred constraint trigger fired at commit.
2. `amount_minor > 0` always; direction carries the sign. No negative amounts, ever.
3. **No `UPDATE`, no `DELETE`** on `ledger_entries` — revoked at the role level, plus a trigger that
   raises. A correction is a **reversing transaction** with `reversal_of_id` set. Nothing else.
4. Escrow account `1000` has `allow_negative = false`; the balance trigger aborts the transaction.
   Better a failed payout than an unfunded one.
5. Single currency per transaction (INR today); `currency` is on both account and entry so the
   constraint is checkable.

### Chart of accounts is registry data

The full chart is in Domain Model §15.3 — `1000` escrow bank, `1010` PG receivable, `2010` party
payable, `2020` security deposit held, `2025` EMD held, `2035` holdback held, `2040–2044` GST
output, `2055` TCS collected on behalf of seller, `2060/2061` TDS payable, `4000` commission income,
`4001` buyer premium, `5000` PG expense, and so on. Accounts are rows. **Adding a new fee type must
not require a code change** — which means the *posting rules* are registry data too:

```
platform.journal_templates(key, version, entity_trigger, is_active, effective_from)
platform.journal_template_legs(template_key, version, seq, account_code_expr,
                               direction, amount_expr, party_expr, condition_expr)
```

`amount_expr` is a rule-DSL expression over the deal-line context
(`line.gross_minor`, `fee.commission_minor`, `tax.cgst_minor`, `tcs.minor`), evaluated by the same
sandboxed evaluator as workflow guards (ADR-0004). Every posted transaction persists
`journal_template_key` **and** `template_version`, so "why were these six legs written" is
answerable years later (ADR-0003 §5).

### What is deliberately NOT configurable

Per ADR-0003's escape hatch: the balance invariant, `allow_negative`, the immutability of entries,
the minor-unit representation, and the **arithmetic** of GST/TCS/TDS. Rates, thresholds, HSN/SAC
codes and applicability conditions are `platform.tax_rules` data. The formula that consumes them is
code, unit-tested against CBIC worked examples. A registry row must never be able to make
`Σ DR ≠ Σ CR`.

### Balances

`payments.account_balances(account_id, currency, balance_minor, last_entry_seq, updated_at)` is a
trigger-maintained materialisation — the hot path never sums entries. A nightly job recomputes from
`ledger_entries` and raises `ledger.balance_drift` on any mismatch. If the materialisation and the
entries ever disagree, **the entries win** and the materialisation is rebuilt.

### Reconciliation (the part that makes it worth it)

| Daily job | Compares | On break |
|---|---|---|
| PA escrow statement ↔ `1000` | Bank/PA closing balance vs ledger balance | `RECON_BREAK` row; payouts for affected deals park in a registry-driven hold state |
| PG settlement report ↔ `1010` → `1000` | Per-UTR settlement batch | Unmatched UTR → ops queue, never auto-written-off |
| Payout rail UTRs ↔ `2030` | Initiated vs settled vs reversed | Reversal posts a reversing journal automatically |
| GST output `2040–2044` ↔ GSTR-1 draft | Tax legs vs return | Blocks filing sign-off |
| TDS `2060/2061` ↔ Form 26Q draft | Deducted vs reported | Blocks challan generation |

**A capture webhook whose amount differs from the intent amount is never auto-accepted** — it
creates an `AMOUNT_MISMATCH` break and parks the deal (Domain Model §18.2).

## Consequences

**Positive**

- "Where is the money?" is a single query with a provable answer, at any timestamp
  (`... WHERE posted_at <= $t`). Point-in-time balance reconstruction is free.
- Escrow cannot go negative. The database refuses. That is a structural guarantee, not a code review.
- A new fee type — say a "RC transfer assist fee" — is a fee component row + a journal template row.
  Zero code, and the ledger is automatically correct for it.
- Statutory liabilities (TCS, TDS, GST output) sit in **named liability accounts** rather than being
  inferred at filing time. The 7th-of-the-month remittance is a report, not an investigation.
- Audit and DPDP: the ledger is append-only and retained; erasure requests carve it out explicitly
  and legally (`trust.erasure_requests` records the carve-out reason).
- Disputes are answerable. "Show me every rupee associated with deal X" is `WHERE deal_id = $1`
  ordered by `posted_at`, and it reads as a story.

**Negative — stated honestly**

- **Every money feature is 2–4× the code.** Adding "partial refund of a security deposit" means
  thinking about which accounts, which direction, which party scope, and which reversal path. This
  is real, permanent tax on velocity.
- **`ledger_entries` is the fastest-growing table in the system** — roughly 6–12 rows per deal, plus
  reversals. Partitioned monthly by `posted_at`; older partitions moved to cheaper storage; never
  deleted.
- **You cannot fix data with an `UPDATE`.** Ops needs a first-class **reversal tool** with approval,
  narration and audit — build it in Phase 6 or someone will be given raw SQL access, which defeats
  the whole ADR.
- **Developers will fight it.** The instinct to "just add a `paid` boolean to `deals`" is strong and
  will recur every sprint. Any denormalised money field is a **cache with a stated refresh rule**, is
  named `*_cached_minor`, and is rebuilt from the ledger — an analyzer rule enforces the naming.
- **Double-entry is a skill.** Not every engineer has it. Journal templates + a test harness that
  asserts balance and expected account codes for every scenario is how we make it teachable.
- **Correct ≠ intuitive.** A seller's "balance" is `2010 Party payable` minus holdbacks minus TDS —
  three accounts. Surfacing that to a dealer requires a deliberate UX translation layer, and getting
  that translation wrong produces support tickets even when the ledger is perfect.
- **Latency.** A settlement posting is 8–12 rows plus balance triggers in one transaction. It is
  fine at our volume and it will not be at 500 postings/sec (see Revisit).

## Alternatives considered

| Alternative | Why rejected |
|---|---|
| **Balance/amount columns on `deals` and `wallets`** | The default, and the incident in Context. Concurrency-unsafe without careful locking, unauditable, and it cannot represent a rupee that is simultaneously "collected", "owed to seller", "owed to government" and "not ours" — which is exactly what escrow is. |
| **Single-entry transaction log** (`money_movements` with a signed amount) | Better than columns: it has history. Still cannot answer "against which liability" and has no structural balance check, so a missing counterpart leg is undetectable. Half a ledger is worse than none because it feels sufficient. |
| **Accounting only in Tally / Zoho Books, platform tracks payments loosely** | Standard Indian SME pattern and completely wrong here: the platform *holds customer money in escrow*. Reconciliation would be monthly and manual, and the negative-escrow guard would not exist. We still **export** to Tally/Zoho — from the ledger. |
| **TigerBeetle** | Purpose-built, extremely fast, correct by construction. Rejected **for now** on ADR-0001 grounds: it is a separate datastore, so a deal write and a ledger write stop being one transaction and become a saga — reintroducing the exact dual-write problem the monolith exists to avoid. It is our named upgrade path at volume. |
| **Formance / an external ledger service** | Same transactional-boundary objection, plus a network hop on the settlement path and a vendor holding the money truth. |
| **Event sourcing the payments module only** | Attractive (money is naturally event-shaped), but a bespoke event store next to a relational one, and the double-entry ledger already *is* an append-only event log with a balance invariant — the parts of event sourcing that matter here, without projections to rebuild. |

## Revisit when

1. `ledger_entries` exceeds **~50 million rows** or posting throughput exceeds **~500/sec sustained**
   — evaluate TigerBeetle behind an anti-corruption boundary, accepting the saga cost knowingly.
2. Multi-currency becomes real (cross-border dealer imports) — the entry model already carries
   `currency`, but FX revaluation accounts, rate sourcing, and a `9000` FX gain/loss account need
   designing. Do not bolt this on ad hoc.
3. Reconciliation breaks exceed **0.1% of daily transaction value** for a week — this is an
   integration or a template bug, and it is a stop-the-line condition, not a backlog item.
4. Balance-trigger contention shows up as lock waits on `account_balances` for high-traffic accounts
   (`1000`, `2030`) — move to bucketed sub-accounts per shard key with a rollup, rather than
   removing the materialisation.
5. RBI directions change the escrow/nodal model (a live area) — the chart of accounts and the
   `1000 ↔ PA statement` reconciliation are where that change lands, and both are registry data plus
   one job. That containment is a large part of why this design was chosen.
