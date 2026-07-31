/**
 * Registry #4 — Pricing & Commission.
 *
 * Nothing in the product computes a fee. Everything asks. Every returned line names the rule that
 * produced it, because a dealer will ask why they were charged and "the code says so" is not an
 * answer support can give.
 */

import { z } from 'zod';
import { CurrencySchema, IdSchema, MinorUnitsSchema } from './common';

export const PricingContextSchema = z.object({
  vehicle: z
    .object({
      category: z.string().optional(),
      makeCode: z.string().optional(),
      modelCode: z.string().optional(),
      year: z.number().int().optional(),
    })
    .optional(),
  location: z
    .object({
      state: z.string().optional(),
      city: z.string().optional(),
    })
    .optional(),
  party: z
    .object({
      type: z.string().optional(),
      tier: z.string().optional(),
      id: IdSchema.optional(),
    })
    .optional(),
  amount: z
    .object({
      minorUnits: MinorUnitsSchema,
      currency: CurrencySchema,
    })
    .optional(),
});
export type PricingContext = z.infer<typeof PricingContextSchema>;

export const PricingQuoteRequestSchema = z.object({
  /** The rule set to evaluate: `auction.win`, `listing.publish`, `rental.booking`. */
  scope: z.string(),
  context: PricingContextSchema,
});
export type PricingQuoteRequest = z.infer<typeof PricingQuoteRequestSchema>;

/** How a line was derived. `percent_of_lines` is how GST rides on top of the fees before it. */
export const PricingBasisSchema = z
  .enum(['percent', 'flat', 'percent_of_lines', 'slab', 'per_unit'])
  .catch('flat');
export type PricingBasis = z.infer<typeof PricingBasisSchema>;

export const PricingLineSchema = z.object({
  code: z.string(),
  label: z.string(),
  basis: PricingBasisSchema,
  /** Fraction, not percent: `0.02` is 2%. */
  rate: z.number().nullish(),
  amountMinor: MinorUnitsSchema,
  /** Line codes this line was computed over. Present when `basis` is `percent_of_lines`. */
  appliesTo: z.array(z.string()).optional(),
  ruleId: IdSchema.optional(),
  ruleName: z.string().optional(),
  hsnSac: z.string().nullish(),
});
export type PricingLine = z.infer<typeof PricingLineSchema>;

export const PricingQuoteSchema = z.object({
  currency: CurrencySchema,
  baseAmount: MinorUnitsSchema,
  lines: z.array(PricingLineSchema),
  totalFeesMinor: MinorUnitsSchema,
  payableMinor: MinorUnitsSchema,
  /** One sentence a dealer can read: "Gold dealers in Maharashtra pay 2% on auction wins." */
  explanation: z.string().nullish(),
});
export type PricingQuote = z.infer<typeof PricingQuoteSchema>;
