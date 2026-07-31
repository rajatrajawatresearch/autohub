/**
 * Valuation. `auth: false` on the first quote is deliberate — asking for a phone number before
 * showing a number is why users bounce.
 */

import { apiPost } from '../http';
import type { RequestContext } from '../http';
import { ValuationQuoteSchema } from '../schemas';
import type { ValuationQuote, ValuationQuoteRequest } from '../schemas';

export function createValuationQuote(
  request: ValuationQuoteRequest,
  ctx: RequestContext & { idempotencyKey?: string } = {},
): Promise<ValuationQuote> {
  return apiPost('/valuation/quotes', ValuationQuoteSchema, request, { ...ctx, auth: false });
}
