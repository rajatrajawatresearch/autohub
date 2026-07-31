/**
 * Indian number formatting — the edge of the money pipeline.
 *
 * Money travels through this product as integer minor units (paise) and is never a float and
 * never formatted by the server. It becomes a string exactly here, at the moment of render.
 *
 * The grouping is not cosmetic. `₹8,47,350` is how India *writes* money and `₹8.4L` is how India
 * *says* it; western `₹847,350` or `₹847.4K` marks the product as foreign on the first screen.
 *
 * Everything below is deterministic and ICU-free on purpose: `Intl.NumberFormat('en-IN')` depends
 * on the runtime's ICU build, and a server that groups differently from the browser is a hydration
 * mismatch on the single most-rendered string in the product.
 */

/** Rendered wherever a number is genuinely absent. A missing figure is never a silent zero. */
const ABSENT = '—';

const PAISE_PER_RUPEE = 100;
const RUPEES_PER_LAKH = 100_000;
const RUPEES_PER_CRORE = 10_000_000;

/**
 * Currency symbols are a locale invariant, not a business rule — there is no registry for what
 * the rupee sign looks like. Anything unmapped falls back to its ISO code so it is never wrong,
 * only verbose.
 */
const CURRENCY_SYMBOL: Record<string, string> = {
  INR: '₹',
};

export interface FormatMoneyOptions {
  /**
   * `true` → `₹8.4L` / `₹1.2Cr` / `₹94,500` (lakh–crore, how the number is spoken).
   * `false` (default) → `₹8,47,350` (full Indian digit grouping, how it is written).
   */
  compact?: boolean;
  /** ISO 4217 code. Defaults to INR. */
  currency?: string;
}

/**
 * Indian digit grouping: the last three digits, then pairs.
 * `847350` → `8,47,350`; `10000000` → `1,00,00,000`.
 */
function groupIndian(digits: string): string {
  if (digits.length <= 3) return digits;
  const last3 = digits.slice(-3);
  const head = digits.slice(0, -3);
  return `${head.replace(/\B(?=(\d{2})+(?!\d))/g, ',')},${last3}`;
}

/** `8.40` → `8.4`, `1.00` → `1`, `5.49` → `5.49`. Two decimals is the ceiling for a spoken figure. */
function trimScaled(value: number): string {
  return value.toFixed(2).replace(/\.?0+$/, '');
}

function splitAbs(value: number): { negative: boolean; abs: number } {
  return { negative: value < 0, abs: Math.abs(value) };
}

/**
 * `1,24,500`. Integers group cleanly; a fractional value keeps up to two decimals with trailing
 * zeros trimmed, so `1245.5` reads `1,245.5` rather than `1,245.50`.
 */
export function formatIndianNumber(value: number): string {
  if (!Number.isFinite(value)) return ABSENT;

  const { negative, abs } = splitAbs(value);
  const whole = Math.floor(abs);
  const fraction = abs - whole;

  let out = groupIndian(String(whole));
  if (fraction > 0) {
    const decimals = fraction.toFixed(2).slice(1).replace(/\.?0+$/, '');
    out += decimals;
  }

  return negative ? `-${out}` : out;
}

/**
 * `38,400 km`. The unit travels with the number — an odometer reading without `km` is a rumour,
 * and the same field on a commercial listing may be read in a different unit tomorrow.
 */
export function formatKms(kms: number): string {
  if (!Number.isFinite(kms)) return ABSENT;
  return `${formatIndianNumber(Math.round(kms))} km`;
}

/**
 * The single most-seen number in AutoHub.
 *
 * @param minorUnits paise, as an integer. Never rupees, never a float from the wire.
 */
export function formatIndianMoney(minorUnits: number, opts?: FormatMoneyOptions): string {
  if (!Number.isFinite(minorUnits)) return ABSENT;

  const currency = opts?.currency ?? 'INR';
  const symbol = CURRENCY_SYMBOL[currency] ?? `${currency} `;
  const { negative, abs } = splitAbs(minorUnits);
  const sign = negative ? '-' : '';

  if (opts?.compact === true) {
    const rupees = abs / PAISE_PER_RUPEE;

    if (rupees >= RUPEES_PER_CRORE) {
      return `${sign}${symbol}${trimScaled(rupees / RUPEES_PER_CRORE)}Cr`;
    }

    if (rupees >= RUPEES_PER_LAKH) {
      const lakhs = Math.round((rupees / RUPEES_PER_LAKH) * 100) / 100;
      // 99.9995L rounds to 100L, which nobody says. Promote it.
      return lakhs >= 100
        ? `${sign}${symbol}${trimScaled(lakhs / 100)}Cr`
        : `${sign}${symbol}${trimScaled(lakhs)}L`;
    }

    const wholeRupees = Math.round(rupees);
    // ₹99,999.60 rounds up across the lakh boundary; say it as a lakh, don't print ₹1,00,000.
    return wholeRupees >= RUPEES_PER_LAKH
      ? `${sign}${symbol}${trimScaled(wholeRupees / RUPEES_PER_LAKH)}L`
      : `${sign}${symbol}${groupIndian(String(wholeRupees))}`;
  }

  const paise = Math.round(abs);
  const rupees = Math.floor(paise / PAISE_PER_RUPEE);
  const remainder = paise % PAISE_PER_RUPEE;
  const grouped = groupIndian(String(rupees));

  // Paise appear only when they exist. A vehicle price never carries them; a ledger line might.
  return remainder === 0
    ? `${sign}${symbol}${grouped}`
    : `${sign}${symbol}${grouped}.${String(remainder).padStart(2, '0')}`;
}
