/**
 * Auction — lots, bids, and the read-side socket.
 *
 * A bid is a POST with an `Idempotency-Key`, never a socket message. The socket only tells you
 * what already happened, so a dropped connection can lose a *notification* but never a bid.
 */

import type { z } from 'zod';
import { apiGet, apiPost } from '../http';
import type { RequestContext } from '../http';
import { getApiConfig } from '../config';
import { subscribeMockAuction } from '../mock';
import {
  AuctionEventSchema,
  AuctionLotSchema,
  BidSchema,
  PlaceBidResponseSchema,
  cursorPageSchema,
} from '../schemas';
import type { AuctionEvent, AuctionLot, AuctionLotListParams, PlaceBidRequest, PlaceBidResponse } from '../schemas';

export const AuctionLotPageSchema = cursorPageSchema(AuctionLotSchema);
export type AuctionLotPage = z.infer<typeof AuctionLotPageSchema>;

const BidPageSchema = cursorPageSchema(BidSchema);
export type BidPage = z.infer<typeof BidPageSchema>;

export function listAuctionLots(
  params: AuctionLotListParams = {},
  ctx: RequestContext = {},
): Promise<AuctionLotPage> {
  return apiGet('/auction/lots', AuctionLotPageSchema, {
    ...ctx,
    query: {
      state: params.state,
      city: params.city,
      categoryCode: params.categoryCode,
      cursor: params.cursor ?? undefined,
      limit: params.limit,
    },
  });
}

export function getAuctionLot(lotId: string, ctx: RequestContext = {}): Promise<AuctionLot> {
  return apiGet(`/auction/lots/${encodeURIComponent(lotId)}`, AuctionLotSchema, ctx);
}

export function getLotBids(lotId: string, ctx: RequestContext = {}): Promise<BidPage> {
  return apiGet(`/auction/lots/${encodeURIComponent(lotId)}/bids`, BidPageSchema, ctx);
}

/**
 * Places a bid.
 *
 * Pass `idempotencyKey` derived from the user's gesture — a double-tap on a flaky connection then
 * replays into the same bid instead of two.
 */
export function placeBid(
  lotId: string,
  request: PlaceBidRequest,
  ctx: RequestContext & { idempotencyKey?: string } = {},
): Promise<PlaceBidResponse> {
  return apiPost(
    `/auction/lots/${encodeURIComponent(lotId)}/bids`,
    PlaceBidResponseSchema,
    request,
    ctx,
  );
}

/* ── Realtime ────────────────────────────────────────────────────────────── */

export type AuctionStreamStatus = 'connecting' | 'open' | 'closed' | 'fixtures';

export interface AuctionStreamOptions {
  /** Reconnect anchor. The server replays the gap from here. */
  sinceSeq?: number;
  accessToken?: string;
  onEvent: (event: AuctionEvent) => void;
  onStatus?: (status: AuctionStreamStatus) => void;
}

/**
 * Opens the lot's broadcast and returns the unsubscribe. Falls back to the fixture stream when
 * mocking or when there is no `WebSocket` (a server render), so a live auction view is
 * demonstrable before the socket exists.
 */
export function openAuctionStream(lotId: string, options: AuctionStreamOptions): () => void {
  const config = getApiConfig();
  const { onEvent, onStatus } = options;

  const emit = (raw: unknown) => {
    const parsed = AuctionEventSchema.safeParse(raw);
    if (parsed.success) onEvent(parsed.data);
  };

  const useFixtures = config.mock || typeof WebSocket === 'undefined';
  if (useFixtures) {
    onStatus?.('fixtures');
    return subscribeMockAuction(lotId, emit);
  }

  onStatus?.('connecting');
  const query = new URLSearchParams();
  if (options.accessToken) query.set('token', options.accessToken);
  if (options.sinceSeq !== undefined) query.set('sinceSeq', String(options.sinceSeq));

  let fixtureStop: (() => void) | null = null;
  let socket: WebSocket | null = null;

  try {
    socket = new WebSocket(
      `${config.wsUrl}/realtime/auction/${encodeURIComponent(lotId)}?${query.toString()}`,
    );
  } catch {
    onStatus?.('fixtures');
    return subscribeMockAuction(lotId, emit);
  }

  socket.onopen = () => onStatus?.('open');
  socket.onmessage = (message: MessageEvent<string>) => {
    try {
      emit(JSON.parse(message.data) as unknown);
    } catch {
      /* A frame we cannot read is a frame we ignore. The next one carries a higher seq. */
    }
  };
  socket.onerror = () => {
    if (!config.mockFallback || fixtureStop) return;
    onStatus?.('fixtures');
    fixtureStop = subscribeMockAuction(lotId, emit);
  };
  socket.onclose = () => onStatus?.('closed');

  return () => {
    fixtureStop?.();
    socket?.close();
  };
}
