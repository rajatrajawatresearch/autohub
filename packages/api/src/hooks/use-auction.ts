'use client';

/**
 * Auction hooks.
 *
 * `useAuctionStream` folds broadcast events straight into the cached lot, so the bid figure and
 * the countdown move without a refetch. A bid still goes over HTTP with an idempotency key — the
 * socket is read-side only, which is what makes a dropped connection survivable.
 */

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import {
  getAuctionLot,
  getLotBids,
  listAuctionLots,
  openAuctionStream,
  placeBid,
} from '../endpoints/auction';
import type { AuctionLotPage, AuctionStreamStatus, BidPage } from '../endpoints/auction';
import { getAuthTokens } from '../http';
import type {
  AuctionEvent,
  AuctionLot,
  AuctionLotListParams,
  PlaceBidRequest,
  PlaceBidResponse,
} from '../schemas';
import { queryKeys } from './keys';

export function useAuctionLots(params: AuctionLotListParams = {}) {
  return useQuery<AuctionLotPage>({
    queryKey: queryKeys.auctionLots(params),
    queryFn: () => listAuctionLots(params),
    staleTime: 10_000,
  });
}

export function useAuctionLot(lotId: string | undefined) {
  return useQuery<AuctionLot>({
    queryKey: queryKeys.auctionLot(lotId ?? ''),
    queryFn: () => getAuctionLot(lotId as string),
    enabled: Boolean(lotId),
    /* The socket keeps this current; the short stale time only covers a reconnect gap. */
    staleTime: 5_000,
  });
}

export function useLotBids(lotId: string | undefined) {
  return useQuery<BidPage>({
    queryKey: queryKeys.lotBids(lotId ?? ''),
    queryFn: () => getLotBids(lotId as string),
    enabled: Boolean(lotId),
    staleTime: 5_000,
  });
}

export function usePlaceBid(lotId: string) {
  const queryClient = useQueryClient();
  return useMutation<PlaceBidResponse, Error, PlaceBidRequest>({
    mutationFn: (request) => placeBid(lotId, request),
    onSuccess: (response) => {
      queryClient.setQueryData(queryKeys.auctionLot(lotId), response.lot);
      void queryClient.invalidateQueries({ queryKey: queryKeys.lotBids(lotId) });
    },
    onError: () => {
      /* An outbid race means the figure on screen is stale. Fetch the truth, do not infer it. */
      void queryClient.invalidateQueries({ queryKey: queryKeys.auctionLot(lotId) });
    },
  });
}

export interface UseAuctionStreamResult {
  status: AuctionStreamStatus;
  lastEvent: AuctionEvent | null;
}

/**
 * Subscribes to a lot and merges events into the cached `AuctionLot`.
 *
 * @param onEvent Optional. Fires for every event — use it for the outbid toast, not for state.
 */
export function useAuctionStream(
  lotId: string | undefined,
  onEvent?: (event: AuctionEvent) => void,
): UseAuctionStreamResult {
  const queryClient = useQueryClient();
  const [status, setStatus] = useState<AuctionStreamStatus>('closed');
  const [lastEvent, setLastEvent] = useState<AuctionEvent | null>(null);

  useEffect(() => {
    if (!lotId) return;

    const key = queryKeys.auctionLot(lotId);

    const stop = openAuctionStream(lotId, {
      accessToken: getAuthTokens()?.accessToken,
      onStatus: setStatus,
      onEvent: (event) => {
        setLastEvent(event);
        onEvent?.(event);

        queryClient.setQueryData<AuctionLot>(key, (previous) => {
          if (!previous || event.seq <= previous.latestSeq) return previous;

          switch (event.type) {
            case 'bid.placed':
              return {
                ...previous,
                latestSeq: event.seq,
                currentBidMinor: event.data.amountMinor,
                minNextBidMinor:
                  event.data.minNextBidMinor ?? event.data.amountMinor + previous.incrementMinor,
                bidCount: event.data.bidCount ?? previous.bidCount + 1,
                highestBidderIsYou: event.data.highestBidderIsYou,
              };
            case 'lot.extended':
              return {
                ...previous,
                latestSeq: event.seq,
                endsAt: event.data.endsAt,
                extensionCount: previous.extensionCount + 1,
              };
            case 'lot.closed':
              return {
                ...previous,
                latestSeq: event.seq,
                state: {
                  ...previous.state,
                  code: event.data.outcome,
                  name: event.data.outcome === 'sold' ? 'Sold' : 'Unsold',
                  category: event.data.outcome === 'sold' ? 'succeeded' : 'failed',
                  isTerminal: true,
                },
              };
            default:
              return previous;
          }
        });

        if (event.type === 'bid.placed') {
          void queryClient.invalidateQueries({ queryKey: queryKeys.lotBids(lotId) });
        }
      },
    });

    return stop;
  }, [lotId, onEvent, queryClient]);

  return { status, lastEvent };
}
