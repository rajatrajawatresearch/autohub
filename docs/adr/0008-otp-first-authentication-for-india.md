# ADR-0008 — OTP-first authentication, phone as the primary identity

**Status:** Accepted — 2026-07-29
**Deciders:** Platform architecture + Trust & Safety
**Related:** ADR-0003 (crypto is not configurable), ADR-0005 (outbox), ADR-0011 (mobile)

---

## Context

North Star §3 locks the shape: *"JWT access + rotating refresh, OTP-first, OAuth. India is
phone-first: OTP is the primary factor, password optional."* This ADR records the reasoning, the
India-specific machinery, and the failure modes.

Who actually logs in:

| Persona | Reality |
|---|---|
| C2C seller | One-time seller. Will abandon at "create a password". Has a phone number and WhatsApp. |
| Buyer | Browses anonymously (the homepage plate input needs **no phone number** — Design System §5), authenticates only to contact a seller or book a test drive |
| Dealer staff | 3–8 people sharing a dealership. Will absolutely share one login unless given a reason not to. Bidding with real money. |
| Inspector / partner | Field device, patchy 4G, sometimes a shared company phone |
| Admin | Internal, high privilege, moves money |

The failure being avoided, first version:

> **Email+password onboarding.** A used-car seller in Nashik is asked for an email address they check
> twice a month, then a password with a symbol, then a verification email that lands in Promotions.
> Funnel completion on the sell flow: **~40%**. The competitor asks for a phone number and an OTP and
> gets 85%.

Second version, the one that bites *after* you choose OTP:

> **Single-provider SMS.** All OTPs go through one aggregator. On a Sunday the DLT template gets
> flagged by the operator for a content mismatch and **100% of logins fail for six hours**. Support
> cannot log in either, because support uses the same flow. There is no fallback channel and no
> break-glass path.

Third:

> **SIM swap.** An attacker ports a dealer's number, receives the OTP, logs in, and changes the
> payout bank account. The OTP was never the weak link — the *phone number as sole recovery factor*
> was.

## Decision

**Phone number (E.164, `+91` default) is the primary identity. OTP is the primary factor. Everything
else is a step-up.**

### 1. The flow

```
POST /api/v1/auth/otp/start   { phone, purpose, device_id, captcha? }
     → { challenge_id, hint, expires_in, channel_used, resend_after }
POST /api/v1/auth/otp/verify  { challenge_id, code, device_id }
     → { access_token, refresh_token, party, kyc_level, amr:["otp"] }
POST /api/v1/auth/otp/resend  { challenge_id, channel? }
```

- `code_hash` is HMAC-SHA256 with a per-row salt in `identity.otp_challenges`. **The raw code is
  never stored and never returned.**
- `hint` is a 4-character code echoed in the message body ("AutoHub: code for **7K2M**"). It defeats
  the vishing script "read me the code you just got" because the victim can see which request it
  belongs to.
- Rows are deleted **24 h after `expires_at`** — DPDP Act 2023 data minimisation, and it keeps the
  table small.

### 2. Channel routing is registry data, not code

Notification Registry (#7) row `identity.otp.login` declares
`channel_priority: ["whatsapp", "sms", "voice"]`. Integration Registry (#8) declares the provider
fallback chain *within* each channel (e.g. SMS: Kaleyra → Gupshup → MSG91) with health-based
routing from `platform.provider_health`. The response's `channel_used` reflects what actually
delivered. `resend` may force `channel: "voice"` — which matters for a user in a basement showroom
with no data.

India specifics that are **registry rows, not constants**: TRAI DLT entity ID, registered
template IDs per template × language, sender IDs (`AUTOHB`), and the WhatsApp approved *utility*
template name plus its 24-hour session-window rule.

### 3. Throttle ladder (Feature Registry `otp.throttle.v1`)

| Scope | Limit | Error |
|---|---|---|
| per phone | 1 send / 30 s | `identity.otp.throttled` + `retry_after_seconds` |
| per phone | 5 sends / 1 h | `identity.otp.throttled` |
| per device | 20 sends / 1 h | `identity.otp.throttled` |
| per IP (/24 v4, /48 v6) | 60 sends / 1 h | CAPTCHA required on next `otp/start` |
| verify attempts | 5 per challenge | `identity.otp.max_attempts_exceeded`, challenge burned |

Counters live in Redis with the phone hashed, not raw. The per-IP rule is the one that stops SMS
pumping fraud — an attacker cycling numbers to farm operator revenue share. Ladder thresholds are
registry data because the right numbers are discovered in production, not in design.

### 4. Tokens

- Access JWT: **10 minutes**, `amr` claim (`["otp"]`, `["otp","totp"]`, `["otp","password"]`),
  `scope` (party/org/branch), `kyc_level`, `device_id`, `registry_version`.
- Refresh: **60 days**, rotating, single-use, stored hashed, **with reuse detection** — presenting a
  consumed refresh token revokes the entire family and forces re-authentication. This is the
  mechanism that turns a stolen refresh token from a persistent compromise into a detected one.
- Signing: asymmetric (ES256), keys in KMS, JWKS at `/.well-known/jwks.json`, rotation quarterly.
  **Algorithm choice is code, not registry** (ADR-0003 escape hatch); rotation is operations.

### 5. Step-up, because OTP alone is not enough for money

Authentication level is **per action**, resolved through RBAC + Feature registries:

| Action | Required |
|---|---|
| Browse, save search, chat | `amr ⊇ [otp]` |
| Publish a listing | `otp` + `kyc_level ≥ 1` |
| Place an auction bid | `otp` + `kyc_level ≥ 2` + EMD funded |
| **Change payout bank account** | `otp` **on the registered device** + a **24 h cooling period** + notification to the old channel + `kyc_level ≥ 2`. This is the SIM-swap countermeasure. |
| Initiate a payout / approve a change set | Admin: `otp` + **TOTP** (mandatory, no SMS second factor for internal users) |

Dealer organisations get **per-staff logins with scoped permissions** (RBAC Registry #5) and a visible
"who did what" audit — the only durable answer to shared credentials is making the individual login
more useful than the shared one.

### 6. KYC is a different rail

Aadhaar/DigiLocker/PAN verification raises `kyc_level` — it is **never a login mechanism**. UIDAI
authentication for login is neither permitted for a private marketplace nor desirable. DigiLocker
issues a consent artefact used to fetch RC/DL/PAN; failures degrade to manual upload + OCR + human
review with `kyc_level` capped at 1 (Domain Model §18.1).

### 7. When every channel is down

`otp.all_channels_down` is a declared incident. Behaviour:
- Existing valid refresh tokens keep working — **an outage must not log everyone out**. This is why
  refresh lifetime is 60 days rather than 7.
- Internal users have a break-glass path: TOTP-only login for accounts holding
  `identity.breakglass` permission, rate-limited, alerting on every use, valid for 60 minutes.
- Public users see the problem stated plainly, in the interface's voice
  (Design System §7): *"We can't send codes right now. Try again in a few minutes."*

## Consequences

**Positive**

- Sell-flow and buyer conversion match market expectation. No password, no email, no verification link.
- One identifier (phone) links the marketplace account, the WhatsApp notification channel, the UPI
  handle used at checkout, and the RC holder's contact — which makes identity resolution and fraud
  signals materially better.
- No password database. No credential stuffing, no bcrypt cost tuning, no "we were breached and
  hashes leaked" incident class.
- Channel and provider failover is registry data, so a DLT template rejection is a config change at
  2am, not a deploy.
- `amr` + `kyc_level` in the token means step-up is enforceable at the edge, uniformly, across web
  and both Flutter apps.

**Negative — stated honestly**

- **Per-login cost.** SMS is ~₹0.12–0.25 including DLT overheads; voice is ~10×. At 200k logins/month
  that is a real line item, and it scales with abuse. WhatsApp-first is partly a cost decision.
- **Delivery is outside our control.** Operator filtering, DND registries, template-content
  mismatches, and 4G congestion all produce "I didn't get the code". Mitigation: multi-provider,
  multi-channel, per-provider delivery-rate dashboards, and automatic health-based deranking.
- **Phone-number recycling is real.** TRAI allows reallocation after ~90 days of disconnection. A
  recycled number could inherit an account. Mitigation: dormancy detection (no login > 120 days) →
  forced re-verification with a device-change signal, and payout details always require the 24 h
  cooling period.
- **SIM swap is the residual risk** and OTP cannot fix it. Mitigated by step-up, device binding,
  cooling periods and old-channel notification — not eliminated.
- **Vishing.** Users can be talked into reading a code aloud. The `hint` code and explicit message
  copy ("AutoHub will never ask for this code") reduce it; nothing removes it.
- **No account recovery without the phone.** A user who loses their number needs a human,
  document-based recovery path with KYC re-verification. That queue must exist from day one or
  support improvises something insecure.
- **International sellers/NRI buyers** need non-`+91` support, where SMS deliverability and cost are
  worse. Feature-flagged, WhatsApp-first, email fallback permitted for those numbers only.

## Alternatives considered

| Alternative | Why rejected |
|---|---|
| **Email + password** | The funnel failure in Context. Also wrong for the market: many users' primary email is their child's, or does not exist. Passwords remain available as an *optional additional* factor for dealer/admin accounts. |
| **OAuth-only (Google / Apple)** | Fine as a convenience, useless as the primary: it does not yield a verified phone number, which we need anyway for notifications, chat masking, and KYC name matching. Offered as an accelerator that still requires phone verification. |
| **Passkeys / WebAuthn as primary** | Technically the right answer and where the world is going. Rejected **for now**: Android passkey support in the tier-2/tier-3 device fleet (Android 9–11, non-GMS variants, shared devices) is inconsistent, and a login that fails on a Redmi from 2019 costs more than the security gain. Named revisit trigger below. |
| **Truecaller / silent network authentication (SNA)** | Genuinely lower friction and worth adding as an *accelerator*. Not the primary: SNA requires mobile data (fails on WiFi-only), coverage is operator-dependent, and it makes an SDK vendor a hard dependency on the login path. |
| **Aadhaar-based login (UIDAI OTP)** | Not permissible as a general authentication rail for a private marketplace, and inappropriate: Aadhaar is a KYC instrument, not a session mechanism. Using it for login would also drag every anonymous browser into the DPDP consent regime unnecessarily. |
| **Magic links over email/WhatsApp** | Link-clicking in WhatsApp on Android opens an in-app browser without the session cookie — a well-known, miserable failure mode. OTP codes survive app-switching; links often do not. |
| **Long-lived sessions with no refresh rotation** | Simpler, and turns any token theft into permanent access with no detection. Rotation with reuse detection is cheap and is the only reason we can safely offer 60-day refresh. |

## Revisit when

1. **Passkey support exceeds ~70% of our actual Android install base** (measured from our own device
   telemetry, not market reports) — then offer passkeys as the primary factor for returning users
   with OTP as fallback, starting with the dealer app where devices are newer.
2. SMS+voice cost exceeds **₹4 lakh/month**, or fraudulent send volume exceeds 5% — tighten the
   ladder, make WhatsApp mandatory-first, and consider SNA.
3. Confirmed SIM-swap-linked account takeovers exceed **2 per quarter** — make device-bound
   cryptographic keys (Android Keystore / iOS Secure Enclave) mandatory for any payout-affecting
   action, not just a step-up.
4. A regulator (RBI for PA-adjacent flows, or CERT-In directions) mandates a specific MFA posture for
   money movement — the `amr` claim and the per-action step-up table are where that lands, which is
   why they are separate from the login mechanism.
5. Dealer organisations routinely exceed **8 staff logins** — introduce SSO (SAML/OIDC) for
   enterprise fleet operators, keeping OTP for everyone else.
