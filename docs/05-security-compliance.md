# 05 — Security & Compliance

> Derives from `00-north-star.md` (stack, prime directive, quality floor) and `01-design-system.md`
> (voice, error copy). Consumes `03-data-model-erd.md` (schemas, `platform.pii_registry`,
> `platform.audit_log`, retention) and `04-api-contract.md` (§4 auth, §5 authz, §6 idempotency,
> §8 rate limits, §13 webhooks).
>
> **The rule this document does not break:** a control that a business or compliance person could
> plausibly want to change — a threshold, an SLA, a retention period, a step-up policy, a consent
> purpose, a tax rate — is a **registry row**, not a constant. Security posture is configuration
> under four-eyes control, not a redeploy.

---

## 0. What this document owns, and what it does not

| Owns | Lives elsewhere |
|---|---|
| STRIDE threat model for the five highest-value flows | Fraud scoring, device graph, case management → `16` Part A |
| AuthN/AuthZ **implementation** detail and hardening beyond the wire contract | The wire contract itself → `04` §4–§5 |
| Cryptography, key hierarchy, rotation, column encryption, tokenisation | Column-by-column DDL → `03` §4 |
| DPDP Act 2023, RBI, IRDAI, GST/e-invoicing, TCS/TDS, TRAI DLT, RTO, CERT-In mapping | Money mechanics, escrow structures, invoice generation → `14` |
| Audit-log integrity, readership, retention | Audit search UI → `16` D4 |
| Application security controls (IDOR, SSRF, upload, supply chain) | Rate-limit tiers → `04` §8; four-eyes matrix → `16` D6 |
| Incident response for **security** incidents; security testing cadence | Availability/DR failover → `16` D7 |

Three facts from the anchors are assumed and never re-argued: money is `bigint` paise, every state is
a `platform.workflow_states` row, and every write carries `Idempotency-Key`.

---

## 1. The security model in one page

AutoHub's real risk is not a zero-day. It is **an authenticated, legitimate user reaching an object
that is not theirs**, or **a rupee leaving escrow one transition too early**. Everything below is
sized to those two.

```
                    ┌──────────────────── PUBLIC INTERNET ────────────────────┐
                    │                                                          │
   apps/web    apps/dealer   apps/admin    mobile (Flutter)   partner ERP     │
      │             │             │              │                 │          │
      └─────────────┴──────┬──────┴──────────────┴─────────────────┘          │
                           │  TLS 1.3 · HSTS preload · cert pinning (mobile)   │
   ════════════════════════╪═══════════ TB-1  edge ════════════════════════════╪══
                    WAF + bot mgmt + L7 DDoS + IP reputation                   │
                           │                                                    │
   ════════════════════════╪═══════════ TB-2  API gateway ═════════════════════╪══
     AuthN (JWT/JWKS) · device bind · session revocation · rate limit · idem   │
                           │                                                    │
   ════════════════════════╪═══════════ TB-3  authorisation ═══════════════════╪══
     permission → scope → OBJECT-LEVEL guard (IScopeGuard) · consent purpose   │
                           │                                                    │
        ┌──────────────────┼──────────────────┬──────────────────┐             │
   AutoHub.Platform   Modules/*          Outbox worker      Media pipeline     │
   (10 registries)    (domain)           (events, webhooks) (transcode/scan)   │
        │                  │                  │                  │             │
   ════════╪══════════════╪══════════════════╪══════════════════╪══ TB-4 data ═╪══
   PostgreSQL 16      Redis 7           OpenSearch         S3/R2 (private)     │
   (roles per module, RLS-ready, column crypto)  (no PII)  (signed URLs only)  │
        │                                                                       │
   ════════╪═══════════════════════════════ TB-5 egress ════════════════════════╪══
     Allow-listed NAT · SSRF guard · mTLS/HMAC to providers · Vault-held creds  │
        │                                                                       │
   Razorpay/Cashfree · Karza/Signzy · VAHAN/PARIVAHAN · MSG91/Gupshup ·         │
   CIBIL · IRP (e-invoice) · DigiLocker · insurer/lender APIs                   │
                    └──────────────────────────────────────────────────────────┘

  Separate blast radius, separate credentials, separate DB:
   ┌───────────────────┐   ┌────────────────────┐   ┌──────────────────────┐
   │ token vault       │   │ Vault / KMS        │   │ WORM audit archive   │
   │ (ah_tokens DB)    │   │ (secrets, HMAC)    │   │ (object-lock, 7 yr)  │
   └───────────────────┘   └────────────────────┘   └──────────────────────┘
```

**Five principles, applied literally**

1. **The database refuses what policy forbids.** `ck_dual_control__no_self`,
   `ck_ledger_accounts__escrow_backing`, the append-only trigger, the deferred ledger-balance
   trigger. Application discipline decays; constraints do not.
2. **Fail closed on money, fail open on browsing.** Already the documented Redis-outage behaviour
   (`04` §4.6). Extended here to Trust, consent and step-up checks.
3. **Every security threshold is registry data.** Step-up amounts, hold ceilings, breach SLAs,
   signed-URL TTLs, key rotation windows.
4. **Registries are code-as-data, so the evaluators must be boring.** JsonLogic guards run in a
   sandboxed, non-Turing-complete interpreter with an op allow-list and a step budget (§10.1). An
   admin compromise must not become RCE.
5. **Assume the perimeter is already inside.** Object-level authz, egress control, token vault
   separation and log integrity are all designed for "the attacker has a valid dealer session".

---

## 2. STRIDE threat model

### 2.0 Method

Per flow: assets → trust boundaries crossed → STRIDE table → controls (with the enforcement point)
→ residual risk. Risk = `Likelihood × Impact` on a 1–5 scale, scored **after** controls.
`platform.codes` set `security_threat_status` drives the register's lifecycle
(`open`, `mitigated`, `accepted`, `transferred`) — it is a workflow, not a spreadsheet column.

The register itself is data:

```sql
CREATE TABLE platform.threat_register (
    id            uuid PRIMARY KEY DEFAULT platform.uuid_generate_v7(),
    flow_code     platform.code_key NOT NULL,   -- 'auction_bid','escrow_release','dealer_payout','kyc','registry_edit'
    stride_code   platform.code_key NOT NULL,   -- 'spoofing','tampering','repudiation','info_disclosure','dos','eop'
    title         text NOT NULL,
    description   text NOT NULL,
    control_refs  text[] NOT NULL DEFAULT '{}', -- doc anchors + code symbols + constraint names
    enforcement_point text NOT NULL,            -- 'db_constraint' | 'middleware' | 'analyser' | 'registry_row' | 'process'
    likelihood    smallint NOT NULL CHECK (likelihood BETWEEN 1 AND 5),
    impact        smallint NOT NULL CHECK (impact BETWEEN 1 AND 5),
    state_code    platform.code_key NOT NULL,   -- workflow 'threat_item'
    owner_user_id uuid,
    review_due_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(), created_by uuid,
    updated_at timestamptz NOT NULL DEFAULT now(), updated_by uuid,
    row_version bigint NOT NULL DEFAULT 1
);
```

A threat item past `review_due_at` appears on the security queue in `apps/admin` (`16` C2). A threat
register nobody re-reads is theatre.

---

### 2.1 Flow A — Auction bidding

**Assets:** hammer price integrity, bid ordering, reserve confidentiality, dealer identity,
`auction.bids` (append-only), the Redis lot state that is authoritative for liveness.

```
Dealer app ──WSS──► Realtime hub ──► Bid API ──► Redis LUA (lot:{id}:state) ──► outbox ──► PG bids
     │                    │              │              │
   TB-1/2               TB-2           TB-3          TB-4
```

| STRIDE | Threat | Control | Enforcement point | L×I |
|---|---|---|---|---|
| **S** | Stolen dealer session bids from another device in the last 10 s | Device binding on every `sensitive` route; new-device bid triggers `security.new_device_login` + step-up when `bid_amount_paise > rule_sets.bid_step_up_threshold` | middleware + registry row | 2×5 |
| **S** | Sales exec bids beyond their authority using the owner's scope | `rbac_role_permissions.constraint_json.max_price_minor` evaluated per bid; `403 auction.bid.above_authority` | RBAC registry + `IScopeGuard` | 2×4 |
| **T** | Client tampers `amount_paise` between UI and API | Server recomputes minimum next bid from `auction_lots` + `fee_components`; client value is a *proposal*, never authoritative. `422 auction.bid.below_increment` | handler | 1×5 |
| **T** | Replayed bid frame over websocket | Bids are **never** accepted on the socket. The socket is read-only push; bids are `POST /auction/auctions/{id}/bids` with `Idempotency-Key` and `uq_bids__idempotency` | API design + DB unique | 1×5 |
| **T** | Two bids at the same ms produce two winners | Single Redis LUA CAS + `entry_seq`-ordered PG write; documented in `03` §10.1 | Lua atomicity | 1×5 |
| **R** | Dealer denies placing a winning bid | `auction.bids` append-only + `platform.audit_log` row with `actor_ip`, `did`, `request_id`, `correlation_id`; the bid confirmation SMS uses a DLT template with the lot ref and timestamp | append-only trigger + audit | 1×4 |
| **I** | Reserve price leaked via API/DTO | `reserve_price_paise` is never in the bidder DTO — enforced by an OpenAPI schema test (`ReserveNeverInBidderResponse`) that fails CI, not by reviewer memory | contract test | 2×4 |
| **I** | Competitor enumerates lot IDs to map another dealer's inventory | Public refs (`LOT-2607-4M8P`), `404` on invisible objects (`04` §5.4), per-tier `read` bucket | scope guard + rate limit | 2×3 |
| **D** | Bid-API starvation in the closing 30 s by a search flood | `bid` is a dedicated bucket, never shared, never shed under load (`04` §8, `04` §11) | rate-limit registry | 2×5 |
| **D** | Provider/infra outage while a lot is closing | `auction.global_pause` kill switch extends live lots rather than closing them (`16` D7.3 step 2) | feature registry | 2×5 |
| **E** | Moderator in `city:MUM` voids a Pune bid | `auction.bid.void` requires `auction.auction.moderate` at ≥ city scope **matching the lot's city**, plus four-eyes (`16` D6) | scope guard + dual control | 1×5 |
| **E** | Shill bidding by a seller-linked account | Trust device/payment/graph signals (`16` A5); `rule_sets.auction_bid_guard` denies bids where bidder and seller share a resolved entity cluster; `403 auction.bid.related_party` | guard rule set | 3×4 |

**Residual accepted:** collusive bidding between two genuinely distinct dealers is not technically
detectable at bid time. It is a post-hoc analytics pattern (`16` B3) and a contractual matter.

---

### 2.2 Flow B — Escrow release

**Assets:** buyer's sale consideration, `payments.ledger_entries`, the PA's escrow mirror, the
platform's licence posture (`14` §1: we are a merchant, not a PA).

```
Deal workflow transition ──► guard rule set ──► ledger txn ──► payout queue ──► PA/bank rail
                                    │                │
                              (all-must-hold)   (double-entry, deferred balance trigger)
```

| STRIDE | Threat | Control | Enforcement point | L×I |
|---|---|---|---|---|
| **S** | Forged Razorpay webhook says "captured ₹6.4L" | Signature over raw bytes, dedupe on `(provider, provider_event_id)`, and **re-fetch the amount from the provider API before crediting** (`04` §13.4 rule 4) | webhook pipeline | 2×5 |
| **T** | A code path releases escrow without a transition | There is no `ReleaseEscrowService.ReleaseNow()`. Release is a `workflow_transitions.effects` payload only (`14` §6.2). A Roslyn analyser (`AH0011`) fails any write to a `holds_funds` account outside `Payments.Application.Posting` | architecture + analyser | 1×5 |
| **T** | Ledger imbalance creates money | Deferred constraint trigger `tg_ledger_entries__balanced`; `ledger.unbalanced` pages on-call | DB constraint | 1×5 |
| **T** | Webhook replay double-credits escrow | `uq_ledger_transactions__idempotency` + inbound event dedupe (`03` §10.2) | DB unique | 1×5 |
| **R** | Buyer denies authorising release | Handover OTP is issued to the buyer's verified number via a DLT template, verified server-side, and the OTP challenge id is stored on the transition's `confirm_form` submission | workflow + audit | 1×4 |
| **I** | Buyer's bank details visible to the seller (or vice versa) | Bank data is `bank_account_hash` + masked last-4 only; the counterparty DTO carries neither. `pii_registry` marks `is_exportable=false` for the hash | schema + DTO test | 2×4 |
| **D** | PA outage strands funds past the hold ceiling | `payments.settlement_overdue` alert (`14` §6.4); `escrow_max_hold_days` is a registry row, and breaching it is a **compliance** alert, not just an ops one | registry + alert |3×4 |
| **E** | Ops agent releases their own related-party deal | `payments.payout.release` > ₹5,00,000 requires four-eyes; `ck_dual_control__no_self`; plus a related-party check against `trust` entity clusters before the approver sees the request | DB constraint + guard | 2×5 |
| **E** | Frozen (disputed) deal still releases | Freeze is at the **account** (`ledger_accounts.is_frozen`), rejected at the posting layer, so every future code path inherits it (`14` §6.3) | posting layer | 1×5 |

**The compliance-specific threat.** Holding money longer than the permitted window turns a merchant
into an unlicensed payment aggregator. The control is therefore not only an alert: when
`now() - escrow_funded_at > rule_sets.escrow_hard_ceiling` (default 30 days) **and** the deal is not
`is_locked` for a documented dispute, the platform auto-fires the `refund_to_source` transition. The
money goes back, not into a suspense account. That behaviour is a `workflow_transitions` row with
`trigger_type='timer'`, so legal can shorten the ceiling without a deploy.

---

### 2.3 Flow C — Dealer payout

**Assets:** the dealer's verified bank account, the payout instruction, the platform's float.

| STRIDE | Threat | Control | Enforcement point | L×I |
|---|---|---|---|---|
| **S** | Account-takeover changes payout account, then withdraws | Payout-account change requires step-up **and** bumps `users.token_epoch` (`04` §4.6), **and** starts a `payout_cooldown_hours` timer (registry, default 24) during which no payout to the new account is queued. The old account and the registered number both get a `security.payout_account_changed` notification | registry + workflow timer | 3×5 |
| **S** | SIM-swap defeats OTP | Cooldown above is the real control; OTP alone is not. Additionally: a number ported/reissued signal from the SMS provider raises a Trust event and forces re-KYC before the next payout | Trust signal | 3×5 |
| **T** | Beneficiary substitution between approval and execution | `dual_control_requests.payload_hash` — the approver approves *that* payload; execution recomputes the hash and aborts on mismatch (`platform.dual_control_payload_mismatch`, pages security) | DB column + executor | 1×5 |
| **T** | IFSC/account edited to a mule account passing penny-drop | Penny-drop name match score stored (`trust.kyc_records.penny_drop_match_score`); below `rule_sets.penny_drop_min_score` → manual review; name must also match PAN name within threshold | rule set | 3×4 |
| **R** | Dealer disputes a payout amount | Payout references ledger transaction + invoice; the statement is generated from `ledger_entries`, never from a cached balance (`03` §12.3) | ledger-as-truth | 1×3 |
| **I** | Bulk export of dealer bank details by a support agent | Bank fields are tokenised (§5.5); the support console renders the token's last-4 only; `export` bucket is 2–50/day by tier; every export writes an audit row **and** a `data_export_receipts` row naming the columns | tokenisation + audit | 2×5 |
| **D** | Payout queue floods at month-end and misses the settlement window | Payout batching is a scheduled job with a per-window cap from the registry; overflow is a queue depth alert, never a dropped instruction | ops | 2×3 |
| **E** | A `dealer_sales_exec` triggers a payout | `payments.payout.release` is `min_scope='global'` and `is_sensitive=true`; no dealer-tenant role may hold it (`rbac_roles.tenant_kind` gate, enforced at role-assignment write time, not at use time) | RBAC constraint | 1×5 |

---

### 2.4 Flow D — KYC

**Assets:** PAN, Aadhaar reference, bank identifiers, selfie/liveness, the KYC verdict itself.
This is the flow where the **worst-case is regulatory, not financial**: a stored Aadhaar number is a
UIDAI/DPDP incident regardless of whether anything was stolen.

| STRIDE | Threat | Control | Enforcement point | L×I |
|---|---|---|---|---|
| **S** | Someone KYCs using another person's PAN | PAN name ↔ Aadhaar/OKYC name ↔ penny-drop name three-way match with thresholds from `rule_sets.kyc_name_match`; liveness + face-match against the OKYC photo | rule set | 3×5 |
| **S** | Injected/replayed liveness video | Provider-side liveness with a server-issued challenge nonce; the app never uploads a gallery file for liveness (camera-only capture, `mediaSource=camera` enforced by the provider SDK); score-only persistence | provider + schema | 3×4 |
| **T** | KYC verdict flipped directly in the DB | `trust.kyc_records.state_code` mirrors `workflow_instances`; a mismatch is caught by the nightly integrity suite (`16` D7.2) and pages | integrity suite | 1×5 |
| **T** | Provider response tampered in transit | mTLS or HMAC-signed responses per `integration_providers.auth_type`; `kyc_checks.request_hash` + `response_redacted` retained for dispute | integration registry | 1×4 |
| **R** | User denies consenting to a bureau pull | `trust.consents` row with `policy_version`, `evidence.{ip_hash, ua_hash, screen}`, the exact `purpose_text_i18n_key` **and** its rendered string version; a **consent receipt** PDF is issued (§6.3) | consent table + receipt | 1×5 |
| **I** | Aadhaar number stored anywhere | **No column can hold it** (`03` §4.11). Reinforced by a CI check that greps migrations for `aadhaar` + a 12-digit-format domain, and a DLP regex on logs/S3 | schema + CI + DLP | 1×5 |
| **I** | KYC document images served by a guessable URL | Documents live in a private bucket; access is a 300 s signed URL minted after an authz check, bound to the requesting `sub` (§10.4); `documents.documents` rows are never public-ref addressable | media pipeline | 2×5 |
| **I** | Support agent browses KYC records at will | `trust.kyc.read_full` is `is_sensitive=true`, requires an open case id in the request (`reason_code` + `entity_id`), and every read writes an audit row; reads without a case are a weekly report to the DPO | RBAC + audit | 3×4 |
| **D** | KYC provider down blocks all dealer onboarding | `provider_routes` failover chain (`karza` → `signzy`), circuit breaker; if all down, `kyc` enters `waiting_provider` (a workflow state with `counts_as_open=true`, `sla_minutes` set), not `rejected` | integration registry | 3×3 |
| **E** | KYC level self-elevated via a client field | `kyc_level` is never accepted from a request body; it is computed from passed `kyc_checks` by a rule set. A CI test asserts no DTO binds to `kyc_level` | rule set + test | 1×5 |

---

### 2.5 Flow E — Admin registry edit

**The highest-privilege flow in the product.** A registry row *is* business logic. An attacker who
can publish a `fee_components` row with `rate_percent = 0` and their own `payee_role_code` has
achieved code execution without touching code.

```
admin UI ──► change-set draft ──► diff ──► review ──► approve (4-eyes) ──► apply (txn) ──► epoch bump ──► cache invalidate
                                                             │
                                                    dual_control_requests.payload_hash
```

| STRIDE | Threat | Control | Enforcement point | L×I |
|---|---|---|---|---|
| **S** | Stolen admin session publishes a pricing change | Staff auth is IdP + hardware/TOTP MFA + IP allow-list (§3.7); `platform.registry.publish` is `step_up: true` **and** four-eyes for pricing/tax/workflow/RBAC (`16` D6) | auth + dual control | 2×5 |
| **T** | A malicious JsonLogic `condition` performs RCE or a resource bomb | Sandboxed evaluator: op allow-list, no `var` traversal outside the declared `input_schema`, max depth 12, max 5,000 steps, 50 ms budget, no I/O, no regex backtracking (RE2). Exceeded → rule denies and raises a P2 | evaluator | 2×5 |
| **T** | Fee routed to an attacker-controlled payee | `fee_components.payee_role_code` FKs `platform.party_roles`; a *party* can never be a payee, only a **role**. Settlement resolves the role to a party from the deal graph at posting time | schema design | 1×5 |
| **T** | A workflow edit removes a `holds_funds` guard mid-flight | Workflows are versioned artifacts; in-flight instances pin their version (`03` §3.12). A new version cannot retroactively unlock funds | versioning | 1×5 |
| **T** | Notification template edited to phish users | `notification_templates` publish requires four-eyes; template bodies are rendered with contextual auto-escaping and a **link allow-list** (only `autohub.in` + registered partner domains + DLT-whitelisted URLs) | registry + renderer | 2×4 |
| **R** | "Who changed the commission?" is unanswerable | `registry_entity_versions` before/after + `audit_log.changed_fields` + `registry_change_sets.author/reviewer/approver` | append-only tables | 1×4 |
| **I** | Diff view leaks `provider_credentials` | `provider_credentials` is `REVOKE ALL FROM PUBLIC`; the change-set diff builder has a hard deny-list of tables and of any column named `*_secret*`, `*_key*`, `encrypted_value` | grants + diff builder | 1×5 |
| **D** | A bad change set takes the platform down | Staged rollout: `environment='staging'` apply → smoke suite → `production`. `platform.registry.freeze` kill switch. One-transaction rollback (`fn_rollback_change_set`) with `55000 registry.rollback_conflict` on drift | change-set machinery | 3×4 |
| **E** | An admin grants themselves a permission | `rbac_role_assignments` write where `user_id = current_actor` is rejected by `ck_role_assignments__no_self_grant`; all RBAC change sets are four-eyes; break-glass grants are time-boxed, page security, and tag every downstream action (`16` D6) | DB constraint | 1×5 |
| **E** | Kill switches bypass change-set review | Kill switches are the one documented exception (`16` F9) — so they are **narrow by construction**: a kill switch may only set a boolean feature flag to `false`; it can never create, price, or route. `ck_feature_flags__killswitch_shape` enforces it | DB constraint | 2×4 |

**Residual accepted:** a fully compromised approver *and* requester pair can publish anything. The
compensating control is detection, not prevention: every applied change set to a pricing, tax, RBAC
or workflow registry emits `platform.registry.applied.v1` to the security channel within 60 s, and
the nightly job diffs production registries against the last approved snapshot.

---

### 2.6 Residual risk register — the ten we actively watch

| # | Risk | Why it survives | Compensating detection | Review |
|---|---|---|---|---|
| 1 | Account takeover → payout redirect | OTP is a weak factor in a SIM-swap market | Cooldown + old-account notification + velocity | monthly |
| 2 | Object-level authz gap in a new endpoint | Human-written handlers | Analyser `AH0007` + nightly IDOR fuzz (§4.6) | per release |
| 3 | Collusive bidding | Not detectable at bid time | Post-hoc graph analytics | quarterly |
| 4 | Insider bulk PII read | Support needs data to do the job | Case-bound reads + weekly no-case report to DPO | weekly |
| 5 | Registry approver collusion | Two-person control has a two-person bypass | Snapshot diff + security-channel broadcast | daily |
| 6 | Partner webhook URL used for SSRF | Partners must be able to register URLs | Validate on write **and** at delivery; egress allow-list | per registration |
| 7 | Provider compromise (KYC/PG) leaks PII we sent | Their perimeter, not ours | Minimised payloads; `provider_routes` kill; contractual breach clock | quarterly |
| 8 | Dependency supply-chain injection | npm/NuGet reality | Cooldown, lockfiles, SBOM, provenance (§10.8) | continuous |
| 9 | Escrow hold-ceiling breach → PA licence exposure | Disputes genuinely take time | Auto-refund timer + compliance alert | daily |
| 10 | Cross-border data flow via a sub-processor | SaaS defaults to US regions | `integration_providers` region attestation; contract register | per onboarding |

---

## 3. Authentication — implementation beyond the wire contract

`04` §4 defines the wire. This section defines what must be true underneath it.

### 3.1 Signing keys and JWKS

| Property | Value | Rationale |
|---|---|---|
| Algorithm | `RS256` (locked by `04` §4.3) | Broad client support incl. Flutter + partner JWT validation |
| Key storage | Cloud KMS asymmetric key, **non-exportable**; signing is a KMS API call, cached CSR-style with a 5 s local signer pool | An app-server compromise cannot exfiltrate the signing key, only mint tokens while it is live |
| `kid` | `uuid v7` of the `platform.crypto_keys` row | Time-sortable, traceable to a rotation event |
| Active keys in JWKS | Exactly 2 (current + next) | Rotation without a validation gap |
| Rotation | 90 days (`rule_sets.key_rotation.jwt_signing`), overlap 7 days | Configurable without a deploy |
| Emergency rotation | `POST /platform/admin/security/keys/{id}/revoke` → key removed from JWKS, `token_epoch` bumped for **all** users | The only correct response to a suspected signing-key compromise is global logout |
| JWKS caching | `Cache-Control: max-age=86400, stale-while-revalidate=604800`; clients must re-fetch on unknown `kid` | A 24 h cache with SWR survives a JWKS endpoint outage |

The `alg` value is validated against an allow-list **before** key resolution; `none` and any HMAC
alg are rejected at parse time. A dedicated unit test (`JwtAlgConfusionTest`) asserts that a token
signed with HS256 using the RSA public key as the secret is rejected.

### 3.2 Token lifetimes are registry data

`platform.security_policies` (new, this document's addition to the Feature Registry surface):

| `policy_key` | Default | Scope | Notes |
|---|---|---|---|
| `auth.access_token.ttl_seconds` | 900 | global, per `tenant_kind` | Staff = 600 |
| `auth.refresh_token.ttl_days` | 60 | per `tenant_kind` | Staff = 7 |
| `auth.refresh.replay_grace_seconds` | 10 | global | Named in `04` §4.4 |
| `auth.session.absolute_max_days` | 180 | per `tenant_kind` | Sliding refresh still dies here |
| `auth.session.idle_timeout_minutes` | 43200 (customer) / 60 (admin) | per `tenant_kind` | Admin idles out in an hour |
| `auth.step_up.max_age_seconds` | 300 | per permission | Overridable per action |
| `auth.device.max_active` | 10 (customer) / 3 (staff) | per `tenant_kind` | 11th device evicts the oldest, with notice |
| `auth.otp.*` | see `04` §4.2.1 | per channel | Ladder, not a single number |

Rows are `effective_from`/`effective_to` dated. Shortening a lifetime never invalidates live tokens
retroactively (a token is valid per its own `exp`); it applies at next mint. Immediate effect
requires a `token_epoch` bump, which is a separate, four-eyes action.

### 3.3 Refresh reuse detection — operational detail

`04` §4.4 defines the algorithm. Three additions that make it survive production:

1. **The family is the unit of revocation, and the family is logged.** On `reuse_detected`, write one
   `security_events` row per revoked token with `family_id`, both `ip_hash` values, both
   `user_agent`s and the time delta. That delta is the single best signal for distinguishing "user's
   phone retried on a flaky train" from "token was pasted into a script in Vietnam".
2. **Reuse detection is a Trust signal, not just an auth error.** `identity.refresh_reuse_detected`
   raises a `trust_signal` with weight from the model registry. Three families in 30 days ⇒
   mandatory re-KYC before the next payout.
3. **The grace window is asymmetric.** The 10 s replay grace applies only when device **and**
   `ip_hash` match. Different IP inside 10 s is treated as reuse. Indian mobile users change IP on
   tower handoff, so we key the grace on `did` primarily and accept a small false-positive rate on
   IP — logging out a user is recoverable; leaving a stolen family live is not.

### 3.4 Device binding hardening

`X-AutoHub-Device-Id` alone is a bearer value. It is hardened as follows:

| Surface | Binding | Notes |
|---|---|---|
| Flutter | Device id in platform secure storage (Keychain / EncryptedSharedPreferences, `StrongBox`/Secure Enclave where present) + an attestation blob (Play Integrity / DeviceCheck) submitted on `otp/verify` | Attestation result is stored on `auth_devices.fingerprint_hash` provenance and is **advisory** — a failed attestation raises the risk band, it does not block login. Blocking would exclude rooted-but-legitimate Indian budget devices |
| Web | `__Host-ah_did` cookie (`HttpOnly; Secure; SameSite=Strict; Path=/`) + IndexedDB mirror for recovery | Cookie is the authority; IndexedDB only re-seeds it after a cookie clear, and re-seeding forces a fresh OTP |
| Partner API | mTLS client cert or API key + `ip_allowlist` (`16` D5) | No device concept; the key is the device |

A device id that appears under **two different user ids within 24 h** is normal (shared phone,
dealer showroom tablet). A device id appearing under **more than `rule_sets.device_user_fanout`
(default 8) users in 24 h** is a farm: the device is blocked, all its sessions revoked, and a Trust
case opens.

### 3.5 Session revocation — how "instant" is actually achieved

```
revocation event ─┬─► Redis:  SETEX revoked:sid:{sid} 900 1
                  │           SET   user:{uid}:token_epoch <n>
                  ├─► Redis Pub/Sub channel `sec.revoke` ──► every API pod's in-process
                  │        L1 cache (60 s TTL) evicts the entry immediately
                  └─► outbox ──► realtime hub: force-disconnect all sockets for {sid}
```

- Auth middleware does **one pipelined Redis read** per request (`revoked:sid`, `token_epoch`,
  `rbac:v{ver}:user:{sub}`) — the same round-trip already required for RBAC (`04` §4.3).
- L1 in-process cache TTL is 60 s **for reads only**; routes flagged `sensitive: true` always hit
  Redis. Worst case, a revoked customer can read listings for 60 s; they can never move money.
- Redis unavailable ⇒ fail **closed** for `sensitive: true`, open for reads (`04` §4.6). This is
  restated here because it is a deliberate, auditable trade-off, and auditors ask.
- Websocket sessions are force-disconnected, not left to expire — a live auction socket outliving a
  revoked session is exactly the scenario revocation exists for.

**Everything that bumps `token_epoch`:** logout-all, admin suspend, Trust `blocked` band, password
change, payout-account change, refresh-reuse detection, signing-key emergency rotation, and a DPDP
erasure execution.

### 3.6 Step-up authentication for money movement

Step-up is declared on the **permission**, not the endpoint, so a new endpoint using an existing
permission inherits it:

```sql
ALTER TABLE platform.permissions
  ADD COLUMN step_up_required        boolean NOT NULL DEFAULT false,
  ADD COLUMN step_up_max_age_seconds integer,
  ADD COLUMN step_up_methods         text[] NOT NULL DEFAULT '{otp}',
  ADD COLUMN step_up_amount_threshold_paise bigint;   -- NULL = always
```

| Action | Threshold | Methods | Max age |
|---|---|---|---|
| `payments.payout_account.write` | always | `otp` + `totp` for staff | 300 s |
| `auction.bid.place` | > ₹25,00,000 | `otp` | 900 s (once per auction session) |
| `payments.payout.release` | always (staff) | `totp` \| `webauthn` | 120 s |
| `platform.registry.publish` | always | `totp` \| `webauthn` | 120 s |
| `identity.user.impersonate` | always | `webauthn` | 60 s |
| `trust.dpdp.export` / `.erase` | always | `otp` (self) / `totp` (staff) | 300 s |
| `dealer.inventory.bulk_price_update` | > `N` listings | `otp` | 600 s |
| `identity.party_credential.write` | always | `otp` | 300 s |

The `amr` claim carries the methods and `auth_time`; the middleware compares
`now - auth_time <= step_up_max_age_seconds`. Failure is `401 identity.mfa.required` with
`params.methods`, so the client renders the right challenge without hardcoding a policy.

**Why `otp` is not enough for staff.** SMS OTP is defeated by SIM swap, and staff actions are the
ones that move other people's money. Staff step-up is TOTP or WebAuthn; SMS is not in
`step_up_methods` for any permission where `tenant_kind='internal'`. This is a CI-asserted invariant
(`StaffStepUpMayNotUseSms`).

### 3.7 Staff authentication is a different system

| Control | Customer / dealer | Staff (`apps/admin`) |
|---|---|---|
| Primary factor | Phone OTP | Corporate IdP (OIDC), SSO-only, no local password |
| Second factor | Optional | Mandatory: WebAuthn preferred, TOTP fallback; SMS never |
| Network | Open | IP allow-list per role tier (`rbac_roles.ip_allowlist inet[]`) — mandatory for `payments.*` and `platform.registry.*` roles |
| Session | 60 days sliding | 8 h absolute, 60 min idle |
| Device | Any | Managed device posture claim from the IdP; unmanaged device ⇒ read-only scopes only |
| Offboarding | Self-service | IdP deprovision → SCIM → `rbac_role_assignments.valid_to = now()` within 5 min; a nightly reconciliation job asserts zero active assignments for disabled IdP users and pages on drift |
| Impersonation | n/a | Four-eyes, time-boxed to 30 min, banner visible in the UI, `audit_log.actor_kind='impersonated'` with both ids, and the impersonated user is notified |

---

## 4. Authorisation — object-level authz is the #1 real risk

A marketplace's characteristic breach is not SQLi. It is `GET /deals/{someone-elses-id}` returning
200. Every control below exists for that.

### 4.1 The three-layer check, in order

```
1. AUTHENTICATED?      valid token, not revoked, epoch current, device bound
2. PERMITTED?          user's effective permission set contains `{module}.{resource}.{action}`
3. IN SCOPE?           the RESOURCE's owner/dealer/branch/city ∈ the user's resolved scope set
   3b. CONSENTED?      if the action needs a DPDP purpose, an un-withdrawn consent exists
   3c. UNRESTRICTED?   trust.v_active_restrictions does not block this actor for this action
```

Layers 1–2 are middleware. **Layer 3 is the one that gets skipped**, because it needs the resource,
which means it lives in the handler. So it is made unskippable.

### 4.2 `IScopeGuard` and analyser `AH0007`

```csharp
[RequirePermission("deals.deal.read", Scope.Resource)]
public async Task<Result<DealResponse>> Handle(GetDeal q, CancellationToken ct)
{
    var deal = await _reader.ById(q.DealId, ct);
    if (deal is null) return Error.NotFound("deals.deal.not_found");

    _guard.Assert(deal);            // ← omitting this is a BUILD ERROR (AH0007)
    return Map(deal);
}
```

- `Assert(T resource)` resolves `IOwnedResource` (`OwnerPartyId`, `DealerId`, `BranchId`, `CityCode`,
  `Visibility`) and compares against the request's resolved scope set.
- **`AH0007`** fails compilation if a handler annotated `Scope.Resource` has any return path that
  does not pass through `Assert`. Not a lint warning — an error, in CI and locally.
- **`AH0008`** fails compilation if a handler returns a type containing a property annotated
  `[Sensitive]` without a corresponding `guard.AssertField(...)`.
- **`AH0009`** fails compilation on any raw `DbContext` query in a module that filters by an id
  coming from route/query/body without a scope predicate — the "trusted id" bug, caught statically.
- `Assert` failure throws `ScopeViolationException`, which the middleware maps to **404** when the
  caller lacks read visibility and **403** when they can read but not act (`04` §5.4). The decision
  is in the guard, so it cannot drift per handler.

### 4.3 The IDOR catalogue — every object, its owner, its visibility rule

This table is the checklist the nightly fuzzer (§4.6) is generated from. Adding a row-owning entity
without adding a row here fails the `ScopeCatalogueComplete` test.

| Object | Owner key | Read visibility | Write | Notes |
|---|---|---|---|---|
| `listings.listings` | `owner_party_id`, `dealer_id` | public when `workflow_states.is_publicly_visible` | owner/dealer scope | The *only* object with a broad public read |
| `catalog.vehicles` | `created_by_party_id` | public subset; full record needs a deal or ownership | catalog roles | Provenance is not the seller's private data (`03` §9.2) |
| `catalog.vehicle_media` | via vehicle | public only for published listings | owner | Private inspection photos never inherit listing visibility |
| `deals.deals` | buyer + seller party ids | parties + assigned ops | parties (state-gated) | `404` to everyone else |
| `payments.ledger_entries` | via `party_id` / `deal_id` | own entries only | none (append-only) | Statements are generated, never queried raw by a client |
| `payments.invoices` | `bill_to_party_id` | party + finance roles | none | Immutable once IRN issued |
| `auction.auctions` | `seller_party_id` | invited/eligible dealers per `auction_participants` | seller + moderators | Private auctions must not be enumerable |
| `auction.bids` | `bidder_party_id` | own bids + aggregate position; never the full book | none | Full book is `auction.auction.moderate` |
| `bookings.bookings` | `party_id`, `provider_party_id` | both parties | both, state-gated | Test-drive addresses are PII |
| `inspection.reports` | `requested_by_party_id`, vehicle owner | buyer of a listing sees the shared report version | inspector + ops | Two DTOs: `full` and `shared` |
| `trust.kyc_records` | `party_id` | self (masked) + `trust.kyc.read_full` with case | KYC pipeline only | Case-bound reads |
| `documents.documents` | polymorphic `entity_type/entity_id` | resolved through the owning entity's rule | uploader + ops | Never directly addressable |
| `chat.threads` / `messages` | participants | participants only | participants | Ops read requires a dispute case |
| `identity.parties` | self | self + counterparties in an active deal (masked contact) | self | Contact reveal is a consent-gated action |
| `dealer.*` inventory, CRM, leads | `dealer_id` | dealer + branch scope | dealer/branch | The classic multi-tenant IDOR surface |
| `finance.applications` | `borrower_party_id` | borrower + the RE it was routed to | borrower, state-gated | Never visible to another lender |
| `insurance.policies` | `insured_party_id` | insured + broker ops | none | |
| `platform.*` registries | none (global) | `platform.registry.read` | change-sets only | `provider_credentials` readable by no application role |

**Contact reveal is a first-class authorised action.** `POST /listings/{id}/contact-reveal` requires
the seller's `profile.share_with_dealer`-class consent purpose, is rate-limited on its own bucket,
writes an audit row, and returns a masked number plus a call-masking session id — the raw number is
never in a response body. Scraping seller phone numbers is the most common real attack on Indian
classifieds, and an unauthenticated `listing.seller_phone` field is how it succeeds.

### 4.4 Identifiers

- Internal ids are `uuid v7` — **time-sortable, therefore enumerable in time order and leaky about
  creation time.** They are never the public handle for a business object.
- Public handles are `public_ref` (`DEAL-2607-4M8P`, `LOT-…`, `CASE-…`): prefix + `YYMM` +
  4 chars of Crockford base32 from a per-prefix HMAC of the id. Non-sequential, non-guessable,
  human-readable over a phone call.
- Media/document handles are opaque storage keys (`uuid v7`, no extension, no original filename),
  never reachable without a signed URL.
- API routes accept **either** form; the resolver normalises. Responses emit `public_ref` for
  business objects and `id` only where the client must correlate (idempotency, realtime).

### 4.5 Enumeration and the existence oracle

`04` §5.4's 404-vs-403 rule closes the direct oracle. Three secondary oracles are closed too:

| Oracle | Fix |
|---|---|
| Response-time difference between "not found" and "not yours" | Both paths do the same work; the guard runs after the fetch either way |
| `409 duplicate` on write reveals another user's object (e.g. a registration number) | `vehicle.registration_in_use` is intentionally revealing — it is a *public* fact on a plate — but `listing.duplicate_active` is scoped to the caller's own listings |
| Error `params` leaking counterparty data | `params` may contain only values the caller supplied or already holds. A contract test asserts no `params` key matches the PII column allow-list |

### 4.6 The IDOR test harness (this is what actually catches it)

Nightly, against staging, generated from §4.3:

```
for each (object type, endpoint) in scope_catalogue:
    create the object as persona A (customer / dealer-X / dealer-Y / staff-city-MUM)
    for each other persona B:
        call GET/PUT/PATCH/DELETE with A's id and B's token
        assert response ∈ {404, 403} per the catalogue's declared rule
        assert body contains no field from pii_registry where is_exportable=false
    call with no token           → assert 401 (or 200 only if declared public)
    call with an expired token   → assert 401
    call with B's X-AutoHub-Scope header while holding A's roles → assert 403
```

Personas are fixtures; the matrix is ~18 objects × 6 personas × 4 methods ≈ 430 assertions and runs
in under four minutes. A new module that adds an object without adding it to the catalogue fails
CI before the fuzzer ever runs.

---

## 5. Cryptography, secrets and key management

### 5.1 Key hierarchy

```
Cloud KMS CMK (per environment, non-exportable, HSM-backed, key policy = 2 named officers)
 ├── JWT signing key pair        (asymmetric, sign-only, rotate 90 d)
 ├── KEK: column encryption      (symmetric, wraps DEKs, rotate 365 d)
 ├── KEK: token vault            (separate CMK, separate key policy, separate DB)
 └── Vault Transit unseal / auto-unseal
        └── Vault (HashiCorp) —
             ├── KV v2: provider credentials  → platform.provider_credentials.secret_ref
             ├── Transit: HMAC keys           → pan_hmac, phone_bidx, ip_hash, public_ref
             ├── Transit: encrypt/decrypt     → low-volume, high-sensitivity columns
             └── Database secrets engine      → short-lived PostgreSQL creds per module role
```

- Application code **never sees** a KEK or an HMAC key. `pan_hash` and `phone_bidx` are computed by
  Vault Transit `hmac` — so a full DB dump plus application RCE still cannot enumerate the ~3×10¹²
  PAN keyspace offline. This is a deliberate upgrade over `03` §4.11's "per-environment pepper":
  a pepper stored where the app can read it is a pepper the attacker gets too.
- PostgreSQL credentials are dynamic (Vault DB engine), TTL 1 h, one role per module
  (`ah_listings`, `ah_payments`, …) with grants only on its own schema plus the contract views. The
  `42501 permission denied → common.module_boundary` mapping in `03` §11 is enforced by these
  grants, not by review.

### 5.2 `platform.crypto_keys`

```sql
CREATE TABLE platform.crypto_keys (
    id            uuid PRIMARY KEY DEFAULT platform.uuid_generate_v7(),   -- this IS the kid / key_id
    purpose_code  platform.code_key NOT NULL,   -- 'jwt_sign','column_dek','token_kek','hmac_pan',
                                                -- 'hmac_phone','webhook_out','media_url','public_ref'
    algorithm     text NOT NULL,                -- 'RS256','AES-256-GCM','HMAC-SHA256'
    provider_code platform.code_key NOT NULL,   -- 'kms','vault_transit','local_dev'
    external_ref  text NOT NULL,                -- KMS key ARN / Vault key name + version
    state_code    platform.code_key NOT NULL,   -- workflow 'crypto_key': pending|active|rotating|retired|revoked
    activated_at  timestamptz,
    retires_at    timestamptz,                  -- decrypt-only after this
    revoked_at    timestamptz,
    rotation_period_days smallint NOT NULL,
    last_rotated_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(), created_by uuid,
    updated_at timestamptz NOT NULL DEFAULT now(), updated_by uuid,
    row_version bigint NOT NULL DEFAULT 1
);
CREATE UNIQUE INDEX uq_crypto_keys__one_active
  ON platform.crypto_keys (purpose_code) WHERE state_code = 'active';
```

That partial unique index is the control: exactly one **active** (encrypt-with) key per purpose, any
number of `rotating`/`retired` (decrypt-only) keys. Ciphertext carries its `key_id`, so rotation is
lazy — nothing is re-encrypted at rotation time; rows are re-wrapped opportunistically on next write
and by a background job that must complete before the old key reaches `revoked`.

**Key states are workflow-registry rows**, and `retired → revoked` has a guard rule set that refuses
to fire while any row still references that `key_id`. Revoking a key that still has live ciphertext
is data loss, so the machine will not let a human do it.

### 5.3 Rotation schedule (`rule_sets.key_rotation`)

| Key | Period | Overlap | Trigger for emergency rotation |
|---|---|---|---|
| JWT signing | 90 d | 7 d | Any suspected app-server compromise |
| Column DEK/KEK | 365 d | until re-wrap completes | DB dump suspicion |
| Token vault KEK | 365 d | until re-wrap completes | Token DB access anomaly |
| HMAC (PAN, phone) | **never routine** | n/a | Compromise only — rotating breaks blind indexes; requires a full re-index migration |
| Outbound webhook secrets | 180 d | 7 d (`secret_previous`, `04` §13.2) | Partner request or breach |
| Provider API credentials | per `provider_credentials.rotates_at`, max 365 d | provider-specific | Provider incident |
| Partner API keys | 90 d default, 365 d max (`16` D5) | until old expires | Key leak |
| Media URL signing key | 30 d | 1 d | Leak of a signing key |
| mTLS client certs | 365 d | 30 d | CA incident |
| DB credentials | 1 h (dynamic) | n/a | n/a |

Rotation is a scheduled job that opens a change set; a key past `rotation_period_days` raises a
compliance alert and appears on the security queue. **A key that cannot be rotated without downtime
was designed wrong** — the HMAC row above is honest about being the one exception, and its blast
radius is why we accept it.

### 5.4 Encryption at rest — the column register

Full-disk / cluster-level encryption (`pgcrypto` is not the mechanism; the volume and the managed
instance are encrypted) is the baseline and protects against a stolen disk. It protects against
**nothing else** — a compromised app or a leaked read-replica credential reads plaintext. So a
defined subset is additionally encrypted at the column level.

`platform.pii_registry` gains three columns (this document's schema requirement):

```sql
ALTER TABLE platform.pii_registry
  ADD COLUMN encryption_class platform.code_key NOT NULL DEFAULT 'none',
      -- 'none' | 'column_aead' | 'tokenised' | 'hash_only' | 'external_ref_only'
  ADD COLUMN blind_index_column text,     -- the searchable HMAC companion, if any
  ADD COLUMN key_purpose_code platform.code_key;   -- → platform.crypto_keys.purpose_code
```

AEAD format, stored as `bytea`:

```
[1B fmt=0x01][16B key_id (uuid v7)][12B nonce][ciphertext][16B GCM tag]
AAD = utf8("{schema}.{table}.{column}|{row_id}")
```

The AAD binding is the important part: ciphertext copied from one row into another **fails to
decrypt**. Without it, an attacker with an UPDATE primitive could swap a mule's encrypted bank
account into a legitimate dealer's row.

| Column | Class | Key purpose | Why this class |
|---|---|---|---|
| `identity.users.phone_e164` | `column_aead` + `phone_bidx` blind index | `column_dek` / `hmac_phone` | Login lookup needs equality, not range. Unique index sits on the HMAC |
| `identity.users.email` | `column_aead` + blind index | same | Same reasoning |
| `identity.users.password_hash` | `none` (already argon2id) | — | Hashing is the protection; encrypting adds nothing |
| `identity.parties.display_name` | `none` | — | Shown to counterparties; encrypting it makes search impossible for zero gain |
| `identity.addresses.line1/line2` | `column_aead` | `column_dek` | Test-drive/home-inspection addresses are the highest-abuse PII we hold |
| `identity.addresses.pincode`, `city_id` | `none` | — | Needed for geo queries; low re-identification value alone |
| `trust.kyc_records.pan_hash` | `hash_only` (Vault Transit HMAC) | `hmac_pan` | Duplicate detection without ever holding a PAN |
| `trust.kyc_records.pan_name` | `column_aead` | `column_dek` | Name-match needs the value at verification time only |
| `trust.kyc_records.aadhaar_ref_id` | `column_aead` | `column_dek` | UIDAI reference is still an identifier |
| `trust.kyc_records.aadhaar_last4`, `pan_last4` | `none` | — | Display-only, 4 digits, low value |
| `trust.kyc_records.bank_account_hash` | `hash_only` | `hmac_pan` | Mule-account detection across parties |
| `payments.payout_accounts.account_number` | `tokenised` | `token_kek` | Must be retrievable to instruct a payout — token vault, not app DB |
| `payments.payout_accounts.ifsc`, `beneficiary_name` | `column_aead` | `column_dek` | |
| `documents.documents.storage_key` | `none` | — | Opaque; access control is the signed URL |
| `documents.documents.extracted_fields` (jsonb) | `column_aead` | `column_dek` | OCR of an RC/DL is a PII pile |
| `chat.messages.body` | `column_aead` | `column_dek` | Contains numbers, addresses, price negotiation |
| `inspection.reports.inspector_notes` | `column_aead` | `column_dek` | Free text always becomes PII |
| `catalog.vehicle_identities.chassis_no`, `engine_no` | `none` | — | Vehicle identity, not personal data; needed for VAHAN lookups and duplicate detection |
| `analytics.*` any user identifier | `tokenised` | `token_kek` | The warehouse never sees a raw identifier |
| `platform.provider_credentials.encrypted_value` | `column_aead` (fallback path only) | `column_dek` | `secret_ref` → Vault is the preferred path (`03` §3.9) |
| `platform.audit_log.before_row/after_row` | `none` (already redacted) | — | Redaction, not encryption — the log must stay readable by compliance |

**What is deliberately NOT encrypted at the column level:** anything the search or reporting path
needs to range-scan, anything already hashed, and anything whose value is public (registration
number, make/model). Encrypting a column you then have to decrypt for every query is a performance
tax with a security theatre payoff.

**Card data: none, ever.** No column, anywhere, may hold a PAN (card), CVV, track data or a card
expiry. Checkout is a gateway-hosted flow or gateway SDK; we receive a `payment_id` and a
network token reference at most. Enforced by (a) no such column existing, (b) a CI regex over
migrations, (c) a DLP regex on application logs and S3 uploads that quarantines and pages, (d) the
WAF blocking request bodies matching a Luhn-valid 13–19 digit pattern to any route other than the
gateway callback. This is what keeps PCI-DSS scope at SAQ-A.

### 5.5 Tokenisation

A separate PostgreSQL database (`ah_tokens`), separate credentials, separate KMS key, reachable only
from a small `AutoHub.Integrations.TokenVault` service with its own network policy.

```sql
-- in ah_tokens, NOT in the main cluster
CREATE TABLE vault.pii_tokens (
    token         text PRIMARY KEY,          -- 'tok_ba_01JC7X…' — random, not derived from the value
    token_class   text NOT NULL,             -- 'bank_account','phone','email','account_holder'
    value_enc     bytea NOT NULL,            -- AEAD under the token KEK
    value_bidx    bytea,                     -- HMAC, for "is this the same account?" without detokenising
    last4         text,
    created_at    timestamptz NOT NULL DEFAULT now(),
    last_detokenised_at timestamptz,
    detokenise_count bigint NOT NULL DEFAULT 0
);
```

Rules:

1. **Detokenisation is an API call, and every call is logged** with `actor_id`, `purpose_code`,
   `correlation_id`. `detokenise_count` climbing on an account with no payout is an alert.
2. Only two callers may detokenise: the payout instruction builder and the DPDP access-request
   exporter. Both are single, reviewed code paths with `CODEOWNERS` protection.
3. The warehouse, OpenSearch, logs, analytics events and ML features receive **tokens only**. A
   token in a breach is inert without a second system.
4. Tokens are not deterministic across classes: the same phone tokenised for `analytics` and for
   `payout` yields different tokens, so joining datasets requires the vault, which requires
   authorisation, which is logged.
5. Erasure of a data principal deletes the token row; every downstream token then dangles harmlessly
   and consistently. **This is the cleanest erasure mechanism we have** — one DELETE renders the
   value unrecoverable everywhere it was ever copied.

### 5.6 Blind indexes and their limits

`phone_bidx = HMAC-SHA256(vault_key, normalise_e164(phone))`, unique-indexed. Supports exact
lookup (login, duplicate detection). It does **not** support prefix search, and that is fine —
nobody needs "all users whose number starts with 98". Admin search by phone works by hashing the
full input. Admin search by *partial* phone is deliberately unsupported; it is a scraping tool.

Blind indexes leak equality. A DB dump reveals *which rows share a phone number*, which for a
marketplace is a real signal (multi-account detection is a feature; multi-account inference by an
attacker is a minor leak). Accepted, documented, reviewed annually.

### 5.7 Encryption in transit

| Hop | Requirement |
|---|---|
| Client → edge | TLS 1.3 only (1.2 with AEAD ciphers permitted for legacy Android ≤ 7 via a separate SNI host, retired on <2% traffic). HSTS `max-age=63072000; includeSubDomains; preload` |
| Mobile → edge | Certificate pinning to the intermediate CA **plus** a backup pin, with a remote kill switch in the Feature Registry (`security.cert_pinning.enabled`) so a mis-issued cert doesn't brick every installed app |
| Edge → API | mTLS inside the VPC; the API rejects requests without the edge's client cert |
| API → PostgreSQL | `sslmode=verify-full`, server CA pinned |
| API → Redis | TLS + AUTH, no public endpoint, no `CONFIG` command (renamed/disabled) |
| API → OpenSearch | TLS + fine-grained roles; no PII is ever indexed |
| API → S3/R2 | TLS; bucket policy denies `aws:SecureTransport=false` |
| API → providers | TLS 1.2+; mTLS where the provider supports it (`integration_providers.auth_type='mtls'`); certificate/SPKI pinning for payment and KYC providers |
| Outbound webhooks | HTTPS only; a partner registering an `http://` URL is rejected at write time |
| Internal service → service | mTLS via the service mesh when the monolith splits; today, in-process |

### 5.8 Secrets in CI/CD, and the never-list

- No long-lived cloud credentials anywhere. CI authenticates to cloud via **OIDC federation**; the
  role is scoped per environment and cannot deploy to production without an approval gate.
- `.env` files exist only for local dev and are populated by `vault agent`; `infra/docker-compose`
  ships obviously-fake dev secrets that fail against any real endpoint.
- Secret scanning: pre-commit (`gitleaks`), push protection at the forge, and a nightly full-history
  scan. A hit is a P2 with mandatory rotation — **rotate first, investigate second**. Removing the
  commit is not remediation.
- **Never in a secret store, never in a config file, never in a JWT:** anything that is really a
  business rule. A "secret" threshold is a registry row with restricted read, not an env var.

---

## 6. DPDP Act 2023 — obligation to feature, one row at a time

### 6.1 What we are

AutoHub is a **Data Fiduciary** for buyers, sellers, dealer staff, drivers, inspectors and loan/
insurance applicants. Payment gateways, KYC vendors, SMS providers and cloud hosts are **Data
Processors** under contract. Lenders and insurers are **independent Data Fiduciaries** — data
routed to them is a disclosure under consent, not a processing instruction, and the consent notice
names them.

The Act is in force with the Rules notified and phased; the substantive notice/consent/rights/breach
machinery has a long runway before it bites. We build to it now because retrofitting consent
provenance onto three years of data is not possible. **Every date in this section is a registry
row** (`rule_sets.dpdp_*`), so when a phase commences or an SLA changes, ops edits data.

### 6.2 Obligation → feature → table → endpoint

| DPDP § | Obligation | System feature | Table / registry | Endpoint |
|---|---|---|---|---|
| 5 | **Notice** in clear plain language, itemised, before/with consent; available in English + Eighth Schedule languages | Consent screens rendered from the Form Schema Registry; notice text from the Localization Registry, versioned | `platform.form_versions`, `platform.i18n_strings`, `trust.consents.purpose_text_i18n_key` + `policy_version` | `GET /platform/forms/consent.{purpose}.v{n}` |
| 6 | **Consent** free, specific, informed, unconditional, unambiguous, with clear affirmative action; as easy to withdraw as to give | One purpose = one toggle. No pre-ticked boxes (schema validator rejects `default: true` on a consent field). Withdrawal is a single `DELETE` | `trust.consents` (grant + withdraw are separate immutable rows) | `POST/GET /identity/consents`, `DELETE /identity/consents/{purpose}` |
| 6(3) | Processing **ceases** on withdrawal, within a reasonable time | Withdrawal emits `identity.consent.withdrawn.v1`; every consumer registers a compensating action (stop campaign, purge feature-store rows, revoke partner share) | `platform.outbox_messages`, `trust.consent_withdrawal_actions` | — |
| 6(9) | **Consent receipt** / provable record | A signed receipt artefact per grant (§6.3) | `trust.consent_receipts`, `documents.documents` | `GET /identity/consents/{id}/receipt` |
| 7 | **Legitimate uses** (voluntarily provided, employment, legal obligation, etc.) | Purposes carry a `legal_basis_code`; not everything needs consent, and pretending otherwise creates consent fatigue and worse compliance | `platform.codes` set `dpdp_legal_basis` on `consent_purposes` | — |
| 8(1)–(3) | **Accountability** incl. for processors; accuracy where data drives a decision | Sub-processor register = `platform.integration_providers` + `contract_ref`; DPAs tracked with expiry | `platform.integration_providers` | `GET /platform/admin/processors` |
| 8(4) | **Reasonable security safeguards** | This entire document; evidence is the control register + audit trail | `platform.threat_register`, `platform.audit_log` | — |
| 8(5) | **Personal data breach** intimation to the Board and to each affected principal | §6.6 | `trust.breach_incidents`, `trust.breach_notifications` | `POST /platform/admin/security/breaches` |
| 8(7) | **Erase** when purpose is served or consent withdrawn, unless retention is legally required | Retention driven by `pii_registry.retention_months`; the erasure executor produces per-table verdicts (`03` §9.2) | `trust.erasure_requests.blocked_by` | `POST /identity/privacy/erasure` |
| 8(9) | **Grievance redressal**; publish contact of DPO / person able to answer | In-app grievance form, ticket SLA, contact published in-app and on the site footer | `trust.grievances` (workflow `grievance`), `platform.settings` | `POST /identity/privacy/grievances` |
| 9 | **Children** (<18): verifiable parental consent; no tracking, behavioural monitoring, or targeted ads | §6.8 | `identity.parties.dob_year`, `trust.age_assurance` | — |
| 10 | **Significant Data Fiduciary** obligations | §6.7 | `platform.settings.sdf_designated` | — |
| 11 | **Right to access**: summary of data, processing activities, and identities of fiduciaries data was shared with | Self-service export bundle (§6.5) | `trust.data_requests` | `POST /identity/privacy/access` |
| 12 | **Right to correction / completion / updating / erasure** | Profile edit with an audit trail; erasure workflow | `trust.data_requests`, `trust.erasure_requests` | `PATCH /identity/me`, `POST /identity/privacy/erasure` |
| 13 | **Right to grievance redressal**, exhausted before approaching the Board | `grievance` workflow with `sla_minutes` and escalation | `trust.grievances` | as above |
| 14 | **Right to nominate** | Nominee capture + a documented invocation path on death/incapacity | `trust.nominations` | `POST /identity/privacy/nominations` |
| 15 | **Duties of the Data Principal** (no false particulars, no frivolous grievances) | Surfaced in the T&C; repeated frivolous requests are rate-limited, never refused outright | `rate_limit_policies` bucket `privacy_request` | — |
| 16 | **Cross-border transfer** — permitted except to notified restricted countries; sectoral rules may be stricter | §6.9 | `platform.integration_providers.data_region` | — |
| 17 | Exemptions (legal claims, court orders, prevention/detection of offences) | Erasure verdicts cite the exemption; legal hold blocks purge | `documents.legal_hold`, `workflow_instances.is_locked` | — |

### 6.3 Consent capture and the consent receipt

A consent grant that cannot be reproduced *as the user saw it* is not provable. So we store the
render, not just the flag.

```sql
CREATE TABLE trust.consent_receipts (
    id            uuid PRIMARY KEY DEFAULT platform.uuid_generate_v7(),
    consent_id    uuid NOT NULL REFERENCES trust.consents(id),
    receipt_ref   text NOT NULL,                    -- 'CNS-2607-9K2X'
    form_version_id uuid NOT NULL REFERENCES platform.form_versions(id),
    locale        text NOT NULL,                    -- what language they actually read
    rendered_notice_sha256 platform.sha256_hex NOT NULL,
    rendered_notice_document_id uuid,               -- the exact HTML/PDF, in object storage, WORM
    purposes      jsonb NOT NULL,                   -- [{"code":"finance.bureau_pull","granted":true,...}]
    processors_disclosed jsonb NOT NULL,            -- provider codes named in the notice at that time
    captured_at   timestamptz NOT NULL DEFAULT now(),
    captured_channel platform.code_key NOT NULL,    -- 'web','android','ios','ivr','in_person'
    evidence      jsonb NOT NULL DEFAULT '{}',      -- {"ip_hash","ua_hash","screen","interaction_ms"}
    signature     bytea NOT NULL,                   -- platform signature over the canonical receipt
    CONSTRAINT uq_consent_receipts__ref UNIQUE (receipt_ref)
);
```

- The receipt is **signed by the platform** (`crypto_keys.purpose_code='consent_receipt'`), so a
  receipt produced in a dispute is verifiable by a third party without trusting our database.
- `interaction_ms` under a threshold (`rule_sets.consent_min_dwell_ms`, default 800) flags a possible
  dark pattern or scripted grant. Consent obtained in 40 ms is not informed consent.
- The receipt is downloadable by the data principal at any time, and is included in the access
  export.
- **Bundled consent is structurally impossible:** the `SchemaForm` renderer rejects a consent form
  whose submit is enabled only when all toggles are on. A CI test (`ConsentMayNotBeBundled`) checks
  every published consent form version.

Consent purposes at launch (`platform.codes` set `consent_purpose`, each with `legal_basis_code`):

| `purpose_code` | Legal basis | Blocks what if absent |
|---|---|---|
| `identity.core` | contract/legitimate use | Nothing — you cannot use the platform without it, and that is lawful |
| `kyc.verification` | legal obligation (via PA/insurer chain) | Dealer onboarding, payouts, high-value deals |
| `finance.bureau_pull` | consent | Loan eligibility with a hard enquiry |
| `finance.share_with_lender` | consent, names the RE | Routing an application |
| `insurance.share_with_insurer` | consent, names insurers | Quote fan-out |
| `profile.share_with_dealer` | consent | Contact reveal to a dealer |
| `marketing.promotional` | consent | Any promotional SMS/WhatsApp/email/push |
| `vehicle.telemetry` | consent | Telematics-based rental scoring |
| `location.precise` | consent | Nearest-inspection-centre, test-drive routing |
| `fraud_prevention` | legitimate use | Nothing (cannot be withdrawn; it is not consent-based) |
| `analytics.behavioural` | consent | Personalised ranking; the platform still works without it |

### 6.4 Purpose limitation, actually enforced

Purpose is not a label on a form; it is a **key on the data path**.

1. Every `provider_routes` row declares `purpose_code`. An adapter call whose ambient purpose does
   not match the route's purpose throws before the HTTP call is made.
2. The feature store (`16` A9.13) partitions features by purpose. A feature computed under
   `fraud_prevention` cannot be read by an `insurance_quote` model — enforced at the read API, not by
   convention. This is `16` F16, restated because it is a DPDP obligation, not a nicety.
3. The warehouse loads `purpose_code` onto every fact row sourced from consented data; a BI query
   without a purpose filter over a purpose-tagged table is blocked by a row-level policy.
4. Consent withdrawal fires `trust.consent_withdrawal_actions` rows — one per registered consumer —
   each with its own workflow and SLA. "We stopped sending marketing" is a completed row, not a
   promise.

### 6.5 Data-principal rights — the operational shape

All four rights share one table and one workflow definition (`dpdp_request`), differing by
`request_kind`:

```sql
CREATE TABLE trust.data_requests (
    id            uuid PRIMARY KEY DEFAULT platform.uuid_generate_v7(),
    public_ref    text NOT NULL,                   -- 'DPR-2607-3T7Q'
    party_id      uuid NOT NULL REFERENCES identity.parties(id),
    request_kind  platform.code_key NOT NULL,      -- 'access','correction','erasure','nomination','withdrawal'
    channel_code  platform.code_key NOT NULL,      -- 'app','web','email','grievance_officer','board_referral'
    workflow_instance_id uuid NOT NULL REFERENCES platform.workflow_instances(id),
    state_code    platform.code_key NOT NULL,
    identity_verified_at timestamptz,
    identity_verification_method platform.code_key, -- 'session_step_up','otp','kyc_match','manual'
    payload       jsonb NOT NULL DEFAULT '{}',      -- correction fields, nominee details, erasure scope
    due_at        timestamptz NOT NULL,             -- from rule_sets.dpdp_response_sla
    responded_at  timestamptz,
    outcome_code  platform.code_key,                -- 'fulfilled','partially_fulfilled','refused'
    outcome_reason_code platform.code_key,
    export_document_id uuid,
    created_at timestamptz NOT NULL DEFAULT now(), created_by uuid,
    updated_at timestamptz NOT NULL DEFAULT now(), updated_by uuid,
    row_version bigint NOT NULL DEFAULT 1,
    CONSTRAINT uq_data_requests__ref UNIQUE (public_ref)
);
```

**Workflow `dpdp_request` — states are registry rows, not an enum:**

| `code` | `kind_code` | Flags | `sla_minutes` | Notes |
|---|---|---|---|---|
| `submitted` | initial | `counts_as_open` | 60 | |
| `identity_pending` | waiting | `counts_as_open` | 1440 | Step-up not yet completed |
| `validated` | active | `counts_as_open` | — | |
| `assessing` | active | `counts_as_open` | from `rule_sets.dpdp_response_sla` | Legal-retention verdicts computed |
| `awaiting_principal` | waiting | `counts_as_open` | 10080 | We asked a clarifying question; clock documented as paused |
| `executing` | active | `counts_as_open`, `blocks_deletion` | 1440 | |
| `fulfilled` | terminal_success | — | — | |
| `partially_fulfilled` | terminal_success | — | — | Response quotes every `blocked_by` basis |
| `refused` | terminal_failure | — | — | Requires `outcome_reason_code` + reviewer |
| `withdrawn_by_principal` | terminal_failure | — | — | |

**Access (§11).** Export bundle is a ZIP containing: `profile.json`, `listings.json`, `deals.json`,
`payments_statement.pdf`, `documents/` (their own uploads), `consents.json` + every consent receipt,
`processors.json` (**who we shared with and what** — this is the part most implementations forget),
and `README.md` explaining each file in the user's locale. Generated asynchronously, delivered as a
one-time signed URL with a 24 h TTL to the verified channel, never as an email attachment. Step-up
required. Free of charge, and rate-limited at 2/day (`04` §8 `export` bucket) — a rate limit is not
a refusal, and the copy says so.

**Correction (§12).** Most corrections are just profile edits, which is the point — a right that
requires a ticket is a right that gets ignored. Fields whose correction has downstream effects
(name after KYC, registration number after a listing is live) route to review with the reason
surfaced: *"Changing the name on a verified KYC record needs a document. Upload your PAN or Aadhaar
to continue."*

**Erasure (§8(7), §12).** Mechanics are `03` §9.2. This document adds the process: erasure is
four-eyes (`trust.dpdp.approve`, `16` D6), it bumps `token_epoch` (all sessions die), it deletes the
token-vault rows first (§5.5 rule 5), and the completion notice enumerates exactly what was retained
and under which statute. Where the data principal disputes a retention verdict, the grievance
workflow is the next step — and the response tells them so, with the Board as the escalation after
that. Copy, per design-system voice: *"We deleted your profile, listings and uploaded documents. We
kept your tax invoices — GST law requires us to hold them for 72 months. Nothing in them is used to
contact you."*

**Nomination (§14).** `trust.nominations(party_id, nominee_name_enc, nominee_relation_code,
nominee_contact_token, verified_at, invoked_at, invocation_evidence_document_id)`. Invocation
requires a death certificate or legal-incapacity document, four-eyes, and grants the nominee the
access and erasure rights only — never transactional control. A nominee cannot sell the deceased's
listed car through the platform; that is a legal-heir process outside our scope, and the copy says
so plainly rather than pretending.

### 6.6 Breach notification

```sql
CREATE TABLE trust.breach_incidents (
    id            uuid PRIMARY KEY DEFAULT platform.uuid_generate_v7(),
    public_ref    text NOT NULL,                    -- 'BRC-2607-1A9F'
    detected_at   timestamptz NOT NULL,             -- when we first *noticed*; the clock starts here
    occurred_from timestamptz, occurred_to timestamptz,
    workflow_instance_id uuid NOT NULL,
    state_code    platform.code_key NOT NULL,       -- workflow 'breach_incident'
    severity_code platform.code_key NOT NULL,       -- 'sev1'..'sev4'
    nature_code   platform.code_key NOT NULL,       -- 'confidentiality','integrity','availability'
    root_cause_code platform.code_key,
    affected_principal_count integer,
    affected_data_classes text[] NOT NULL DEFAULT '{}',   -- pii_class values
    is_personal_data_breach boolean NOT NULL,       -- DPDP trigger
    cert_in_reportable boolean NOT NULL,            -- CERT-In Direction trigger (§8.6)
    rbi_reportable boolean NOT NULL DEFAULT false,
    irdai_reportable boolean NOT NULL DEFAULT false,
    board_intimated_at timestamptz,                 -- DPDP Board, initial
    board_detail_due_at timestamptz,                -- detected_at + rule_sets.dpdp_board_detail_hours
    board_detail_sent_at timestamptz,
    cert_in_due_at timestamptz,                     -- detected_at + rule_sets.cert_in_report_hours
    cert_in_sent_at timestamptz,
    principals_notified_started_at timestamptz,
    principals_notified_completed_at timestamptz,
    containment_at timestamptz, eradication_at timestamptz, recovery_at timestamptz,
    postmortem_document_id uuid,
    created_at timestamptz NOT NULL DEFAULT now(), created_by uuid,
    updated_at timestamptz NOT NULL DEFAULT now(), updated_by uuid,
    row_version bigint NOT NULL DEFAULT 1
);
```

**The clocks, as registry rows (`rule_sets.breach_notification_sla`):**

| Recipient | Trigger | Deadline | Content | Registry key |
|---|---|---|---|---|
| **Each affected Data Principal** | Any personal data breach | **Without delay** — target ≤ 24 h from confirmation | Nature, extent, timing, likely consequences, mitigation taken, what *they* should do, our contact | `dpdp_principal_notify_hours` = 24 |
| **Data Protection Board** — initial | Any personal data breach | **Without delay** on becoming aware | Description, nature, extent, timing, location, likely impact | `dpdp_board_initial_hours` = 6 |
| **Data Protection Board** — detailed | Same incident | **72 h** from awareness (extendable only on the Board's allowance) | Events/circumstances/reasons, mitigation, remedial measures, findings on who caused it, the intimations sent to principals and a report on them | `dpdp_board_detail_hours` = 72 |
| **CERT-In** | Any incident in the notified list (targeted scanning, unauthorised access, data breach/leak, identity theft, attacks on payment systems, ransomware…) | **6 h** of noticing | CERT-In's prescribed format, to `incident@cert-in.org.in` / the portal | `cert_in_report_hours` = 6 |
| **Payment aggregator / bank** | Anything touching payment flows | Per contract, typically ≤ 6 h | Per contract | `pa_notify_hours` = 6 |
| **IRDAI + CERT-In** (insurance arm) | Cyber incident affecting policyholder data | **6 h** | Per IRDAI cyber guidelines | `irdai_notify_hours` = 6 |
| **Affected partners (lenders/insurers)** | Their data principals affected | Per DPA, typically 24 h | Scope + remediation | `partner_notify_hours` = 24 |

**Six hours is the binding constraint, and it is short.** So the runbook (§11.2) front-loads a
"notify now, refine later" step: the CERT-In initial report goes out on *suspicion with evidence*,
not on completed forensics. Under-reporting is a statutory offence; over-reporting is an
inconvenience. The workflow enforces it — `breach_incident` state `triaged` has
`sla_minutes = 300` with `sla_breach_transition_code = 'escalate_to_ciso'`, so a stalled triage
pages before the 6 h clock expires.

Principal notification is a Notification Registry event (`security.data_breach_notice`) with
templates per channel and locale, pre-approved and DLT-registered **in advance** — a breach is not
the moment to discover your SMS template needs 48 h of DLT approval. This is a real, boring,
India-specific failure mode and the mitigation is: register the templates on day one.

### 6.7 Significant Data Fiduciary posture

We assume SDF designation (volume of personal data + risk to rights + electoral/public-order factors
are the notified criteria; a national vehicle marketplace with KYC, financial and location data at
scale is a plausible candidate). Building for it costs little now and is expensive to retrofit.

| §10 obligation | Implementation | Evidence artefact |
|---|---|---|
| **DPO** based in India, reporting to the board, contact published | Named role with `platform.settings.dpo_*`; the grievance workflow escalates to the DPO by row, not by email convention | Board minutes; DPO listed in-app and on site |
| **Independent data auditor** + annual **data audit** | Annual engagement; auditor gets a read-only warehouse role and the full control register | Audit report, filed |
| **DPIA** for high-risk processing | Mandatory DPIA before any new purpose, new provider, new ML model touching personal data. `platform.dpia_records` linked to `integration_providers` and `ml_model_versions` — a provider route cannot go `active` without a completed DPIA row | DPIA per processing activity |
| **Algorithmic due diligence** — algorithms must not risk data-principal rights | Every model in the model registry (`16` B1) carries fairness/robustness evaluation records; pricing and risk models are reviewed for proxy discrimination (city, name-derived community signals are **excluded features** by policy, asserted by a feature allow-list test) | Model cards |
| **Restricted cross-border transfer** of notified categories | `data_region` on every provider; a provider outside India cannot be routed for a capability marked `residency_required` | Provider register |
| Periodic compliance measures | Quarterly control-effectiveness review, tracked in `platform.threat_register` review dates | Review log |

### 6.8 Children's data

Vehicle purchase, rental and driving are adult activities, so our position is simple and defensible:
**AutoHub is not intended for anyone under 18.**

| Control | Mechanism |
|---|---|
| Age assurance at signup | `identity.parties.dob_year` collected in the profile form; a DL or PAN in KYC provides a stronger signal. `trust.age_assurance(party_id, method_code, asserted_dob_year, confidence, verified_at)` |
| Under-18 detected | Account moves to a `restricted_minor` state: no listings, no bids, no bookings, no finance/insurance, no marketing of any kind. Data is minimised to what is needed to keep the block effective |
| Behavioural monitoring / tracking | Disabled for any account flagged minor or with unknown age below the confidence threshold — analytics events are dropped at the ingest gate, not filtered later |
| Targeted advertising | Never for minors. Our ad/recommendation surfaces read a single `may_personalise` flag that is false for minors, unknown-age, and anyone who withdrew `analytics.behavioural` |
| Verifiable parental consent | Not implemented, because we do not offer a service to children. If a product ever requires it (e.g. a learner-driver module), it is a DPIA-gated project, not a feature toggle |
| Test-drive with a minor present | Out of scope for data purposes; inspector/driver notes may not record minors' details — the inspection form schema has no such field |

The honest risk is a 17-year-old lying about their birth year. The control is proportionate: we do
not build invasive age verification for a service minors have no reason to use, and we act
decisively on any signal that surfaces.

### 6.9 Cross-border transfer and residency

DPDP permits transfer except to countries the Central Government notifies as restricted — a
comparatively liberal rule. **Sectoral rules are stricter, and we design to the strictest applicable
one, not the loosest** (`16` D7.4 states this for backups; here it is stated as the general policy).

| Data class | Where it may live | Binding rule |
|---|---|---|
| Payment-related data (transaction details, payment credentials, the escrow mirror) | **India only**, end-to-end | RBI payment-data storage directive |
| Policyholder / insurance records | **India only** | IRDAI records maintenance + cyber guidelines |
| Aggregator/ride data if the attachment module ever dispatches | **India only**, 3–24 months | MoRTH Aggregator Guidelines |
| KYC artefacts, ICT logs | **India only** | CERT-In log localisation (180 days), PMLA-adjacent contractual terms |
| Everything else personal | India by default | DPDP §16 + our own policy |
| Fully anonymised aggregates | Anywhere | Not personal data — but "anonymised" means k-anonymity ≥ 20 on the release, checked by a job, not asserted by a developer |

Enforcement: `platform.capabilities` gains `residency_required boolean`; `provider_routes` cannot
bind a provider whose `integration_providers.data_region <> 'IN'` to a residency-required capability.
A DB CHECK plus a route-publish guard. Sub-processor onboarding requires a DPIA, a DPA with the
DPDP-mandated processor terms, a security questionnaire, and a region attestation.

### 6.10 Penalties, and what that buys

Schedule penalties run to **₹250 crore** for failure to take reasonable security safeguards,
**₹200 crore** for breach-notification failure and for children's-data failures, **₹150 crore** for
SDF obligations, and up to **₹50 crore** otherwise. Two consequences for engineering:

1. The security budget line is not compared to the cost of a breach; it is compared to the penalty
   for *not having controls*, which is assessed on the failure itself.
2. Cyber-liability insurance is bought, but it does not cover regulatory penalties in most Indian
   policies. Controls are the only mitigation for the largest number on this page.

---

## 7. RBI — what a marketplace may and may not do with money

### 7.1 Restated posture

`14` §1 is the authority; the security-relevant consequence is: **AutoHub is a merchant on a licensed
Payment Aggregator, an LSP for lending, and a Direct Broker for insurance. It is not a PA, not an
NBFC, not an insurer.** Every control here defends that boundary, because crossing it accidentally
is an unlicensed-activity problem, not a bug.

### 7.2 Escrow — the account rules that shape the code

Under the PA/PG Directions, a non-bank PA must maintain customer funds in an **escrow account with a
scheduled commercial bank**, with a closed list of permitted credits and debits:

| Permitted **credits** | Permitted **debits** |
|---|---|
| Payments from customers for merchandise/services | Payment to merchants |
| Pre-funding by merchants / the PA | Payment to any other account per a pre-determined agreement |
| Transfers per pre-determined agreement | Refunds to customers for returns/cancellations |
| Lien-marked deposits from the PA's own bank | Payment of the PA's commission — into the PA's **own** account, at agreed intervals |
| Reversals for failed/disputed/returned transactions | Payment of amounts received erroneously |

The escrow is **not the PA's money and cannot be used as float**. Our `payments.ledger_accounts` rows
with `account_code='escrow_holding'` are a **mirror** of a balance held under someone else's licence
— which is why `ck_ledger_accounts__escrow_backing` forbids `external_kind='current'`. Two additional
controls this document requires:

1. **Daily three-way reconciliation** — PA settlement report ↔ `payments.ledger_entries` ↔ bank
   statement. A break older than one business day is a P1 finance case (`16` D7.3 step 9's daily
   equivalent), never a silent adjustment.
2. **The mirror can never be authoritative.** A read of "escrow balance" that has not been
   reconciled within `rule_sets.escrow_recon_max_age_hours` (default 26) renders with an explicit
   staleness marker in admin, and blocks manual release. A number nobody has checked against the
   bank is a rumour, and the design system already says numbers carry their date.

For high-value deals above the registry threshold, Structure 2 (tri-party bank escrow with a named
escrow agent) applies. Its release instructions are dual-signed, and the instruction file is
generated from the ledger, hash-logged, and stored WORM.

### 7.3 Settlement timelines

| Leg | Timeline | Our obligation |
|---|---|---|
| Customer → PA escrow | Instant (UPI/card/netbanking) | Reconcile the capture, never trust the webhook amount |
| PA escrow → merchant (us, for our own fees) | **Ts+1** where Ts is the date of the charge, per the Directions (subject to the PA's agreed cycle) | Monitor; a delayed settlement is a PA issue, but *our* books must show the receivable |
| Escrow hold for a deal | `escrow_max_hold_days` (default 7), hard ceiling 30 | Auto-refund on ceiling breach (§2.2) |
| Payout to seller/dealer | Same business day as the release transition, batched | Four-eyes above ₹5,00,000 |
| Refunds | Initiated same day as the refund transition; credited per rail (UPI ~instant, card 5–7 working days) | Communicate the rail's timeline, not ours |
| Insurance premium (collect-and-remit) | `remittance_window_hours` per insurer (`14` §4.8) | Hard timer transition; premium may not sit |

### 7.4 The "must not" list, each with its enforcement point

| The platform must **not** | Why it matters | Enforcement |
|---|---|---|
| Hold customer funds in an account it operates | Unlicensed PA activity | `ck_ledger_accounts__escrow_backing` |
| Hold funds beyond the permitted/agreed window without a documented dispute | Same | Auto-refund timer transition + `payments.settlement_overdue` |
| Use escrow balances as working capital or earn on the float in its own books | Same | No ledger account maps escrow interest to income; the chart of accounts has no such row |
| Route loan principal through any AutoHub account | RBI Digital Lending: no pass-through via an LSP pool account | No journal template debits escrow for loan principal (`14` §1) |
| Charge the borrower a facilitation fee | Digital Lending: LSP is paid by the RE | DB CHECK on `fee_components` |
| Give a default-loss guarantee beyond a permitted, capped, structured arrangement | DLG rules | No contingent-liability account exists |
| Net off its commission before crediting the seller without a disclosed, agreed fee schedule | Consumer + contractual | Fee breakdown is rendered from `fee_components` with `explain_i18n_key` before the deal is accepted |
| Accept or record a cash leg of ₹2,00,000 or more | **§269ST, Income-tax Act** — penalty equal to the amount received, on the *receiver* | `ck_deal_payments__cash_limit CHECK (method_code <> 'cash' OR amount_paise < 20000000)`, plus the UI has no path to record it |
| Facilitate a 4-wheeler sale/purchase without a PAN (or Form 60) on record | **Rule 114B** — PAN is mandatory for sale/purchase of a motor vehicle other than a two-wheeler, *at any value* | Deal workflow guard: `sale` intent for a non-two-wheeler category requires `kyc_level >= 2` or a Form 60 document |
| Store card data | PCI scope | §5.4 |

### 7.5 KYC / AML obligations, honestly scoped

AutoHub is **not** a Reporting Entity under the PMLA by virtue of being a marketplace. Obligations
reach us through three real channels, and we implement to the strictest:

1. **Merchant onboarding CDD by the PA.** The PA must KYC us and, for a marketplace, expects us to
   perform onboarding diligence on the sellers we settle to. Dealers therefore go through full KYC
   (PAN + GSTIN + bank penny-drop + beneficial-owner check for entities) before any payout account
   is enabled.
2. **The insurance broking arm.** IRDAI's AML/CFT expectations apply to intermediaries: customer
   due diligence, record maintenance, and suspicious-transaction awareness for premium flows.
3. **Contractual flow-down** from lenders (as their DSA/LSP) — their own KYC/AML standards apply to
   the data we collect and forward.

Concrete controls: PEP and sanctions screening at KYC (`trust.kyc_records.pep_match`,
`sanctions_match`) with re-screening on a `rule_sets.sanctions_rescreen_days` cadence; structuring
detection (many sub-threshold deals between the same pair) as a Trust rule; record retention 5 years
post-relationship (`03` §9.3); and an internal escalation path for a suspicious pattern that ends
with the compliance officer, not with an automated filing we are not entitled to make.

### 7.6 Digital lending — the security-relevant bits

- **KFS and APR come from the RE.** We display; we do not compute. A null APR blocks the offer card
  (`14` F4). This is an anti-mis-selling control that also prevents us from becoming the source of a
  regulated disclosure.
- **Data minimisation is mandated, not optional.** Digital lending rules restrict collection to what
  is need-based with audit trails; no access to phone contacts, media, or call logs. The Flutter app
  requests **camera and location only**, both at point of use, both with an in-context rationale.
  There is no `READ_CONTACTS` permission in the manifest, and a CI check on the merged manifest
  fails the build if one appears — this is the single most-abused permission in Indian lending apps
  and the easiest to get wrong via a third-party SDK.
- **Cooling-off / look-up period** and grievance escalation to the RE's nodal officer are surfaced
  in the app, sourced from the lender's registry row, not hardcoded.

---

## 8. The other regulators

### 8.1 IRDAI (insurance)

| Obligation | Control | Enforcement |
|---|---|---|
| Act as a licensed **Direct Broker (General)**; solicit only through the licensed path | No surface outside the Insurance module may quote, compare or recommend insurance — including the chatbot and campaign engine (`16` F17) | `rule_sets.assistant_prohibited_topics`; feature gate |
| **§64VB**: no risk assumed before premium receipt | Issuance transitions are downstream of *realised* payment, not of an initiated payment | Workflow guard |
| **§41**: no rebating of premium or brokerage | Promotions cannot target `module_code='insurance'` | Feature Registry gate (`14` I3) |
| We may not display a premium we computed | `ck_quotes__premium_source CHECK (premium_source_code='insurer')` | DB constraint |
| Policyholder records maintained in India, for the prescribed period | India-pinned storage; retention per `pii_registry` | Provider `data_region` |
| **Cyber security**: board-approved policy, CISO, annual audit, VAPT, incident reporting within 6 h | §11, §12 | Process + `breach_incidents.irdai_reportable` |
| Individuals soliciting must hold a valid licence/certificate | `identity.party_credentials` with `valid_to`; expiry auto-gates the capability (`16` F19) | Credential expiry job |

### 8.2 GST and e-invoicing

| Item | Treatment | Registry |
|---|---|---|
| Platform commission / service fees | Taxable supply of service, 18% | `platform.tax_rules` rows, effective-dated |
| Place of supply | B2B: recipient's location (dealer's GSTIN state) → IGST/CGST+SGST determined by rule | `rule_sets.gst_place_of_supply` |
| Used-vehicle sale **by a registered dealer** | **Margin scheme** — GST on (selling price − purchase price/depreciated WDV); 18% uniform for used vehicles following the 55th Council change; **negative margin ⇒ no GST** | `fee_components` + `tax_rules` with `basis='margin'` |
| Used-vehicle sale **by an unregistered individual (C2C)** | No GST on the vehicle. GST applies only to our service fee | Rule outcome `{"vehicle_gst":"not_applicable"}` |
| **e-invoicing (IRN)** | Mandatory for B2B supplies once AATO crosses the notified threshold (₹5 crore). IRN + signed QR from the IRP before the invoice is valid | `capabilities.einvoice.irp`, `provider_routes` |
| **30-day reporting limit** | Invoices must be reported to the IRP within 30 days of the document date for taxpayers at/above the notified AATO | A job that alerts at day 20 and hard-blocks manual back-dating beyond the window |
| **e-way bill** | Required when a vehicle moves **as goods** (on a carrier) above the value threshold; not when driven under its own power under a valid registration | `capabilities.ewaybill.nic`, decided by a rule set, not a developer |
| Credit notes | Only via credit note; **an invoice is never edited** (`ck_invoices__irn`, append-only) | DB |

Security-relevant consequence: `payments.invoices` is immutable once an IRN is issued, the invoice
number series is **gapless** (`uq_invoices__number`, and a collision pages on-call per `03` §11),
and invoices are stored WORM with object-lock. A tax invoice that can be silently altered is
evidence destruction, and the storage layer makes it impossible rather than the policy discouraging
it.

### 8.3 TCS and TDS on vehicle transactions

| Provision | Who | Rate | Trigger | How we implement |
|---|---|---|---|---|
| **§206C(1F)** TCS on motor vehicle | **The seller** collects from the buyer | 1% | Sale consideration of a motor vehicle **exceeding ₹10,00,000** | We are not the seller in C2C or dealer-direct sales — so we **compute and display** the buyer's TCS liability and the seller's collection obligation, and we generate the entry only where an AutoHub group entity is itself the seller (assured/owned inventory) |
| **§206C(1F)** extension to notified goods > ₹10 lakh | Seller | 1% | Notified luxury goods, w.e.f. 1 Jan 2025 | Same rule engine, different `tax_rules` rows |
| **§206C(1H)** TCS on sale of goods > ₹50 lakh | Seller | — | **Omitted w.e.f. 1 Apr 2025** | The row exists with `effective_to` set — historical invoices must remain reproducible |
| **§194-O** TDS by an e-commerce operator | **AutoHub**, where it facilitates a sale of goods/services by an e-commerce participant | 0.1% of gross amount (w.e.f. 1 Oct 2024) | Facilitated sale to an e-commerce participant, subject to the individual/HUF exemption threshold | This one **is** ours. A `tax_rules` row with `payer_role_code='platform'`; deducted at the settlement transition and posted to `tds_payable` |
| **§194Q** TDS on purchase of goods | Buyer (business) | 0.1% | Purchases > ₹50 lakh from a resident | Displayed for dealer buyers; interaction with 206C(1H) omission handled by rule priority |
| PAN / Form 60 | Both parties | — | Any 4-wheeler sale/purchase (Rule 114B) | Deal guard (§7.4) |

**Every rate, threshold and effective date above is a `platform.tax_rules` row with
`effective_from`/`effective_to`.** Not one of them is a constant in code. A Finance Act changes these
annually; a marketplace that hardcodes 1% ships a hotfix every February. And because pricing/tax
rows are **temporal** (`03` §3.12) — closed, never updated — a three-year-old invoice can still be
reproduced exactly for an assessment.

### 8.4 TRAI DLT (TCCCPR) — SMS and voice

| Requirement | Implementation |
|---|---|
| Register as a **Principal Entity** on an operator DLT platform; register **headers** (sender IDs) and **content templates** | `notification_templates.dlt_template_id`, `dlt_header_id`, `dlt_entity_id`. `ck_notification_templates__dlt CHECK (channel_code <> 'sms' OR dlt_template_id IS NOT NULL)` — an SMS template without a DLT id **cannot be published** |
| Correct message **category** (transactional / service-implicit / service-explicit / promotional) | `notification_templates.dlt_category_code`; OTP and transaction alerts are transactional, and a rule blocks any marketing content in a transactional template (a keyword deny-list check at publish time) |
| **Consent / preference scrubbing** against DND (NCPR) before promotional sends | Send pipeline checks `trust.consents` for `marketing.promotional` **and** the operator's preference response; a send that passes our consent but fails NCPR is dropped and logged |
| **Traceability of the message chain** (Feb 2025 amendments) — untraceable telemarketer chains are rejected | One registered chain per header; provider fallback (`msg91` → `gupshup`) requires the template registered on **both**, or the fallback is disabled for that template. This is a real operational trap: a fallback provider without the template silently fails delivery |
| **URL / APK / OTT link whitelisting** | Only whitelisted domains may appear in an SMS; the template renderer rejects an unwhitelisted link at publish and at send |
| Variable content limits | Template variables validated against the registered pattern; overflow truncates deterministically rather than failing the send |
| Complaint handling and record retention | `notification.messages` retained 12 months hot / 3 years archived (`03` §9.3) with delivery receipts |

WhatsApp is outside DLT but inside Meta's template approval — the same registry columns
(`provider_template_ref`) hold it, so the constraint generalises.

### 8.5 RTO, ownership transfer and vehicle-legal reality

AutoHub facilitates; the statutory acts are the parties'. But the platform's guards must reflect the
law or it will facilitate an invalid transfer.

| Step | Legal basis | Form / artefact | Platform behaviour |
|---|---|---|---|
| Seller reports transfer | MV Act §50 | **Form 29** (notice of transfer) | Generated pre-filled from the vehicle record; the seller uploads the acknowledged copy |
| Buyer applies for transfer | MV Act §50 | **Form 30** (report of transfer) | Generated; deal guard requires the acknowledgement before escrow release for `sale` |
| Timelines | §50 | Within **14 days** (same-state) / **45 days** (other state, after NOC) for the transferor's report; **30 days** for the transferee | `deals` workflow timer states with `sla_minutes` from the registry; breach notifies both parties and escalates to RC-transfer-assist ops |
| Inter-state move | §48 | **Form 28** NOC from the original RTO | Required document type on the deal when buyer and seller states differ |
| Hypothecation termination | CMVR | **Form 35** + financier NOC | Escrow release for a hypothecated vehicle is blocked until Form 35 + NOC are on file, or the loan is being closed as part of the deal — this is the single most common real-world dispute in used-car sales |
| Hypothecation addition (new loan) | CMVR | **Form 34** | Finance module posts it after disbursal |
| Insurance transfer | Motor insurance rules | Endorsement within the prescribed window (typically 14 days) | Insurance module reminder; an uninsured vehicle handed over is a liability event |
| **HSRP** | MoRTH mandate | High-Security Registration Plate + colour-coded fuel sticker | Vehicle record carries HSRP status; listings show it, and `PlateBlock` renders the real plate class (white/yellow/green — `01` §5) |
| **PUC** | MV Act §190(2) | Valid PUC certificate | Required for transfer; expiry surfaced on the listing |
| **Challans** | e-Challan (NIC) | Outstanding challan check | VAHAN/e-Challan pull at listing and again pre-release; outstanding above `rule_sets.challan_release_threshold` blocks release |
| **Blacklist / theft / scrappage** | VAHAN | RC status | `vahan_rc_status_adverse` is **tri-state** (`16` F15) — `unknown` never renders as clear |
| **BH series / commercial permits** | MV Act §66, BH rules | Permit validity | Attachment module gates commercial listings on a valid permit |

**Data-protection angle:** VAHAN/PARIVAHAN and e-Challan pulls are personal-data disclosures about
the registered owner. Each pull is bound to a `purpose_code` on the `provider_routes` row, requires
the seller's consent at listing time (or is performed only against a registration number the user
has proven control of), is rate-limited per party, and is logged. Bulk enumeration of registration
numbers through our VAHAN route would be both a contract breach and a privacy incident, so the
capability has a hard per-party daily cap and an anomaly alert on distinct-plate velocity.

**Aggregator boundary (attachment module).** MoRTH's Motor Vehicle Aggregator Guidelines impose
licensing, driver verification, insurance, fare and data-localisation obligations on anyone who
*dispatches* rides. AutoHub's taxi-attachment centre **matches vehicles to fleets and aggregators
and stops there**. The enforcement point is architectural: there is no ride-dispatch endpoint, no
fare computation, and no trip entity in the domain model. If that ever changes, it is a licensed
product, not a sprint.

### 8.6 CERT-In and the IT Act

| Direction / rule | Obligation | Implementation |
|---|---|---|
| CERT-In Directions (§70B(6), IT Act) | Report listed incident types **within 6 hours of noticing** | §6.6 clock; `cert_in_reportable` flag; a named PoC registered with CERT-In |
| Same | Maintain **ICT system logs for a rolling 180 days**, **within India** | Centralised log store in an Indian region, 180-day hot retention minimum, immutable object-lock tier; explicitly not shipped to a foreign SIEM region |
| Same | **NTP synchronisation** to NIC/NPL servers or traceable to them | All hosts sync to NPL/NIC-traceable NTP; drift > 1 s pages, because a bid timestamp that cannot be defended is a legal problem |
| Same | Designate a PoC; respond to CERT-In orders | Named in the IR runbook |
| SPDI Rules 2011 (§43A) | Reasonable security practices — ISO 27001 or an approved code | ISO 27001-aligned ISMS; SoA maintained |
| §72A IT Act | Penalty for disclosure of information in breach of a lawful contract | Contractual + the controls in §4/§5 |
| Consumer Protection (E-Commerce) Rules 2020 | Grievance officer; acknowledge in **48 hours**, redress within **one month**; display seller details; no unfair trade practice | `trust.grievances` workflow with those SLAs as registry rows; seller identity and terms shown on every listing |

---

## 9. Audit logging

### 9.1 What is logged

`platform.audit_log` (`03` §7.3) covers every registry table, `payments.*`, `deals.*`, `trust.*`,
`identity.users`, `identity.organizations`, and anything governed by a permission with
`risk_level >= 3`. This document adds three categories that a data-change trigger cannot see:

| Category | Examples | Where |
|---|---|---|
| **Read events on sensitive data** | KYC full read, document download, contact reveal, data export, detokenisation, audit-log search itself | `audit_log` with `action_code` ending `.read`, `result_code='success'`, and a mandatory `reason_code` where the permission demands one |
| **Authentication and session events** | OTP start/verify, login, refresh rotation, reuse detection, device add/block, step-up pass/fail, logout-all, epoch bump | `identity.security_events` (partitioned, 24 months), summarised into `audit_log` for high-risk items only — auth volume would otherwise drown the audit log |
| **Authorisation denials** | 403s on `is_sensitive` permissions, scope-guard violations, consent-required blocks | `audit_log` with `result_code='denied'` — denials are the earliest signal of both a probing attacker and a broken role design |

Every row carries `correlation_id` and `request_id`, so an incident timeline joins API access logs,
audit rows, outbox messages, ledger transactions and webhook deliveries on one key.

### 9.2 Immutability — and how it is proven

Three layers, because "append-only trigger" alone is defeated by anyone with `db_admin`:

1. **`tg_append_only`** blocks `UPDATE`/`DELETE` (`55006 → common.immutable_record`, pages on-call —
   a fired trigger means someone tried).
2. **Hash chaining.** This document requires two columns on `platform.audit_log`:
   ```sql
   ALTER TABLE platform.audit_log
     ADD COLUMN prev_hash bytea,
     ADD COLUMN row_hash  bytea;
   -- row_hash = SHA256(prev_hash || id || occurred_at || actor_id || action_code
   --                   || entity_type || entity_id || coalesce(changed_fields) || result_code)
   ```
   Chained per `(partition, writer shard)` to avoid a global serialisation point. A gap or a break is
   detectable by a verifier, not by trust.
3. **Daily anchoring.** At 02:00 IST a job computes the day's terminal hash per partition, writes it
   to `platform.audit_anchors`, ships it to the **WORM object-lock bucket** (retention 7 years,
   compliance mode — deletable by nobody, including the root account), and emails it to the DPO and
   the external auditor. Anyone who rewrites history must now also rewrite an object nobody can
   delete and an email they do not control.

The nightly integrity suite (`16` D7.2) verifies the chain over the last 24 h and a random historical
window. A verification failure is a **SEV1**, treated as a confirmed compromise until proven
otherwise.

### 9.3 Who may read the audit log

| Role | Scope of read | Constraint |
|---|---|---|
| `compliance_auditor` | All rows, all time, read-only | Cannot see un-redacted PII (rows are already redacted per `pii_registry`) |
| `security_analyst` | All rows, 24 months | Reads are themselves audited |
| `ops_support` | Rows for entities within their assigned case | Case-bound: `entity_id` must appear on an open case they are assigned |
| `dealer_owner` | Their own dealer's rows, action allow-list | Served via a filtered view (`platform.v_audit_dealer`), never the base table |
| Data principal | Their own security events (login history, device list, consent history, export/erasure history) | Via `GET /identity/auth/sessions` and the privacy dashboard — the same right, rendered as a product feature |
| Everyone else | Nothing | |

`platform.audit_log` has `REVOKE ALL FROM PUBLIC`; module DB roles have **no** grant on it at all —
writes arrive via the trigger running as the table owner. An application-level SQL injection in the
Listings module cannot read or write audit history.

### 9.4 Retention

Per `03` §9.3: 12 months hot, 7 years archived, purge after 7. Archived partitions stay queryable
(a subpoena or a GST audit needs SQL). `legal_hold` blocks the purge job for any entity under
dispute or investigation. ICT/system logs follow the CERT-In 180-day-in-India minimum and are
retained 13 months for security analytics.

---

## 10. Application security controls

### 10.1 Input validation

- **One schema, two runtimes.** Form Schema Registry → Zod (web/Flutter) and FluentValidation
  (server) generated from the same rows. A client-only validation is a bug the analyser catches
  (`AH0010`: an endpoint whose request DTO has no registered validator fails the build).
- **Reject unknown properties** on every request body. Silent ignoring is how mass-assignment
  vulnerabilities happen; `kyc_level`, `state_code`, `commission_paise` must never be bindable.
- Body size caps per route class (registry): `default 256 KiB`, `bulk_import 20 MiB`,
  `media_meta 64 KiB`. JSON depth ≤ 32, array length ≤ 5,000, JSONB keys ≤ 200.
- **The filter grammar (`04` §7.4) is parsed into a whitelisted AST.** Field names are validated
  against `attribute_definitions`/column allow-lists; operators against an enum; values are
  parameterised. No string concatenation ever reaches SQL or the OpenSearch query DSL. A fuzz suite
  runs the grammar nightly with SQL/DSL metacharacters and asserts a 400, never a 500.
- **JsonLogic sandbox** (from §2.5): op allow-list, no I/O, depth 12, 5,000 steps, 50 ms, RE2 for
  regex. Registry rows are data, but they are *executable* data, so they get the same distrust as
  user input.
- Output encoding is React's default; `dangerouslySetInnerHTML` is banned by lint with a single
  reviewed exception for CMS blocks, which are sanitised server-side with an allow-list sanitiser.

### 10.2 SSRF

Every outbound HTTP call goes through `IOutboundHttpClient`. Nothing else may construct an
`HttpClient` (analyser `AH0012`).

```
1. Parse URL. Scheme ∈ {https}. No userinfo. No fragment. Port ∈ {443} (or the registry-declared port).
2. Host must match an allow-list resolved from:
     platform.integration_providers.base_url   (change-set controlled)
     webhook_endpoints.url                     (validated at registration AND at delivery)
3. Resolve DNS. EVERY resolved A/AAAA must be public:
     deny 0.0.0.0/8 10/8 100.64/10 127/8 169.254/16 172.16/12 192.0.0/24 192.168/16
          198.18/15 224/4 240/4 ::1 fc00::/7 fe80::/10 ::ffff:0:0/96
     deny the cloud metadata endpoint explicitly (169.254.169.254, fd00:ec2::254)
4. PIN the resolved IP for the connection (custom SocketsHttpHandler ConnectCallback).
   → closes the DNS-rebinding TOCTOU between validation and connect.
5. Redirects: max 2, each re-validated through steps 1–4. Cross-host redirect to a
   non-allow-listed host → abort.
6. Response: size cap 8 MiB, timeout from provider_routes.timeout_ms, no auto-decompress bombs.
7. Egress leaves through a NAT gateway whose security group allows only the allow-listed CIDRs —
   defence in depth for the case where the code check is bypassed.
```

The highest-risk SSRF vector is a **partner-registered webhook URL**, because it is attacker-supplied
by design. It is validated at registration *and* re-validated at every delivery (a hostname's DNS can
change after registration), and IMDSv2 is enforced on all instances so a successful metadata hit
still yields nothing without a token.

### 10.3 File upload hardening

The media pipeline is the second-largest attack surface after authz, because it accepts bytes from
anyone and serves them to everyone.

| Control | Detail |
|---|---|
| Transport | Direct-to-S3 **presigned PUT** with `content-length-range`, `Content-Type` condition, and a 5-minute expiry. The API never proxies bytes |
| Naming | Storage key is `uuid v7`, no extension, no original filename. The original filename is stored as metadata and never echoed into a header or a path |
| Type detection | **Magic bytes**, not the declared `Content-Type`, not the extension. Mismatch ⇒ reject `security.upload.type_mismatch` |
| Allow-list | Images: JPEG, PNG, HEIC, WebP. Documents: PDF only. **SVG is banned outright** (it is a script container). Archives, HTML, Office formats: banned |
| Re-encode | Every image is re-encoded through libvips into a normalised JPEG/WebP at bounded dimensions. This destroys polyglots, embedded payloads and **EXIF — including GPS coordinates**, which are location PII we have no basis to keep on a listing photo |
| PDF | Sanitised via `qpdf --decrypt --linearize`; JavaScript, embedded files and launch actions stripped; page/size limits |
| Malware scan | Async scan before the object becomes visible; unscanned objects are not servable. `documents.documents.scan_state` gates the signed-URL mint |
| Decompression | Image dimension and pixel-count caps before decode (`libvips` limits) to stop decompression bombs |
| Quotas | Per-user and per-dealer daily caps from `rate_limit_policies` (`media_upload` bucket) |
| Serving origin | Media is served from `media.autohub.in` — a **separate origin** with no cookies and a restrictive CSP, so a hypothetical stored-XSS in a served file cannot touch an app session |
| Documents | `Content-Disposition: attachment` and `X-Content-Type-Options: nosniff` for anything not an image |
| Provenance | `catalog.vehicle_media` stores a perceptual hash + checksum; `uq_vehicle_media__checksum` catches re-uploads, and pHash matching against other listings catches photo theft — a genuine, frequent problem on Indian classifieds |

### 10.4 Signed media URLs

```
GET /api/v1/documents/{id}/url
  → authz: resolve the OWNING entity, run IScopeGuard against it (§4.3)
  → checks: scan_state='clean', not legal_hold-blocked for this actor, consent purpose if applicable
  → mint: CDN signed URL, TTL = rule_sets.media_url_ttl_seconds (300 default, 60 for KYC/documents)
          policy binds { resource path, expiry, requesting sub, single IP /24 for doc classes }
  → audit: one audit_log row per mint, action_code 'documents.document.read'
```

- Buckets are **private**; there is no public-read path, and a bucket-policy test in CI asserts it.
- Listing photos use long-lived CDN URLs **only after** the listing is public, and they are still
  keyed by uuid — a private listing's photos are never reachable, even by URL guess.
- KYC documents, inspection raw photos, e-signed agreements and invoices are **never** long-lived.
  Each view is a fresh mint and a fresh audit row.
- Signed-URL keys rotate every 30 days (§5.3). A leaked key expires on its own.
- Watermarking: inspection report PDFs and shared documents carry a visible watermark with the
  viewing party's `public_ref` and timestamp — so a leaked PDF identifies its source.

### 10.5 Rate limiting as an abuse control

`04` §8 defines the tiers. Security additions:

| Bucket | Extra control |
|---|---|
| `otp` | Ladder per phone/device/IP (`04` §4.2.1); CAPTCHA on IP breach; a phone that fails verification 10× in an hour is locked for 15 min |
| `contact_reveal` | 20/day customer, 200/day dealer; breach raises a scraping Trust signal, and the *response* degrades (masked number only) rather than 429-ing, so a scraper cannot distinguish success from a limit |
| `search` | Anonymous search behind bot management; result pages beyond N are cursor-only, and the cursor is signed — preventing deep-crawl enumeration |
| `export`, `privacy_request` | 2–5/day; deliberately generous enough not to obstruct a genuine DPDP right |
| `login` failures | Exponential per-account and per-IP backoff; account is never permanently locked by a remote attacker (that is itself a DoS), only slowed |
| `bid` | Never shed under load (`04` §8) |

Exceeding any bucket 5× in 10 minutes raises a Trust velocity event, which is the bridge from
rate-limiting into fraud scoring (`16` A4).

### 10.6 Webhook signature verification

**Inbound** (`04` §13.4) is authoritative; the security restatement is the ordering, because getting
it wrong is the classic bug:

```
verify signature over RAW BYTES → dedupe on (provider, provider_event_id) → persist → 200 → process async
                                   ↑ never parse-then-verify; never trust the body's amount
```

Constant-time comparison, a ±300 s timestamp window, and **re-fetch from the provider API before any
ledger posting**. A webhook is a *notification that something may have happened*, never a source of
truth about money.

**Outbound** (`04` §13.2): HMAC-SHA256 over `{timestamp}.{raw_body}`, dual `v1` values during
rotation, `secret_previous` valid 7 days. We publish the verification requirements to partners and
provide a reference implementation in the partner docs — a partner who verifies incorrectly is our
incident too.

### 10.7 Browser-surface controls

| Header | Value | Note |
|---|---|---|
| `Content-Security-Policy` | `default-src 'self'; script-src 'self' 'nonce-{n}' 'strict-dynamic'; style-src 'self' 'nonce-{n}'; img-src 'self' https://media.autohub.in data:; connect-src 'self' https://api.autohub.in wss://rt.autohub.in; frame-ancestors 'none'; base-uri 'none'; form-action 'self'; object-src 'none'; upgrade-insecure-requests` | Nonce per request; **no `unsafe-inline`**. Self-hosted fonts (`01` §3) mean no font CDN in the policy — the design decision and the security decision agree |
| `Strict-Transport-Security` | `max-age=63072000; includeSubDomains; preload` | |
| `X-Content-Type-Options` | `nosniff` | |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Listing URLs contain no PII, but referrer leakage to partner sites is still avoidable |
| `Permissions-Policy` | `camera=(self), geolocation=(self), microphone=(), payment=(), interest-cohort=()` | Camera/geo only where the product genuinely needs them |
| `Cross-Origin-Opener-Policy` | `same-origin` | |
| `Cross-Origin-Resource-Policy` | `same-site` (`cross-origin` on the media host) | |
| CORS | Allow-list of our own origins + registered partner origins from the registry. `Access-Control-Allow-Credentials` only for our own origins; never `*` with credentials | |
| Cookies | `__Host-` prefix, `HttpOnly; Secure; SameSite=Strict`, path-scoped to the auth route | `04` §4.3 |
| CSRF | Cookie-borne refresh is `SameSite=Strict` **and** path-scoped; the API is otherwise `Authorization`-header-based, so classic CSRF does not apply. **Next.js Server Actions are the exception** — each is a real endpoint and must call the same auth + scope guard; a lint rule fails any Server Action without an auth assertion |
| Clickjacking | `frame-ancestors 'none'` | |
| Secrets in the bundle | Only `NEXT_PUBLIC_*` values may reach the client; CI greps the built bundle for known secret patterns and for any `process.env` key not on the public allow-list | |

**Mobile:** certificate pinning with a backup pin and a remote kill switch (§5.7), no PII in logs
(a Dart lint rule on `print`/`debugPrint` with a PII-shaped argument), `FLAG_SECURE` on KYC,
payment and document screens, secure storage for tokens, and no WebView with `javaScriptEnabled`
pointed at a non-AutoHub origin.

### 10.8 Dependency and supply-chain policy

| Control | Rule |
|---|---|
| Lockfiles | `pnpm-lock.yaml` and NuGet `packages.lock.json` committed; CI installs with `--frozen-lockfile` / `--locked-mode`. A lockfile change in a PR that also changes source requires a separate reviewer |
| **Cooldown** | A newly published version is not installable for `minimumReleaseAge = 7 days` (pnpm config). Most npm supply-chain compromises are caught within 72 h; a week of latency costs nothing and removes the entire class |
| Install scripts | `pnpm` `ignore-scripts=true` globally; an allow-list of packages permitted to run postinstall, reviewed by CODEOWNERS |
| Provenance | npm packages with provenance attestation preferred; NuGet packages must be signed |
| SBOM | CycloneDX SBOM generated per build for backend, web and mobile; stored with the artefact and ingested into Dependency-Track for continuous CVE matching against **shipped** versions, not just current HEAD |
| Vulnerability SLA | Critical (CVSS ≥ 9.0 or KEV-listed): patch in **24 h**. High: 7 days. Medium: 30 days. Low: next scheduled. The clock starts at *disclosure*, not at ticket triage |
| Container images | Distroless/chiselled base, non-root, read-only rootfs, no shell. Scanned at build and daily in the registry. Signed with `cosign`; the cluster admits only signed images |
| CI | GitHub Actions pinned to commit SHAs, never tags. `pull_request_target` banned. Secrets exposed to workflows only on protected branches. Cloud access via OIDC, no static keys |
| Provenance of our own builds | SLSA build-provenance attestation; target level 3 |
| CODEOWNERS | Mandatory review for `AutoHub.Platform/**`, `Modules/AutoHub.Modules.Payments/**`, `Persistence/Migrations/**`, `infra/**`, and anything touching `crypto_keys` or `pii_registry` |
| Third-party JS in the browser | Effectively none. Analytics is first-party and server-side where possible. Any tag manager or vendor script requires a DPIA and a CSP entry, which makes "just paste this snippet" impossible by construction |
| AI-generated code | Reviewed like any other code; the analysers (`AH0007`–`AH0012`) are the safety net, and they do not care who wrote the handler |

### 10.9 The banned list

Things that will not pass review, stated so nobody has to argue about them:

- A status string, percentage, fee, role name or option list in code (North Star §2 — restated here
  because most of them are also security constants).
- `dangerouslySetInnerHTML` outside the sanitised CMS block.
- A raw `HttpClient`, a raw SQL string built by concatenation, or a `DbContext` query filtered by an
  unguarded request-supplied id.
- A public S3 bucket, a permanent media URL for a document, or an unsigned media URL.
- A secret in an env var that is really a business threshold.
- A new PII column without a `pii_registry` row in the same migration.
- An SMS template without a DLT id, or a notification with a non-whitelisted link.
- A DTO field named `kyc_level`, `state_code`, `commission_paise`, `is_verified` bound from a request.
- `catch { }` around anything in the payments or auth path.

---

## 11. Incident response

### 11.1 Severity and clocks

| Sev | Definition | Page | Comms | Statutory clock |
|---|---|---|---|---|
| **SEV1** | Confirmed unauthorised access to personal or financial data; funds moved without authorisation; signing-key or KEK compromise; audit-chain break | Immediate, CISO + CEO + DPO | Status page + affected principals | CERT-In 6 h; DPDP Board initial without delay, detail 72 h; PA/bank per contract |
| **SEV2** | Credible indication of the above; active exploitation of a critical vuln; mass account takeover attempt; registry tampering | 15 min, CISO | Internal + partners if implicated | CERT-In 6 h once the incident type is met |
| **SEV3** | Contained exploit attempt, single-account compromise, leaked secret with no evidence of use | 1 h, security on-call | Internal | Assess; usually reportable if a listed type |
| **SEV4** | Vulnerability report, policy violation, failed control with no exposure | Next business day | Internal | None |

**The 6-hour CERT-In clock starts when someone *notices*, not when forensics concludes.** The runbook
therefore has an explicit "report on suspicion" step and a named person who owns pressing send.

### 11.2 Runbook

```
T+0   DETECT      Alert / report / anomaly. On-call acknowledges. Open a security incident
                  (workflow 'breach_incident', state 'detected'). Open the war-room channel.
                  START THE CLOCK — the incident row's detected_at is the legal timestamp.

T+10m TRIAGE      Assign IC (incident commander), scribe, comms lead, and a legal/DPO liaison.
                  Classify: is personal data involved? funds? a CERT-In listed type?
                  Set severity. State 'triaged' has sla_minutes = 300 and escalates on breach.

T+15m PRESERVE    BEFORE any remediation: snapshot affected hosts/volumes, export the relevant
                  audit_log window and its anchor, freeze log rotation, copy to the WORM bucket.
                  Chain of custody recorded. You cannot un-destroy evidence.

T+20m CONTAIN     Choose the smallest effective action, in this order:
                    1. Revoke: token_epoch bump (scoped or global), API key revoke, session kill
                    2. Isolate: security-group cutover, provider_routes deactivate, WAF rule
                    3. Freeze money: feature flag 'payments.global_pause'; then 'auction.global_pause'
                       (freeze order is money-first, non-negotiable — 16 D7.3)
                    4. Rotate: any credential plausibly exposed. Rotate first, investigate second.

T+2h  ASSESS      Scope: which parties, which pii_class columns, which records, what time window.
                  Query the audit log, not the application, so the answer is defensible.
                  Populate breach_incidents.affected_* fields. This drives every notification.

T+≤6h REPORT      CERT-In initial report (on suspicion with evidence, not on certainty).
                  DPDP Board initial intimation if personal data is involved.
                  PA / bank / insurer / lender per contract.
                  Each send writes a breach_notifications row with the artefact attached.

T+≤24h NOTIFY     Affected data principals, in their locale, via the pre-registered DLT/WhatsApp/
                  email templates: what happened, what data, what we did, what they should do,
                  who to contact. Plain language. No apology-as-substitute-for-facts (01 §7).

T+≤72h DETAIL     DPDP Board detailed report: events, circumstances, reasons, mitigation, remedial
                  measures, findings on who caused it, and a report on the principal intimations.

T+…   ERADICATE   Remove the root cause. Patch, revoke, redeploy, re-key, re-image.
      RECOVER     Restore service. Verify with the integrity suite BEFORE unfreezing (16 D7.3 §7).
                  Unfreeze auctions first (with extended end times + notice), then payments.

T+5d  LEARN       Blameless postmortem. Every action item becomes a threat_register row with an
                  owner and a review date, or it does not exist. Update this runbook.
```

**Security kill switches** (Feature Registry, four-eyes to set, single-approver to *un*set is
forbidden):

| Flag | Effect |
|---|---|
| `payments.global_pause` | New payment intents 503; inbound webhooks still accepted and queued |
| `auction.global_pause` | Live lots auto-extend; no lot closes during an incident |
| `auth.force_reauth_all` | Global `token_epoch` bump |
| `platform.registry.freeze` | No change set may apply |
| `media.signed_url_ttl_seconds` | Drop to 60 |
| `integration.{provider}.disable` | Deactivate a provider route chain-wide |
| `security.cert_pinning.enabled` | Disable mobile pinning (mis-issuance escape hatch) |

### 11.3 Roles

| Role | Who | Owns |
|---|---|---|
| Incident Commander | Rotating senior engineer | Decisions, not typing |
| Scribe | Anyone | The timeline — which becomes the regulator's timeline |
| Comms lead | Support/marketing lead | Status page, principal notifications, partner comms |
| DPO | Named, India-based | DPDP assessment, Board intimation, principal notice content |
| CISO | Named | CERT-In/IRDAI reports, severity call, external counsel |
| Finance lead | Named | Ledger impact, reconciliation, any money movement decision |

---

## 12. Security testing cadence

| Activity | Frequency | Gate | Owner |
|---|---|---|---|
| SAST (CodeQL + .NET analysers + ESLint security rules) | Every PR | Blocking on high | Eng |
| Custom analysers `AH0007`–`AH0012` (scope guard, sensitive fields, raw queries, validators, HttpClient) | Every build | Blocking, always | Eng |
| Secret scanning (pre-commit, push protection, nightly full history) | Continuous | Blocking + rotation | Eng |
| Dependency + container scan (SBOM ↔ CVE) | Daily | SLA-driven, §10.8 | Eng |
| **IDOR fuzz harness** (§4.6) | Nightly on staging | Blocking for release | Eng |
| Contract tests: reserve leakage, PII-in-DTO, error-`params` leakage, consent-not-bundled, staff-step-up-not-SMS, public-bucket, no-card-column | Every PR | Blocking | Eng |
| DAST (ZAP authenticated scan) | Nightly on staging | Report; high findings block release | Sec |
| IaC scan (Terraform/K8s policy) | Every PR | Blocking on high | Platform |
| Load + rate-limit abuse test (bid bucket starvation scenario) | Before each auction-affecting release | Blocking | Eng |
| Restore + integrity suite (ledger balance, invoice gaps, audit chain) | Nightly | Blocking; failure = SEV2 | Platform |
| Access review (RBAC assignments vs IdP, API keys, break-glass grants) | Monthly | Report to CISO | Sec |
| Threat-register review | Quarterly, per item's `review_due_at` | Report | Sec |
| **External penetration test** | 2×/year, **plus** before launch of any money-touching module (Deals/Escrow, Auction, Finance, Insurance) | Critical/high must be closed or formally accepted before go-live | Sec + vendor |
| VAPT for IRDAI cyber compliance | Annual | Filed | Sec |
| Red team (assumed-breach: valid dealer session → how far?) | Annual | Report + threat-register updates | Sec + vendor |
| DPDP data audit (SDF) | Annual | Independent auditor | DPO |
| DPIA | Per new purpose / provider / model | Blocking on `provider_routes` activation | DPO |
| Tabletop: breach with a 6-hour clock | 2×/year | Timed; the clock is the score | CISO |
| DR game day (region failover) | Quarterly (`16` D7.2) | Written report | Platform |
| Phishing simulation (staff) | Quarterly | Training for failures | Sec |
| Vulnerability disclosure programme | Continuous | `security.txt`, 90-day disclosure, safe-harbour language | Sec |
| Paid bug bounty | From Phase 6 (money modules live) | Scoped, with a payout schedule in the registry | Sec |

**Why pentest gates the money modules specifically:** Phase 2 (escrow), Phase 4 (auction) and
Phase 6 (finance/insurance) are the phases where a defect converts directly into rupees. Testing
after launch is a discount on the finding and a premium on the loss.

---

## 13. The compliance calendar

| Cadence | Item | Owner |
|---|---|---|
| Continuous | CERT-In 6 h reporting readiness; 180-day India log retention | Platform |
| Daily | Escrow three-way reconciliation; hold-ceiling report; registry snapshot diff; audit-chain verify | Finance / Sec |
| Weekly | Case-less sensitive-data reads report to DPO; break-glass grant review | DPO / Sec |
| Monthly | GST returns and e-invoice reconciliation; TDS §194-O deposit; access review; key-rotation-due report | Finance / Sec |
| Quarterly | TDS returns; threat-register review; DR game day; phishing sim; control-effectiveness review | Multiple |
| Half-yearly | External pentest; breach tabletop | Sec |
| Annual | DPDP data audit + DPIA refresh (SDF); IRDAI VAPT + cyber-policy board approval; ISO 27001 surveillance; insurance broker licence renewal; red team; policy review | DPO / CISO |
| Event-driven | New provider ⇒ DPIA + DPA + region attestation. New purpose ⇒ consent form version + notice update. Finance Act ⇒ `tax_rules` rows. New state/city ⇒ Geography Registry + RTO forms. New notification template ⇒ DLT registration **before** launch | Multiple |

---

## 14. Cross-module assumptions this document makes

Other specs must honour these, or say why not.

1. **`platform.pii_registry` gains `encryption_class`, `blind_index_column`, `key_purpose_code`.**
   Every migration adding a PII column sets them. CI already fails on a missing `pii_registry` row
   (`03` §12.8); it now also fails on `encryption_class = 'none'` for classes
   `direct_identifier`, `financial` and `biometric` without a written justification in the migration.
2. **`platform.audit_log` gains `prev_hash` and `row_hash`.** The append-only trigger computes them;
   no module writes them.
3. **New platform tables introduced here:** `platform.crypto_keys`, `platform.security_policies`,
   `platform.threat_register`, `platform.dpia_records`, `platform.audit_anchors`,
   `trust.consent_receipts`, `trust.data_requests`, `trust.nominations`, `trust.grievances`,
   `trust.breach_incidents`, `trust.breach_notifications`, `trust.age_assurance`,
   `trust.consent_withdrawal_actions`, `identity.security_events`, plus the separate `ah_tokens`
   database holding `vault.pii_tokens`.
4. **New workflow definitions:** `dpdp_request`, `breach_incident`, `grievance`, `crypto_key`,
   `threat_item`. Their states are `workflow_states` rows with the semantic flags set — no module may
   introduce a status enum for any of them.
5. **`platform.permissions` gains `step_up_required`, `step_up_max_age_seconds`, `step_up_methods`,
   `step_up_amount_threshold_paise`.** Step-up is declared on the permission, never on the endpoint,
   so a new endpoint inherits the policy.
6. **`platform.capabilities` gains `residency_required`; `platform.integration_providers` gains
   `data_region` and `purpose_code` (on `provider_routes`).** A provider route cannot activate
   without a completed `dpia_records` row.
7. **Object-level authorisation is not optional and not per-module.** Every module registers its
   owned objects in the scope catalogue (§4.3) and every `Scope.Resource` handler calls
   `IScopeGuard.Assert`. `AH0007` makes omission a build error.
8. **No module may construct an `HttpClient`, build SQL by concatenation, or serve a permanent media
   URL.** Outbound calls go through `IOutboundHttpClient`; media goes through the signing service.
9. **Every notification template targeting SMS carries a DLT template id**, and every template
   published in a new locale is DLT/Meta-registered *before* the feature that sends it ships. Breach
   and security templates are registered on day one, before there is anything to notify about.
10. **Every rate, threshold, fee, SLA and retention period in this document is a registry row.**
    Specifically: `rule_sets.dpdp_response_sla`, `rule_sets.breach_notification_sla`,
    `rule_sets.key_rotation`, `rule_sets.escrow_hard_ceiling`, `rule_sets.media_url_ttl_seconds`,
    `platform.tax_rules` (TCS/TDS/GST), `platform.security_policies` (token lifetimes),
    `platform.pii_registry.retention_months`. No module may read any of these from configuration.
11. **The audit log is not readable by module DB roles.** A module needing audit data reads a
    purpose-built view with an explicit grant, or it does not read it.
12. **Money is never moved by a security decision.** Trust and security may set `holds_funds`,
    freeze an account, or revoke a session. Only Deals/Payments post ledger rows (`16` F6, restated).
13. **"Unknown" is a value.** Every externally-sourced assurance flag — VAHAN status, challan status,
    sanctions match, scan state, age assurance — is tri-state, and no module may coerce `unknown` to
    a safe-looking `false` (`16` F15, extended to security flags).
14. **The attachment module must not dispatch rides, compute fares, or model trips** without a state
    aggregator licence. The absence of those endpoints is the control.
