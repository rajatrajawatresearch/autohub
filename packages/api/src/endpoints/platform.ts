/**
 * The registry plane. Everything the product refuses to hardcode is fetched here.
 */

import { apiGet, apiPost } from '../http';
import type { RequestContext } from '../http';
import {
  AttributesResponseSchema,
  CitiesResponseSchema,
  FeaturesSchema,
  FormDefinitionSchema,
  FormValidateResponseSchema,
  I18nBundleSchema,
  PricingQuoteSchema,
  TaxonomyNodesResponseSchema,
  TaxonomyTreeSchema,
  WorkflowDefinitionSchema,
  WorkflowSnapshotSchema,
} from '../schemas';
import type {
  AttributesResponse,
  CitiesResponse,
  Features,
  FormDefinition,
  FormValidateResponse,
  I18nBundle,
  PricingQuote,
  PricingQuoteRequest,
  TaxonomyNodesResponse,
  TaxonomyTree,
  WorkflowActionRequest,
  WorkflowDefinition,
  WorkflowSnapshot,
  WorkflowTarget,
} from '../schemas';

/* ── #1 Taxonomy ─────────────────────────────────────────────────────────── */

export function getTaxonomyTree(ctx: RequestContext = {}): Promise<TaxonomyTree> {
  return apiGet('/platform/taxonomy/tree', TaxonomyTreeSchema, ctx);
}

export function getAttributes(
  category: string,
  ctx: RequestContext = {},
): Promise<AttributesResponse> {
  return apiGet('/platform/taxonomy/attributes', AttributesResponseSchema, {
    ...ctx,
    query: { category },
  });
}

export function getMakes(
  category?: string,
  ctx: RequestContext = {},
): Promise<TaxonomyNodesResponse> {
  return apiGet('/platform/taxonomy/makes', TaxonomyNodesResponseSchema, {
    ...ctx,
    query: { category },
  });
}

export function getModels(make: string, ctx: RequestContext = {}): Promise<TaxonomyNodesResponse> {
  return apiGet('/platform/taxonomy/models', TaxonomyNodesResponseSchema, {
    ...ctx,
    query: { make },
  });
}

export function getVariants(
  model: string,
  year?: number,
  ctx: RequestContext = {},
): Promise<TaxonomyNodesResponse> {
  return apiGet('/platform/taxonomy/variants', TaxonomyNodesResponseSchema, {
    ...ctx,
    query: { model, year },
  });
}

/* ── #2 Form schema ──────────────────────────────────────────────────────── */

/** `context` selects a variant, e.g. `category:suv`. */
export function getFormDefinition(
  key: string,
  context?: string,
  ctx: RequestContext = {},
): Promise<FormDefinition> {
  return apiGet(`/platform/forms/${encodeURIComponent(key)}`, FormDefinitionSchema, {
    ...ctx,
    query: { context },
  });
}

/**
 * Server-side validation of a partial or complete payload. A `422` arrives as an `ApiError` whose
 * `fieldErrors` map straight onto the form's fields.
 */
export function validateForm(
  key: string,
  values: Record<string, unknown>,
  ctx: RequestContext = {},
): Promise<FormValidateResponse> {
  return apiPost(
    `/platform/forms/${encodeURIComponent(key)}/validate`,
    FormValidateResponseSchema,
    values,
    ctx,
  );
}

/* ── #3 Workflow ─────────────────────────────────────────────────────────── */

export function getWorkflowDefinition(
  key: string,
  ctx: RequestContext = {},
): Promise<WorkflowDefinition> {
  return apiGet(`/platform/workflows/${encodeURIComponent(key)}`, WorkflowDefinitionSchema, ctx);
}

function workflowPath(target: WorkflowTarget): string {
  return `/${target.module}/${target.resource}/${encodeURIComponent(target.id)}/workflow`;
}

export function getWorkflowSnapshot(
  target: WorkflowTarget,
  ctx: RequestContext = {},
): Promise<WorkflowSnapshot> {
  return apiGet(workflowPath(target), WorkflowSnapshotSchema, ctx);
}

/**
 * Fires a transition. Returns the same shape as the GET, so the caller replaces state rather than
 * reconciling it. A `409 platform.workflow.transition_not_allowed` carries `metadata.currentState`
 * — refresh from that, never guess.
 */
export function fireWorkflowAction(
  target: WorkflowTarget,
  request: WorkflowActionRequest,
  ctx: RequestContext & { idempotencyKey?: string } = {},
): Promise<WorkflowSnapshot> {
  return apiPost(`${workflowPath(target)}/actions`, WorkflowSnapshotSchema, request, ctx);
}

/* ── #4 Pricing ──────────────────────────────────────────────────────────── */

export function getPricingQuote(
  request: PricingQuoteRequest,
  ctx: RequestContext = {},
): Promise<PricingQuote> {
  return apiPost('/platform/pricing/quote', PricingQuoteSchema, request, ctx);
}

/* ── #6 Features ─────────────────────────────────────────────────────────── */

export function getFeatures(ctx: RequestContext = {}): Promise<Features> {
  return apiGet('/platform/features', FeaturesSchema, ctx);
}

/* ── #9 Localisation ─────────────────────────────────────────────────────── */

export function getI18nBundle(
  locale: string,
  namespace = 'web',
  ctx: RequestContext = {},
): Promise<I18nBundle> {
  return apiGet(`/platform/i18n/${encodeURIComponent(locale)}`, I18nBundleSchema, {
    ...ctx,
    query: { namespace },
  });
}

/* ── #10 Geography ───────────────────────────────────────────────────────── */

export function getCities(
  serviceableOnly = true,
  ctx: RequestContext = {},
): Promise<CitiesResponse> {
  return apiGet('/platform/geo/cities', CitiesResponseSchema, {
    ...ctx,
    query: { serviceable: serviceableOnly ? 'true' : undefined },
  });
}
