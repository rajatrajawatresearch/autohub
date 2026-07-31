/**
 * Listings.
 *
 * PROVISIONAL: `docs/contracts/registry-api.md` pins the *search* projection of a listing (§9) but
 * not yet the detail projection. Everything below is composed from shapes the contract does define
 * — registration, spec strip, media, dynamic attributes, workflow snapshot — so when the backend
 * publishes the detail endpoint the reconciliation is additive rather than a rewrite. Nothing here
 * invents a business constant: spec groups, attribute codes and badge codes all arrive as data.
 */

import { z } from 'zod';
import {
  AttributeValuesSchema,
  CurrencySchema,
  IdSchema,
  IsoDateTimeSchema,
  ListingIntentSchema,
  MediaAssetSchema,
  MinorUnitsSchema,
  SpecItemSchema,
} from './common';
import { SearchItemSchema } from './search';
import { WorkflowSnapshotSchema } from './workflow';

/** The card projection. Identical to a search hit — one shape, one `VehicleCard`. */
export const ListingSummarySchema = SearchItemSchema;
export type ListingSummary = z.infer<typeof ListingSummarySchema>;

/** Spec rows grouped by the taxonomy attribute's `displayGroup`. Order comes from the registry. */
export const SpecGroupSchema = z.object({
  code: z.string(),
  label: z.string(),
  items: z.array(SpecItemSchema),
});
export type SpecGroup = z.infer<typeof SpecGroupSchema>;

export const SellerSchema = z.object({
  id: IdSchema,
  displayName: z.string(),
  /** `individual` or `dealer`. Drives which trust signals are shown, never a colour. */
  type: z.string(),
  city: z.string().nullish(),
  isVerified: z.boolean().default(false),
  memberSince: IsoDateTimeSchema.nullish(),
  listingsCount: z.number().int().nonnegative().nullish(),
  responseTimeMinutes: z.number().int().nonnegative().nullish(),
});
export type Seller = z.infer<typeof SellerSchema>;

export const ListingDetailSchema = ListingSummarySchema.extend({
  description: z.string().nullish(),
  currency: CurrencySchema,
  gallery: z.array(MediaAssetSchema).default([]),
  /** Dynamic per-category values, keyed by taxonomy attribute code. */
  attributes: AttributeValuesSchema.default({}),
  specGroups: z.array(SpecGroupSchema).default([]),
  seller: SellerSchema.nullish(),
  workflow: WorkflowSnapshotSchema.nullish(),
  categoryCode: z.string().nullish(),
  makeCode: z.string().nullish(),
  modelCode: z.string().nullish(),
  variantCode: z.string().nullish(),
  year: z.number().int().nullish(),
  /** Where the vehicle physically is, for a test drive. */
  locality: z.string().nullish(),
  createdAt: IsoDateTimeSchema.nullish(),
  expiresAt: IsoDateTimeSchema.nullish(),
  viewCount: z.number().int().nonnegative().nullish(),
});
export type ListingDetail = z.infer<typeof ListingDetailSchema>;

export const ListingDraftRequestSchema = z.object({
  intent: ListingIntentSchema,
  categoryCode: z.string(),
  registrationNumber: z.string().optional(),
  /** The sell form's own payload, validated against the form registry — not typed here by design. */
  values: z.record(z.unknown()).default({}),
  askingPriceMinor: MinorUnitsSchema.optional(),
});
export type ListingDraftRequest = z.infer<typeof ListingDraftRequestSchema>;

export const ListingListParamsSchema = z.object({
  intent: ListingIntentSchema.optional(),
  /** Workflow state code. Which codes exist is answered by the workflow registry. */
  state: z.string().optional(),
  q: z.string().optional(),
  cursor: z.string().nullish(),
  limit: z.number().int().positive().max(100).optional(),
});
export type ListingListParams = z.infer<typeof ListingListParamsSchema>;
