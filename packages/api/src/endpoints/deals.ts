/**
 * Deals — the settlement spine. Whatever the intent was, it ends here.
 */

import type { z } from 'zod';
import { apiGet, apiPost } from '../http';
import type { RequestContext } from '../http';
import { DealDetailSchema, DealSummarySchema, cursorPageSchema } from '../schemas';
import type { DealDetail, DealListParams, OfferRequest } from '../schemas';

export const DealPageSchema = cursorPageSchema(DealSummarySchema);
export type DealPage = z.infer<typeof DealPageSchema>;

export function listDeals(
  params: DealListParams = {},
  ctx: RequestContext = {},
): Promise<DealPage> {
  return apiGet('/deals/deals', DealPageSchema, {
    ...ctx,
    query: {
      side: params.side,
      state: params.state,
      cursor: params.cursor ?? undefined,
      limit: params.limit,
    },
  });
}

export function getDeal(dealId: string, ctx: RequestContext = {}): Promise<DealDetail> {
  return apiGet(`/deals/deals/${encodeURIComponent(dealId)}`, DealDetailSchema, ctx);
}

export function makeOffer(
  request: OfferRequest,
  ctx: RequestContext & { idempotencyKey?: string } = {},
): Promise<DealDetail> {
  return apiPost('/deals/offers', DealDetailSchema, request, ctx);
}
