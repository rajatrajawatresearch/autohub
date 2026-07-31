/**
 * Server-side data access for React Server Components.
 *
 * No react-query, no hooks, no client state — a Server Component awaits a promise and renders. The
 * schemas and the mock fallback are shared with the client layer, so a page rendered on the server
 * and a page hydrated on the client cannot disagree about what a listing is.
 *
 * Cache policy is set here rather than at every call site:
 *   · registries are revalidated every five minutes and tagged `registry`, so a registry publish
 *     webhook can call `revalidateTag('registry')` and refresh three portals at once
 *   · anything a user is about to act on — a lot, a deal, a workflow snapshot — is never cached
 *
 * Import from `@autohub/api/server`.
 */

import type { RequestContext } from './http';
import {
  getAttributes,
  getCities,
  getFeatures,
  getFormDefinition,
  getI18nBundle,
  getMakes,
  getModels,
  getPricingQuote,
  getTaxonomyTree,
  getVariants,
  getWorkflowDefinition,
  getWorkflowSnapshot,
} from './endpoints/platform';
import { search } from './endpoints/catalog';
import { getListing, listListings } from './endpoints/listings';
import { getAuctionLot, listAuctionLots } from './endpoints/auction';
import { getDeal, listDeals } from './endpoints/deals';
import { getMe } from './endpoints/auth';
import type {
  AuctionLotListParams,
  DealListParams,
  ListingListParams,
  PricingQuoteRequest,
  SearchParams,
  WorkflowTarget,
} from './schemas';

export interface ServerContext {
  /** From the request's `Accept-Language`, or the user's profile. */
  locale?: string;
  /** From the city cookie. Scopes search, pricing and feature evaluation. */
  city?: string;
  /** Read from the request cookie in a layout and threaded down. Servers hold no ambient session. */
  accessToken?: string;
  signal?: AbortSignal;
}

const REGISTRY_REVALIDATE_SECONDS = 300;

/** Registries: cached, tagged, and invalidated as a group when the registry version moves. */
function registryOptions(ctx: ServerContext): RequestContext {
  return { ...ctx, revalidate: REGISTRY_REVALIDATE_SECONDS, tags: ['registry'] };
}

/** Content: cached briefly. A listing page is SEO-critical and changes slowly. */
function contentOptions(ctx: ServerContext, tags: string[] = []): RequestContext {
  return { ...ctx, revalidate: 60, tags: ['content', ...tags] };
}

/** Live: never cached. A bid figure or a deal state served from a cache is a lie. */
function liveOptions(ctx: ServerContext): RequestContext {
  return { ...ctx, cache: 'no-store' };
}

export const serverApi = {
  /* ── Registries ─────────────────────────────────────────────────────── */
  taxonomyTree: (ctx: ServerContext = {}) => getTaxonomyTree(registryOptions(ctx)),
  attributes: (category: string, ctx: ServerContext = {}) =>
    getAttributes(category, registryOptions(ctx)),
  makes: (category?: string, ctx: ServerContext = {}) => getMakes(category, registryOptions(ctx)),
  models: (make: string, ctx: ServerContext = {}) => getModels(make, registryOptions(ctx)),
  variants: (model: string, year?: number, ctx: ServerContext = {}) =>
    getVariants(model, year, registryOptions(ctx)),
  formDefinition: (key: string, context?: string, ctx: ServerContext = {}) =>
    getFormDefinition(key, context, registryOptions(ctx)),
  workflowDefinition: (key: string, ctx: ServerContext = {}) =>
    getWorkflowDefinition(key, registryOptions(ctx)),
  features: (ctx: ServerContext = {}) => getFeatures(registryOptions(ctx)),
  i18n: (locale: string, namespace = 'web', ctx: ServerContext = {}) =>
    getI18nBundle(locale, namespace, registryOptions(ctx)),
  cities: (serviceableOnly = true, ctx: ServerContext = {}) =>
    getCities(serviceableOnly, registryOptions(ctx)),

  /* ── Content ────────────────────────────────────────────────────────── */
  search: (params: SearchParams, ctx: ServerContext = {}) => search(params, contentOptions(ctx)),
  listings: (params: ListingListParams = {}, ctx: ServerContext = {}) =>
    listListings(params, contentOptions(ctx)),
  listing: (listingId: string, ctx: ServerContext = {}) =>
    getListing(listingId, contentOptions(ctx, [`listing:${listingId}`])),

  /* ── Live ───────────────────────────────────────────────────────────── */
  me: (ctx: ServerContext = {}) => getMe(liveOptions(ctx)),
  workflow: (target: WorkflowTarget, ctx: ServerContext = {}) =>
    getWorkflowSnapshot(target, liveOptions(ctx)),
  pricingQuote: (request: PricingQuoteRequest, ctx: ServerContext = {}) =>
    getPricingQuote(request, liveOptions(ctx)),
  auctionLots: (params: AuctionLotListParams = {}, ctx: ServerContext = {}) =>
    listAuctionLots(params, liveOptions(ctx)),
  auctionLot: (lotId: string, ctx: ServerContext = {}) => getAuctionLot(lotId, liveOptions(ctx)),
  deals: (params: DealListParams = {}, ctx: ServerContext = {}) =>
    listDeals(params, liveOptions(ctx)),
  deal: (dealId: string, ctx: ServerContext = {}) => getDeal(dealId, liveOptions(ctx)),
};

export { ApiError } from './http';
export { getApiConfig, configureApi } from './config';
export * from './schemas';
