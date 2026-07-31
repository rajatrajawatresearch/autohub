'use client';

/**
 * Valuation. A mutation, not a query: the homepage hero submits a plate and gets one answer, and
 * that answer is the moment the product is judged on. Nothing here re-fetches it in the background.
 */

import { useMutation } from '@tanstack/react-query';
import { createValuationQuote } from '../endpoints/valuation';
import type { ValuationBand, ValuationQuote, ValuationQuoteRequest } from '../schemas';

export function useValuationQuote() {
  return useMutation<ValuationQuote, Error, ValuationQuoteRequest>({
    mutationFn: (request) => createValuationQuote(request),
  });
}

/**
 * The figure the odometer rolls to — the one band flagged primary, or the first one if the server
 * did not flag any. Exactly one number is the headline; the rest are context.
 */
export function primaryValuation(quote: ValuationQuote | undefined): ValuationBand | undefined {
  if (!quote) return undefined;
  return quote.valuations.find((band) => band.isPrimary) ?? quote.valuations[0];
}
