'use client';

/**
 * Pricing. Nothing in the product computes a fee — every screen that shows one asks for it, and
 * shows the lines it gets back with the rule name attached.
 */

import { useMutation, useQuery } from '@tanstack/react-query';
import { getPricingQuote } from '../endpoints/platform';
import type { PricingQuote, PricingQuoteRequest } from '../schemas';
import { queryKeys } from './keys';

/**
 * A quote for a known context — the fee panel on a deal, the commission line on an auction lot.
 * Cached for a minute: a slab change mid-session is real, but not second-to-second.
 */
export function usePricingQuote(request: PricingQuoteRequest | undefined) {
  return useQuery<PricingQuote>({
    queryKey: queryKeys.pricing(request?.scope ?? '', request?.context),
    queryFn: () => getPricingQuote(request as PricingQuoteRequest),
    enabled: Boolean(request?.scope),
    staleTime: 60_000,
  });
}

/** For a moving amount — the bid box, where the fee has to follow what is being typed. */
export function useQuotePricing() {
  return useMutation<PricingQuote, Error, PricingQuoteRequest>({
    mutationFn: (request) => getPricingQuote(request),
  });
}
