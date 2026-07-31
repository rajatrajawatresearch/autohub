/**
 * Search — `GET /catalog/search`.
 *
 * Facets are generated from the taxonomy registry, so the query grammar cannot be a fixed field
 * list. Every attribute filter travels as `attr.{code}`; the client learns which codes exist from
 * the taxonomy registry and from the facets the previous response returned.
 */

import { z } from 'zod';
import {
  IdSchema,
  ListingIntentSchema,
  MediaSchema,
  MinorUnitsSchema,
  OptionSchema,
  RegistrationSchema,
  SpecItemSchema,
} from './common';

export const SearchSortSchema = z
  .enum(['relevance', 'price_asc', 'price_desc', 'newest', 'kms_asc'])
  .catch('relevance');
export type SearchSort = z.infer<typeof SearchSortSchema>;

export const RangeFilterSchema = z.object({
  min: z.number().optional(),
  max: z.number().optional(),
});
export type RangeFilter = z.infer<typeof RangeFilterSchema>;

/**
 * One attribute's filter. An array is OR-within-the-attribute (`petrol,cng`); a range serialises
 * as `0..40000`; a boolean is a toggle facet.
 */
export const AttributeFilterSchema = z.union([z.array(z.string()), RangeFilterSchema, z.boolean()]);
export type AttributeFilter = z.infer<typeof AttributeFilterSchema>;

export const SearchParamsSchema = z.object({
  q: z.string().optional(),
  city: z.string().optional(),
  intent: ListingIntentSchema.optional(),
  price: RangeFilterSchema.optional(),
  /**
   * Keyed by taxonomy attribute code — `fuel_type`, `kms_driven`, `category`. Nothing in this
   * package knows what those codes are.
   */
  attributes: z.record(AttributeFilterSchema).optional(),
  sort: SearchSortSchema.optional(),
  cursor: z.string().nullish(),
  limit: z.number().int().positive().max(60).optional(),
});
export type SearchParams = z.infer<typeof SearchParamsSchema>;

export const SearchItemSchema = z.object({
  listingId: IdSchema,
  intent: ListingIntentSchema,
  registration: RegistrationSchema,
  title: z.string(),
  priceMinor: MinorUnitsSchema.nullish(),
  /** Rental and lease price by period; absent on a straight sale. */
  pricePeriod: z.string().nullish(),
  city: z.string(),
  /** Rendered by `SpecStrip`. Order and content come from the registry, not from the component. */
  specStrip: z.array(SpecItemSchema).default([]),
  media: MediaSchema,
  /** `inspected`, `verified_rc` — codes; labels are resolved through the i18n registry. */
  badges: z.array(z.string()).default([]),
  dealerName: z.string().nullish(),
});
export type SearchItem = z.infer<typeof SearchItemSchema>;

const FacetBaseSchema = z.object({
  code: z.string(),
  label: z.string(),
});

export const SearchFacetSchema = z.discriminatedUnion('style', [
  FacetBaseSchema.extend({ style: z.literal('chips'), options: z.array(OptionSchema) }),
  FacetBaseSchema.extend({ style: z.literal('list'), options: z.array(OptionSchema) }),
  FacetBaseSchema.extend({ style: z.literal('toggle'), options: z.array(OptionSchema) }),
  FacetBaseSchema.extend({
    style: z.literal('range'),
    min: z.number(),
    max: z.number(),
    step: z.number().positive().optional(),
    unit: z.string().nullish(),
  }),
]);
export type SearchFacet = z.infer<typeof SearchFacetSchema>;

export const SearchResponseSchema = z.object({
  items: z.array(SearchItemSchema),
  facets: z.array(SearchFacetSchema).default([]),
  nextCursor: z.string().nullish().default(null),
  totalEstimate: z.number().int().nonnegative().nullish().default(null),
});
export type SearchResponse = z.infer<typeof SearchResponseSchema>;
