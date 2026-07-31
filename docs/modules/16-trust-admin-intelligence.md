# Module 16 — Fraud & Trust, Admin, Super Admin, AI, Analytics

> PRD modules 19, 20, 24, 25, 26.
> Owning schemas: `trust`, `analytics`, plus authoring surfaces over `platform`.
> Owning projects: `AutoHub.Modules.Trust`, `AutoHub.Modules.Analytics`, `apps/admin`.
>
> Derives from `00-north-star.md` (prime directive, stack, module isolation),
> `01-design-system.md` (instrumentation aesthetic, `StatusPill` tone from registry `ui.tone`),
> `02-domain-model.md` (aggregates, ledger, error register),
> `03-data-model-erd.md` (schema-per-module, standard columns, registries, audit, PII registry),
> `04-api-contract.md` (envelope, error codes, idempotency, step-up, degradation).
> Where those documents already decided something, this document **uses** it and does not restate it.

---

## 0. What this document owns, and the one rule it never breaks

This module is where the platform judges people, judges itself, and lets a business person change
the rules of both. Four things follow from that:

| Concern | This doc owns | It explicitly does NOT own |
|---|---|---|
| Trust | risk signals, scores, cases, restrictions, blacklists, device/graph intelligence | KYC provider orchestration (Identity), consent capture UI (Identity), document storage (Documents) |
| AI | model registry, serving contract, per-feature specs, HITL, drift | the valuation *domain* object (Valuation module) — AI supplies the number, Valuation owns the record |
| Admin | queues, editors, publish/rollback UX | the underlying registry DDL (data model doc) |
| Super Admin | permission authoring, dual control, DR runbook | auth token mechanics (API contract §4) |
| Analytics | event taxonomy, warehouse, metric definitions, dashboards | OLTP counters (`analytics.listing_counters` is a rollup, not a source) |

**The rule.** Trust, admin and analytics are the three places where hardcoding is most tempting,
because every one of them looks like "just an internal tool". So the prime directive is applied
hardest here:

```
✗ if (riskScore > 70) Block(user);                    → rule_sets domain='scoring', code='trust_risk_band'
✗ case "PENDING_REVIEW": ...                          → workflow_states of the 'trust_case' machine
✗ const REFUND_FEE = 0.02;                            → fee_components code='refund_processing_fee'
✗ if (model == "damage-v3") endpoint = "https://..."  → provider_routes for capability 'ai.damage_detect'
✗ ["Odometer tampering","Fake RC","Shill bidding"]    → codes set_code='trust_signal_type'
✗ MetricCard(title: "GMV")                            → analytics.metric_definitions + i18n key
```

Even the **metric definitions** are rows. A CFO who wants "GMV excludes cancelled-after-payout"
changes a SQL fragment in `analytics.metric_definitions`, gets a diff, a preview against last
month's numbers, and a publish — not a Jira ticket.

---

# PART A — TRUST & FRAUD

## A1. The threat model, stated in the vocabulary of this market

Generic fraud taxonomies are useless here. These are the eight frauds that actually cost money on
an Indian vehicle marketplace, each mapped to the signal that catches it and the registry row that
governs the response.

| # | Fraud | How it presents | Primary detection signals | Response driven by |
|---|---|---|---|---|
| 1 | **Odometer tampering** | 2016 Innova listed at 48,000 km; last PUC said 1,42,000 km | `odometer_rollback_delta_km`, `odometer_source_count`, OCR of cluster photo vs. declared, insurance-claim km, service-record km | `trust_signal_type='odometer_rollback'`, action `require_inspection` |
| 2 | **Cloned / stolen vehicle** | Two live listings, same chassis last-5, different states; or NCRB-flagged plate | `vehicle_identity_conflict` (domain doc §4.4 T2 alarm), VAHAN blacklist, chassis-plate mismatch on inspection | `hard_block` + law-enforcement pack export |
| 3 | **Fake / forged RC** | Photoshopped RC image, DigiLocker refused, HSRP laser code absent | OCR field-vs-VAHAN mismatch, ELA/copy-move tamper score, font-inconsistency score, DigiLocker issuance absent | `require_digilocker_rc`, listing held in `pending_docs` |
| 4 | **Curbstoning** (dealer posing as private seller) | "Private" seller with 6 listings in 90 days, 3 different payout accounts, GSTIN-linked PAN | `listings_created_180d` at party level, `pan_hash_shared_party_count`, bank-account graph degree, weekday-daytime response pattern, photo-location clustering | `reclassify_party_kind` → dealer fees apply (pricing registry does the rest) |
| 5 | **Shill bidding** | Seller's cousin bids the lot from ₹4.2L to ₹5.6L then withdraws | `seller_bidder_link_score`, shared device/IP/bank graph edges, bid-then-never-settle ratio, bid timing autocorrelation | `void_bid` + `suspend_bidder` + lot re-run |
| 6 | **Payment diversion off platform** | "Sir, GST bachega, direct UPI kar do" in chat or on a call | contact/UPI pattern hits in chat (already `chat.message.blocked_content`), rapid thread-then-silence, deal abandoned after inspection with vehicle marked sold externally | `disintermediation_suspected` → seller fee still due, escrow-only enforcement |
| 7 | **Refund abuse** | Books inspection, cancels after report, repeats across accounts | `refund_requests_90d`, `deals_cancelled_ratio_90d`, cancel-after-report ratio, device graph across the accounts | `refund_manual_review`, `refund_cooldown_days` |
| 8 | **Salvage / total-loss laundering** | Insurer declared it a constructive total loss (CTL) in Chennai after the floods; rebuilt, re-registered, sold in Nashik as "single owner, no accident" | `insurance_total_loss_flag` (IIB), claim-history gap, flood-region + registration-month correlation, repaint/panel-gap density from `vehicle-damage-detect`, `odometer_source_count = 1` | `require_inspection` (mandatory, non-waivable) + `disclosure_required` badge |

Two more that ride along and must be caught by the same machinery: **promo/referral farming**
(self-referral rings — same device graph, sequential phone numbers) and **account takeover**
(SIM swap → OTP → payout-account change within minutes; the `payout_account_changes_30d` +
`device_age_days` pair is the tell).

### A1.1 What "fraud" is *not*, and why that distinction is in the schema

A high risk score is not a verdict. `trust.risk_scores` records a **probability**; only a
`trust.cases` row that reaches a `resolved_fraud_confirmed` state, or a hard rule with
`is_deterministic = true`, produces a durable adverse label. This matters because:

- adverse labels feed model training — a mislabelled auto-block poisons the next model version;
- DPDP Act 2023 §11/§13 give the data principal a grievance right, and "the model said so" is not
  an answer we can give. Every restriction row carries a `reason_code` from
  `platform.codes(set_code='trust_restriction_reason')` and an i18n-keyed explanation.

---

## A2. Identity assurance ladder

The PRD's ladder is not five booleans — it is a monotone **level** that gates capability, and every
gate is a registry row, never an `if`.

```
L0 ANONYMOUS      browse, search, save
 │  phone OTP (Identity module, §4.2 API contract)
L1 CONTACTABLE    chat, shortlist, book test drive
 │  email verify + PAN validate (NSDL) + name match
L2 IDENTIFIED     create listing, place C2C offer, request valuation with report
 │  Aadhaar OKYC via DigiLocker OR offline eKYC XML  (NO Aadhaar number stored — schema-enforced)
L3 VERIFIED       auction bidder registration, escrow-funded deals up to slab cap
 │  face capture + passive liveness, matched to DigiLocker/OKYC photo
L4 ASSURED        high-value auctions, lease as lessee, rental host
 │  bank penny-drop (name match) + GSTIN verify (business) + address proof
L5 SETTLEMENT-READY  receive payouts, dealer inventory at scale, finance disbursal
```

### A2.1 The ladder is data

`trust.kyc_records.kyc_level` (data model §4.11) holds the achieved level. What each level *unlocks*
is **not** in Trust — it is a guard on the workflow transition that needs it:

```jsonc
// platform.rule_sets: code='guard_bidder_registration', domain='guard'
// platform.rules row
{
  "code": "min_assurance_for_bidding",
  "priority": 10,
  "condition": { ">=": [ { "var": "actor.kyc_level" }, 3 ] },
  "outcome":   { "allow": true },
  "stop_on_match": true,
  "explain_i18n_key": "trust.guard.bidding_requires_l3"
}
```

Raising the bar for auction bidding from L3 to L4 in Maharashtra only is one extra `rules` row with
`condition` including `{"==":[{"var":"scope.state_code"},"MH"]}` and `priority: 5`. No deploy.

### A2.2 Step ladder → check matrix

| Level | Checks (`trust.kyc_checks.check_code`) | Capability (`platform.capabilities`) | Typical provider (`integration_providers`) | Cost/call | p95 |
|---|---|---|---|---|---|
| L1 | `phone_otp` | `sms`, `whatsapp` | msg91 → gupshup → voice OTP | ₹0.18 | 4 s to deliver |
| L2 | `email_verify`, `pan_validate`, `pan_name_match` | `kyc.pan` | karza → signzy | ₹3.50 | 900 ms |
| L3 | `digilocker_fetch` (RC/Aadhaar/DL), `aadhaar_okyc` | `kyc.digilocker`, `kyc.aadhaar_okyc` | DigiLocker partner API | ₹6.00 | 2.4 s + user redirect |
| L4a | `face_liveness`, `face_match` | `kyc.face_liveness`, `kyc.face_match` | signzy / hyperverge | ₹9.00 | 1.8 s |
| L4b | `penny_drop`, `gstin_verify` | `kyc.bank_penny_drop`, `kyc.gstin` | cashfree verification suite | ₹4.00 + ₹1 re-credit | 6 s (async) |
| L5 | `bank_name_match ≥ threshold`, `address_proof_ocr`, optional `video_kyc` | — | — | — | — |

Thresholds (`pan_name_match_score ≥ 0.85`, `penny_drop_match_score ≥ 0.90`,
`face_match_score ≥ 0.78`, `liveness_score ≥ 0.90`) are **not constants**. They live in
`platform.rule_sets(code='kyc_thresholds', domain='scoring')` with one `rules` row per check, so
Trust Ops can loosen face-match in a month where the provider's model regresses without a release.

### A2.3 India-specific hard requirements encoded here

- **Aadhaar number is unstorable by construction** (data model §4.11). Any spec sentence saying
  "verify Aadhaar" means DigiLocker issuance or OKYC XML with `aadhaar_ref_id`. The face image used
  for L4a is deleted after scoring — only `face_match_score` and `liveness_score` persist, and
  `selfie_document_id` points at a document whose `document_types.retention_months = 0` when the
  case closes clean.
- **Penny-drop re-credit.** Cashfree/RBI-compliant penny drop debits ₹1 from our nodal account and
  credits the seller. That ₹1 is a real ledger movement — it posts to
  `payments.ledger_accounts(account_code='kyc_penny_drop_expense')`, not to a suspense bucket.
- **PMLA retention.** `trust.kyc_records.pan_hash`, `verified_at`, `risk_band_code` survive a DPDP
  erasure with `erasure_action='retain'` and `legal_basis='PMLA record-keeping, 5y from relationship end'`.
- **Consent before bureau.** A CIBIL pull for finance requires `trust.consents` with
  `purpose_code='credit_bureau_pull'` and `granted = true`; absence returns
  `403 finance.bureau.consent_missing`. Trust is the system of record for that artefact, not Finance.

---

## A3. Trust schema (additions to `trust`)

Everything below follows data-model §2.4 standard columns unless marked append-only.

```sql
-- ─────────────────────────────────────────────────────────────────────────────
-- Devices and sessions: the spine of graph detection
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE trust.devices (
    id                 uuid PRIMARY KEY DEFAULT platform.uuid_generate_v7(),
    device_id          text NOT NULL,                 -- X-AutoHub-Device-Id, client-generated, opaque
    fingerprint_hash   platform.sha256_hex NOT NULL,  -- server-computed, see A4.1
    fingerprint_components jsonb NOT NULL DEFAULT '{}', -- redacted; no raw canvas/audio payloads
    platform_code      platform.code_key NOT NULL,    -- 'web','android','ios'
    app_version        text,
    os_version         text,
    model_name         text,
    is_emulator        boolean NOT NULL DEFAULT false,
    is_rooted          boolean NOT NULL DEFAULT false,
    integrity_verdict  platform.code_key,             -- Play Integrity / DeviceCheck: 'meets_device','basic','fail'
    first_seen_at      timestamptz NOT NULL DEFAULT now(),
    last_seen_at       timestamptz NOT NULL DEFAULT now(),
    reinstall_count    integer NOT NULL DEFAULT 0,
    blocked_at         timestamptz,
    blocked_case_id    uuid,
    created_at timestamptz NOT NULL DEFAULT now(), created_by uuid,
    updated_at timestamptz NOT NULL DEFAULT now(), updated_by uuid,
    deleted_at timestamptz, deleted_by uuid,
    row_version bigint NOT NULL DEFAULT 1,
    CONSTRAINT uq_devices__device_id UNIQUE (device_id)
);
CREATE INDEX ix_devices__fingerprint ON trust.devices (fingerprint_hash) WHERE deleted_at IS NULL;

CREATE TABLE trust.device_party_links (     -- many-to-many, the raw material of the link graph
    device_id     uuid NOT NULL REFERENCES trust.devices(id) ON DELETE CASCADE,
    party_id      uuid NOT NULL REFERENCES identity.parties(id),
    first_seen_at timestamptz NOT NULL DEFAULT now(),
    last_seen_at  timestamptz NOT NULL DEFAULT now(),
    session_count integer NOT NULL DEFAULT 1,
    CONSTRAINT pk_device_party_links PRIMARY KEY (device_id, party_id)
);
CREATE INDEX ix_device_party_links__party ON trust.device_party_links (party_id);

-- ─────────────────────────────────────────────────────────────────────────────
-- Signals: append-only, one row per observation
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE trust.risk_signals (
    id             uuid NOT NULL DEFAULT platform.uuid_generate_v7(),
    occurred_at    timestamptz NOT NULL DEFAULT now(),
    subject_type   platform.code_key NOT NULL REFERENCES platform.entity_types(code),  -- party|listing|vehicle|deal|bid|device|auction_lot
    subject_id     uuid NOT NULL,
    signal_set     platform.code_key NOT NULL DEFAULT 'trust_signal_type'
        CONSTRAINT ck_risk_signals__set CHECK (signal_set = 'trust_signal_type'),
    signal_code    platform.code_key NOT NULL,      -- 'odometer_rollback','shared_bank_account','vahan_blacklist_hit'
    severity       smallint NOT NULL DEFAULT 2 CHECK (severity BETWEEN 1 AND 5),
    confidence     numeric(4,3) NOT NULL DEFAULT 1.000,
    source_code    platform.code_key NOT NULL,      -- 'rule','model','integration','user_report','ops'
    source_ref     text,                            -- model_key@version, rule code, provider ref
    evidence       jsonb NOT NULL DEFAULT '{}',     -- PII-free; pointers, hashes, scores, spans
    related_subject_type platform.code_key,         -- the other end of a graph edge
    related_subject_id   uuid,
    correlation_id uuid,
    expires_at     timestamptz,                     -- decayed signals stop counting
    CONSTRAINT pk_risk_signals PRIMARY KEY (id, occurred_at),
    CONSTRAINT fk_risk_signals__code FOREIGN KEY (signal_set, signal_code)
        REFERENCES platform.codes(set_code, code)
) PARTITION BY RANGE (occurred_at);   -- monthly
CREATE INDEX ix_risk_signals__subject ON trust.risk_signals (subject_type, subject_id, occurred_at DESC);
CREATE INDEX ix_risk_signals__code    ON trust.risk_signals (signal_code, occurred_at DESC);
CREATE TRIGGER tg_risk_signals__append_only BEFORE UPDATE OR DELETE ON trust.risk_signals
    FOR EACH ROW EXECUTE FUNCTION platform.tg_append_only();

-- ─────────────────────────────────────────────────────────────────────────────
-- Scores: current state per subject + append-only history
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE trust.risk_scores (
    subject_type   platform.code_key NOT NULL REFERENCES platform.entity_types(code),
    subject_id     uuid NOT NULL,
    score          smallint NOT NULL CHECK (score BETWEEN 0 AND 1000),
    band_code      platform.code_key NOT NULL,      -- from rule_set 'trust_risk_band'
    model_key      text NOT NULL,                   -- analytics.ml_models.key
    model_version  text NOT NULL,
    rule_overlay   jsonb NOT NULL DEFAULT '[]',     -- deterministic rules that fired and their deltas
    top_features   jsonb NOT NULL DEFAULT '[]',     -- [{"feature":"device_party_count_30d","value":9,"shap":0.21}]
    computed_at    timestamptz NOT NULL DEFAULT now(),
    valid_until    timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    row_version bigint NOT NULL DEFAULT 1,
    CONSTRAINT pk_risk_scores PRIMARY KEY (subject_type, subject_id)
);
CREATE INDEX ix_risk_scores__band ON trust.risk_scores (band_code, computed_at DESC);

CREATE TABLE trust.risk_score_history (       -- append-only, partitioned monthly, feeds model eval
    LIKE trust.risk_scores INCLUDING DEFAULTS,
    id uuid NOT NULL DEFAULT platform.uuid_generate_v7(),
    CONSTRAINT pk_risk_score_history PRIMARY KEY (id, computed_at)
) PARTITION BY RANGE (computed_at);

-- ─────────────────────────────────────────────────────────────────────────────
-- Blacklists / allowlists: typed, scoped, expiring, sourced
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE trust.list_entries (
    id             uuid PRIMARY KEY DEFAULT platform.uuid_generate_v7(),
    list_code      platform.code_key NOT NULL,      -- 'blacklist','watchlist','allowlist','vip'
    identifier_type platform.code_key NOT NULL,     -- 'phone_hash','email_hash','pan_hash','bank_account_hash',
                                                    -- 'device_fingerprint','ip_cidr','chassis_masked','registration_number',
                                                    -- 'gstin','upi_vpa_hash','party_id'
    identifier_hash platform.sha256_hex,            -- hashed with the environment pepper
    identifier_plain text,                          -- ONLY for non-PII: ip_cidr, registration_number, chassis_masked
    reason_set     platform.code_key NOT NULL DEFAULT 'trust_list_reason'
        CONSTRAINT ck_list_entries__reason_set CHECK (reason_set = 'trust_list_reason'),
    reason_code    platform.code_key NOT NULL,
    source_code    platform.code_key NOT NULL,      -- 'internal_case','vahan_blacklist','ncrb','partner_bank','law_enforcement'
    source_ref     text,
    case_id        uuid,
    scope_type_code platform.code_key NOT NULL DEFAULT 'global'
        REFERENCES platform.scope_types(code),
    scope_id       uuid,
    effective_from timestamptz NOT NULL DEFAULT now(),
    expires_at     timestamptz,                     -- NULL = permanent (requires four-eyes)
    added_by       uuid NOT NULL,
    approved_by    uuid,                            -- four-eyes for permanent / party_id entries
    removed_at     timestamptz,
    removed_by     uuid,
    removal_reason text,
    created_at timestamptz NOT NULL DEFAULT now(), created_by uuid,
    updated_at timestamptz NOT NULL DEFAULT now(), updated_by uuid,
    row_version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_list_entries__one_identifier CHECK (
        (identifier_hash IS NOT NULL) <> (identifier_plain IS NOT NULL)),
    CONSTRAINT fk_list_entries__reason FOREIGN KEY (reason_set, reason_code)
        REFERENCES platform.codes(set_code, code)
);
CREATE UNIQUE INDEX uq_list_entries__active ON trust.list_entries
    (list_code, identifier_type, COALESCE(identifier_hash, identifier_plain))
    WHERE removed_at IS NULL;

-- ─────────────────────────────────────────────────────────────────────────────
-- Cases: the human layer. State lives in the workflow registry.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE trust.cases (
    id             uuid PRIMARY KEY DEFAULT platform.uuid_generate_v7(),
    public_ref     text NOT NULL,                   -- 'CASE-2026-0001842', prefix from entity_types
    case_type_code platform.code_key NOT NULL,      -- codes set 'trust_case_type'
    subject_type   platform.code_key NOT NULL REFERENCES platform.entity_types(code),
    subject_id     uuid NOT NULL,
    priority       smallint NOT NULL DEFAULT 3 CHECK (priority BETWEEN 1 AND 5),
    workflow_instance_id uuid NOT NULL REFERENCES platform.workflow_instances(id),
    state_code     platform.code_key NOT NULL,      -- denormalised (data-model §12.5)
    opened_reason_code platform.code_key NOT NULL,
    opened_by_kind platform.code_key NOT NULL,      -- 'system','model','user_report','ops','partner'
    assignee_user_id uuid,
    assignee_queue_code platform.code_key NOT NULL, -- codes set 'trust_queue'
    score_at_open  smallint,
    band_at_open   platform.code_key,
    amount_at_risk_paise platform.money_paise,      -- drives priority and four-eyes threshold
    sla_due_at     timestamptz,
    first_touch_at timestamptz,
    resolved_at    timestamptz,
    resolution_code platform.code_key,              -- codes set 'trust_case_resolution'
    resolution_notes text,
    linked_case_ids uuid[] NOT NULL DEFAULT '{}',
    created_at timestamptz NOT NULL DEFAULT now(), created_by uuid,
    updated_at timestamptz NOT NULL DEFAULT now(), updated_by uuid,
    deleted_at timestamptz, deleted_by uuid,
    row_version bigint NOT NULL DEFAULT 1,
    CONSTRAINT uq_cases__public_ref UNIQUE (public_ref)
);
CREATE INDEX ix_cases__queue_open ON trust.cases (assignee_queue_code, priority, sla_due_at)
    WHERE resolved_at IS NULL AND deleted_at IS NULL;
CREATE INDEX ix_cases__subject ON trust.cases (subject_type, subject_id, created_at DESC);

CREATE TABLE trust.case_actions (           -- append-only; what was DONE, not what was decided
    id             uuid NOT NULL DEFAULT platform.uuid_generate_v7(),
    occurred_at    timestamptz NOT NULL DEFAULT now(),
    case_id        uuid NOT NULL,
    action_code    platform.code_key NOT NULL,      -- codes set 'trust_action'
    target_type    platform.code_key NOT NULL,
    target_id      uuid NOT NULL,
    params         jsonb NOT NULL DEFAULT '{}',
    actor_id       uuid,
    actor_kind     platform.code_key NOT NULL DEFAULT 'user',
    approved_by    uuid,                            -- four-eyes
    reversal_of_action_id uuid,
    result_code    platform.code_key NOT NULL DEFAULT 'applied',
    correlation_id uuid,
    CONSTRAINT pk_case_actions PRIMARY KEY (id, occurred_at)
) PARTITION BY RANGE (occurred_at);

-- ─────────────────────────────────────────────────────────────────────────────
-- Restrictions: the only thing other modules read. One row = one live constraint.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE trust.restrictions (
    id             uuid PRIMARY KEY DEFAULT platform.uuid_generate_v7(),
    subject_type   platform.code_key NOT NULL REFERENCES platform.entity_types(code),
    subject_id     uuid NOT NULL,
    restriction_code platform.code_key NOT NULL,    -- codes set 'trust_restriction'
    -- 'block_login','block_listing_create','block_bidding','hold_payout','require_inspection',
    -- 'require_escrow_only','limit_listing_count','shadow_limit_reach','require_step_up',
    -- 'cap_transaction_amount','refund_cooldown','block_referral_payout'
    params         jsonb NOT NULL DEFAULT '{}',     -- {"max_active_listings":2} | {"cap_paise":50000000}
    reason_code    platform.code_key NOT NULL,
    case_id        uuid REFERENCES trust.cases(id),
    applied_by     uuid,
    applied_by_kind platform.code_key NOT NULL DEFAULT 'system',
    effective_from timestamptz NOT NULL DEFAULT now(),
    expires_at     timestamptz,
    lifted_at      timestamptz,
    lifted_by      uuid,
    lift_reason    text,
    is_user_visible boolean NOT NULL DEFAULT true,  -- false = shadow measure (see A7.3)
    created_at timestamptz NOT NULL DEFAULT now(), created_by uuid,
    updated_at timestamptz NOT NULL DEFAULT now(), updated_by uuid,
    row_version bigint NOT NULL DEFAULT 1
);
CREATE INDEX ix_restrictions__active ON trust.restrictions (subject_type, subject_id)
    WHERE lifted_at IS NULL;

-- ─────────────────────────────────────────────────────────────────────────────
-- Link graph: materialised edges, rebuilt incrementally
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE trust.link_edges (
    from_party_id  uuid NOT NULL,
    to_party_id    uuid NOT NULL,
    edge_type_code platform.code_key NOT NULL,      -- 'shared_device','shared_ip_24h','shared_bank_account',
                                                    -- 'shared_pan','shared_address','referral','same_vehicle',
                                                    -- 'chat_counterparty','shared_upi_vpa'
    weight         numeric(4,3) NOT NULL DEFAULT 1.000,
    first_seen_at  timestamptz NOT NULL DEFAULT now(),
    last_seen_at   timestamptz NOT NULL DEFAULT now(),
    observation_count integer NOT NULL DEFAULT 1,
    CONSTRAINT pk_link_edges PRIMARY KEY (from_party_id, to_party_id, edge_type_code),
    CONSTRAINT ck_link_edges__no_self CHECK (from_party_id <> to_party_id)
);
CREATE INDEX ix_link_edges__to ON trust.link_edges (to_party_id, edge_type_code);

-- ─────────────────────────────────────────────────────────────────────────────
-- Velocity counters: Redis is authoritative, Postgres is the audit copy
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE trust.velocity_breaches (
    id             uuid NOT NULL DEFAULT platform.uuid_generate_v7(),
    occurred_at    timestamptz NOT NULL DEFAULT now(),
    rule_code      platform.code_key NOT NULL,
    subject_type   platform.code_key NOT NULL,
    subject_id     uuid NOT NULL,
    window_seconds integer NOT NULL,
    observed_count integer NOT NULL,
    threshold      integer NOT NULL,
    action_code    platform.code_key NOT NULL,
    correlation_id uuid,
    CONSTRAINT pk_velocity_breaches PRIMARY KEY (id, occurred_at)
) PARTITION BY RANGE (occurred_at);
```

**Deliberate omissions.** There is no `trust.fraud_types` table (that is `platform.codes`), no
`trust.rules` table (that is `platform.rule_sets` with `domain='scoring'`), and no
`trust.case_statuses` table (that is `platform.workflow_states`). Three tables that every fraud
system grows and every fraud system regrets.

---

## A4. Signal collection

### A4.1 Device fingerprinting

Two identifiers, deliberately:

| Identifier | Who mints it | Stability | Attack |
|---|---|---|---|
| `X-AutoHub-Device-Id` | client, once per install, stored in Keychain/Keystore/localStorage | survives app updates; dies on reinstall/clear | trivially rotatable |
| `fingerprint_hash` | **server**, from a component bundle the client posts | survives reinstall; drifts on OS upgrade | needs real effort |

Server-side hash inputs (`fingerprint_components`, all hashed, none stored raw):

```
web      : UA-CH platform+model, screen (w×h×dpr), tz offset, language list,
           canvas hash, WebGL vendor+renderer hash, audio context hash,
           font enumeration bucket, hardwareConcurrency bucket, deviceMemory bucket
android  : Play Integrity verdict, build fingerprint, ABI list, SIM operator MCC/MNC,
           screen metrics, locale list, install source, ANDROID_ID (per-app, non-reset)
ios      : DeviceCheck bits, identifierForVendor, model identifier, locale, tz,
           screen metrics, jailbreak heuristics bundle
```

Rules that make it useful rather than creepy:

1. **We never store a raw canvas or audio buffer.** Only a 64-bit bucketed hash per component,
   plus the composite. `fingerprint_components` is registered in `platform.pii_registry` as
   `pii_class='device'`, `erasure_action='delete_row'`.
2. **Fingerprint drift tolerance.** OS upgrades change 2–3 components. We store the composite and
   the per-component hashes, and match at **≥ 70% component agreement** (threshold in
   `rule_sets(code='device_match_threshold')`). A drifted device links to the old row rather than
   creating a new one, and `reinstall_count` increments.
3. **Consent.** Fingerprinting for fraud prevention is a "legitimate use" under DPDP §7(i)
   (prevention of fraud); we still surface it in the privacy notice and log a
   `trust.consents` row with `purpose_code='fraud_prevention'` at signup. We do **not** reuse
   fingerprint data for ad targeting — that would need separate consent and we do not ask for it.

### A4.2 IP and GPS consistency

| Check | Signal emitted | Notes |
|---|---|---|
| IP ASN is hosting/datacenter | `ip_datacenter` (sev 2) | MaxMind/IPinfo via `capability='geo.ip_intel'` |
| IP is known VPN/proxy/Tor exit | `ip_anonymiser` (sev 3) | not fatal — corporate VPNs are common |
| IP country ≠ IN on a write | `ip_offshore_write` (sev 3) | NRI sellers exist; combined with new device it is sev 4 |
| GPS-to-IP-city distance > 250 km | `geo_mismatch` (sev 2) | threshold in `rule_sets(code='geo_consistency')` |
| Mock-location provider active (Android) | `gps_mocked` (sev 4) | near-deterministic fraud signal for inspection/delivery proofs |
| Listing city ≠ GPS city ≠ RTO state of the plate | `location_triangle_mismatch` (sev 3) | the classic remote-scam pattern |
| Inspector's GPS at photo time > 500 m from booking address | `inspection_location_mismatch` (sev 4) | inspector fraud, not customer fraud |

We store `ip_hash` (sha-256 + pepper) and `ip_cidr /24`, never the full IP, except in
`platform.audit_log.actor_ip` where an `inet` is retained for security-incident forensics under the
IT Act reasonable-security-practices basis. Both are registered PII columns.

### A4.3 Document OCR and tamper detection

Pipeline for every uploaded RC / PAN / insurance / PUC / DL image:

```
upload → virus scan → EXIF+ICC extract → OCR (capability 'ocr.rc' etc.)
       → field normalisation → cross-check vs VAHAN/DigiLocker
       → tamper model (capability 'ai.doc_tamper')
       → signals + document.verification_state
```

Tamper detector output components (all persisted in `risk_signals.evidence`, none as a single
opaque score):

| Component | What it catches | Model approach |
|---|---|---|
| Error Level Analysis residual | Re-saved JPEG regions after edit | classical ELA + CNN head |
| Copy-move / clone detection | Duplicated pixel blocks (digit cloning) | keypoint matching (SIFT) + RANSAC |
| Font & baseline consistency | Retyped fields in a different font/kerning | per-glyph height/stroke variance |
| Screenshot / rephotograph detection | A photo of a screen (moiré, screen-edge) | CNN classifier |
| DigiLocker provenance | The only positive proof | issuance check, not an image check |
| MRZ/checksum validity | Impossible PAN/RC serials | deterministic checksum rules |

**Judgement call encoded here:** we never auto-reject on the tamper score alone. A genuine RC
photographed under a tube light through a plastic pouch scores badly. Score ≥ threshold produces a
**case**, not a rejection; the human sees the heatmap overlay and the VAHAN comparison side by side.
The one auto-reject is *deterministic*: OCR'd registration number valid, but VAHAN returns a
different make/model/fuel — that is `vehicle.rc_adverse` and it is not a judgement.

**DigiLocker is the escape hatch.** Any user whose RC fails verification is offered the DigiLocker
flow. A DigiLocker-issued RC sets `documents.verification_state='issuer_verified'`, clears all
tamper signals for that document, and skips the case entirely. That single flow converts most of
what would otherwise be a manual queue.

### A4.4 Duplicate and multi-account detection

Three ladders, three different jobs.

**Duplicate vehicle** (already specified in domain doc §4.4 T1–T4). Trust consumes the outcome:
a T2 alarm (`vehicle.identity_conflict`) opens a `case_type_code='cloned_vehicle'` case at priority 1.

**Duplicate listing** — same car listed twice, by the same seller or by two sellers:

```
L1  exact media checksum match            → 23505 uq_vehicle_media__checksum, blocked at upload
L2  perceptual hash (pHash 64-bit) within Hamming ≤ 6 on ≥ 3 photos  → signal 'listing_media_reuse'
L3  registration number normalised match on a live listing           → listing.intent_conflict
L4  chassis last-5 + make + year match                               → duplicate candidate, case
L5  embedding similarity (CLIP-ish, cosine ≥ 0.94) + price within 5%  → soft duplicate, dedupe in search
```

pHash lookup is a Redis `BF`+ bucketed index keyed by the 16-bit prefix, so the L2 gate costs
< 15 ms at listing submit. The embedding path (L5) is async.

**Multi-account** — one human, many accounts. Deterministic joins first, graph second:

| Rung | Join key | Strength | Legitimate collisions |
|---|---|---|---|
| M1 | `pan_hash` | near-certain | none (PAN is per-person) |
| M2 | `bank_account_hash` | near-certain | joint accounts, family |
| M3 | `device fingerprint` (≥ 3 sessions each) | strong | shared family phone, cyber café, dealer's showroom tablet |
| M4 | `phone_hash` normalised (+91 variants) | certain | — |
| M5 | `upi_vpa_hash` | strong | — |
| M6 | IP /24 within 24 h + same city | weak | CGNAT, one apartment complex, one office |
| M7 | address text similarity ≥ 0.9 | weak | — |

M6 and M7 **never** produce a restriction on their own — India's CGNAT deployment makes IP a
notoriously bad join key, and an over-eager IP rule blocks an entire Jio subscriber pool. They only
raise `link_edges.weight` and feed `component_size`.

### A4.5 Velocity rules

Counters live in Redis (`INCR` + `EXPIRE`, key
`vel:{rule_code}:{subject_type}:{subject_id}:{bucket}`), sliding-window over 1-minute buckets.
Every rule is a `platform.rules` row in `rule_set(code='trust_velocity', domain='scoring')`:

```jsonc
{
  "code": "listing_create_burst",
  "priority": 20,
  "condition": {
    "and": [
      { ">": [ { "var": "counters.listings_created_24h" }, 5 ] },
      { "<": [ { "var": "actor.kyc_level" }, 4 ] },
      { "!=": [ { "var": "actor.party_kind" }, "dealer" ] }
    ]
  },
  "outcome": {
    "signal": "listing_velocity_high",
    "severity": 3,
    "action": "queue_case",
    "case_type": "curbstoning_suspected"
  },
  "stop_on_match": false,
  "explain_i18n_key": "trust.velocity.listing_burst"
}
```

Seeded velocity rules (thresholds are data; these are the launch values):

| `rule_code` | Window | Threshold | Subject | Action |
|---|---|---|---|---|
| `otp_request_burst` | 1 h | 8 | phone_hash | `throttle` → `429 trust.velocity.limit_exceeded` |
| `otp_request_device_burst` | 1 h | 20 | device | `throttle` + captcha |
| `listing_create_burst` | 24 h | 5 (non-dealer) | party | `queue_case` (curbstoning) |
| `price_edit_burst` | 24 h | 10 per listing | listing | `signal` only |
| `bid_burst` | 60 s | 15 | party | `throttle`, never block (a real dealer in a hot lot bids fast) |
| `payout_account_change` | 30 d | 2 | party | `hold_payout` + `require_step_up` |
| `chat_fanout` | 1 h | 25 distinct counterparties | party | `queue_case` (spam/disintermediation) |
| `refund_request_burst` | 90 d | 3 | party | `refund_manual_review` |
| `referral_signup_burst` | 24 h | 6 from one device | device | `block_referral_payout` |
| `failed_payment_burst` | 1 h | 6 | party | `throttle` (card testing) |
| `search_scrape` | 5 min | 600 | ip_hash + device | `throttle`, then WAF rule |

**Race condition, named:** two concurrent requests can both read a counter at threshold−1 and both
pass. For non-money rules we accept it. For `payout_account_change` and `refund_request_burst` the
check runs inside the same PostgreSQL transaction as the write, using
`SELECT count(*) ... FOR UPDATE` on the party row — correctness beats latency where rupees move.

---

## A5. The risk-scoring model

### A5.1 Shape

```
                    ┌──────────────────────────────────────────┐
   feature store ──▶│  GBDT (LightGBM), 220 trees, depth 6      │──▶ p(fraud) ∈ [0,1]
   (Redis + PG)     │  isotonic calibration on a held-out month │        │
                    └──────────────────────────────────────────┘        │
                                                                        ▼
   deterministic rules ─────────────────────────────────────────▶  max(model_score, rule_floor)
   (rule_set 'trust_hard_rules')                                        │
                                                                        ▼
                                              score = round(p × 1000) ──▶ band via 'trust_risk_band'
```

Why GBDT and not a neural net: tabular features, ~40k labelled cases in year one, hard requirement
for per-decision explanation (SHAP on a GBDT is exact and cheap), and a p95 inference budget of
120 ms including feature fetch. A deep model buys nothing here and costs auditability.

Why a rule floor on top: some facts are not probabilistic. A VAHAN blacklist hit, an NCRB stolen
flag, a PAN on the sanctions list — these set `score = 1000` regardless of what the model thinks.
Those rules carry `is_deterministic = true` and are listed in `risk_scores.rule_overlay` so the
case reviewer sees *"blocked by rule `vahan_blacklist_hit`, not by the model"*.

### A5.2 Feature catalogue (named, sourced, typed)

| Group | Feature | Type | Source | Refresh |
|---|---|---|---|---|
| Identity | `idv_level` | int 0–5 | `trust.kyc_records.kyc_level` | on change |
| | `days_since_kyc_verified` | int | `kyc_records.verified_at` | daily |
| | `pan_name_match_score` | float | `kyc_records` | on check |
| | `penny_drop_match_score` | float | `kyc_records` | on check |
| | `face_match_score`, `liveness_score` | float | `kyc_records` | on check |
| | `pan_hash_shared_party_count` | int | `trust.link_edges` (`shared_pan`) | hourly |
| Device | `device_age_days` | int | `trust.devices.first_seen_at` | request-time |
| | `device_party_count_30d` | int | `device_party_links` | hourly |
| | `device_is_emulator`, `device_is_rooted` | bool | `devices` | request-time |
| | `device_integrity_fail` | bool | Play Integrity / DeviceCheck | request-time |
| | `device_reinstall_count_7d` | int | `devices.reinstall_count` | request-time |
| Network | `ip_asn_is_datacenter`, `ip_is_anonymiser` | bool | `geo.ip_intel` | request-time |
| | `ip24_party_count_24h` | int | Redis counter | request-time |
| | `gps_ip_distance_km` | float | derived | request-time |
| | `gps_is_mocked` | bool | client attestation | request-time |
| Velocity | `listings_created_24h` / `_7d` / `_180d` | int | Redis + PG | request-time |
| | `bids_placed_1h` | int | Redis | request-time |
| | `payout_account_changes_30d` | int | PG | request-time |
| | `otp_requests_1h` | int | Redis | request-time |
| | `chat_distinct_counterparties_1h` | int | Redis | request-time |
| Content | `image_phash_reuse_count` | int | media index | on upload |
| | `image_exif_missing_ratio` | float | media pipeline | on upload |
| | `image_is_stock_or_web_prob` | float | model `ai.image_quality` | on upload |
| | `desc_contact_pattern_hits` | int | regex set (registry) | on save |
| | `price_zscore_vs_valuation` | float | Valuation module | on save |
| | `listing_completeness_ratio` | float | form schema fill rate | on save |
| Vehicle | `vahan_rc_status_adverse` | bool | `catalog` RC snapshot | on fetch |
| | `vahan_owner_name_match_score` | float | RC vs KYC name | on fetch |
| | `rc_hypothecation_open` | bool | RC snapshot | on fetch |
| | `challan_pending_count` / `_amount_paise` | int / bigint | `parivahan.challan` (per-state) | daily while live |
| | `odometer_rollback_delta_km` | int | multi-source reconciliation (A7.1) | on new reading |
| | `odometer_source_count` | int | same | on new reading |
| | `vehicle_listed_by_distinct_parties_180d` | int | `catalog` | hourly |
| | `insurance_total_loss_flag` | bool | IIB claim-history check | on fetch |
| | `insurance_claim_count_5y` | int | IIB | on fetch |
| | `largest_claim_ratio_to_idv` | float | IIB / policy OCR | on fetch |
| | `policy_ncb_percent` | int | policy OCR — a reset NCB means an undisclosed claim | on upload |
| | `repaint_panel_discontinuity` | float | `vehicle-damage-detect` (B3.2) | on upload |
| Graph | `graph_degree_shared_device` | int | `link_edges` | hourly |
| | `graph_degree_shared_bank` | int | `link_edges` | hourly |
| | `graph_component_size` | int | connected components job | 6-hourly |
| | `graph_hops_to_blocked_party` | int (0–4, 9=none) | BFS depth-4 | 6-hourly |
| | `seller_bidder_link_score` | float | `link_edges` weighted path | on bid |
| Commercial | `completed_deals_count`, `gmv_completed_paise` | int, bigint | warehouse | daily |
| | `deals_cancelled_ratio_90d` | float | warehouse | daily |
| | `refund_requests_90d`, `refund_amount_ratio_90d` | int, float | warehouse | daily |
| | `dispute_loss_ratio` | float | warehouse | daily |
| | `chargeback_count_365d` | int | payments | daily |
| | `free_look_cancellation_rate_90d` | float | insurance module — per originating agent, window from `free_look_days` (A7.8) | daily |
| | `no_show_count_90d` | int | bookings | daily |
| | `review_rating_avg`, `review_count` | float, int | reviews | daily |
| Tenure | `party_age_days` | int | `identity.parties` | request-time |
| | `is_dealer_verified`, `dealer_plan_tier` | bool, code | dealer module | on change |

**Feature store mechanics.** Request-time features are read from Redis hash
`feat:{subject_type}:{subject_id}` (TTL 15 min, written by the signal pipeline); batch features are
written nightly from the warehouse into the same hash with a longer TTL. A miss falls back to the
PostgreSQL query with a 40 ms budget; a second miss substitutes the training-set median and sets
`risk_scores.top_features[].imputed = true`. **We never fail a payment or a bid because a feature
was missing** — we score with what we have and mark the score `partial`.

### A5.3 Bands and the actions each unlocks

Bands are `platform.rules` rows in `rule_set(code='trust_risk_band', domain='scoring')`. Launch values:

| Band | Score | Auto-action | Human queue | User-visible? |
|---|---|---|---|---|
| `clear` | 0–199 | none | – | – |
| `low` | 200–399 | log signals only | – | – |
| `watch` | 400–599 | `shadow_limit_reach` (listing ranks lower in search), payout T+2 instead of T+1 | sampled 5% into `trust_review_low` | no |
| `elevated` | 600–749 | `require_inspection` on listings; `require_escrow_only`; `hold_payout` until case closes | `trust_review_std`, SLA 8 business hours | yes, with reason |
| `high` | 750–899 | `block_listing_create`, `block_bidding`, `hold_payout`, existing live listings unpublished | `trust_review_priority`, SLA 2 h | yes, with reason + appeal link |
| `blocked` | 900–1000 | `block_login` (session revoke), all restrictions, funds frozen (never confiscated) | `trust_review_l2`, SLA 1 h, four-eyes to confirm or reverse | yes, with reason + appeal link |

Three points of engineering judgement baked in:

1. **`watch` is invisible and reversible.** It costs the user reach, not access. This is where most
   false positives land, and they never learn about it. Shadow measures carry
   `restrictions.is_user_visible = false` and are capped at 14 days by `expires_at` — a shadow
   restriction that nobody revisits is a bug, so it expires itself.
2. **Money is held, never taken.** `hold_payout` writes no ledger entry. The funds stay in the
   escrow/nodal account under the deal's existing ledger position. RBI nodal-account rules do not
   permit us to sweep customer funds into our own account because we suspect the customer.
   The only lawful outcomes are: release to the intended payee, refund to the payer, or, in a
   law-enforcement matter, hold under written instruction — recorded as
   `case_actions.action_code='law_enforcement_hold'` with the reference number in `params`.
3. **`blocked` requires four-eyes to *confirm*, and four-eyes to *reverse*.** Both directions are
   dangerous. `platform.permissions(code='trust.party.block', risk_level=5,
   requires_second_approver=true)`.

### A5.4 Training, evaluation, and the label problem

- **Labels** come from `trust.cases.resolution_code ∈ {fraud_confirmed, fraud_confirmed_chargeback,
  false_positive}` plus hard outcomes (chargebacks, law-enforcement confirmations, reversed deals).
  A case resolved `insufficient_evidence` is **excluded**, not labelled negative — labelling
  ambiguity as "clean" is how fraud models rot.
- **Sampling bias correction.** Blocked users never generate the outcome we would have observed.
  We hold out a **1% random control** in the `high` band that is allowed through with monitoring
  (never in `blocked`, never above ₹2,00,000 exposure). Without it, the model only ever learns
  about the frauds we already catch. The control rate is a `feature_flags` variant so it can be
  killed instantly.
- **Metrics.** Precision@`elevated`, recall on confirmed-fraud rupees (not counts — one ₹18L cloned
  Fortuner outweighs 200 fake listings), review-queue load per 1000 sessions, and false-positive
  cost measured as *lost GMV from wrongly restricted sellers*. Target at launch: recall ≥ 0.72 of
  fraud-rupees at a review load ≤ 1.5% of active sellers/week.
- **Retraining** monthly; shadow-deploy for 14 days (A9.4) before promotion.
- **Fairness guard.** We monitor band distribution by city tier and by `preferred_locale`. A model
  version whose `blocked`-rate for any city tier exceeds 2.5× the platform mean is **not promoted**
  — it is almost always a proxy for connectivity or Android-version skew, not fraud.

---

## A6. Case management

### A6.1 The `trust_case` workflow (registry rows, not an enum)

`workflow_definitions(code='trust_case', entity_type='trust_case', version=1)`.

| state `code` | `kind_code` | `counts_as_open` | `sla_minutes` | `ui.tone` | Notes |
|---|---|---|---|---|---|
| `new` | initial | true | 30 | caution | auto-triage runs here |
| `triage` | active | true | 60 | caution | dedupe against open cases on the same subject |
| `assigned` | active | true | per priority (A6.2) | info | |
| `awaiting_user_evidence` | waiting | true | 4320 (3 d) | caution | clock pauses for the analyst SLA |
| `awaiting_external` | waiting | true | 1440 | info | VAHAN re-check, bank confirmation, insurer |
| `escalated_l2` | active | true | 120 | flag | |
| `pending_dual_control` | waiting | true | 60 | flag | a dangerous action awaits a second approver |
| `resolved_fraud_confirmed` | terminal_failure | false | – | flag | writes labels + list entries |
| `resolved_false_positive` | terminal_success | false | – | clear | lifts restrictions, writes a negative label |
| `resolved_no_action` | terminal_success | false | – | info | insufficient evidence; **not** a clean label |
| `reopened` | active | true | 120 | caution | from any terminal, within 180 days |

| transition `code` | from → to | permission | `requires_reason` | effects |
|---|---|---|---|---|
| `auto_triage` | new → triage | – (system) | no | `invoke: trust.dedupe_cases` |
| `assign` | triage → assigned | `trust.case.assign` | no | `notify: trust.case.assigned` |
| `request_evidence` | assigned → awaiting_user_evidence | `trust.case.request_evidence` | yes | `notify` (template registry) |
| `evidence_received` | awaiting_user_evidence → assigned | – (event `documents.uploaded`) | no | |
| `escalate` | assigned → escalated_l2 | `trust.case.escalate` | yes | |
| `propose_action` | *any active* → pending_dual_control | `trust.case.act` | yes | opens `platform.dual_control_requests` |
| `confirm_fraud` | escalated_l2 → resolved_fraud_confirmed | `trust.case.resolve_fraud` | yes | list entries, restrictions, `trust.case.resolved` event |
| `mark_false_positive` | *any active* → resolved_false_positive | `trust.case.resolve` | yes | lift all restrictions opened by this case |
| `close_no_action` | *any active* → resolved_no_action | `trust.case.resolve` | yes | |
| `reopen` | *any terminal* → reopened | `trust.case.reopen` | yes | guard: `now() - resolved_at < 180d` |
| `sla_breach_escalate` | assigned → escalated_l2 | – | – | timer, fired by `sla_breach_transition_code` |

### A6.2 Queues, routing and SLA

Queues are `platform.codes(set_code='trust_queue')` rows; routing is a `rule_set(code='trust_case_routing')`.

| Queue | Feeds from | Priority rule | SLA (assigned→first touch) | Staffing note |
|---|---|---|---|---|
| `trust_review_priority` | band `high`, amount at risk ≥ ₹5,00,000, auction lots live now | P1 | 30 min | on-call rotation; pages after 2× SLA |
| `trust_review_std` | band `elevated` | P3 | 8 business hours | |
| `trust_review_low` | 5% sample of `watch` | P5 | 3 business days | quality-control sampling, not enforcement |
| `trust_review_l2` | band `blocked`, cloned vehicle, law-enforcement | P1 | 1 h | senior only; dual control mandatory |
| `docs_verification` | tamper score ≥ threshold, OCR/VAHAN mismatch | P3 | 4 h | highest volume; the DigiLocker deflection is what keeps it small |
| `dealer_kyc` | dealer onboarding L4/L5 | P3 | 1 business day | shared with Admin (B2.2) |
| `disintermediation` | chat pattern hits, deal-abandoned-after-inspection | P4 | 2 business days | |
| `appeals` | user-submitted appeal on any restriction | P2 | 2 business days (DPDP grievance clock) | never the same analyst who applied it |

**Auto-assignment** is round-robin within the queue over agents whose RBAC scope covers the case's
city, weighted by open-case count, skipping anyone with an open case on a linked party (conflict of
interest is a graph query, not a policy document).

**SLA clocks pause** in `awaiting_*` states. `workflow_states.sla_minutes` drives the timer;
`workflow_timers` fires `sla_breach_escalate`. Business hours are `platform.calendar_days` +
`service_areas.operating_hours` — Diwali is not an SLA breach.

### A6.3 The case UI (admin)

Design-system language, not a generic ticket view. Layout at `88rem`, `compact` density:

```
┌─ CASE-2026-0001842 ────────────────────────── [ StatusPill: ESCALATED L2 ] ─┐
│ ┌────────────────────┐  RISK 812 / BLOCKED    ┌──────────────────────────┐  │
│ │  MH 12  DE  1433   │  model fraud-party@4.2 │ SLA  01:12 remaining      │  │
│ └────────────────────┘  ▂▃▅▇  score history   │ AMOUNT AT RISK  ₹8.4L     │  │
│                                                └──────────────────────────┘  │
│ SUBJECT            PARTY · Ramesh K · L3 · joined 11 days ago                │
│ ─────────────────────────────────────────────────────────────────────────── │
│ TOP FACTORS                          EVIDENCE            LINKED             │
│ device_party_count_30d      9  +0.24 [RC scan ⧉]         3 parties          │
│ odometer_rollback_delta_km  62k +0.19 [VAHAN diff ⧉]     2 open cases       │
│ graph_hops_to_blocked_party 1  +0.17 [chat spans ⧉]      1 blocked at hop 1  │
│ penny_drop_match_score   0.41  +0.11                                        │
│ ─────────────────────────────────────────────────────────────────────────── │
│ TIMELINE (WorkflowTimeline)     │ ACTIONS (permission-filtered)             │
│ 09:12 opened by model           │ [ Request evidence ]  [ Require inspect ] │
│ 09:12 auto_triage               │ [ Hold payout ✓applied ] [ Escalate ]     │
│ 09:40 assigned → a.deshpande    │ [ Confirm fraud ⚑ dual control ]          │
│ 11:02 escalate (reason: graph)  │ [ Mark false positive ]                   │
└─────────────────────────────────────────────────────────────────────────────┘
```

Non-negotiables in this screen:

- **Every factor row is clickable to its evidence.** A reviewer who cannot see why cannot decide.
- **The action list is rendered from `workflow_transitions` filtered by the caller's permissions** —
  the same mechanism as every other entity. No hardcoded button set.
- **PII is masked by default** (`role_permissions.field_mask`), with a "reveal" that writes an
  `audit_log` row with `action_code='trust.pii.reveal'` and `reason_text` required. Reviewers see
  masked phone `+91 98••••4433` until they justify otherwise.
- **The `PlateBlock` is the case's visual anchor** when the subject is a vehicle or listing —
  consistent with design system §5.

---

## A7. Fraud playbooks (the specific ones)

### A7.1 Odometer tampering

VAHAN does not return odometer. So we reconstruct a **km timeline** from every source we touch and
look for a non-monotonic step.

| Source | `odometer_readings.source_code` | Trust | Typical availability |
|---|---|---|---|
| Seller declaration | `seller_declared` | 0.2 | always |
| Cluster photo OCR | `ocr_cluster` | 0.7 | when the sell flow asks for it (it does) |
| AutoHub inspection | `inspection` | 0.98 | when inspected |
| PUC certificate | `puc` | 0.85 | most cars; PUC records odometer |
| Insurance claim / survey | `insurance_claim` | 0.9 | if a claim exists |
| Service record (OEM/partner) | `service_record` | 0.9 | partner coverage only |
| Prior AutoHub listing | `prior_listing` | 0.8 | repeat vehicles |
| Loan/lease agreement | `finance_doc` | 0.85 | financed vehicles |

```sql
-- catalog.odometer_readings is owned by Catalog; Trust reads it via a contract view
-- signal computation, run on every new reading
WITH ordered AS (
  SELECT reading_km, observed_on, trust_weight,
         lag(reading_km)     OVER w AS prev_km,
         lag(observed_on)    OVER w AS prev_on
    FROM catalog.v_odometer_timeline
   WHERE vehicle_id = :vehicle_id AND trust_weight >= 0.7
  WINDOW w AS (ORDER BY observed_on)
)
SELECT max(prev_km - reading_km) AS rollback_delta_km
  FROM ordered WHERE reading_km < prev_km;
```

Rules on top (registry, `rule_set='odometer_integrity'`):

- `rollback_delta_km > 2000` between two sources of weight ≥ 0.7 → signal `odometer_rollback` sev 4,
  action `require_inspection`, listing badge withheld.
- Implied usage `< 1,500 km/yr` or `> 60,000 km/yr` over ≥ 2 years → signal `odometer_implausible` sev 2.
- Declared km more than 25% below the model's expected-km-for-age band → sev 2, prompts the seller
  to upload a cluster photo before the listing can go live (a `form_bindings` conditional field —
  Form Schema registry, not a code branch).

The user-facing outcome is deliberately not an accusation. Design-system voice:
*"The odometer reading doesn't match earlier records for this vehicle. An inspection will confirm it
and unlock the Verified badge."*

### A7.2 Cloned / stolen vehicle

Trigger set: VAHAN `blacklist` status, NCRB match (via partner where available), duplicate active
`REG_NUMBER` across two vehicles (domain doc invariant), chassis-stamping mismatch found at
inspection, or an RC whose issuing RTO does not exist in `platform.rto_offices`.

Response is the only fully-automatic hard block in the system:

```
1. listing → workflow transition 'place_on_legal_hold'  (from any state)
2. vehicle → catalog flag 'identity_disputed'; all other listings of the same
   vehicle_identity → legal_hold
3. any open deal → deals workflow 'freeze'; escrow holds_funds = true
4. trust.cases (case_type='cloned_vehicle', priority 1, queue trust_review_l2)
5. trust.list_entries: chassis_masked + registration_number, source='vahan_blacklist'
6. NO notification to the seller beyond a neutral "under review" — tipping off is
   a real problem. The notification template for this case type is deliberately terse.
```

We do **not** contact police on the user's behalf automatically. We produce a **law-enforcement
pack** on request (`POST /trust/cases/{id}/export`, permission `trust.case.export_le`, four-eyes,
watermarked PDF, every export audited): RC snapshots, VAHAN responses with timestamps, photos with
EXIF, chat transcript, payment trail, device and IP history.

### A7.3 Fake RC

Detection is A4.3. The escalation ladder is what matters:

| Evidence | Verdict | Action |
|---|---|---|
| Tamper score high, VAHAN agrees on all fields | probably a bad photo | ask for a re-shoot, no case |
| Tamper score high, VAHAN unreachable | unknown | listing goes live **without** the Verified badge; `vahan.reconcile` queued |
| OCR ≠ VAHAN on owner name only | common (recent transfer) | ask for Form 29/30 or the transfer receipt |
| OCR ≠ VAHAN on make/model/fuel/chassis | deterministic fraud | `vehicle.rc_adverse`, listing rejected, case P1 |
| VAHAN has no record of the registration number at all | deterministic | reject; the plate may be fabricated |
| DigiLocker issuance present | verified | all tamper signals suppressed |

### A7.4 Curbstoning

The commercial problem: a dealer using the C2C flow pays a ₹1,999 `c2c_success_fee` instead of
dealer subscription + commission. It is a revenue leak first and a consumer-protection issue second
(private sales carry no warranty obligations).

Detection features: `listings_created_180d ≥ 4`, `pan_hash_shared_party_count`, distinct payout
accounts, GSTIN linked to the PAN, photo-background clustering (the same showroom wall behind six
"private" cars — an embedding cluster), listing-time-of-day concentration in business hours,
response latency profile, and reuse of a phone number that appears in a dealer's public listing.

Response is commercial, not punitive, and it is entirely pricing-registry driven:

```
1. trust.cases (case_type='curbstoning_suspected', queue 'disintermediation')
2. analyst confirms → action 'reclassify_party_kind' → identity.parties.party_kind_code='dealer'
3. that single field change makes fee resolution pick up the dealer fee components
   (fee_components.scope_party_tier), the dealer workflow, and the dealer form schema.
   Zero pricing code runs. Zero conditionals.
4. outstanding completed deals in the last 90 days generate a fee-difference invoice line
   ONLY where the seller agreement permits it (a rule_set flag, off at launch — we do not
   retroactively bill on a suspicion).
```

### A7.5 Shill bidding

Computed per lot at bid time and again at lot close.

```
seller_bidder_link_score(seller_party, bidder_party) =
    Σ over edge types of  weight × decay(days_since_last_seen)
    where shared_bank_account = 1.00, shared_pan = 1.00, shared_device = 0.85,
          shared_address = 0.60, referral = 0.45, shared_ip_24h = 0.20
```

| Score / pattern | Action |
|---|---|
| ≥ 0.85 or any deterministic edge | bid rejected at placement → `403 bid.self_bidding` (domain error code, already reserved) |
| 0.50 – 0.84 | bid accepted, signal `shill_suspected` sev 3, lot flagged for auction-oversight review before settlement |
| Bidder wins ≥ 3 lots from the same seller and settles none | `block_bidding` + case |
| Bid-timing autocorrelation: bidder always bids within 4 s of another specific bidder, ≥ 8 occurrences | signal `bid_collusion_pattern` sev 3 |
| Bidder registers, bids once above reserve, withdraws | signal `reserve_probe` sev 2 |

**Post-hoc void is expensive** — the underbidder may have already been notified. So the pre-bid
check must be fast: the link score for the (seller, bidder) pair is precomputed into Redis
`shill:{lot_id}:{party_id}` when the bidder registers for the auction, giving an O(1) lookup inside
the bid LUA path. If Redis is cold, the check falls back to PostgreSQL with a 20 ms budget; if that
times out we **accept the bid** and evaluate async — a rejected legitimate bid in the last 30
seconds of a lot is worse than a shill bid we void within the hour.

Voiding a bid is `auction.bid.void` (permission, `risk_level=4`), writes a compensating record
(bids are append-only), re-derives the current high bid, extends the lot by the anti-sniping window,
and notifies every participant. If the lot has already closed, the transition is
`reopen_lot_after_void`, guarded to within 24 h of close and requiring dual control.

### A7.6 Payment diversion off platform

| Layer | Mechanism |
|---|---|
| Prevention | Chat contact-masking until `deal.in_progress` (API contract §9.15). Pattern set is a registry row — `rule_set(code='chat_contact_patterns')` — covering phone formats, UPI VPAs, "@ok/@ybl/@paytm", spelled-out digits ("nine eight double three"), Devanagari digits, and image-embedded text via OCR on chat attachments |
| Detection | `deal_abandoned_after_inspection`, listing marked sold with no deal, seller's phone appearing in a review from a buyer with no deal, buyer and seller both going silent within minutes of exchanging a masked-bypass attempt |
| Commercial response | The seller agreement makes the success fee payable on a sale to a buyer introduced by the platform within 90 days. A confirmed case raises an invoice line; non-payment moves the party to `require_escrow_only` and blocks new listings |
| Consumer response | Every masked-bypass attempt shows the buyer a registry-driven warning: *"Payments made outside AutoHub are not protected. There is no escrow, no refund, and no dispute cover."* |

We do not ban the users involved on first offence. The measured behaviour is that a warning plus a
fee-recovery invoice converts better than a block, and a blocked seller takes their supply with them.

**The rail asymmetry is the whole argument, and the UI must say it.** On-platform collection runs
through the gateway (`capability='payment.collect'`, Razorpay primary → Cashfree fallback per the
Integration registry) into an RBI-compliant **nodal/escrow account**, so the money is traceable,
refundable, and disputable. An off-platform payment is a **P2P UPI transfer**, and P2P UPI has no
chargeback, no dispute rail, and no recall — NPCI reversal only covers technical failures, never
"he took the money and vanished". That is a factual difference, not marketing, so the warning states
it factually.

One deterministic detection worth naming: we store `upi_vpa_hash` for every payout instrument. When
a VPA-shaped token appears in a chat message, we hash it with the same pepper and compare against
the counterparty's registered `upi_vpa_hash`. A match is not a heuristic — it is the seller handing
over their own collection handle, and it raises `disintermediation_confirmed` (sev 4) rather than
`disintermediation_suspected`. The reverse case matters too: a VPA that matches **no** party on the
platform, sent by a seller mid-deal, is the classic mule-account shape and goes to `trust_review_l2`.

### A7.7 Refund abuse

Refund policy is `rule_set(code='refund_policy')`, and refund fees are `fee_components`
(`refund_processing_fee`, `inspection_cancellation_fee`) — never constants.

| Pattern | Signal | Action |
|---|---|---|
| ≥ 3 refund requests in 90 days | `refund_velocity` | `refund_manual_review` |
| Cancels after receiving the inspection report ≥ 2 times | `inspection_report_harvesting` | inspection fee becomes non-refundable for this party (a `restrictions.params` value, honoured by the pricing engine's context) |
| Refund requested to a bank account ≠ the funding instrument | `refund_account_mismatch` | hard block — refunds go back to source, always. This is both an AML control and an RBI expectation |
| Chargeback filed while a refund is already in flight | `double_recovery_attempt` | freeze both, case P1 |
| New account + immediate high-value booking + immediate cancel | `refund_laundering_pattern` | case P1; classic card-testing / layering shape |

**Refunds always return to source.** No exception is codable — the payout rail for a refund is
determined by `payments.payment_intents.instrument_ref`, not by user input.

### A7.8 Salvage / total-loss laundering, and insurance-side fraud

The most expensive fraud on this list per incident, and the one buyers cannot detect themselves. An
insurer declares a vehicle a **constructive total loss** (CTL — repair cost exceeds ~75% of IDV) or
a **cash-loss** settlement. The wreck is sold to a salvage buyer, rebuilt cosmetically, and re-enters
the market. Structural integrity is gone; airbags are often non-functional. Flood cars from Chennai
2015 and Kerala 2018 are still circulating.

**Why VAHAN alone does not catch it.** India has no branded-title system. VAHAN will happily show a
clean RC for a rebuilt CTL vehicle, because the RC was never cancelled — the insurer settled, the
salvage moved, and the registration carried on. So the RC check that catches a fake RC (A7.3) is
silent here. Detection has to come from the claim side.

| Source | Signal | Availability |
|---|---|---|
| **IIB** (Insurance Information Bureau, IRDAI's data arm) V-Seva / claim-history lookup by registration or chassis | `insurance_total_loss_flag`, `insurance_claim_count_5y`, `largest_claim_ratio_to_idv` | Partner-dependent; the single highest-value external check we buy |
| Policy-document OCR (A4.3) | `policy_ncb_percent` reset to 0 after years of continuity → a claim was made | Always, when a policy is uploaded |
| Insurer/broker partner (Insurance module) | Direct claim history on quote-back | Only for vehicles quoted through us |
| Own inspection | Chassis-rail straightening, weld spatter, mismatched panel gaps, silt/waterline in the spare-wheel well, corroded seat rails, ECU water marks | When inspected — the flood checklist is a `form_definitions` section, so adding a monsoon-season item is a Form Builder edit |
| Damage model | Repaint-discontinuity density across ≥ 4 adjacent panels (B3.2) | Always, on seller photos |

Response ladder — note that nothing here is a block, because a rebuilt vehicle is **legal to sell**;
it is illegal to *conceal*:

```
1. signal 'salvage_history_suspected' (sev 4)
2. restriction 'require_inspection' with params {"checklist":"structural_flood","waivable":false}
   — this is the one inspection requirement a dealer plan cannot buy its way out of
3. listing gains a DISCLOSURE badge; the seller MUST answer the
   'accident_and_claim_history' form section before the listing can leave pending_docs
4. if the seller declared "no accident" AND IIB says total loss → 'misrepresentation_confirmed',
   listing rejected, case P1, and the declaration itself is retained as evidence
5. buyer-facing: the inspection report shows the claim finding with its source and date.
   We disclose the finding, never a conclusion — "insurer recorded a total-loss settlement
   in Mar 2019" is a fact; "this car is unsafe" is a claim we are not qualified to make.
```

**IRDAI constraints that bind this module.** AutoHub is a marketplace, and when it sells insurance it
does so through a licensed entity — the Insurance module owns that relationship. Three rules land in
*this* document because they constrain Trust and AI:

| IRDAI rule | What it forbids here | Where it is enforced |
|---|---|---|
| Only a licensed intermediary (broker / corporate agent / **POSP**) may *solicit* insurance | The chatbot (B3.9) must never recommend a policy, quote a premium, compare insurers, or advise on IDV. It may state the user's own policy status and hand off | `rule_set='assistant_prohibited_topics'` — `insurance_solicitation` is a hard handoff intent, not a confidence threshold |
| POSP conduct and mandatory training/certification | A dealer or agent user cannot be shown lead-routing for insurance unless `identity.party_credentials` holds a live POSP/agent code with an unexpired validity date | `dealer-match` (B3.7) filters on the credential; expiry is a `platform.rule_sets` guard, so a lapsed certificate stops lead flow the same day, without anyone remembering to switch it off |
| Claims/underwriting data is purpose-bound | IIB claim history pulled for fraud screening may **not** be reused to price a policy quote, and vice versa | `trust.consents.purpose_code` is distinct per use (`fraud_prevention` vs `insurance_quote`); the integration route records which purpose it was called under, and `ml_predictions` for `fraud-*` models cannot read features tagged `purpose=insurance_quote` |

Mis-selling controls are the Insurance module's, but the **signal** is ours. The Finance/Insurance
spec already owns `free_look_days` as an insurer registry row and a `free_look_cancelled` workflow
transition with a brokerage clawback; Trust simply aggregates it. `free_look_cancellation_rate_90d`,
computed per **originating agent**, is a Trust feature — and it is deliberately a *rate*, not a
count, so a high-volume agent is not punished for volume. An agent above the registry threshold gets
a case in the `disintermediation` queue, because a spike there means the buyer did not understand
what they bought. The window is never a constant in code: it is whatever
`free_look_days` says for that insurer, which also means a regulator extending the free-look period
is a registry edit, not a release.

### A7.9 Encumbrance concealment — hypothecation and challans

Not glamorous, and the single most common cause of a post-sale dispute. Both liabilities follow the
**vehicle**, not the seller, so a buyer who pays and takes delivery inherits them.

| Encumbrance | Where it lives | What goes wrong | Signal |
|---|---|---|---|
| **Open hypothecation** (HP) | RC shows the financier; loan not closed | RC transfer is impossible without **Form 35** + the financier's NOC. The buyer pays, then discovers the RTO will not transfer | `rc_hypothecation_open = true` while the seller declared "loan closed" |
| **Pending challans** | PARIVAHAN / state e-challan portals, keyed by registration | Transfer is blocked at many RTOs until challans are cleared; the buyer inherits fines they did not incur | `challan_pending_count`, `challan_pending_amount_paise` |
| **Blacklist / NOC pending** | VAHAN status | Vehicle cannot be transferred at all | folded into `vahan_rc_status_adverse` |

Handling — this is a **disclosure and settlement-ordering** problem, not a fraud block:

```
1. Both are re-fetched at deal creation, not just at listing time. A challan raised
   between listing and delivery is the buyer's problem unless we re-check, so we re-check.
   Staleness budget: RC snapshot ≤ 30 d at listing, ≤ 48 h at deal creation.
2. rc_hypothecation_open forces the deal workflow down the 'loan_closure_first' path:
   escrow releases the financier's foreclosure amount directly to the financier, and the
   balance to the seller only after Form 35 / NOC is uploaded and verified.
   Two payees on one deal — the ledger already supports it; the workflow decides the order.
3. challan_pending_amount_paise > registry threshold → 'disclosure_required'; the deal's
   fee context carries it so the settlement engine can net it out of the seller's proceeds
   if both parties agree (a deal_line_item, never an off-ledger adjustment).
4. Declared-vs-actual mismatch → signal 'encumbrance_misdeclared' (sev 3), not a block.
   Most sellers genuinely do not know a challan exists.
```

**Failure mode, named:** PARIVAHAN challan endpoints are per-state, rate-limited, and frequently
down. A challan check that cannot complete must **never** block a deal — it degrades to
`challan_status='unknown'`, the buyer sees *"Challan status could not be checked at
{{timestamp}} — verify on parivahan.gov.in before transfer"* (localization registry, not a literal),
and a `vahan.reconcile` retry is queued. Showing "no pending challans" when we simply could not
reach the portal is the kind of false assurance that turns an integration outage into a legal claim.

---

## A8. Trust API, events, errors

### A8.1 Endpoints (additive to API contract §9.15)

| Method | Path | Permission | Notes |
|---|---|---|---|
| POST | `/api/v1/trust/reports` | user | Report a listing/user/message. Idempotent on `(reporter, subject, reason)` within 24 h |
| GET | `/api/v1/trust/parties/{id}/risk` | `trust.signal.read` | Score + band + top factors. Never exposed to the subject |
| GET | `/api/v1/trust/risk-signals/{subject_type}/{id}` | `trust.signal.read` | Cursor paged |
| POST | `/api/v1/trust/evaluate` | internal (mTLS/service) | Synchronous scoring for a gate; `{subject, context}` → `{score, band, actions[]}` |
| GET | `/api/v1/trust/cases` | `trust.case.read` | Filter grammar per API §7.4; scope-filtered by RBAC |
| GET | `/api/v1/trust/cases/{id}` | `trust.case.read` | Full evidence bundle, PII masked per `field_mask` |
| POST | `/api/v1/trust/cases/{id}/workflow/actions` | per-transition | Standard workflow action endpoint (registry-api §3) |
| POST | `/api/v1/trust/cases/{id}/actions` | `trust.case.act` | Apply a `trust_action`; may return `202` + dual-control request |
| POST | `/api/v1/trust/cases/{id}/export` | `trust.case.export_le` | Law-enforcement pack; step-up + four-eyes |
| GET | `/api/v1/trust/restrictions` | `trust.restriction.read` | Also `GET /me/restrictions` for the subject's own, user-visible ones |
| DELETE | `/api/v1/trust/restrictions/{id}` | `trust.restriction.lift` | Requires reason; four-eyes if applied by a `blocked` band |
| POST | `/api/v1/trust/blacklist` | `trust.blacklist.write` | Step-up; four-eyes for `identifier_type='party_id'` or permanent |
| GET | `/api/v1/trust/link-graph/{party_id}` | `trust.graph.read` | Depth ≤ 3, ≤ 500 nodes, node labels masked |
| POST | `/api/v1/trust/devices/attest` | user | Play Integrity / DeviceCheck token exchange |
| POST | `/api/v1/trust/appeals` | user | Opens an `appeals` queue case; DPDP grievance clock starts |

### A8.2 Error codes (additive to API contract §3.10)

| Code | HTTP | Condition |
|---|---|---|
| `trust.user.blacklisted` | 403 | *(existing)* |
| `trust.fraud.manual_review_required` | 202 | *(existing)* — action queued, not rejected |
| `trust.device.blocked` | 403 | *(existing)* |
| `trust.velocity.limit_exceeded` | 429 | *(existing)*, with `Retry-After` |
| `trust.restriction.active` | 403 | `params.restriction_code`, `params.expires_at`, `params.appeal_url` |
| `trust.case.transition_not_allowed` | 409 | delegates to `workflow.transition_not_allowed` semantics |
| `trust.case.conflict_of_interest` | 403 | assignee is graph-linked to the subject |
| `trust.dual_control.required` | 202 | `params.dual_control_request_id` |
| `trust.dual_control.self_approval` | 403 | maker ≠ checker, enforced |
| `trust.score.unavailable` | 200 + `meta.degraded` | model down; gate falls back to rules (A9.5) |
| `trust.appeal.duplicate_open` | 409 | one open appeal per restriction |
| `trust.evidence.expired` | 410 | signed evidence URL past TTL |

### A8.3 Domain events

Emitted through the outbox, `aggregate_type='trust_case'` / `'party'`:

```
trust.signal.raised            {subject, signal_code, severity, source}
trust.score.changed            {subject, from_band, to_band, score, model_key@version}
trust.case.opened              {case_id, case_type, subject, priority, queue}
trust.case.assigned            {case_id, assignee_user_id}
trust.case.resolved            {case_id, resolution_code, actions_applied[]}
trust.restriction.applied      {subject, restriction_code, expires_at, is_user_visible}
trust.restriction.lifted       {subject, restriction_code, lifted_by}
trust.list.entry_added         {list_code, identifier_type, reason_code}
trust.party.blocked            {party_id, case_id}            → Identity revokes sessions
trust.kyc.level_changed        {party_id, from_level, to_level}
```

Consumers: Listings (unpublish on `blocked`), Auction (drop live bids, bar registration), Payments
(hold payouts), Notifications (templated messaging), Analytics (fact tables), Search (reindex with
the new visibility).

---

## A9. Failure modes and race conditions (trust)

| # | Scenario | What breaks | Guard |
|---|---|---|---|
| 1 | Scoring service down at bid time | Every bid would fail closed | Gate falls back to `rule_set='trust_hard_rules'` only (deterministic blocks still apply); `meta.degraded=true`; scores backfilled async. **Never fail a bid on a model outage** |
| 2 | VAHAN down during sell flow | No RC verification | Cached RC ≤ 30 d, else manual-entry form version; badge withheld; `vahan.reconcile` outbox retry. Per API §11 |
| 3 | Redis flush → all velocity counters zero | Burst rules stop firing | Counters rebuild from `analytics.funnel_events` for the last window on startup (best-effort); the money-critical rules (payout change, refund) read PostgreSQL directly and are unaffected |
| 4 | Two analysts open the same case | Conflicting actions | `row_version` + `If-Match` on the case; second write → `409 concurrency.stale_write`. UI shows presence ("A. Deshpande is viewing") via the SignalR admin hub |
| 5 | Restriction applied and lifted in the same second by system + human | Order ambiguity | `restrictions` is append-only in effect: lifting sets `lifted_at`, never deletes. The **most restrictive live row wins** at read time, so ordering does not change the outcome |
| 6 | Model promotes with a broken feature (all-null) | Mass false positives | Promotion gate requires 14-day shadow with feature-null-rate < 2% and band-distribution KL divergence < 0.15 vs. incumbent. Auto-rollback if `blocked`-rate > 3× baseline in any 1-hour window |
| 7 | Party blocked while an escrow-funded deal is mid-settlement | Funds stranded | `block_login` never freezes an in-flight settlement that is past `funds_released`. Freezing happens at `holds_funds` states only; the deal workflow decides, Trust only requests |
| 8 | Duplicate case storm (one bad actor, 40 listings) | Queue flooded | `auto_triage` dedupes: an open case on the same `(subject_type, subject_id, case_type)` absorbs the new signals into the existing case and bumps priority instead of creating a row |
| 9 | Appeal from a user whose account is blocked | Cannot log in to appeal | Appeals accept an unauthenticated flow keyed by phone OTP + case public_ref, rate-limited hard. A block that cannot be appealed is a DPDP grievance failure |
| 10 | Analyst is the fraudster | Internal fraud | Conflict-of-interest graph check on assignment, four-eyes on all `risk_level ≥ 4` actions, `audit_log` immutability, and a weekly report of analysts whose false-positive-reversal rate is a statistical outlier |
| 11 | IIB / claim-history provider unavailable | Salvage check silent; a CTL vehicle could list clean | `insurance_total_loss_flag` becomes `unknown`, **never `false`**. The listing publishes but cannot earn the `no_major_claim` disclosure badge, and the check is queued. A tri-state (`true`/`false`/`unknown`) is mandatory on every external assurance flag in this module — a nullable boolean rendered as "clean" is how an outage becomes a mis-selling claim |
| 12 | PARIVAHAN state portal down at deal creation | Challan liability unverified | Deal proceeds with `challan_status='unknown'` surfaced to **both** parties with the attempt timestamp (A7.9). Never rendered as "no challans" |
| 13 | Buyer's own vehicle appears in their fraud-screening features via an insurance quote pull | Purpose-limitation breach under DPDP + IRDAI data norms | Feature-store rows carry the `purpose_code` they were collected under; `fraud-*` models are configured with an allowed-purpose list and silently drop out-of-purpose features (imputing the median), rather than failing. Enforced in the feature-fetch layer, so no model author can opt out |

---

# PART B — AI FEATURE CATALOGUE

## B1. The model registry — every model is data

No model endpoint, name, threshold or version appears in application code. Models are registered
twice, and the two registrations do different jobs:

1. **`platform.capabilities` + `platform.provider_routes`** (Registry #8) — *how to call it*:
   which provider serves `ai.damage_detect` today, with what timeout, retry, breaker and fallback
   chain. Swapping a self-hosted model for a vendor API is two `provider_routes` UPDATEs.
2. **`analytics.ml_models` / `ml_model_versions`** — *what it is*: lineage, training data window,
   metrics, thresholds, champion/challenger status, promotion history.

```sql
CREATE TABLE analytics.ml_models (
    key            text PRIMARY KEY,               -- 'listing-image-quality','fraud-party','price-reco'
    i18n_key       text NOT NULL,
    capability_code platform.code_key NOT NULL REFERENCES platform.capabilities(code),
    task_type      platform.code_key NOT NULL,     -- 'classification','regression','detection','ranking','embedding','generation','asr'
    owner_user_id  uuid,
    is_human_in_loop boolean NOT NULL DEFAULT true,
    decision_impact platform.code_key NOT NULL,    -- 'advisory','gating','automatic'  ← governs approval level
    created_at timestamptz NOT NULL DEFAULT now(), created_by uuid,
    updated_at timestamptz NOT NULL DEFAULT now(), updated_by uuid,
    row_version bigint NOT NULL DEFAULT 1
);

CREATE TABLE analytics.ml_model_versions (
    id             uuid PRIMARY KEY DEFAULT platform.uuid_generate_v7(),
    model_key      text NOT NULL REFERENCES analytics.ml_models(key),
    version        text NOT NULL,                  -- '4.2', semantic within the model
    artifact_uri   text NOT NULL,                  -- s3://models/fraud-party/4.2/model.txt
    artifact_sha256 platform.sha256_hex NOT NULL,
    framework      platform.code_key NOT NULL,     -- 'lightgbm','onnx','torchscript','vendor_api'
    training_data_from date, training_data_to date,
    feature_list   text[] NOT NULL DEFAULT '{}',
    hyperparams    jsonb NOT NULL DEFAULT '{}',
    eval_metrics   jsonb NOT NULL DEFAULT '{}',    -- {"auc":0.91,"recall_at_p90":0.72,"mape":0.081}
    thresholds     jsonb NOT NULL DEFAULT '{}',    -- named cut-points, referenced by rules
    status_code    platform.code_key NOT NULL DEFAULT 'draft',
    -- draft|shadow|canary|champion|retired|rolled_back   ← workflow registry: 'ml_model_version'
    workflow_instance_id uuid REFERENCES platform.workflow_instances(id),
    traffic_percent numeric(5,2) NOT NULL DEFAULT 0,
    promoted_at    timestamptz, promoted_by uuid, approved_by uuid,
    retired_at     timestamptz,
    model_card_uri text NOT NULL,                  -- REQUIRED: purpose, data, limits, fairness notes
    created_at timestamptz NOT NULL DEFAULT now(), created_by uuid,
    updated_at timestamptz NOT NULL DEFAULT now(), updated_by uuid,
    row_version bigint NOT NULL DEFAULT 1,
    CONSTRAINT uq_ml_model_versions__key_version UNIQUE (model_key, version)
);
CREATE UNIQUE INDEX uq_ml_model_versions__champion ON analytics.ml_model_versions (model_key)
    WHERE status_code = 'champion';    -- exactly one champion per model, enforced by the database

CREATE TABLE analytics.ml_predictions (            -- append-only, partitioned monthly
    id             uuid NOT NULL DEFAULT platform.uuid_generate_v7(),
    occurred_at    timestamptz NOT NULL DEFAULT now(),
    model_key      text NOT NULL,
    model_version  text NOT NULL,
    subject_type   platform.code_key NOT NULL,
    subject_id     uuid NOT NULL,
    input_hash     platform.sha256_hex NOT NULL,   -- dedupe + cache key; inputs not stored
    output         jsonb NOT NULL,
    confidence     numeric(4,3),
    latency_ms     integer NOT NULL,
    served_from    platform.code_key NOT NULL,     -- 'model','cache','fallback','shadow'
    cost_paise     platform.money_paise,
    correlation_id uuid,
    CONSTRAINT pk_ml_predictions PRIMARY KEY (id, occurred_at)
) PARTITION BY RANGE (occurred_at);

CREATE TABLE analytics.ml_feedback (               -- the HITL loop, closed
    id             uuid PRIMARY KEY DEFAULT platform.uuid_generate_v7(),
    prediction_id  uuid,
    model_key      text NOT NULL,
    model_version  text NOT NULL,
    subject_type   platform.code_key NOT NULL,
    subject_id     uuid NOT NULL,
    human_label    jsonb NOT NULL,
    agreed         boolean,                        -- did the human agree with the model?
    reviewer_id    uuid,
    review_source  platform.code_key NOT NULL,     -- 'moderation_queue','trust_case','user_correction','ops_audit'
    reviewed_at    timestamptz NOT NULL DEFAULT now(),
    created_at timestamptz NOT NULL DEFAULT now()
);
```

**`decision_impact` is the governance dial.** `advisory` models can be promoted by the ML owner.
`gating` models (they block a user action) need a second approver. `automatic` models (they take an
action with no human in the path) need a Super Admin approval and a documented rollback trigger.
Today exactly two models are `automatic`: image-quality auto-reject for corrupt/blank uploads, and
duplicate-media exact-hash blocking. Everything else is `advisory` or `gating`.

## B2. Serving contract

```
client / handler
   │  IModelClient.PredictAsync(capability, input, ct)
   ▼
Integration Registry route resolution  (primary → fallback → shadow, breaker state from Redis)
   │
   ├─▶ cache: Redis key  ml:{model_key}:{version}:{input_hash}   (TTL per capability)
   ├─▶ champion  (traffic_percent)          ──▶ response
   ├─▶ canary    (traffic_percent)          ──▶ response, tagged
   └─▶ shadow    (fire-and-forget, no wait) ──▶ ml_predictions only, never the response
```

Rules:
- **Shadow calls can never affect latency or the response.** They run on a bounded channel; if the
  channel is full they are dropped and a counter increments.
- **Every prediction writes `ml_predictions`** — including cache hits (`served_from='cache'`), because
  drift monitoring needs the input distribution, not just the compute events.
- **Timeouts are per-route**, and every AI call has a defined `fallback` that is *not* an error page.
- **Cost is tracked per call** (`cost_per_call_paise` on the route); the Super Admin dashboard shows
  ₹/day per model. An LLM feature that quietly costs ₹40,000/month must be visible on day two.

## B3. Feature specifications

### B3.1 Image quality & compliance — `listing-image-quality`

| | |
|---|---|
| **Purpose** | Stop unusable listings before a human ever sees them; tell the seller exactly what to re-shoot |
| **Inputs** | Image bytes, declared angle slot (front-3/4, rear-3/4, interior, odometer, engine bay, tyres), category, `exif`, upload context |
| **Model approach** | Small CNN (EfficientNet-lite, ONNX, CPU) with 6 heads: blur (Laplacian + learned), exposure, resolution/upscale detection, angle-vs-slot match, watermark/stock-photo probability, forbidden-content (people's faces, competitor watermarks, obscene). Plate-region detector feeds the optional plate-masking step |
| **Latency** | p95 400 ms sync per image (returns the retake hint while the upload progress bar is still moving); full set async ≤ 4 s |
| **Failure/fallback** | Provider down → accept the image, mark `media_quality='unscored'`, listing may still publish, and a backfill job scores it later. Never block a sell flow on a quality model |
| **HITL** | Moderation queue sees images scored `reject` and `borderline`; every override writes `ml_feedback` |
| **Success metric** | % listings reaching `live` on first submit (target +18pp), moderator images-reviewed per 1000 listings (target −60%), buyer-reported "photos don't match" rate |
| **Registry** | `capability='ai.image_quality'`, thresholds in `ml_model_versions.thresholds` and *referenced* by `rule_set='media_quality_gate'` |

### B3.2 Damage detection & severity — `vehicle-damage-detect`

| | |
|---|---|
| **Purpose** | Pre-fill the inspection checklist and flag undisclosed damage on seller photos |
| **Inputs** | Image set with panel-slot labels, vehicle body type from taxonomy, inspection context flag |
| **Model approach** | Two-stage: panel segmentation (Mask R-CNN / SAM-distilled) → per-panel damage classifier (scratch / dent / rust / crack / repaint / panel-gap / missing) with severity 1–4. Repaint detection uses colour-histogram discontinuity across adjacent panels, which is what human inspectors actually look at |
| **Latency** | Async, ≤ 8 s per 12-image set; inspector app shows a skeleton and fills in |
| **Failure/fallback** | Inspector fills the checklist manually — the form is a Form Schema render either way; AI only pre-fills. `inspection.ai_assisted = false` on the record |
| **HITL** | **Always.** The inspector confirms or corrects every pre-filled item; the diff is the training label. AI never writes an inspection grade |
| **Success metric** | Inspector time per inspection (target −25%), inspector correction rate < 20%, post-sale damage disputes per 1000 inspected sales |
| **Registry** | `capability='ai.damage_detect'`; checklist item mapping is `form_definitions` field codes, so a new checklist item needs no model change |

### B3.3 Duplicate & fake listing detection — `listing-dup-fake`

| | |
|---|---|
| **Purpose** | Kill duplicate supply (which destroys buyer trust and search quality) and catch fabricated listings |
| **Inputs** | Image pHashes + embeddings, title/description text, price, registration/chassis fragments, seller party features, posting-time metadata |
| **Model approach** | Retrieval + classifier. Retrieval: pHash Hamming ≤ 6 and image-embedding ANN (HNSW in OpenSearch, cosine ≥ 0.94). Classifier: GBDT over pair features (image sim, text sim, price delta, km delta, city delta, seller link score) → `duplicate` / `same-vehicle-relist` / `distinct`. Fake head: price-vs-valuation z-score, stock-photo probability, description-template similarity to known scam corpus, contact-pattern hits |
| **Latency** | Sync gate 150 ms (pHash + exact keys) at submit; deep pass async ≤ 3 s |
| **Failure/fallback** | Sync gate degrades to exact checksum + registration-number uniqueness only (both are database constraints, so correctness survives); deep pass queued |
| **HITL** | `duplicate` with confidence ≥ threshold auto-collapses in search (both stay live, one is canonical); below threshold goes to the moderation queue |
| **Success metric** | Duplicate rate in top-50 search results (target < 1.5%), scam-listing reports per 10k live listings, false-collapse appeals |
| **Registry** | `capability='ai.listing_dedupe'`; the collapse threshold is a `rule_set='search_dedupe'` row so Search Ops tunes it without a model release |

### B3.4 Price recommendation — `price-reco`

| | |
|---|---|
| **Purpose** | Tell a seller what their car is worth and what price will actually sell; power the homepage plate-input valuation |
| **Inputs** | make/model/variant/year/km/fuel/transmission/owners/city/RTO/colour/insurance validity/accident history/inspection grade; live supply-demand features (`active_listings_same_variant_city`, `median_days_to_sell_variant_city`, `search_impressions_30d`); seasonality; OEM price changes |
| **Model approach** | LightGBM quantile regression at τ ∈ {0.1, 0.5, 0.9} → a **band**, not a point. Plus a "sell-fast price" = the τ where predicted days-to-sell ≤ 21 from a companion survival model (Cox / GBDT-AFT). Depreciation-curve fallback model from the registry when data is thin (< 30 comparables) |
| **Latency** | p95 300 ms. Non-negotiable: the odometer roll animation (design system §5) is 600 ms and the number must be there when it lands |
| **Failure/fallback** | Registry depreciation curve → band widens, `fallback_used=true`, UI **must** label it an estimate (domain doc §18.1). Never present a fallback as a model price |
| **HITL** | Category managers review weekly variant-level MAPE outliers and can pin an override band per variant×city in `rule_set='valuation_overrides'`; overrides expire in 60 days by default |
| **Success metric** | MAPE vs. actual sale price ≤ 8% at p50; % of listings priced inside the recommended band (target 62%); days-to-sell for in-band vs. out-of-band listings |
| **Registry** | `capability='valuation.model'`; `valuation.method_code='ml_model'` with `model_key`/`model_version` frozen on the record (domain doc §10) |

### B3.5 Demand prediction — `demand-forecast`

| | |
|---|---|
| **Purpose** | Tell dealers what to stock and Ops where to push acquisition; drive the requirement board |
| **Inputs** | 24 months of `analytics.fact_lead`, `fact_deal`, `search_events` (including zero-result searches), inventory levels, price bands, festival/season calendar (`platform.calendar_days`), fuel-price series, new-model launch dates |
| **Model approach** | Hierarchical forecast at `(city × category × make × model × price_band × week)` with reconciliation to city totals. Gradient-boosted trees on lagged/rolling features beat classical seasonal models here because launches and price shocks dominate. Zero-result searches enter as an unmet-demand covariate |
| **Latency** | Batch, nightly, ≤ 40 min for the full cube; served from `analytics.rollup_demand_forecast` in < 50 ms |
| **Failure/fallback** | Serve the previous run with `as_of` clearly displayed (design system §7: *a number without its date is a rumor*). After 72 h stale, the widget shows the trailing 4-week actual instead of a forecast |
| **HITL** | Category managers can annotate a forecast (`analytics.forecast_annotations`) — "Nexon EV price cut on 12 Aug" — which becomes a feature in the next run |
| **Success metric** | wMAPE at city×category×week ≤ 18%; dealer stock-to-sell ratio for forecast-following dealers vs. not |
| **Registry** | `capability='ai.demand_forecast'`; the cube dimensions come from the Taxonomy registry, so adding "Construction Equipment" adds a forecast slice with no code change |

### B3.6 Best-selling city / market opportunity — `market-opportunity`

| | |
|---|---|
| **Purpose** | Answer "where should I sell this?" for a seller and "where should we open next?" for Ops |
| **Inputs** | Per city×variant: realised price premium, days-to-sell, demand-supply ratio, transport cost (distance × per-km rate from `fee_components`), inter-state RTO/NOC friction, road-tax differential, buyer-search volume |
| **Model approach** | Mostly deterministic scoring over warehouse aggregates, not ML: `net_advantage_paise = predicted_price(city_b) − predicted_price(city_a) − transport_cost − rto_transfer_cost − expected_holding_cost(days_to_sell)`. The only learned components are the price and days-to-sell models it calls. This is deliberate — a transparent formula is defensible to a dealer; an opaque score is not |
| **Latency** | p95 200 ms (precomputed matrix, top-5 cities per variant) |
| **Failure/fallback** | Show realised-price-by-city table for the last 90 days with no recommendation |
| **HITL** | Ops reviews the top-20 recommended corridors monthly; a corridor can be suppressed (e.g. an RTO with a 6-week NOC backlog) via `rule_set='market_corridor_suppression'` |
| **Success metric** | Attach rate of the "sell in another city" suggestion; realised premium for sellers who took it minus transport cost |
| **India note** | Inter-state resale means NOC + Form 28/29/30 + road-tax refund in the origin state and fresh road tax in the destination. The formula must include it or the advice is wrong. Those amounts are `fee_components` with `scope_state_id` |

### B3.7 Dealer recommendation / lead routing — `dealer-match`

| | |
|---|---|
| **Purpose** | Route a buyer lead or a sell-lead to the dealer most likely to close it, fairly |
| **Inputs** | Lead features (category, budget band, city, urgency, finance interest); dealer features (inventory match, historical close rate for this segment, median response time, review rating, current open-lead load, subscription tier, distance) |
| **Model approach** | Learning-to-rank (LambdaMART) on historical lead→deal outcomes, then a **fairness/limit layer**: per-dealer daily lead caps from the subscription plan, a minimum floor for new dealers (cold-start needs data), and round-robin tie-breaking within a score band |
| **Latency** | p95 150 ms — it is inside the lead-creation transaction path |
| **Failure/fallback** | Deterministic fallback: inventory match → distance → rating → round-robin. It is genuinely good, which is why we can afford to fail over to it silently |
| **HITL** | Ops can pin or exclude a dealer for a segment; dealers see *why* they got a lead (match reasons), which is what makes the ranking trusted rather than resented |
| **Success metric** | Lead-to-deal conversion (target +22% vs. round-robin), median first-response minutes, Gini coefficient of lead distribution among comparable dealers (must not worsen) |
| **Registry** | Caps and floors are `feature_flags` variants + `rule_set='lead_routing'`; the subscription tier's cap is a plan attribute, not a constant |

### B3.8 Fraud scoring — `fraud-party`, `fraud-listing`, `fraud-transaction`

| | |
|---|---|
| **Purpose** | Part A. Three sibling models sharing a feature store |
| **Inputs** | A5.2 |
| **Model approach** | Calibrated LightGBM + deterministic rule floor; SHAP top-4 persisted per decision |
| **Latency** | p95 120 ms inline (bid/payout/listing gates), incl. feature fetch; async re-score on every new signal |
| **Failure/fallback** | Hard rules only, `meta.degraded=true`, backfill; **never fail closed on bids or payments** (A9.1) |
| **HITL** | The entire case system (A6). `resolution_code` is the label |
| **Success metric** | A5.4 |
| **Governance** | `decision_impact='gating'` — a second approver is required to promote a version |

### B3.9 Chatbot — `assistant-chat`

| | |
|---|---|
| **Purpose** | Deflect support volume and answer pre-purchase questions in the user's language, at 11pm, when no dealer is awake |
| **Inputs** | User message, conversation history (bounded), user context (active listings, open deals, city, locale), retrieved documents |
| **Model approach** | RAG over a governed corpus — help articles, policy pages, the user's own entities, and live registry data (fees, workflow state meanings, feature availability). Intent classifier in front routes to: (a) deterministic tool calls (`get_deal_status`, `get_valuation`, `book_test_drive`, `raise_ticket`), (b) RAG answer, (c) human handoff. LLM served via `capability='ai.llm_chat'` with a fallback provider |
| **Latency** | First token < 1.2 s, full answer < 6 s. Tool-call answers (status lookups) bypass the LLM entirely and return in < 200 ms |
| **Failure/fallback** | Provider down → deterministic intent routing only, plus "connect me to support". Degraded banner per API §11 |
| **HITL** | Confidence below threshold, any money/legal/dispute topic, and any second failed attempt hand off to the support console with the full transcript. **The bot never states a fee, a commission, a tax rate, or a legal position from generative text** — those come from a pricing-registry tool call, verbatim |
| **Regulated-topic hard stop** | `rule_set='assistant_prohibited_topics'` routes to a human *before* generation, not after: **insurance solicitation** (IRDAI — only a licensed intermediary may recommend, quote or compare policies; the bot may state the user's own policy status and nothing more), **loan eligibility or interest-rate promises** (a lender's underwriting decision is not ours to predict), and **legal advice on ownership, challan liability or accident claims**. These are intent matches, not confidence thresholds — a *confident* wrong answer here is the expensive one |
| **Success metric** | Containment rate (resolved without human) target 55%, CSAT on bot-handled ≥ 4.1/5, escalation-after-bot rate < 12%, hallucination rate on a weekly 200-sample audit = 0 for money/legal claims |
| **India note** | Hindi/Marathi/Tamil/Telugu/Kannada/Bengali/Gujarati input including Romanised Hinglish ("gaadi ka kagaz kab milega"). Responses use the Localization registry for all fixed strings; only the free-form portion is generated |

### B3.10 Voice assistant — `assistant-voice`

| | |
|---|---|
| **Purpose** | Search and status by speech — for two-wheeler sellers on a phone, and for inspectors with greasy hands |
| **Inputs** | Audio stream, locale hint, screen context |
| **Model approach** | Streaming ASR (`capability='ai.asr'`) tuned for Indian-accented English + code-switched Hinglish, with a domain lexicon boost (make/model/variant names from the Taxonomy registry — "Alto K10", "Bolero", "Activa 6G" are the tokens generic ASR gets wrong). Intent+slot extraction over the transcript, then the same tool layer as B3.9. TTS optional per locale |
| **Latency** | Partial transcript < 400 ms, final < 900 ms after speech end, intent < 200 ms after final |
| **Failure/fallback** | Falls back to the text field with the partial transcript pre-filled — never a dead end |
| **HITL** | Low-confidence slots become a spoken confirmation ("Swift, 2019, Pune — correct?") rather than a wrong search |
| **Success metric** | Word error rate on the domain lexicon < 12%, task completion rate ≥ 70%, % of voice sessions abandoned to text |
| **Registry** | The ASR lexicon is generated nightly from the Taxonomy registry — a new model added by a category manager is speakable the next day, with no ML work |

### B3.11 Auto description — `listing-autodesc`

| | |
|---|---|
| **Purpose** | Remove the blank-textarea drop-off in the sell flow; produce consistent, honest, SEO-usable copy |
| **Inputs** | Structured listing attributes (EAV, so it works for any category), inspection findings if present, damage-detection output, RC facts, taxonomy labels, target locale |
| **Model approach** | Template-constrained generation. A registry-held skeleton (`notification_templates`-style, `template_code='listing_desc_v3'`) supplies sections; the LLM fills only the prose spans, and is given **facts only** — it cannot invent a service history it was not handed. Post-generation validator re-extracts claims and rejects any number not present in the input |
| **Latency** | p95 2.5 s, streamed into the textarea so the seller watches it appear |
| **Failure/fallback** | Deterministic template fill from attributes ("2019 Maruti Swift VXi, 42,300 km, 1 owner, petrol, Pune") — plain, correct, boring, always available |
| **HITL** | **The seller is the human in the loop.** The text lands in an editable field, never auto-published. Edited-vs-generated diff is stored as feedback |
| **Success metric** | Sell-flow completion rate at the description step (+15pp), % of generated text kept unedited, zero factual-claim violations in the weekly audit, organic search impressions per listing |
| **Guardrail** | Banned claim categories are registry rows (`rule_set='autodesc_prohibited_claims'`): no "accident-free" unless inspection says so, no "genuine km" ever, no warranty promise, no "single-hand driven" without RC owner count = 1 |

### B3.12 Supporting models (registered identically, used by the above)

| Model key | Capability | Used by |
|---|---|---|
| `doc-ocr-rc` / `doc-ocr-pan` / `doc-ocr-odometer` | `ocr.rc`, `ocr.pan`, `ocr.odometer` | A4.3, A7.1, KYC |
| `doc-tamper` | `ai.doc_tamper` | A4.3 |
| `image-plate-detect` | `ai.plate_detect` | plate masking on public photos, `PlateBlock` extraction |
| `text-contact-redact` | `ai.contact_redact` | chat masking, description scrubbing |
| `image-embed` / `text-embed` | `ai.embed_image`, `ai.embed_text` | dedupe, semantic search, curbstoning photo clustering |
| `review-abuse` | `ai.text_abuse` | review + chat moderation queue |

## B4. Cross-cutting AI rules

| Rule | Why |
|---|---|
| Every AI-influenced user-visible outcome is **labelled as AI-assisted** | DPDP transparency + it is simply honest. "AI estimate" on a valuation band, "AI-suggested" on a description |
| No model output is a **final adverse decision** without either a deterministic rule or a human | Appealability. `decision_impact` enforces the policy |
| Every model has a **fallback that is not an error** | An AI outage must degrade the product, not break it |
| Model artefacts are **content-addressed** (`artifact_sha256`) and pinned per version | Reproducing a 2026 valuation in a 2029 dispute must be possible |
| Prompts and templates are **registry rows**, versioned like any other artifact | A prompt change is a business change; it gets a diff, a preview and a rollback |
| Training data honours **erasure** | The nightly training-set build excludes parties with an executed `trust.erasure_requests`; already-trained models are not retrained for erasure (the model holds no personal data), which is the position we document in the model card |
| **No user PII in prompts** beyond what the user is already looking at | Chat context is scoped to the caller's own entities; a prompt-injection in a listing description cannot make the assistant read another user's deal, because the tool layer applies the same RBAC scope guard as the REST API |

---

# PART C — ADMIN PANEL

## C1. Shape of `apps/admin`

One Next.js app, two personas (Admin, Super Admin), zero role checks in components. Navigation is
built from `platform.modules` + the caller's resolved permissions returned by
`GET /identity/auth/context`. A nav item the user cannot use does not render; a route they reach
directly returns the standard 403 body.

```
apps/admin
├── (queues)        moderation · dealers · listings · auctions · disputes · refunds · trust cases
├── (registry)      taxonomy · forms · workflows · pricing · templates · flags · i18n · geo
├── (commerce)      coupons · referrals · CMS · campaigns
├── (support)       ticket console · user lookup · impersonation
├── (system)        settings · tax · api-keys · audit · dr            ← Super Admin
└── (insight)       dashboards · metric catalogue · exports
```

Density is `compact`, width `88rem`, `DataTable` everywhere, `StatusPill` tone always from
`workflow_states.ui.tone` or `codes.ui.tone`. An admin screen that picks its own colour for a status
is a bug.

## C2. Queues

Every queue is the same component over a different saved filter, and every queue's row actions come
from `workflow_transitions` filtered by permission. This is why adding a queue is configuration.

| Queue | Source | Key decisions | SLA | Bulk actions |
|---|---|---|---|---|
| **Listing approval** | `listings` in `pending_approval` | approve / reject (reason set `listing_reject_reason`) / request changes | 4 h (`workflow_states.sla_minutes`) | approve N (guarded: only rows with AI score `clear`), reject N with one reason |
| **Media moderation** | images scored `borderline`/`reject` | keep / replace-request / remove | 8 h | – |
| **Dealer approval** | `dealer_onboarding` workflow | verify GSTIN, trade certificate, PAN, address, showroom photos; assign plan | 1 business day | – |
| **Auction oversight** | live + just-closed lots | pause lot, extend, void bid, cancel lot, force-settle | live | – |
| **Dispute desk** | `deals` in dispute states | evidence review, partial refund, release escrow, chargeback response | 3 business days | – |
| **Refunds** | refund requests | approve / partial / reject; four-eyes above threshold | 2 business days | – |
| **Trust cases** | Part A | A6 | per band | – |
| **Payout exceptions** | held / failed payouts | retry, change account (step-up), release, refund | 1 business day | – |
| **Review moderation** | flagged reviews | publish / redact / remove / respond-on-behalf (never) | 2 business days | – |

**Bulk actions are dangerous and are treated as such.** Any bulk operation over
`platform.rule_sets(code='admin_bulk_limits')`'s threshold (launch: 50 rows, or any row where
`amount_at_risk ≥ ₹2,00,000`) requires step-up auth and produces **one** `dual_control_request`, not
N of them. The executor is idempotent per row and writes a single `audit_log` row per row with a
shared `correlation_id`, so "what did that bulk approve actually do" is one query.

### C2.1 Auction oversight specifics

Live auctions are the highest-stakes admin surface. The console shows every live lot with current
bid, bidder count, time remaining, extension count, and any `shill_suspected` flag, over the same
SignalR hub the bidders use (so the moderator sees exactly what they see, at the same time).

| Action | Permission | Guard | Effect |
|---|---|---|---|
| Pause lot | `auction.lot.pause` | lot is live | clock stops, bids rejected with `bid.lot_paused`, all participants notified |
| Extend lot | `auction.lot.extend` | ≤ 3 extensions (registry) | new `ends_at`, notify |
| Void bid | `auction.bid.void` | A7.5 | compensating record, re-derive high bid, extend |
| Cancel lot | `auction.lot.cancel` | no settled deal | deposits released, seller notified, reason mandatory |
| Force close | `auction.lot.force_close` | risk_level 4, four-eyes | closes at current high bid |

### C2.2 Dispute desk and refunds — the money rules

- A refund is **never** an ad-hoc ledger write. The desk raises a refund on the deal; Payments posts
  the double-entry rows (domain doc §15). Admin cannot touch `payments.*` (schema grant).
- Refund fees come from `fee_components` (`refund_processing_fee`), and whether a fee applies at all
  is `rule_set='refund_policy'` — city, category, cancellation stage and cause all feed it.
- Partial refunds must specify which `deal_line_items` are being reversed. "Refund ₹12,000" with no
  line attribution is rejected at the API — it would make the GST credit note unissuable.
- A refund on an invoiced amount generates a **credit note** with its own IRN (e-invoicing), not a
  reversal of the original invoice. An issued tax invoice is immutable.

## C3. Support console

- **User lookup** by phone/email/PAN-last4/plate/deal ref/case ref, with PII masked until revealed
  (audited, reason required).
- **A 360 timeline** that is a merge of `platform.workflow_history`, `platform.audit_log`,
  `notification.messages`, `payments.ledger_entries` and `trust.risk_signals` for that party —
  one chronological instrument view. This single screen removes most "check with engineering" tickets.
- **Impersonation** (`identity.user.impersonate`, `risk_level=5`): time-boxed to 30 minutes, requires
  a reason and a linked ticket ref, **never** available for money actions (payout, bid, payment
  confirmation are hard-blocked in the impersonated session), shows a persistent banner in every
  client, notifies the user by their preferred channel, and writes an `audit_log` row on entry, exit
  and every action with `actor_kind='impersonation'` plus the real actor id.
- **Ticketing** uses the same case machinery as Trust with a different `workflow_definitions.code`
  (`support_ticket`) and different queues — one engine, two products.

## C4. CMS, coupons, referrals

| Surface | Storage | Notes |
|---|---|---|
| CMS pages / banners / FAQ | `platform.cms_blocks` (slug, locale, workflow `cms_block`, scheduled publish) | Draft → review → scheduled → published → archived, all workflow-registry states. Content is locale-scoped, so a Marathi homepage banner is a row |
| Coupons | `platform.promotions` + `promotion_rules` (JsonLogic eligibility) + `fee_components` with negative outcome | A coupon is a **discount rule on a fee**, not a separate money path. `SAVE500` on `rc_transfer_assist` is a rule row; the ledger sees one fee line and one discount line, both with their own GL accounts |
| Referrals | `platform.referral_programs` + `identity.referrals` | Reward is a `fee_components` credit or a wallet credit; payout is gated on the referee's first *completed* deal, not signup (A7 referral farming). `block_referral_payout` restriction is honoured here |
| Campaigns | `notification_events` + `notification_templates` + audience `rule_set` | Sends respect DLT template registration, TRAI DND, quiet hours from `platform.calendar_days`, and per-user channel opt-outs. An unregistered DLT template cannot be published — the editor validates against the registered header/template id before allowing `published` |

## C5. THE REGISTRY EDITORS — where the no-deploy promise becomes real

All six editors share one lifecycle, implemented once:

```
   DRAFT ──validate──▶ VALID ──preview──▶ REVIEWED ──approve──▶ SCHEDULED ──apply──▶ APPLIED
     ▲                    │                                                            │
     └──── edit ──────────┘                                              rollback ◀────┘
```

Backed by `platform.registry_change_sets` + `registry_entity_versions` (data model §3.12). The
change set is the unit of everything: one diff, one approval, one apply, one rollback, one audit
trail. **You cannot edit a registry outside a change set** — the write endpoints require a
`change_set_id`, and direct SQL is blocked by grants.

Shared mechanics:

| Stage | What happens | Failure |
|---|---|---|
| **Validate** | Structural (JSON Schema), referential (every code resolves), semantic (per-editor rules below), and impact (how many live rows this touches) | `422` with a field-path list, rendered inline |
| **Preview** | Dry-run against real data in a read-only transaction that is always rolled back. Every editor defines its own preview (below) | Preview failure blocks approval, never blocks saving a draft |
| **Approve** | `risk_level` from the affected registries decides one or two approvers; approver ≠ author, always | `trust.dual_control.self_approval` |
| **Apply** | One transaction: write rows, write `registry_entity_versions`, bump `registry_epochs`, `pg_notify`, purge CDN surrogate key | Any failure → whole change set rolls back; epochs never half-bump |
| **Rollback** | `platform.fn_rollback_change_set(id)`; aborts on `row_version` drift with `registry.rollback_conflict` | The conflict is shown as a three-way diff for a human decision |
| **Schedule** | `scheduled_for` + a timer worker. A GST rate change at 00:00 on 1 Apr is scheduled, not watched | Missed schedule alerts; never silently applies late |

### C5.1 Taxonomy editor

**UX.** A tree on the left (category → make → model → generation → variant), an attribute panel on
the right. Adding "Construction Equipment" is: new category node → pick a plate class → add
attributes (`boom_length`, unit `m`, type `number`, min/max, required-at `listing_publish`) →
optionally inherit from an existing category.

**Validation.** Code uniqueness; no cycles; an attribute cannot change `data_type` once
`entity_attribute_values` exist (it can be deprecated and superseded); deleting an option that is in
use is refused with the usage count and a "deprecate instead" action; unit must exist in
`platform.units`.

**Preview.** Three panels, side by side: (1) the generated sell-form for that category rendered live
by `SchemaForm`; (2) the search facets that will appear, with the OpenSearch field names
(`attr_num_boom_length`) so nobody discovers a mapping problem in production; (3) affected-row counts.

**Publish/rollback.** Standard. Additionally, a taxonomy publish enqueues an OpenSearch reindex for
the affected category; the reindex is a separate, resumable job and the change set records its id —
a rollback of the taxonomy also enqueues the inverse reindex.

**Guard rail.** Renaming a `code` is not offered. Labels are i18n keys and are freely editable; codes
are identity and are immutable. This single decision prevents the most common metadata-system
disaster.

### C5.2 Form builder

**UX.** Left: palette of field types (from `platform.field_types` — text, number, money, date, select,
multiselect, media, address, plate, chassis, file, section, repeater). Middle: canvas with drag-drop
sections and fields. Right: property inspector (label i18n key, help text, placeholder, validation,
conditional visibility as JsonLogic with a picker UI, data binding to an attribute or entity column).
Bottom: a live device preview toggling Web / Android / iOS width.

**Validation.** Every field binds to something real (an `attribute_definitions` row or a whitelisted
entity column); no orphan fields; conditional expressions reference only in-scope fields; required
fields must be reachable given the conditions (a required field behind a condition that can never be
true is caught by a satisfiability check, not by a QA cycle); file fields declare accepted MIME types
and max size; PII-bearing fields must name their `pii_class`.

**Preview.** The real renderer, not a mock — `packages/forms` running the actual schema, with a
"fill with sample data" and a "submit against validation" that calls
`POST /platform/forms/{key}/validate` server-side. What you see is exactly what the Flutter app will
render, because it is the same schema and the same rules.

**Publish/rollback.** New `form_versions` row. In-flight instances keep their pinned version
(data model §3.12). Rollback republishes the prior version as a new version — we never resurrect a
retired version id, because a form version id appears in submitted-data provenance.

**The killer feature.** "Add a field to the sell flow across web + Android + iOS" is: drag, bind,
validate, preview, approve, apply. Elapsed time, realistically: four minutes.

### C5.3 Workflow designer

**UX.** A canvas of states (boxes) and transitions (arrows). Click a state: kind, semantic flags
(`is_publicly_visible`, `is_editable`, `holds_resource`, `holds_funds`, `counts_as_open`,
`blocks_deletion`), SLA minutes, breach transition, entry/exit effects, `ui.tone` and pill label.
Click a transition: from/to, trigger type, required permission, guard rule set, confirm form, effects
list, reason requirement, reversibility.

**Validation.**
- Exactly one `initial` state; at least one terminal.
- **Reachability**: every state reachable from initial; every state can reach a terminal. Both are
  graph checks, both reported with the offending path highlighted.
- No transition to a terminal state from which a non-automatic transition exits (except explicit
  `reopen` transitions marked `is_reversible`).
- Every referenced permission, rule set, form version, notification event and command exists.
- A state with `holds_funds = false` cannot be the target of a transition whose effects include a
  `ledger` action that credits escrow — the designer catches money/state contradictions.
- SLA breach transitions must originate from the state that declares the SLA.

**Preview.** A **simulator**. Pick a real entity (or a synthetic one), then walk transitions in a
sandbox: guards are evaluated for real against that entity's data, effects are listed but not
executed, and the resulting timeline renders in `WorkflowTimeline` exactly as the user would see it.
Plus an impact report: how many live instances sit in states this change set touches, and what
happens to them (nothing — they are pinned to v1 — but the designer must *see* that stated).

**Publish/rollback.** New `workflow_definitions.version`. New instances get v2; existing finish on
v1. Rollback simply retires v2 — instances already on v2 are migrated only if a explicit state map is
supplied, and if any instance sits in a state that does not exist in v1, the rollback is refused
with the instance list. That refusal is correct: silently teleporting live deals is worse than a
manual fix.

**Insert "Legal Hold" before payout** (the north star's example) is: add state `legal_hold`
(`kind=waiting`, `holds_funds=true`, `counts_as_open=true`, tone `flag`), add transition
`place_on_legal_hold` with `from_state_id = NULL` (any state) and permission `deals.deal.legal_hold`,
add `release_legal_hold` back to the prior state via `context.previous_state`. Three rows.

### C5.4 Commission / pricing rule builder

**UX.** A fee component form with a live calculator beside it. Choose calc method → the form
reshapes (slabs get a slab table with an inline overlap check; formula gets an expression editor with
autocomplete over the fee context). Scope pickers for city/state/category/party tier. Payer/payee
role pickers. GL account picker. Tax rule picker with the effective-dated list.

**Validation.** Slab coverage (no gaps, no overlaps — the DB `EXCLUDE` constraint is the backstop,
but the editor catches it first and shows the gap in rupees); min ≤ max; percent within 0–100; a
`percent` component must name a basis that exists in the context; payer ≠ payee; GL account exists
and its `normal_balance` matches the direction; tax rule effective-dated to cover the component's
window; **and a check that the sum of all applicable components on a worst-case deal does not exceed
a configured ceiling** (a runaway stacking of city + category + tier overrides is a real risk).

**Preview — the one that earns the feature.** A **side-by-side re-run of the last 500 real deals**
(or a chosen segment) under current rules vs. proposed rules:

```
                       CURRENT        PROPOSED       Δ
Deals matched            412            412
Platform revenue      ₹18.4L         ₹14.2L      −₹4.2L   (−22.8%)
Avg fee per deal      ₹4,466         ₹3,446      −₹1,020
Max fee                ₹25,000        ₹18,750
Deals with fee = 0        3              3
GST collected          ₹3.31L         ₹2.56L
▸ 12 deals would have crossed the TCS 206C(1F) threshold differently
```

Nobody publishes a commission change without this. It runs in a rolled-back transaction against a
read replica with `SET LOCAL statement_timeout = '30s'`.

**Publish/rollback.** Temporal: the old row gets `effective_to = now()`, the new row
`effective_from`. Never an UPDATE. Rollback = close the new row and reopen the old with a fresh row.
Invoices already issued are untouched, by construction, because they froze their `pricing_snapshot`.

### C5.5 Template editor (notifications + documents)

**UX.** A three-pane editor: variable palette (from the event's payload schema) · body editor
(rich-text for email, plain with variable chips for SMS/WhatsApp, MJML-lite for email layout) ·
live preview per channel and per locale with sample data. Locale tabs across the top; a completeness
bar shows which locales are missing.

**Validation.**
- Every `{{variable}}` exists in the event's declared payload schema — an unresolvable variable is a
  hard error, because in production it renders as an empty string in a message to a customer.
- SMS: character count with GSM-7 vs UCS-2 detection (a Marathi message is UCS-2 → 70 chars per
  segment, and a 4-segment OTP SMS is a cost bug); **DLT template id and header must be registered
  and matching**, validated against the DLT registry before publish. An unregistered template will
  be silently dropped by the operator, which is the worst kind of failure.
- WhatsApp: template must exist and be `APPROVED` at the BSP; button/variable counts must match.
- Email: link domains must be on the allowlist; unsubscribe link mandatory for marketing categories.
- Money variables must render through the money formatter (lakh/crore) — a template that string-
  concatenates paise is rejected.

**Preview.** Rendered per channel × locale with three sample payloads: minimum, typical, and
adversarial (longest possible name, ₹1,23,45,678 amount, RTL-ish edge cases, missing optional fields).

**Publish/rollback.** Versioned per `(event, channel, locale)`. Rollback restores the prior version
and does not re-send anything.

### C5.6 Feature-flag console

**UX.** A table of flags with owner, age, expiry, current variant distribution, and live evaluation
counts (last 24 h) per variant. Clicking a flag opens the rule list: drag to reorder priority, each
rule showing audience (a JsonLogic builder with plain-English rendering: *"users in Bengaluru who
are dealers on app ≥ 2.4.0"*), rollout percent, and effective window.

**Validation.** Variant conforms to `variant_schema`; percentages within 0–100; a non-permanent flag
must have `expires_at`; a kill-switch flag cannot have audience rules (a kill switch is global by
definition); a flag gating a module must respect `modules.depends_on`.

**Preview.** An evaluator: enter a user id (or pick one), see exactly which rule matched and the
resulting variant, with the bucket hash shown. This kills the "why is this user seeing that?"
support loop.

**Publish/rollback.** Kill switches (`is_kill_switch = true`) bypass the change-set flow entirely —
they apply immediately with a single actor, because a kill switch that needs an approval is not a
kill switch. Every other flag change goes through the change set. All flag flips write `audit_log`
regardless.

**Hygiene.** A weekly job lists flags past `expires_at` and flags with 100%/0% distribution for 30+
days, and opens a cleanup ticket assigned to `owner_user_id`. Flag debt is technical debt with a
customer-visible failure mode.

---

# PART D — SUPER ADMIN

## D1. Roles and permission authoring

The RBAC editor writes `platform.roles`, `role_permissions`, `user_role_assignments` (data model §3.6).

**UX.** Permission matrix: rows are permissions grouped by module/resource, columns are the role's
scope, cells are allow/deny/inherit. Inherited permissions from `parent_role_id` render greyed with
their source named. Per-cell advanced settings open `row_filter` (JsonLogic builder),
`field_mask` (column checkboxes with a live "what this role sees" sample row), and
`amount_limit_paise`.

**Validation.**
- A role cannot grant a permission whose `min_scope` is wider than the role's `scope_type_code`.
- Cycle detection on `parent_role_id`, max depth 5.
- `max_assignees` respected at assignment time.
- Granting any `risk_level = 5` permission requires the author to already hold it (no privilege
  escalation by authorship) **and** a second approver.
- `deny` always wins, and the editor **shows the effective result**, not the authored intent —
  the number-one RBAC bug is an admin who granted something that a wider deny silently kills.

**Preview.** "Test as user": pick a real user, see their fully resolved permission set before and
after the change set, diffed, with the resolution path for any permission (which assignment, which
parent role, which deny). Plus a blast-radius count: N users affected.

**The example, done.** "Regional Auction Moderator — West": one role row
(`scope_type_code='state'`), permissions `auction.auction.moderate`, `auction.lot.pause`,
`auction.lot.extend`, `auction.bid.void`, `trust.case.read`; then five `user_role_assignments` rows
(MH, GJ, GA, MP, RJ) per moderator. Zero code, and the assignment rows carry `expires_at` so a
temporary cover during someone's leave expires itself.

## D2. System settings and tax configuration

Settings are `platform.system_settings(key, value jsonb, value_schema, scope, is_secret, requires_restart)`.
Nothing about them is special-cased: the editor renders from `value_schema`, validates against it,
and change-sets them like any registry.

Tax configuration is `platform.tax_rules` (data model §3.5) and gets its own screen because the
consequences are statutory:

| Control | Behaviour |
|---|---|
| GST rate change | New effective-dated row; the old row is closed, never edited. The editor refuses an `effective_from` in the past |
| Place-of-supply rule | `recipient_state` (B2B) / `supplier_state` (B2C) per tax rule; IGST vs CGST+SGST is derived, never chosen |
| TCS 206C(1F) | 1% above ₹10,00,000 per transaction on motor-vehicle sale value; threshold and basis are columns |
| TDS 194-O | 0.1% on participant payouts above ₹5,00,000 per FY per seller; the FY accumulator lives in the warehouse and is read by the payout engine |
| e-invoicing | AATO threshold, IRP provider route, and the 30-day IRN window are settings; invoices generate with `irn IS NULL` when the IRP is down and are retried (data model §3.9) |
| GSTIN of the platform | Per state of operation; place-of-supply resolution needs it |
| Invoice series | Gapless per `(series, FY)`; the editor can open a new series for a new FY but **cannot** edit or delete an issued number |

**Preview for tax changes** re-issues (in a rolled-back transaction) the last 200 invoices under the
proposed rules and reports total tax delta, count of invoices whose IGST/CGST split would flip, and
any that would fail validation. A tax change that would have produced an invalid invoice is not
approvable.

## D3. Feature toggles and kill switches

Same console as C5.6, with Super Admin additions: module enable/disable (`platform.modules`), forced
app-version floors (`min_app_version`), the load-shedding priority list (API §11), and the maintenance
banner. Disabling a module validates `depends_on` in reverse — you cannot disable `payments` while
`deals` is enabled, and the console says so with the dependency chain rather than failing at runtime.

## D4. Audit log search

`platform.audit_log` is append-only, partitioned, and redacted per `pii_registry`. The search UI:

| Filter | Index used |
|---|---|
| Actor (user, or `actor_kind='system'/'impersonation'`) | `ix_audit_log__actor` |
| Entity (type + id) | `ix_audit_log__entity` |
| Time range | BRIN |
| Action code (prefix, e.g. `payout.*`) | partial index on high-risk prefixes |
| Changed field (`'reserve_price_paise' = ANY(changed_fields)`) | GIN on `changed_fields` |
| Correlation id | full request trace across modules |
| Risk level ≥ N | partial index |

Two things this screen must do that generic log viewers do not:

1. **Render a before/after diff** in the product's own vocabulary — field labels from the form/taxonomy
   registries, states as `StatusPill`s, money as `MoneyValue`. A compliance reviewer should not have
   to read JSONB.
2. **Follow a `correlation_id` across modules** into one timeline: the API request, the workflow
   transitions, the outbox messages, the ledger entries, the notifications sent. This is the single
   most valuable incident-response tool in the platform.

Export is CSV/JSONL, capped, watermarked, itself audited (`action_code='audit.export'`), and requires
step-up. Exporting the audit log is exactly as sensitive as reading the data it describes.

## D5. API key management

For dealer ERP/DMS integrations, partner lenders/insurers, and aggregators.

```sql
CREATE TABLE platform.api_clients (
    id            uuid PRIMARY KEY DEFAULT platform.uuid_generate_v7(),
    code          platform.code_key NOT NULL,
    name          text NOT NULL,
    owner_party_id uuid,                            -- dealer / partner org
    client_kind   platform.code_key NOT NULL,       -- 'dealer_dms','partner_lender','aggregator','internal_service'
    auth_mode     platform.code_key NOT NULL,       -- 'api_key','oauth2_cc','mtls'
    scopes        text[] NOT NULL DEFAULT '{}',     -- permission codes, subset of the owner's own
    ip_allowlist  inet[] NOT NULL DEFAULT '{}',
    rate_limit    jsonb NOT NULL DEFAULT '{"rps":10,"burst":20,"daily":50000}',
    webhook_url   text,
    webhook_secret_ref text,
    is_active     boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(), created_by uuid,
    updated_at timestamptz NOT NULL DEFAULT now(), updated_by uuid,
    row_version bigint NOT NULL DEFAULT 1,
    CONSTRAINT uq_api_clients__code UNIQUE (code)
);

CREATE TABLE platform.api_keys (
    id            uuid PRIMARY KEY DEFAULT platform.uuid_generate_v7(),
    api_client_id uuid NOT NULL REFERENCES platform.api_clients(id) ON DELETE CASCADE,
    key_prefix    text NOT NULL,                    -- 'ahk_live_7f3a' — shown in lists, searchable
    key_hash      platform.sha256_hex NOT NULL,     -- argon2id in practice; sha256 column type for shape
    label         text NOT NULL,
    environment   platform.code_key NOT NULL,
    expires_at    timestamptz NOT NULL,             -- NOT NULL: no immortal keys
    last_used_at  timestamptz,
    last_used_ip  inet,
    rotated_from_id uuid REFERENCES platform.api_keys(id),
    revoked_at    timestamptz, revoked_by uuid, revoke_reason text,
    created_at timestamptz NOT NULL DEFAULT now(), created_by uuid,
    CONSTRAINT uq_api_keys__prefix UNIQUE (key_prefix)
);
```

Rules: the secret is shown **once** at creation and never again; `expires_at` is mandatory (max 365
days, default 90); rotation creates an overlapping key with `rotated_from_id` and both work until the
old one expires; scopes cannot exceed the owning party's own permissions (a dealer's API key cannot
read another dealer's inventory, enforced by the same `IScopeGuard`); every call writes to a per-key
usage rollup; a key unused for 60 days triggers a notification to its owner and auto-revokes at 90.

## D6. Four-eyes (dual control)

```sql
CREATE TABLE platform.dual_control_requests (
    id            uuid PRIMARY KEY DEFAULT platform.uuid_generate_v7(),
    action_code   platform.code_key NOT NULL,       -- 'payout.release','trust.party.block','registry.change_set.apply'
    permission_code platform.code_key NOT NULL REFERENCES platform.permissions(code),
    entity_type   platform.code_key NOT NULL,
    entity_id     uuid,
    payload       jsonb NOT NULL,                   -- the exact command to execute on approval
    payload_hash  platform.sha256_hex NOT NULL,     -- approver approves THIS payload, not a later one
    amount_paise  platform.money_paise,
    requested_by  uuid NOT NULL,
    requested_at  timestamptz NOT NULL DEFAULT now(),
    reason        text NOT NULL,
    state_code    platform.code_key NOT NULL DEFAULT 'pending',  -- workflow 'dual_control'
    approved_by   uuid,
    approved_at   timestamptz,
    rejected_by   uuid, rejected_at timestamptz, rejection_reason text,
    executed_at   timestamptz,
    execution_result jsonb,
    expires_at    timestamptz NOT NULL DEFAULT now() + interval '24 hours',
    correlation_id uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    row_version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_dual_control__no_self CHECK (approved_by IS NULL OR approved_by <> requested_by)
);
```

`ck_dual_control__no_self` is the whole point, and it is a database constraint so no code path can
bypass it. Which actions require it is **data**: `platform.permissions.requires_second_approver`,
optionally conditioned on amount by `role_permissions.amount_limit_paise`.

Actions requiring four-eyes at launch:

| Action | Threshold | Approver must hold |
|---|---|---|
| `payments.payout.release` | > ₹5,00,000 | `payments.payout.approve` |
| `payments.refund.approve` | > ₹1,00,000 | `payments.refund.approve` |
| `trust.party.block` / `unblock` | always | `trust.case.resolve_fraud` |
| `trust.blacklist.write` (permanent or party_id) | always | `trust.blacklist.approve` |
| `platform.registry.publish` for pricing/tax/workflow/RBAC | always | `platform.registry.approve` |
| `identity.user.impersonate` | always for staff targets | `identity.impersonate.approve` |
| `auction.lot.force_close` / `auction.bid.void` | always | `auction.auction.moderate` at ≥ city scope |
| `platform.api_key.create` for `partner_*` clients | always | `platform.api_client.approve` |
| DPDP bulk export / bulk erasure | always | `trust.dpdp.approve` |
| Database `db_admin` hard delete | always | Super Admin |

**Break-glass** exists (`platform.user_permission_grants`, always time-boxed, always reasoned), pages
the security channel on creation, and every action taken under a break-glass grant is tagged in the
audit log. A break-glass grant that is never reviewed within 24 h escalates.

## D7. Backup, DR and the runbook

### D7.1 Targets

| Component | RPO | RTO | Mechanism |
|---|---|---|---|
| PostgreSQL 16 | **≤ 60 s** | **≤ 30 min** (same region), ≤ 2 h (cross-region) | Streaming replica (sync-standby for the primary WAL, async cross-region) + continuous WAL archiving to object storage (`pgBackRest`), full backup nightly, PITR to any second in 35 days |
| Redis 7 | ≤ 5 min (best effort) | ≤ 10 min | AOF everysec + RDB hourly. **Redis is never the sole source of truth**: auction bid state rebuilds from `auction.bids`, registry caches rebuild from PostgreSQL, velocity counters rebuild best-effort |
| OpenSearch | N/A (derived) | ≤ 4 h to full, minutes to degraded | Snapshots 6-hourly; full reindex from PostgreSQL is the real recovery path. Search degrades to the PostgreSQL fallback (API §11) meanwhile |
| Object store (R2/S3) | ≤ 15 min | minutes | Versioning + cross-region replication + lifecycle to cold. Object-lock (WORM) on invoices, e-signed agreements and law-enforcement exports |
| Secrets (vault/KMS) | ≤ 24 h | ≤ 1 h | Sealed backup, 3-of-5 key shards held by named officers |
| ML artefacts | ≤ 24 h | ≤ 1 h | Content-addressed in object storage; a lost artefact means fallback mode, not an outage |

### D7.2 Backup verification (the part everyone skips)

- **Nightly**: automated restore of the previous night's full backup into an isolated environment,
  followed by an integrity suite: ledger balance check (`Σ debits = Σ credits` per transaction and
  globally), invoice-series gap check, workflow-instance/entity `state_code` agreement, orphan scan
  (`platform.fn_orphan_scan`), and registry-epoch consistency. A restore that passes SQL but fails the
  ledger check is a failed restore.
- **Monthly**: a timed PITR drill to a random timestamp; the wall-clock is recorded and trended. If
  the trend crosses the RTO target, that is a capacity ticket.
- **Quarterly**: full region-failover game day, including DNS cutover, replica promotion, Redis cold
  start, OpenSearch reindex, and a **payment reconciliation** against the gateway's own records for
  the failover window. The output is a written report with the actual RPO/RTO achieved, filed against
  the targets above.

### D7.3 Failover runbook (abridged, but executable)

```
0. DECLARE          on-call declares; incident channel opened; status page → "investigating"
1. FREEZE MONEY     feature_flags kill switch 'payments.global_pause' = true
                    → new payment intents rejected with 503; in-flight webhooks still accepted
                       and queued (they are idempotent; losing one is worse than delaying one)
2. FREEZE AUCTIONS  'auction.global_pause' = true → live lots extend automatically rather than
                       closing during an outage. A lot must never close while bidders cannot bid.
3. ASSESS           primary reachable? replica lag? WAL archive current?
4. PROMOTE          pg_ctl promote on the standby; update the connection secret; restart API pods
5. REDIS            start cold; warm registry caches via GET /platform/* sweep; rebuild auction
                       state from auction.bids; accept that velocity counters restart at zero
6. SEARCH           serve degraded (PostgreSQL fallback) and start the reindex
7. VERIFY           run the integrity suite (D7.2) against the promoted primary BEFORE unfreezing
8. UNFREEZE         auctions first (with extended end times and a notification), then payments
9. RECONCILE        gateway settlement report vs. payments.ledger_entries for the window;
                       any mismatch opens a P1 finance case, not a silent adjustment
10. POSTMORTEM      within 5 business days; actual RPO/RTO recorded; the runbook is edited
```

**Freeze order matters and is not negotiable.** Money first, auctions second, everything else after.
The reverse order has produced closed-at-the-wrong-price lots at other marketplaces.

### D7.4 Data residency and legal

All primary and replica storage stays in Indian regions (Mumbai + Hyderabad). Backups do not leave
India. DPDP cross-border rules currently permit transfer except to notified restricted countries, but
sectoral expectations (RBI payment-data localisation for anything touching payment credentials) are
stricter, and we design to the strictest applicable rule rather than the loosest. Sub-processors are
listed in `platform.integration_providers` with `contract_ref`, which doubles as the DPDP
data-processor register.

---

# PART E — ANALYTICS

## E1. Event taxonomy

One event envelope, everywhere. Emitted from web (Next.js), Flutter, and server-side; server-side is
the source of truth for anything money- or state-related, because client events are lossy and
spoofable.

```jsonc
{
  "event_id": "0193c2f1-…",          // uuid v7, client-generated, dedupe key
  "event_name": "listing.published",  // {object}.{past_tense_verb}  — ALWAYS past tense
  "event_version": 1,
  "occurred_at": "2026-07-29T14:03:11.482+05:30",
  "received_at": "2026-07-29T14:03:11.902+05:30",
  "session_id": "…", "user_id": "…", "party_id": "…", "anonymous_id": "…",
  "device_id": "…", "platform": "android", "app_version": "3.1.0",
  "city_id": "…", "locale": "mr-IN",
  "source": "server",                 // 'web' | 'android' | 'ios' | 'server' | 'batch'
  "correlation_id": "…",              // ties to audit_log and outbox
  "experiment_variants": { "search_ranker": "v3" },
  "properties": { … }                 // event-specific, schema-validated
}
```

**Naming law:** `{object}.{past_tense_verb}`. `listing.published`, not `publish_listing` or
`Listing Published`. Event names are `platform.codes(set_code='analytics_event')` rows with a JSON
Schema for `properties`; an event whose payload fails its schema lands in a dead-letter table and
alerts — it is never silently dropped, because a silently dropped event becomes a wrong number on a
CEO dashboard six weeks later.

### E1.1 The catalogue (abridged to the load-bearing set)

| Domain | Events |
|---|---|
| Acquisition | `session.started`, `app.installed`, `campaign.clicked`, `referral.link_opened` |
| Discovery | `search.performed`, `search.zero_results`, `facet.applied`, `listing.impressed`, `listing.viewed`, `listing.shortlisted`, `listing.shared`, `comparison.performed` |
| Supply | `sell.flow_started`, `sell.step_completed`, `sell.flow_abandoned`, `valuation.requested`, `valuation.returned`, `listing.created`, `listing.submitted`, `listing.published`, `listing.rejected`, `listing.price_changed`, `listing.paused`, `listing.expired`, `listing.sold` |
| Lead | `lead.created`, `lead.assigned`, `lead.first_responded`, `chat.thread_started`, `chat.message_sent`, `call.initiated`, `testdrive.booked`, `testdrive.completed`, `testdrive.no_show` |
| Inspection | `inspection.booked`, `inspection.started`, `inspection.completed`, `inspection.report_viewed` |
| Auction | `auction.registered`, `lot.viewed`, `bid.placed`, `bid.outbid`, `lot.extended`, `lot.won`, `lot.unsold`, `deposit.paid`, `deposit.forfeited` |
| Deal | `deal.created`, `deal.terms_agreed`, `escrow.funded`, `documents.signed`, `rc.transfer_initiated`, `rc.transfer_completed`, `deal.completed`, `deal.cancelled`, `dispute.raised`, `dispute.resolved` |
| Money | `payment.initiated`, `payment.succeeded`, `payment.failed`, `payout.initiated`, `payout.settled`, `refund.requested`, `refund.completed`, `invoice.issued`, `credit_note.issued` |
| Utility | `rental.booked`, `rental.started`, `rental.returned`, `lease.activated`, `attachment.matched`, `attachment.activated`, `attachment.renewed`, `attachment.terminated` |
| Referral money | `loan.applied`, `loan.sanctioned`, `loan.disbursed`, `insurance.quoted`, `insurance.purchased`, `insurance.renewed` |
| Trust | `trust.signal_raised`, `trust.case_opened`, `trust.case_resolved`, `trust.restriction_applied`, `report.submitted` |
| Platform | `feature.evaluated`, `registry.published`, `integration.degraded`, `error.surfaced` |

`listing.impressed` is high-volume and is **batched client-side** (one event per viewport batch with
an array of listing ids and positions) — emitting one event per card is how an analytics bill becomes
a board topic.

## E2. Warehouse

Ingest: outbox + client events → Kafka-compatible stream (RabbitMQ streams at our scale) → raw
landing in object storage (Parquet, partitioned by date/event_name) → dbt-style transforms → star
schema in a PostgreSQL analytics database (separate instance, restored from the replica). We do not
introduce a separate warehouse engine on day one; PostgreSQL with columnar-friendly partitioning
handles the first two years, and the ADR to move to ClickHouse/BigQuery is written but not executed.

### E2.1 Dimensions

| Table | Grain | Notable columns | SCD |
|---|---|---|---|
| `dim_date` | day | `date_key, iso_week, fiscal_year_in (Apr–Mar), is_holiday, festival_code` | — |
| `dim_time_of_day` | 15 min | `slot_key, ist_hour, daypart` | — |
| `dim_city` | city | `city_id, name, state_id, gst_state_code, tier, is_serviceable, launched_on` | Type 2 |
| `dim_geography_rto` | RTO office | `rto_code, state, district` | Type 1 |
| `dim_party` | party | `party_id, party_kind, kyc_level_band, city_key, signup_date_key, acquisition_channel, dealer_plan_tier, risk_band` | **Type 2** (risk band and plan tier change and history matters) |
| `dim_vehicle` | vehicle | `vehicle_id, category, make, model, variant, fuel, transmission, body, mfg_year, plate_class` | Type 2 |
| `dim_listing` | listing | `listing_id, intent_type, seller_party_key, vehicle_key, first_published_date_key, price_band` | Type 2 |
| `dim_dealer` | org | `org_id, name, city_key, plan_tier, branch_count, verified_on` | Type 2 |
| `dim_channel` | channel | `channel_code, medium, source, campaign, is_paid` | Type 1 |
| `dim_intent` | intent | `intent_type_code, settlement_mode` | Type 1 |
| `dim_workflow_state` | state | `entity_type, state_code, kind_code, counts_as_open` | Type 1, **sourced from the workflow registry** |
| `dim_fee_component` | fee | `fee_code, module, basis, payer_role` | Type 2 |

`dim_workflow_state` being registry-sourced is the point: when Ops adds a `legal_hold` state, the
warehouse gets it on the next sync and every state-based metric keeps working. No dashboard hardcodes
a state string either.

### E2.2 Facts

| Table | Grain | Type | Key measures |
|---|---|---|---|
| `fact_listing_lifecycle` | one row per listing | accumulating snapshot | `created_at, published_at, first_lead_at, first_offer_at, sold_at, expired_at, days_to_first_lead, days_to_sell, price_first_paise, price_final_paise, price_change_count, impressions, views, leads` |
| `fact_lead` | one row per lead | transaction | `created_at, first_response_at, response_minutes, converted_deal_key, source_channel_key, dealer_key` |
| `fact_deal` | one row per deal | accumulating snapshot | `gross_value_paise, platform_fee_paise, tax_paise, escrow_funded_at, completed_at, cancelled_at, cancel_reason_key, cycle_days, intent_key, buyer_key, seller_key, city_key` |
| `fact_ledger_entry` | one row per ledger entry | transaction (append) | `amount_paise, dr_cr, account_key, deal_key, posted_at` — the revenue truth |
| `fact_bid` | one row per bid | transaction | `amount_paise, lot_key, bidder_key, placed_at, is_winning, is_proxy, seconds_before_close` |
| `fact_auction_lot` | one row per lot | accumulating | `reserve_paise, opening_paise, hammer_paise, bid_count, bidder_count, extension_count, cleared, settled_at` |
| `fact_session` | one row per session | transaction | `duration_seconds, page_views, searches, listings_viewed, converted_lead, device_key, channel_key` |
| `fact_attachment_period` | one row per attachment × month | periodic snapshot | `active_days, revenue_paise, renewed, terminated_reason_key` |
| `fact_rental_booking` | one row per booking | transaction | `days, rent_paise, damage_claim_paise, utilisation_contrib` |
| `fact_marketing_spend` | channel × day × city | periodic snapshot | `spend_paise, impressions, clicks, installs` |
| `fact_inspection` | one row per inspection | transaction | `grade, defect_count, duration_minutes, ai_assisted, correction_count` |
| `fact_trust_case` | one row per case | accumulating | `opened_at, first_touch_at, resolved_at, resolution_key, amount_at_risk_paise, band_at_open` |

**Bridge tables** where fan-out would otherwise corrupt sums: `bridge_deal_party` (a deal has buyer,
seller, dealer, financier, insurer — never denormalise five party keys onto `fact_deal` and then
`GROUP BY` one of them), and `bridge_listing_attribute` for EAV analysis.

## E3. Metric definitions — as data

```sql
CREATE TABLE analytics.metric_definitions (
    code           platform.code_key PRIMARY KEY,   -- 'gmv','take_rate','sell_through'
    i18n_key       text NOT NULL,
    description    text NOT NULL,
    unit_code      platform.code_key NOT NULL,      -- 'paise','percent','days','count','ratio'
    grain          platform.code_key NOT NULL,      -- 'day','week','month'
    sql_expression text NOT NULL,                   -- the measure, over a declared fact
    fact_table     text NOT NULL,
    filters        jsonb NOT NULL DEFAULT '{}',
    dimensions_allowed text[] NOT NULL DEFAULT '{}',
    is_money       boolean NOT NULL DEFAULT false,
    higher_is_better boolean NOT NULL DEFAULT true,
    target         numeric,
    owner_user_id  uuid,
    version        integer NOT NULL DEFAULT 1,
    change_set_id  uuid REFERENCES platform.registry_change_sets(id),
    created_at timestamptz NOT NULL DEFAULT now(), created_by uuid,
    updated_at timestamptz NOT NULL DEFAULT now(), updated_by uuid,
    row_version bigint NOT NULL DEFAULT 1
);
```

A metric change goes through the same change-set flow as any registry, and its **preview re-computes
the last 13 weeks under both definitions and shows the delta**. That is how you change "GMV" without
three departments reporting different numbers for the same month.

### E3.1 The definitions that matter

| Metric | Definition (precise) | Trap it avoids |
|---|---|---|
| **GMV** | `Σ fact_deal.gross_value_paise WHERE state ∈ (states where kind_code='terminal_success')` at `completed_at` | Counting listed price, counting cancelled deals, counting an exchange twice (an EXCHANGE deal has two assets — GMV counts the *transaction value*, once, per `bridge_deal_party` dedupe) |
| **Take rate** | `Σ platform revenue (fact_ledger_entry on revenue accounts, ex-GST) ÷ GMV` | Including GST as revenue; including pass-through amounts (RTO fees we collect and remit) |
| **Net revenue** | Take-rate numerator minus refunds, credit notes, and coupon discounts, ex-GST | Reporting gross fees while discounts sit in a different table |
| **Sell-through rate** | `listings sold within N days of publish ÷ listings published in the cohort`, cohort by publish week, N from the registry (default 60) | Dividing by *live* listings, which flatters the number as inventory grows |
| **Days to sell** | `median(sold_at − first_published_at)` in **business days**, on sold listings only, reported with the censoring rate | Reporting a mean (skewed by one 400-day truck) and hiding that 40% never sold |
| **Auction clearing rate** | `lots where hammer ≥ reserve AND settled ÷ lots opened`, and separately `lots hammer≥reserve ÷ lots opened` (clearing vs. settlement) | Conflating "someone bid enough" with "money actually moved". Both numbers are needed; the gap is the settlement-failure rate |
| **Attachment renewal rate** | `attachments renewed within 30 days of term end ÷ attachments reaching term end`, cohort by activation month | Counting month-over-month active contracts, which hides churn behind new sales |
| **Lead-to-deal conversion** | `deals with a linked lead ÷ leads created`, attributed to the lead's *first* touch, measured on a 45-day window from lead creation | Last-touch attribution (which credits the dealer's final call for a lead search created it) and open-window measurement (which makes recent weeks look bad forever) |
| **CAC by channel** | `fact_marketing_spend ÷ first-time transacting users attributed to that channel`, 30-day post-click / 1-day post-view | Counting signups instead of transactors; ignoring organic cannibalisation (we report paid CAC and blended CAC side by side, always) |
| **LTV by channel** | `Σ net revenue per party over 24 months, discounted at the registry rate, cohorted by acquisition month` | Extrapolating LTV from 6 weeks of data; using GMV instead of net revenue |
| **Contribution margin per deal** | `net revenue − payment gateway cost − inspection cost − logistics − support cost allocation` | Celebrating a take rate that loses money after a ₹999 inspection and a ₹1,200 tow |
| **Fraud loss rate** | `confirmed fraud rupees ÷ GMV`, plus `false-positive lost GMV ÷ GMV` reported beside it | Optimising fraud loss to zero by blocking half the market |
| **Inventory freshness** | `% live listings whose seller confirmed availability in the last 14 days` | Search results full of sold cars, which is the #1 killer of buyer trust |

Every money metric renders through `MoneyValue` — `₹18.4Cr`, not `₹184,000,000.00`. A dashboard that
shows Indian money in Western grouping is a dashboard nobody in the Pune office trusts.

## E4. Dashboards per persona

| Persona | Dashboard | Top widgets | Refresh |
|---|---|---|---|
| **Founder / CEO** | Business pulse | GMV + take rate + net revenue (MTD vs. plan), live deal count, city league table, funnel from search→deal, cash in escrow, fraud loss rate, 13-week trend of contribution margin | hourly |
| **City Ops Manager** | City cockpit | Supply added vs. sold, days-to-sell by category, inspection SLA, lead response times by dealer, zero-result searches in this city, open trust cases, logistics backlog | 15 min |
| **Category Manager** | Category health | Price-band supply/demand heatmap, valuation MAPE by variant, top variants by demand-supply ratio, forecast vs. actual, listings rejected by reason | daily |
| **Auction Manager** | Auction floor | Live lots with bid velocity, clearing rate today/week, reserve-miss distribution, settlement failure rate, deposit forfeitures, bidder concentration (are 3 dealers winning everything?) | live |
| **Trust & Safety Lead** | Trust console | Queue depths vs. SLA, band distribution over time, precision/recall trend, top firing signals, model version in production + drift, appeals volume and overturn rate | 5 min |
| **Finance / Controller** | Money | Ledger trial balance, escrow balance vs. bank statement (reconciliation break count), GST liability by state, TCS/TDS accumulators, invoice/IRN pending, payout aging, refund liability | hourly + daily close |
| **Growth / Marketing** | Acquisition | CAC and blended CAC by channel, LTV:CAC by cohort, campaign ROAS, install→first-search→first-lead funnel, referral program economics, organic share | daily |
| **Dealer** (in dealer portal, not admin) | My business | Inventory aging buckets, views/leads per listing, lead response time vs. city median, win rate at auction, subscription usage vs. cap, "what to stock next" from `demand-forecast` | hourly |
| **Engineering / SRE** | Platform | p95 latency by endpoint, error rate by code, outbox lag and dead-letters, integration breaker states, registry epoch staleness across clients, AI model latency and ₹/day | live |

**Dashboard definitions are also rows** (`analytics.dashboards`, `dashboard_widgets` referencing
`metric_definitions`), so a persona dashboard is composed, permission-filtered by RBAC, and shipped
without a frontend deploy.

## E5. Pipeline SLA, quality and failure modes

| Concern | Rule |
|---|---|
| Freshness | Streaming facts (`fact_bid`, `fact_lead`, sessions) ≤ 5 min. Deal/ledger facts ≤ 15 min. Batch dims and cohort metrics by 06:00 IST. Every dashboard displays its `as_of` — always |
| Late data | Events accepted up to 7 days late; the affected day's aggregates are recomputed and the dashboard marks the day `revised`. Silent restatement is forbidden |
| Deduplication | On `event_id` (uuid v7) with a 7-day Bloom filter + exact check in the landing zone |
| Ordering | Never assume it. Facts are built from state-change events with explicit `occurred_at`; accumulating snapshots take `LEAST`/`GREATEST` semantics per milestone column |
| Schema evolution | Additive only within an `event_version`; a breaking change increments the version and both versions are transformed until the old clients age out (`min_app_version` tells us when) |
| Reconciliation | Nightly: `fact_deal` GMV vs. `deals.deals`, `fact_ledger_entry` vs. `payments.ledger_entries` (must match to the paise), event counts vs. outbox counts. Any break opens a P2 |
| PII | The warehouse holds `party_key`, never phone/email/PAN. Erasure propagates by deleting the `dim_party` natural-key mapping and the event-level rows, keeping aggregates (data model §9.2) |
| Cost | Impressions are the volume driver; they are pre-aggregated at ingest to `(listing, day, position_bucket)` and the raw rows expire in 30 days |

---

## F. Cross-module assumptions this document makes

Other module specs must honour these, or say why not.

1. **`platform.entity_types` gains rows** `trust_case`, `ml_model_version`, `dual_control_request`,
   `api_client`, `cms_block`, `promotion` — with `supports_workflow = true` for the first three and
   `public_ref_prefix` `CASE`, `MDL`, `DCR`.
2. **`platform.codes` gains code sets** `trust_signal_type`, `trust_case_type`, `trust_case_resolution`,
   `trust_action`, `trust_restriction`, `trust_restriction_reason`, `trust_list_reason`, `trust_queue`,
   `analytics_event`, `ml_task_type`. All `is_extensible = true` except `ml_task_type`.
3. **Trust never writes to another module's schema.** It emits `trust.*` events; Listings, Auction,
   Payments, Identity and Search react. A restriction is a row other modules *read* through
   `trust.v_active_restrictions` (a new contract view); it is never a column on `identity.users`.
4. **Every gate that consults Trust must tolerate `trust.score.unavailable`** and must define its
   degraded behaviour. Bids and payments **never** fail closed on a model outage; listing publication
   and payouts **do** fail closed (they are not latency-critical and the money risk is asymmetric).
5. **`platform.permissions` is the only place `requires_second_approver` lives.** No module may
   implement its own maker-checker. If an action needs four eyes, it declares it on the permission.
6. **Money never moves on a Trust decision.** Trust can set `holds_funds` via a workflow transition
   request; only Deals/Payments post ledger rows (domain doc §20.3).
7. **AI outputs are advisory records, not domain facts.** `analytics.ml_predictions` is the store; a
   module that wants to act on a prediction copies the value into its own domain record with
   `model_key` + `model_version` frozen (as Valuation already does).
8. **All admin write endpoints require `Idempotency-Key`, `If-Match`, and a reason where the
   permission says so.** Admin is not exempt from the API contract.
9. **The registry-editing API is `/api/v1/platform/registry/change-sets/*`** and every registry write
   endpoint requires an open `change_set_id`. Kill switches are the single documented exception.
10. **`analytics.metric_definitions` is the only definition of a metric.** No dashboard, no export,
    and no board deck may compute GMV or take rate independently. A number without a metric code is
    not a number we ship.
11. **`dim_workflow_state` and `dim_fee_component` are generated from the registries.** Analytics must
    never enumerate states or fees; a new state appears in the warehouse automatically.
12. **Event names are past tense and registered.** An unregistered event is dead-lettered, not stored.
13. **Audit and case evidence are append-only** (`platform.tg_append_only`). Corrections are new rows.
14. **The `blocked` risk band revokes sessions but never confiscates funds.** Any module reading
    `trust.restrictions` must distinguish "cannot act" from "has no claim".
15. **External assurance flags are tri-state.** `insurance_total_loss_flag`, `challan_status`,
    `vahan_rc_status_adverse` and every future provider-sourced flag are `true` / `false` /
    `unknown`. No module may coerce `unknown` to `false` for display, and no badge may be awarded on
    an `unknown`. Catalog, Listings, Inspection and Insurance all render the third state.
16. **`trust.consents.purpose_code` binds data to a purpose across modules.** Data pulled for
    `fraud_prevention` (IIB claim history, bureau, device graph) may not be reused for
    `insurance_quote` or `credit_underwriting`, and vice versa. Any module adding a provider call
    declares its purpose on the `provider_routes` row; the feature store enforces it (A9.13).
17. **Insurance solicitation is a licensed activity.** No module — chatbot, notification campaign,
    lead-routing widget or dealer CRM — may recommend, quote, compare or push an insurance product
    outside the Insurance module's licensed intermediary path. Trust supplies the
    `assistant_prohibited_topics` rule set; every conversational surface must consult it.
18. **`require_inspection` carries a `waivable` param.** Plan tiers and dealer privileges may waive
    an inspection requirement only where `params.waivable = true`. Salvage-history and
    identity-conflict inspections set it `false`, and no pricing or plan rule may override it.
19. **Identity owns `identity.party_credentials`** — professional licences held by a party (POSP /
    insurance agent code, DSA code, trade certificate, inspector certification) with issuer, number,
    `valid_from`, `valid_to` and a verification state. Trust, Insurance, Finance and lead-routing all
    read it; expiry gates capability automatically. Without it, "is this person still licensed?"
    becomes a spreadsheet.
20. **Analytics adds `fact_insurance_policy` and `fact_claim_check`** so free-look rate, attach rate
    and IIB hit rate are metric rows like everything else, not ad-hoc queries in a Trust dashboard.
