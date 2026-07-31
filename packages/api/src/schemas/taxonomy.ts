/**
 * Registry #1 — Taxonomy.
 *
 * Drives every category picker, every make/model dropdown, every search facet and every spec row.
 * A new attribute here becomes a form field, a facet and a spec row with no deploy — which is the
 * whole reason the client never carries a fuel-type array.
 */

import { z } from 'zod';
import { IdSchema, OptionSchema, RegistryVersionSchema } from './common';

/**
 * How a value is stored and edited. `money` is minor units; `multi_enum` is an array of option
 * values. Unknown data types degrade to `string` rather than blanking the screen — a registry the
 * client has not shipped support for yet must not take a page down.
 */
export const AttributeDataTypeSchema = z
  .enum(['string', 'number', 'integer', 'boolean', 'enum', 'multi_enum', 'date', 'money'])
  .catch('string');
export type AttributeDataType = z.infer<typeof AttributeDataTypeSchema>;

/** How the attribute is offered as a filter. Maps to a facet component, not to a colour. */
export const FacetStyleSchema = z.enum(['chips', 'list', 'range', 'toggle']).catch('list');
export type FacetStyle = z.infer<typeof FacetStyleSchema>;

export const AttributeRangeSchema = z.object({
  min: z.number(),
  max: z.number(),
  step: z.number().positive().optional(),
});
export type AttributeRange = z.infer<typeof AttributeRangeSchema>;

export const TaxonomyAttributeSchema = z.object({
  code: z.string(),
  label: z.string(),
  dataType: AttributeDataTypeSchema,
  /** `km`, `%`, `kg`, `bhp` — rendered next to the value, never baked into the label. */
  unit: z.string().nullish(),
  required: z.boolean().default(false),
  /** True when the attribute is an OpenSearch facet. */
  searchable: z.boolean().default(false),
  facetStyle: FacetStyleSchema.optional(),
  /** Groups spec rows on the detail page: `engine`, `usage`, `documents`. */
  displayGroup: z.string().nullish(),
  sortOrder: z.number().int().default(0),
  options: z.array(OptionSchema).optional(),
  range: AttributeRangeSchema.optional(),
  helpText: z.string().nullish(),
});
export type TaxonomyAttribute = z.infer<typeof TaxonomyAttributeSchema>;

export const AttributesResponseSchema = z.object({
  version: RegistryVersionSchema,
  attributes: z.array(TaxonomyAttributeSchema),
});
export type AttributesResponse = z.infer<typeof AttributesResponseSchema>;

const TaxonomyCategoryBaseSchema = z.object({
  code: z.string(),
  name: z.string(),
  icon: z.string().nullish(),
  sortOrder: z.number().int().default(0),
  /** Attribute codes this category exposes; resolve them via `GET /taxonomy/attributes`. */
  attributes: z.array(z.string()).default([]),
});

/**
 * The one place a type is written by hand rather than inferred: TypeScript cannot infer a
 * self-referential zod schema. The non-recursive half is still inferred, so only `children` is
 * asserted.
 */
export type TaxonomyCategory = z.infer<typeof TaxonomyCategoryBaseSchema> & {
  children: TaxonomyCategory[];
};

/**
 * The parsed shape and the accepted shape differ: `sortOrder`, `attributes` and `children` all
 * carry defaults, so the server may omit them even though every parsed category has them. Zod's
 * third type parameter is what keeps those two shapes distinct — annotating only the output would
 * demand that callers supply fields the schema exists to fill in.
 */
export type TaxonomyCategoryInput = z.input<typeof TaxonomyCategoryBaseSchema> & {
  children?: TaxonomyCategoryInput[];
};

export const TaxonomyCategorySchema: z.ZodType<
  TaxonomyCategory,
  z.ZodTypeDef,
  TaxonomyCategoryInput
> = TaxonomyCategoryBaseSchema.extend({
  children: z.lazy(() => z.array(TaxonomyCategorySchema)).default([]),
});

export const TaxonomyTreeSchema = z.object({
  version: RegistryVersionSchema,
  categories: z.array(TaxonomyCategorySchema),
});
export type TaxonomyTree = z.infer<typeof TaxonomyTreeSchema>;

/** Makes, models and variants share one node shape — the query string says which you asked for. */
export const TaxonomyNodeSchema = z.object({
  code: z.string(),
  name: z.string(),
  makeCode: z.string().optional(),
  modelCode: z.string().optional(),
  categoryCode: z.string().optional(),
  /** `[firstYear, lastYear]`, inclusive. */
  years: z.tuple([z.number().int(), z.number().int()]).optional(),
  logoUrl: z.string().nullish(),
  popularity: z.number().int().optional(),
});
export type TaxonomyNode = z.infer<typeof TaxonomyNodeSchema>;

export const TaxonomyNodesResponseSchema = z.object({
  version: RegistryVersionSchema,
  items: z.array(TaxonomyNodeSchema),
});
export type TaxonomyNodesResponse = z.infer<typeof TaxonomyNodesResponseSchema>;

/** Convenience shape for the vehicle picker: one resolved make/model/variant/year selection. */
export const VehicleSelectionSchema = z.object({
  categoryCode: z.string(),
  makeCode: z.string(),
  modelCode: z.string(),
  variantCode: z.string().optional(),
  year: z.number().int().optional(),
  vehicleId: IdSchema.optional(),
});
export type VehicleSelection = z.infer<typeof VehicleSelectionSchema>;
