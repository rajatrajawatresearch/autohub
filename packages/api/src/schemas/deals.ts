/**
 * Deals — the settlement spine. One ledger, one escrow, one commission engine, whatever the
 * listing intent was.
 *
 * PROVISIONAL: composed from contract shapes (workflow snapshot, pricing lines, listing summary)
 * pending the backend publishing the deals read model.
 */

import { z } from 'zod';
import { CurrencySchema, IdSchema, IsoDateTimeSchema, MinorUnitsSchema } from './common';
import { ListingSummarySchema } from './listings';
import { PricingLineSchema } from './pricing';
import { WorkflowSnapshotSchema, WorkflowStateSchema } from './workflow';

export const DealPartySchema = z.object({
  id: IdSchema,
  displayName: z.string(),
  /** `buyer`, `seller`, `agent`, `financier`. */
  role: z.string(),
  isYou: z.boolean().default(false),
  phone: z.string().nullish(),
});
export type DealParty = z.infer<typeof DealPartySchema>;

export const DealDocumentSchema = z.object({
  id: IdSchema,
  /** `rc`, `form_29`, `form_30`, `noc`, `insurance` — codes, labels come from i18n. */
  code: z.string(),
  label: z.string(),
  status: z.string(),
  url: z.string().nullish(),
  uploadedAt: IsoDateTimeSchema.nullish(),
});
export type DealDocument = z.infer<typeof DealDocumentSchema>;

export const DealSummarySchema = z.object({
  dealId: IdSchema,
  reference: z.string(),
  listing: ListingSummarySchema,
  currency: CurrencySchema,
  amountMinor: MinorUnitsSchema,
  state: WorkflowStateSchema,
  counterparty: DealPartySchema.nullish(),
  createdAt: IsoDateTimeSchema,
  updatedAt: IsoDateTimeSchema.nullish(),
});
export type DealSummary = z.infer<typeof DealSummarySchema>;

export const DealDetailSchema = DealSummarySchema.extend({
  parties: z.array(DealPartySchema).default([]),
  /** Every rupee, with the rule that produced it. Support answers "why?" from this array. */
  fees: z.array(PricingLineSchema).default([]),
  totalFeesMinor: MinorUnitsSchema.default(0),
  payableMinor: MinorUnitsSchema.default(0),
  escrowHeldMinor: MinorUnitsSchema.nullish(),
  documents: z.array(DealDocumentSchema).default([]),
  workflow: WorkflowSnapshotSchema.nullish(),
});
export type DealDetail = z.infer<typeof DealDetailSchema>;

export const DealListParamsSchema = z.object({
  /** `buying` or `selling` — which side of the table the caller is on. */
  side: z.enum(['buying', 'selling']).optional(),
  state: z.string().optional(),
  cursor: z.string().nullish(),
  limit: z.number().int().positive().max(100).optional(),
});
export type DealListParams = z.infer<typeof DealListParamsSchema>;

export const OfferRequestSchema = z.object({
  listingId: IdSchema,
  amountMinor: MinorUnitsSchema,
  message: z.string().max(500).optional(),
});
export type OfferRequest = z.infer<typeof OfferRequestSchema>;
