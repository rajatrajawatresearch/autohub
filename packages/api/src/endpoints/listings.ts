/**
 * Listings — the offer a vehicle is making. One shape for the card, one for the detail page.
 */

import type { z } from 'zod';
import { apiGet, apiPatch, apiPost } from '../http';
import type { RequestContext } from '../http';
import { ListingDetailSchema, ListingSummarySchema, cursorPageSchema } from '../schemas';
import type { ListingDetail, ListingDraftRequest, ListingListParams } from '../schemas';

export const ListingPageSchema = cursorPageSchema(ListingSummarySchema);
export type ListingPage = z.infer<typeof ListingPageSchema>;

export function listListings(
  params: ListingListParams = {},
  ctx: RequestContext = {},
): Promise<ListingPage> {
  return apiGet('/listings', ListingPageSchema, {
    ...ctx,
    query: {
      intent: params.intent,
      state: params.state,
      q: params.q,
      cursor: params.cursor ?? undefined,
      limit: params.limit,
    },
  });
}

export function getListing(listingId: string, ctx: RequestContext = {}): Promise<ListingDetail> {
  return apiGet(`/listings/${encodeURIComponent(listingId)}`, ListingDetailSchema, ctx);
}

export function createListingDraft(
  request: ListingDraftRequest,
  ctx: RequestContext & { idempotencyKey?: string } = {},
): Promise<ListingDetail> {
  return apiPost('/listings', ListingDetailSchema, request, ctx);
}

export function updateListing(
  listingId: string,
  patch: Partial<ListingDraftRequest>,
  ctx: RequestContext & { idempotencyKey?: string } = {},
): Promise<ListingDetail> {
  return apiPatch(
    `/listings/${encodeURIComponent(listingId)}`,
    ListingDetailSchema,
    patch,
    ctx,
  );
}
