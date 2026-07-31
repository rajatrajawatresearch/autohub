'use client';

/**
 * Deal hooks.
 */

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { getDeal, listDeals, makeOffer } from '../endpoints/deals';
import type { DealPage } from '../endpoints/deals';
import type { DealDetail, DealListParams, OfferRequest } from '../schemas';
import { queryKeys } from './keys';

export function useDeals(params: DealListParams = {}) {
  return useQuery<DealPage>({
    queryKey: queryKeys.deals(params),
    queryFn: () => listDeals(params),
    staleTime: 20_000,
  });
}

export function useDeal(dealId: string | undefined) {
  return useQuery<DealDetail>({
    queryKey: queryKeys.deal(dealId ?? ''),
    queryFn: () => getDeal(dealId as string),
    enabled: Boolean(dealId),
    staleTime: 20_000,
  });
}

export function useMakeOffer() {
  const queryClient = useQueryClient();
  return useMutation<DealDetail, Error, OfferRequest>({
    mutationFn: (request) => makeOffer(request),
    onSuccess: (deal) => {
      queryClient.setQueryData(queryKeys.deal(deal.dealId), deal);
      void queryClient.invalidateQueries({ queryKey: [queryKeys.root[0], 'deals'] });
    },
  });
}
