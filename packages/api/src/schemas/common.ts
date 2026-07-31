/**
 * Shapes that recur across every endpoint in docs/contracts/registry-api.md.
 *
 * Parsing policy: zod's default `strip`. Unknown response fields are ignored rather than rejected,
 * which is exactly what the versioning policy requires of clients — the server adds fields into the
 * current major without announcement, and a client that rejects them is a client that breaks on a
 * Tuesday.
 */

import { z } from 'zod';

/** Opaque server identifier. UUIDv7 today; clients may not depend on the shape. */
export const IdSchema = z.string().min(1);

/** ISO-8601 UTC. Kept as a string — formatting happens at the edge, in IST. */
export const IsoDateTimeSchema = z.string().min(1);

/** ISO-8601 calendar date, `YYYY-MM-DD`. */
export const IsoDateSchema = z.string().min(1);

/**
 * Money is always integer minor units (paise). Never a float, never a formatted string.
 * `54900000` is ₹5,49,000 — rendered by the UI as `₹5.49L`.
 */
export const MinorUnitsSchema = z.number().int();

export const CurrencySchema = z.string().min(3).default('INR');

/** Monotonic registry snapshot counter. Clients cache against it and invalidate exactly. */
export const RegistryVersionSchema = z.number().int().nonnegative();

/** A selectable value. Taxonomy attributes, form fields and facets all speak this. */
export const OptionSchema = z.object({
  value: z.string(),
  label: z.string(),
  sortOrder: z.number().int().optional(),
  icon: z.string().nullish(),
  /** Present on facet options only — the number of matching documents. */
  count: z.number().int().nonnegative().optional(),
});
export type Option = z.infer<typeof OptionSchema>;

export const MoneyAmountSchema = z.object({
  minorUnits: MinorUnitsSchema,
  currency: CurrencySchema,
});
export type MoneyAmount = z.infer<typeof MoneyAmountSchema>;

/**
 * Cursor pagination. `totalEstimate` is an estimate by contract — never a `COUNT(*)` on a hot
 * table — so the UI says "about 1,284" and never promises precision it does not have.
 */
export function cursorPageSchema<TItem extends z.ZodTypeAny>(item: TItem) {
  return z.object({
    items: z.array(item),
    nextCursor: z.string().nullish().default(null),
    totalEstimate: z.number().int().nonnegative().nullish().default(null),
  });
}

export type CursorPage<TItem> = {
  items: TItem[];
  nextCursor: string | null | undefined;
  totalEstimate: number | null | undefined;
};

/** Indian plate classes carry real information: white private, yellow commercial, green EV. */
export const PlateClassSchema = z
  .enum(['private', 'commercial', 'ev', 'government', 'defence'])
  .catch('private');
export type PlateClass = z.infer<typeof PlateClassSchema>;

export const RegistrationSchema = z.object({
  /** `MH12AB1234` — the canonical, storage and comparison form. */
  canonical: z.string(),
  /** `MH 12 AB 1234` — the form a PlateBlock renders. */
  display: z.string(),
  plateClass: PlateClassSchema.optional(),
  stateCode: z.string().optional(),
});
export type Registration = z.infer<typeof RegistrationSchema>;

/** A hairline-divided mono data row. Order and content come from the registry, never from code. */
export const SpecItemSchema = z.object({
  label: z.string(),
  value: z.string(),
  code: z.string().optional(),
});
export type SpecItem = z.infer<typeof SpecItemSchema>;

/**
 * A listing's cover image, if it has one.
 *
 * `coverUrl` is nullable because a listing with no photos is a real state, not an error — a dealer's
 * bulk upload lands the rows before the images, and a listing in `draft` may never have had any. An
 * earlier version required a string here, and the effect was out of all proportion to the cause: one
 * photoless listing failed the parse for the *entire* search page, and the marketplace rendered
 * empty. Tolerating the null at the boundary is what stops a missing thumbnail from looking like an
 * outage.
 *
 * `aspectRatio` normalises to 1.5 rather than being left null, because every media slot must have a
 * known ratio before the image arrives or the page shifts as it loads.
 */
export const MediaSchema = z.object({
  coverUrl: z.string().nullish(),
  aspectRatio: z
    .number()
    .positive()
    .nullish()
    .transform((ratio) => ratio ?? 1.5),
  count: z.number().int().nonnegative().catch(0),
});
export type Media = z.infer<typeof MediaSchema>;

export const MediaAssetSchema = z.object({
  id: IdSchema,
  url: z.string(),
  aspectRatio: z.number().positive().default(1.5),
  kind: z.string().default('photo'),
  caption: z.string().nullish(),
});
export type MediaAsset = z.infer<typeof MediaAssetSchema>;

/**
 * Listing intents. This is the one place a closed set is tolerable on the client: an intent is a
 * *route shape*, not a business constant — `/buy`, `/rent`, `/auction` are pages, and a new intent
 * needs a page. Whether an intent is *available* is answered by the feature registry, never here.
 */
export const ListingIntentSchema = z
  .enum(['sale', 'auction', 'rental', 'lease', 'attachment', 'exchange'])
  .catch('sale');
export type ListingIntent = z.infer<typeof ListingIntentSchema>;

/** Dynamic per-category attributes. Keys are taxonomy attribute codes, unknown at compile time. */
export const AttributeValuesSchema = z.record(z.unknown());
export type AttributeValues = z.infer<typeof AttributeValuesSchema>;
