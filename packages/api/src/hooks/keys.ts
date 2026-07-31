/**
 * Query keys, in one place.
 *
 * Every key starts `['autohub', <plane>, …]` so a portal can invalidate a whole plane — say, every
 * registry after a registry-version bump — without knowing what is cached inside it.
 */

import type { SearchParams, WorkflowTarget } from '../schemas';

const ROOT = 'autohub' as const;

export const queryKeys = {
  root: [ROOT] as const,

  registry: [ROOT, 'registry'] as const,
  taxonomyTree: () => [ROOT, 'registry', 'taxonomy', 'tree'] as const,
  attributes: (category: string) => [ROOT, 'registry', 'taxonomy', 'attributes', category] as const,
  makes: (category?: string) => [ROOT, 'registry', 'taxonomy', 'makes', category ?? 'all'] as const,
  models: (make: string) => [ROOT, 'registry', 'taxonomy', 'models', make] as const,
  variants: (model: string, year?: number) =>
    [ROOT, 'registry', 'taxonomy', 'variants', model, year ?? 'any'] as const,
  form: (key: string, context?: string) =>
    [ROOT, 'registry', 'form', key, context ?? 'default'] as const,
  workflowDefinition: (key: string) => [ROOT, 'registry', 'workflow', key] as const,
  features: () => [ROOT, 'registry', 'features'] as const,
  i18n: (locale: string, namespace: string) =>
    [ROOT, 'registry', 'i18n', locale, namespace] as const,
  cities: (serviceableOnly: boolean) => [ROOT, 'registry', 'cities', serviceableOnly] as const,

  workflow: (target: WorkflowTarget) =>
    [ROOT, 'workflow', target.module, target.resource, target.id] as const,

  pricing: (scope: string, context: unknown) => [ROOT, 'pricing', scope, context] as const,

  search: (params: SearchParams) => [ROOT, 'search', params] as const,

  listings: (params: unknown) => [ROOT, 'listings', 'list', params] as const,
  listing: (listingId: string) => [ROOT, 'listings', 'detail', listingId] as const,

  auctionLots: (params: unknown) => [ROOT, 'auction', 'lots', params] as const,
  auctionLot: (lotId: string) => [ROOT, 'auction', 'lot', lotId] as const,
  lotBids: (lotId: string) => [ROOT, 'auction', 'lot', lotId, 'bids'] as const,

  deals: (params: unknown) => [ROOT, 'deals', 'list', params] as const,
  deal: (dealId: string) => [ROOT, 'deals', 'detail', dealId] as const,

  me: () => [ROOT, 'auth', 'me'] as const,
};
