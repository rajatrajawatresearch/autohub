/**
 * Valuation — `POST /valuation/quotes`.
 *
 * The homepage hero posts here. No auth is required for the first quote: asking for a phone number
 * before showing a number is why users bounce.
 *
 * `amountMinor` everywhere. The client renders `₹5.49L`; the server never sends a formatted string.
 */

import { z } from 'zod';
import {
  CurrencySchema,
  IdSchema,
  IsoDateSchema,
  IsoDateTimeSchema,
  MinorUnitsSchema,
  RegistrationSchema,
} from './common';

/** How the seller describes the car. The options themselves come from the form registry. */
export const ValuationConditionSchema = z
  .enum(['excellent', 'good', 'fair', 'poor'])
  .catch('good');
export type ValuationCondition = z.infer<typeof ValuationConditionSchema>;

export const ValuationQuoteRequestSchema = z.object({
  registrationNumber: z.string().min(4),
  kmsDriven: z.number().int().nonnegative().optional(),
  city: z.string().optional(),
  condition: ValuationConditionSchema.optional(),
  /** Set when the plate is not in VAHAN and the user typed the vehicle in by hand. */
  makeCode: z.string().optional(),
  modelCode: z.string().optional(),
  variantCode: z.string().optional(),
  year: z.number().int().optional(),
});
export type ValuationQuoteRequest = z.infer<typeof ValuationQuoteRequestSchema>;

export const ValuationVehicleSchema = z.object({
  title: z.string(),
  registration: RegistrationSchema,
  year: z.number().int().nullish(),
  fuel: z.string().nullish(),
  transmission: z.string().nullish(),
  /** `vahan` when the RTO record answered, `manual` when the user typed it. */
  source: z.string().nullish(),
});
export type ValuationVehicle = z.infer<typeof ValuationVehicleSchema>;

export const ValuationBandSchema = z.object({
  code: z.string(),
  label: z.string(),
  amountMinor: MinorUnitsSchema,
  /** Exactly one band is primary — the number the odometer rolls to. */
  isPrimary: z.boolean().default(false),
  helpText: z.string().nullish(),
});
export type ValuationBand = z.infer<typeof ValuationBandSchema>;

export const ValuationConfidenceSchema = z.object({
  score: z.number().min(0).max(1),
  band: z.enum(['high', 'medium', 'low']).catch('medium'),
  /** "412 comparable sales in Pune over 90 days". A number without its basis is a rumour. */
  basedOn: z.string().nullish(),
});
export type ValuationConfidence = z.infer<typeof ValuationConfidenceSchema>;

export const ValuationQuoteSchema = z.object({
  quoteId: IdSchema,
  vehicle: ValuationVehicleSchema,
  currency: CurrencySchema,
  valuations: z.array(ValuationBandSchema),
  range: z.object({ lowMinor: MinorUnitsSchema, highMinor: MinorUnitsSchema }),
  confidence: ValuationConfidenceSchema,
  /** The "as of" date. A valuation without one is a rumour. */
  asOf: IsoDateSchema,
  expiresAt: IsoDateTimeSchema,
  modelVersion: z.string().nullish(),
});
export type ValuationQuote = z.infer<typeof ValuationQuoteSchema>;
