# Registry API Contract (authoritative)

> Frontend and backend are built in parallel against **this file**. It is the seam.
> If an implementation disagrees with this document, the implementation is wrong.
> Base path: `/api/v1`. All responses `application/json`. All times ISO-8601 UTC.

---

## 0. Envelope

Success is the payload itself. Failure is RFC 9457 Problem Details plus a stable `code`:

```jsonc
// 4xx / 5xx
{
  "type":   "https://autohub.in/errors/listing.not_found",
  "title":  "Listing not found",
  "status": 404,
  "code":   "listing.not_found",          // stable machine code — clients branch on this
  "detail": "No listing with that id.",
  "instance": "/api/v1/listings/019fab0c-...",
  "traceId": "00-4bf92f...-01",
  "metadata": { "listingId": "019fab0c-..." }
}
```

Lists are cursor-paginated:

```jsonc
{
  "items": [ /* ... */ ],
  "nextCursor": "eyJpZCI6IjAxOWZ...",   // null when exhausted
  "totalEstimate": 1284                  // estimate — never a COUNT(*) on a hot table
}
```

Every registry response carries a version so clients cache hard and invalidate exactly:

```
ETag: "taxonomy:v41"
Cache-Control: public, max-age=60, stale-while-revalidate=600
X-Registry-Version: 41
```

Clients send `If-None-Match` and expect `304`. Mobile apps persist the body against the ETag.

---

## 1. Taxonomy — `GET /platform/taxonomy/tree`

Drives every category picker, every make/model dropdown, and every search facet.

```jsonc
{
  "version": 41,
  "categories": [
    {
      "code": "car",
      "name": "Car",
      "icon": "car",
      "sortOrder": 1,
      "children": [
        { "code": "suv", "name": "SUV", "sortOrder": 2, "children": [] }
      ],
      "attributes": ["fuel_type", "transmission", "owners", "kms_driven", "seats"]
    },
    {
      "code": "commercial",
      "name": "Commercial",
      "sortOrder": 5,
      "children": [ { "code": "truck", "name": "Truck", "children": [] } ],
      "attributes": ["fuel_type", "payload_kg", "axle_config", "permit_type"]
    }
  ]
}
```

### `GET /platform/taxonomy/attributes?category=suv`

The attribute definitions a category exposes. **This is what makes a new vehicle type possible
without a deploy** — a new attribute here becomes a form field, a search facet, and a spec row.

```jsonc
{
  "version": 41,
  "attributes": [
    {
      "code": "fuel_type",
      "label": "Fuel",
      "dataType": "enum",            // string | number | integer | boolean | enum | multi_enum | date | money
      "unit": null,
      "required": true,
      "searchable": true,            // becomes an OpenSearch facet
      "facetStyle": "chips",         // chips | list | range | toggle
      "displayGroup": "engine",
      "sortOrder": 10,
      "options": [
        { "value": "petrol", "label": "Petrol", "sortOrder": 1 },
        { "value": "diesel", "label": "Diesel", "sortOrder": 2 },
        { "value": "cng",    "label": "CNG",    "sortOrder": 3 },
        { "value": "ev",     "label": "Electric","sortOrder": 4 }
      ]
    },
    {
      "code": "kms_driven",
      "label": "Kms driven",
      "dataType": "integer",
      "unit": "km",
      "required": true,
      "searchable": true,
      "facetStyle": "range",
      "range": { "min": 0, "max": 500000, "step": 1000 },
      "displayGroup": "usage",
      "sortOrder": 20
    }
  ]
}
```

### `GET /platform/taxonomy/makes?category=car`
### `GET /platform/taxonomy/models?make=maruti-suzuki`
### `GET /platform/taxonomy/variants?model=swift&year=2021`

```jsonc
{ "version": 41, "items": [ { "code": "swift", "name": "Swift", "makeCode": "maruti-suzuki", "years": [2005, 2026] } ] }
```

---

## 2. Form schema — `GET /platform/forms/{key}`

`key` examples: `sell_vehicle`, `dealer_onboarding`, `inspection_checklist`, `loan_application`.
Optional `?context=category:suv` selects the variant for a category.

The frontend has **one** form renderer. It never knows what a sell form contains.

```jsonc
{
  "key": "sell_vehicle",
  "version": 7,
  "title": "Tell us about your car",
  "submitLabel": "Get my price",
  "steps": [
    {
      "key": "identity",
      "title": "Your car",
      "description": "We pull most of this from the RTO record.",
      "fields": [
        {
          "key": "registration_number",
          "label": "Registration number",
          "control": "plate",              // plate | text | number | select | multiselect | radio | checkbox
                                            // | date | money | phone | otp | file | photo_set | slider | toggle
                                            // | address | vehicle_picker | rich_text
          "required": true,
          "placeholder": "MH 12 AB 1234",
          "helpText": "Exactly as printed on the plate.",
          "validation": [
            { "rule": "pattern", "value": "^[A-Z]{2}[0-9]{1,2}[A-Z]{0,3}[0-9]{1,4}$",
              "code": "registration.malformed",
              "message": "Check the digits — example MH12AB1234." }
          ]
        },
        {
          "key": "kms_driven",
          "label": "Kms driven",
          "control": "number",
          "unit": "km",
          "required": true,
          "validation": [ { "rule": "max", "value": 500000, "code": "kms.too_high",
                            "message": "Enter the odometer reading in kilometres." } ]
        },
        {
          "key": "battery_health_pct",
          "label": "Battery health",
          "control": "slider",
          "unit": "%",
          "required": true,
          // Conditional visibility is JsonLogic over the form's own values.
          // This is why adding EVs needed no code: the field appears when fuel_type is ev.
          "visibleWhen": { "==": [ { "var": "fuel_type" }, "ev" ] }
        }
      ]
    },
    {
      "key": "condition",
      "title": "Condition",
      "fields": [
        { "key": "photos", "label": "Photos", "control": "photo_set", "required": true,
          "config": { "min": 6, "max": 20, "prompts": ["Front 3/4", "Rear 3/4", "Odometer", "Interior", "Engine bay", "Tyres"] } }
      ]
    }
  ]
}
```

**Validation rules** (`rule` values): `required`, `min`, `max`, `minLength`, `maxLength`, `pattern`,
`email`, `phone`, `oneOf`, `custom` (server-side only). Every rule carries its own `code` and
`message` so the same schema validates identically on web, Flutter, and the server.

`POST /platform/forms/{key}/validate` — server-side validation of a partial or complete payload.
Returns `200 {"valid":true}` or `422` with `metadata.fieldErrors: { "kms_driven": ["kms.too_high"] }`.

---

## 3. Workflow — `GET /platform/workflows/{key}`

```jsonc
{
  "key": "listing",
  "version": 3,
  "entityType": "listings.listing",
  "states": [
    { "code": "draft",             "name": "Draft",              "category": "draft",           "isInitial": true },
    { "code": "pending_review",    "name": "Under review",       "category": "awaiting_action", "slaMinutes": 240 },
    { "code": "live",              "name": "Live",               "category": "in_progress" },
    { "code": "sold",              "name": "Sold",               "category": "succeeded",       "isTerminal": true },
    { "code": "rejected",          "name": "Rejected",           "category": "failed",          "isTerminal": true }
  ],
  "transitions": [
    { "actionCode": "submit", "label": "Submit for review", "from": "draft", "to": "pending_review",
      "requiredPermission": "listing.submit", "requiresReason": false },
    { "actionCode": "approve", "label": "Approve", "from": "pending_review", "to": "live",
      "requiredPermission": "listing.moderate" },
    { "actionCode": "reject", "label": "Reject", "from": "pending_review", "to": "rejected",
      "requiredPermission": "listing.moderate", "requiresReason": true }
  ]
}
```

`category` maps to a `StatusPill` tone in the design system — the server never sends a colour.

### `GET /{module}/{resource}/{id}/workflow`

The live state of one entity, plus the actions **this** user may fire right now (guards and
permissions already evaluated server-side — the client never re-derives authorisation):

```jsonc
{
  "workflowKey": "listing",
  "currentState": { "code": "pending_review", "name": "Under review", "category": "awaiting_action" },
  "enteredAt": "2026-07-29T04:10:00Z",
  "dueAt": "2026-07-29T08:10:00Z",
  "availableActions": [
    { "actionCode": "approve", "label": "Approve", "requiresReason": false },
    { "actionCode": "reject",  "label": "Reject",  "requiresReason": true }
  ],
  "history": [
    { "from": null, "to": "draft", "actionCode": null, "occurredAt": "2026-07-28T11:00:00Z", "actor": "You" },
    { "from": "draft", "to": "pending_review", "actionCode": "submit",
      "occurredAt": "2026-07-29T04:10:00Z", "actor": "You", "timeInPreviousState": "PT17H10M" }
  ]
}
```

### `POST /{module}/{resource}/{id}/workflow/actions`

```jsonc
// request — Idempotency-Key header required
{ "actionCode": "reject", "reason": "Photos do not match the registration number." }
```
Returns the same shape as the GET. `409 workflow.transition_not_allowed` if the state moved
underneath the client — with `metadata.currentState` so the UI can refresh rather than guess.

---

## 4. Pricing quote — `POST /platform/pricing/quote`

Nothing in the product computes a fee. Everything asks.

```jsonc
// request
{
  "scope": "auction.win",              // the rule set to evaluate
  "context": {
    "vehicle":  { "category": "suv", "makeCode": "mahindra", "year": 2021 },
    "location": { "state": "MH", "city": "pune" },
    "party":    { "type": "dealer", "tier": "gold", "id": "019fa..." },
    "amount":   { "minorUnits": 84735000, "currency": "INR" }
  }
}
```
```jsonc
// response — every line is explainable, because a dealer will ask
{
  "currency": "INR",
  "baseAmount": 84735000,
  "lines": [
    { "code": "auction_commission", "label": "Auction commission", "basis": "percent",
      "rate": 0.02, "amountMinor": 1694700, "ruleId": "019fa...", "ruleName": "Auction — gold dealer, MH" },
    { "code": "gst", "label": "GST 18%", "basis": "percent_of_lines", "rate": 0.18,
      "amountMinor": 305046, "appliesTo": ["auction_commission"] }
  ],
  "totalFeesMinor": 1999746,
  "payableMinor": 86734746,
  "explanation": "Gold dealers in Maharashtra pay 2% on auction wins (slab ₹5L–₹15L)."
}
```

---

## 5. Feature flags — `GET /platform/features`

```jsonc
{
  "version": 12,
  "modules":  { "auction": true, "lease": false, "attachment": true },
  "flags":    { "instant_offer": true, "ai_damage_detection": false, "whatsapp_chat": true }
}
```
Evaluated server-side for the calling user. The client receives booleans, never targeting rules —
targeting logic on the client is targeting logic an attacker can read.

---

## 6. Localisation — `GET /platform/i18n/{locale}?namespace=web`

`locale`: `en-IN` (default), `hi-IN`, `mr-IN`, `ta-IN`, `te-IN`, `bn-IN`, `gu-IN`, `kn-IN`.

```jsonc
{
  "locale": "hi-IN",
  "version": 88,
  "resources": {
    "listing.cta.get_valuation": "कीमत जानें",
    "error.registration.malformed": "अंक जाँचें — उदाहरण MH12AB1234"
  }
}
```
Missing key → fall back to `en-IN` → fall back to the key itself. Never render an empty string.

---

## 7. Geography — `GET /platform/geo/cities?serviceable=true`

```jsonc
{ "version": 9,
  "items": [ { "code": "pune", "name": "Pune", "stateCode": "MH", "lat": 18.5204, "lng": 73.8567,
               "serviceable": true, "modules": ["buy","sell","auction","rental"] } ] }
```

---

## 8. Auth — `/auth/*`

OTP-first, because India is phone-first.

| Method | Path | Body | Returns |
|---|---|---|---|
| POST | `/auth/otp/request` | `{ "phone": "+919812345678", "purpose": "login" }` | `{ "challengeId", "expiresInSeconds": 300, "resendAfterSeconds": 30 }` |
| POST | `/auth/otp/verify` | `{ "challengeId", "code": "482915", "deviceId" }` | `{ "accessToken", "refreshToken", "expiresIn": 900, "user": {…} }` |
| POST | `/auth/refresh` | `{ "refreshToken", "deviceId" }` | new pair; the old refresh token is invalidated |
| POST | `/auth/logout` | `{ "refreshToken" }` | `204` |
| GET | `/auth/me` | — | `{ "id", "displayName", "phone", "roles": [], "permissions": [], "activeCity" }` |

Access token: 15 min. Refresh: 30 days, rotating, single-use, with reuse detection — a replayed
refresh token revokes the whole family and forces re-authentication.

`GET /auth/me` returns resolved **permissions**, never roles-as-authorisation. The client shows and
hides on permissions; the server enforces on permissions. Role is a grouping, not a check.

---

## 9. Search — `GET /catalog/search`

Facets are generated from the taxonomy registry, so the query grammar cannot be a fixed field list.

```
/catalog/search?q=swift&city=pune&intent=sale
  &attr.fuel_type=petrol,cng          // multi-value = OR within an attribute
  &attr.kms_driven=0..40000           // range
  &price=300000..600000
  &sort=relevance|price_asc|price_desc|newest|kms_asc
  &cursor=...&limit=24
```

```jsonc
{
  "items": [
    {
      "listingId": "019fa...",
      "intent": "sale",
      "registration": { "canonical": "MH12AB1234", "display": "MH 12 AB 1234", "plateClass": "private" },
      "title": "2021 Maruti Suzuki Swift VXi",
      "priceMinor": 54900000,
      "city": "Pune",
      "specStrip": [                    // rendered by SpecStrip — order and content come from the registry
        { "label": "Year",  "value": "2021" },
        { "label": "Kms",   "value": "38,400" },
        { "label": "Fuel",  "value": "Petrol" },
        { "label": "Owner", "value": "1st" }
      ],
      "media": { "coverUrl": "https://…/cover.avif", "aspectRatio": 1.5, "count": 14 },
      "badges": ["inspected", "verified_rc"]
    }
  ],
  "facets": [
    { "code": "fuel_type", "label": "Fuel", "style": "chips",
      "options": [ { "value": "petrol", "label": "Petrol", "count": 412 } ] },
    { "code": "kms_driven", "label": "Kms driven", "style": "range", "min": 0, "max": 180000 }
  ],
  "nextCursor": "eyJ…",
  "totalEstimate": 1284
}
```

---

## 10. Valuation — `POST /valuation/quotes`

The homepage hero posts here. No auth required for the first quote — asking for a phone number
before showing a number is why users bounce.

```jsonc
// request
{ "registrationNumber": "MH12AB1234", "kmsDriven": 38400, "city": "pune", "condition": "good" }
```
```jsonc
// response
{
  "quoteId": "019fa...",
  "vehicle": { "title": "2021 Maruti Suzuki Swift VXi", "registration": { "display": "MH 12 AB 1234" },
               "year": 2021, "fuel": "Petrol", "source": "vahan" },
  "currency": "INR",
  "valuations": [
    { "code": "market",      "label": "Market value",    "amountMinor": 54900000, "isPrimary": true },
    { "code": "trade",       "label": "Trade value",     "amountMinor": 49500000 },
    { "code": "dealer",      "label": "Dealer offer",    "amountMinor": 51200000 },
    { "code": "auction",     "label": "Auction estimate","amountMinor": 50100000 },
    { "code": "quick_sale",  "label": "Quick sale",      "amountMinor": 47800000 }
  ],
  "range": { "lowMinor": 47800000, "highMinor": 58200000 },
  "confidence": { "score": 0.86, "band": "high",
                  "basedOn": "412 comparable sales in Pune over 90 days" },
  "asOf": "2026-07-29",
  "expiresAt": "2026-08-05T00:00:00Z",
  "modelVersion": "valuation-ensemble@2026.07.3"
}
```

`amountMinor` everywhere. The client formats `₹5.49L`. The server never sends a formatted string.

---

## 11. Realtime — auction

`WSS /realtime/auction/{lotId}?token={accessToken}`

```jsonc
// server → client
{ "seq": 1042, "type": "bid.placed",
  "data": { "lotId": "019fa…", "amountMinor": 51500000, "bidderMask": "Dealer ••42",
            "placedAt": "2026-07-29T09:14:02.418Z", "highestBidderIsYou": false } }
{ "seq": 1043, "type": "lot.extended", "data": { "endsAt": "2026-07-29T09:16:02Z", "reason": "anti_snipe" } }
{ "seq": 1044, "type": "lot.closed",   "data": { "outcome": "sold", "winnerIsYou": true } }
```

Bids are **not** placed over the socket — they go to `POST /auction/lots/{id}/bids` with an
`Idempotency-Key`, so a dropped connection can never lose or duplicate a bid. The socket is a
read-side broadcast. On reconnect the client sends `?sinceSeq=1042` and receives the gap.

---

## 12. Standard headers

| Header | Direction | Meaning |
|---|---|---|
| `Idempotency-Key` | → | Required on every POST/PUT/PATCH/DELETE. UUID. Replays return the original response. |
| `X-Device-Id` | → | Stable per install. Binds refresh tokens; feeds fraud signals. |
| `Accept-Language` | → | Locale for server-resolved strings. |
| `X-Active-City` | → | Scopes search, pricing, and feature evaluation. |
| `X-Registry-Version` | ← | Version of the registry snapshot that served this response. |
| `X-Request-Id` / `traceparent` | ↔ | Correlation across services and into logs. |
