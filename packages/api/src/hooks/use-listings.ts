'use client';

/**
 * Listing hooks — the dealer's inventory table and the customer's detail page read from here.
 */

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  createListingDraft,
  getListing,
  listListings,
  updateListing,
} from '../endpoints/listings';
import type { ListingPage } from '../endpoints/listings';
import type { ListingDetail, ListingDraftRequest, ListingListParams } from '../schemas';
import { queryKeys } from './keys';

export function useListings(params: ListingListParams = {}) {
  return useQuery<ListingPage>({
    queryKey: queryKeys.listings(params),
    queryFn: () => listListings(params),
    staleTime: 30_000,
  });
}

export function useListing(listingId: string | undefined) {
  return useQuery<ListingDetail>({
    queryKey: queryKeys.listing(listingId ?? ''),
    queryFn: () => getListing(listingId as string),
    enabled: Boolean(listingId),
    staleTime: 60_000,
  });
}

export function useCreateListingDraft() {
  const queryClient = useQueryClient();
  return useMutation<ListingDetail, Error, ListingDraftRequest>({
    mutationFn: (request) => createListingDraft(request),
    onSuccess: (listing) => {
      queryClient.setQueryData(queryKeys.listing(listing.listingId), listing);
      void queryClient.invalidateQueries({ queryKey: [queryKeys.root[0], 'listings'] });
    },
  });
}

export function useUpdateListing(listingId: string) {
  const queryClient = useQueryClient();
  return useMutation<ListingDetail, Error, Partial<ListingDraftRequest>>({
    mutationFn: (patch) => updateListing(listingId, patch),
    onSuccess: (listing) => {
      queryClient.setQueryData(queryKeys.listing(listingId), listing);
    },
  });
}
