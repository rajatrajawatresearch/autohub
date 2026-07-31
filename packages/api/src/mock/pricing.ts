/**
 * Mock pricing engine.
 *
 * The percentages below live here — in a fixture standing in for the Pricing Registry — and
 * nowhere else. No screen computes a fee; every screen asks this endpoint and renders the lines it
 * gets back, each one naming the rule that produced it.
 */

import type { PricingQuote, PricingQuoteRequest } from '../schemas';

interface CommissionRule {
  id: string;
  name: string;
  scope: string;
  code: string;
  label: string;
  /** Fraction. `0.02` is 2%. */
  rate: number;
  minMinor?: number;
  maxMinor?: number;
  capMinor?: number;
  explanation: string;
}

const RULES: CommissionRule[] = [
  {
    id: '019fa000-0001-7000-8000-000000000001',
    name: 'Auction — gold dealer, MH',
    scope: 'auction.win',
    code: 'auction_commission',
    label: 'Auction commission',
    rate: 0.02,
    minMinor: 50000000,
    maxMinor: 150000000,
    explanation: 'Gold dealers in Maharashtra pay 2% on auction wins (slab ₹5L–₹15L).',
  },
  {
    id: '019fa000-0002-7000-8000-000000000002',
    name: 'Auction — standard',
    scope: 'auction.win',
    code: 'auction_commission',
    label: 'Auction commission',
    rate: 0.025,
    explanation: 'Standard auction commission is 2.5% of the winning bid.',
  },
  {
    id: '019fa000-0003-7000-8000-000000000003',
    name: 'Deal settlement — platform fee',
    scope: 'deal.settlement',
    code: 'platform_fee',
    label: 'Platform fee',
    rate: 0.01,
    capMinor: 2500000,
    explanation: 'Platform fee is 1% of the deal value, capped at ₹25,000.',
  },
  {
    id: '019fa000-0004-7000-8000-000000000004',
    name: 'Rental booking',
    scope: 'rental.booking',
    code: 'booking_fee',
    label: 'Booking fee',
    rate: 0.08,
    explanation: 'Rental bookings carry an 8% service fee.',
  },
  {
    id: '019fa000-0005-7000-8000-000000000005',
    name: 'Listing publish — individual seller',
    scope: 'listing.publish',
    code: 'listing_fee',
    label: 'Listing fee',
    rate: 0,
    explanation: 'Listing is free for individual sellers.',
  },
];

const GST_RATE = 0.18;
const RC_TRANSFER_FACILITATION_MINOR = 199900;

function selectRule(request: PricingQuoteRequest): CommissionRule | undefined {
  const amount = request.context.amount?.minorUnits ?? 0;
  const tier = request.context.party?.tier;
  const state = request.context.location?.state;

  const candidates = RULES.filter((rule) => rule.scope === request.scope);
  const slabMatch = candidates.find((rule) => {
    if (rule.minMinor !== undefined && amount < rule.minMinor) return false;
    if (rule.maxMinor !== undefined && amount > rule.maxMinor) return false;
    if (rule.name.includes('gold') && tier !== 'gold') return false;
    if (rule.name.includes('MH') && state !== undefined && state !== 'MH') return false;
    return true;
  });
  return slabMatch ?? candidates[candidates.length - 1];
}

export function mockPricingQuote(request: PricingQuoteRequest): PricingQuote {
  const baseAmount = request.context.amount?.minorUnits ?? 0;
  const rule = selectRule(request);

  const lines: PricingQuote['lines'] = [];

  if (rule) {
    const raw = Math.round(baseAmount * rule.rate);
    const amountMinor = rule.capMinor !== undefined ? Math.min(raw, rule.capMinor) : raw;
    lines.push({
      code: rule.code,
      label: rule.label,
      basis: 'percent',
      rate: rule.rate,
      amountMinor,
      ruleId: rule.id,
      ruleName: rule.name,
      hsnSac: '998599',
    });
  }

  if (request.scope === 'deal.settlement') {
    lines.push({
      code: 'rc_transfer',
      label: 'RC transfer facilitation',
      basis: 'flat',
      rate: null,
      amountMinor: RC_TRANSFER_FACILITATION_MINOR,
      ruleId: '019fa000-0006-7000-8000-000000000006',
      ruleName: 'RC transfer — facilitation, flat',
      hsnSac: '998599',
    });
  }

  const taxableMinor = lines.reduce((sum, line) => sum + line.amountMinor, 0);
  if (taxableMinor > 0) {
    lines.push({
      code: 'gst',
      label: 'GST 18%',
      basis: 'percent_of_lines',
      rate: GST_RATE,
      amountMinor: Math.round(taxableMinor * GST_RATE),
      appliesTo: lines.map((line) => line.code),
      ruleId: '019fa000-0099-7000-8000-000000000099',
      ruleName: 'GST — services, 18%',
    });
  }

  const totalFeesMinor = lines.reduce((sum, line) => sum + line.amountMinor, 0);

  return {
    currency: request.context.amount?.currency ?? 'INR',
    baseAmount,
    lines,
    totalFeesMinor,
    payableMinor: baseAmount + totalFeesMinor,
    explanation: rule?.explanation ?? null,
  };
}
