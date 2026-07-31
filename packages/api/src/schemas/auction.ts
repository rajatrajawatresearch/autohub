/**
 * Auction — lots, bids, and the read-side socket.
 *
 * Bids are never placed over the socket. They go to `POST /auction/lots/{id}/bids` with an
 * `Idempotency-Key`, so a dropped connection can neither lose nor duplicate a bid. The socket is a
 * broadcast; on reconnect the client sends `?sinceSeq=` and receives the gap.
 *
 * PROVISIONAL beyond §11 of the contract: the lot and bid projections below are composed from
 * contract shapes pending the backend publishing the auction read model.
 */

import { z } from 'zod';
import { CurrencySchema, IdSchema, IsoDateTimeSchema, MinorUnitsSchema } from './common';
import { ListingSummarySchema } from './listings';
import { WorkflowStateSchema } from './workflow';

export const BidSchema = z.object({
  bidId: IdSchema,
  lotId: IdSchema,
  /** Monotonic per lot. The client's reconnect anchor and its stale-bid guard. */
  seq: z.number().int().nonnegative(),
  amountMinor: MinorUnitsSchema,
  /** `Dealer ••42` — bidders are masked from each other. */
  bidderMask: z.string(),
  placedAt: IsoDateTimeSchema,
  isYours: z.boolean().default(false),
  isAutoBid: z.boolean().default(false),
});
export type Bid = z.infer<typeof BidSchema>;

export const AuctionLotSchema = z.object({
  lotId: IdSchema,
  lotNumber: z.string().nullish(),
  listing: ListingSummarySchema,
  currency: CurrencySchema,
  state: WorkflowStateSchema,
  startsAt: IsoDateTimeSchema,
  endsAt: IsoDateTimeSchema,
  startPriceMinor: MinorUnitsSchema,
  /** The ladder step. The next legal bid is `currentBidMinor + incrementMinor`. */
  incrementMinor: MinorUnitsSchema,
  currentBidMinor: MinorUnitsSchema.nullish(),
  minNextBidMinor: MinorUnitsSchema,
  bidCount: z.number().int().nonnegative().default(0),
  /** The reserve amount is never exposed — only whether it has been met. */
  reserveMet: z.boolean().default(false),
  /** Highest sequence broadcast so far. Sent back as `seenBidSeq` to detect an outbid race. */
  latestSeq: z.number().int().nonnegative().default(0),
  highestBidderIsYou: z.boolean().default(false),
  yourMaxBidMinor: MinorUnitsSchema.nullish(),
  /** Anti-snipe extensions already spent. */
  extensionCount: z.number().int().nonnegative().default(0),
  inspectionReportUrl: z.string().nullish(),
});
export type AuctionLot = z.infer<typeof AuctionLotSchema>;

export const PlaceBidRequestSchema = z.object({
  amountMinor: MinorUnitsSchema,
  /** The highest seq the client had rendered. Stale → `auction.bid.outbid_race`, refresh not guess. */
  seenBidSeq: z.number().int().nonnegative().optional(),
  /** Proxy bidding: bid up to this automatically. */
  maxAmountMinor: MinorUnitsSchema.optional(),
});
export type PlaceBidRequest = z.infer<typeof PlaceBidRequestSchema>;

export const PlaceBidResponseSchema = z.object({
  bid: BidSchema,
  lot: AuctionLotSchema,
});
export type PlaceBidResponse = z.infer<typeof PlaceBidResponseSchema>;

export const AuctionLotListParamsSchema = z.object({
  /** Workflow state code — `live`, `scheduled`, `closed`. The registry says which exist. */
  state: z.string().optional(),
  city: z.string().optional(),
  categoryCode: z.string().optional(),
  cursor: z.string().nullish(),
  limit: z.number().int().positive().max(60).optional(),
});
export type AuctionLotListParams = z.infer<typeof AuctionLotListParamsSchema>;

/* ── Realtime, §11 ────────────────────────────────────────────────────────── */

export const BidPlacedEventSchema = z.object({
  lotId: IdSchema,
  amountMinor: MinorUnitsSchema,
  bidderMask: z.string(),
  placedAt: IsoDateTimeSchema,
  highestBidderIsYou: z.boolean().default(false),
  bidCount: z.number().int().nonnegative().optional(),
  minNextBidMinor: MinorUnitsSchema.optional(),
});

export const LotExtendedEventSchema = z.object({
  endsAt: IsoDateTimeSchema,
  /** `anti_snipe` — why the clock moved. The UI says so; a clock that jumps silently reads broken. */
  reason: z.string(),
});

export const LotClosedEventSchema = z.object({
  outcome: z.string(),
  winnerIsYou: z.boolean().default(false),
  finalAmountMinor: MinorUnitsSchema.nullish(),
});

export const AuctionEventSchema = z.discriminatedUnion('type', [
  z.object({
    seq: z.number().int().nonnegative(),
    type: z.literal('bid.placed'),
    data: BidPlacedEventSchema,
  }),
  z.object({
    seq: z.number().int().nonnegative(),
    type: z.literal('lot.extended'),
    data: LotExtendedEventSchema,
  }),
  z.object({
    seq: z.number().int().nonnegative(),
    type: z.literal('lot.closed'),
    data: LotClosedEventSchema,
  }),
]);
export type AuctionEvent = z.infer<typeof AuctionEventSchema>;
export type BidPlacedEvent = z.infer<typeof BidPlacedEventSchema>;
export type LotExtendedEvent = z.infer<typeof LotExtendedEventSchema>;
export type LotClosedEvent = z.infer<typeof LotClosedEventSchema>;
