# AutoHub Design System — "Registry"

> The visual anchor. `packages/ui` implements exactly this. If a screen invents a token,
> the screen is wrong.

---

## 1. The thesis

Most Indian auto marketplaces look like **retail**: rounded cards, hero photography, a bright
promotional accent, gradient CTAs. They sell you a mood.

AutoHub is not retail. It is a **registry** — the authoritative record of what a vehicle *is*,
what it is *worth*, and who may *transact* it. Dealers spend six hours a day inside it. Fleet
operators reconcile commissions in it. Inspectors fill it in standing next to a car in the sun.

So the design language is **instrumentation, not merchandising.** Precise, dense, legible, calm.
Printed rather than glowing. The interface should feel like a well-made measuring tool.

The materials come from the subject's own world — the things that actually exist in Indian
automotive life:

| Artifact | What we take from it |
|---|---|
| HSRP number plate | The identity block: mono, letter-spaced, hard-bordered, black on plate-white |
| Highway signage (green boards) | The primary color, and the rule that structure is legible at speed |
| Commercial / taxi plate (yellow) | The action color — reserved for live, urgent, money-moving states |
| RC smart card | The document card layout: label above value, fields in a fixed grid |
| Odometer | Numerals roll. Data has weight. Used once, on the valuation reveal. |
| Chassis stamping | Hard, monospaced, uppercase identifiers — never prettified |

---

## 2. Color

A two-color system with a disciplined neutral ramp. Green carries **structure and trust**.
Yellow carries **action and liveness**. Nothing else gets a color unless it is a status.

```css
/* Core */
--ah-tarmac:      #0F1512;   /* near-black, faintly green — dark surfaces, plate text  */
--ah-highway:     #0B3D2E;   /* deep signage green — PRIMARY: structure, trust, verified */
--ah-highway-lit: #145A44;   /* hover / raised primary                                  */
--ah-signal:      #F2C230;   /* commercial-plate yellow — ACTION: bid, live, CTA        */
--ah-plate:       #F6F5F1;   /* plate off-white — page background                       */
--ah-chalk:       #FFFFFF;   /* card surface                                            */

/* Neutral ramp — cool-green tinted, never pure gray */
--ah-n-950: #0F1512;
--ah-n-800: #1D2622;
--ah-n-600: #47544D;
--ah-n-500: #64726A;   /* secondary text — AA on chalk and plate */
--ah-n-300: #C3CBC5;
--ah-n-200: #DEE3DF;   /* hairline rules */
--ah-n-100: #EDF0EC;   /* subtle fills */

/* Status — the only other colors permitted */
--ah-flag:    #C1402B;   /* expired, rejected, overdue        */
--ah-caution: #B5771A;   /* pending, expiring soon            */
--ah-clear:   #1E7A54;   /* verified, settled, passed         */
--ah-info:    #2C5A8C;   /* informational, under review       */
```

### Rules that keep it from drifting

1. **Yellow is rationed.** One `--ah-signal` element per viewport, maximum. It marks the single
   most consequential action on screen (Place bid / Get valuation / Accept offer). Yellow on a
   secondary button is a bug.
2. **Green is structure, not decoration.** Headers, active nav, verified marks, primary buttons.
   Never a gradient. Never a glow. Flat fills only.
3. **No gradients anywhere.** Not on buttons, not on hero, not on charts. This is a printed
   instrument.
4. **Dark mode is a real mode, not an inversion.** `--ah-tarmac` surfaces, `--ah-signal` unchanged
   (it is already a dark-surface color), green lifts to `--ah-highway-lit`, neutrals invert through
   the ramp. Both themes ship at parity — dealers work at night.

---

## 3. Type

Three faces, three jobs. All self-hosted via `next/font/google` — zero external requests.

| Role | Face | Why |
|---|---|---|
| **Display** | **Archivo** (variable, `wdth` axis) | A grotesque with a real width axis. Set expanded + 700 it reads like signage; set normal it disappears into UI. One family, signage-to-interface range — no costume needed. |
| **Body** | **Instrument Sans** | Warmer and more humane than Inter, still neutral at 14px in a dense table. Keeps a data-heavy product from feeling clinical. |
| **Data** | **JetBrains Mono** | Plates, VIN, registration, odometer, money, IDs. Tabular figures, unambiguous `0/O` and `1/l` — non-negotiable when a user is typing a chassis number. |

### Scale

Not a modular scale — a **signage scale**. Sizes are chosen for the distance they're read from.

```
display-xl   clamp(2.75rem, 6vw, 4.5rem)  Archivo 700, wdth 112, tracking -0.03em
display-lg   clamp(2rem, 4vw, 3rem)       Archivo 700, wdth 108, tracking -0.025em
heading      1.375rem                     Archivo 600, wdth 100, tracking -0.015em
subheading   1.0625rem                    Archivo 600
body         0.9375rem / 1.6              Instrument Sans 400
body-sm      0.8125rem / 1.55             Instrument Sans 400
label        0.6875rem                    Instrument Sans 600, tracking 0.08em, UPPERCASE
data         0.875rem                     JetBrains Mono 500, tabular-nums
plate        1.25rem                      JetBrains Mono 700, tracking 0.14em, UPPERCASE
```

**The label style is load-bearing.** Every value in the product sits under a small uppercase
letter-spaced label — exactly like the fields on an RC card. That one repetition does more for
coherence than any other single decision.

---

## 4. Layout

- **Grid:** 12 columns, 72rem max content, 1.5rem gutters. Dealer and admin run wide (88rem) —
  they have monitors and they need density.
- **Spacing:** 4px base. `4 8 12 16 24 32 48 64 96`. Nothing between.
- **Radius:** `2px` on interactive elements, `4px` on cards, `0` on plate blocks and table cells.
  Near-square. Rounded corners read as consumer-retail; we are an instrument.
- **Borders:** `1px solid --ah-n-200`. Structure comes from rules, not shadows.
- **Elevation:** exactly two shadows exist — `sm` for hover-raise, `md` for overlays. No `xl` glow.
- **Density modes:** `comfortable` (customer) and `compact` (dealer/admin) — set once on `<html>`,
  changes row heights and paddings via tokens. Same components, two densities.

---

## 5. The signature: `PlateBlock`

**The one thing this product is remembered by.**

Every vehicle in AutoHub — in search, in auction, in inventory, in an inspection report, in an
admin table — is identified by a rendering of its actual Indian registration plate:

```
┌────────────────────┐
│  MH 01  AB  1234   │   JetBrains Mono 700 · +0.14em tracking · UPPERCASE
└────────────────────┘   2px --ah-tarmac border · 0 radius · --ah-plate fill
```

It is not decoration — it is the primary key made visible. It appears at four sizes
(`xs` inline in tables → `lg` on the vehicle detail page), and it carries a state:
white plate = private, yellow plate = commercial/taxi, green stripe = EV. **The plate color is
real information**, matching how Indian plates actually work.

### And it is the hero

The customer homepage does not open with a photograph of a car and a promotional headline.
It opens with an **empty plate, waiting for you to type in it**:

```
        WHAT IS YOUR CAR WORTH TODAY?

        ┌──────────────────────────────┐
        │  M H _ _   _ _   _ _ _ _     │   ← the input IS the plate
        └──────────────────────────────┘
              [ Get valuation → ]

        Free · 30 seconds · no phone number needed
```

That is the thesis rendered as an interaction: the most characteristic artifact in the subject's
world, doing the most valuable job in the product. One input, one outcome, no funnel.

### The one motion

When the valuation returns, the rupee figure **rolls up like a mechanical odometer** —
digits counting into place, ~600ms, easing out. Once. Nowhere else in the product.
Fully suppressed under `prefers-reduced-motion`.

Everything else is instant. No page-load chorus, no scroll-reveals, no ambient background motion,
no parallax. The restraint is what makes the one moment land.

---

## 6. Component canon

Built once in `packages/ui`, used identically by web / dealer / admin:

| Component | Job |
|---|---|
| `PlateBlock` | Registration identity, 4 sizes, 3 plate classes |
| `VehicleCard` | The canonical vehicle object — plate, title, mono spec strip (year · km · fuel · owners) |
| `SpecStrip` | Hairline-divided mono data row. Used in cards, headers, PDFs |
| `FieldBlock` | Uppercase label over value. The RC-card atom. Everything is made of these |
| `StatusPill` | Square, 2px radius, status color, uppercase label. Driven by workflow registry |
| `MoneyValue` | Mono, tabular, ₹ symbol at 0.85em, lakh/crore aware (`₹8.4L`, `₹1.2Cr`) |
| `OdometerValue` | The one animated numeral |
| `SchemaForm` | Renders any form from the Form Schema Registry |
| `WorkflowTimeline` | Renders any entity's state history from the Workflow Registry |
| `DataTable` | TanStack Table + compact density + column registry |
| `BidTicket` | Auction-only: live countdown, current bid, your position |

`MoneyValue` being lakh/crore aware is not a nicety. `₹8,40,000` is how India writes money, and
`₹8.4L` is how India *says* it. Rendering `$840,000.00` formatting would mark the product as
foreign on the first screen.

---

## 7. Voice

Words are design material. The register is **a knowledgeable dealer, not a salesperson.**

- Say what happens: **Place bid**, not *Submit*. **Get valuation**, not *Check price now!*
- The action keeps its name through the whole flow. `Place bid` → toast `Bid placed`.
- Name things the way the user does: *RC*, *challan*, *owner count*, *kms driven*, *insurance
  validity*. Never *vehicle_registration_certificate_document*.
- Errors state the problem and the fix, in the interface's voice, and never apologize:
  > *This registration number isn't in the RTO records. Check the digits, or enter details manually.*
- Empty states are instructions, not moods:
  > *No bids yet. Bidding opens when the reserve is set.*
- Numbers always carry their unit and their date. A valuation without "as of today" is a rumor.

---

## 8. Quality floor (enforced, not aspirational)

- AA contrast on every text/background pair — including `--ah-signal` (dark text only, never white).
- Every interactive element keyboard-reachable with a **visible** 2px `--ah-highway` focus ring.
- `prefers-reduced-motion` removes the odometer roll and all transitions.
- Responsive to 360px — inspectors use phones outdoors, in sunlight, one-handed.
- Touch targets ≥ 44px in `comfortable`, ≥ 36px in `compact` (desktop-only mode).
- Zero layout shift on image load — every media slot has a known aspect ratio.
